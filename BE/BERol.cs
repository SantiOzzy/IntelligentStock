using System;
using System.Collections.Generic;

namespace BE
{
    /// Perfil del sistema (WebMaster, Encargado, Auditor, Tecnico, Empleado).
    /// El rol es el cliente del Composite: guarda una lista de componentes
    /// que pueden ser patentes sueltas o familias completas.
    public class BERol
    {
        private readonly List<Componente> permisos = new List<Componente>();

        public BERol()
        {
        }

        public BERol(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public int Id { get; set; }

        public string Nombre { get; set; }

        public IList<Componente> Permisos
        {
            get { return permisos; }
        }

        public void Agregar(Componente componente)
        {
            if (componente != null && !permisos.Contains(componente))
            {
                permisos.Add(componente);
            }
        }

        public void Quitar(Componente componente)
        {
            permisos.Remove(componente);
        }

        /// Recorre el arbol de permisos preguntandole a cada componente,
        /// sin distinguir si es una patente o una familia.
        public bool TienePermiso(string nombrePatente)
        {
            if (string.IsNullOrEmpty(nombrePatente))
            {
                return false;
            }

            foreach (Componente componente in permisos)
            {
                if (componente.Contiene(nombrePatente))
                {
                    return true;
                }
            }
            return false;
        }

        /// Devuelve la lista plana de patentes efectivas del rol.
        public IList<Patente> ObtenerPatentes()
        {
            List<Patente> patentes = new List<Patente>();
            foreach (Componente componente in permisos)
            {
                componente.ListarPatentes(patentes);
            }
            return patentes;
        }

        public override string ToString()
        {
            return Nombre;
        }
    }
}
