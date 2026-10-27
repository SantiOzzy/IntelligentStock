using System;
using BLL;
using Servicios;

// CU-003-013 Cambiar contraseña.
// Cada usuario cambia la suya, por eso la patente la tienen todos los perfiles.
public partial class CambiarContraseña : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();

    protected override string PatenteRequerida
    {
        get { return "CambiarContraseña"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            // La BLL vuelve a pedir la clave a la base en vez de confiar en la
            // que quedó cacheada en la sesión.
            bllUsuario.CambiarClave(UsuarioActual, txtActual.Text, txtNueva.Text, txtConfirmacion.Text);

            Limpiar();
            MostrarMensaje(lblMensaje, "La contraseña se cambió correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private void Limpiar()
    {
        txtActual.Text = string.Empty;
        txtNueva.Text = string.Empty;
        txtConfirmacion.Text = string.Empty;
    }
}
