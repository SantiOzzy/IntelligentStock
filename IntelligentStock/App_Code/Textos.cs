using System;

/// Ayudas para armar los textos que se muestran en pantalla.
public static class Textos
{
    /// Cantidad con el sustantivo en singular o plural, por ejemplo
    /// Contar(1, "proveedor encontrado", "proveedores encontrados") devuelve
    /// "1 proveedor encontrado".
    public static string Contar(int cantidad, string singular, string plural)
    {
        return cantidad + " " + (cantidad == 1 ? singular : plural);
    }
}
