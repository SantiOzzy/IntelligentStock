using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-033 Generar orden de compra predictiva.
//
// Al entrar se calcula la sugerencia: el modelo de ML.NET (BLLPrediccionDemanda)
// estima cuánto se va a consumir de cada insumo en los próximos días a partir
// de las ventas registradas, y a eso se le suma el stock mínimo y se le resta
// el stock actual y lo que ya está pedido en órdenes pendientes. El usuario
// elige el proveedor y la fecha y genera la orden con esas cantidades, que
// queda Pendiente hasta que se registre la factura (CU-006-036).
public partial class GenerarOrdenCompra : PaginaBase
{
    private BLLOrdenCompra bllOrden = new BLLOrdenCompra();
    private BLLProveedor bllProveedor = new BLLProveedor();

    protected override string PatenteRequerida
    {
        get { return "GenerarOrdenCompra"; }
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

        try
        {
            List<BESugerenciaCompra> sugerencias;
            ResultadoPrediccion prediccion;
            BEOrdenCompra orden = bllOrden.GenerarPredictiva(ddlProveedor.SelectedValue, fecha, out sugerencias, out prediccion);

            gvDetalle.DataSource = sugerencias;
            gvDetalle.DataBind();
            MostrarPrediccion(prediccion);
            litIndicadores.Text = new Indicadores()
                .Agregar("Orden generada", orden.Numero, "destacado")
                .Agregar("Insumos pedidos", orden.Detalles.Count.ToString())
                .Agregar("Importe estimado", orden.ImporteEstimado.ToString("C"))
                .Agregar("Estado", BEOrdenCompra.EstadoPendiente, "aviso")
                .ToString();

            MostrarMensaje(lblMensaje,
                "Se generó la orden de compra " + orden.Numero + " para " + orden.ProveedorTexto + " con " +
                Textos.Contar(orden.Detalles.Count, "insumo", "insumos") + ", por un importe estimado de " +
                orden.ImporteEstimado.ToString("C") + ". Quedó en estado Pendiente.", false);

            ddlProveedor.SelectedIndex = 0;
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

    /// Calcula la sugerencia para mostrarla antes de generar la orden. Si no
    /// hay nada para pedir (o no se puede predecir) no se ofrece generarla.
    private void MostrarSugerencias()
    {
        litPeriodo.Text = "próximos " + BLLOrdenCompra.DiasProyeccion + " días";
        try
        {
            ResultadoPrediccion prediccion;
            List<BESugerenciaCompra> sugerencias = bllOrden.CalcularSugerencias(BLLOrdenCompra.DiasProyeccion, out prediccion);
            gvDetalle.DataSource = sugerencias;
            gvDetalle.DataBind();
            MostrarPrediccion(prediccion);

            decimal importe = 0;
            foreach (BESugerenciaCompra sugerencia in sugerencias)
            {
                importe += sugerencia.ImporteEstimado;
            }
            litIndicadores.Text = new Indicadores()
                .Agregar("Insumos para pedir", sugerencias.Count.ToString(), "destacado")
                .Agregar("Importe estimado", importe.ToString("C"))
                .Agregar("Días proyectados", prediccion.DiasProyectados.ToString())
                .Agregar("Días de ventas analizados", prediccion.DiasDeHistorial.ToString())
                .ToString();
        }
        catch (Exception ex)
        {
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
    }
}
