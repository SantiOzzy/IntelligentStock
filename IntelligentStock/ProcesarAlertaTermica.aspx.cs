using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-007-043 Procesar alerta térmica IoT.
//
// Las alertas las genera WebServiceTelemetria.asmx cuando un sensor informa una
// temperatura por encima de su máxima permitida. Acá el técnico elige una
// alerta abierta, ve la ficha del sensor (control EstadoSensorIoT) y el valor
// de la mercadería perecedera en riesgo (control ImpactoEconomico), y registra
// el nuevo estado con sus observaciones. "Exportar alertas" genera
// AlertasGeneradas.xml con XmlTextWriter y lo descarga.
public partial class ProcesarAlertaTermica : PaginaBase
{
    private BLLAlerta bllAlerta = new BLLAlerta();
    private BLLSensor bllSensor = new BLLSensor();

    protected override string PatenteRequerida
    {
        get { return "ProcesarAlertaTermica"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarAlertas();
            LimpiarDatos();
        }
    }

    protected void ddlAlerta_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarAlertaSeleccionada();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        int idAlerta = IdSeleccionado();
        if (idAlerta == 0)
        {
            MostrarMensaje(lblMensaje, "Elija la alerta a procesar.", true);
            return;
        }

        try
        {
            BEAlerta alerta = bllAlerta.Procesar(idAlerta, ddlEstado.SelectedValue, txtObservaciones.Text);

            CargarAlertas();

            if (alerta != null && alerta.Abierta)
            {
                // Sigue abierta (por ejemplo, pasó a En revisión): queda elegida.
                SeleccionarEnLista(idAlerta);
                CargarAlertaSeleccionada();
            }
            else
            {
                LimpiarDatos();
            }

            MostrarMensaje(lblMensaje, "La alerta N° " + idAlerta + " quedó en estado " +
                (alerta != null ? alerta.Estado : ddlEstado.SelectedValue) + ".", false);
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

    protected void btnExportar_Click(object sender, EventArgs e)
    {
        string ruta;
        try
        {
            ruta = bllAlerta.ExportarXml();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            return;
        }

        try
        {
            Response.Clear();
            Response.ContentType = "application/xml";
            Response.AppendHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(ruta));
            Response.TransmitFile(ruta);
            Response.End();
        }
        catch (System.Threading.ThreadAbortException)
        {
            // Response.End() siempre aborta el hilo: no es un error.
            throw;
        }
        catch (Exception)
        {
            // Se vuelve a la página normal: sin esto el navegador descargaría el
            // HTML como si fuera el XML.
            Response.ClearHeaders();
            Response.ClearContent();
            Response.ContentType = "text/html";
            MostrarMensaje(lblMensaje, "El archivo AlertasGeneradas.xml se generó en la carpeta App_Data " +
                "del sitio, pero no se pudo descargar. Intente nuevamente.", true);
        }
    }

    private void CargarAlertas()
    {
        try
        {
            List<BEAlerta> alertas = bllAlerta.TraerAlertas();

            ddlAlerta.Items.Clear();
            List<BEAlerta> abiertas = alertas.FindAll(delegate(BEAlerta a) { return a.Abierta; });
            ddlAlerta.Items.Add(new ListItem(
                abiertas.Count == 0 ? "(no hay alertas abiertas)" : "(seleccione)", "0"));

            foreach (BEAlerta alerta in abiertas)
            {
                ddlAlerta.Items.Add(new ListItem(alerta.Etiqueta, alerta.IdAlerta.ToString()));
            }

            gvDetalle.DataSource = alertas;
            gvDetalle.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarAlertaSeleccionada()
    {
        int idAlerta = IdSeleccionado();

        if (idAlerta == 0)
        {
            LimpiarDatos();
            return;
        }

        try
        {
            BEAlerta alerta = bllAlerta.ObtenerPorId(idAlerta);
            if (alerta == null)
            {
                LimpiarDatos();
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            ListItem estado = ddlEstado.Items.FindByValue(alerta.Estado);
            if (estado != null)
            {
                ddlEstado.ClearSelection();
                estado.Selected = true;
            }
            txtObservaciones.Text = alerta.Observaciones;

            ucEstadoSensor.CargarDatosSensor(bllSensor.ObtenerPorId(alerta.IdSensor));
            ucImpacto.MostrarMonto(bllAlerta.CalcularValorEnRiesgo(), true, "Stock perecedero valorizado");
            pnlAlerta.Visible = true;
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void LimpiarDatos()
    {
        ddlAlerta.ClearSelection();
        ddlEstado.ClearSelection();
        txtObservaciones.Text = string.Empty;
        pnlAlerta.Visible = false;
    }

    private int IdSeleccionado()
    {
        int id;
        return int.TryParse(ddlAlerta.SelectedValue, out id) ? id : 0;
    }

    private void SeleccionarEnLista(int idAlerta)
    {
        ListItem item = ddlAlerta.Items.FindByValue(idAlerta.ToString());
        if (item != null)
        {
            ddlAlerta.ClearSelection();
            item.Selected = true;
        }
    }
}
