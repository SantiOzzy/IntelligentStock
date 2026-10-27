using System;
using System.Drawing;
using System.Web.UI;

// Componente personalizado ImpactoEconomico.
//
// Muestra en pesos lo que cuesta (o puede costar) una contingencia con la
// mercadería. Cambia según la pantalla que lo usa:
//   - "Valor en riesgo" (naranja): al procesar una alerta térmica, lo que se
//     perdería si la mercadería no se traslada a tiempo.
//   - "Pérdida confirmada" (rojo oscuro): al registrar un ajuste de stock
//     negativo, lo que ya se perdió.
// Si el monto es cero (por ejemplo, el insumo todavía no tiene precio cargado)
// el panel no se muestra.
public partial class Controles_ImpactoEconomico : UserControl
{
    /// Cantidad afectada por el precio unitario.
    public void CalcularImpacto(decimal cantidadAfectada, decimal precioUnitario, bool esRiesgo)
    {
        if (cantidadAfectada > 0 && precioUnitario > 0)
        {
            MostrarMonto(cantidadAfectada * precioUnitario, esRiesgo, string.Empty);
        }
        else
        {
            Ocultar();
        }
    }

    /// Para cuando el monto ya viene calculado (por ejemplo, la suma de varios
    /// insumos). El detalle es una aclaración corta que va debajo del monto.
    public void MostrarMonto(decimal monto, bool esRiesgo, string detalle)
    {
        if (monto <= 0)
        {
            Ocultar();
            return;
        }

        lblMonto.Text = monto.ToString("C");
        lblDetalle.Text = detalle ?? string.Empty;

        if (esRiesgo)
        {
            lblTitulo.Text = "Valor en riesgo";
            lblTitulo.ForeColor = Color.DarkGoldenrod;
            pnlImpacto.BackColor = Color.LightYellow;
            pnlImpacto.BorderColor = Color.Khaki;
            lblMonto.ForeColor = Color.DarkOrange;
            lblDetalle.ForeColor = Color.DarkGoldenrod;
        }
        else
        {
            lblTitulo.Text = "Pérdida confirmada";
            lblTitulo.ForeColor = ColorTranslator.FromHtml("#721c24");
            pnlImpacto.BackColor = ColorTranslator.FromHtml("#f8d7da");
            pnlImpacto.BorderColor = ColorTranslator.FromHtml("#f5c6cb");
            lblMonto.ForeColor = ColorTranslator.FromHtml("#721c24");
            lblDetalle.ForeColor = ColorTranslator.FromHtml("#721c24");
        }

        pnlImpacto.Visible = true;
    }

    public void Ocultar()
    {
        pnlImpacto.Visible = false;
    }
}
