using System;
using BE;
using BLL;
using Servicios;

// CU-005-026 Baja de insumo.
// Se listan los insumos activos. El usuario elige uno en la grilla, presiona
// "Dar de baja" y confirma. La BLL rechaza la baja si el insumo forma parte de
// una receta activa o de una orden de compra pendiente (#ERR008).
public partial class BajaInsumo : PaginaBase
{
    private BLLInsumo bllInsumo = new BLLInsumo();

    protected override string PatenteRequerida
    {
        get { return "BajaInsumo"; }
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
            MostrarMensaje(lblMensaje, "Seleccione un insumo de la grilla.", true);
            return;
        }

        int idInsumo = Convert.ToInt32(gvDetalle.SelectedDataKey.Values["IdInsumo"]);
        string nombre = gvDetalle.SelectedRow.Cells[1].Text;

        try
        {
            bool activo = Convert.ToBoolean(gvDetalle.SelectedDataKey.Values["Activo"]);
            if (activo)
            {
                bllInsumo.Baja(idInsumo);
            }
            else
            {
                bllInsumo.Reactivar(idInsumo);
            }

            CargarActivos();
            MostrarMensaje(lblMensaje, "El insumo " + nombre + " se " +
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
            gvDetalle.DataSource = chkMostrarInactivos.Checked ? bllInsumo.TraerInsumos() : bllInsumo.TraerActivos();
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
            ? "return confirm('¿Confirma la baja del insumo seleccionado?');"
            : "return confirm('¿Confirma la reactivación del insumo seleccionado?');";
    }
}
