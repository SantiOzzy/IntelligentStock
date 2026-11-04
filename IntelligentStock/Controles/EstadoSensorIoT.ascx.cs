using System;
using System.Web.UI;
using BE;

// Componente personalizado EstadoSensorIoT.
//
// Muestra en formato de tarjeta los datos técnicos de un sensor: modelo,
// dirección MAC, temperatura límite y estado. Las páginas le pasan los datos
// que leyeron con BLLSensor, así la presentación queda separada del acceso a
// datos. Lo usan Consulta de sensores (CU-007-042) y Procesar alerta térmica
// (CU-007-043): si mañana se agrega un dato del sensor, se cambia solo acá.
// El estado se muestra con las etiquetas de estado del diseño (Site.css).
public partial class Controles_EstadoSensorIoT : UserControl
{
    public void CargarDatosSensor(string modelo, string macAddress, double temperaturaMaxima)
    {
        lblModelo.Text = string.IsNullOrEmpty(modelo) ? "(sin modelo)" : modelo;
        lblMacAddress.Text = macAddress;
        lblTempMaxima.Text = temperaturaMaxima.ToString("0.0") + " °C";
        lblEstado.Text = string.Empty;
        lblEstado.CssClass = string.Empty;
    }

    /// Carga todos los datos del sensor, incluido su estado y si tiene alertas
    /// sin resolver.
    public void CargarDatosSensor(BESensor sensor)
    {
        if (sensor == null)
        {
            Visible = false;
            return;
        }

        CargarDatosSensor(sensor.Modelo, sensor.MacAddress, sensor.TemperaturaMaxima);

        if (!sensor.Activo)
        {
            lblEstado.Text = "Inactivo";
            lblEstado.CssClass = "estado estado-neutro";
        }
        else if (sensor.AlertasAbiertas > 0)
        {
            lblEstado.Text = "Activo, con " + sensor.AlertasAbiertas +
                (sensor.AlertasAbiertas == 1 ? " alerta abierta" : " alertas abiertas");
            lblEstado.CssClass = "estado estado-aviso";
        }
        else
        {
            lblEstado.Text = "Activo, sin alertas abiertas";
            lblEstado.CssClass = "estado estado-exito";
        }

        Visible = true;
    }
}
