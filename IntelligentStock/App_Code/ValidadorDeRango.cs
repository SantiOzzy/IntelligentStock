using System;
using System.Globalization;
using System.Threading;
using System.Web.UI;
using System.Web.UI.WebControls;

/// RangeValidator de todo el sitio. En el Web.config (pages/tagMapping) cada
/// <asp:RangeValidator> de las páginas se crea como esta clase, así que en el
/// markup se sigue escribiendo el RangeValidator estándar.
///
/// Los campos numéricos son <input type="number">, que el navegador manda
/// siempre con punto decimal ("2.5"). El sitio corre con cultura es-AR, donde
/// el punto es separador de miles, y el RangeValidator estándar de tipo Double
/// o Currency rechazaría "2.5" tanto en el navegador como en el servidor. Para
/// esos dos tipos se valida con el formato invariante (punto decimal), el
/// mismo criterio que usa Numeros para leer los valores. Los rangos de
/// enteros y de fechas funcionan igual que en el RangeValidator estándar.
public class ValidadorDeRango : RangeValidator
{
    private bool ConPuntoDecimal
    {
        get { return Type == ValidationDataType.Double || Type == ValidationDataType.Currency; }
    }

    /// Atributos que lee la validación del navegador (separador decimal,
    /// mínimo y máximo).
    protected override void AddAttributesToRender(HtmlTextWriter writer)
    {
        EnCulturaInvariante(delegate { base.AddAttributesToRender(writer); return true; });
    }

    protected override bool ControlPropertiesValid()
    {
        return EnCulturaInvariante(delegate { return base.ControlPropertiesValid(); });
    }

    /// Validación del lado del servidor (se repite aunque el navegador ya
    /// haya validado: el pedido se puede armar a mano).
    protected override bool EvaluateIsValid()
    {
        return EnCulturaInvariante(delegate { return base.EvaluateIsValid(); });
    }

    /// Rango de fechas que depende del día de hoy (por ejemplo, una orden de
    /// compra: de hoy a 60 días adelante). Las páginas lo cargan en cada
    /// pedido, con el formato año-mes-día que mandan los campos de fecha.
    public static void CargarFechas(RangeValidator validador, DateTime desde, DateTime hasta)
    {
        validador.Type = ValidationDataType.Date;
        validador.MinimumValue = desde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        validador.MaximumValue = hasta.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private bool EnCulturaInvariante(Func<bool> accion)
    {
        if (!ConPuntoDecimal)
        {
            return accion();
        }

        CultureInfo anterior = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            return accion();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = anterior;
        }
    }
}
