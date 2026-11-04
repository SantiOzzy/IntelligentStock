using System;
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
// el panel no se muestra. Los colores salen de las clases del diseño
// (Site.css, .impacto-economico).
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
            pnlImpacto.CssClass = "impacto-economico impacto-economico-riesgo";
        }
        else
        {
            lblTitulo.Text = "Pérdida confirmada";
            pnlImpacto.CssClass = "impacto-economico impacto-economico-perdida";
        }

        pnlImpacto.Visible = true;
    }

    public void Ocultar()
    {
        pnlImpacto.Visible = false;
    }
}
