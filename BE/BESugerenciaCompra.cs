using System;

namespace BE
{
    /// Cantidad a pedir de un insumo según la predicción de demanda (CU-006-033).
    ///
    /// Sugerido = consumo previsto para los próximos días + stock mínimo
    ///            - stock actual - lo que ya está pedido en órdenes pendientes.
    ///
    /// Un perecedero no se pide para más días de los que dura (su vida útil):
    /// lo que sobrara se tiraría.
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

        /// Lo que el usuario decide pedir (empieza siendo lo sugerido).
        public decimal CantidadAPedir { get; set; }

        /// Días de consumo que cubre la sugerencia: los proyectados, o menos si
        /// la vida útil del insumo es más corta.
        public int DiasCubiertos { get; set; }

        public bool LimitadoPorVidaUtil { get; set; }

        /// Proveedor al que se le pidió este insumo la última vez (vacío si
        /// nunca se pidió).
        public string CuitProveedorHabitual { get; set; }

        public string ProveedorHabitual { get; set; }

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

        public string DiasCubiertosTexto
        {
            get
            {
                return DiasCubiertos + (DiasCubiertos == 1 ? " día" : " días") +
                       (LimitadoPorVidaUtil ? " (vida útil)" : string.Empty);
            }
        }

        public string ProveedorHabitualTexto
        {
            get { return string.IsNullOrEmpty(ProveedorHabitual) ? "-" : ProveedorHabitual; }
        }
    }
}
