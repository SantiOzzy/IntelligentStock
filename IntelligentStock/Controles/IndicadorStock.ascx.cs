using System;
using System.Web.UI;

// Componente personalizado IndicadorStock.
//
// Recibe el stock actual de un insumo, su stock mínimo y la unidad de medida,
// y muestra la cantidad pintada según el estado del inventario:
//   - rojo     "CRÍTICO"     si el stock está en el mínimo o por debajo
//   - amarillo "PRECAUCIÓN"  si está a menos de un 50% por encima del mínimo
//   - verde    "ÓPTIMO"      en cualquier otro caso
// Los colores salen de las clases del diseño (Site.css, .indicador-stock),
// así el control se ve igual que el resto del sistema.
//
// Se usa dentro de las grillas, enlazando las propiedades con Eval(). Los
// valores se guardan en el ViewState para que el indicador se siga viendo bien
// después de un postback aunque la grilla no se vuelva a enlazar.
public partial class Controles_IndicadorStock : UserControl
{
    /// Margen de precaución: hasta un 50% por encima del mínimo.
    private const decimal FactorPrecaucion = 1.5m;

    public decimal StockActual
    {
        get { return ViewState["StockActual"] == null ? 0m : (decimal)ViewState["StockActual"]; }
        set { ViewState["StockActual"] = value; }
    }

    public decimal StockMinimo
    {
        get { return ViewState["StockMinimo"] == null ? 0m : (decimal)ViewState["StockMinimo"]; }
        set { ViewState["StockMinimo"] = value; }
    }

    public string UnidadMedida
    {
        get { return ViewState["UnidadMedida"] as string ?? string.Empty; }
        set { ViewState["UnidadMedida"] = value; }
    }

    // Se evalúa recién antes de dibujar, cuando la grilla ya le cargó los valores.
    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);
        EvaluarNivel();
    }

    public void EvaluarNivel()
    {
        lblCantidad.Text = StockActual.ToString("0.##");
        lblUnidad.Text = " " + UnidadMedida;

        if (StockActual <= StockMinimo)
        {
            pnlFondo.CssClass = "indicador-stock indicador-stock-critico";
            lblEstado.Text = "CRÍTICO";
        }
        else if (StockActual <= StockMinimo * FactorPrecaucion)
        {
            pnlFondo.CssClass = "indicador-stock indicador-stock-precaucion";
            lblEstado.Text = "PRECAUCIÓN";
        }
        else
        {
            pnlFondo.CssClass = "indicador-stock indicador-stock-optimo";
            lblEstado.Text = "ÓPTIMO";
        }
    }
}
