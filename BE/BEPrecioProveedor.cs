using System;

namespace BE
{
    /// Último precio que facturó un proveedor por un insumo (CU-006-033): sale
    /// del renglón de la orden recibida más reciente de ese proveedor que lo
    /// incluyó. No es una tabla: se arma con OrdenCompra y DetalleOrdenCompra.
    [Serializable]
    public class BEPrecioProveedor
    {
        public int IdInsumo { get; set; }

        public string CuitProveedor { get; set; }

        public string NombreProveedor { get; set; }

        public decimal PrecioUnitario { get; set; }

        /// Fecha de la orden en la que se pagó ese precio.
        public DateTime Fecha { get; set; }

        /// El precio es de los últimos días (BLLOrdenCompra.DiasVigenciaPrecio):
        /// solo los recientes compiten por el mejor precio.
        public bool Reciente { get; set; }

        /// "Distribuidora Sur $ 1.250,00 (02/11/26)".
        public string Texto
        {
            get { return NombreProveedor + " " + PrecioUnitario.ToString("C") + " (" + Fecha.ToString("dd/MM/yy") + ")"; }
        }
    }
}
