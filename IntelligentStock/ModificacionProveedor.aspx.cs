using System;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-004-021 Modificación de proveedor.
// Al elegir el proveedor en la lista se cargan sus datos en los controles. El
// CUIT no se edita: es la clave del registro. Solo se listan los proveedores
// activos, porque los dados de baja ya no operan.
public partial class ModificacionProveedor : PaginaBase
{
    private BLLProveedor bllProveedor = new BLLProveedor();

    protected override string PatenteRequerida
    {
        get { return "ModificacionProveedor"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarProveedores();
            LimpiarDatos();
        }
    }

    protected void ddlProveedor_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarProveedorSeleccionado();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        string cuit = ddlProveedor.SelectedValue;
        if (string.IsNullOrEmpty(cuit))
        {
            MostrarMensaje(lblMensaje, "Seleccione un proveedor de la lista.", true);
            return;
        }

        try
        {
            BEProveedor proveedor = new BEProveedor(
                cuit,
                txtNombre.Text,
                txtRazonSocial.Text,
                txtCorreo.Text,
                txtNumero.Text);

            bllProveedor.Modificar(proveedor);

            // Se vuelve a armar la lista porque el nombre que muestra pudo cambiar.
            CargarProveedores();
            SeleccionarEnLista(cuit);
            CargarProveedorSeleccionado();

            MostrarMensaje(lblMensaje, "Los datos del proveedor se actualizaron correctamente.", false);
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

    private void CargarProveedores()
    {
        try
        {
            ddlProveedor.Items.Clear();
            ddlProveedor.Items.Add(new ListItem("(seleccione)", string.Empty));

            foreach (BEProveedor proveedor in bllProveedor.TraerActivos())
            {
                ddlProveedor.Items.Add(new ListItem(proveedor.Descripcion, proveedor.Cuit));
            }

            // Si no hay nada para elegir, se dice en la misma lista.
            if (ddlProveedor.Items.Count == 1)
            {
                ddlProveedor.Items[0].Text = "(no hay proveedores activos)";
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarProveedorSeleccionado()
    {
        string cuit = ddlProveedor.SelectedValue;

        if (string.IsNullOrEmpty(cuit))
        {
            LimpiarDatos();
            return;
        }

        try
        {
            BEProveedor proveedor = bllProveedor.ObtenerPorCuit(cuit);

            if (proveedor == null)
            {
                LimpiarDatos();
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            txtNombre.Text = proveedor.Nombre;
            txtRazonSocial.Text = proveedor.RazonSocial;
            txtCorreo.Text = proveedor.Correo;
            txtNumero.Text = proveedor.Numero;
            HabilitarEdicion(true);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // Hasta que no se elige un proveedor los campos quedan deshabilitados, para
    // que no se escriba sobre un formulario que todavía no tiene datos cargados.
    private void LimpiarDatos()
    {
        txtNombre.Text = string.Empty;
        txtRazonSocial.Text = string.Empty;
        txtCorreo.Text = string.Empty;
        txtNumero.Text = string.Empty;
        HabilitarEdicion(false);
    }

    private void HabilitarEdicion(bool habilitar)
    {
        txtNombre.Enabled = habilitar;
        txtRazonSocial.Enabled = habilitar;
        txtCorreo.Enabled = habilitar;
        txtNumero.Enabled = habilitar;
        btnAceptar.Enabled = habilitar;
    }

    private void SeleccionarEnLista(string cuit)
    {
        ListItem item = ddlProveedor.Items.FindByValue(cuit);
        if (item != null)
        {
            ddlProveedor.ClearSelection();
            item.Selected = true;
        }
    }
}
