using System;
using System.Globalization;

/// Lectura y escritura de números en los formularios.
///
/// Los campos numéricos son <input type="number">, y el navegador siempre
/// manda el valor con punto decimal ("2.5"), sin importar el idioma. El sitio
/// corre con cultura es-AR, donde el punto es separador de miles: un
/// decimal.Parse común leería "2.5" como 25. Por eso se leen acá, aceptando
/// tanto punto como coma, y se escriben siempre con punto.
public static class Numeros
{
    public static bool TryParseDecimal(string texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrEmpty(texto))
        {
            return false;
        }

        string normalizado = texto.Trim().Replace(" ", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalizado, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out valor);
    }

    /// Mismo criterio para los valores con coma flotante (temperaturas).
    public static bool TryParseDouble(string texto, out double valor)
    {
        valor = 0d;
        if (string.IsNullOrEmpty(texto))
        {
            return false;
        }

        string normalizado = texto.Trim().Replace(" ", string.Empty).Replace(',', '.');
        return double.TryParse(normalizado, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out valor);
    }

    public static bool TryParseEntero(string texto, out int valor)
    {
        valor = 0;
        return !string.IsNullOrEmpty(texto) &&
               int.TryParse(texto.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out valor);
    }

    /// Valor para cargar en un <input type="number">.
    public static string ParaInput(decimal valor)
    {
        return valor.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string ParaInput(double valor)
    {
        return valor.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
