using System;
using System.Collections.Generic;
using BE;

namespace BLL
{
    /// Patrón Observer - sujeto.
    ///
    /// Lo usan el ajuste de stock (CU-005-032) y el descuento por venta
    /// (CU-006-037): después de restar stock le avisan al controlador, y si el
    /// insumo quedó en su mínimo o por debajo, el controlador notifica a todos
    /// los observadores suscriptos. Para agregar una acción nueva ante un
    /// quiebre de stock alcanza con crear otro observador y suscribirlo, sin
    /// tocar el código que descuenta el stock.
    public class ControladorStock
    {
        private readonly List<IObserverStock> observadores = new List<IObserverStock>();

        public void Suscribir(IObserverStock observador)
        {
            if (observador != null && !observadores.Contains(observador))
            {
                observadores.Add(observador);
            }
        }

        public void Desuscribir(IObserverStock observador)
        {
            observadores.Remove(observador);
        }

        /// Se llama después de descontar stock de un insumo. Avisa solo cuando
        /// el descuento hace que el insumo caiga a su stock mínimo (o por debajo):
        /// si ya estaba en el mínimo antes, el aviso ya se dio y no se repite en
        /// cada descuento siguiente.
        public void RegistrarDescuento(BEInsumo insumo, decimal stockAnterior)
        {
            if (insumo != null && insumo.BajoMinimo && stockAnterior > insumo.StockMinimo)
            {
                NotificarQuiebreStock(insumo);
            }
        }

        public void NotificarQuiebreStock(BEInsumo insumo)
        {
            foreach (IObserverStock observador in observadores)
            {
                try
                {
                    observador.Actualizar(insumo);
                }
                catch (Exception)
                {
                    // El descuento de stock ya quedó grabado: que falle una
                    // notificación (por ejemplo, el correo) no lo tiene que
                    // revertir ni impedir que se ejecuten los demás observadores.
                }
            }
        }
    }
}
