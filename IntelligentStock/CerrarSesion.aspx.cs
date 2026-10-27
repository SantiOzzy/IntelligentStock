using System;
using System.Web;
using BE;
using BLL;
using Servicios;

// CU-001-002 Cerrar sesion.
// Antes de cerrar se pide confirmacion, tal como esta descripto en el caso de uso.
public partial class CerrarSesion : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();

    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack)
        {
            return;
        }

        BEUsuario usuario = UsuarioActual;
        litConfirmacion.Text = "¿Confirma que desea cerrar la sesión de <strong>" +
                               HttpUtility.HtmlEncode(usuario.NombreUsuario) + "</strong>?";
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        // El Logout deja el evento en la bitacora y despues limpia la sesion.
        bllUsuario.Logout(UsuarioActual);

        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }
}
