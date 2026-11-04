using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using BE;

// Componente personalizado PermisosDelPerfil.
//
// Muestra lo que otorga un perfil (rol), separado como pide el punto 14:
//   - las familias a las que pertenece, cada una con las patentes que da
//     (incluidas las de sus subfamilias);
//   - los permisos particulares: patentes asignadas directamente al perfil,
//     fuera de cualquier familia;
//   - la cantidad total de patentes distintas que habilita.
// Lo usan Alta de usuario y Modificación de usuario (al elegir el rol) y
// Consultar familia (Permisos por perfil). La página solo le pasa el rol con
// Mostrar(rol); el armado sale del árbol del Composite de permisos.
public partial class Controles_PermisosDelPerfil : UserControl
{
    /// Muestra los permisos del rol. Con null avisa que no hay permisos.
    public void Mostrar(BERol rol)
    {
        if (rol == null)
        {
            MostrarAviso("Sin permisos asignados.");
            return;
        }

        List<Componente> familias = new List<Componente>();
        List<Patente> particulares = new List<Patente>();
        foreach (Componente componente in rol.Permisos)
        {
            if (componente.Tipo == 'F')
            {
                familias.Add(componente);
            }
            else
            {
                componente.ListarPatentes(particulares);
            }
        }

        litCantidadFamilias.Text = familias.Count.ToString(CultureInfo.InvariantCulture);
        phSinFamilias.Visible = familias.Count == 0;
        rptFamilias.DataSource = familias;
        rptFamilias.DataBind();

        litCantidadParticulares.Text = particulares.Count.ToString(CultureInfo.InvariantCulture);
        phSinParticulares.Visible = particulares.Count == 0;
        rptParticulares.DataSource = particulares;
        rptParticulares.DataBind();

        // Una patente puede llegar por más de una familia: se cuenta una vez.
        HashSet<string> distintas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Patente patente in rol.ObtenerPatentes())
        {
            distintas.Add(patente.Nombre);
        }
        litTotal.Text = distintas.Count.ToString(CultureInfo.InvariantCulture);

        lblAviso.Visible = false;
        pnlPerfil.Visible = true;
    }

    /// En lugar de los permisos muestra un aviso (por ejemplo, que no hay roles
    /// cargados o el error que devolvió la capa de negocio).
    public void MostrarAviso(string texto)
    {
        lblAviso.Text = HttpUtility.HtmlEncode(texto ?? string.Empty);
        lblAviso.Visible = true;
        pnlPerfil.Visible = false;
    }

    /// Cada familia lista sus subfamilias y todas las patentes que otorga.
    protected void rptFamilias_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        Componente familia = e.Item.DataItem as Componente;
        if (familia == null)
        {
            return;
        }

        List<string> subfamilias = new List<string>();
        foreach (Componente hijo in familia.Hijos)
        {
            if (hijo.Tipo == 'F')
            {
                subfamilias.Add(hijo.Nombre);
            }
        }

        PlaceHolder phSubfamilias = (PlaceHolder)e.Item.FindControl("phSubfamilias");
        Literal litSubfamilias = (Literal)e.Item.FindControl("litSubfamilias");
        phSubfamilias.Visible = subfamilias.Count > 0;
        litSubfamilias.Text = HttpUtility.HtmlEncode(string.Join(", ", subfamilias.ToArray()));

        List<Patente> patentes = new List<Patente>();
        familia.ListarPatentes(patentes);
        Repeater rptPatentes = (Repeater)e.Item.FindControl("rptPatentes");
        rptPatentes.DataSource = patentes;
        rptPatentes.DataBind();
    }
}
