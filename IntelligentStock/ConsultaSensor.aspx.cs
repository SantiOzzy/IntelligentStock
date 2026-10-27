using System;
using System.Collections.Generic;
using BE;
using BLL;
using Servicios;

// CU-007-042 Consulta de sensores IoT.
// Al entrar se muestra el listado completo. Los filtros son opcionales y se
// combinan entre sí. Con "Ver" se muestra la ficha del sensor en el control
// EstadoSensorIoT. Es una consulta: no modifica ningún dato.
public partial class ConsultaSensor : PaginaBase
{
    private BLLSensor bllSensor = new BLLSensor();

    protected override string PatenteRequerida
    {
        get { return "ConsultaSensor"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            Buscar();
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        Buscar();
    }

    protected void gvDetalle_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);

        try
        {
            BESensor sensor = bllSensor.ObtenerPorId(Convert.ToInt32(gvDetalle.SelectedDataKey.Value));
            if (sensor == null)
            {
                ucEstadoSensor.Visible = false;
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            ucEstadoSensor.CargarDatosSensor(sensor);
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

    private void Buscar()
    {
        OcultarMensaje(lblMensaje);
        ucEstadoSensor.Visible = false;
        gvDetalle.SelectedIndex = -1;

        try
        {
            List<BESensor> sensores = bllSensor.Filtrar(txtBusqueda.Text, ddlEstado.SelectedValue);

            gvDetalle.DataSource = sensores;
            gvDetalle.DataBind();

            if (sensores.Count == 0)
            {
                MostrarError(lblMensaje, "ERR013");
                litResumen.Text = string.Empty;
            }
            else
            {
                litResumen.Text = Textos.Contar(sensores.Count, "sensor encontrado.", "sensores encontrados.");
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }
}
