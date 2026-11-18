using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-036 Registrar factura de proveedor.
//
// Se elige una orden Pendiente y se carga el comprobante de la factura. En la
// grilla se controla contra el remito cuánto llegó de cada insumo (puede ser
// menos que lo pedido, o nada) y el precio unitario facturado. La orden pasa a
// "Recibida: <comprobante>", lo que llegó de cada insumo se suma al stock (R8)
// y los precios de los insumos se actualizan, todo en una sola transacción. El
// comprobante no se puede repetir para el mismo proveedor (#ERR006). El importe
// total de la factura y las diferencias con lo pedido quedan en la bitácora.
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

        List<BEDetalleOrdenCompra> recibido = LeerRecibido();
        if (recibido == null)
        {
            return;
        }

        try
        {
            // Lo que había antes de recibir, para mostrar cómo quedó cada insumo.
            BEOrdenCompra antes = bllOrden.ObtenerPorId(idOrden);
            Dictionary<int, decimal> stockAntes = antes == null ? new Dictionary<int, decimal>() : StockDe(antes);
            Dictionary<int, decimal> pedido = new Dictionary<int, decimal>();
            if (antes != null)
            {
                foreach (BEDetalleOrdenCompra detalle in antes.Detalles)
                {
                    pedido[detalle.IdInsumo] = detalle.Cantidad;
                }
            }

            string aviso;
            BEOrdenCompra orden = bllOrden.RegistrarFactura(idOrden, txtComprobante.Text, fecha, importe, recibido, out aviso);

            List<object> filas = new List<object>();
            int distintos = 0;
            foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
            {
                decimal previo, cantidadPedida;
                stockAntes.TryGetValue(detalle.IdInsumo, out previo);
                pedido.TryGetValue(detalle.IdInsumo, out cantidadPedida);
                if (cantidadPedida != detalle.Cantidad)
                {
                    distintos++;
                }
                BEInsumo insumo = bllInsumo.ObtenerPorId(detalle.IdInsumo);
                filas.Add(new
                {
                    Insumo = detalle.NombreInsumo,
                    Pedido = cantidadPedida.ToString("0.##") + " " + detalle.UnidadDeMedida,
                    Recibido = detalle.CantidadTexto,
                    StockAntes = previo.ToString("0.##") + " " + detalle.UnidadDeMedida,
                    StockDespues = (insumo == null ? previo + detalle.Cantidad : insumo.Stock).ToString("0.##") + " " + detalle.UnidadDeMedida,
                    Precio = detalle.PrecioUnitario.ToString("C")
                });
            }
            gvDetalle.DataSource = null;
            gvDetalle.DataBind();
            gvDetalle.Visible = false;
            gvResultado.DataSource = filas;
            gvResultado.DataBind();
            gvResultado.Visible = true;

            litIndicadores.Text = new Indicadores()
                .Agregar("Orden recibida", orden.Numero, "destacado")
                .Agregar("Comprobante", orden.DetalleEstado)
                .Agregar("Importe de la factura", importe.ToString("C"))
                .Agregar("Insumos ingresados", orden.Detalles.Count.ToString(), "exito")
                .Agregar("Llegó distinto de lo pedido", distintos.ToString(), distintos > 0 ? "aviso" : null)
                .ToString();

            txtComprobante.Text = string.Empty;
            txtImporte.Text = string.Empty;
            CargarOrdenes();

            MostrarMensaje(lblMensaje,
                "Se registró el comprobante " + orden.DetalleEstado + " de la orden " + orden.Numero + " (" +
                orden.ProveedorTexto + "). La orden quedó Recibida, ingresó al stock lo que llegó de cada insumo y se " +
                "actualizaron los precios unitarios." +
                (distintos > 0 ? " " + Textos.Contar(distintos, "insumo llegó", "insumos llegaron") + " distinto de lo pedido." : string.Empty) +
                (string.IsNullOrEmpty(aviso) ? string.Empty : " Atención: " + aviso), false);
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

    /// Muestra los insumos de la orden con lo pedido, para cargar lo que llegó
    /// y el precio facturado (se proponen lo pedido y el precio de la orden) y, si
    /// se pide, propone como importe el estimado de la orden.
    private void MostrarDetalle(BEOrdenCompra orden, bool proponerImporte)
    {
        litIndicadores.Text = string.Empty;
        gvResultado.Visible = false;
        gvDetalle.Visible = true;
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
                    filas.Add(new
                    {
                        IdInsumo = detalle.IdInsumo,
                        Insumo = detalle.NombreInsumo,
                        Pedido = detalle.CantidadTexto,
                        Unidad = detalle.UnidadDeMedida,
                        CantidadInput = Numeros.ParaInput(detalle.Cantidad),
                        PrecioInput = Numeros.ParaInput(detalle.PrecioUnitario),
                        StockAntes = actual.ToString("0.##") + " " + detalle.UnidadDeMedida
                    });
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

    /// Lo que llegó de cada insumo y su precio, leído de la grilla. Devuelve
    /// null (y muestra el error) si algún valor no es un número.
    private List<BEDetalleOrdenCompra> LeerRecibido()
    {
        List<BEDetalleOrdenCompra> recibido = new List<BEDetalleOrdenCompra>();
        foreach (GridViewRow fila in gvDetalle.Rows)
        {
            string insumo = HttpUtility.HtmlDecode(fila.Cells[0].Text);
            TextBox txtRecibido = (TextBox)fila.FindControl("txtRecibido");
            TextBox txtPrecio = (TextBox)fila.FindControl("txtPrecio");

            decimal cantidad, precio;
            if (txtRecibido == null || !Numeros.TryParseDecimal(txtRecibido.Text, out cantidad))
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "Ingrese cuánto llegó de " + insumo + " (0 si no llegó)."), true);
                return null;
            }
            if (txtPrecio == null || !Numeros.TryParseDecimal(txtPrecio.Text, out precio))
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "Ingrese el precio unitario facturado de " + insumo + "."), true);
                return null;
            }

            BEDetalleOrdenCompra detalle = new BEDetalleOrdenCompra();
            detalle.IdInsumo = (int)gvDetalle.DataKeys[fila.RowIndex].Value;
            detalle.NombreInsumo = insumo;
            detalle.Cantidad = cantidad;
            detalle.PrecioUnitario = precio;
            recibido.Add(detalle);
        }
        return recibido;
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
}
