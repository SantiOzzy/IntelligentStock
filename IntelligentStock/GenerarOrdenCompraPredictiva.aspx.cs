using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-033 Generar orden de compra predictiva.
//
// Al entrar se calcula la propuesta: el modelo de ML.NET (BLLPrediccionDemanda)
// estima cuánto se va a consumir de cada insumo en los próximos días a partir
// de las ventas registradas (los perecederos, solo para los días de su vida
// útil), y a eso se le suma el stock mínimo y se le resta el stock actual y lo
// que ya está pedido en órdenes pendientes.
//
// Para cada insumo se sugiere a qué proveedor pedírselo: el que lo facturó más
// barato en los últimos días, según las facturas registradas (cada renglón de
// una orden recibida guarda el precio facturado). El usuario puede desmarcar
// insumos, cambiar las cantidades o elegir otro proveedor; abajo se ve qué
// órdenes se van a generar. Se genera una orden por proveedor, queda Pendiente
// hasta que se registre la factura (CU-006-036) y se le envía al proveedor por
// correo.
public partial class GenerarOrdenCompraPredictiva : PaginaBase
{
    private const string ClaveSugerencias = "Sugerencias";
    private const string ClaveDiasHistorial = "DiasHistorial";

    private BLLOrdenCompra bllOrden = new BLLOrdenCompra();
    private BLLProveedor bllProveedor = new BLLProveedor();

    /// Proveedores activos, cargados una vez por pedido.
    private Dictionary<string, BEProveedor> proveedores;

    protected override string PatenteRequerida
    {
        get { return "GenerarOrdenCompra"; }
    }

    /// La propuesta que se muestra en la grilla, en el mismo orden.
    private List<BESugerenciaCompra> Sugerencias
    {
        get { return ViewState[ClaveSugerencias] as List<BESugerenciaCompra> ?? new List<BESugerenciaCompra>(); }
        set { ViewState[ClaveSugerencias] = value; }
    }

    private Dictionary<string, BEProveedor> Proveedores
    {
        get
        {
            if (proveedores == null)
            {
                proveedores = new Dictionary<string, BEProveedor>();
                foreach (BEProveedor proveedor in bllProveedor.TraerActivos())
                {
                    proveedores[proveedor.Cuit] = proveedor;
                }
            }
            return proveedores;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidadorDeRango.CargarFechas(rvFecha, DateTime.Today, DateTime.Today.AddDays(BLLOrdenCompra.DiasMaximosFechaOrden));

        if (!IsPostBack)
        {
            txtFecha.Text = DateTime.Today.ToString("yyyy-MM-dd");
            MostrarSugerencias();
        }
    }

    /// Se marcó o desmarcó un insumo, se cambió una cantidad o un proveedor:
    /// se actualizan los precios, los importes y el resumen de órdenes.
    protected void Grilla_Cambio(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        try
        {
            LeerGrilla(false);
            Enlazar();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void gvDetalle_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType != DataControlRowType.DataRow)
        {
            return;
        }

        BESugerenciaCompra sugerencia = (BESugerenciaCompra)e.Row.DataItem;
        DropDownList ddl = (DropDownList)e.Row.FindControl("ddlProveedorFila");
        ddl.Items.Clear();
        ddl.Items.Add(new ListItem("(elegir proveedor)", string.Empty));

        // Primero los que lo facturaron, del más barato al más caro; después el resto.
        List<string> agregados = new List<string>();
        foreach (BEPrecioProveedor precio in sugerencia.Precios)
        {
            if (Proveedores.ContainsKey(precio.CuitProveedor))
            {
                ddl.Items.Add(new ListItem(precio.NombreProveedor + " - " + precio.PrecioUnitario.ToString("C"), precio.CuitProveedor));
                agregados.Add(precio.CuitProveedor);
            }
        }
        List<BEProveedor> resto = new List<BEProveedor>();
        foreach (BEProveedor proveedor in Proveedores.Values)
        {
            if (!agregados.Contains(proveedor.Cuit))
            {
                resto.Add(proveedor);
            }
        }
        resto.Sort(delegate(BEProveedor a, BEProveedor b)
        {
            return string.Compare(a.Nombre, b.Nombre, StringComparison.CurrentCultureIgnoreCase);
        });
        foreach (BEProveedor proveedor in resto)
        {
            ddl.Items.Add(new ListItem(proveedor.Nombre, proveedor.Cuit));
        }

        ListItem elegido = ddl.Items.FindByValue(sugerencia.CuitProveedor ?? string.Empty);
        if (elegido != null)
        {
            ddl.ClearSelection();
            elegido.Selected = true;
        }

        if (!sugerencia.Incluir)
        {
            e.Row.CssClass = "text-muted";
        }
        else if (string.IsNullOrEmpty(sugerencia.CuitProveedor))
        {
            e.Row.CssClass = "warning";
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        DateTime valor;
        if (!DateTime.TryParseExact(txtFecha.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        try
        {
            if (!LeerGrilla(true))
            {
                Enlazar();
                return;
            }

            List<BESugerenciaCompra> pedido = Sugerencias.FindAll(delegate(BESugerenciaCompra s) { return s.Incluir; });
            List<OrdenGenerada> generadas = bllOrden.GenerarPorProveedor(valor, pedido, chkEnviarCorreo.Checked);

            List<string> ordenes = new List<string>();
            List<string> correos = new List<string>();
            decimal importe = 0;
            int insumos = 0;
            foreach (OrdenGenerada generada in generadas)
            {
                BEOrdenCompra orden = generada.Orden;
                ordenes.Add(orden.Numero + " para " + orden.ProveedorTexto + " (" +
                            Textos.Contar(orden.Detalles.Count, "insumo", "insumos") + ", " + orden.ImporteEstimado.ToString("C") + ")");
                if (!string.IsNullOrEmpty(generada.ResultadoCorreo))
                {
                    correos.Add(generada.ResultadoCorreo);
                }
                importe += orden.ImporteEstimado;
                insumos += orden.Detalles.Count;
            }

            string mensaje = (generadas.Count == 1 ? "Se generó la orden de compra " : "Se generaron las órdenes de compra ") +
                             string.Join("; ", ordenes.ToArray()) + ", en estado Pendiente." +
                             (correos.Count > 0 ? " " + string.Join(" ", correos.ToArray()) : string.Empty);

            // La propuesta se vuelve a calcular: lo pedido ya figura como pendiente.
            MostrarSugerencias();

            litIndicadores.Text = new Indicadores()
                .Agregar(generadas.Count == 1 ? "Orden generada" : "Órdenes generadas", generadas.Count.ToString(), "destacado")
                .Agregar("Insumos pedidos", insumos.ToString())
                .Agregar("Importe estimado", importe.ToString("C"))
                .Agregar("Quedan por pedir", Sugerencias.Count.ToString(), Sugerencias.Count > 0 ? "aviso" : "exito")
                .ToString();

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

    /// Pasa a la propuesta lo que el usuario cambió en la grilla: qué insumos
    /// pide, cuánto y a qué proveedor. Con 'validar', si alguna cantidad
    /// marcada no es válida muestra el error y devuelve false; si no, la
    /// cantidad inválida se deja como estaba.
    private bool LeerGrilla(bool validar)
    {
        List<BESugerenciaCompra> sugerencias = Sugerencias;
        for (int i = 0; i < gvDetalle.Rows.Count && i < sugerencias.Count; i++)
        {
            BESugerenciaCompra sugerencia = sugerencias[i];
            GridViewRow fila = gvDetalle.Rows[i];

            CheckBox chk = (CheckBox)fila.FindControl("chkIncluir");
            sugerencia.Incluir = chk != null && chk.Checked;

            TextBox txt = (TextBox)fila.FindControl("txtCantidadPedir");
            decimal cantidad;
            if (txt != null && Numeros.TryParseDecimal(txt.Text, out cantidad) && cantidad > 0)
            {
                sugerencia.CantidadAPedir = cantidad;
            }
            else if (validar && sugerencia.Incluir)
            {
                Sugerencias = sugerencias;
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "Ingrese la cantidad a pedir de " + sugerencia.NombreInsumo + " (mayor que cero)."), true);
                return false;
            }

            DropDownList ddl = (DropDownList)fila.FindControl("ddlProveedorFila");
            string cuit = ddl == null ? string.Empty : ddl.SelectedValue;
            if (cuit != (sugerencia.CuitProveedor ?? string.Empty))
            {
                BEProveedor proveedor;
                if (!string.IsNullOrEmpty(cuit) && Proveedores.TryGetValue(cuit, out proveedor))
                {
                    sugerencia.ElegirProveedor(proveedor.Cuit, proveedor.Nombre);
                    BEPrecioProveedor mejor = sugerencia.Precios.Count > 0 ? sugerencia.Precios[0] : null;
                    sugerencia.MotivoProveedor = mejor != null && mejor.Reciente && mejor.CuitProveedor != cuit &&
                                                 mejor.PrecioUnitario < sugerencia.PrecioUnitario
                        ? "Elegido por el usuario (" + mejor.NombreProveedor + " lo cobró " + mejor.PrecioUnitario.ToString("C") + ")"
                        : "Elegido por el usuario";
                }
                else
                {
                    sugerencia.ElegirProveedor(null, null);
                    sugerencia.MotivoProveedor = "Elija el proveedor";
                }
            }
        }
        Sugerencias = sugerencias;
        return true;
    }

    /// Calcula la propuesta para mostrarla. Si no hay nada para pedir (o no se
    /// puede calcular) no se ofrece generar órdenes.
    private void MostrarSugerencias()
    {
        litPeriodo.Text = "próximos " + BLLOrdenCompra.DiasProyeccion + " días";
        lblAvisoPrediccion.Visible = false;
        try
        {
            ResultadoPrediccion prediccion;
            Sugerencias = bllOrden.CalcularSugerencias(BLLOrdenCompra.DiasProyeccion, out prediccion);
            ViewState[ClaveDiasHistorial] = prediccion.DiasDeHistorial;
            MostrarPrediccion(prediccion);
            Enlazar();
        }
        catch (Exception ex)
        {
            Sugerencias = new List<BESugerenciaCompra>();
            Enlazar();
            btnAceptar.Enabled = false;
            if (!lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, ex.Message, true);
            }
        }
    }

    /// Muestra la propuesta, el resumen de órdenes por proveedor y los indicadores.
    private void Enlazar()
    {
        List<BESugerenciaCompra> sugerencias = Sugerencias;
        gvDetalle.DataSource = sugerencias;
        gvDetalle.DataBind();

        List<ResumenProveedor> resumen = new List<ResumenProveedor>();
        Dictionary<string, ResumenProveedor> porCuit = new Dictionary<string, ResumenProveedor>();
        int incluidos = 0, sinProveedor = 0, perecederos = 0;
        decimal importe = 0;
        foreach (BESugerenciaCompra sugerencia in sugerencias)
        {
            if (sugerencia.LimitadoPorVidaUtil)
            {
                perecederos++;
            }
            if (!sugerencia.Incluir)
            {
                continue;
            }

            incluidos++;
            importe += sugerencia.ImporteEstimado;
            if (string.IsNullOrEmpty(sugerencia.CuitProveedor))
            {
                sinProveedor++;
                continue;
            }

            ResumenProveedor fila;
            if (!porCuit.TryGetValue(sugerencia.CuitProveedor, out fila))
            {
                BEProveedor proveedor;
                Proveedores.TryGetValue(sugerencia.CuitProveedor, out proveedor);
                fila = new ResumenProveedor();
                fila.Proveedor = sugerencia.NombreProveedor;
                fila.Correo = proveedor == null || string.IsNullOrEmpty(proveedor.Correo)
                    ? "Sin correo: la orden no se le puede enviar"
                    : proveedor.Correo;
                porCuit[sugerencia.CuitProveedor] = fila;
                resumen.Add(fila);
            }
            fila.CantidadInsumos++;
            fila.Importe += sugerencia.ImporteEstimado;
        }

        gvResumen.DataSource = resumen;
        gvResumen.DataBind();

        lblSinProveedor.Visible = sinProveedor > 0;
        lblSinProveedor.Text = sinProveedor > 0
            ? "Hay " + Textos.Contar(sinProveedor, "insumo marcado", "insumos marcados") +
              " sin proveedor: elíjalo en la grilla (fila en amarillo) o desmárquelo."
            : string.Empty;
        btnAceptar.Enabled = incluidos > 0;

        object diasHistorial = ViewState[ClaveDiasHistorial];
        litIndicadores.Text = new Indicadores()
            .Agregar("Insumos para pedir", incluidos.ToString(), "destacado")
            .Agregar("Importe estimado", importe.ToString("C"))
            .Agregar("Órdenes (proveedores)", resumen.Count.ToString())
            .Agregar("Limitados por vida útil", perecederos.ToString())
            .Agregar("Días de ventas analizados", diasHistorial == null ? "-" : diasHistorial.ToString())
            .ToString();
    }

    private void MostrarPrediccion(ResultadoPrediccion prediccion)
    {
        litPeriodo.Text = "próximos " + prediccion.DiasProyectados + " días";
        lblPrediccion.Text = prediccion.Descripcion;
        lblPrediccion.Visible = true;
        lblAvisoPrediccion.Text = prediccion.Aviso;
        lblAvisoPrediccion.Visible = !string.IsNullOrEmpty(prediccion.Aviso);
    }

    /// Una fila del resumen: la orden que se va a generar para un proveedor.
    public class ResumenProveedor
    {
        public string Proveedor { get; set; }

        public int CantidadInsumos { get; set; }

        public decimal Importe { get; set; }

        public string Correo { get; set; }

        public string Insumos
        {
            get { return CantidadInsumos.ToString(CultureInfo.InvariantCulture); }
        }

        public string ImporteTexto
        {
            get { return Importe.ToString("C"); }
        }
    }
}
