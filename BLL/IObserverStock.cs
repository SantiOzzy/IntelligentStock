using System;
using BE;

namespace BLL
{
    /// Patrón Observer - observador.
    ///
    /// Cualquier acción que haya que disparar cuando un insumo queda en su
    /// stock mínimo o por debajo (avisar por correo, dejarlo en la bitácora,
    /// etc.) implementa esta interfaz y se suscribe al ControladorStock. La
    /// lógica que descuenta el stock no necesita saber qué hace cada uno.
    public interface IObserverStock
    {
        void Actualizar(BEInsumo insumo);
    }
}
