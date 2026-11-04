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
    /// Genera el archivo del reporte gerencial (CU-006-038) en PDF, Excel o CSV.
    ///
    /// Patrón Strategy: cada formato es una estrategia que implementa
    /// IFormatoReporte (FormatoReportePdf, FormatoReporteExcel y
    /// FormatoReporteCsv) y sabe armar su archivo. Esta clase es el contexto:
    /// elige la estrategia por el nombre del formato y le pide el archivo, sin
    /// saber cómo se arma cada uno. Para agregar un formato alcanza con
    /// escribir una clase más y sumarla a la lista de estrategias.
    ///
    /// Los archivos se arman "a mano", sin bibliotecas externas (ver cada
    /// estrategia).
    public static class ExportadorReporte
    {
        public const string FormatoPdf = "PDF";
        public const string FormatoExcel = "Excel";
        public const string FormatoCsv = "CSV";

        /// Las estrategias disponibles. La primera es la que se usa si el
        /// formato pedido no existe.
        private static readonly IFormatoReporte[] estrategias =
        {
            new FormatoReportePdf(), new FormatoReporteExcel(), new FormatoReporteCsv()
        };

        public static readonly string[] Formatos = { FormatoPdf, FormatoExcel, FormatoCsv };

        /// La estrategia del formato indicado (PDF si no existe).
        public static IFormatoReporte ObtenerFormato(string formato)
        {
            foreach (IFormatoReporte estrategia in estrategias)
            {
                if (estrategia.Nombre == formato)
                {
                    return estrategia;
                }
            }
            return estrategias[0];
        }

        /// Devuelve el contenido del archivo, su extensión y su tipo MIME.
        public static byte[] Exportar(BEReporte reporte, string formato, out string extension, out string tipoMime)
        {
            if (reporte == null)
            {
                throw new ArgumentNullException("reporte");
            }

            IFormatoReporte estrategia = ObtenerFormato(formato);
            extension = estrategia.Extension;
            tipoMime = estrategia.TipoMime;
            return estrategia.Generar(reporte);
        }
    }
}
