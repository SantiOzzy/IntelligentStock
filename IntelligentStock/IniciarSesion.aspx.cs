using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using BE;
using BLL;
using Servicios;

// CU-001-001 Iniciar sesion.
//
// El formulario trabaja en cuatro modos porque el sitio no tiene un Default.aspx:
//   - sin sesion            -> muestra el formulario de usuario y clave
//   - con sesion            -> muestra el panel principal con las opciones que el rol permite
//   - "olvide mi clave"     -> pide el correo para mandar el enlace de recuperacion
//   - con "?token=" valido  -> permite definir la clave nueva sin pasar por un administrador
public partial class IniciarSesion : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();

    // Es la unica pantalla publica del sistema.
    protected override bool RequiereSesion
    {
        get { return false; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack)
        {
            return;
        }

        if (SessionManager.HaySesionActiva)
        {
            MostrarPanelPrincipal();
            AvisarSiVieneDeUnAccesoDenegado();
            return;
        }

        // El enlace que llega por correo apunta a esta misma pantalla con el
        // token en la direccion.
        string token = Request.QueryString["token"];
        if (!string.IsNullOrEmpty(token))
        {
            MostrarFormularioDeNuevaClave(token);
            return;
        }

        MostrarFormularioDeLogin();
    }

    protected void btnIngresar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            ResultadoLogin resultado = bllUsuario.Login(txtUsuario.Text, txtClave.Text);

            // La base quedo con el dígito verificador roto. Segun quien sea el que
            // esta entrando, se lo manda a repararla o se le corta el acceso.
            if (!resultado.BaseConsistente)
            {
                if (resultado.EsAdministrador)
                {
                    SessionManager.UsuarioActual = resultado.Usuario;
                    SessionManager.Inconsistencias = resultado.Inconsistencias;
                    Response.Redirect("~/ReparacionDV.aspx", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }

                SessionManager.CerrarSesion();
                txtClave.Text = string.Empty;
                MostrarMensaje(lblMensaje,
                    "El sistema no se encuentra disponible en este momento. " +
                    "Por favor avise al administrador del sistema. [#ERR007]", true);
                return;
            }

            SessionManager.UsuarioActual = resultado.Usuario;
            SessionManager.LimpiarInconsistencias();

            Response.Redirect("~/IniciarSesion.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            txtClave.Text = string.Empty;
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // ---------------- Recuperacion de contraseña (autoservicio) ----------------

    protected void lnkOlvideClave_Click(object sender, EventArgs e)
    {
        MostrarFormularioDeRecuperacion();
    }

    protected void lnkVolverAlLogin_Click(object sender, EventArgs e)
    {
        txtCorreoRecuperar.Text = string.Empty;
        MostrarFormularioDeLogin();
    }

    protected void btnEnviarEnlace_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            bllUsuario.SolicitarRecuperacionDeClave(txtCorreoRecuperar.Text, UrlDeEstaPantalla());

            // Siempre el mismo aviso, exista o no la cuenta: si el mensaje
            // cambiara, alcanzaria para averiguar que correos estan registrados.
            txtCorreoRecuperar.Text = string.Empty;
            MostrarFormularioDeLogin();
            MostrarMensaje(lblMensaje,
                "Si el correo ingresado corresponde a una cuenta del sistema, en unos minutos " +
                "va a recibir un enlace para definir una contraseña nueva. El enlace vence en " +
                BLLUsuario.MinutosVigenciaEnlaceRecuperacion + " minutos.", false);
        }
        catch (Exception ex)
        {
            MostrarFormularioDeRecuperacion();
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnGuardarClaveNueva_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            bllUsuario.RecuperarClaveConToken(hdnToken.Value, txtClaveNueva.Text, txtRepetirClaveNueva.Text);

            // Se vuelve a pedir la pantalla sin el token en la direccion, asi
            // un F5 no reintenta un enlace que ya quedo consumido.
            Response.Redirect("~/IniciarSesion.aspx?motivo=clave", false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            txtClaveNueva.Text = string.Empty;
            txtRepetirClaveNueva.Text = string.Empty;
            pnlLogin.Visible = false;
            pnlRecuperar.Visible = false;
            pnlNuevaClave.Visible = true;
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarFormularioDeRecuperacion()
    {
        pnlLogin.Visible = false;
        pnlPanel.Visible = false;
        pnlRecuperar.Visible = true;
        pnlNuevaClave.Visible = false;
        Page.Title = "Recuperar contraseña";
    }

    // Valida el token antes de mostrar los campos: si el enlace ya se uso o
    // vencio, no tiene sentido dejar escribir la clave nueva.
    private void MostrarFormularioDeNuevaClave(string token)
    {
        try
        {
            BEUsuario usuario = bllUsuario.ValidarTokenDeRecuperacion(token);

            pnlLogin.Visible = false;
            pnlPanel.Visible = false;
            pnlRecuperar.Visible = false;
            pnlNuevaClave.Visible = true;
            Page.Title = "Nueva contraseña";

            hdnToken.Value = token;
            litCuentaRecuperada.Text = "Está definiendo una contraseña nueva para la cuenta " +
                                       HttpUtility.HtmlEncode(usuario.NombreUsuario) + ".";
        }
        catch (Exception ex)
        {
            MostrarFormularioDeLogin();
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // Direccion absoluta de esta pantalla, sin parametros, para armar el enlace
    // que viaja por correo. Si está configurada en el Web.config se usa esa: la
    // que arma el pedido sale del encabezado Host, que lo puede mandar cualquiera,
    // y alguien podría hacer que el enlace apunte a un sitio suyo.
    private string UrlDeEstaPantalla()
    {
        string configurada = System.Configuration.ConfigurationManager.AppSettings["UrlInicioDeSesion"];
        if (!string.IsNullOrEmpty(configurada) && configurada.Trim().Length > 0)
        {
            return configurada.Trim();
        }
        return Request.Url.GetLeftPart(UriPartial.Path);
    }

    private void MostrarFormularioDeLogin()
    {
        pnlLogin.Visible = true;
        pnlPanel.Visible = false;
        pnlRecuperar.Visible = false;
        pnlNuevaClave.Visible = false;
        Page.Title = "Inicio de sesión";

        // PaginaBase manda para aca cuando pateo un acceso, y avisa el motivo por querystring.
        string motivo = Request.QueryString["motivo"];
        if (motivo == "sesion")
        {
            MostrarMensaje(lblMensaje,
                "La sesión expiró o todavía no inició sesión. Vuelva a ingresar sus credenciales.", true);
        }
        else if (motivo == "clave")
        {
            MostrarMensaje(lblMensaje,
                "La contraseña se cambió correctamente. Ya puede iniciar sesión con la contraseña nueva.", false);
        }
        else if (motivo == "restore")
        {
            MostrarMensaje(lblMensaje,
                "La base de datos se restauró correctamente y se recalcularon los dígitos verificadores. " +
                "Vuelva a iniciar sesión.", false);
        }
        else if (motivo == "cuenta")
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR004",
                "Su sesión se cerró porque la cuenta fue dada de baja."), true);
        }
        else if (motivo == "bloqueo")
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR004",
                "Su sesión se cerró porque un administrador del sistema bloqueó la cuenta."), true);
        }
    }

    private void MostrarPanelPrincipal()
    {
        BEUsuario usuario = UsuarioActual;

        pnlLogin.Visible = false;
        pnlRecuperar.Visible = false;
        pnlNuevaClave.Visible = false;
        pnlPanel.Visible = true;
        Page.Title = "Panel principal";

        litBienvenida.Text = "Bienvenido, " + HttpUtility.HtmlEncode(usuario.Nombre) +
                             " (" + HttpUtility.HtmlEncode(usuario.NombreRol) + ")";

        List<ModuloMenu> modulos = MenuSistema.ObtenerMenuDe(usuario);

        if (modulos.Count == 0)
        {
            litOpciones.Text =
                "<div class=\"alert alert-warning\">Su perfil no tiene ninguna funcionalidad habilitada. " +
                "Comuníquese con el administrador del sistema. [#ERR009]</div>";
            return;
        }

        litOpciones.Text = ArmarTarjetas(modulos);
    }

    // Dibuja un panel por modulo con la lista de opciones adentro. Sale de la
    // misma definicion que usa la barra de navegacion, asi no se duplica el menu.
    private string ArmarTarjetas(List<ModuloMenu> modulos)
    {
        StringBuilder html = new StringBuilder();
        html.Append("<div class=\"row\">");

        foreach (ModuloMenu modulo in modulos)
        {
            html.Append("<div class=\"col-md-4\">");
            html.Append("<div class=\"panel panel-default\">");
            html.Append("<div class=\"panel-heading\"><strong>");
            html.Append(HttpUtility.HtmlEncode(modulo.Titulo));
            html.Append("</strong></div>");
            html.Append("<ul class=\"list-group\">");

            foreach (OpcionMenu opcion in modulo.Opciones)
            {
                html.Append("<li class=\"list-group-item\"><a href=\"");
                html.Append(ResolveUrl(opcion.Url));
                html.Append("\">");
                html.Append(HttpUtility.HtmlEncode(opcion.Texto));
                html.Append("</a></li>");
            }

            html.Append("</ul></div></div>");
        }

        html.Append("</div>");
        return html.ToString();
    }

    private void AvisarSiVieneDeUnAccesoDenegado()
    {
        if (Request.QueryString["motivo"] != "permiso")
        {
            return;
        }

        string patente = Request.QueryString["f"];
        string detalle = string.IsNullOrEmpty(patente)
            ? string.Empty
            : "Funcionalidad: " + MenuSistema.DescribirPatente(patente) + ".";

        MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR009", detalle), true);
    }
}
