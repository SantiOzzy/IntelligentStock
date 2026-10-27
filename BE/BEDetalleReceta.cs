using System;

namespace BE
{
    /// Renglón de una receta: qué insumo lleva y en qué cantidad.
    /// Se corresponde con la tabla DetalleReceta.
    [Serializable]
    public class BEDetalleReceta
    {
        public BEDetalleReceta()
        {
        }

        public BEDetalleReceta(int idInsumo, string nombreInsumo, string unidadDeMedida, decimal cantidad)
        {
            IdInsumo = idInsumo;
            NombreInsumo = nombreInsumo;
            UnidadDeMedida = unidadDeMedida;
            Cantidad = cantidad;
        }

        public int IdInsumo { get; set; }

        public string NombreInsumo { get; set; }

        public string UnidadDeMedida { get; set; }

        /// Cantidad del insumo que consume una unidad de la receta.
        public decimal Cantidad { get; set; }

        public string CantidadTexto
        {
            get { return Cantidad.ToString("0.##"); }
        }
    }
}
