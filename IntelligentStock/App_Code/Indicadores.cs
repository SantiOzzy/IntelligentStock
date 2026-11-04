using System.Text;
using System.Web;

/// Arma los indicadores de una pantalla: los números que la resumen (cantidad
/// de insumos, importe, etc.), que se muestran como tarjetas arriba de la grilla.
public class Indicadores
{
    private readonly StringBuilder html = new StringBuilder();

    public Indicadores Agregar(string etiqueta, string valor)
    {
        return Agregar(etiqueta, valor, null);
    }

    /// tono: null (normal), "destacado", "exito", "aviso" o "peligro".
    public Indicadores Agregar(string etiqueta, string valor, string tono)
    {
        html.Append("<div class=\"indicador").Append(string.IsNullOrEmpty(tono) ? string.Empty : " indicador-" + tono)
            .Append("\"><span class=\"indicador-valor\">").Append(HttpUtility.HtmlEncode(valor))
            .Append("</span><span class=\"indicador-etiqueta\">").Append(HttpUtility.HtmlEncode(etiqueta))
            .Append("</span></div>");
        return this;
    }

    public override string ToString()
    {
        return html.Length == 0 ? string.Empty : "<div class=\"indicadores\">" + html + "</div>";
    }
}
