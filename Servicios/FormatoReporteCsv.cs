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
    /// Estrategia CSV del reporte gerencial (patrón Strategy, ver ExportadorReporte):
    /// texto separado por punto y coma, que es lo que espera Excel en español.
    public class FormatoReporteCsv : IFormatoReporte
    {
        public string Nombre
        {
            get { return ExportadorReporte.FormatoCsv; }
        }

        public string Extension
        {
            get { return ".csv"; }
        }

        public string TipoMime
        {
            get { return "text/csv"; }
        }

        public byte[] Generar(BEReporte reporte)
        {
            return GenerarCsv(reporte);
        }

        private static byte[] GenerarCsv(BEReporte reporte)
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine(LineaCsv(reporte.Columnas.ToArray()));
            foreach (string[] fila in reporte.Filas)
            {
                csv.AppendLine(LineaCsv(fila));
            }

            // Con BOM para que Excel reconozca los acentos.
            byte[] bom = Encoding.UTF8.GetPreamble();
            byte[] texto = Encoding.UTF8.GetBytes(csv.ToString());
            byte[] resultado = new byte[bom.Length + texto.Length];
            Buffer.BlockCopy(bom, 0, resultado, 0, bom.Length);
            Buffer.BlockCopy(texto, 0, resultado, bom.Length, texto.Length);
            return resultado;
        }

        private static string LineaCsv(string[] valores)
        {
            List<string> celdas = new List<string>();
            foreach (string valor in valores)
            {
                string v = valor ?? string.Empty;
                if (v.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
                {
                    v = "\"" + v.Replace("\"", "\"\"") + "\"";
                }
                celdas.Add(v);
            }
            return string.Join(";", celdas.ToArray());
        }
    }
}
