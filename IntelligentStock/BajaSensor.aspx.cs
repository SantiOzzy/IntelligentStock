using System;
using BE;
using BLL;
using Servicios;

// CU-007-041 Baja de sensor IoT.
// Se listan los sensores activos. El usuario elige uno en la grilla, presiona
// "Dar de baja" y confirma. La BLL rechaza la baja si el sensor tiene alertas
// térmicas sin resolver (#ERR008).
public partial class BajaSensor : PaginaBase
{
    private BLLSensor bllSensor = new BLLSensor();

    protected override string PatenteRequerida
    {
        get { return "BajaSensor"; }
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
            MostrarMensaje(lblMensaje, "Seleccione un sensor de la grilla.", true);
            return;
        }

        int idSensor = Convert.ToInt32(gvDetalle.SelectedDataKey.Values["IdSensor"]);
        string mac = gvDetalle.SelectedRow.Cells[1].Text;

        try
        {
            bool activo = Convert.ToBoolean(gvDetalle.SelectedDataKey.Values["Activo"]);
            if (activo)
            {
                bllSensor.Baja(idSensor);
            }
            else
            {
                bllSensor.Reactivar(idSensor);
            }

            CargarActivos();
            MostrarMensaje(lblMensaje, "El sensor " + mac + " se " +
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
            gvDetalle.DataSource = chkMostrarInactivos.Checked ? bllSensor.TraerSensores() : bllSensor.TraerActivos();
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
            ? "return confirm('¿Confirma la baja del sensor seleccionado?');"
            : "return confirm('¿Confirma la reactivación del sensor seleccionado?');";
    }
}
