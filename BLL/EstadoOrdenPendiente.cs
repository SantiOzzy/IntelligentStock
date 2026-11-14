using BE;

namespace BLL
{
    /// Estado State: orden pendiente. Es el estado con el que se genera toda
    /// orden (CU-006-033) y el único que admite cambios: la orden se puede
    /// recibir o anular.
    public class EstadoOrdenPendiente : EstadoOrdenCompra
    {
        public override string Nombre
        {
            get { return BEOrdenCompra.EstadoPendiente; }
        }

        public override string Recibir(BEOrdenCompra orden, string comprobante)
        {
            return Recibida.Nombre + BEOrdenCompra.Separador + comprobante;
        }

        public override string Anular(BEOrdenCompra orden, string motivo)
        {
            return Anulada.Nombre + BEOrdenCompra.Separador + motivo;
        }

        public override void AcumularEn(BEGastoProveedor gasto, decimal importe)
        {
            gasto.OrdenesPendientes++;
        }
    }
}
