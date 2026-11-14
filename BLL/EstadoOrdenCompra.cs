using System;
using BE;
using Servicios;

namespace BLL
{
    /// Patrón State aplicado a la orden de compra (CU-006-033 a CU-006-036).
    ///
    /// Una orden se genera Pendiente y desde ahí puede pasar a Recibida (cuando
    /// se registra la factura del proveedor) o a Anulada. Una orden recibida o
    /// anulada ya no cambia:
    ///
    ///     Pendiente --Recibir--> Recibida
    ///     Pendiente --Anular---> Anulada
    ///
    /// En lugar de preguntar en cada operación "¿la orden está pendiente?", cada
    /// estado es una clase que sabe qué se puede hacer con la orden y a qué
    /// estado pasa. BLLOrdenCompra (el contexto) obtiene el estado de la orden
    /// con De() y le delega la operación; si el estado no la permite, es la
    /// propia clase del estado la que la rechaza con el error #ERR021. Agregar
    /// un estado nuevo (por ejemplo, "Recibida parcialmente") es agregar una
    /// clase, sin tocar las demás.
    ///
    /// La orden sigue guardando su estado como texto en la columna estado de la
    /// tabla OrdenCompra: la base de datos no cambia.
    public abstract class EstadoOrdenCompra
    {
        // Los estados no guardan datos propios: alcanza una instancia de cada uno.
        public static readonly EstadoOrdenCompra Pendiente = new EstadoOrdenPendiente();
        public static readonly EstadoOrdenCompra Recibida = new EstadoOrdenRecibida();
        public static readonly EstadoOrdenCompra Anulada = new EstadoOrdenAnulada();

        /// Nombre del estado, tal como se guarda en la columna estado.
        public abstract string Nombre { get; }

        /// Estado en el que se encuentra la orden, según lo que tiene guardado.
        public static EstadoOrdenCompra De(BEOrdenCompra orden)
        {
            switch (orden.EstadoBase)
            {
                case BEOrdenCompra.EstadoPendiente:
                    return Pendiente;
                case BEOrdenCompra.EstadoRecibida:
                    return Recibida;
                default:
                    // Un valor desconocido se trata como una orden cerrada: no admite cambios.
                    return Anulada;
            }
        }

        /// CU-006-036 Registrar factura de proveedor. Devuelve el valor de la
        /// columna estado con el que la orden queda recibida.
        public virtual string Recibir(BEOrdenCompra orden, string comprobante)
        {
            throw NoPermitido(orden, "solo se reciben órdenes pendientes.");
        }

        /// CU-006-035 Anular orden de compra. Devuelve el valor de la columna
        /// estado con el que la orden queda anulada.
        public virtual string Anular(BEOrdenCompra orden, string motivo)
        {
            throw NoPermitido(orden, "solo se anulan órdenes pendientes.");
        }

        /// Suma la orden a las compras del proveedor (reporte gerencial y
        /// servicio web de gastos por proveedor).
        public abstract void AcumularEn(BEGastoProveedor gasto, decimal importe);

        protected ApplicationException NoPermitido(BEOrdenCompra orden, string regla)
        {
            return new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                "La orden " + orden.Numero + " está " + Nombre.ToLower() + ": " + regla));
        }
    }
}
