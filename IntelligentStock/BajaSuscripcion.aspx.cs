using System;
using BE;
using BLL;
using Servicios;

// CU-008-045 Baja de suscripción.
// Se listan todas las suscripciones: las activas se pueden elegir para darlas
// de baja y las canceladas quedan a la vista con la fecha en que termina el
// servicio ya pagado. El usuario elige una activa, presiona "Dar de baja" y
// confirma. La BLL desencripta el token (R7), le pide a la pasarela que
// detenga los cobros y deja la suscripción como Cancelada (baja lógica). El
// servicio sigue hasta el cierre del período en curso.
public partial class BajaSuscripcion : PaginaBase
{
    private BLLSuscripcion bllSuscripcion = new BLLSuscripcion();

    protected override string PatenteRequerida
    {
        get { return "BajaSuscripcion"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarSuscripciones();
        }
    }

    protected void gvDetalle_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        btnAceptar.Enabled = gvDetalle.SelectedIndex >= 0;
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (gvDetalle.SelectedIndex < 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione una suscripción de la grilla.", true);
            return;
        }

        int idSuscripcion = Convert.ToInt32(gvDetalle.SelectedDataKey.Value);

        try
        {
            bool pasarelaNotificada;
            DateTime cierre = bllSuscripcion.Baja(idSuscripcion, out pasarelaNotificada);

            CargarSuscripciones();

            string mensaje = "La suscripción N° " + idSuscripcion + " se dio de baja correctamente. " +
                             "El servicio sigue habilitado hasta el " + cierre.ToString("dd/MM/yyyy") + ".";
            if (pasarelaNotificada)
            {
                mensaje += " Se detuvieron los cobros futuros.";
            }
            else
            {
                mensaje += " Atención: no se pudo avisar a la pasarela de cobros (no se pudo leer el token " +
                           "de pago). Solicite la baja de los cobros directamente a la pasarela.";
            }

            MostrarMensaje(lblMensaje, mensaje, false);
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

    private void CargarSuscripciones()
    {
        try
        {
            gvDetalle.SelectedIndex = -1;
            gvDetalle.DataSource = bllSuscripcion.TraerSuscripciones();
            gvDetalle.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }

        // Hasta que no se elige una suscripción no hay nada para dar de baja.
        btnAceptar.Enabled = false;
    }
}
