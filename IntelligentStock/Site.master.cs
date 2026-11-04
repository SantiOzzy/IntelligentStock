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

    /// Clase del body: "con-menu" cuando hay un usuario con sesión (se ve el
    /// menú lateral) y "sin-menu" en el inicio de sesión y en la reparación
    /// del dígito verificador.
    protected string ClaseDelCuerpo
    {
        get { return tieneMenu ? "con-menu" : "sin-menu"; }
    }

    private bool tieneMenu;

    /// <summary>
    /// Arma el menú lateral a partir del mapa de navegación filtrado por el
    /// Composite de permisos del rol del usuario logueado. Marca la pantalla
    /// actual, deja abierto su grupo y arma la ruta "Módulo › Pantalla" de la
    /// barra superior.
    /// </summary>
    private void ArmarMenu()
    {
        BEUsuario usuario = SessionManager.UsuarioActual;

        if (usuario == null || SessionManager.BaseDeDatosInconsistente)
        {
            tieneMenu = false;
            litMenu.Text = string.Empty;
            litUsuario.Text = string.Empty;
            litSalir.Text = string.Empty;
            litMigas.Text = Migas(null, Page.Title);
            return;
        }

        tieneMenu = true;
        List<ModuloMenu> modulos = MenuSistema.ObtenerMenuDe(usuario);
        string actual = Request.AppRelativeCurrentExecutionFilePath ?? string.Empty;
        string moduloActual = null, opcionActual = null;

        StringBuilder menu = new StringBuilder();
        menu.Append("<nav class=\"menu\">");

        bool enPanel = EsLaPaginaActual("~/IniciarSesion.aspx", actual);
        menu.Append(Enlace("~/IniciarSesion.aspx", "Panel principal", enPanel, "menu-enlace menu-inicio", "home"));

        int numero = 0;
        foreach (ModuloMenu modulo in modulos)
        {
            numero++;
            bool contieneLaActual = false;
            StringBuilder opciones = new StringBuilder();
            foreach (OpcionMenu opcion in modulo.Opciones)
            {
                bool esActual = EsLaPaginaActual(opcion.Url, actual);
                if (esActual)
                {
                    contieneLaActual = true;
                    moduloActual = modulo.Titulo;
                    opcionActual = opcion.Texto;
                }
                opciones.Append("<li>").Append(Enlace(opcion.Url, opcion.Texto, esActual, "menu-enlace")).Append("</li>");
            }

            string idLista = "menuGrupo" + numero;
            menu.Append("<div class=\"menu-grupo").Append(contieneLaActual ? " abierto" : string.Empty)
                .Append("\" data-grupo=\"").Append(HttpUtility.HtmlAttributeEncode(modulo.Titulo)).Append("\">");
            menu.Append("<button type=\"button\" class=\"menu-grupo-titulo\" aria-controls=\"").Append(idLista)
                .Append("\" aria-expanded=\"").Append(contieneLaActual ? "true" : "false").Append("\">");
            menu.Append("<span class=\"glyphicon glyphicon-").Append(IconoDelModulo(modulo.Titulo))
                .Append(" menu-icono\" aria-hidden=\"true\"></span>");
            menu.Append("<span class=\"menu-grupo-texto\">").Append(HttpUtility.HtmlEncode(modulo.Titulo)).Append("</span>");
            menu.Append("<span class=\"menu-flecha\" aria-hidden=\"true\"></span></button>");
            menu.Append("<ul class=\"menu-opciones\" id=\"").Append(idLista).Append("\">");
            menu.Append(opciones);
            menu.Append("</ul></div>");
        }

        menu.Append("</nav>");
        litMenu.Text = menu.ToString();

        litMigas.Text = enPanel ? Migas(null, "Panel principal") : Migas(moduloActual, opcionActual ?? Page.Title);

        StringBuilder derecha = new StringBuilder();
        derecha.Append("<div class=\"usuario-bloque\">");
        derecha.Append("<span class=\"usuario-avatar\" translate=\"no\" aria-hidden=\"true\">")
               .Append(HttpUtility.HtmlEncode(Iniciales(usuario))).Append("</span>");
        derecha.Append("<div class=\"usuario\">");
        derecha.Append("<span class=\"usuario-nombre\" translate=\"no\">").Append(HttpUtility.HtmlEncode(usuario.NombreUsuario)).Append("</span>");
        derecha.Append("<span class=\"usuario-rol\">").Append(HttpUtility.HtmlEncode(usuario.NombreRol)).Append("</span>");
        derecha.Append("</div></div>");
        litUsuario.Text = derecha.ToString();

        // El selector de idioma queda entre el usuario y "Cerrar sesión" (ver Site.master).
        litSalir.Text = "<a class=\"boton-salir\" href=\"" + ResolveUrl("~/CerrarSesion.aspx") + "\">Cerrar sesión</a>";
    }

    private string Enlace(string url, string texto, bool actual, string clase)
    {
        return Enlace(url, texto, actual, clase, null);
    }

    private string Enlace(string url, string texto, bool actual, string clase, string icono)
    {
        return "<a class=\"" + clase + (actual ? " activo" : string.Empty) + "\" href=\"" + ResolveUrl(url) + "\"" +
               (actual ? " aria-current=\"page\"" : string.Empty) + ">" +
               (icono == null ? string.Empty : "<span class=\"glyphicon glyphicon-" + icono + " menu-icono\" aria-hidden=\"true\"></span>") +
               HttpUtility.HtmlEncode(texto) + "</a>";
    }

    /// Ícono de cada módulo del menú (Glyphicons de Bootstrap). La pantalla
    /// también lo muestra junto a su título.
    private static string IconoDelModulo(string modulo)
    {
        switch (modulo)
        {
            case "Seguridad": return "user";
            case "Permisos": return "lock";
            case "Base de datos": return "hdd";
            case "Proveedores": return "briefcase";
            case "Insumos y recetas": return "cutlery";
            case "Operaciones": return "shopping-cart";
            case "Telemetría IoT": return "dashboard";
            case "Suscripción": return "credit-card";
            default: return "folder-open";
        }
    }

    /// Iniciales del usuario para el círculo de la barra superior.
    private static string Iniciales(BEUsuario usuario)
    {
        string nombre = (usuario.Nombre ?? string.Empty).Trim();
        string apellido = (usuario.Apellido ?? string.Empty).Trim();
        if (nombre.Length > 0 && apellido.Length > 0)
        {
            return (nombre.Substring(0, 1) + apellido.Substring(0, 1)).ToUpperInvariant();
        }

        string alias = (usuario.NombreUsuario ?? "?").Trim();
        return (alias.Length > 1 ? alias.Substring(0, 2) : alias).ToUpperInvariant();
    }

    private static bool EsLaPaginaActual(string url, string actual)
    {
        return string.Equals(url, actual, StringComparison.OrdinalIgnoreCase);
    }

    /// Ruta de la pantalla actual en la barra superior: "Módulo › Pantalla".
    private static string Migas(string modulo, string pantalla)
    {
        if (string.IsNullOrEmpty(pantalla))
        {
            return string.Empty;
        }

        StringBuilder migas = new StringBuilder("<nav class=\"migas\" aria-label=\"Ubicación\"><ol>");
        if (!string.IsNullOrEmpty(modulo))
        {
            migas.Append("<li>").Append(HttpUtility.HtmlEncode(modulo)).Append("</li>");
        }
        migas.Append("<li aria-current=\"page\">").Append(HttpUtility.HtmlEncode(pantalla)).Append("</li>");
        migas.Append("</ol></nav>");
        return migas.ToString();
    }
}
