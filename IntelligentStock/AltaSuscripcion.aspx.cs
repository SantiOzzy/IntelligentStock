using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-008-044 Alta de suscripción: la compra de un plan de IntelligentStock.
// El cliente elige el plan y los meses que paga por adelantado, carga sus
// datos de facturación y paga al contado o con tarjeta de crédito. La pantalla
// muestra el cálculo (subtotal, descuento, IVA, total y cuotas) antes de
// confirmar. La BLL valida la tarjeta (dígito de control, vencimiento, código
// de seguridad), cobra con la pasarela simulada (si la rechaza: #ERR024),
// guarda el token encriptado con AES-256 (R6), recalcula el dígito verificador,
// pide el CAE, genera la factura A o B en PDF, la registra en la bitácora y la
// manda por correo. La factura se puede descargar y reenviar desde acá.
public partial class AltaSuscripcion : PaginaBase
{
    private BLLSuscripcion bllSuscripcion = new BLLSuscripcion();

    private static readonly CultureInfo Pesos = CultureInfo.GetCultureInfo("es-AR");

    protected override string PatenteRequerida
    {
        get { return "AltaSuscripcion"; }
    }

    /// La última compra hecha en la pantalla (para descargar o reenviar la
    /// factura). No tiene datos de la tarjeta.
    private BECompraPlan UltimaCompra
    {
        get { return ViewState["compra"] as BECompraPlan; }
        set { ViewState["compra"] = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidadorDeRango.CargarFechas(rvFechaInicio, DateTime.Today,
            DateTime.Today.AddDays(BLLSuscripcion.DiasMaximosHastaElInicio));

        if (!IsPostBack)
        {
            CargarListas();
            txtFechaInicio.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (UsuarioActual != null)
            {
                txtCorreo.Text = UsuarioActual.CorreoElectronico;
            }
            Recalcular(this, EventArgs.Empty);
        }
    }

    private void CargarListas()
    {
        ddlPlan.Items.Clear();
        foreach (KeyValuePair<string, decimal> plan in BLLSuscripcion.TraerPlanes())
        {
            ddlPlan.Items.Add(new ListItem(plan.Key + " ($ " + plan.Value.ToString("N0", Pesos) + " por mes + IVA)", plan.Key));
        }

        ddlMeses.Items.Clear();
        foreach (int meses in BLLSuscripcion.MesesPermitidos)
        {
            decimal descuento = BLLSuscripcion.DescuentoPorMeses(meses);
            string texto = meses == 1 ? "1 mes" : meses + " meses";
            if (descuento > 0)
            {
                texto += " (" + descuento.ToString("0.##", Pesos) + " % de descuento)";
            }
            ddlMeses.Items.Add(new ListItem(texto, meses.ToString(CultureInfo.InvariantCulture)));
        }

        ddlCondicionIva.Items.Clear();
        foreach (string condicion in BLLSuscripcion.CondicionesIva)
        {
            ddlCondicionIva.Items.Add(new ListItem(condicion, condicion));
        }

        rblMedioPago.Items.Clear();
        rblMedioPago.Items.Add(new ListItem("Contado (efectivo o transferencia)", BECompraPlan.MedioContado));
        rblMedioPago.Items.Add(new ListItem("Tarjeta de crédito", BECompraPlan.MedioTarjeta));
        rblMedioPago.SelectedIndex = 0;

        ddlCuotas.Items.Clear();
        foreach (int cuotas in BLLSuscripcion.CuotasPermitidas)
        {
            ddlCuotas.Items.Add(new ListItem(cuotas == 1 ? "1 pago" : cuotas + " cuotas sin interés",
                                             cuotas.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private bool ConTarjeta
    {
        get { return rblMedioPago.SelectedValue == BECompraPlan.MedioTarjeta; }
    }

    /// Cada vez que cambia el plan, los meses, la condición frente al IVA, el
    /// medio de pago o las cuotas se vuelve a mostrar el cálculo.
    protected void Recalcular(object sender, EventArgs e)
    {
        pnlTarjeta.Visible = ConTarjeta;
        pnlContado.Visible = !ConTarjeta;

        BECompraPlan compra;
        try
        {
            compra = BLLSuscripcion.Calcular(ddlPlan.SelectedValue, LeerEntero(ddlMeses.SelectedValue));
        }
        catch (Exception ex)
        {
            litResumen.Text = string.Empty;
            MostrarMensaje(lblMensaje, ex.Message, true);
            return;
        }

        compra.CondicionIva = ddlCondicionIva.SelectedValue;
        compra.MedioPago = rblMedioPago.SelectedValue;
        compra.Cuotas = ConTarjeta ? Math.Max(1, LeerEntero(ddlCuotas.SelectedValue)) : 1;

        StringBuilder html = new StringBuilder();
        html.Append("<table class=\"table table-condensed resumen-compra\"><tbody>");
        Fila(html, "Plan", compra.Plan, false);
        Fila(html, "Precio mensual (sin IVA)", Importe(compra.PrecioMensual), false);
        Fila(html, "Meses", compra.Meses.ToString(CultureInfo.InvariantCulture), false);
        Fila(html, "Subtotal", Importe(compra.Subtotal), false);
        Fila(html, "Descuento por pago adelantado (" + compra.PorcentajeDescuento.ToString("0.##", Pesos) + " %)",
             compra.Descuento > 0 ? "- " + Importe(compra.Descuento) : Importe(0m), false);
        Fila(html, "Neto gravado", Importe(compra.NetoGravado), false);
        Fila(html, "IVA 21 %", Importe(compra.Iva), false);
        Fila(html, "Total", Importe(compra.Total), true);
        if (compra.Cuotas > 1)
        {
            Fila(html, compra.Cuotas + " cuotas sin interés de", Importe(compra.ImporteCuota), false);
        }
        html.Append("</tbody></table>");
        html.Append("<p class=\"campo-ayuda\">Se emite factura ").Append(compra.TipoFactura)
            .Append(compra.TipoFactura == "A"
                ? " (con el IVA discriminado)."
                : " (el IVA va incluido en el precio y se informa como IVA contenido).")
            .Append(" El plan queda pagado hasta el ")
            .Append(LeerFecha().AddMonths(compra.Meses).AddDays(-1).ToString("dd/MM/yyyy"))
            .Append(".</p>");

        litResumen.Text = html.ToString();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        Recalcular(sender, e);
        if (!IsValid)
        {
            return;
        }

        BECompraPlan pedido = new BECompraPlan();
        pedido.Plan = ddlPlan.SelectedValue;
        pedido.Meses = LeerEntero(ddlMeses.SelectedValue);
        pedido.FechaInicio = LeerFecha();
        pedido.RazonSocial = txtRazonSocial.Text;
        pedido.DocumentoCliente = txtDocumento.Text;
        pedido.CondicionIva = ddlCondicionIva.SelectedValue;
        pedido.Domicilio = txtDomicilio.Text;
        pedido.Correo = txtCorreo.Text;
        pedido.MedioPago = rblMedioPago.SelectedValue;
        pedido.Cuotas = ConTarjeta ? LeerEntero(ddlCuotas.SelectedValue) : 1;
        pedido.TitularTarjeta = ConTarjeta ? txtTitular.Text : null;

        try
        {
            BECompraPlan compra = bllSuscripcion.Comprar(pedido,
                ConTarjeta ? txtNumeroTarjeta.Text : null,
                ConTarjeta ? txtVencimiento.Text : null,
                ConTarjeta ? txtCodigo.Text : null);

            UltimaCompra = compra;
            LimpiarTarjeta();
            MostrarResultado(compra);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarResultado(BECompraPlan compra)
    {
        pnlCompra.Visible = false;
        pnlResultado.Visible = true;

        StringBuilder html = new StringBuilder();
        html.Append(new Indicadores()
            .Agregar("Factura " + compra.TipoFactura, compra.NumeroFactura, "destacado")
            .Agregar("Total", Importe(compra.Total))
            .Agregar("Suscripción N°", compra.IdSuscripcion.ToString(CultureInfo.InvariantCulture))
            .Agregar("Pagado hasta", compra.PagadoHasta.AddDays(-1).ToString("dd/MM/yyyy")));

        html.Append("<div class=\"resumen-caja\"><table class=\"table table-condensed resumen-compra\"><tbody>");
        FilaTexto(html, "Plan", compra.Plan + " por " + compra.Meses + (compra.Meses == 1 ? " mes" : " meses") +
                                ", desde el " + compra.FechaInicio.ToString("dd/MM/yyyy"), false);
        FilaTexto(html, "Cliente", compra.RazonSocial + " (" + compra.DocumentoCliente + ")", true);
        FilaTexto(html, "Condición frente al IVA", compra.CondicionIva, false);
        FilaTexto(html, "Forma de pago", compra.DescripcionMedioPago, false);
        if (!string.IsNullOrEmpty(compra.CodigoTransaccion))
        {
            FilaTexto(html, "Transacción de la pasarela", compra.CodigoTransaccion, true);
        }
        FilaTexto(html, "CAE", compra.Cae + " (vence el " + compra.VencimientoCae.ToString("dd/MM/yyyy") + ")", false);
        html.Append("</tbody></table></div>");
        litResultado.Text = html.ToString();

        if (compra.FacturaEnviada)
        {
            MostrarMensaje(lblMensaje, "La compra quedó registrada y la factura " + compra.TipoFactura + " " +
                compra.NumeroFactura + " se mandó a " + compra.Correo + ".", false);
        }
        else
        {
            MostrarMensaje(lblMensaje, "La compra quedó registrada, pero la factura no se pudo mandar por correo (" +
                compra.MotivoNoEnviada + "). Descárguela con el botón de abajo o vuelva a intentarlo con " +
                "Reenviar por correo cuando haya conexión.", true);
        }
    }

    protected void btnDescargar_Click(object sender, EventArgs e)
    {
        BECompraPlan compra = UltimaCompra;
        byte[] pdf = compra == null ? null : bllSuscripcion.LeerFactura(compra.ArchivoFactura);
        if (pdf == null)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR013", "No se encontró el archivo de la factura."), true);
            return;
        }

        Response.Clear();
        Response.ContentType = "application/pdf";
        Response.AppendHeader("Content-Disposition", "attachment; filename=" + compra.ArchivoFactura);
        Response.BinaryWrite(pdf);
        Response.Flush();
        Response.SuppressContent = true;
        Context.ApplicationInstance.CompleteRequest();
    }

    protected void btnReenviar_Click(object sender, EventArgs e)
    {
        BECompraPlan compra = UltimaCompra;
        if (compra == null)
        {
            return;
        }

        bllSuscripcion.EnviarFactura(compra);
        UltimaCompra = compra;
        MostrarResultado(compra);
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private void LimpiarTarjeta()
    {
        txtNumeroTarjeta.Text = string.Empty;
        txtVencimiento.Text = string.Empty;
        txtCodigo.Text = string.Empty;
        txtTitular.Text = string.Empty;
    }

    /// El navegador manda la fecha como aaaa-mm-dd, sin importar el idioma.
    private DateTime LeerFecha()
    {
        DateTime fecha;
        return DateTime.TryParseExact(txtFechaInicio.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out fecha)
            ? fecha
            : DateTime.Today;
    }

    private static int LeerEntero(string texto)
    {
        int valor;
        return int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out valor) ? valor : 0;
    }

    private static string Importe(decimal valor)
    {
        return "$ " + valor.ToString("N2", Pesos);
    }

    private static void Fila(StringBuilder html, string etiqueta, string valor, bool total)
    {
        html.Append(total ? "<tr class=\"resumen-total\">" : "<tr>")
            .Append("<th>").Append(HttpUtility.HtmlEncode(etiqueta)).Append("</th>")
            .Append("<td class=\"num\">").Append(HttpUtility.HtmlEncode(valor)).Append("</td></tr>");
    }

    /// Fila de texto. Los datos que cargó el cliente (razón social, documento)
    /// no se traducen: el traductor cambiaba nombres propios.
    private static void FilaTexto(StringBuilder html, string etiqueta, string valor, bool sinTraducir)
    {
        html.Append("<tr><th>").Append(HttpUtility.HtmlEncode(etiqueta)).Append("</th><td")
            .Append(sinTraducir ? " translate=\"no\"" : string.Empty).Append(">")
            .Append(HttpUtility.HtmlEncode(valor)).Append("</td></tr>");
    }
}
