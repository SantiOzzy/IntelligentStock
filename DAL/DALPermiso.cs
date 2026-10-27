using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using BE;

namespace DAL
{
    /// Acceso a datos de las tablas Rol, Familia, RelacionFamilia y RolFamilia.
    /// Reconstruye en memoria el arbol del patron Composite.
    public class DALPermiso
    {
        private readonly DALConexion dalCon = new DALConexion();

        #region Roles

        public List<BERol> TraerRoles()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerRoles", null);

            List<BERol> roles = new List<BERol>();
            foreach (DataRow fila in tabla.Rows)
            {
                roles.Add(new BERol(Convert.ToInt32(fila["id_rol"]), Convert.ToString(fila["nombre"])));
            }
            return roles;
        }

        /// Pares (id_rol, id_familia) de la tabla RolFamilia.
        public DataTable TraerAsignacionesDeRoles()
        {
            return dalCon.ConsultaProcAlmacenado("TraerRolFamilia", null);
        }

        public void AsignarComponenteARol(int idRol, int idComponente)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdRol", idRol),
                new SqlParameter("@IdFamilia", idComponente)
            };

            dalCon.EjecutarProcAlmacenado("AsignarFamiliaARol", parametros);
        }

        public void QuitarComponenteDeRol(int idRol, int idComponente)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdRol", idRol),
                new SqlParameter("@IdFamilia", idComponente)
            };

            dalCon.EjecutarProcAlmacenado("QuitarFamiliaDeRol", parametros);
        }

        public int CrearRol(string nombre)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Nombre", nombre)
            };

            return dalCon.EjecutarYTraerId("CrearRol", parametros);
        }

        /// Crea el rol y le asigna sus familias y patentes en un solo
        /// procedimiento, dentro de la misma transacción: o queda todo grabado
        /// o no se graba nada. Devuelve el id del rol nuevo.
        public int CrearRolConComponentes(string nombre, List<int> idsComponentes)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Nombre", nombre),
                ParametroComponentes("@Componentes", idsComponentes)
            };

            return dalCon.EjecutarYTraerId("CrearRolConComponentes", parametros);
        }

        /// Lista de ids como XML: <Componentes><Componente id="5" /></Componentes>
        private static SqlParameter ParametroComponentes(string nombreParametro, List<int> ids)
        {
            StringBuilder xml = new StringBuilder("<Componentes>");
            if (ids != null)
            {
                foreach (int id in ids)
                {
                    xml.Append("<Componente id=\"").Append(id.ToString(CultureInfo.InvariantCulture)).Append("\" />");
                }
            }
            xml.Append("</Componentes>");

            SqlParameter parametro = new SqlParameter(nombreParametro, SqlDbType.Xml);
            parametro.Value = xml.ToString();
            return parametro;
        }

        public void RenombrarRol(int idRol, string nombre)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdRol", idRol),
                new SqlParameter("@Nombre", nombre)
            };

            dalCon.EjecutarProcAlmacenado("RenombrarRol", parametros);
        }

        public void EliminarRol(int idRol)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdRol", idRol)
            };

            dalCon.EjecutarProcAlmacenado("EliminarRol", parametros);
        }

        /// Cantidad de usuarios que tienen asignado el rol indicado.
        public int ContarUsuariosConRol(int idRol)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdRol", idRol)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("ContarUsuariosConRol", parametros);
            return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0][0]);
        }

        #endregion

        #region Familias y patentes

        /// Devuelve todas las familias y patentes sin la jerarquia resuelta.
        public DataTable TraerComponentes()
        {
            return dalCon.ConsultaProcAlmacenado("TraerFamilias", null);
        }

        public DataTable TraerComponentesPorTipo(char tipo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Tipo", tipo.ToString())
            };

            return dalCon.ConsultaProcAlmacenado("TraerFamiliasPorTipo", parametros);
        }

        /// Pares (id_familiaPadre, id_familiaHija) de la tabla RelacionFamilia.
        public DataTable TraerRelaciones()
        {
            return dalCon.ConsultaProcAlmacenado("TraerRelacionesFamilia", null);
        }

        public int CrearFamilia(string nombre, char tipo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Nombre", nombre),
                new SqlParameter("@Tipo", tipo.ToString())
            };

            return dalCon.EjecutarYTraerId("CrearFamilia", parametros);
        }

        /// Crea la familia y le cuelga sus componentes en un solo procedimiento,
        /// dentro de la misma transacción: o queda la familia con todos sus
        /// hijos o no se graba nada. Devuelve el id de la familia nueva.
        public int CrearFamiliaConHijos(string nombre, List<int> idsHijos)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Nombre", nombre),
                ParametroComponentes("@Hijos", idsHijos)
            };

            return dalCon.EjecutarYTraerId("CrearFamiliaConHijos", parametros);
        }

        public void ModificarFamilia(int idFamilia, string nombre)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdFamilia", idFamilia),
                new SqlParameter("@Nombre", nombre)
            };

            dalCon.EjecutarProcAlmacenado("ModificarFamilia", parametros);
        }

        public void EliminarFamilia(int idFamilia)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdFamilia", idFamilia)
            };

            dalCon.EjecutarProcAlmacenado("EliminarFamilia", parametros);
        }

        public void AgregarHijo(int idPadre, int idHijo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdPadre", idPadre),
                new SqlParameter("@IdHijo", idHijo)
            };

            dalCon.EjecutarProcAlmacenado("AgregarHijoAFamilia", parametros);
        }

        public void QuitarHijo(int idPadre, int idHijo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdPadre", idPadre),
                new SqlParameter("@IdHijo", idHijo)
            };

            dalCon.EjecutarProcAlmacenado("QuitarHijoDeFamilia", parametros);
        }

        /// Cantidad de roles que tienen asignado el componente indicado.
        public int ContarRolesQueUsan(int idComponente)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdFamilia", idComponente)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("ContarRolesDeFamilia", parametros);
            return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0][0]);
        }

        /// Cantidad de familias padre que contienen al componente indicado.
        public int ContarFamiliasQueUsan(int idComponente)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdFamilia", idComponente)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("ContarPadresDeFamilia", parametros);
            return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0][0]);
        }

        #endregion
    }
}
