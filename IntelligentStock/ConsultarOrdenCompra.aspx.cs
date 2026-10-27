using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-034 Consultar orden de compra.
//
// Lista las órdenes de compra filtrando por proveedor, estado y rango de
// fechas. "Ver detalle" muestra los insumos de la orden con su importe
// estimado (cantidad por el precio unitario actual del insumo).
public partial class ConsultarOrdenCompra : PaginaBase
{
    private BLLOrdenCompra bllOrden = new BLLOrdenCompra();
    private BLLProveedor bllProveedor = new BLLProveedor();

    protected override string PatenteRequerida
    {
        get { return "ConsultarOrdenCompra"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarProveedores();
            Buscar();
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        pnlItems.Visible = false;
        Buscar();
    }

    protected void gvDetalle_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName != "Ver")
        {
            return;
        }

        int fila;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out fila) || fila < 0 || fila >= gvDetalle.DataKeys.Count)
        {
            return;
        }

        int idOrden = (int)gvDetalle.DataKeys[fila].Value;
        try
        {
            BEOrdenCompra orden = bllOrden.ObtenerPorId(idOrden);
            if (orden == null)
            {
                MostrarError(lblMensaje, "ERR013");
                pnlItems.Visible = false;
                return;
            }

            lblOrden.Text = "Orden " + orden.Numero + " del " + orden.FechaTexto + " - " + orden.ProveedorTexto +
                            " - " + orden.Estado + ". Importe estimado: " + orden.ImporteEstimado.ToString("C") + ".";
            gvItems.DataSource = orden.Detalles;
            gvItems.DataBind();
            pnlItems.Visible = true;
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

    private void CargarProveedores()
    {
        try
        {
            // Se listan también los dados de baja: pueden tener órdenes viejas.
            foreach (BEProveedor proveedor in bllProveedor.TraerProveedores())
            {
                ddlProveedor.Items.Add(new ListItem(proveedor.Descripcion, proveedor.Cuit));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void Buscar()
    {
        DateTime? desde;
        DateTime? hasta;
        if (!LeerFecha(txtDesde.Text, out desde) || !LeerFecha(txtHasta.Text, out hasta))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "Alguna de las fechas no es válida."), true);
            return;
        }

        try
        {
            List<BEOrdenCompra> ordenes = bllOrden.Filtrar(ddlProveedor.SelectedValue, ddlEstado.SelectedValue, desde, hasta);
            gvDetalle.DataSource = ordenes;
            gvDetalle.DataBind();

            if (ordenes.Count == 0 && IsPostBack)
            {
                MostrarError(lblMensaje, "ERR013");
            }
        }
        catch (Exception ex)
        {
            gvDetalle.DataSource = null;
            gvDetalle.DataBind();
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Fecha vacía: sin filtro. Devuelve false si el texto no es una fecha.
    private static bool LeerFecha(string texto, out DateTime? fecha)
    {
        fecha = null;
        if (string.IsNullOrEmpty(texto))
        {
            return true;
        }

        DateTime valor;
        if (!DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
        {
            return false;
        }
        fecha = valor;
        return true;
    }
}
