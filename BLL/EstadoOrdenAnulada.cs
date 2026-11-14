using BE;

namespace BLL
{
    /// Estado State: orden anulada. No admite más cambios: Recibir y Anular
    /// quedan rechazados por la clase base.
    public class EstadoOrdenAnulada : EstadoOrdenCompra
    {
        public override string Nombre
        {
            get { return BEOrdenCompra.EstadoAnulada; }
        }

        public override void AcumularEn(BEGastoProveedor gasto, decimal importe)
        {
            gasto.OrdenesAnuladas++;
        }
    }
}
