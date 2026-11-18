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
// Como cada proveedor vende lo suyo, se arma una orden por proveedor: al
// elegirlo se marcan los insumos que se le pidieron la última vez, el usuario
// marca o desmarca los que quiere pedirle y puede ajustar las cantidades. La
// orden queda Pendiente hasta que se registre la factura (CU-006-036) y se le
// envía al proveedor por correo. Lo que no se pidió sigue en la propuesta para
// el próximo proveedor.
public partial class GenerarOrdenCompraPredictiva : PaginaBase
{
    private const string ClaveSugerencias = "Sugerencias";

    private BLLOrdenCompra bllOrden = new BLLOrdenCompra();
    private BLLProveedor bllProveedor = new BLLProveedor();

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

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidadorDeRango.CargarFechas(rvFecha, DateTime.Today, DateTime.Today.AddDays(60));

        if (!IsPostBack)
        {
            txtFecha.Text = DateTime.Today.ToString("yyyy-MM-dd");
            CargarProveedores();
            MostrarSugerencias();
        }
    }

    /// Marca los insumos que se le pidieron a este proveedor la última vez.
    protected void ddlProveedor_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        string cuit = ddlProveedor.SelectedValue;
        List<BESugerenciaCompra> sugerencias = Sugerencias;

        int marcados = 0;
        for (int i = 0; i < gvDetalle.Rows.Count && i < sugerencias.Count; i++)
        {
            CheckBox chk = (CheckBox)gvDetalle.Rows[i].FindControl("chkIncluir");
            bool delProveedor = !string.IsNullOrEmpty(cuit) && sugerencias[i].CuitProveedorHabitual == cuit;
            chk.Checked = delProveedor;
            if (delProveedor)
            {
                marcados++;
            }
        }

        if (string.IsNullOrEmpty(cuit))
        {
            return;
        }

        try
        {
            BEProveedor proveedor = bllProveedor.ObtenerPorCuit(cuit);
            bool tieneCorreo = proveedor != null && !string.IsNullOrEmpty(proveedor.Correo);
            chkEnviarCorreo.Enabled = tieneCorreo;
            chkEnviarCorreo.Checked = tieneCorreo;
            chkEnviarCorreo.Text = tieneCorreo
                ? "Enviarle la orden por correo a " + proveedor.Correo + " (con la orden en PDF)"
                : "El proveedor no tiene correo cargado: la orden no se le puede enviar por correo";

            MostrarMensaje(lblMensaje, marcados > 0
                ? "Se marcaron " + Textos.Contar(marcados, "insumo que se le pidió", "insumos que se le pidieron") +
                  " a " + ddlProveedor.SelectedItem.Text + " la última vez. Marque o desmarque los que quiera pedirle."
                : "A " + ddlProveedor.SelectedItem.Text + " todavía no se le pidió ninguno de estos insumos: marque los que quiera pedirle.",
                false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        if (string.IsNullOrEmpty(ddlProveedor.SelectedValue) || string.IsNullOrEmpty(txtFecha.Text.Trim()))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002", "Elija el proveedor y la fecha de la orden."), true);
            return;
        }

        DateTime valor;
        if (!DateTime.TryParseExact(txtFecha.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }
        DateTime? fecha = valor;

        List<BESugerenciaCompra> pedido = LeerPedido();
        if (pedido == null)
        {
            return;
        }
        if (pedido.Count == 0)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Marque al menos un insumo para pedirle a " + ddlProveedor.SelectedItem.Text + "."), true);
            return;
        }

        try
        {
            string resultadoCorreo;
            BEOrdenCompra orden = bllOrden.GenerarPredictiva(ddlProveedor.SelectedValue, fecha, pedido,
                                                             chkEnviarCorreo.Enabled && chkEnviarCorreo.Checked, out resultadoCorreo);

            string mensaje = "Se generó la orden de compra " + orden.Numero + " para " + orden.ProveedorTexto + " con " +
                             Textos.Contar(orden.Detalles.Count, "insumo", "insumos") + ", por un importe estimado de " +
                             orden.ImporteEstimado.ToString("C") + ". Quedó en estado Pendiente.";
            if (!string.IsNullOrEmpty(resultadoCorreo))
            {
                mensaje += " " + resultadoCorreo;
            }

            // La propuesta se vuelve a calcular: lo pedido ya figura como pendiente.
            ddlProveedor.SelectedIndex = 0;
            chkEnviarCorreo.Enabled = true;
            chkEnviarCorreo.Checked = true;
            chkEnviarCorreo.Text = "Enviarle la orden por correo al proveedor (con la orden en PDF)";
            MostrarSugerencias();

            litIndicadores.Text = new Indicadores()
                .Agregar("Orden generada", orden.Numero, "destacado")
                .Agregar("Insumos pedidos", orden.Detalles.Count.ToString())
                .Agregar("Importe estimado", orden.ImporteEstimado.ToString("C"))
                .Agregar("Quedan por pedir", Sugerencias.Count.ToString(), Sugerencias.Count > 0 ? "aviso" : "exito")
                .ToString();

            MostrarMensaje(lblMensaje, mensaje + (Sugerencias.Count > 0
                ? " Quedan " + Textos.Contar(Sugerencias.Count, "insumo", "insumos") + " en la propuesta: elija el próximo proveedor."
                : string.Empty), false);
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
            foreach (BEProveedor proveedor in bllProveedor.TraerActivos())
            {
                ddlProveedor.Items.Add(new ListItem(proveedor.Descripcion, proveedor.Cuit));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Los insumos marcados con la cantidad a pedir. Devuelve null (y muestra
    /// el error) si alguna cantidad no es válida.
    private List<BESugerenciaCompra> LeerPedido()
    {
        List<BESugerenciaCompra> sugerencias = Sugerencias;
        List<BESugerenciaCompra> pedido = new List<BESugerenciaCompra>();
        for (int i = 0; i < gvDetalle.Rows.Count && i < sugerencias.Count; i++)
        {
            CheckBox chk = (CheckBox)gvDetalle.Rows[i].FindControl("chkIncluir");
            if (chk == null || !chk.Checked)
            {
                continue;
            }

            TextBox txt = (TextBox)gvDetalle.Rows[i].FindControl("txtCantidadPedir");
            decimal cantidad;
            if (txt == null || !Numeros.TryParseDecimal(txt.Text, out cantidad) || cantidad <= 0)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "Ingrese la cantidad a pedir de " + sugerencias[i].NombreInsumo + " (mayor que cero)."), true);
                return null;
            }

            BESugerenciaCompra item = sugerencias[i];
            item.CantidadAPedir = cantidad;
            pedido.Add(item);
        }
        return pedido;
    }

    /// Calcula la propuesta para mostrarla. Si no hay nada para pedir (o no se
    /// puede calcular) no se ofrece generar la orden.
    private void MostrarSugerencias()
    {
        litPeriodo.Text = "próximos " + BLLOrdenCompra.DiasProyeccion + " días";
        lblAvisoPrediccion.Visible = false;
        try
        {
            ResultadoPrediccion prediccion;
            List<BESugerenciaCompra> sugerencias = bllOrden.CalcularSugerencias(BLLOrdenCompra.DiasProyeccion, out prediccion);
            Sugerencias = sugerencias;
            gvDetalle.DataSource = sugerencias;
            gvDetalle.DataBind();
            MostrarPrediccion(prediccion);
            btnAceptar.Enabled = true;

            decimal importe = 0;
            int perecederos = 0;
            foreach (BESugerenciaCompra sugerencia in sugerencias)
            {
                importe += sugerencia.ImporteEstimado;
                if (sugerencia.LimitadoPorVidaUtil)
                {
                    perecederos++;
                }
            }
            litIndicadores.Text = new Indicadores()
                .Agregar("Insumos para pedir", sugerencias.Count.ToString(), "destacado")
                .Agregar("Importe estimado", importe.ToString("C"))
                .Agregar("Días proyectados", prediccion.DiasProyectados.ToString())
                .Agregar("Limitados por vida útil", perecederos.ToString())
                .Agregar("Días de ventas analizados", prediccion.DiasDeHistorial.ToString())
                .ToString();
        }
        catch (Exception ex)
        {
            Sugerencias = new List<BESugerenciaCompra>();
            gvDetalle.DataSource = null;
            gvDetalle.DataBind();
            btnAceptar.Enabled = false;
            if (!lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, ex.Message, true);
            }
        }
    }

    private void MostrarPrediccion(ResultadoPrediccion prediccion)
    {
        litPeriodo.Text = "próximos " + prediccion.DiasProyectados + " días";
        lblPrediccion.Text = prediccion.Descripcion;
        lblPrediccion.Visible = true;
        lblAvisoPrediccion.Text = prediccion.Aviso;
        lblAvisoPrediccion.Visible = !string.IsNullOrEmpty(prediccion.Aviso);
    }
}
