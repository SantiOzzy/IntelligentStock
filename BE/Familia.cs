using System;
using System.Collections.Generic;

namespace BE
{
    /// Nodo compuesto del Composite. Una familia agrupa patentes y/u otras
    /// familias, de modo que asignarla a un rol equivale a asignar todos
    /// los permisos que contiene.
    public class Familia : Componente
    {
        private readonly List<Componente> hijos = new List<Componente>();

        public Familia()
        {
        }

        public Familia(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public override char Tipo
        {
            get { return 'F'; }
        }

        public override IList<Componente> Hijos
        {
            get { return hijos; }
        }

        public override void Agregar(Componente componente)
        {
            if (componente == null)
            {
                throw new ArgumentNullException("componente");
            }

            if (componente == this)
            {
                throw new InvalidOperationException(
                    "Una familia no puede contenerse a sí misma.");
            }

            // Evita ciclos en el arbol de permisos.
            Familia familiaHija = componente as Familia;
            if (familiaHija != null && familiaHija.ContieneFamilia(this))
            {
                throw new InvalidOperationException(
                    "No se puede agregar la familia porque generaría una referencia circular.");
            }

            if (!hijos.Contains(componente))
            {
                hijos.Add(componente);
            }
        }

        public override void Quitar(Componente componente)
        {
            hijos.Remove(componente);
        }

        public override bool Contiene(string nombrePatente)
        {
            foreach (Componente hijo in hijos)
            {
                if (hijo.Contiene(nombrePatente))
                {
                    return true;
                }
            }
            return false;
        }

        public override void ListarPatentes(IList<Patente> destino)
        {
            foreach (Componente hijo in hijos)
            {
                hijo.ListarPatentes(destino);
            }
        }

        /// Indica si la familia buscada forma parte del subarbol.
        public bool ContieneFamilia(Familia buscada)
        {
            foreach (Componente hijo in hijos)
            {
                Familia familia = hijo as Familia;
                if (familia == null)
                {
                    continue;
                }
                if (familia.Id == buscada.Id || familia.ContieneFamilia(buscada))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
