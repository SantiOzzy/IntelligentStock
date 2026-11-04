using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using BE;

namespace Servicios
{
    /// Estrategia PDF del reporte gerencial (patrón Strategy, ver ExportadorReporte):
    /// el formato es texto con instrucciones de dibujo; se escribe una tabla con la
    /// fuente Helvetica, que todos los lectores de PDF traen.
    public class FormatoReportePdf : IFormatoReporte
    {
        public string Nombre
        {
            get { return ExportadorReporte.FormatoPdf; }
        }

        public string Extension
        {
            get { return ".pdf"; }
        }

        public string TipoMime
        {
            get { return "application/pdf"; }
        }

        public byte[] Generar(BEReporte reporte)
        {
            return GenerarPdf(reporte);
        }

        // Hoja A4 apaisada, en puntos (1/72 de pulgada).
        private const float AnchoPagina = 842f;
        private const float AltoPagina = 595f;
        private const float Margen = 40f;
        private const float TamanioLetra = 9f;
        private const float AltoLinea = 12f;

        private static byte[] GenerarPdf(BEReporte reporte)
        {
            int columnas = Math.Max(1, reporte.Columnas.Count);
            float[] anchos = AnchosDeColumna(reporte, columnas);

            List<StringBuilder> paginas = new List<StringBuilder>();
            StringBuilder pagina = null;
            float y = 0;

            Action nuevaPagina = null;
            nuevaPagina = delegate
            {
                pagina = new StringBuilder();
                paginas.Add(pagina);
                y = AltoPagina - Margen;
                Texto(pagina, "F2", 13f, Margen, y, reporte.Titulo);
                y -= 18f;
                Texto(pagina, "F1", TamanioLetra, Margen, y, reporte.Subtitulo);
                y -= 20f;
                y = DibujarFila(pagina, reporte.Columnas.ToArray(), anchos, y, true);
            };

            nuevaPagina();
            foreach (string[] fila in reporte.Filas)
            {
                float alto = AltoFila(fila, anchos);
                if (y - alto < Margen + 20f)
                {
                    nuevaPagina();
                }
                y = DibujarFila(pagina, fila, anchos, y, false);
            }

            if (!string.IsNullOrEmpty(reporte.Pie))
            {
                if (y - 2 * AltoLinea < Margen)
                {
                    nuevaPagina();
                }
                y -= AltoLinea;
                foreach (string linea in Partir(reporte.Pie, AnchoPagina - 2 * Margen, TamanioLetra))
                {
                    Texto(pagina, "F1", TamanioLetra, Margen, y, linea);
                    y -= AltoLinea;
                }
            }

            // Número de página al pie de cada hoja.
            for (int i = 0; i < paginas.Count; i++)
            {
                Texto(paginas[i], "F1", 8f, AnchoPagina - Margen - 60f, Margen - 15f,
                      "Página " + (i + 1) + " de " + paginas.Count);
            }

            return ArmarDocumento(paginas);
        }

        private static float[] AnchosDeColumna(BEReporte reporte, int columnas)
        {
            float[] pesos = new float[columnas];
            for (int c = 0; c < columnas; c++)
            {
                int largo = c < reporte.Columnas.Count ? reporte.Columnas[c].Length : 5;
                foreach (string[] fila in reporte.Filas)
                {
                    if (c < fila.Length && fila[c] != null)
                    {
                        largo = Math.Max(largo, fila[c].Length);
                    }
                }
                pesos[c] = Math.Min(60, Math.Max(6, largo));
            }

            float total = 0;
            foreach (float peso in pesos)
            {
                total += peso;
            }

            float disponible = AnchoPagina - 2 * Margen;
            float[] anchos = new float[columnas];
            for (int c = 0; c < columnas; c++)
            {
                anchos[c] = disponible * pesos[c] / total;
            }
            return anchos;
        }

        private static float AltoFila(string[] fila, float[] anchos)
        {
            int lineas = 1;
            for (int c = 0; c < anchos.Length && c < fila.Length; c++)
            {
                lineas = Math.Max(lineas, Partir(fila[c], anchos[c] - 6f, TamanioLetra).Count);
            }
            return lineas * AltoLinea + 4f;
        }

        private static float DibujarFila(StringBuilder pagina, string[] fila, float[] anchos, float y, bool encabezado)
        {
            float alto = AltoFila(fila, anchos);
            float x = Margen;

            if (encabezado)
            {
                // Fondo gris claro para los encabezados.
                pagina.Append("0.9 g ").Append(N(Margen)).Append(' ').Append(N(y - alto + 3f)).Append(' ')
                      .Append(N(AnchoPagina - 2 * Margen)).Append(' ').Append(N(alto)).Append(" re f 0 g\n");
            }

            for (int c = 0; c < anchos.Length; c++)
            {
                string valor = c < fila.Length ? fila[c] : string.Empty;
                float yLinea = y - AltoLinea + 3f;
                foreach (string linea in Partir(valor, anchos[c] - 6f, TamanioLetra))
                {
                    Texto(pagina, encabezado ? "F2" : "F1", TamanioLetra, x + 3f, yLinea, linea);
                    yLinea -= AltoLinea;
                }
                x += anchos[c];
            }

            // Línea de separación debajo de la fila.
            float base_ = y - alto + 2f;
            pagina.Append("0.75 G 0.5 w ").Append(N(Margen)).Append(' ').Append(N(base_)).Append(" m ")
                  .Append(N(AnchoPagina - Margen)).Append(' ').Append(N(base_)).Append(" l S 0 G\n");

            return y - alto;
        }

        /// Parte un texto en renglones que entren en el ancho dado. El ancho de
        /// cada letra se estima (Helvetica mide en promedio media letra de alto).
        private static List<string> Partir(string texto, float ancho, float tamanio)
        {
            List<string> lineas = new List<string>();
            texto = (texto ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            int maximo = Math.Max(3, (int)(ancho / (tamanio * 0.52f)));

            string actual = string.Empty;
            foreach (string palabra in texto.Split(' '))
            {
                string resto = palabra;
                while (resto.Length > maximo)
                {
                    if (actual.Length > 0)
                    {
                        lineas.Add(actual);
                        actual = string.Empty;
                    }
                    lineas.Add(resto.Substring(0, maximo));
                    resto = resto.Substring(maximo);
                }

                if (actual.Length == 0)
                {
                    actual = resto;
                }
                else if (actual.Length + 1 + resto.Length <= maximo)
                {
                    actual += " " + resto;
                }
                else
                {
                    lineas.Add(actual);
                    actual = resto;
                }
            }

            if (actual.Length > 0 || lineas.Count == 0)
            {
                lineas.Add(actual);
            }
            return lineas;
        }

        private static void Texto(StringBuilder pagina, string fuente, float tamanio, float x, float y, string texto)
        {
            pagina.Append("BT /").Append(fuente).Append(' ').Append(N(tamanio)).Append(" Tf ")
                  .Append(N(x)).Append(' ').Append(N(y)).Append(" Td (").Append(EscaparPdf(texto)).Append(") Tj ET\n");
        }

        /// Los caracteres especiales de un texto PDF se escapan; los que no
        /// existen en la codificación de la fuente (WinAnsi) se reemplazan.
        private static string EscaparPdf(string texto)
        {
            StringBuilder salida = new StringBuilder();
            foreach (char c in texto ?? string.Empty)
            {
                if (c == '(' || c == ')' || c == '\\')
                {
                    salida.Append('\\').Append(c);
                }
                else if (c < 32)
                {
                    salida.Append(' ');
                }
                else
                {
                    salida.Append(c);
                }
            }
            return salida.ToString();
        }

        private static string N(float valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static byte[] ArmarDocumento(List<StringBuilder> paginas)
        {
            Encoding winAnsi = Encoding.GetEncoding(1252);
            MemoryStream salida = new MemoryStream();
            List<long> posiciones = new List<long>();

            Action<string> escribir = delegate(string texto)
            {
                byte[] bytes = winAnsi.GetBytes(texto);
                salida.Write(bytes, 0, bytes.Length);
            };

            // Objetos: 1 catálogo, 2 árbol de páginas, 3 y 4 fuentes, y por cada
            // página un objeto página y otro con su contenido.
            int cantidad = paginas.Count;
            escribir("%PDF-1.4\n");

            posiciones.Add(salida.Position);
            escribir("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n");

            StringBuilder hijos = new StringBuilder();
            for (int i = 0; i < cantidad; i++)
            {
                hijos.Append(5 + 2 * i).Append(" 0 R ");
            }
            posiciones.Add(salida.Position);
            escribir("2 0 obj << /Type /Pages /Kids [" + hijos + "] /Count " + cantidad + " >> endobj\n");

            posiciones.Add(salida.Position);
            escribir("3 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >> endobj\n");
            posiciones.Add(salida.Position);
            escribir("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >> endobj\n");

            for (int i = 0; i < cantidad; i++)
            {
                int numeroPagina = 5 + 2 * i;
                int numeroContenido = numeroPagina + 1;
                byte[] contenido = winAnsi.GetBytes(paginas[i].ToString());

                posiciones.Add(salida.Position);
                escribir(numeroPagina + " 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 " + N(AnchoPagina) + " " +
                         N(AltoPagina) + "] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " +
                         numeroContenido + " 0 R >> endobj\n");

                posiciones.Add(salida.Position);
                escribir(numeroContenido + " 0 obj << /Length " + contenido.Length + " >> stream\n");
                salida.Write(contenido, 0, contenido.Length);
                escribir("\nendstream endobj\n");
            }

            long inicioXref = salida.Position;
            escribir("xref\n0 " + (posiciones.Count + 1) + "\n0000000000 65535 f \n");
            foreach (long posicion in posiciones)
            {
                escribir(posicion.ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
            }
            escribir("trailer << /Size " + (posiciones.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + inicioXref + "\n%%EOF\n");

            return salida.ToArray();
        }
    }
}
