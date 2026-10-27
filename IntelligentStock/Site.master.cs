using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using BE;
using Servicios;

public partial class SiteMaster : MasterPage
{
    private const string AntiXsrfTokenKey = "__AntiXsrfToken";
    private const string AntiXsrfUserNameKey = "__AntiXsrfUserName";
    private string _antiXsrfTokenValue;

    protected void Page_Init(object sender, EventArgs e)
    {
        // El codigo siguiente ayuda a proteger frente a ataques XSRF
        var requestCookie = Request.Cookies[AntiXsrfTokenKey];
        Guid requestCookieGuidValue;
        if (requestCookie != null && Guid.TryParse(requestCookie.Value, out requestCookieGuidValue))
        {
            _antiXsrfTokenValue = requestCookie.Value;
            Page.ViewStateUserKey = _antiXsrfTokenValue;
        }
        else
        {
            _antiXsrfTokenValue = Guid.NewGuid().ToString("N");
            Page.ViewStateUserKey = _antiXsrfTokenValue;

            var responseCookie = new HttpCookie(AntiXsrfTokenKey)
            {
                HttpOnly = true,
                Value = _antiXsrfTokenValue
            };
            if (FormsAuthentication.RequireSSL && Request.IsSecureConnection)
            {
                responseCookie.Secure = true;
            }
            Response.Cookies.Set(responseCookie);
        }

        Page.PreLoad += master_Page_PreLoad;
    }

    protected void master_Page_PreLoad(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ViewState[AntiXsrfTokenKey] = Page.ViewStateUserKey;
            ViewState[AntiXsrfUserNameKey] = SessionManager.NombreUsuarioActual;
        }
        else
        {
            if ((string)ViewState[AntiXsrfTokenKey] != _antiXsrfTokenValue)
            {
                throw new InvalidOperationException("Error de validación del token Anti-XSRF.");
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        ArmarMenu();
    }

    /// <summary>
    /// Arma la barra de navegacion a partir del mapa de navegacion filtrado
    /// por el Composite de permisos del rol del usuario logueado.
    /// </summary>
    private void ArmarMenu()
    {
        BEUsuario usuario = SessionManager.UsuarioActual;

        if (usuario == null || SessionManager.BaseDeDatosInconsistente)
        {
            litMenu.Text = string.Empty;
            litUsuario.Text = string.Empty;
            return;
        }

        List<ModuloMenu> modulos = MenuSistema.ObtenerMenuDe(usuario);

        StringBuilder menu = new StringBuilder();
        menu.Append("<ul class=\"nav navbar-nav\">");
        menu.Append("<li><a href=\"" + ResolveUrl("~/IniciarSesion.aspx") + "\">Panel principal</a></li>");

        foreach (ModuloMenu modulo in modulos)
        {
            menu.Append("<li class=\"dropdown\">");
            menu.Append("<a href=\"#\" class=\"dropdown-toggle\" data-toggle=\"dropdown\">");
            menu.Append(HttpUtility.HtmlEncode(modulo.Titulo));
            menu.Append(" <span class=\"caret\"></span></a>");
            menu.Append("<ul class=\"dropdown-menu\">");

            foreach (OpcionMenu opcion in modulo.Opciones)
            {
                menu.Append("<li><a href=\"");
                menu.Append(ResolveUrl(opcion.Url));
                menu.Append("\">");
                menu.Append(HttpUtility.HtmlEncode(opcion.Texto));
                menu.Append("</a></li>");
            }

            menu.Append("</ul></li>");
        }

        menu.Append("</ul>");
        litMenu.Text = menu.ToString();

        StringBuilder derecha = new StringBuilder();
        derecha.Append("<ul class=\"nav navbar-nav navbar-right\">");
        derecha.Append("<li><a href=\"#\">");
        derecha.Append(HttpUtility.HtmlEncode(usuario.NombreUsuario));
        derecha.Append(" (");
        derecha.Append(HttpUtility.HtmlEncode(usuario.NombreRol));
        derecha.Append(")</a></li>");
        derecha.Append("<li><a href=\"");
        derecha.Append(ResolveUrl("~/CerrarSesion.aspx"));
        derecha.Append("\">Cerrar sesión</a></li>");
        derecha.Append("</ul>");
        litUsuario.Text = derecha.ToString();
    }
}
