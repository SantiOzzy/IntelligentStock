using System;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-007-040 Modificación de sensor IoT.
// Al elegir el sensor en la lista se cargan sus datos en los controles. Solo se
// listan los sensores activos. La MAC se puede cambiar (por ejemplo, si se
// reemplazó el equipo), siempre que no la tenga otro sensor activo (#ERR023).
public partial class ModificacionSensor : PaginaBase
{
    private BLLSensor bllSensor = new BLLSensor();

    protected override string PatenteRequerida
    {
        get { return "ModificacionSensor"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarSensores();
            LimpiarDatos();
        }
    }

    protected void ddlSensor_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarSensorSeleccionado();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        int idSensor = IdSeleccionado();
        if (idSensor == 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione un sensor de la lista.", true);
            return;
        }

        double temperatura;
        if (!Numeros.TryParseDouble(txtTemperatura.Text, out temperatura))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                "La temperatura máxima permitida tiene que ser un número."), true);
            return;
        }

        try
        {
            BESensor sensor = new BESensor(txtMac.Text, txtModelo.Text, temperatura);
            sensor.IdSensor = idSensor;

            bllSensor.Modificar(sensor);

            // Se vuelve a armar la lista porque el texto que muestra pudo cambiar.
            CargarSensores();
            SeleccionarEnLista(idSensor);
            CargarSensorSeleccionado();

            MostrarMensaje(lblMensaje, "Los datos del sensor se actualizaron correctamente.", false);
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

    private void CargarSensores()
    {
        try
        {
            ddlSensor.Items.Clear();
            ddlSensor.Items.Add(new ListItem("(seleccione)", "0"));

            foreach (BESensor sensor in bllSensor.TraerActivos())
            {
                ddlSensor.Items.Add(new ListItem(sensor.Etiqueta, sensor.IdSensor.ToString()));
            }

            // Si no hay nada para elegir, se dice en la misma lista.
            if (ddlSensor.Items.Count == 1)
            {
                ddlSensor.Items[0].Text = "(no hay sensores activos)";
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarSensorSeleccionado()
    {
        int idSensor = IdSeleccionado();

        if (idSensor == 0)
        {
            LimpiarDatos();
            return;
        }

        try
        {
            BESensor sensor = bllSensor.ObtenerPorId(idSensor);

            if (sensor == null)
            {
                LimpiarDatos();
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            txtMac.Text = sensor.MacAddress;
            txtModelo.Text = sensor.Modelo;
            txtTemperatura.Text = Numeros.ParaInput(sensor.TemperaturaMaxima);
            HabilitarEdicion(true);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // Hasta que no se elige un sensor los campos quedan deshabilitados.
    private void LimpiarDatos()
    {
        txtMac.Text = string.Empty;
        txtModelo.Text = string.Empty;
        txtTemperatura.Text = string.Empty;
        HabilitarEdicion(false);
    }

    private void HabilitarEdicion(bool habilitar)
    {
        txtMac.Enabled = habilitar;
        txtModelo.Enabled = habilitar;
        txtTemperatura.Enabled = habilitar;
        btnAceptar.Enabled = habilitar;
    }

    private int IdSeleccionado()
    {
        int id;
        return int.TryParse(ddlSensor.SelectedValue, out id) ? id : 0;
    }

    private void SeleccionarEnLista(int idSensor)
    {
        ListItem item = ddlSensor.Items.FindByValue(idSensor.ToString());
        if (item != null)
        {
            ddlSensor.ClearSelection();
            item.Selected = true;
        }
    }
}
