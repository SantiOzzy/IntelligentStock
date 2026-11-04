using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using BE;

namespace Servicios
{
    /// Patrón Singleton: punto único de acceso a los datos de la sesión del
    /// usuario logueado. Evita que cada formulario tenga que conocer las claves
    /// de Session.
    ///
    /// Hay una sola instancia en toda la aplicación, creada la primera vez que
    /// se usa la clase (inicialización estática: el CLR garantiza que se crea
    /// una sola vez aunque lleguen varios pedidos al mismo tiempo, sin lock).
    /// El constructor es privado, así que nadie más puede crear otra, y se
    /// obtiene con SessionManager.ObtenerInstancia().
    ///
    /// La instancia no guarda datos en campos propios: en una aplicación web
    /// es compartida por todos los usuarios conectados, así que si guardara
    /// "el usuario logueado" en un campo, todos verían al mismo. Cada dato se
    /// lee y se escribe en la Session de ASP.NET del pedido actual, que es
    /// propia de cada usuario.
    public sealed class SessionManager
    {
        private static readonly SessionManager instancia = new SessionManager();

        private SessionManager()
        {
        }

        public static SessionManager ObtenerInstancia()
        {
            return instancia;
        }

        private const string ClaveUsuario = "UsuarioLogueado";
        private const string ClaveInconsistencias = "InconsistenciasDV";
        private const string ClaveVersionPermisos = "VersionPermisos";

        private HttpSessionState Sesion
        {
            get
            {
                return HttpContext.Current == null ? null : HttpContext.Current.Session;
            }
        }

        public BEUsuario UsuarioActual
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

        public bool HaySesionActiva
        {
            get { return UsuarioActual != null; }
        }

        /// DNI del usuario logueado, o null si todavia no inicio sesion.
        public int? DniActual
        {
            get
            {
                BEUsuario usuario = UsuarioActual;
                return usuario == null ? (int?)null : usuario.DNI;
            }
        }

        public string NombreUsuarioActual
        {
            get
            {
                BEUsuario usuario = UsuarioActual;
                return usuario == null ? string.Empty : usuario.NombreUsuario;
            }
        }

        /// Versión de la estructura de permisos con la que se cargaron los
        /// permisos del usuario de la sesión (ver BLLPermiso.VersionPermisos).
        /// Null si todavía no se anotó.
        public long? VersionPermisos
        {
            get
            {
                HttpSessionState sesion = Sesion;
                return sesion == null ? null : sesion[ClaveVersionPermisos] as long?;
            }
            set
            {
                HttpSessionState sesion = Sesion;
                if (sesion != null)
                {
                    sesion[ClaveVersionPermisos] = value;
                }
            }
        }

        /// Inconsistencias de DV detectadas durante el ultimo intento de login.
        public List<Inconsistencia> Inconsistencias
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

        public bool BaseDeDatosInconsistente
        {
            get
            {
                List<Inconsistencia> lista = Inconsistencias;
                return lista != null && lista.Count > 0;
            }
        }

        public void LimpiarInconsistencias()
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
        public void CerrarSesion()
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
