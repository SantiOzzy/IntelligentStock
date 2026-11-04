using System;
using System.Collections.Generic;
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
// para el mismo proveedor (#ERR006). El importe total de la factura queda en la
// bitácora junto con la recepción.
public partial class RegistrarFacturaProveedor : PaginaBase
{
    private BLLOrdenCompra bllOrden = new BLLOrdenCompra();
    private BLLInsumo bllInsumo = new BLLInsumo();

    protected override string PatenteRequerida
    {
        get { return "RegistrarFacturaProveedor"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // Que no sea anterior a la fecha de la orden lo controla la BLL.
        ValidadorDeRango.CargarFechas(rvFecha, new DateTime(2000, 1, 1), DateTime.Today);

        if (!IsPostBack)
        {
            txtFecha.Text = DateTime.Today.ToString("yyyy-MM-dd");
            CargarOrdenes();
            MostrarDetalle(null);
        }
    }

    protected void ddlOrden_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        MostrarDetalle(ObtenerOrdenElegida(), true);
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        int idOrden;
        int.TryParse(ddlOrden.SelectedValue, out idOrden);

        if (idOrden <= 0 || string.IsNullOrEmpty(txtComprobante.Text.Trim()) ||
            string.IsNullOrEmpty(txtFecha.Text.Trim()) || string.IsNullOrEmpty(txtImporte.Text.Trim()))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002"), true);
            return;
        }

        DateTime fecha;
        if (!DateTime.TryParseExact(txtFecha.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        decimal importe;
        if (!Numeros.TryParseDecimal(txtImporte.Text, out importe))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "El importe tiene que ser un número."), true);
            return;
        }

        try
        {
            // Lo que había antes de recibir, para mostrar cómo quedó cada insumo.
            BEOrdenCompra antes = bllOrden.ObtenerPorId(idOrden);
            Dictionary<int, decimal> stockAntes = antes == null ? new Dictionary<int, decimal>() : StockDe(antes);

            BEOrdenCompra orden = bllOrden.RegistrarFactura(idOrden, txtComprobante.Text, fecha, importe);

            List<object> filas = new List<object>();
            foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
            {
                decimal previo;
                stockAntes.TryGetValue(detalle.IdInsumo, out previo);
                BEInsumo insumo = bllInsumo.ObtenerPorId(detalle.IdInsumo);
                filas.Add(Fila(detalle, previo, insumo == null ? previo + detalle.Cantidad : insumo.Stock));
            }
            gvDetalle.DataSource = filas;
            gvDetalle.DataBind();

            litIndicadores.Text = new Indicadores()
                .Agregar("Orden recibida", orden.Numero, "destacado")
                .Agregar("Comprobante", orden.DetalleEstado)
                .Agregar("Importe de la factura", importe.ToString("C"))
                .Agregar("Insumos ingresados", orden.Detalles.Count.ToString(), "exito")
                .ToString();

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

    private BEOrdenCompra ObtenerOrdenElegida()
    {
        int idOrden;
        if (!int.TryParse(ddlOrden.SelectedValue, out idOrden) || idOrden <= 0)
        {
            return null;
        }

        try
        {
            return bllOrden.ObtenerPorId(idOrden);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            return null;
        }
    }

    private void MostrarDetalle(BEOrdenCompra orden)
    {
        MostrarDetalle(orden, false);
    }

    /// Muestra cómo va a quedar el stock de cada insumo de la orden y, si se
    /// pide, propone como importe el estimado de la orden.
    private void MostrarDetalle(BEOrdenCompra orden, bool proponerImporte)
    {
        litIndicadores.Text = string.Empty;
        List<object> filas = new List<object>();

        if (orden != null)
        {
            try
            {
                Dictionary<int, decimal> stock = StockDe(orden);
                foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
                {
                    decimal actual;
                    stock.TryGetValue(detalle.IdInsumo, out actual);
                    filas.Add(Fila(detalle, actual, actual + detalle.Cantidad));
                }

                litIndicadores.Text = new Indicadores()
                    .Agregar("Proveedor", string.IsNullOrEmpty(orden.NombreProveedor) ? orden.CuitProveedor : orden.NombreProveedor, "destacado")
                    .Agregar("Fecha de la orden", orden.FechaTexto)
                    .Agregar("Insumos", orden.Detalles.Count.ToString())
                    .Agregar("Importe estimado", orden.ImporteEstimado.ToString("C"))
                    .ToString();

                if (proponerImporte)
                {
                    txtImporte.Text = orden.ImporteEstimado > 0 ? Numeros.ParaInput(Math.Round(orden.ImporteEstimado, 2)) : string.Empty;
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje(lblMensaje, ex.Message, true);
            }
        }

        gvDetalle.DataSource = filas;
        gvDetalle.DataBind();
    }

    /// Stock actual de los insumos de la orden.
    private Dictionary<int, decimal> StockDe(BEOrdenCompra orden)
    {
        Dictionary<int, decimal> stock = new Dictionary<int, decimal>();
        foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
        {
            BEInsumo insumo = bllInsumo.ObtenerPorId(detalle.IdInsumo);
            stock[detalle.IdInsumo] = insumo == null ? 0m : insumo.Stock;
        }
        return stock;
    }

    private static object Fila(BEDetalleOrdenCompra detalle, decimal stockAntes, decimal stockDespues)
    {
        return new
        {
            Insumo = detalle.NombreInsumo,
            Cantidad = detalle.CantidadTexto,
            StockAntes = stockAntes.ToString("0.##") + " " + detalle.UnidadDeMedida,
            StockDespues = stockDespues.ToString("0.##") + " " + detalle.UnidadDeMedida,
            Importe = detalle.ImporteTexto
        };
    }
}
