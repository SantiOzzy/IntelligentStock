using System;
using System.Collections.Generic;

namespace BE
{
    /// Orden de compra a un proveedor. Se corresponde con la tabla OrdenCompra.
    ///
    /// El estado empieza con Pendiente, Recibida o Anulada. Como indican las
    /// interfaces de los CU-006-035 y CU-006-036, en el mismo campo se guarda el
    /// motivo de la anulación ("Anulada: ...") o el comprobante con el que se
    /// recibió ("Recibida: ...").
    [Serializable]
    public class BEOrdenCompra
    {
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoRecibida = "Recibida";
        public const string EstadoAnulada = "Anulada";

        /// Separador entre el estado y su detalle (comprobante o motivo).
        public const string Separador = ": ";

        private readonly List<BEDetalleOrdenCompra> detalles = new List<BEDetalleOrdenCompra>();

        public int IdOrdenCompra { get; set; }

        public string CuitProveedor { get; set; }

        public string NombreProveedor { get; set; }

        public DateTime Fecha { get; set; }

        /// Valor completo de la columna estado.
        public string Estado { get; set; }

        public int CantidadItems { get; set; }

        public List<BEDetalleOrdenCompra> Detalles
        {
            get { return detalles; }
        }

        /// Pendiente, Recibida o Anulada, sin el detalle.
        public string EstadoBase
        {
            get
            {
                string estado = Estado ?? string.Empty;
                int corte = estado.IndexOf(Separador, StringComparison.Ordinal);
                return corte < 0 ? estado.Trim() : estado.Substring(0, corte).Trim();
            }
        }

        /// Comprobante (si se recibió) o motivo (si se anuló).
        public string DetalleEstado
        {
            get
            {
                string estado = Estado ?? string.Empty;
                int corte = estado.IndexOf(Separador, StringComparison.Ordinal);
                return corte < 0 ? string.Empty : estado.Substring(corte + Separador.Length).Trim();
            }
        }

        public bool Pendiente
        {
            get { return EstadoBase == EstadoPendiente; }
        }

        public string Numero
        {
            get { return "N° " + IdOrdenCompra; }
        }

        public string FechaTexto
        {
            get { return Fecha.ToString("dd/MM/yyyy"); }
        }

        public string ProveedorTexto
        {
            get { return string.IsNullOrEmpty(NombreProveedor) ? CuitProveedor : NombreProveedor + " (" + CuitProveedor + ")"; }
        }

        /// Importe estimado: cantidad pedida por el precio unitario actual de cada insumo.
        public decimal ImporteEstimado
        {
            get
            {
                decimal total = 0;
                foreach (BEDetalleOrdenCompra detalle in detalles)
                {
                    total += detalle.Importe;
                }
                return total;
            }
        }

        /// Texto para mostrar la orden en una lista desplegable.
        public string Etiqueta
        {
            get { return Numero + " - " + FechaTexto + " - " + ProveedorTexto; }
        }
    }
}
