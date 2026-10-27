using System;
using BE;
using BLL;
using Servicios;

// CU-004-022 Baja de proveedor.
// Se listan los proveedores activos. El usuario elige uno en la grilla, presiona
// "Dar de baja" y confirma. La BLL rechaza la baja si el proveedor tiene órdenes
// de compra pendientes (#ERR008).
public partial class BajaProveedor : PaginaBase
{
    private BLLProveedor bllProveedor = new BLLProveedor();

    protected override string PatenteRequerida
    {
        get { return "BajaProveedor"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarActivos();
        }
    }

    protected void gvDetalle_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        PrepararBoton();
    }

    protected void chkMostrarInactivos_CheckedChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarActivos();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (gvDetalle.SelectedIndex < 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione un proveedor de la grilla.", true);
            return;
        }

        string cuit = Convert.ToString(gvDetalle.SelectedDataKey.Values["Cuit"]);
        string nombre = gvDetalle.SelectedRow.Cells[1].Text;
        bool activo = Convert.ToBoolean(gvDetalle.SelectedDataKey.Values["Activo"]);

        try
        {
            if (activo)
            {
                bllProveedor.Baja(cuit);
            }
            else
            {
                bllProveedor.Reactivar(cuit);
            }

            CargarActivos();
            MostrarMensaje(lblMensaje, "El proveedor " + nombre + " (CUIT " + cuit + ") se " +
                (activo ? "dio de baja" : "reactivó") + " correctamente.", false);
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

    private void CargarActivos()
    {
        try
        {
            gvDetalle.SelectedIndex = -1;
            gvDetalle.DataSource = chkMostrarInactivos.Checked ? bllProveedor.TraerProveedores() : bllProveedor.TraerActivos();
            gvDetalle.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }

        PrepararBoton();
    }

    // El mismo botón da de baja o reactiva, según el estado del elegido.
    private void PrepararBoton()
    {
        bool hayElegido = gvDetalle.SelectedIndex >= 0;
        bool activo = !hayElegido || Convert.ToBoolean(gvDetalle.SelectedDataKey.Values["Activo"]);

        btnAceptar.Enabled = hayElegido;
        btnAceptar.Text = activo ? "Dar de baja" : "Reactivar";
        btnAceptar.CssClass = activo ? "btn btn-danger" : "btn btn-success";
        btnAceptar.OnClientClick = activo
            ? "return confirm('¿Confirma la baja del proveedor seleccionado?');"
            : "return confirm('¿Confirma la reactivación del proveedor seleccionado?');";
    }
}
