using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-008-044 Alta de suscripción.
// La BLL controla que no haya otra suscripción activa (#ERR021), cobra el
// primer mes en la pasarela de cobros (si la rechaza: #ERR024), guarda el
// token encriptado con AES-256 (R6), recalcula el dígito verificador de la
// tabla Suscripcion y deja el evento en la bitácora.
public partial class AltaSuscripcion : PaginaBase
{
    private BLLSuscripcion bllSuscripcion = new BLLSuscripcion();

    protected override string PatenteRequerida
    {
        get { return "AltaSuscripcion"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarPlanes();
            txtFechaInicio.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        // El navegador manda la fecha como aaaa-mm-dd, sin importar el idioma.
        DateTime? fechaInicio = null;
        if (txtFechaInicio.Text.Trim().Length > 0)
        {
            DateTime fecha;
            if (!DateTime.TryParseExact(txtFechaInicio.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out fecha))
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "La fecha de inicio no es válida."), true);
                return;
            }
            fechaInicio = fecha;
        }

        try
        {
            BESuscripcion suscripcion = bllSuscripcion.Alta(ddlPlan.SelectedValue, txtToken.Text, fechaInicio);

            txtToken.Text = string.Empty;
            txtFechaInicio.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            MostrarMensaje(lblMensaje,
                "La suscripción al plan " + suscripcion.TipoPlan + " (" + suscripcion.AbonoTexto +
                " por mes) quedó activa desde el " + suscripcion.FechaInicioTexto +
                ". Código de transacción de la pasarela: " + suscripcion.CodigoTransaccion + ".", false);
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

    private void CargarPlanes()
    {
        ddlPlan.Items.Clear();
        foreach (KeyValuePair<string, decimal> plan in BLLSuscripcion.TraerPlanes())
        {
            ddlPlan.Items.Add(new ListItem(plan.Key + " (ARS " + plan.Value.ToString("N0") + " por mes)", plan.Key));
        }
    }
}
