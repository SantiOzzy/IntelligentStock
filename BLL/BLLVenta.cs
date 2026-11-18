using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// CU-006-037 Descontar stock por venta.
    ///
    /// Cada venta descuenta del stock lo que consume la receta (R8) y queda en
    /// las tablas Venta y ConsumoVenta, que son el historial de consumo con el
    /// que se entrena la predicción de demanda (CU-006-033) y el que usan el
    /// reporte gerencial y el servicio web de ventas. Además queda auditada en
    /// la bitácora (quién la cargó y cuándo).
    ///
    /// Las ventas las registra el punto de venta del local, que es el que toma
    /// los pedidos y factura. IntelligentStock las recibe de cuatro formas, que
    /// pasan todas por las mismas reglas:
    ///   - una venta suelta (carga manual),
    ///   - el cierre del día: todas las recetas vendidas en un día, de una vez,
    ///   - el archivo que exporta el punto de venta (CSV),
    ///   - el servicio web WebServiceVentas, que el punto de venta puede llamar
    ///     por cada venta (R9: solo desde orígenes autorizados).
    ///
    /// La venta ya ocurrió: si el stock del sistema no alcanza, no se rechaza.
    /// El insumo queda en cero y se avisa la diferencia, que se corrige con un
    /// ajuste de stock (CU-005-032).
    public class BLLVenta
    {
        /// Acción con la que cada registro de ventas queda en la bitácora.
        public const string AccionVenta = "Registro de ventas";

        public const string OrigenManual = "Carga manual";
        public const string OrigenCierre = "Cierre del día";
        public const string OrigenArchivo = "Archivo del punto de venta";
        public const string OrigenServicioWeb = "Punto de venta (servicio web)";

        /// Unidades que se pueden registrar en una sola venta.
        public const decimal CantidadMaxima = 10000m;

        /// Hasta cuántos días hacia atrás se puede registrar una venta.
        public const int DiasMaximosHaciaAtras = 365;

        /// Límites de la importación del archivo del punto de venta.
        public const int LineasMaximasPorArchivo = 5000;
        public const int BytesMaximosPorArchivo = 2 * 1024 * 1024;

        private const int LargoMaximoDescripcion = 500;

        private readonly DALVenta dalVenta = new DALVenta();
        private readonly DALInsumo dalInsumo = new DALInsumo();
        private readonly BLLReceta bllReceta = new BLLReceta();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();
        private readonly ControladorStock controladorStock = new ControladorStock();

        public BLLVenta()
        {
            // Patrón Observer: igual que en el ajuste de stock, si una venta deja
            // a un insumo en su mínimo se avisa por correo y queda en la bitácora.
            controladorStock.Suscribir(new NotificadorEmailStock());
            controladorStock.Suscribir(new BitacoraAlertaStock());
        }

        /// Recetas que se pueden vender: activas y con insumos cargados.
        public List<BEReceta> TraerRecetasVendibles()
        {
            List<BEReceta> recetas = new List<BEReceta>();
            foreach (BEReceta receta in bllReceta.TraerActivas())
            {
                if (receta.CantidadInsumos > 0)
                {
                    recetas.Add(receta);
                }
            }
            return recetas;
        }

        #region Vista previa

        /// Lo que se va a descontar de cada insumo, sin grabar nada, para
        /// mostrarlo antes de confirmar.
        public List<BEConsumoVenta> Previsualizar(int idReceta, decimal cantidad)
        {
            BEReceta receta = ObtenerRecetaVendible(idReceta);
            ValidarCantidad(cantidad);

            BEVenta venta = new BEVenta();
            venta.IdReceta = receta.IdReceta;
            venta.Cantidad = cantidad;
            List<BEConsumoVenta> consumos = Previsualizar(new List<BEVenta> { venta });

            foreach (BEConsumoVenta consumo in consumos)
            {
                foreach (BEDetalleReceta detalle in receta.Detalles)
                {
                    if (detalle.IdInsumo == consumo.IdInsumo)
                    {
                        consumo.ConsumoUnitario = detalle.Cantidad;
                    }
                }
            }
            return consumos;
        }

        /// Lo que se va a descontar de cada insumo por varias ventas juntas (el
        /// cierre del día o un archivo), sumado por insumo.
        public List<BEConsumoVenta> Previsualizar(List<BEVenta> ventas)
        {
            Dictionary<int, BEConsumoVenta> porInsumo = new Dictionary<int, BEConsumoVenta>();
            List<BEConsumoVenta> consumos = new List<BEConsumoVenta>();
            Dictionary<int, BEReceta> recetas = new Dictionary<int, BEReceta>();

            foreach (BEVenta venta in ventas)
            {
                BEReceta receta;
                if (!recetas.TryGetValue(venta.IdReceta, out receta))
                {
                    receta = bllReceta.ObtenerPorId(venta.IdReceta);
                    recetas[venta.IdReceta] = receta;
                }
                if (receta == null)
                {
                    continue;
                }

                foreach (BEDetalleReceta detalle in receta.Detalles)
                {
                    BEConsumoVenta consumo;
                    if (!porInsumo.TryGetValue(detalle.IdInsumo, out consumo))
                    {
                        BEInsumo insumo = dalInsumo.ObtenerPorId(detalle.IdInsumo);
                        consumo = new BEConsumoVenta();
                        consumo.IdInsumo = detalle.IdInsumo;
                        consumo.NombreInsumo = detalle.NombreInsumo;
                        consumo.UnidadDeMedida = detalle.UnidadDeMedida;
                        consumo.StockAnterior = insumo == null ? 0m : insumo.Stock;
                        consumo.StockMinimo = insumo == null ? 0m : insumo.StockMinimo;
                        porInsumo[detalle.IdInsumo] = consumo;
                        consumos.Add(consumo);
                    }
                    // Igual que ROUND de SQL Server, que es el que descuenta de verdad.
                    consumo.ConsumoTotal += Math.Round(detalle.Cantidad * venta.Cantidad, 2, MidpointRounding.AwayFromZero);
                }
            }

            foreach (BEConsumoVenta consumo in consumos)
            {
                consumo.StockResultante = Math.Max(0m, consumo.StockAnterior - consumo.ConsumoTotal);
            }
            consumos.Sort(delegate(BEConsumoVenta a, BEConsumoVenta b)
            {
                return string.Compare(a.NombreInsumo, b.NombreInsumo, StringComparison.CurrentCultureIgnoreCase);
            });
            return consumos;
        }

        /// Ventas ya registradas con esa fecha, para avisar antes de cargar un
        /// cierre del día o un archivo que podrían repetirlas.
        public List<BEVenta> TraerVentasDelDia(DateTime fecha)
        {
            return dalVenta.TraerVentas(fecha.Date, fecha.Date);
        }

        #endregion

        #region Registro

        /// Una venta suelta (carga manual).
        public List<BEConsumoVenta> Registrar(int idReceta, decimal cantidad, DateTime? fecha)
        {
            if (idReceta <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002", "Elija la receta vendida."));
            }

            BEVenta venta = new BEVenta();
            venta.IdReceta = idReceta;
            venta.Cantidad = cantidad;
            venta.Fecha = ValidarFecha(fecha);

            List<BEConsumoVenta> consumos = RegistrarVentas(new List<BEVenta> { venta }, OrigenManual, true, null);

            BEReceta receta = bllReceta.ObtenerPorId(idReceta);
            foreach (BEConsumoVenta consumo in consumos)
            {
                foreach (BEDetalleReceta detalle in receta.Detalles)
                {
                    if (detalle.IdInsumo == consumo.IdInsumo)
                    {
                        consumo.ConsumoUnitario = detalle.Cantidad;
                    }
                }
            }
            return consumos;
        }

        /// Cierre del día: todas las recetas vendidas en un día, en una sola
        /// operación. Las líneas sin cantidad no se registran.
        public List<BEConsumoVenta> RegistrarCierreDelDia(List<BEVenta> ventas, DateTime? fecha)
        {
            DateTime dia = ValidarFecha(fecha);
            List<BEVenta> conCantidad = new List<BEVenta>();
            foreach (BEVenta venta in ventas)
            {
                if (venta.Cantidad != 0)
                {
                    venta.Fecha = dia;
                    conCantidad.Add(venta);
                }
            }
            if (conCantidad.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Ingrese la cantidad vendida de al menos una receta."));
            }

            return RegistrarVentas(conCantidad, OrigenCierre, true,
                "Cierre del día del " + dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }

        /// Registra las ventas válidas de un archivo ya leído con LeerArchivo.
        public List<BEConsumoVenta> RegistrarImportadas(List<BEVenta> ventas, string nombreArchivo)
        {
            if (ventas == null || ventas.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "El archivo no tiene ventas para registrar."));
            }
            foreach (BEVenta venta in ventas)
            {
                venta.Fecha = ValidarFecha(venta.Fecha);
            }

            return RegistrarVentas(ventas, OrigenArchivo, true,
                "Importación del archivo " + (nombreArchivo ?? string.Empty));
        }

        /// Una venta que informa el punto de venta por el servicio web. La receta
        /// se indica por su número o por su nombre. No hay usuario con sesión: el
        /// registro queda en la bitácora a nombre del sistema.
        public List<BEConsumoVenta> RegistrarDesdePuntoDeVenta(string receta, decimal cantidad, DateTime fecha)
        {
            BEReceta encontrada = BuscarRecetaVendible(receta, IndiceDeRecetas());
            if (encontrada == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013",
                    "No hay una receta activa con insumos que se llame \"" + receta + "\"."));
            }

            BEVenta venta = new BEVenta();
            venta.IdReceta = encontrada.IdReceta;
            venta.Cantidad = cantidad;
            venta.Fecha = ValidarFecha(fecha);
            return RegistrarVentas(new List<BEVenta> { venta }, OrigenServicioWeb, false, null);
        }

        /// Valida las ventas, las graba y descuenta el stock en una sola
        /// transacción (con el control de dígitos verificadores), deja el
        /// registro en la bitácora y avisa por el Observer los insumos que
        /// quedaron en su mínimo. Devuelve lo descontado de cada insumo.
        private List<BEConsumoVenta> RegistrarVentas(List<BEVenta> ventas, string origen, bool conUsuario, string titulo)
        {
            Dictionary<int, BEReceta> recetas = new Dictionary<int, BEReceta>();
            foreach (BEVenta venta in ventas)
            {
                if (!recetas.ContainsKey(venta.IdReceta))
                {
                    recetas[venta.IdReceta] = ObtenerRecetaVendible(venta.IdReceta);
                }
                venta.NombreReceta = recetas[venta.IdReceta].Nombre;
                ValidarCantidad(venta.Cantidad);
            }

            List<BEConsumoVenta> consumos = null;
            bool registrado = bllDV.EjecutarControlado(
                () => dalVenta.Registrar(ventas, origen, out consumos),
                ok => ok,
                "Insumo", "Venta", "ConsumoVenta");

            if (!registrado)
            {
                List<string> inactivos = new List<string>();
                foreach (BEConsumoVenta consumo in consumos)
                {
                    inactivos.Add(consumo.NombreInsumo);
                }
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "Hay recetas que llevan insumos dados de baja (" + string.Join(", ", inactivos.ToArray()) +
                    "). Revíselas desde Modificación de receta. No se registró nada."));
            }

            RegistrarEnBitacora(ventas, origen, titulo, consumos, conUsuario);

            // Patrón Observer: avisa si algún insumo cruzó su stock mínimo.
            foreach (BEConsumoVenta consumo in consumos)
            {
                BEInsumo actualizado = dalInsumo.ObtenerPorId(consumo.IdInsumo);
                if (actualizado != null)
                {
                    consumo.StockMinimo = actualizado.StockMinimo;
                }
                controladorStock.RegistrarDescuento(actualizado, consumo.StockAnterior);
            }

            return consumos;
        }

        private void RegistrarEnBitacora(List<BEVenta> ventas, string origen, string titulo,
                                         List<BEConsumoVenta> consumos, bool conUsuario)
        {
            decimal unidades = 0;
            DateTime desde = DateTime.MaxValue, hasta = DateTime.MinValue;
            foreach (BEVenta venta in ventas)
            {
                unidades += venta.Cantidad;
                if (venta.Fecha < desde) desde = venta.Fecha;
                if (venta.Fecha > hasta) hasta = venta.Fecha;
            }

            StringBuilder texto = new StringBuilder();
            if (ventas.Count == 1 && titulo == null)
            {
                texto.Append("Venta de ").Append(ventas[0].Cantidad.ToString("0.##")).Append(" x ")
                     .Append(ventas[0].NombreReceta).Append(" del ").Append(desde.ToString("dd/MM/yyyy"));
            }
            else
            {
                texto.Append(titulo ?? "Registro de ventas").Append(": ")
                     .Append(ventas.Count).Append(ventas.Count == 1 ? " venta" : " ventas")
                     .Append(" (").Append(unidades.ToString("0.##")).Append(" unidades)");
                if (desde != hasta)
                {
                    texto.Append(" del ").Append(desde.ToString("dd/MM/yyyy")).Append(" al ").Append(hasta.ToString("dd/MM/yyyy"));
                }
                else if (titulo == null || !titulo.StartsWith("Cierre", StringComparison.Ordinal))
                {
                    texto.Append(" del ").Append(desde.ToString("dd/MM/yyyy"));
                }
            }
            texto.Append(" (").Append(origen.ToLower()).Append("). Se descontó el stock de ")
                 .Append(consumos.Count).Append(consumos.Count == 1 ? " insumo." : " insumos.");

            List<string> faltantes = new List<string>();
            foreach (BEConsumoVenta consumo in consumos)
            {
                if (consumo.SinStock)
                {
                    faltantes.Add(consumo.NombreInsumo + " (" + consumo.FaltanteTexto + ")");
                }
            }
            if (faltantes.Count > 0)
            {
                texto.Append(" El stock del sistema no alcanzaba y quedó en cero: ")
                     .Append(string.Join("; ", faltantes.ToArray()))
                     .Append(". Corregirlo con un ajuste de stock.");
            }

            string descripcion = Recortar(texto.ToString(), LargoMaximoDescripcion);
            CriticidadEvento criticidad = faltantes.Count > 0 ? CriticidadEvento.Advertencia : CriticidadEvento.Informativa;
            if (conUsuario)
            {
                bllEvento.RegistrarDelUsuarioActual(AccionVenta, descripcion, criticidad);
            }
            else
            {
                bllEvento.Registrar(null, AccionVenta, descripcion, criticidad);
            }
        }

        #endregion

        #region Archivo del punto de venta

        /// Lee el archivo de ventas que exporta el punto de venta (CSV, con
        /// punto y coma, coma o tabulación) con las columnas fecha, receta y
        /// cantidad. Si la primera línea tiene los nombres de las columnas, se
        /// respeta su orden (también se aceptan "plato", "producto" o
        /// "artículo" por receta y "unidades" por cantidad); si no, se toman en
        /// ese orden. La receta se busca por su nombre (sin importar mayúsculas
        /// ni acentos) o por su número. Las líneas de una misma receta y un
        /// mismo día se suman. No graba nada: devuelve las ventas válidas y las
        /// líneas con errores, para mostrarlas antes de confirmar.
        public ResultadoImportacionVentas LeerArchivo(byte[] contenido, string nombreArchivo)
        {
            if (contenido == null || contenido.Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002", "Elija el archivo de ventas."));
            }
            if (contenido.Length > BytesMaximosPorArchivo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El archivo no puede superar los " + (BytesMaximosPorArchivo / 1024 / 1024) + " MB."));
            }

            string[] lineas = Decodificar(contenido).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            ResultadoImportacionVentas resultado = new ResultadoImportacionVentas();
            resultado.NombreArchivo = nombreArchivo;
            Dictionary<string, BEReceta> indice = IndiceDeRecetas();
            Dictionary<string, BEVenta> agrupadas = new Dictionary<string, BEVenta>();

            char separador = '\0';
            int colFecha = 0, colReceta = 1, colCantidad = 2;
            bool primera = true;
            int leidas = 0;

            for (int i = 0; i < lineas.Length; i++)
            {
                string linea = lineas[i].Trim();
                if (linea.Length == 0)
                {
                    continue;
                }

                if (separador == '\0')
                {
                    separador = DetectarSeparador(linea);
                }
                List<string> campos = Partir(linea, separador);

                if (primera)
                {
                    primera = false;
                    if (EsEncabezado(campos, ref colFecha, ref colReceta, ref colCantidad))
                    {
                        continue;
                    }
                }

                leidas++;
                if (leidas > LineasMaximasPorArchivo)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                        "El archivo no puede tener más de " + LineasMaximasPorArchivo + " líneas de ventas."));
                }

                BEVenta venta = new BEVenta();
                venta.Linea = i + 1;
                venta.Error = LeerLinea(campos, colFecha, colReceta, colCantidad, indice, venta);
                if (venta.Error != null)
                {
                    resultado.ConError.Add(venta);
                    continue;
                }

                string clave = venta.IdReceta + "|" + venta.Fecha.ToString("yyyyMMdd");
                BEVenta existente;
                if (agrupadas.TryGetValue(clave, out existente))
                {
                    existente.Cantidad += venta.Cantidad;
                }
                else
                {
                    agrupadas[clave] = venta;
                    resultado.Validas.Add(venta);
                }
            }

            // Una receta y un día que, sumados, pasan el máximo de una venta.
            for (int i = resultado.Validas.Count - 1; i >= 0; i--)
            {
                if (resultado.Validas[i].Cantidad > CantidadMaxima)
                {
                    resultado.Validas[i].Error = "La cantidad del día supera las " + CantidadMaxima.ToString("0") + " unidades.";
                    resultado.ConError.Add(resultado.Validas[i]);
                    resultado.Validas.RemoveAt(i);
                }
            }

            resultado.Validas.Sort(delegate(BEVenta a, BEVenta b)
            {
                int c = a.Fecha.CompareTo(b.Fecha);
                return c != 0 ? c : string.Compare(a.NombreReceta, b.NombreReceta, StringComparison.CurrentCultureIgnoreCase);
            });
            resultado.ConError.Sort(delegate(BEVenta a, BEVenta b) { return a.Linea.CompareTo(b.Linea); });

            AvisarRepetidas(resultado);
            return resultado;
        }

        /// Avisa los días del archivo que ya tienen ventas registradas: si el
        /// archivo se importó antes (o se cargó el cierre de ese día), se
        /// contarían dos veces.
        private void AvisarRepetidas(ResultadoImportacionVentas resultado)
        {
            if (resultado.Validas.Count == 0)
            {
                return;
            }

            DateTime desde = resultado.Validas[0].Fecha, hasta = resultado.Validas[0].Fecha;
            foreach (BEVenta venta in resultado.Validas)
            {
                if (venta.Fecha < desde) desde = venta.Fecha;
                if (venta.Fecha > hasta) hasta = venta.Fecha;
            }

            SortedDictionary<DateTime, decimal> yaCargado = new SortedDictionary<DateTime, decimal>();
            foreach (BEVenta venta in dalVenta.TraerVentas(desde, hasta))
            {
                decimal previo;
                yaCargado.TryGetValue(venta.Fecha.Date, out previo);
                yaCargado[venta.Fecha.Date] = previo + venta.Cantidad;
            }

            List<string> dias = new List<string>();
            foreach (KeyValuePair<DateTime, decimal> dia in yaCargado)
            {
                foreach (BEVenta venta in resultado.Validas)
                {
                    if (venta.Fecha.Date == dia.Key)
                    {
                        dias.Add(dia.Key.ToString("dd/MM/yyyy") + " (" + dia.Value.ToString("0.##") + " unidades)");
                        break;
                    }
                }
            }
            if (dias.Count > 0)
            {
                resultado.Avisos.Add("Ya hay ventas registradas para " + (dias.Count == 1 ? "el día " : "los días ") +
                                     string.Join(", ", dias.ToArray()) + ". Si el archivo incluye esas mismas ventas, " +
                                     "se contarían dos veces.");
            }
        }

        private static string Decodificar(byte[] contenido)
        {
            // Los puntos de venta exportan en UTF-8 o en la codificación de
            // Windows (la de Excel en español): si no es UTF-8 válido, se lee así.
            try
            {
                return new UTF8Encoding(false, true).GetString(contenido).TrimStart('﻿');
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1252).GetString(contenido);
            }
        }

        private static char DetectarSeparador(string linea)
        {
            if (linea.IndexOf(';') >= 0) return ';';
            if (linea.IndexOf('\t') >= 0) return '\t';
            return ',';
        }

        /// Parte una línea CSV respetando los campos entre comillas.
        private static List<string> Partir(string linea, char separador)
        {
            List<string> campos = new List<string>();
            StringBuilder actual = new StringBuilder();
            bool entreComillas = false;
            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];
                if (c == '"')
                {
                    if (entreComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                    {
                        actual.Append('"');
                        i++;
                    }
                    else
                    {
                        entreComillas = !entreComillas;
                    }
                }
                else if (c == separador && !entreComillas)
                {
                    campos.Add(actual.ToString().Trim());
                    actual.Length = 0;
                }
                else
                {
                    actual.Append(c);
                }
            }
            campos.Add(actual.ToString().Trim());
            return campos;
        }

        private static bool EsEncabezado(List<string> campos, ref int colFecha, ref int colReceta, ref int colCantidad)
        {
            int fecha = -1, receta = -1, cantidad = -1;
            for (int i = 0; i < campos.Count; i++)
            {
                string nombre = Normalizar(campos[i]);
                if (nombre.StartsWith("fecha")) fecha = i;
                else if (nombre == "receta" || nombre == "plato" || nombre == "producto" || nombre == "articulo" ||
                         nombre == "item" || nombre == "descripcion") receta = i;
                else if (nombre == "cantidad" || nombre == "unidades" || nombre == "cant") cantidad = i;
            }
            if (fecha < 0 && receta < 0 && cantidad < 0)
            {
                return false;
            }
            if (fecha >= 0) colFecha = fecha;
            if (receta >= 0) colReceta = receta;
            if (cantidad >= 0) colCantidad = cantidad;
            return true;
        }

        /// Completa la venta con los datos de la línea. Devuelve el error, o null si la línea es válida.
        private string LeerLinea(List<string> campos, int colFecha, int colReceta, int colCantidad,
                                 Dictionary<string, BEReceta> indice, BEVenta venta)
        {
            int columnas = Math.Max(colFecha, Math.Max(colReceta, colCantidad)) + 1;
            if (campos.Count < columnas)
            {
                return "Faltan columnas: se esperan fecha, receta y cantidad.";
            }

            DateTime fecha;
            if (!LeerFecha(campos[colFecha], out fecha))
            {
                return "La fecha \"" + campos[colFecha] + "\" no es válida (por ejemplo, 17/11/2026).";
            }
            if (fecha.Date > DateTime.Today)
            {
                return "La fecha " + fecha.ToString("dd/MM/yyyy") + " es posterior a hoy.";
            }
            if (fecha.Date < DateTime.Today.AddDays(-DiasMaximosHaciaAtras))
            {
                return "La fecha " + fecha.ToString("dd/MM/yyyy") + " tiene más de un año.";
            }

            BEReceta receta = BuscarRecetaVendible(campos[colReceta], indice);
            if (receta == null)
            {
                return "No hay una receta activa con insumos que se llame \"" + campos[colReceta] + "\".";
            }

            decimal cantidad;
            if (!LeerCantidad(campos[colCantidad], out cantidad))
            {
                return "La cantidad \"" + campos[colCantidad] + "\" no es un número.";
            }
            if (cantidad <= 0)
            {
                return "La cantidad tiene que ser mayor que cero.";
            }
            if (decimal.Round(cantidad, 2) != cantidad)
            {
                return "La cantidad admite hasta dos decimales.";
            }

            venta.Fecha = fecha.Date;
            venta.IdReceta = receta.IdReceta;
            venta.NombreReceta = receta.Nombre;
            venta.Cantidad = cantidad;
            return null;
        }

        private static readonly string[] FormatosFecha =
        {
            "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yy", "d/M/yy", "yyyyMMdd"
        };

        private static bool LeerFecha(string texto, out DateTime fecha)
        {
            texto = (texto ?? string.Empty).Trim();
            // Algunos puntos de venta exportan fecha y hora: alcanza con la fecha.
            int espacio = texto.IndexOf(' ');
            if (espacio < 0) espacio = texto.IndexOf('T');
            if (espacio > 0)
            {
                texto = texto.Substring(0, espacio);
            }
            return DateTime.TryParseExact(texto, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
        }

        private static bool LeerCantidad(string texto, out decimal cantidad)
        {
            texto = (texto ?? string.Empty).Trim().Replace(" ", string.Empty);
            if (texto.IndexOf(',') >= 0 && texto.IndexOf('.') >= 0)
            {
                // 1.234,5: el punto es de miles.
                texto = texto.Replace(".", string.Empty);
            }
            texto = texto.Replace(',', '.');
            return decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out cantidad);
        }

        /// Recetas que se pueden vender, por nombre normalizado y por número.
        private Dictionary<string, BEReceta> IndiceDeRecetas()
        {
            Dictionary<string, BEReceta> indice = new Dictionary<string, BEReceta>();
            foreach (BEReceta receta in TraerRecetasVendibles())
            {
                indice["#" + receta.IdReceta.ToString(CultureInfo.InvariantCulture)] = receta;
                string nombre = Normalizar(receta.Nombre);
                if (!indice.ContainsKey(nombre))
                {
                    indice[nombre] = receta;
                }
            }
            return indice;
        }

        private static BEReceta BuscarRecetaVendible(string texto, Dictionary<string, BEReceta> indice)
        {
            texto = (texto ?? string.Empty).Trim();
            BEReceta receta;
            int numero;
            string sinNumeral = texto.TrimStart('#').Replace("N°", string.Empty).Trim();
            if (int.TryParse(sinNumeral, NumberStyles.None, CultureInfo.InvariantCulture, out numero) &&
                indice.TryGetValue("#" + numero.ToString(CultureInfo.InvariantCulture), out receta))
            {
                return receta;
            }
            return indice.TryGetValue(Normalizar(texto), out receta) ? receta : null;
        }

        /// Minúsculas, sin acentos y sin espacios de más, para comparar nombres.
        private static string Normalizar(string texto)
        {
            string descompuesto = (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder limpio = new StringBuilder();
            bool espacio = false;
            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    espacio = limpio.Length > 0;
                    continue;
                }
                if (espacio)
                {
                    limpio.Append(' ');
                    espacio = false;
                }
                limpio.Append(c);
            }
            return limpio.ToString();
        }

        #endregion

        #region Consultas

        /// Ventas con fecha de venta entre desde y hasta (inclusive).
        public List<BEVenta> TraerVentas(DateTime desde, DateTime hasta)
        {
            return dalVenta.TraerVentas(desde.Date, hasta.Date);
        }

        /// Período máximo de las estadísticas de ventas.
        public const int DiasMaximosEstadisticas = 366;

        /// Estadísticas de ventas entre desde y hasta (inclusive): unidades y
        /// operaciones, promedio diario, receta más vendida, día de más ventas
        /// y el detalle por receta. Las publica WebServiceVentas.
        public BEEstadisticaVentas Estadisticas(DateTime desde, DateTime hasta)
        {
            desde = desde.Date;
            hasta = hasta.Date;

            if (desde > hasta)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de inicio no puede ser posterior a la fecha de fin."));
            }
            if ((hasta - desde).TotalDays >= DiasMaximosEstadisticas)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El período no puede superar un año."));
            }

            BEEstadisticaVentas estadistica = new BEEstadisticaVentas();
            estadistica.Desde = desde;
            estadistica.Hasta = hasta;
            estadistica.Dias = (int)(hasta - desde).TotalDays + 1;

            Dictionary<int, BEVentaPorReceta> porReceta = new Dictionary<int, BEVentaPorReceta>();
            Dictionary<DateTime, decimal> porDia = new Dictionary<DateTime, decimal>();

            foreach (BEVenta venta in TraerVentas(desde, hasta))
            {
                BEVentaPorReceta fila;
                if (!porReceta.TryGetValue(venta.IdReceta, out fila))
                {
                    fila = new BEVentaPorReceta();
                    fila.IdReceta = venta.IdReceta;
                    porReceta[venta.IdReceta] = fila;
                }
                fila.Receta = venta.NombreReceta;
                fila.UnidadesVendidas += venta.Cantidad;
                fila.OperacionesDeVenta++;

                decimal delDia;
                porDia.TryGetValue(venta.Fecha.Date, out delDia);
                porDia[venta.Fecha.Date] = delDia + venta.Cantidad;

                estadistica.UnidadesVendidas += venta.Cantidad;
                estadistica.OperacionesDeVenta++;
            }

            estadistica.PromedioDiario = Math.Round(estadistica.UnidadesVendidas / estadistica.Dias, 2);

            foreach (BEVentaPorReceta fila in porReceta.Values)
            {
                fila.PromedioDiario = Math.Round(fila.UnidadesVendidas / estadistica.Dias, 2);
                fila.PorcentajeDelTotal = estadistica.UnidadesVendidas == 0
                    ? 0
                    : Math.Round(fila.UnidadesVendidas * 100 / estadistica.UnidadesVendidas, 1);
                estadistica.Recetas.Add(fila);
            }
            estadistica.Recetas.Sort(delegate(BEVentaPorReceta a, BEVentaPorReceta b)
            {
                return b.UnidadesVendidas.CompareTo(a.UnidadesVendidas);
            });
            estadistica.RecetaMasVendida = estadistica.Recetas.Count > 0 ? estadistica.Recetas[0].Receta : string.Empty;

            foreach (KeyValuePair<DateTime, decimal> dia in porDia)
            {
                if (!estadistica.DiaConMasVentas.HasValue || dia.Value > estadistica.UnidadesDelDiaConMasVentas)
                {
                    estadistica.DiaConMasVentas = dia.Key;
                    estadistica.UnidadesDelDiaConMasVentas = dia.Value;
                }
            }

            return estadistica;
        }

        #endregion

        #region Privados

        private BEReceta ObtenerRecetaVendible(int idReceta)
        {
            if (idReceta <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Elija la receta vendida."));
            }

            BEReceta receta = bllReceta.ObtenerPorId(idReceta);
            if (receta == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!receta.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La receta " + receta.Nombre + " está dada de baja."));
            }

            if (receta.Detalles.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR017",
                    "La receta " + receta.Nombre + " no tiene insumos cargados."));
            }

            return receta;
        }

        private static DateTime ValidarFecha(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Ingrese la fecha de la venta."));
            }

            DateTime fechaVenta = fecha.Value.Date;
            if (fechaVenta > DateTime.Today)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de la venta no puede ser posterior a hoy."));
            }
            if (fechaVenta < DateTime.Today.AddDays(-DiasMaximosHaciaAtras))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de la venta no puede tener más de un año."));
            }
            return fechaVenta;
        }

        private static void ValidarCantidad(decimal cantidad)
        {
            if (cantidad <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad vendida tiene que ser mayor que cero."));
            }

            if (cantidad > CantidadMaxima)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad vendida no puede superar " + CantidadMaxima.ToString("0") + " unidades."));
            }

            if (decimal.Round(cantidad, 2) != cantidad)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad vendida admite hasta dos decimales."));
            }
        }

        private static string Recortar(string texto, int largo)
        {
            return texto.Length <= largo ? texto : texto.Substring(0, largo - 3) + "...";
        }

        #endregion
    }

    /// Resultado de leer el archivo de ventas del punto de venta: las ventas
    /// que se pueden registrar (ya sumadas por receta y día), las líneas con
    /// errores y los avisos (por ejemplo, días que ya tienen ventas cargadas).
    [Serializable]
    public class ResultadoImportacionVentas
    {
        private readonly List<BEVenta> validas = new List<BEVenta>();
        private readonly List<BEVenta> conError = new List<BEVenta>();
        private readonly List<string> avisos = new List<string>();

        public string NombreArchivo { get; set; }

        public List<BEVenta> Validas
        {
            get { return validas; }
        }

        public List<BEVenta> ConError
        {
            get { return conError; }
        }

        public List<string> Avisos
        {
            get { return avisos; }
        }

        public decimal UnidadesValidas
        {
            get
            {
                decimal total = 0;
                foreach (BEVenta venta in validas)
                {
                    total += venta.Cantidad;
                }
                return total;
            }
        }
    }
}
