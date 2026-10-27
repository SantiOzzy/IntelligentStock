using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using BE;

namespace Servicios
{
    /// Punto unico de acceso a los datos de la sesion del usuario logueado.
    /// Evita que cada formulario tenga que conocer las claves de Session.
    public static class SessionManager
    {
        private const string ClaveUsuario = "UsuarioLogueado";
        private const string ClaveInconsistencias = "InconsistenciasDV";

        private static HttpSessionState Sesion
        {
            get
            {
                return HttpContext.Current == null ? null : HttpContext.Current.Session;
            }
        }

        public static BEUsuario UsuarioActual
        {
            get
            {
                HttpSessionState sesion = Sesion;
                return sesion == null ? null : sesion[ClaveUsuario] as BEUsuario;
            }
            set
            {
                HttpSessionState sesion = Sesion;
                if (sesion != null)
                {
                    sesion[ClaveUsuario] = value;
                }
            }
        }

        public static bool HaySesionActiva
        {
            get { return UsuarioActual != null; }
        }

        /// DNI del usuario logueado, o null si todavia no inicio sesion.
        public static int? DniActual
        {
            get
            {
                BEUsuario usuario = UsuarioActual;
                return usuario == null ? (int?)null : usuario.DNI;
            }
        }

        public static string NombreUsuarioActual
        {
            get
            {
                BEUsuario usuario = UsuarioActual;
                return usuario == null ? string.Empty : usuario.NombreUsuario;
            }
        }

        /// Inconsistencias de DV detectadas durante el ultimo intento de login.
        public static List<Inconsistencia> Inconsistencias
        {
            get
            {
                HttpSessionState sesion = Sesion;
                return sesion == null ? null : sesion[ClaveInconsistencias] as List<Inconsistencia>;
            }
            set
            {
                HttpSessionState sesion = Sesion;
                if (sesion != null)
                {
                    sesion[ClaveInconsistencias] = value;
                }
            }
        }

        public static bool BaseDeDatosInconsistente
        {
            get
            {
                List<Inconsistencia> lista = Inconsistencias;
                return lista != null && lista.Count > 0;
            }
        }

        public static void LimpiarInconsistencias()
        {
            HttpSessionState sesion = Sesion;
            if (sesion != null)
            {
                sesion[ClaveInconsistencias] = null;
            }
        }

        /// Cierra la sesión. Además de vaciarla, la abandona y vence la cookie
        /// con su identificador: el próximo pedido arranca una sesión nueva y
        /// nadie puede seguir usando el identificador anterior.
        public static void CerrarSesion()
        {
            HttpSessionState sesion = Sesion;
            if (sesion != null)
            {
                sesion[ClaveUsuario] = null;
                sesion[ClaveInconsistencias] = null;
                sesion.Clear();
                sesion.Abandon();
            }

            HttpContext contexto = HttpContext.Current;
            if (contexto != null)
            {
                HttpCookie vencida = new HttpCookie("ASP.NET_SessionId", string.Empty);
                vencida.Expires = DateTime.Now.AddYears(-1);
                vencida.HttpOnly = true;
                contexto.Response.Cookies.Add(vencida);
            }
        }
    }
}
