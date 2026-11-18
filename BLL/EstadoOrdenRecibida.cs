using BE;

namespace BLL
{
    /// Estado State: orden recibida (se registró la factura del proveedor y lo
    /// que llegó ya ingresó al stock; el detalle de la orden quedó con las
    /// cantidades recibidas). No admite más cambios: Recibir y Anular quedan
    /// rechazados por la clase base.
    public class EstadoOrdenRecibida : EstadoOrdenCompra
    {
        public override string Nombre
        {
            get { return BEOrdenCompra.EstadoRecibida; }
        }

        /// Solo lo recibido cuenta como compra en el importe del proveedor.
        public override void AcumularEn(BEGastoProveedor gasto, decimal importe)
        {
            gasto.OrdenesRecibidas++;
            gasto.ImporteRecibido += importe;
        }
    }
}
