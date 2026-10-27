using System;
using System.Collections.Generic;

namespace BE
{
    /// Componente del patron Composite de permisos.
    /// Es la clase abstracta que comparten las hojas (Patente) y los
    /// nodos compuestos (Familia), de manera que un Rol pueda tratar
    /// de la misma forma a un permiso individual y a un conjunto de permisos.
    public abstract class Componente
    {
        public int Id { get; set; }

        public string Nombre { get; set; }

        /// 'P' para las patentes (hojas), 'F' para las familias (compuestos).
        public abstract char Tipo { get; }

        public abstract void Agregar(Componente componente);

        public abstract void Quitar(Componente componente);

        public abstract IList<Componente> Hijos { get; }

        /// Indica si el componente contiene (directa o indirectamente) la patente indicada.
        public abstract bool Contiene(string nombrePatente);

        /// Vuelca en la lista destino todas las patentes que cuelgan del componente.
        public abstract void ListarPatentes(IList<Patente> destino);

        public override string ToString()
        {
            return Nombre;
        }
    }
}
