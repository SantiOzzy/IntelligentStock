using System;
using System.Web;
using System.Web.SessionState;
using BE;
using BLL;
using Servicios;

/// Atiende al aviso de cierre de sesión por inactividad de Site.master
/// (MantenerSesion.ashx):
///   - accion=renovar: con solo pedir la sesión, el plazo de inactividad
///     vuelve a empezar. Responde si la sesión sigue iniciada.
///   - accion=cerrar: se cumplió el plazo sin que el usuario pidiera seguir
///     conectado. Cierra la sesión y lo deja en la bitácora.
/// Solo acepta POST hechos desde las páginas del sitio (con el encabezado
/// X-Requested-With que agrega jQuery, que otro sitio no puede mandar), así una
/// página ajena no puede cerrarle la sesión a nadie.
public class MantenerSesion : IHttpHandler, IRequiresSessionState
{
    public bool IsReusable
    {
        get { return true; }
    }

    public void ProcessRequest(HttpContext context)
    {
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        context.Response.ContentType = "application/json";

        if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Headers["X-Requested-With"] != "XMLHttpRequest")
        {
            context.Response.StatusCode = 400;
            return;
        }

        BEUsuario usuario = SessionManager.ObtenerInstancia().UsuarioActual;
        if (context.Request.Form["accion"] == "cerrar")
        {
            if (usuario != null)
            {
                new BLLUsuario().LogoutPorInactividad(usuario, context.Session.Timeout);
            }
            context.Response.Write("{\"activa\":false}");
            return;
        }

        context.Response.Write(usuario != null ? "{\"activa\":true}" : "{\"activa\":false}");
    }
}
