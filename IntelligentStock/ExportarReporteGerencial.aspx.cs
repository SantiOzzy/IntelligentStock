using System;
using System.Globalization;
using System.IO;
using BLL;
using Servicios;

// CU-006-038 Exportar reporte gerencial.
//
// Se elige el reporte, el período y el formato (PDF, Excel o CSV). La BLL arma
// los datos (BLLReporte), el servicio ExportadorReporte genera el archivo, se
// guarda en App_Data\Reportes y se descarga. Si no hay datos en el período se
// informa #ERR013; si no se puede escribir la carpeta, #ERR022.
public partial class ExportarReporteGerencial : PaginaBase
{
    private BLLReporte bllReporte = new BLLReporte();

    protected override string PatenteRequerida
    {
        get { return "ExportarReporteGerencial"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            txtDesde.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
            txtHasta.Text = DateTime.Today.ToString("yyyy-MM-dd");
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        DateTime? desde = LeerFecha(txtDesde.Text);
        DateTime? hasta = LeerFecha(txtHasta.Text);
        if ((!string.IsNullOrEmpty(txtDesde.Text) && !desde.HasValue) ||
            (!string.IsNullOrEmpty(txtHasta.Text) && !hasta.HasValue))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "Alguna de las fechas no es válida."), true);
            return;
        }

        string ruta;
        string tipoMime;
        try
        {
            ruta = bllReporte.Exportar(ddlReporte.SelectedValue, desde, hasta, ddlFormato.SelectedValue, out tipoMime);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            return;
        }

        try
        {
            Response.Clear();
            Response.ContentType = tipoMime;
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
            // Se vuelve a la página normal: sin esto el navegador descargaría
            // el HTML como si fuera el reporte.
            Response.ClearHeaders();
            Response.ClearContent();
            Response.ContentType = "text/html";
            MostrarMensaje(lblMensaje, "El reporte se generó en " + ruta + ", pero no se pudo descargar. " +
                "Intente nuevamente.", true);
        }
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private static DateTime? LeerFecha(string texto)
    {
        DateTime fecha;
        if (DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
        {
            return fecha;
        }
        return null;
    }
}
