using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using BLL;
using Servicios;

// CU-003-017 BackUp de base de datos.
//
// La carpeta de destino se escribe o se elige con Examinar, que abre un
// explorador de las carpetas del equipo donde corre el sistema (el navegador
// no puede abrir el explorador de Windows para elegir una carpeta: solo deja
// elegir archivos para subir). El archivo lo escribe SQL Server en esa carpeta.
public partial class BackupBaseDatos : PaginaBase
{
    private const string ClaveUltimoArchivo = "UltimoBackUpGenerado";

    private BLLRespaldo bllRespaldo = new BLLRespaldo();

    protected override string PatenteRequerida
    {
        get { return "BackUp"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // Se propone App_Data\Respaldos: la aplicación le da permiso a la
            // cuenta del servicio de SQL Server (que es quien escribe el archivo)
            // y después puede descargarlo.
            txtCarpeta.Text = bllRespaldo.RutaPorDefecto();
        }
    }

    #region Explorador de carpetas

    /// Carpeta que muestra el explorador ("" = discos y accesos directos).
    private string RutaExplorador
    {
        get { return ViewState["RutaExplorador"] as string ?? string.Empty; }
        set { ViewState["RutaExplorador"] = value; }
    }

    protected void btnExaminar_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        string actual = txtCarpeta.Text.Trim();
        MostrarCarpeta(!string.IsNullOrEmpty(actual) && Directory.Exists(actual) ? actual : string.Empty);
    }

    protected void rptCarpetas_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
        if (e.CommandName == "Abrir")
        {
            MostrarCarpeta(Convert.ToString(e.CommandArgument));
        }
    }

    protected void btnSubirCarpeta_Click(object sender, EventArgs e)
    {
        MostrarCarpeta(bllRespaldo.CarpetaSuperior(RutaExplorador) ?? string.Empty);
    }

    protected void btnInicioExplorador_Click(object sender, EventArgs e)
    {
        MostrarCarpeta(string.Empty);
    }

    protected void btnUsarCarpeta_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(RutaExplorador))
        {
            lblExplorador.Text = "Abra una carpeta (o un disco) para elegirla.";
            lblExplorador.Visible = true;
            return;
        }
        txtCarpeta.Text = RutaExplorador;
        pnlExplorador.Visible = false;
    }

    protected void btnCerrarExplorador_Click(object sender, EventArgs e)
    {
        pnlExplorador.Visible = false;
    }

    /// Muestra las subcarpetas de una carpeta del equipo (o, con "", los
    /// discos y los accesos directos).
    private void MostrarCarpeta(string ruta)
    {
        pnlExplorador.Visible = true;
        lblExplorador.Visible = false;
        try
        {
            List<CarpetaDelEquipo> carpetas = string.IsNullOrEmpty(ruta)
                ? bllRespaldo.CarpetasIniciales()
                : bllRespaldo.ListarCarpetas(ruta);
            RutaExplorador = ruta;
            lblRutaExplorador.Text = string.IsNullOrEmpty(ruta) ? "(discos y accesos directos)" : Server.HtmlEncode(ruta);
            btnSubirCarpeta.Enabled = !string.IsNullOrEmpty(ruta);
            btnUsarCarpeta.Enabled = !string.IsNullOrEmpty(ruta);
            rptCarpetas.DataSource = carpetas;
            rptCarpetas.DataBind();
            if (carpetas.Count == 0)
            {
                lblExplorador.Text = "Esta carpeta no tiene subcarpetas. Puede usarla tal cual.";
                lblExplorador.Visible = true;
            }
        }
        catch (Exception ex)
        {
            lblExplorador.Text = ex.Message;
            lblExplorador.Visible = true;
        }
    }

    #endregion

    protected void btnGenerar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        pnlExplorador.Visible = false;
        try
        {
            string ruta = bllRespaldo.RealizarBackUp(txtCarpeta.Text.Trim());

            // Se guarda para el boton de descarga; no conviene mandar la ruta al
            // cliente en un campo oculto.
            Session[ClaveUltimoArchivo] = ruta;
            btnDescargar.Visible = true;

            MostrarMensaje(lblMensaje,
                "Copia generada correctamente en " + ruta, false);
        }
        catch (Exception ex)
        {
            btnDescargar.Visible = false;
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnDescargar_Click(object sender, EventArgs e)
    {
        string ruta = Session[ClaveUltimoArchivo] as string;

        if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta))
        {
            MostrarMensaje(lblMensaje, "El archivo ya no está disponible en el servidor.", true);
            return;
        }

        try
        {
            Response.Clear();
            Response.ContentType = "application/octet-stream";
            Response.AppendHeader("Content-Disposition",
                "attachment; filename=" + Path.GetFileName(ruta));
            Response.TransmitFile(ruta);
            Response.End();
        }
        catch (System.Threading.ThreadAbortException)
        {
            // Response.End() siempre aborta el hilo: no es un error.
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // El archivo lo creó el motor y puede quedar con permisos que el
            // usuario del sitio no tiene.
            MostrarMensaje(lblMensaje,
                "El archivo se generó en " + ruta + " pero la aplicación no tiene permiso " +
                "para leerlo. Cópielo manualmente desde el servidor.", true);
        }
    }
}
