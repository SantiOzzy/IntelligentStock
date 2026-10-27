using System;
using BE;
using BLL;
using Servicios;

// CU-004-020 Alta de proveedor.
// La BLL valida el CUIT (formato y dígito verificador), controla que no esté
// repetido ni el CUIT ni el correo, graba, recalcula el dígito verificador de
// la tabla Proveedor y deja el evento en la bitácora.
public partial class AltaProveedor : PaginaBase
{
    private BLLProveedor bllProveedor = new BLLProveedor();

    protected override string PatenteRequerida
    {
        get { return "AltaProveedor"; }
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
            BEProveedor proveedor = new BEProveedor(
                txtCuit.Text,
                txtNombre.Text,
                txtRazonSocial.Text,
                txtCorreo.Text,
                txtNumero.Text);

            bllProveedor.Alta(proveedor);

            Limpiar();
            MostrarMensaje(lblMensaje,
                "El proveedor " + proveedor.Nombre + " (CUIT " + proveedor.Cuit + ") se dio de alta correctamente.",
                false);
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
        txtCuit.Text = string.Empty;
        txtNombre.Text = string.Empty;
        txtRazonSocial.Text = string.Empty;
        txtCorreo.Text = string.Empty;
        txtNumero.Text = string.Empty;
    }
}
