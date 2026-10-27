using System;
using System.IO;
using BLL;
using Servicios;

// CU-003-018 Restore de base de datos.
//
// Tambien es la tercera opcion de la pantalla de reparacion del digito
// verificador, por eso se puede entrar con la base marcada como inconsistente.
public partial class Restore : PaginaBase
{
    private BLLRespaldo bllRespaldo = new BLLRespaldo();
    private BLLUsuario bllUsuario = new BLLUsuario();
    private BLLEvento bllEvento = new BLLEvento();

    protected override bool PermitidoConBaseInconsistente
    {
        get { return true; }
    }

    // Con la base rota no se puede confiar en el arbol de permisos, asi que en
    // ese caso el control se hace contra el perfil dentro del Page_Load.
    protected override string PatenteRequerida
    {
        get { return SessionManager.BaseDeDatosInconsistente ? string.Empty : "Restore"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // Redirect(url, true) corta el pedido: con false la página seguía y el
        // botón Restaurar se podía disparar igual.
        if (SessionManager.BaseDeDatosInconsistente && !bllUsuario.EsAdministrador(UsuarioActual))
        {
            SessionManager.CerrarSesion();
            Response.Redirect("~/IniciarSesion.aspx", true);
        }
    }

    protected void btnRestaurar_Click(object sender, EventArgs e)
    {
        if (!fuArchivo.HasFile)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Seleccione el archivo de restauración."), true);
            return;
        }

        string extension = Path.GetExtension(fuArchivo.FileName);
        if (!EsExtensionValida(extension))
        {
            MostrarError(lblMensaje, "ERR016");
            return;
        }

        string rutaGuardado;
        try
        {
            rutaGuardado = GuardarArchivoSubido();
        }
        catch (Exception)
        {
            MostrarError(lblMensaje, "ERR015");
            return;
        }

        try
        {
            if (string.Equals(extension, ".bak", StringComparison.OrdinalIgnoreCase))
            {
                bllRespaldo.RestaurarDesdeBak(rutaGuardado);
            }
            else
            {
                bllRespaldo.RestaurarDesdeScript(rutaGuardado);
            }

            // Despues de restaurar, los digitos guardados corresponden al backup,
            // asi que se recalculan sobre los datos que quedaron. El evento lo
            // registra BLLRespaldo.
            bllRespaldo.RecalcularIntegridad();

            // La tabla Usuario pudo haber cambiado por completo: se corta la sesion.
            SessionManager.CerrarSesion();

            Response.Redirect("~/IniciarSesion.aspx?motivo=restore", false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (System.Threading.ThreadAbortException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, "No fue posible restaurar la base de datos. " + ex.Message, true);
        }
        finally
        {
            // El archivo subido tiene la base completa (incluidas las claves
            // encriptadas de los usuarios): no se deja guardado en el servidor.
            BorrarArchivoSubido(rutaGuardado);
        }
    }

    private static void BorrarArchivoSubido(string ruta)
    {
        try
        {
            if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }
        catch (Exception)
        {
            // Si el motor todavía lo tiene abierto queda para la próxima vez.
        }
    }

    protected void btnVolver_Click(object sender, EventArgs e)
    {
        string destino = SessionManager.BaseDeDatosInconsistente
            ? "~/ReparacionDV.aspx"
            : "~/IniciarSesion.aspx";

        Response.Redirect(destino, false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private static bool EsExtensionValida(string extension)
    {
        return string.Equals(extension, ".bak", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase);
    }

    // El archivo se deja en App_Data\Uploads. Ojo: para un .bak la cuenta del
    // servicio de SQL Server tiene que poder leer esa carpeta, porque el RESTORE
    // lo hace el motor y no la aplicacion.
    private string GuardarArchivoSubido()
    {
        string carpeta = Server.MapPath("~/App_Data/Uploads");

        if (!Directory.Exists(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        string ruta = Path.Combine(carpeta, Path.GetFileName(fuArchivo.FileName));
        fuArchivo.SaveAs(ruta);
        return ruta;
    }
}
