using System;
using System.Collections.Generic;

namespace BE
{
    /// Reporte gerencial ya armado (CU-006-038): un título, las columnas y las
    /// filas como texto. El formato del archivo (PDF, Excel o CSV) lo resuelve
    /// el exportador, sin saber de qué trata el reporte.
    public class BEReporte
    {
        private readonly List<string> columnas = new List<string>();
        private readonly List<string[]> filas = new List<string[]>();

        public string Titulo { get; set; }

        /// Período y fecha de emisión, por ejemplo.
        public string Subtitulo { get; set; }

        /// Línea de totales o aclaración al pie (opcional).
        public string Pie { get; set; }

        public List<string> Columnas
        {
            get { return columnas; }
        }

        public List<string[]> Filas
        {
            get { return filas; }
        }

        public void AgregarFila(params string[] valores)
        {
            filas.Add(valores);
        }
    }
}
