using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-037 Descontar stock por venta.
//
// Esta pantalla recibe las ventas para descontar del stock lo que lleva cada
// receta. Hay dos formas de cargarlas:
//   - Venta: un pedido con una o varias recetas (por ejemplo, todo lo que pidió
//     una mesa). Se agregan las recetas de a una y se registra todo junto.
//   - Cierre del día: todas las recetas vendidas en un día, de una vez.
// En las dos, la grilla muestra antes de confirmar cuánto se descuenta de cada
// insumo y cómo queda el stock. La BLL graba las ventas (tablas Venta y
// ConsumoVenta) y descuenta todo en una sola transacción, recalcula el dígito
// verificador y deja el registro en la bitácora. Si el stock del sistema no
// alcanza, la venta igual se registra y el insumo queda en cero (se avisa para
// corregirlo con un ajuste). Si un insumo queda en su mínimo, el
// ControladorStock (Observer) avisa por correo.
//
// La tercera pestaña, "Ventas registradas", consulta las ventas cargadas en un
// período y lo que descontó cada una. Solo consulta: no graba nada.
public partial class DescontarStockVenta : PaginaBase
{
    private const int ModoPedido = 0;
    private const int ModoCierre = 1;
    private const int ModoVentas = 2;

    private const string ClavePedido = "Pedido";

    /// Filtro de la última búsqueda de "Ventas registradas" (para paginar y
    /// ver el consumo sin depender de lo que esté escrito en los filtros).
    private const string ClaveVentasDesde = "VentasDesde";
    private const string ClaveVentasHasta = "VentasHasta";
    private const string ClaveVentasReceta = "VentasReceta";

    /// Días hacia atrás que muestra la consulta al entrar.
    private const int DiasConsultaInicial = 30;

    private BLLVenta bllVenta = new BLLVenta();

    protected override string PatenteRequerida
    {
        get { return "DescontarStockVenta"; }
    }

    /// Las recetas del pedido que se está armando, en el orden en que se agregaron.
    private List<BEVenta> Pedido
    {
        get { return ViewState[ClavePedido] as List<BEVenta> ?? new List<BEVenta>(); }
        set { ViewState[ClavePedido] = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidadorDeRango.CargarFechas(rvFecha, DateTime.Today.AddDays(-BLLVenta.DiasMaximosHaciaAtras), DateTime.Today);
        ValidadorDeRango.CargarFechas(rvFechaCierre, DateTime.Today.AddDays(-BLLVenta.DiasMaximosHaciaAtras), DateTime.Today);

        if (!IsPostBack)
        {
            txtFecha.Text = DateTime.Today.ToString("yyyy-MM-dd");
            txtFechaCierre.Text = DateTime.Today.ToString("yyyy-MM-dd");
            txtVentasDesde.Text = DateTime.Today.AddDays(1 - DiasConsultaInicial).ToString("yyyy-MM-dd");
            txtVentasHasta.Text = DateTime.Today.ToString("yyyy-MM-dd");
            CargarRecetas();
            CambiarModo(ModoPedido);
        }
    }

    protected void lnkModo_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CambiarModo(int.Parse(((LinkButton)sender).CommandArgument, CultureInfo.InvariantCulture));
    }

    protected void btnAgregar_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        if (!IsValid)
        {
            return;
        }

        int idReceta;
        int.TryParse(ddlReceta.SelectedValue, out idReceta);
        decimal cantidad;
        if (!Numeros.TryParseDecimal(txtCantidad.Text, out cantidad))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La cantidad vendida tiene que ser un número."), true);
            return;
        }

        try
        {
            Pedido = bllVenta.AgregarAlPedido(Pedido, idReceta, cantidad);
            ddlReceta.SelectedIndex = 0;
            txtCantidad.Text = string.Empty;
            ddlReceta.Focus();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
        MostrarPedido();
    }

    protected void gvPedido_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName != "Quitar")
        {
            return;
        }

        OcultarMensaje(lblMensaje);
        int indice = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
        List<BEVenta> pedido = Pedido;
        if (indice >= 0 && indice < pedido.Count)
        {
            pedido.RemoveAt(indice);
            Pedido = pedido;
        }
        MostrarPedido();
    }

    protected void txtFechaCierre_TextChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        AvisarVentasDelDia();
    }

    protected void btnPrevisualizarCierre_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        if (!IsValid)
        {
            return;
        }

        List<BEVenta> ventas = LeerCierre();
        if (ventas == null)
        {
            return;
        }
        if (ventas.Count == 0)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Ingrese la cantidad vendida de al menos una receta."), true);
            LimpiarDetalle();
            return;
        }

        AvisarVentasDelDia();
        MostrarVistaPrevia(ventas);
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            if (mvModo.ActiveViewIndex == ModoCierre)
            {
                RegistrarCierre();
            }
            else
            {
                RegistrarPedido();
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnBuscarVentas_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        if (!IsValid)
        {
            return;
        }

        DateTime? desde = LeerFecha(txtVentasDesde.Text);
        DateTime? hasta = LeerFecha(txtVentasHasta.Text);
        if (!desde.HasValue || !hasta.HasValue)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        int idReceta;
        int.TryParse(ddlRecetaVentas.SelectedValue, out idReceta);
        ViewState[ClaveVentasDesde] = desde.Value;
        ViewState[ClaveVentasHasta] = hasta.Value;
        ViewState[ClaveVentasReceta] = idReceta;
        gvVentas.PageIndex = 0;
        MostrarVentas();
    }

    protected void gvVentas_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvVentas.PageIndex = e.NewPageIndex;
        MostrarVentas();
    }

    protected void gvVentas_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName != "Consumo")
        {
            return;
        }

        OcultarMensaje(lblMensaje);
        int fila;
        if (!int.TryParse(Convert.ToString(e.CommandArgument), out fila) || fila < 0 || fila >= gvVentas.DataKeys.Count)
        {
            return;
        }

        int idVenta = (int)gvVentas.DataKeys[fila].Value;
        try
        {
            BEVenta venta = ConsultarVentas().Find(delegate(BEVenta v) { return v.IdVenta == idVenta; });
            if (venta == null)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR013"), true);
                return;
            }

            gvVentas.SelectedIndex = fila;
            litTituloConsumo.Text = HttpUtility.HtmlEncode("Consumo de la venta: " + venta.CantidadTexto + " x " +
                                                           venta.NombreReceta + " del " + venta.FechaTexto);
            gvConsumoVenta.DataSource = venta.Consumos;
            gvConsumoVenta.DataBind();
            pnlConsumoVenta.Visible = true;
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

    #region Modos

    private void CambiarModo(int modo)
    {
        mvModo.ActiveViewIndex = modo;
        tabPedido.Attributes["class"] = modo == ModoPedido ? "active" : string.Empty;
        tabCierre.Attributes["class"] = modo == ModoCierre ? "active" : string.Empty;
        tabVentas.Attributes["class"] = modo == ModoVentas ? "active" : string.Empty;

        // La consulta de ventas no registra nada: no se muestra la grilla de
        // stock ni el botón de registrar.
        pnlStock.Visible = modo != ModoVentas;
        btnAceptar.Visible = modo != ModoVentas;
        btnAceptar.ValidationGroup = modo == ModoPedido ? "Venta" : "Cierre";
        btnAceptar.Text = modo == ModoPedido ? "Registrar la venta" : "Registrar el cierre del día";

        LimpiarDetalle();
        if (modo == ModoVentas)
        {
            // Al entrar se muestran las ventas de los últimos días (o la
            // última búsqueda, si ya se hizo una).
            if (ViewState[ClaveVentasDesde] == null)
            {
                ViewState[ClaveVentasDesde] = DateTime.Today.AddDays(1 - DiasConsultaInicial);
                ViewState[ClaveVentasHasta] = DateTime.Today;
                ViewState[ClaveVentasReceta] = 0;
            }
            MostrarVentas();
        }
        else if (modo == ModoPedido)
        {
            MostrarPedido();
        }
        else
        {
            if (gvCierre.Rows.Count == 0)
            {
                CargarCierre();
            }
            AvisarVentasDelDia();
        }
    }

    private void LimpiarDetalle()
    {
        gvDetalle.DataSource = null;
        gvDetalle.DataBind();
        litIndicadores.Text = string.Empty;
    }

    #endregion

    #region Venta (pedido)

    /// Muestra las recetas del pedido y lo que se descontaría con todas juntas.
    private void MostrarPedido()
    {
        List<BEVenta> pedido = Pedido;
        gvPedido.DataSource = pedido;
        gvPedido.DataBind();

        if (pedido.Count == 0)
        {
            LimpiarDetalle();
            return;
        }
        MostrarVistaPrevia(pedido);
    }

    private void RegistrarPedido()
    {
        List<BEVenta> pedido = Pedido;
        if (pedido.Count == 0)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Agregue al pedido al menos una receta: elíjala, ingrese las unidades y presione Agregar al pedido."), true);
            return;
        }

        DateTime? fecha = LeerFecha(txtFecha.Text);
        if (!fecha.HasValue)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        decimal unidades = 0;
        foreach (BEVenta venta in pedido)
        {
            unidades += venta.Cantidad;
        }
        string descripcion = pedido.Count == 1
            ? pedido[0].CantidadTexto + " x " + pedido[0].NombreReceta
            : Textos.Contar(pedido.Count, "receta", "recetas") + ", " + unidades.ToString("0.##") + " unidades";

        List<BEConsumoVenta> consumos = bllVenta.RegistrarPedido(pedido, fecha);

        // El pedido ya se registró: se vacía para cargar el próximo.
        Pedido = new List<BEVenta>();
        gvPedido.DataSource = Pedido;
        gvPedido.DataBind();

        MostrarResultado(consumos, "Se registró la venta de " + descripcion, descripcion);
    }

    private void CargarRecetas()
    {
        try
        {
            foreach (BEReceta receta in bllVenta.TraerRecetasVendibles())
            {
                ddlReceta.Items.Add(new ListItem(receta.Nombre, receta.IdReceta.ToString()));
                ddlRecetaVentas.Items.Add(new ListItem(receta.Nombre, receta.IdReceta.ToString()));
            }

            if (ddlReceta.Items.Count == 1)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR013",
                    "No hay recetas activas con insumos cargados."), true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    #endregion

    #region Cierre del día

    private void CargarCierre()
    {
        try
        {
            gvCierre.DataSource = bllVenta.TraerRecetasVendibles();
            gvCierre.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Las recetas con cantidad cargada. Devuelve null (y muestra el error) si
    /// alguna cantidad no es válida.
    private List<BEVenta> LeerCierre()
    {
        List<BEVenta> ventas = new List<BEVenta>();
        foreach (GridViewRow fila in gvCierre.Rows)
        {
            TextBox txt = (TextBox)fila.FindControl("txtCantidadReceta");
            string texto = txt == null ? string.Empty : txt.Text.Trim();
            if (texto.Length == 0)
            {
                continue;
            }

            decimal cantidad;
            string receta = HttpUtility.HtmlDecode(fila.Cells[0].Text);
            if (!Numeros.TryParseDecimal(texto, out cantidad) || cantidad < 0)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "La cantidad de " + receta + " no es válida."), true);
                return null;
            }
            if (cantidad == 0)
            {
                continue;
            }

            BEVenta venta = new BEVenta();
            venta.IdReceta = (int)gvCierre.DataKeys[fila.RowIndex].Value;
            venta.NombreReceta = receta;
            venta.Cantidad = cantidad;
            ventas.Add(venta);
        }
        return ventas;
    }

    private void RegistrarCierre()
    {
        List<BEVenta> ventas = LeerCierre();
        if (ventas == null)
        {
            return;
        }

        DateTime? fecha = LeerFecha(txtFechaCierre.Text);
        if (!fecha.HasValue)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        decimal unidades = 0;
        foreach (BEVenta venta in ventas)
        {
            unidades += venta.Cantidad;
        }

        List<BEConsumoVenta> consumos = bllVenta.RegistrarCierreDelDia(ventas, fecha);

        foreach (GridViewRow fila in gvCierre.Rows)
        {
            TextBox txt = (TextBox)fila.FindControl("txtCantidadReceta");
            if (txt != null)
            {
                txt.Text = string.Empty;
            }
        }
        lblAvisoCierre.Visible = false;

        MostrarResultado(consumos, "Se registró el cierre del " + fecha.Value.ToString("dd/MM/yyyy") + ": " +
                                   Textos.Contar(ventas.Count, "receta", "recetas") + ", " + unidades.ToString("0.##") + " unidades",
                         Textos.Contar(ventas.Count, "receta", "recetas") + " (" + unidades.ToString("0.##") + " u.)");
    }

    /// Avisa si el día elegido ya tiene ventas cargadas, para no contarlas dos veces.
    private void AvisarVentasDelDia()
    {
        lblAvisoCierre.Visible = false;
        DateTime? fecha = LeerFecha(txtFechaCierre.Text);
        if (!fecha.HasValue)
        {
            return;
        }

        try
        {
            List<BEVenta> ventas = bllVenta.TraerVentasDelDia(fecha.Value);
            if (ventas.Count == 0)
            {
                return;
            }

            decimal unidades = 0;
            foreach (BEVenta venta in ventas)
            {
                unidades += venta.Cantidad;
            }
            lblAvisoCierre.Text = "El " + fecha.Value.ToString("dd/MM/yyyy") + " ya tiene " +
                                  Textos.Contar(ventas.Count, "venta registrada", "ventas registradas") + " (" +
                                  unidades.ToString("0.##") + " unidades). Cargue solo lo que falta, para no contarlas dos veces.";
            lblAvisoCierre.Visible = true;
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    #endregion

    #region Ventas registradas

    /// Ventas de la última búsqueda.
    private List<BEVenta> ConsultarVentas()
    {
        DateTime desde = (DateTime)ViewState[ClaveVentasDesde];
        DateTime hasta = (DateTime)ViewState[ClaveVentasHasta];
        int idReceta = ViewState[ClaveVentasReceta] == null ? 0 : (int)ViewState[ClaveVentasReceta];
        return bllVenta.ConsultarVentas(desde, hasta, idReceta);
    }

    /// Muestra las ventas de la última búsqueda y sus indicadores.
    private void MostrarVentas()
    {
        pnlConsumoVenta.Visible = false;
        gvVentas.SelectedIndex = -1;
        litIndicadoresVentas.Text = string.Empty;
        try
        {
            List<BEVenta> ventas = ConsultarVentas();
            if (gvVentas.PageIndex * gvVentas.PageSize >= ventas.Count)
            {
                gvVentas.PageIndex = 0;
            }
            gvVentas.DataSource = ventas;
            gvVentas.DataBind();

            decimal unidades = 0;
            List<DateTime> dias = new List<DateTime>();
            Dictionary<string, decimal> porReceta = new Dictionary<string, decimal>();
            foreach (BEVenta venta in ventas)
            {
                unidades += venta.Cantidad;
                if (!dias.Contains(venta.Fecha.Date))
                {
                    dias.Add(venta.Fecha.Date);
                }
                decimal acumulado;
                porReceta.TryGetValue(venta.NombreReceta, out acumulado);
                porReceta[venta.NombreReceta] = acumulado + venta.Cantidad;
            }

            string masVendida = "-";
            decimal maximo = 0;
            foreach (KeyValuePair<string, decimal> receta in porReceta)
            {
                if (receta.Value > maximo)
                {
                    maximo = receta.Value;
                    masVendida = receta.Key + " (" + receta.Value.ToString("0.##") + " u.)";
                }
            }

            litIndicadoresVentas.Text = new Indicadores()
                .Agregar("Ventas", ventas.Count.ToString(), "destacado")
                .Agregar("Unidades vendidas", unidades.ToString("0.##"))
                .Agregar("Días con ventas", dias.Count.ToString())
                .Agregar("Receta más vendida", masVendida)
                .ToString();
        }
        catch (Exception ex)
        {
            gvVentas.DataSource = null;
            gvVentas.DataBind();
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    #endregion

    #region Grilla de stock

    private void MostrarVistaPrevia(List<BEVenta> ventas)
    {
        try
        {
            MostrarConsumos(bllVenta.Previsualizar(ventas), true);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Muestra lo que se descuenta de cada insumo. En la vista previa marca en
    /// rojo lo que no alcanza y en amarillo lo que queda en su mínimo.
    private void MostrarConsumos(List<BEConsumoVenta> consumos, bool vistaPrevia)
    {
        gvDetalle.DataSource = consumos;
        gvDetalle.DataBind();

        int sinStock = 0, enMinimo = 0;
        for (int i = 0; i < consumos.Count && i < gvDetalle.Rows.Count; i++)
        {
            if (consumos[i].SinStock)
            {
                gvDetalle.Rows[i].CssClass = "danger";
                sinStock++;
            }
            else if (consumos[i].QuedaEnMinimo)
            {
                gvDetalle.Rows[i].CssClass = "warning";
                enMinimo++;
            }
        }

        if (vistaPrevia)
        {
            litIndicadores.Text = new Indicadores()
                .Agregar("Insumos que se descuentan", consumos.Count.ToString(), "destacado")
                .Agregar("No alcanza el stock", sinStock.ToString(), sinStock > 0 ? "peligro" : "exito")
                .Agregar("Quedarían en su mínimo", enMinimo.ToString(), enMinimo > 0 ? "aviso" : null)
                .ToString();

            if (sinStock > 0 && !lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, "Los insumos marcados en rojo no alcanzan según el stock del sistema. " +
                                           "La venta se puede registrar igual: esos insumos quedan en cero y conviene " +
                                           "revisarlos con un ajuste de stock.", true);
            }
        }
    }

    /// Muestra el resultado de un registro: cómo quedó el stock, los insumos
    /// que quedaron en su mínimo y los que no alcanzaban.
    private void MostrarResultado(List<BEConsumoVenta> consumos, string inicioMensaje, string indicador)
    {
        MostrarConsumos(consumos, false);

        List<string> enMinimo = new List<string>();
        List<string> faltantes = new List<string>();
        foreach (BEConsumoVenta consumo in consumos)
        {
            if (consumo.SinStock)
            {
                faltantes.Add(consumo.NombreInsumo + " (faltó " + consumo.FaltanteTexto + ")");
            }
            else if (consumo.QuedaEnMinimo)
            {
                enMinimo.Add(consumo.NombreInsumo);
            }
        }

        string mensaje = inicioMensaje + " y se descontó el stock de " + Textos.Contar(consumos.Count, "insumo", "insumos") + ".";
        if (enMinimo.Count > 0)
        {
            mensaje += " Quedaron en su stock mínimo o por debajo: " + string.Join(", ", enMinimo.ToArray()) + ".";
        }
        if (faltantes.Count > 0)
        {
            mensaje += " El stock del sistema no alcanzaba y quedó en cero: " + string.Join(", ", faltantes.ToArray()) +
                       ". Revíselo con un ajuste de stock.";
        }

        litIndicadores.Text = new Indicadores()
            .Agregar("Registrado", indicador, "destacado")
            .Agregar("Insumos descontados", consumos.Count.ToString(), "exito")
            .Agregar("En su mínimo o por debajo", enMinimo.Count.ToString(), enMinimo.Count > 0 ? "aviso" : null)
            .Agregar("Sin stock suficiente", faltantes.Count.ToString(), faltantes.Count > 0 ? "peligro" : null)
            .ToString();

        MostrarMensaje(lblMensaje, mensaje, false);
    }

    #endregion

    private static DateTime? LeerFecha(string texto)
    {
        DateTime fecha;
        if (DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
        {
            return fecha;
        }
        return null;
    }
}
