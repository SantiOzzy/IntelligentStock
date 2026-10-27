using System;

namespace BE
{
    /// Dígitos verificadores calculados para una tabla completa.
    public class ObjetoDV
    {
        public ObjetoDV()
        {
        }

        public ObjetoDV(string tabla, string dvh, string dvv)
        {
            Tabla = tabla;
            DVH = dvh;
            DVV = dvv;
        }

        public string Tabla { get; set; }

        /// Dígito verificador horizontal de la tabla (recorrido fila por fila).
        public string DVH { get; set; }

        /// Dígito verificador vertical de la tabla (recorrido columna por columna).
        public string DVV { get; set; }
    }
}
