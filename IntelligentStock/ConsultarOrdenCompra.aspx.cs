using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
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
        if (!IsValid)
        {
            return;
        }

        OcultarMensaje(lblMensaje);
        pnlItems.Visible = false;
        gvDetalle.SelectedIndex = -1;
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

            gvDetalle.SelectedIndex = fila;
            litTituloOrden.Text = HttpUtility.HtmlEncode("Detalle de la orden " + orden.Numero);
            litDatosOrden.Text = HttpUtility.HtmlEncode(orden.FechaTexto + " · " + orden.ProveedorTexto + " · " + orden.Estado +
                                                        " · importe estimado " + orden.ImporteEstimado.ToString("C"));
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
            MostrarIndicadores(ordenes);

            if (IsPostBack)
            {
                if (ordenes.Count == 0)
                {
                    MostrarError(lblMensaje, "ERR013");
                }
                else
                {
                    // Paso 6 del caso de uso: se informa la cantidad de registros obtenidos.
                    MostrarMensaje(lblMensaje, "Se " + (ordenes.Count == 1 ? "encontró " : "encontraron ") +
                        Textos.Contar(ordenes.Count, "orden de compra", "órdenes de compra") + ".", false);
                }
            }
        }
        catch (Exception ex)
        {
            gvDetalle.DataSource = null;
            gvDetalle.DataBind();
            litIndicadores.Text = string.Empty;
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Cuántas órdenes hay en el resultado y cuántas de cada estado.
    private void MostrarIndicadores(List<BEOrdenCompra> ordenes)
    {
        int pendientes = 0, recibidas = 0, anuladas = 0;
        foreach (BEOrdenCompra orden in ordenes)
        {
            switch (orden.EstadoBase)
            {
                case BEOrdenCompra.EstadoPendiente: pendientes++; break;
                case BEOrdenCompra.EstadoRecibida: recibidas++; break;
                default: anuladas++; break;
            }
        }

        litIndicadores.Text = new Indicadores()
            .Agregar("Órdenes encontradas", ordenes.Count.ToString(), "destacado")
            .Agregar("Pendientes", pendientes.ToString(), pendientes > 0 ? "aviso" : null)
            .Agregar("Recibidas", recibidas.ToString(), recibidas > 0 ? "exito" : null)
            .Agregar("Anuladas", anuladas.ToString())
            .ToString();
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
