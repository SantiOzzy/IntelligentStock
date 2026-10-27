using System;
using System.Collections.Generic;

namespace BE
{
    /// Receta (plato o producto que se vende). Se corresponde con la tabla
    /// Receta, y su lista de insumos con la tabla DetalleReceta.
    [Serializable]
    public class BEReceta
    {
        private List<BEDetalleReceta> detalles = new List<BEDetalleReceta>();

        public BEReceta()
        {
        }

        public BEReceta(int idReceta, string nombre)
        {
            IdReceta = idReceta;
            Nombre = nombre;
        }

        public int IdReceta { get; set; }

        public string Nombre { get; set; }

        public bool Activo { get; set; }

        public List<BEDetalleReceta> Detalles
        {
            get { return detalles; }
            set { detalles = value ?? new List<BEDetalleReceta>(); }
        }

        /// Cantidad de insumos distintos que lleva. En los listados viene
        /// calculada desde la base, sin traer el detalle completo.
        public int CantidadInsumos { get; set; }

        public string Estado
        {
            get { return Activo ? "Activa" : "Inactiva"; }
        }
    }
}
