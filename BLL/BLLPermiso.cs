using System;
using System.Collections.Generic;
using System.Data;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Arma en memoria el arbol del patron Composite a partir de las tablas
    /// Familia, RelacionFamilia y RolFamilia, y expone el ABM de familias,
    /// patentes y asignaciones a roles.
    public class BLLPermiso
    {
        private readonly DALPermiso dalPermiso = new DALPermiso();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        #region Construccion del arbol

        /// Devuelve todos los componentes indexados por id, con la jerarquia
        /// padre - hijo ya resuelta.
        public Dictionary<int, Componente> ConstruirArbol()
        {
            Dictionary<int, Componente> componentes = new Dictionary<int, Componente>();

            DataTable tabla = dalPermiso.TraerComponentes();
            foreach (DataRow fila in tabla.Rows)
            {
                int id = Convert.ToInt32(fila["id_familia"]);
                string nombre = Convert.ToString(fila["nombre"]);
                string tipo = Convert.ToString(fila["tipo"]);

                Componente componente = string.Equals(tipo, "F", StringComparison.OrdinalIgnoreCase)
                    ? (Componente)new Familia(id, nombre)
                    : new Patente(id, nombre);

                componentes[id] = componente;
            }

            DataTable relaciones = dalPermiso.TraerRelaciones();
            foreach (DataRow fila in relaciones.Rows)
            {
                int idPadre = Convert.ToInt32(fila["id_familiaPadre"]);
                int idHijo = Convert.ToInt32(fila["id_familiaHija"]);

                Componente padre;
                Componente hijo;
                if (componentes.TryGetValue(idPadre, out padre) &&
                    componentes.TryGetValue(idHijo, out hijo) &&
                    padre is Familia)
                {
                    try
                    {
                        padre.Agregar(hijo);
                    }
                    catch (InvalidOperationException)
                    {
                        // Relacion circular guardada en la base: se ignora.
                    }
                }
            }

            return componentes;
        }

        /// Componentes que no dependen de ninguna familia padre.
        public List<Componente> TraerRaices()
        {
            Dictionary<int, Componente> componentes = ConstruirArbol();
            HashSet<int> hijos = new HashSet<int>();

            DataTable relaciones = dalPermiso.TraerRelaciones();
            foreach (DataRow fila in relaciones.Rows)
            {
                hijos.Add(Convert.ToInt32(fila["id_familiaHija"]));
            }

            List<Componente> raices = new List<Componente>();
            foreach (KeyValuePair<int, Componente> par in componentes)
            {
                if (!hijos.Contains(par.Key))
                {
                    raices.Add(par.Value);
                }
            }

            raices.Sort(CompararPorTipoYNombre);
            return raices;
        }

        public List<Componente> TraerTodos()
        {
            Dictionary<int, Componente> componentes = ConstruirArbol();
            List<Componente> lista = new List<Componente>(componentes.Values);
            lista.Sort(CompararPorTipoYNombre);
            return lista;
        }

        public List<Familia> TraerFamilias()
        {
            List<Familia> familias = new List<Familia>();
            foreach (Componente componente in TraerTodos())
            {
                Familia familia = componente as Familia;
                if (familia != null)
                {
                    familias.Add(familia);
                }
            }
            return familias;
        }

        public List<Patente> TraerPatentes()
        {
            List<Patente> patentes = new List<Patente>();
            foreach (Componente componente in TraerTodos())
            {
                Patente patente = componente as Patente;
                if (patente != null)
                {
                    patentes.Add(patente);
                }
            }
            return patentes;
        }

        public Componente ObtenerComponente(int id)
        {
            Dictionary<int, Componente> componentes = ConstruirArbol();
            Componente componente;
            return componentes.TryGetValue(id, out componente) ? componente : null;
        }

        #endregion

        #region Roles

        public List<BERol> TraerRoles()
        {
            List<BERol> roles = dalPermiso.TraerRoles();
            Dictionary<int, Componente> componentes = ConstruirArbol();
            DataTable asignaciones = dalPermiso.TraerAsignacionesDeRoles();

            foreach (BERol rol in roles)
            {
                CargarPermisos(rol, componentes, asignaciones);
            }

            return roles;
        }

        /// Carga en el rol los componentes asignados en la tabla RolFamilia.
        public void CargarPermisos(BERol rol)
        {
            CargarPermisos(rol, ConstruirArbol(), dalPermiso.TraerAsignacionesDeRoles());
        }

        private static void CargarPermisos(BERol rol, Dictionary<int, Componente> componentes, DataTable asignaciones)
        {
            if (rol == null)
            {
                return;
            }

            rol.Permisos.Clear();

            foreach (DataRow fila in asignaciones.Rows)
            {
                if (Convert.ToInt32(fila["id_rol"]) != rol.Id)
                {
                    continue;
                }

                Componente componente;
                if (componentes.TryGetValue(Convert.ToInt32(fila["id_familia"]), out componente))
                {
                    rol.Agregar(componente);
                }
            }
        }

        public void AsignarARol(int idRol, int idComponente)
        {
            bllDV.EjecutarControlado(() => dalPermiso.AsignarComponenteARol(idRol, idComponente), "RolFamilia");
            bllEvento.RegistrarDelUsuarioActual("Asignar permiso a rol",
                "Se asignó el componente " + idComponente + " al rol " + idRol + ".");
        }

        public void QuitarDeRol(int idRol, int idComponente)
        {
            bllDV.EjecutarControlado(() => dalPermiso.QuitarComponenteDeRol(idRol, idComponente), "RolFamilia");
            bllEvento.RegistrarDelUsuarioActual("Quitar permiso de rol",
                "Se quitó el componente " + idComponente + " del rol " + idRol + ".");
        }

        public BERol ObtenerRol(int idRol)
        {
            foreach (BERol rol in TraerRoles())
            {
                if (rol.Id == idRol)
                {
                    return rol;
                }
            }
            return null;
        }

        /// CU-003-004 (análogo, para roles). El rol WebMaster es parte del sistema
        /// (es el "administrador" del que depende el dígito verificador y el resto
        /// de la seguridad) y no se puede crear otro igual, así que alcanza con
        /// bloquear su modificación y baja.
        public int AltaRol(string nombre)
        {
            int asignados;
            return AltaRol(nombre, null, out asignados);
        }

        /// Alta de rol con sus familias y patentes. El rol y sus permisos se
        /// graban juntos, en una sola transacción: si algo falla no queda un
        /// rol a medio armar. Lo redundante (una patente que ya trae una de las
        /// familias elegidas, o una familia que ya está dentro de otra de las
        /// elegidas) no se graba dos veces; asignados dice cuántos quedaron.
        public int AltaRol(string nombre, List<int> idsComponentes, out int asignados)
        {
            ValidarNombre(nombre);

            if (ExisteNombreDeRol(nombre, 0))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                    "Ya existe un rol llamado " + nombre.Trim() + "."));
            }

            List<Componente> componentes = DepurarComponentes(idsComponentes);
            List<int> ids = IdsDe(componentes);

            int id = bllDV.EjecutarControlado(
                () => dalPermiso.CrearRolConComponentes(nombre.Trim(), ids), "Rol", "RolFamilia");

            asignados = ids.Count;
            bllEvento.RegistrarDelUsuarioActual("Alta de rol",
                "Se creó el rol " + nombre.Trim() + " (id " + id + ")" + DescribirComponentes(componentes));

            return id;
        }

        public void ModificarRol(int idRol, string nombre)
        {
            ValidarNombre(nombre);

            BERol rol = ObtenerRol(idRol);
            if (rol == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (EsRolWebMaster(rol))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR011",
                    "El rol " + rol.Nombre + " es parte del sistema y no se puede modificar."));
            }

            if (ExisteNombreDeRol(nombre, idRol))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010"));
            }

            bllDV.EjecutarControlado(() => dalPermiso.RenombrarRol(idRol, nombre.Trim()), "Rol");
            bllEvento.RegistrarDelUsuarioActual("Modificación de rol",
                "Se renombró el rol id " + idRol + ". Nuevo nombre: " + nombre.Trim() + ".");
        }

        public void BajaRol(int idRol)
        {
            BERol rol = ObtenerRol(idRol);
            if (rol == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (EsRolWebMaster(rol))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR011",
                    "El rol " + rol.Nombre + " es parte del sistema y no se puede eliminar."));
            }

            BEUsuario actual = SessionManager.UsuarioActual;
            if (actual != null && actual.IdRol == idRol)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR012",
                    "No puede eliminar el rol de su propia cuenta."));
            }

            if (dalPermiso.ContarUsuariosConRol(idRol) > 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "Hay usuarios con el rol " + rol.Nombre +
                    " asignado: reasígnelos a otro rol antes de eliminarlo."));
            }

            bllDV.EjecutarControlado(() => dalPermiso.EliminarRol(idRol), "RolFamilia", "Rol");

            bllEvento.RegistrarDelUsuarioActual("Baja de rol",
                "Se eliminó el rol " + rol.Nombre + " (id " + idRol + ").");
        }

        /// Indica cuántos usuarios tienen el rol, para avisar antes de eliminarlo.
        public string DescribirUsoDeRol(int idRol)
        {
            int usuarios = dalPermiso.ContarUsuariosConRol(idRol);
            if (usuarios == 0)
            {
                return "El rol no tiene usuarios asignados.";
            }
            return "El rol tiene " + usuarios +
                   " usuario(s) asignado(s). Reasígnelos a otro rol antes de eliminarlo.";
        }

        private static bool EsRolWebMaster(BERol rol)
        {
            return rol != null && string.Equals(rol.Nombre, "WebMaster", StringComparison.OrdinalIgnoreCase);
        }

        private bool ExisteNombreDeRol(string nombre, int idExcluido)
        {
            foreach (BERol rol in TraerRoles())
            {
                if (rol.Id != idExcluido &&
                    string.Equals(rol.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region ABM de familias y patentes

        /// Da de alta una familia. Solo se crean familias: las patentes representan
        /// funcionalidades concretas del sistema y vienen cargadas con el script,
        /// no tiene sentido que el usuario pueda inventar una.
        public int Alta(string nombre)
        {
            int asignados;
            return Alta(nombre, null, out asignados);
        }

        /// CU-003-004 Alta de familia. La familia y sus componentes se graban
        /// juntos, en una sola transacción: si algo falla no queda una familia
        /// a medio armar. Lo redundante (una patente que ya trae una de las
        /// familias elegidas, o una familia que ya está dentro de otra de las
        /// elegidas) no se graba dos veces; asignados dice cuántos quedaron.
        public int Alta(string nombre, List<int> idsComponentes, out int asignados)
        {
            ValidarNombre(nombre);

            if (ExisteNombre(nombre, 0))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                    "Ya existe un componente llamado " + nombre.Trim() + "."));
            }

            List<Componente> componentes = DepurarComponentes(idsComponentes);
            List<int> ids = IdsDe(componentes);

            int id = bllDV.EjecutarControlado(
                () => dalPermiso.CrearFamiliaConHijos(nombre.Trim(), ids), "Familia", "RelacionFamilia");

            asignados = ids.Count;
            bllEvento.RegistrarDelUsuarioActual("Alta de familia",
                "Se creó la familia " + nombre.Trim() + " (id " + id + ")" + DescribirComponentes(componentes));

            return id;
        }

        /// Busca los componentes elegidos y descarta los redundantes: una
        /// familia que ya está dentro de otra de las elegidas, o una patente que
        /// ya llega a través de una de ellas.
        private List<Componente> DepurarComponentes(List<int> idsComponentes)
        {
            List<Componente> resultado = new List<Componente>();
            if (idsComponentes == null || idsComponentes.Count == 0)
            {
                return resultado;
            }

            Dictionary<int, Componente> arbol = ConstruirArbol();

            List<Familia> familias = new List<Familia>();
            List<Componente> patentes = new List<Componente>();
            foreach (int id in idsComponentes)
            {
                Componente componente;
                if (!arbol.TryGetValue(id, out componente))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR013",
                        "Uno de los componentes elegidos ya no existe. Vuelva a cargar la pantalla."));
                }

                Familia familia = componente as Familia;
                if (familia != null)
                {
                    if (!familias.Exists(delegate(Familia f) { return f.Id == familia.Id; }))
                    {
                        familias.Add(familia);
                    }
                }
                else if (!patentes.Exists(delegate(Componente p) { return p.Id == componente.Id; }))
                {
                    patentes.Add(componente);
                }
            }

            foreach (Familia familia in familias)
            {
                bool cubierta = familias.Exists(delegate(Familia otra)
                {
                    return otra.Id != familia.Id && otra.ContieneFamilia(familia);
                });
                if (!cubierta)
                {
                    resultado.Add(familia);
                }
            }

            foreach (Componente patente in patentes)
            {
                bool cubierta = resultado.Exists(delegate(Componente f) { return f.Contiene(patente.Nombre); });
                if (!cubierta)
                {
                    resultado.Add(patente);
                }
            }

            return resultado;
        }

        private static List<int> IdsDe(List<Componente> componentes)
        {
            List<int> ids = new List<int>();
            foreach (Componente componente in componentes)
            {
                ids.Add(componente.Id);
            }
            return ids;
        }

        private static string DescribirComponentes(List<Componente> componentes)
        {
            if (componentes.Count == 0)
            {
                return ", todavía sin componentes.";
            }

            List<string> nombres = new List<string>();
            foreach (Componente componente in componentes)
            {
                nombres.Add(componente.Nombre);
            }
            return ", con " + componentes.Count + (componentes.Count == 1 ? " componente: " : " componentes: ") +
                   string.Join(", ", nombres.ToArray()) + ".";
        }

        public void Modificar(int id, string nombre)
        {
            ValidarNombre(nombre);

            if (ExisteNombre(nombre, id))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010"));
            }

            bllDV.EjecutarControlado(() => dalPermiso.ModificarFamilia(id, nombre.Trim()), "Familia");
            bllEvento.RegistrarDelUsuarioActual("Modificación de familia",
                "Se modificó el componente id " + id + ". Nuevo nombre: " + nombre.Trim() + ".");
        }

        public void Baja(int id)
        {
            Componente componente = ObtenerComponente(id);
            if (componente == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            // Una patente representa una funcionalidad del sistema: no se elimina.
            if (componente.Tipo == 'P')
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR011"));
            }

            // Una familia que forma parte del rol propio o del WebMaster no se
            // puede eliminar: el sistema podría quedar sin nadie que administre
            // los permisos (CU-003-006, paso 6).
            BEUsuario actor = SessionManager.UsuarioActual;
            foreach (BERol rol in TraerRoles())
            {
                if (!RolIncluyeComponente(rol, id))
                {
                    continue;
                }

                if (actor != null && rol.Id == actor.IdRol)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR012",
                        "La familia " + componente.Nombre + " forma parte de los permisos de su propio rol."));
                }

                if (string.Equals(rol.Nombre, BLLUsuario.RolAdministrador, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR011",
                        "La familia " + componente.Nombre + " forma parte del perfil " + BLLUsuario.RolAdministrador +
                        ": eliminarla dejaría al sistema sin quien lo administre."));
                }
            }

            bllDV.EjecutarControlado(() => dalPermiso.EliminarFamilia(id), "RelacionFamilia", "RolFamilia", "Familia");

            bllEvento.RegistrarDelUsuarioActual("Baja de familia",
                "Se eliminó la familia " + componente.Nombre + " (id " + id + ").");
        }

        /// Indica si el componente está en el rol, directamente o dentro de
        /// alguna de sus familias.
        private static bool RolIncluyeComponente(BERol rol, int idComponente)
        {
            foreach (Componente componente in rol.Permisos)
            {
                if (ComponenteIncluye(componente, idComponente))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ComponenteIncluye(Componente componente, int idComponente)
        {
            if (componente == null)
            {
                return false;
            }

            if (componente.Id == idComponente)
            {
                return true;
            }

            foreach (Componente hijo in componente.Hijos)
            {
                if (ComponenteIncluye(hijo, idComponente))
                {
                    return true;
                }
            }
            return false;
        }

        /// Agrega un componente a una familia evitando permisos repetidos.
        ///
        /// Si el componente ya está (directamente o dentro de otra familia hija)
        /// se rechaza. Si lo que se agrega es una familia que ya cubre patentes
        /// sueltas de la misma familia padre, esas patentes se quitan: quedan
        /// representadas por la familia y no tiene sentido tenerlas dos veces.
        ///
        /// Devuelve el texto que describe lo que efectivamente pasó.
        public string AgregarHijo(int idPadre, int idHijo)
        {
            if (idPadre == idHijo)
            {
                throw new ApplicationException("Una familia no puede contenerse a sí misma.");
            }

            Dictionary<int, Componente> componentes = ConstruirArbol();

            Componente padre;
            Componente hijo;
            if (!componentes.TryGetValue(idPadre, out padre) || !componentes.TryGetValue(idHijo, out hijo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (padre.Tipo != 'F')
            {
                throw new ApplicationException("Solo una familia puede contener otros componentes.");
            }

            VerificarQueNoEsteRepetido(padre, hijo);

            // El Composite valida que no se genere una referencia circular.
            padre.Agregar(hijo);

            // Al incorporar una familia, las patentes sueltas que esa familia ya
            // cubre quedan duplicadas: se limpian.
            List<Componente> absorbidos = bllDV.EjecutarControlado(delegate
            {
                dalPermiso.AgregarHijo(idPadre, idHijo);
                return QuitarSueltosCubiertosPor(padre, hijo);
            }, "RelacionFamilia");

            string detalle = "Se agregó " + hijo.Nombre + " a la familia " + padre.Nombre + ".";
            if (absorbidos.Count > 0)
            {
                detalle += " Se quitaron " + absorbidos.Count +
                           " permisos que quedaron cubiertos por esa familia.";
            }

            bllEvento.RegistrarDelUsuarioActual("Modificación de familia", detalle);
            return detalle;
        }

        private static void VerificarQueNoEsteRepetido(Componente padre, Componente hijo)
        {
            foreach (Componente actual in padre.Hijos)
            {
                if (actual.Id == hijo.Id)
                {
                    throw new ApplicationException(
                        hijo.Nombre + " ya está asignado a la familia " + padre.Nombre + ".");
                }

                // Contiene() recorre todo el subárbol, así que también detecta el
                // caso de una patente que llega a través de una familia hija.
                if (hijo.Tipo == 'P' && actual.Tipo == 'F' && actual.Contiene(hijo.Nombre))
                {
                    throw new ApplicationException(
                        "La patente " + hijo.Nombre + " ya está incluida a través de la familia " +
                        actual.Nombre + ".");
                }

                if (hijo.Tipo == 'F' && actual.Tipo == 'F')
                {
                    Familia contenedora = actual as Familia;
                    Familia candidata = hijo as Familia;
                    if (contenedora != null && candidata != null && contenedora.ContieneFamilia(candidata))
                    {
                        throw new ApplicationException(
                            "La familia " + hijo.Nombre + " ya está incluida dentro de " +
                            actual.Nombre + ".");
                    }
                }
            }
        }

        private List<Componente> QuitarSueltosCubiertosPor(Componente padre, Componente incorporado)
        {
            List<Componente> quitados = new List<Componente>();

            if (incorporado.Tipo != 'F')
            {
                return quitados;
            }

            foreach (Componente actual in new List<Componente>(padre.Hijos))
            {
                if (actual.Id == incorporado.Id)
                {
                    continue;
                }

                if (actual.Tipo == 'P' && incorporado.Contiene(actual.Nombre))
                {
                    dalPermiso.QuitarHijo(padre.Id, actual.Id);
                    quitados.Add(actual);
                }
            }

            return quitados;
        }

        public void QuitarHijo(int idPadre, int idHijo)
        {
            bllDV.EjecutarControlado(() => dalPermiso.QuitarHijo(idPadre, idHijo), "RelacionFamilia");
            bllEvento.RegistrarDelUsuarioActual("Modificación de familia",
                "Se quitó el componente " + idHijo + " de la familia " + idPadre + ".");
        }

        /// Indica donde esta siendo utilizado un componente antes de darlo de baja.
        public string DescribirUso(int id)
        {
            int roles = dalPermiso.ContarRolesQueUsan(id);
            int familias = dalPermiso.ContarFamiliasQueUsan(id);

            if (roles == 0 && familias == 0)
            {
                return "El componente no está asignado a ningún rol ni familia.";
            }

            return "El componente está asignado a " + roles + " rol(es) y contenido en " +
                   familias + " familia(s). Al eliminarlo se quitarán esas asignaciones.";
        }

        private bool ExisteNombre(string nombre, int idExcluido)
        {
            foreach (Componente componente in TraerTodos())
            {
                if (componente.Id != idExcluido &&
                    string.Equals(componente.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static void ValidarNombre(string nombre)
        {
            if (string.IsNullOrEmpty(nombre) || nombre.Trim().Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }
        }

        private static int CompararPorTipoYNombre(Componente x, Componente y)
        {
            int comparacion = x.Tipo.CompareTo(y.Tipo);
            if (comparacion != 0)
            {
                return comparacion;
            }
            return string.Compare(x.Nombre, y.Nombre, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
