using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using BE;

/// Utilidades de presentacion para dibujar el arbol del Composite.
/// Se usa en el alta y la modificacion de usuario (para mostrar que otorga el rol),
/// y en las consultas de familias y patentes.
public static class RenderPermisos
{
    // Recorre el arbol en profundidad. Como Familia y Patente comparten la
    // interfaz Componente, el metodo no necesita preguntar de que tipo es cada nodo
    // salvo para elegir el icono.
    public static string Arbol(IEnumerable<Componente> componentes)
    {
        StringBuilder html = new StringBuilder();
        bool hayAlgo = false;

        html.Append("<ul class=\"list-unstyled\">");

        foreach (Componente componente in componentes)
        {
            hayAlgo = true;
            Nodo(html, componente, 0);
        }

        html.Append("</ul>");

        return hayAlgo ? html.ToString() : "<em>Sin permisos asignados.</em>";
    }

    public static string ArbolDelRol(BERol rol)
    {
        if (rol == null)
        {
            return "<em>Sin permisos asignados.</em>";
        }
        return Arbol(rol.Permisos);
    }

    /// Lista plana de las patentes efectivas, util cuando interesa el resultado
    /// y no la estructura que lo produjo.
    public static string PatentesEfectivas(BERol rol)
    {
        return rol == null ? "<em>Sin permisos.</em>" : RenderizarPatentes(rol.ObtenerPatentes());
    }

    /// Lista plana de las patentes efectivas de una familia (o de cualquier componente).
    public static string PatentesEfectivas(Componente componente)
    {
        if (componente == null)
        {
            return "<em>Sin permisos.</em>";
        }

        List<Patente> patentes = new List<Patente>();
        componente.ListarPatentes(patentes);
        return RenderizarPatentes(patentes);
    }

    private static string RenderizarPatentes(IList<Patente> patentes)
    {
        if (patentes == null || patentes.Count == 0)
        {
            return "<em>Sin permisos.</em>";
        }

        StringBuilder html = new StringBuilder();
        foreach (Patente patente in patentes)
        {
            html.Append("<span class=\"label label-default\" style=\"margin-right:4px; display:inline-block; margin-bottom:4px\">");
            html.Append(HttpUtility.HtmlEncode(patente.Nombre));
            html.Append("</span>");
        }
        return html.ToString();
    }

    private static void Nodo(StringBuilder html, Componente componente, int nivel)
    {
        // Tope de seguridad: si alguna vez quedara un ciclo grabado en
        // RelacionFamilia, esto evita que se cuelgue el render.
        if (nivel > 10)
        {
            return;
        }

        string etiqueta = componente.Tipo == 'F' ? "label-primary" : "label-default";
        string tipo = componente.Tipo == 'F' ? "Familia" : "Patente";

        html.Append("<li style=\"padding-left:");
        html.Append(nivel * 18);
        html.Append("px\">");
        html.Append("<span class=\"label ");
        html.Append(etiqueta);
        html.Append("\">");
        html.Append(tipo);
        html.Append("</span> ");
        html.Append(HttpUtility.HtmlEncode(componente.Nombre));
        html.Append("</li>");

        foreach (Componente hijo in componente.Hijos)
        {
            Nodo(html, hijo, nivel + 1);
        }
    }
}
