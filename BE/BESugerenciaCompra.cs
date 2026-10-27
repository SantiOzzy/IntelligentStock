using System;

namespace BE
{
    /// Cantidad a pedir de un insumo según la predicción de demanda (CU-006-033).
    ///
    /// Sugerido = consumo previsto para los próximos días + stock mínimo
    ///            - stock actual - lo que ya está pedido en órdenes pendientes.
    [Serializable]
    public class BESugerenciaCompra
    {
        public int IdInsumo { get; set; }

        public string NombreInsumo { get; set; }

        public string UnidadDeMedida { get; set; }

        public decimal StockActual { get; set; }

        public decimal StockMinimo { get; set; }

        /// Cantidad que ya está pedida en órdenes pendientes.
        public decimal CantidadPendiente { get; set; }

        /// Consumo que predice el modelo para el período proyectado.
        public decimal ConsumoPrevisto { get; set; }

        public decimal CantidadSugerida { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal ImporteEstimado
        {
            get { return CantidadSugerida * PrecioUnitario; }
        }

        public string StockActualTexto
        {
            get { return StockActual.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string StockMinimoTexto
        {
            get { return StockMinimo.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string CantidadPendienteTexto
        {
            get { return CantidadPendiente.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string ConsumoPrevistoTexto
        {
            get { return ConsumoPrevisto.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string CantidadSugeridaTexto
        {
            get { return CantidadSugerida.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string ImporteEstimadoTexto
        {
            get { return ImporteEstimado.ToString("C"); }
        }
    }
}
