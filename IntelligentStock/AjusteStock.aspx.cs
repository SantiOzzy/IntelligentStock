using System;
using BE;
using BLL;
using Servicios;

// CU-005-032 Registrar ajuste de stock.
//
// Corrige el stock de un insumo (diferencias de inventario, mermas, etc.). La
// BLL suma o resta la cantidad (R8), controla que no se descuente más de lo
// que hay (#ERR018), recalcula el dígito verificador y deja el ajuste en la
// bitácora, que es de donde sale la grilla de últimos ajustes. Si un decremento
// deja el insumo en su mínimo, el ControladorStock (patrón Observer) avisa por
// correo a los encargados y registra el quiebre de stock. Los decrementos se
// valorizan con el precio unitario del insumo y el control ImpactoEconomico
// muestra la pérdida confirmada.
public partial class AjusteStock : PaginaBase
{
    /// Cantidad de ajustes que se muestran en la grilla.
    private const int AjustesAMostrar = 20;

    private BLLInsumo bllInsumo = new BLLInsumo();

    protected override string PatenteRequerida
    {
        get { return "AjusteStock"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarUltimosAjustes();
        }
        else
        {
            // El panel de pérdida corresponde solo al ajuste recién registrado.
            ucImpacto.Ocultar();
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        ucImpacto.Ocultar();

        if (!IsValid)
        {
            return;
        }

        int idInsumo = ucBuscadorInsumo.IdInsumoSeleccionado;
        if (idInsumo == 0)
        {
            MostrarMensaje(lblMensaje, "Elija un insumo de la lista.", true);
            return;
        }

        decimal cantidad;
        if (!Numeros.TryParseDecimal(txtCantidad.Text, out cantidad))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                "La cantidad tiene que ser un número."), true);
            return;
        }

        TipoAjusteStock tipo = ddlTipo.SelectedValue == "Decremento"
            ? TipoAjusteStock.Decremento
            : TipoAjusteStock.Incremento;

        try
        {
            BEInsumo insumo = bllInsumo.AjustarStock(idInsumo, tipo, cantidad, txtMotivo.Text);

            txtCantidad.Text = string.Empty;
            txtMotivo.Text = string.Empty;

            // Se recarga la lista del buscador para que muestre el stock nuevo.
            ucBuscadorInsumo.RecargarOpciones();
            CargarUltimosAjustes();

            string mensaje = "El ajuste se registró correctamente. Stock actual de " + insumo.Nombre +
                             ": " + insumo.StockTexto + ".";

            if (tipo == TipoAjusteStock.Decremento)
            {
                ucImpacto.MostrarMonto(Math.Round(cantidad, 2) * insumo.PrecioUnitario, false,
                    Math.Round(cantidad, 2).ToString("0.##") + " " + insumo.UnidadDeMedida + " de " + insumo.Nombre);
            }

            if (tipo == TipoAjusteStock.Decremento && insumo.BajoMinimo)
            {
                mensaje += " Atención: el insumo está en su stock mínimo (" + insumo.StockMinimoTexto +
                           ") o por debajo.";
            }

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

    private void CargarUltimosAjustes()
    {
        try
        {
            gvDetalle.DataSource = bllInsumo.TraerUltimosAjustes(AjustesAMostrar);
            gvDetalle.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }
}
