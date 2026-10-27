using System;
using BE;

namespace BLL
{
    /// Patrón Observer - observador concreto.
    /// Ante un quiebre de stock deja el incidente registrado en la bitácora con
    /// criticidad Advertencia, para poder filtrarlo después.
    public class BitacoraAlertaStock : IObserverStock
    {
        public const string Accion = "Quiebre de stock";

        private readonly BLLEvento bllEvento = new BLLEvento();

        public void Actualizar(BEInsumo insumo)
        {
            bllEvento.RegistrarDelUsuarioActual(Accion,
                "Quiebre de stock detectado para el insumo " + insumo.Nombre +
                ". Stock actual: " + insumo.StockTexto +
                " (mínimo: " + insumo.StockMinimoTexto + ").",
                CriticidadEvento.Advertencia);
        }
    }
}
