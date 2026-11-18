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

        /// Stock mínimo del insumo (para avisar si la venta lo deja en el mínimo).
        public decimal StockMinimo { get; set; }

        /// Lo que faltó: la parte del consumo que el stock del sistema no cubría.
        /// La venta igual se registra (ya ocurrió) y el insumo queda en cero.
        public decimal Faltante
        {
            get { return ConsumoTotal > StockAnterior ? ConsumoTotal - StockAnterior : 0m; }
        }

        /// El stock del sistema no alcanzaba para la venta.
        public bool SinStock
        {
            get { return Faltante > 0; }
        }

        public string FaltanteTexto
        {
            get { return Faltante > 0 ? Faltante.ToString("0.##") + " " + UnidadDeMedida : "-"; }
        }

        /// Alcanza, pero el insumo queda en su stock mínimo o por debajo.
        public bool QuedaEnMinimo
        {
            get { return !SinStock && StockResultante <= StockMinimo; }
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
