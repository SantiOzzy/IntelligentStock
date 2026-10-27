using System;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-035 Anular orden de compra.
//
// Solo se anulan órdenes Pendientes. La orden queda en estado
// "Anulada: <motivo>" (el motivo completo va a la bitácora) y deja de contar
// como pedido pendiente para la próxima orden predictiva.
public partial class AnularOrdenCompra : PaginaBase
{
    private BLLOrdenCompra bllOrden = new BLLOrdenCompra();

    protected override string PatenteRequerida
    {
        get { return "AnularOrdenCompra"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarOrdenes();
        }
    }

    protected void ddlOrden_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        MostrarDetalle();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        int idOrden;
        int.TryParse(ddlOrden.SelectedValue, out idOrden);

        try
        {
            string etiqueta = ddlOrden.SelectedItem.Text;
            bllOrden.Anular(idOrden, txtMotivo.Text);

            txtMotivo.Text = string.Empty;
            CargarOrdenes();
            MostrarDetalle();
            MostrarMensaje(lblMensaje, "Se anuló la orden " + etiqueta + ".", false);
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

    private void CargarOrdenes()
    {
        ddlOrden.Items.Clear();
        ddlOrden.Items.Add(new ListItem("(seleccione)", string.Empty));

        try
        {
            foreach (BEOrdenCompra orden in bllOrden.TraerPendientes())
            {
                ddlOrden.Items.Add(new ListItem(orden.Etiqueta, orden.IdOrdenCompra.ToString()));
            }

            if (ddlOrden.Items.Count == 1)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR013", "No hay órdenes pendientes."), true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarDetalle()
    {
        gvDetalle.DataSource = null;

        int idOrden;
        if (int.TryParse(ddlOrden.SelectedValue, out idOrden) && idOrden > 0)
        {
            try
            {
                BEOrdenCompra orden = bllOrden.ObtenerPorId(idOrden);
                if (orden != null)
                {
                    gvDetalle.DataSource = orden.Detalles;
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje(lblMensaje, ex.Message, true);
            }
        }

        gvDetalle.DataBind();
    }
}
