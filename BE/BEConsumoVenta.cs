using System;

namespace BE
{
    /// Consumo de un insumo en una venta (CU-006-037): lo que lleva la receta por
    /// unidad, el total según la cantidad vendida y cómo queda el stock.
    [Serializable]
    public class BEConsumoVenta
    {
        public int IdInsumo { get; set; }

        public string NombreInsumo { get; set; }

        public string UnidadDeMedida { get; set; }

        public decimal ConsumoUnitario { get; set; }

        public decimal ConsumoTotal { get; set; }

        public decimal StockAnterior { get; set; }

        public decimal StockResultante { get; set; }

        /// El stock no alcanza para la venta.
        public bool SinStock
        {
            get { return StockResultante < 0; }
        }

        public string ConsumoUnitarioTexto
        {
            get { return ConsumoUnitario.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string ConsumoTotalTexto
        {
            get { return ConsumoTotal.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string StockAnteriorTexto
        {
            get { return StockAnterior.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string StockResultanteTexto
        {
            get { return StockResultante.ToString("0.##") + " " + UnidadDeMedida; }
        }
    }
}
