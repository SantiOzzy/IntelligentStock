using System;
using BE;
using BLL;
using Servicios;

// CU-007-039 Alta de sensor IoT.
// La BLL lleva la MAC al formato AA:BB:CC:DD:EE:FF, controla que no esté
// asignada a otro sensor activo (#ERR023), graba, recalcula el dígito
// verificador de la tabla Sensor y deja el evento en la bitácora.
public partial class AltaSensor : PaginaBase
{
    private BLLSensor bllSensor = new BLLSensor();

    protected override string PatenteRequerida
    {
        get { return "AltaSensor"; }
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

            bllSensor.Alta(sensor);

            Limpiar();
            MostrarMensaje(lblMensaje,
                "El sensor " + sensor.MacAddress + " se dio de alta correctamente.", false);
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
        txtMac.Text = string.Empty;
        txtModelo.Text = string.Empty;
        txtTemperatura.Text = string.Empty;
    }
}
