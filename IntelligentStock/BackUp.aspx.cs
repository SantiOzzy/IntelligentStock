using System;
using System.IO;
using BLL;
using Servicios;

// CU-003-017 BackUp de base de datos.
public partial class BackUp : PaginaBase
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
            // Se propone la carpeta de backups del propio motor: el archivo lo
            // escribe SQL Server y sobre esa ruta ya tiene permisos.
            txtCarpeta.Text = bllRespaldo.RutaPorDefecto(Server.MapPath("~/App_Data/Backups"));
        }
    }

    protected void btnGenerar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

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
