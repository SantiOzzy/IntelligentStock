using System;

namespace BE
{
    /// Renglón de una orden de compra. Se corresponde con la tabla DetalleOrdenCompra.
    [Serializable]
    public class BEDetalleOrdenCompra
    {
        public int IdOrdenCompra { get; set; }

        public int IdInsumo { get; set; }

        public string NombreInsumo { get; set; }

        public string UnidadDeMedida { get; set; }

        public decimal Cantidad { get; set; }

        /// Precio unitario actual del insumo (para valorizar la orden).
        public decimal PrecioUnitario { get; set; }

        public decimal Importe
        {
            get { return Cantidad * PrecioUnitario; }
        }

        public string CantidadTexto
        {
            get { return Cantidad.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string ImporteTexto
        {
            get { return Importe.ToString("C"); }
        }
    }
}
