using System;
using System.Collections.Generic;
using BE;
using BLL;
using Servicios;

// CU-003-019 Reparar dígito verificador.
//
// Se llega a esta pantalla de dos maneras:
//   1) el login detecto una inconsistencia y el que entro es el WebMaster
//   2) el WebMaster la abre desde el menu para verificar la integridad a mano
public partial class ReparacionDV : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();
    private BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
    private BLLEvento bllEvento = new BLLEvento();

    // Es la unica pantalla a la que se puede entrar con la base marcada como rota.
    protected override bool PermitidoConBaseInconsistente
    {
        get { return true; }
    }

    // El permiso no se controla por patente sino contra el perfil, porque si las
    // tablas de seguridad son justamente las alteradas el arbol de permisos no es confiable.
    protected override string PatenteRequerida
    {
        get { return string.Empty; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // En los dos casos se corta el pedido con Redirect(url, true): con
        // Redirect(url, false) la página seguía ejecutándose y un usuario común
        // podía disparar igual el botón Recalcular armando el pedido a mano.
        if (!bllUsuario.EsAdministrador(UsuarioActual))
        {
            // Un usuario comun no puede ver el detalle de la base: se lo saca.
            SessionManager.CerrarSesion();
            Response.Redirect("~/IniciarSesion.aspx", true);
            return;
        }

        // A esta pantalla se llega únicamente cuando el inicio de sesión detectó
        // una inconsistencia. No figura en el menú, y si alguien escribe la URL a
        // mano se lo devuelve al panel principal.
        if (!SessionManager.BaseDeDatosInconsistente && !IsPostBack)
        {
            Response.Redirect("~/IniciarSesion.aspx", true);
            return;
        }

        if (!IsPostBack)
        {
            CargarInconsistencias();
        }
    }

    private void CargarInconsistencias()
    {
        List<Inconsistencia> inconsistencias =
            SessionManager.Inconsistencias ?? new List<Inconsistencia>();

        gvInconsistencias.DataSource = inconsistencias;
        gvInconsistencias.DataBind();

        MostrarMensaje(lblMensaje,
            "Se encontraron " + inconsistencias.Count +
            (inconsistencias.Count == 1 ? " inconsistencia. " : " inconsistencias. ") +
            AdministradorErrores.Mensaje("ERR007"), true);
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        // Si la base sigue marcada como rota hay que cortar la sesion; si no,
        // se vuelve al panel como en cualquier otra pantalla.
        if (SessionManager.BaseDeDatosInconsistente)
        {
            bllEvento.RegistrarDelUsuarioActual("Reparación de dígito verificador",
                "El administrador canceló la reparación y cerró la sesión.");
            SessionManager.CerrarSesion();
        }

        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    protected void btnRecalcular_Click(object sender, EventArgs e)
    {
        // Se vuelve a controlar acá: recalcular acepta como buenos los datos
        // actuales, así que solo lo puede hacer el administrador.
        if (!bllUsuario.EsAdministrador(UsuarioActual))
        {
            MostrarError(lblMensaje, "ERR009");
            return;
        }

        try
        {
            bllDV.RecalcularBaseDeDatos();

            bllEvento.RegistrarDelUsuarioActual("Recálculo de dígito verificador",
                "El administrador recalculó los dígitos verificadores ignorando la inconsistencia detectada.");

            // Con los digitos al dia la marca de la sesion ya no corresponde.
            SessionManager.LimpiarInconsistencias();

            gvInconsistencias.DataSource = new List<Inconsistencia>();
            gvInconsistencias.DataBind();
            btnRecalcular.Visible = false;
            btnRestore.Visible = false;
            btnCancelar.Text = "Continuar al panel principal";

            MostrarMensaje(lblMensaje,
                "Se recalcularon los dígitos verificadores. El sistema vuelve a funcionar con normalidad.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnRestore_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Restore.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }
}
