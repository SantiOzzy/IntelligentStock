using System;

namespace BE
{
    /// Compras a un proveedor en un período (WebServiceGastoProveedores y
    /// reporte gerencial "Órdenes por proveedor").
    [Serializable]
    public class BEGastoProveedor
    {
        public string Cuit { get; set; }

        public string Proveedor { get; set; }

        public int OrdenesPendientes { get; set; }

        public int OrdenesRecibidas { get; set; }

        public int OrdenesAnuladas { get; set; }

        /// Lo recibido, valorizado al precio unitario actual de cada insumo.
        public decimal ImporteRecibido { get; set; }
    }
}
