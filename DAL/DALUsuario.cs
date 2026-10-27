using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de la tabla Usuario.
    public class DALUsuario
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Recupera un usuario a partir de su nombre de usuario (login).
        public BEUsuario ValidarUsuario(string nombreUsuario)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@NombreUsuario", nombreUsuario)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("ValidarUsuario", parametros);
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            BEUsuario usuario = Mapear(fila);
            usuario.Clave = Convert.ToString(fila["contrasena"]);
            usuario.Rol = new BERol(Convert.ToInt32(fila["rolId"]), Convert.ToString(fila["rolNombre"]));
            return usuario;
        }

        public BEUsuario ObtenerPorDNI(int dni)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DNI", dni)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerUsuarioPorDNI", parametros);
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            BEUsuario usuario = Mapear(fila);
            usuario.Rol = new BERol(Convert.ToInt32(fila["id_rol"]), Convert.ToString(fila["rolNombre"]));
            return usuario;
        }

        public List<BEUsuario> TraerUsuarios()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerUsuarios", null);

            List<BEUsuario> lista = new List<BEUsuario>();
            foreach (DataRow fila in tabla.Rows)
            {
                BEUsuario usuario = Mapear(fila);
                usuario.Rol = new BERol(usuario.IdRol, Convert.ToString(fila["rolNombre"]));
                lista.Add(usuario);
            }
            return lista;
        }

        public void Crear(BEUsuario usuario)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DNI", usuario.DNI),
                new SqlParameter("@IdRol", usuario.IdRol),
                new SqlParameter("@Nombre", usuario.Nombre),
                new SqlParameter("@Apellido", usuario.Apellido),
                new SqlParameter("@FechaNacimiento", (object)usuario.FechaNacimiento ?? DBNull.Value),
                new SqlParameter("@CorreoElectronico", (object)usuario.CorreoElectronico ?? DBNull.Value),
                new SqlParameter("@NumeroTelefono", (object)usuario.NumeroTelefono ?? DBNull.Value),
                new SqlParameter("@NombreUsuario", usuario.NombreUsuario),
                new SqlParameter("@Contrasena", usuario.Clave)
            };

            dalCon.EjecutarProcAlmacenado("CrearUsuario", parametros);
        }

        public void Modificar(BEUsuario usuario)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DNI", usuario.DNI),
                new SqlParameter("@IdRol", usuario.IdRol),
                new SqlParameter("@Nombre", usuario.Nombre),
                new SqlParameter("@Apellido", usuario.Apellido),
                new SqlParameter("@FechaNacimiento", (object)usuario.FechaNacimiento ?? DBNull.Value),
                new SqlParameter("@CorreoElectronico", (object)usuario.CorreoElectronico ?? DBNull.Value),
                new SqlParameter("@NumeroTelefono", (object)usuario.NumeroTelefono ?? DBNull.Value),
                new SqlParameter("@NombreUsuario", usuario.NombreUsuario)
            };

            dalCon.EjecutarProcAlmacenado("ModificarUsuario", parametros);
        }

        public void Baja(int dni)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DNI", dni)
            };

            dalCon.EjecutarProcAlmacenado("BajaUsuario", parametros);
        }

        public void Activar(int dni)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DNI", dni)
            };

            dalCon.EjecutarProcAlmacenado("ActivarUsuario", parametros);
        }

        public void ModificarBloqueo(int dni, bool bloqueado)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DNI", dni),
                new SqlParameter("@Bloqueo", bloqueado)
            };

            dalCon.EjecutarProcAlmacenado("ModificarBloquearUsuario", parametros);
        }

        public void ModificarContFallido(string nombreUsuario, int intentos)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@NombreUsuario", nombreUsuario),
                new SqlParameter("@ContClaveIncorrecta", intentos)
            };

            dalCon.EjecutarProcAlmacenado("ModificarContFallido", parametros);
        }

        public void CambiarClave(string nombreUsuario, string claveEncriptada)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@NombreUsuario", nombreUsuario),
                new SqlParameter("@NuevaContrasena", claveEncriptada)
            };

            dalCon.EjecutarProcAlmacenado("CambiarContrasena", parametros);
        }

        /// Guarda un enlace de recuperación de contraseña (solo su hash).
        public void CrearTokenRecuperacion(BETokenRecuperacion token)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@TokenHash", token.TokenHash),
                new SqlParameter("@DniUsuario", token.DniUsuario),
                new SqlParameter("@FechaExpiracion", token.FechaExpiracion)
            };

            dalCon.EjecutarProcAlmacenado("CrearTokenRecuperacion", parametros);
        }

        public BETokenRecuperacion ObtenerTokenRecuperacion(string tokenHash)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@TokenHash", tokenHash)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerTokenRecuperacion", parametros);
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = tabla.Rows[0];
            BETokenRecuperacion token = new BETokenRecuperacion();
            token.TokenHash = Convert.ToString(fila["tokenHash"]);
            token.DniUsuario = Convert.ToInt32(fila["dni_usuario"]);
            token.FechaExpiracion = Convert.ToDateTime(fila["fechaExpiracion"]);
            token.Usado = Convert.ToBoolean(fila["usado"]);
            return token;
        }

        public void MarcarTokenRecuperacionUsado(string tokenHash)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@TokenHash", tokenHash)
            };

            dalCon.EjecutarProcAlmacenado("MarcarTokenRecuperacionUsado", parametros);
        }

        /// Anula los enlaces pendientes del usuario: al pedir uno nuevo, los
        /// anteriores dejan de servir.
        public void InvalidarTokensDeUsuario(int dni)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DniUsuario", dni)
            };

            dalCon.EjecutarProcAlmacenado("InvalidarTokensDeUsuario", parametros);
        }

        /// Persiste el dígito verificador horizontal del registro del usuario.
        public void ActualizarDVH(int dni, string dvh)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DNI", dni),
                new SqlParameter("@DVH", dvh)
            };

            dalCon.EjecutarProcAlmacenado("ActualizarDVHUsuario", parametros);
        }

        private static BEUsuario Mapear(DataRow fila)
        {
            BEUsuario usuario = new BEUsuario();
            usuario.DNI = Convert.ToInt32(fila["dni"]);
            usuario.IdRol = Convert.ToInt32(fila["id_rol"]);
            usuario.Nombre = Convert.ToString(fila["nombre"]);
            usuario.Apellido = Convert.ToString(fila["apellido"]);
            usuario.FechaNacimiento = fila["fechaNacimiento"] == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(fila["fechaNacimiento"]);
            usuario.CorreoElectronico = Convert.ToString(fila["correoElectronico"]);
            usuario.NumeroTelefono = Convert.ToString(fila["numeroTelefono"]);
            usuario.NombreUsuario = Convert.ToString(fila["nombreUsuario"]);
            usuario.Activo = fila["activo"] != DBNull.Value && Convert.ToBoolean(fila["activo"]);
            usuario.Bloqueado = fila["bloqueado"] != DBNull.Value && Convert.ToBoolean(fila["bloqueado"]);
            usuario.IntentosFallidos = fila["intentosFallidos"] == DBNull.Value
                ? 0
                : Convert.ToInt32(fila["intentosFallidos"]);
            return usuario;
        }
    }
}
