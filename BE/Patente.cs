using System;
using System.Collections.Generic;

namespace BE
{
    /// Hoja del Composite. Representa un permiso individual sobre una
    /// funcionalidad concreta del sistema (por ejemplo "AltaUsuario").
    /// El nombre de la patente coincide con el nombre del formulario .aspx
    /// que habilita.
    public class Patente : Componente
    {
        private static readonly IList<Componente> sinHijos =
            new List<Componente>().AsReadOnly();

        public Patente()
        {
        }

        public Patente(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public override char Tipo
        {
            get { return 'P'; }
        }

        public override IList<Componente> Hijos
        {
            get { return sinHijos; }
        }

        public override void Agregar(Componente componente)
        {
            throw new InvalidOperationException(
                "Una patente es un permiso individual y no admite componentes hijos.");
        }

        public override void Quitar(Componente componente)
        {
            throw new InvalidOperationException(
                "Una patente es un permiso individual y no admite componentes hijos.");
        }

        public override bool Contiene(string nombrePatente)
        {
            return string.Equals(Nombre, nombrePatente, StringComparison.OrdinalIgnoreCase);
        }

        public override void ListarPatentes(IList<Patente> destino)
        {
            if (!destino.Contains(this))
            {
                destino.Add(this);
            }
        }
    }
}
