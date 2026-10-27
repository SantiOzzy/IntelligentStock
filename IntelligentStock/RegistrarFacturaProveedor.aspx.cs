using System;
using System.Globalization;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-036 Registrar factura de proveedor.
//
// Se elige una orden Pendiente y se carga el comprobante de la factura. La
// orden pasa a "Recibida: <comprobante>" y lo pedido de cada insumo se suma al
// stock (R8), todo en una sola transacción. El comprobante no se puede repetir
// para el mismo proveedor (#ERR006). El importe es opcional y queda en la
// bitácora junto con la recepción.
public partial class RegistrarFacturaProveedor : PaginaBase
{
    private BLLOrdenCompra bllOrden = new BLLOrdenCompra();

    protected override string PatenteRequerida
    {
        get { return "RegistrarFacturaProveedor"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            txtFecha.Text = DateTime.Today.ToString("yyyy-MM-dd");
            CargarOrdenes();
        }
    }

    protected void ddlOrden_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        MostrarDetalle(true);
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        int idOrden;
        int.TryParse(ddlOrden.SelectedValue, out idOrden);

        DateTime? fecha = null;
        DateTime valor;
        if (DateTime.TryParseExact(txtFecha.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
        {
            fecha = valor;
        }
        else if (!string.IsNullOrEmpty(txtFecha.Text))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        decimal? importe = null;
        if (!string.IsNullOrEmpty(txtImporte.Text.Trim()))
        {
            decimal monto;
            if (!Numeros.TryParseDecimal(txtImporte.Text, out monto))
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "El importe tiene que ser un número."), true);
                return;
            }
            importe = monto;
        }

        try
        {
            BEOrdenCompra orden = bllOrden.RegistrarFactura(idOrden, txtComprobante.Text, fecha, importe);

            gvDetalle.DataSource = orden.Detalles;
            gvDetalle.DataBind();

            txtComprobante.Text = string.Empty;
            txtImporte.Text = string.Empty;
            CargarOrdenes();

            MostrarMensaje(lblMensaje,
                "Se registró el comprobante " + orden.DetalleEstado + " de la orden " + orden.Numero + " (" +
                orden.ProveedorTexto + "). La orden quedó Recibida y ingresaron al stock " +
                Textos.Contar(orden.Detalles.Count, "insumo", "insumos") + ".", false);
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

            if (ddlOrden.Items.Count == 1 && !lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR013", "No hay órdenes pendientes."), true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Muestra los insumos de la orden elegida y propone su importe estimado.
    private void MostrarDetalle(bool proponerImporte)
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
                    if (proponerImporte)
                    {
                        txtImporte.Text = Numeros.ParaInput(Math.Round(orden.ImporteEstimado, 2));
                    }
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
