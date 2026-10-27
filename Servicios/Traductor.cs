using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Servicios
{
    /// Multi-idioma resuelto en el propio programa (no es una funcionalidad del
    /// navegador): antes de mandar la página al navegador, PaginaBase le pasa el
    /// HTML completo a esta clase y acá se traducen todos los textos.
    ///
    /// Motor de traducción: MyMemory Translation API (api.mymemory.translated.net),
    /// gratuita y sin necesidad de una API key para el volumen de un sistema
    /// como este.
    ///
    /// Para que la pantalla no quede lenta:
    ///   * cada texto se traduce UNA sola vez por idioma y queda guardado en
    ///     App_Data\Traducciones\&lt;idioma&gt;.txt, así que sobrevive a que se
    ///     reinicie el sitio: de la segunda vez en adelante no hay pedidos a la
    ///     API y la página sale igual de rápido que en español;
    ///   * los textos que faltan se piden en paralelo (de a 8) y con un tiempo
    ///     máximo de espera, en vez de uno por uno;
    ///   * si la API no contesta se deja el texto en español y NO se guarda en
    ///     la caché, para reintentarlo más adelante;
    ///   * una página no espera a la API más de 3 segundos en total: lo que
    ///     no se llegó a traducir sale en español y se traduce en la próxima;
    ///   * si la API falla (sin internet, demora o límite diario de la cuenta
    ///     gratuita), se deja de consultarla por 10 minutos, y un texto que la
    ///     API no pudo traducir no se vuelve a pedir por 30 minutos. Sin esto,
    ///     cada página en otro idioma volvía a esperar a la API por los mismos
    ///     textos y el sistema se ponía lento.
    public static class Traductor
    {
        public const string IdiomaPorDefecto = "es";

        /// Pedidos simultáneos a la API cuando hay textos nuevos.
        private const int PedidosEnParalelo = 8;

        /// Tiempo máximo que se espera a la API por cada texto.
        private const int MilisegundosDeEspera = 3000;

        /// Después de este tiempo una página no hace pedidos nuevos a la API.
        private const int MilisegundosPorPagina = 3000;

        private const int MinutosDePausaSiFallaLaApi = 10;
        private const int MinutosParaReintentarUnTexto = 30;

        private static readonly Dictionary<string, Dictionary<string, string>> cache =
            new Dictionary<string, Dictionary<string, string>>();
        private static readonly object candado = new object();

        /// Textos que la API no pudo traducir (clave: idioma + texto) y desde
        /// cuándo se pueden volver a pedir.
        private static readonly Dictionary<string, DateTime> sinTraduccion = new Dictionary<string, DateTime>();

        /// Hasta cuándo no se consulta la API porque falló.
        private static DateTime apiPausadaHasta = DateTime.MinValue;

        /// La API rechazó el pedido por el límite de uso de la cuenta gratuita.
        private class LimiteDeLaApiException : Exception
        {
        }

        #region Traducción de un texto suelto

        public static string Traducir(string texto, string idiomaDestino)
        {
            if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(idiomaDestino) ||
                idiomaDestino == IdiomaPorDefecto || texto.Trim().Length == 0)
            {
                return texto;
            }

            Dictionary<string, string> traducciones = TraducirVarios(new string[] { texto }, idiomaDestino);

            string resultado;
            return traducciones.TryGetValue(texto, out resultado) ? resultado : texto;
        }

        #endregion

        #region Traducción de la página entera

        // Un texto de la página es lo que hay entre dos etiquetas. Los bloques
        // de script, de estilo, los comentarios y el contenido de un textarea
        // se saltean enteros: ahí no hay nada para traducir y tocarlo rompería
        // la página. También los select y span marcados con translate="no"
        // (el atributo estándar de HTML para "no traducir"), como el selector
        // de idioma, donde cada idioma va con su propio nombre. Las etiquetas
        // de apertura se capturan aparte, para traducir los textos que viven
        // en atributos (ver TraducirEtiqueta).
        private static readonly Regex Segmentos = new Regex(
            @"<script\b[^>]*>.*?</script>|<style\b[^>]*>.*?</style>|" +
            @"<textarea\b[^>]*>.*?</textarea>|<!--.*?-->|" +
            @"<(?<sinTraducir>select|span)\b[^>]*\btranslate\s*=\s*[""']no[""'][^>]*>.*?</\k<sinTraducir>\s*>|" +
            @"(?<etiqueta><[a-zA-Z][^<>]*>)|" +
            @"(?<=>)(?<texto>[^<>]+)(?=<)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ConLetras = new Regex(@"\p{L}{2,}", RegexOptions.Compiled);

        private static readonly Regex Atributo = new Regex(
            @"(?<nombre>[a-zA-Z_:][-a-zA-Z0-9_:.]*)(?<igual>\s*=\s*)(?<comilla>[""'])(?<valor>.*?)\k<comilla>",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex NombreDeEtiqueta = new Regex(@"^<(?<nombre>[a-zA-Z][a-zA-Z0-9]*)", RegexOptions.Compiled);

        private static readonly Regex TipoDeInput = new Regex(
            @"\btype\s*=\s*[""']?(?<tipo>[a-zA-Z]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Mensaje de un confirm('...') de un onclick. ASP.NET puede escribir
        // las comillas simples tal cual o como &#39;.
        private static readonly Regex MensajeDeConfirm = new Regex(
            @"confirm\((?<comilla>&#39;|')(?<mensaje>.*?)(?<!\\)\k<comilla>\)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        /// Traduce todos los textos visibles del HTML de una página: los que
        /// están entre etiqueta y etiqueta y los que el navegador muestra desde
        /// un atributo (el texto de los botones, los placeholder, los title y
        /// los mensajes de confirmación). El ViewState, los scripts y el resto
        /// de los atributos quedan intactos.
        public static string TraducirHtml(string html, string idiomaDestino)
        {
            if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(idiomaDestino) ||
                idiomaDestino == IdiomaPorDefecto)
            {
                return html;
            }

            // Primera pasada: juntar los textos a traducir (sin repetidos).
            List<string> pendientes = new List<string>();
            HashSet<string> vistos = new HashSet<string>();

            foreach (Match m in Segmentos.Matches(html))
            {
                if (m.Groups["etiqueta"].Success)
                {
                    foreach (string texto in TextosDeLaEtiqueta(m.Groups["etiqueta"].Value))
                    {
                        if (vistos.Add(texto))
                        {
                            pendientes.Add(texto);
                        }
                    }
                    continue;
                }

                if (!m.Groups["texto"].Success)
                {
                    continue;
                }

                string limpio = HttpUtility.HtmlDecode(m.Groups["texto"].Value).Trim();
                if (EsTraducible(limpio) && vistos.Add(limpio))
                {
                    pendientes.Add(limpio);
                }
            }

            if (pendientes.Count == 0)
            {
                return html;
            }

            Dictionary<string, string> traducciones = TraducirVarios(pendientes, idiomaDestino);

            // Segunda pasada: reemplazar respetando los espacios de los bordes.
            return Segmentos.Replace(html, delegate(Match m)
            {
                if (m.Groups["etiqueta"].Success)
                {
                    return TraducirEtiqueta(m.Groups["etiqueta"].Value, traducciones);
                }

                if (!m.Groups["texto"].Success)
                {
                    return m.Value;
                }

                string original = m.Groups["texto"].Value;
                string limpio = HttpUtility.HtmlDecode(original).Trim();

                string traducido;
                if (!EsTraducible(limpio) || !traducciones.TryGetValue(limpio, out traducido) ||
                    traducido == limpio)
                {
                    return m.Value;
                }

                int izquierda = original.Length - original.TrimStart().Length;
                int derecha = original.Length - original.TrimEnd().Length;

                return original.Substring(0, izquierda) +
                       HttpUtility.HtmlEncode(traducido) +
                       original.Substring(original.Length - derecha);
            });
        }

        /// Textos de una etiqueta que el navegador le muestra al usuario. Del
        /// value solo cuentan los botones: en un cuadro de texto el value es
        /// un dato que cargó el usuario y no se toca. Cambiar el texto de un
        /// botón no afecta el postback, porque ASP.NET identifica el botón
        /// presionado por su name, no por lo que dice.
        private static List<string> TextosDeLaEtiqueta(string etiqueta)
        {
            List<string> textos = new List<string>();
            if (!PuedeTenerTextosVisibles(etiqueta))
            {
                return textos;
            }

            bool esBoton = EsBoton(etiqueta);

            foreach (Match atributo in Atributo.Matches(etiqueta))
            {
                string nombre = atributo.Groups["nombre"].Value.ToLowerInvariant();
                string valor = atributo.Groups["valor"].Value;

                if (EsAtributoConTexto(nombre, esBoton))
                {
                    AgregarSiEsTraducible(textos, HttpUtility.HtmlDecode(valor).Trim());
                }
                else if (nombre == "onclick")
                {
                    foreach (Match confirm in MensajeDeConfirm.Matches(valor))
                    {
                        AgregarSiEsTraducible(textos, MensajeDeConfirmLegible(confirm));
                    }
                }
            }

            return textos;
        }

        private static string TraducirEtiqueta(string etiqueta, Dictionary<string, string> traducciones)
        {
            if (!PuedeTenerTextosVisibles(etiqueta))
            {
                return etiqueta;
            }

            bool esBoton = EsBoton(etiqueta);

            return Atributo.Replace(etiqueta, delegate(Match atributo)
            {
                string nombre = atributo.Groups["nombre"].Value.ToLowerInvariant();
                string valor = atributo.Groups["valor"].Value;
                string comilla = atributo.Groups["comilla"].Value;
                string traducido;

                if (EsAtributoConTexto(nombre, esBoton))
                {
                    string limpio = HttpUtility.HtmlDecode(valor).Trim();
                    if (!traducciones.TryGetValue(limpio, out traducido) || traducido == limpio)
                    {
                        return atributo.Value;
                    }
                    return ArmarAtributo(atributo, comilla, CodificarParaAtributo(traducido, comilla));
                }

                if (nombre != "onclick")
                {
                    return atributo.Value;
                }

                string nuevoValor = MensajeDeConfirm.Replace(valor, delegate(Match confirm)
                {
                    string mensaje = MensajeDeConfirmLegible(confirm);
                    string traduccion;
                    if (!traducciones.TryGetValue(mensaje, out traduccion) || traduccion == mensaje)
                    {
                        return confirm.Value;
                    }

                    // Primero se escapa como texto de JavaScript y después como
                    // atributo HTML, que es el orden inverso al que lo lee el navegador.
                    string js = traduccion.Replace("\\", "\\\\").Replace("'", "\\'")
                                          .Replace("\r", " ").Replace("\n", " ")
                                          .Replace("\u2028", " ").Replace("\u2029", " ");
                    string delimitador = confirm.Groups["comilla"].Value;
                    return "confirm(" + delimitador + CodificarParaAtributo(js, comilla) + delimitador + ")";
                });

                return ArmarAtributo(atributo, comilla, nuevoValor);
            });
        }

        /// Las etiquetas de cierre, las de la estructura y las que no muestran
        /// texto propio se descartan rápido, sin mirar sus atributos.
        private static bool PuedeTenerTextosVisibles(string etiqueta)
        {
            return etiqueta.IndexOf('=') >= 0 &&
                   (etiqueta.IndexOf("value", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    etiqueta.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    etiqueta.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    etiqueta.IndexOf("alt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    etiqueta.IndexOf("confirm(", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool EsBoton(string etiqueta)
        {
            Match nombre = NombreDeEtiqueta.Match(etiqueta);
            if (!nombre.Success ||
                !string.Equals(nombre.Groups["nombre"].Value, "input", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Match tipo = TipoDeInput.Match(etiqueta);
            if (!tipo.Success)
            {
                return false;
            }

            string valor = tipo.Groups["tipo"].Value.ToLowerInvariant();
            return valor == "submit" || valor == "button" || valor == "reset";
        }

        private static bool EsAtributoConTexto(string nombre, bool esBoton)
        {
            return nombre == "placeholder" || nombre == "title" || nombre == "alt" ||
                   (esBoton && nombre == "value");
        }

        private static string MensajeDeConfirmLegible(Match confirm)
        {
            return HttpUtility.HtmlDecode(confirm.Groups["mensaje"].Value)
                              .Replace("\\'", "'").Replace("\\\\", "\\").Trim();
        }

        private static string ArmarAtributo(Match atributo, string comilla, string valor)
        {
            return atributo.Groups["nombre"].Value + atributo.Groups["igual"].Value + comilla + valor + comilla;
        }

        private static string CodificarParaAtributo(string texto, string comilla)
        {
            string codificado = texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            return comilla == "'"
                ? codificado.Replace("'", "&#39;")
                : codificado.Replace("\"", "&quot;");
        }

        private static void AgregarSiEsTraducible(List<string> textos, string texto)
        {
            if (EsTraducible(texto) && !textos.Contains(texto))
            {
                textos.Add(texto);
            }
        }

        /// Se traducen los textos que tienen alguna palabra de verdad: no los
        /// números sueltos, los símbolos, los espacios ni los textos larguísimos.
        private static bool EsTraducible(string texto)
        {
            return !string.IsNullOrEmpty(texto) &&
                   texto.Length >= 2 && texto.Length <= 300 &&
                   ConLetras.IsMatch(texto);
        }

        #endregion

        #region Caché y pedidos a la API

        /// Devuelve la traducción de cada texto. Los que ya están en la caché
        /// salen de ahí; el resto se pide a la API en paralelo.
        public static Dictionary<string, string> TraducirVarios(IList<string> textos, string idiomaDestino)
        {
            Dictionary<string, string> resultado = new Dictionary<string, string>();
            if (textos == null || textos.Count == 0)
            {
                return resultado;
            }

            Dictionary<string, string> diccionario = CacheDelIdioma(idiomaDestino);
            List<string> faltantes = new List<string>();
            DateTime ahora = DateTime.Now;

            lock (candado)
            {
                foreach (string texto in textos)
                {
                    string traducido;
                    DateTime reintentarDesde;
                    if (diccionario.TryGetValue(texto, out traducido))
                    {
                        resultado[texto] = traducido;
                    }
                    else if (!resultado.ContainsKey(texto))
                    {
                        resultado[texto] = texto;
                        if (!sinTraduccion.TryGetValue(idiomaDestino + "\n" + texto, out reintentarDesde) ||
                            reintentarDesde <= ahora)
                        {
                            faltantes.Add(texto);
                        }
                    }
                }

                if (faltantes.Count == 0 || ahora < apiPausadaHasta)
                {
                    return resultado;
                }
            }

            string[] nuevos = new string[faltantes.Count];
            bool[] fallo = new bool[faltantes.Count];
            ParallelOptions opciones = new ParallelOptions();
            opciones.MaxDegreeOfParallelism = PedidosEnParalelo;
            Stopwatch reloj = Stopwatch.StartNew();

            Parallel.For(0, faltantes.Count, opciones, delegate(int i)
            {
                // Pasado el tiempo de la página, o con la API en pausa, el
                // texto queda en español por ahora y se pide en otra página.
                if (reloj.ElapsedMilliseconds > MilisegundosPorPagina || ApiPausada())
                {
                    return;
                }

                try
                {
                    nuevos[i] = TraducirConMyMemory(faltantes[i], idiomaDestino);
                    fallo[i] = string.IsNullOrEmpty(nuevos[i]);
                }
                catch (Exception)
                {
                    // Sin internet, demora o límite diario superado: se deja el
                    // texto en español y se deja de consultar la API un rato.
                    PausarApi();
                }
            });

            List<string> paraGuardar = new List<string>();
            lock (candado)
            {
                for (int i = 0; i < faltantes.Count; i++)
                {
                    if (fallo[i])
                    {
                        sinTraduccion[idiomaDestino + "\n" + faltantes[i]] = ahora.AddMinutes(MinutosParaReintentarUnTexto);
                    }
                    if (string.IsNullOrEmpty(nuevos[i]))
                    {
                        continue;
                    }
                    diccionario[faltantes[i]] = nuevos[i];
                    resultado[faltantes[i]] = nuevos[i];
                    paraGuardar.Add(Escapar(faltantes[i]) + "\t" + Escapar(nuevos[i]));
                }
            }

            GuardarEnDisco(idiomaDestino, paraGuardar);
            return resultado;
        }

        private static bool ApiPausada()
        {
            lock (candado)
            {
                return DateTime.Now < apiPausadaHasta;
            }
        }

        private static void PausarApi()
        {
            lock (candado)
            {
                apiPausadaHasta = DateTime.Now.AddMinutes(MinutosDePausaSiFallaLaApi);
            }
        }

        private static Dictionary<string, string> CacheDelIdioma(string idioma)
        {
            lock (candado)
            {
                Dictionary<string, string> diccionario;
                if (cache.TryGetValue(idioma, out diccionario))
                {
                    return diccionario;
                }

                diccionario = LeerDeDisco(idioma);
                cache[idioma] = diccionario;
                return diccionario;
            }
        }

        private static string TraducirConMyMemory(string texto, string idiomaDestino)
        {
            string url = "https://api.mymemory.translated.net/get?q=" +
                         Uri.EscapeDataString(texto) +
                         "&langpair=" + IdiomaPorDefecto + "|" + idiomaDestino;

            // Con un correo, MyMemory permite 50.000 caracteres por día en vez
            // de 5.000 (Web.config, clave "CorreoTraductor"; es opcional).
            string correo = ConfigurationManager.AppSettings["CorreoTraductor"];
            if (!string.IsNullOrEmpty(correo))
            {
                url += "&de=" + Uri.EscapeDataString(correo.Trim());
            }

            using (ClienteConEspera cliente = new ClienteConEspera())
            {
                cliente.Encoding = Encoding.UTF8;
                cliente.Headers["User-Agent"] = "IntelligentStock/1.0";
                string json = cliente.DownloadString(url);
                string traducido = ExtraerTraduccion(json);

                // La API devuelve el texto en mayúsculas y con un aviso cuando
                // rechaza el pedido. Si es por el límite diario, se pausa la
                // API; si es por el texto (por ejemplo, demasiado largo), solo
                // ese texto queda sin traducir.
                if (!string.IsNullOrEmpty(traducido) &&
                    traducido.IndexOf("MYMEMORY WARNING", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new LimiteDeLaApiException();
                }
                if (string.IsNullOrEmpty(traducido) ||
                    traducido.IndexOf("QUERY LENGTH LIMIT", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return null;
                }

                return traducido;
            }
        }

        /// WebClient no deja configurar el tiempo de espera: se hereda para
        /// poder hacerlo y que una API lenta no deje la pantalla colgada.
        private class ClienteConEspera : WebClient
        {
            protected override WebRequest GetWebRequest(Uri direccion)
            {
                WebRequest pedido = base.GetWebRequest(direccion);
                if (pedido != null)
                {
                    pedido.Timeout = MilisegundosDeEspera;
                }
                return pedido;
            }
        }

        // Se hace con una expresión regular, sin agregar una dependencia a una
        // biblioteca de JSON, porque solo interesa un campo de la respuesta:
        // {"responseData":{"translatedText":"..."}, ...}
        private static string ExtraerTraduccion(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            Match m = Regex.Match(json, "\"translatedText\"\\s*:\\s*\"(.*?)(?<!\\\\)\"");
            return m.Success ? DesescaparJson(m.Groups[1].Value) : null;
        }

        private static string DesescaparJson(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return texto;
            }

            StringBuilder salida = new StringBuilder(texto.Length);

            for (int i = 0; i < texto.Length; i++)
            {
                if (texto[i] != '\\' || i == texto.Length - 1)
                {
                    salida.Append(texto[i]);
                    continue;
                }

                i++;
                switch (texto[i])
                {
                    case '"': salida.Append('"'); break;
                    case '\\': salida.Append('\\'); break;
                    case '/': salida.Append('/'); break;
                    case 'n': salida.Append(' '); break;
                    case 'r': break;
                    case 't': salida.Append(' '); break;
                    case 'b': break;
                    case 'f': break;

                    // La API devuelve las letras con acento como ê: sin esto,
                    // en la pantalla se ve el escape en vez de la letra.
                    case 'u':
                        int codigo;
                        if (i + 4 < texto.Length &&
                            int.TryParse(texto.Substring(i + 1, 4),
                                         System.Globalization.NumberStyles.HexNumber,
                                         System.Globalization.CultureInfo.InvariantCulture,
                                         out codigo))
                        {
                            salida.Append((char)codigo);
                            i += 4;
                        }
                        else
                        {
                            salida.Append("\\u");
                        }
                        break;

                    default:
                        salida.Append('\\').Append(texto[i]);
                        break;
                }
            }

            return salida.ToString();
        }

        #endregion

        #region Archivo de la caché

        /// Carpeta donde se guardan las traducciones ya resueltas. Si no hay
        /// una petición web en curso (por ejemplo, en una prueba de consola),
        /// la caché queda solo en memoria.
        private static string CarpetaDeLaCache()
        {
            try
            {
                if (HttpContext.Current == null)
                {
                    return null;
                }

                string carpeta = HttpContext.Current.Server.MapPath("~/App_Data/Traducciones");
                if (!Directory.Exists(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                }
                return carpeta;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ArchivoDelIdioma(string idioma)
        {
            string carpeta = CarpetaDeLaCache();
            if (carpeta == null)
            {
                return null;
            }

            // El idioma viene de una lista cerrada (PreferenciaIdioma), pero se
            // filtra igual para no armar una ruta con lo que venga.
            StringBuilder limpio = new StringBuilder();
            foreach (char c in idioma)
            {
                if (char.IsLetter(c))
                {
                    limpio.Append(char.ToLowerInvariant(c));
                }
            }

            return limpio.Length == 0 ? null : Path.Combine(carpeta, limpio + ".txt");
        }

        private static Dictionary<string, string> LeerDeDisco(string idioma)
        {
            Dictionary<string, string> diccionario = new Dictionary<string, string>();
            string archivo = ArchivoDelIdioma(idioma);

            if (archivo == null || !File.Exists(archivo))
            {
                return diccionario;
            }

            try
            {
                foreach (string linea in File.ReadAllLines(archivo, Encoding.UTF8))
                {
                    int corte = linea.IndexOf('\t');
                    if (corte <= 0)
                    {
                        continue;
                    }
                    diccionario[Desescapar(linea.Substring(0, corte))] =
                        Desescapar(linea.Substring(corte + 1));
                }
            }
            catch (Exception)
            {
                // Un archivo de caché ilegible no puede tirar abajo la pantalla.
            }

            return diccionario;
        }

        private static void GuardarEnDisco(string idioma, List<string> lineas)
        {
            if (lineas == null || lineas.Count == 0)
            {
                return;
            }

            string archivo = ArchivoDelIdioma(idioma);
            if (archivo == null)
            {
                return;
            }

            try
            {
                lock (candado)
                {
                    File.AppendAllLines(archivo, lineas, Encoding.UTF8);
                }
            }
            catch (Exception)
            {
                // Si no se puede escribir (permisos), la caché sigue funcionando
                // en memoria mientras viva el proceso.
            }
        }

        private static string Escapar(string texto)
        {
            return texto.Replace("\\", "\\\\").Replace("\t", "\\t")
                        .Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string Desescapar(string texto)
        {
            StringBuilder salida = new StringBuilder(texto.Length);
            for (int i = 0; i < texto.Length; i++)
            {
                if (texto[i] != '\\' || i == texto.Length - 1)
                {
                    salida.Append(texto[i]);
                    continue;
                }

                i++;
                switch (texto[i])
                {
                    case 't': salida.Append('\t'); break;
                    case 'r': salida.Append('\r'); break;
                    case 'n': salida.Append('\n'); break;
                    case '\\': salida.Append('\\'); break;
                    default: salida.Append('\\').Append(texto[i]); break;
                }
            }
            return salida.ToString();
        }

        #endregion
    }
}
