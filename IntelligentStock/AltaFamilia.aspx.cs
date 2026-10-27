using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-004 Alta de familia.
//
// Solo se dan de alta familias. Las patentes son las funcionalidades del sistema
// y vienen cargadas con el script de la base.
//
// A medida que se tildan componentes se recalcula la composición y se avisa
// cuando un permiso queda repetido, para no armar familias con permisos
// duplicados.
public partial class AltaFamilia : PaginaBase
{
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "AltaFamilia"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarComponentes();
            RefrescarComposicion();
            CargarComponentesRol();
            RefrescarComposicionRol();
        }
    }

    protected void cblComponentes_SelectedIndexChanged(object sender, EventArgs e)
    {
        DepurarSeleccion();
        RefrescarComposicion();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        List<int> seleccionados = ObtenerSeleccionados();

        try
        {
            // La familia y sus componentes se graban juntos: si algo falla no
            // queda una familia creada a medias.
            int agregados;
            bllPermiso.Alta(txtNombre.Text, seleccionados, out agregados);

            string detalle = agregados > 0
                ? " Se le asignaron " + agregados + (agregados == 1 ? " componente." : " componentes.")
                : " Todavía no tiene componentes asignados.";

            Limpiar();
            CargarComponentes();
            RefrescarComposicion();

            // La familia nueva también tiene que aparecer para asignarla a un rol.
            CargarComponentesRol();
            RefrescarComposicionRol();

            MostrarMensaje(lblMensaje,
                "La familia se creó correctamente." + detalle, false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnLimpiar_Click(object sender, EventArgs e)
    {
        Limpiar();
        RefrescarComposicion();
        OcultarMensaje(lblMensaje);
    }

    private void CargarComponentes()
    {
        try
        {
            cblComponentes.Items.Clear();

            foreach (Componente componente in bllPermiso.TraerTodos())
            {
                string etiqueta = (componente.Tipo == 'F' ? "[Familia] " : "[Patente] ") + componente.Nombre;
                cblComponentes.Items.Add(new ListItem(etiqueta, componente.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Destilda lo que ya viene cubierto por otro componente elegido: si se tilda
    /// una familia, sus patentes sueltas se sacan; si se tilda una patente que ya
    /// está dentro de una familia elegida, se rechaza.
    private void DepurarSeleccion()
    {
        Dictionary<int, Componente> arbol;
        try
        {
            arbol = bllPermiso.ConstruirArbol();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            return;
        }

        List<Componente> familias = new List<Componente>();
        foreach (int id in ObtenerSeleccionados())
        {
            Componente componente;
            if (arbol.TryGetValue(id, out componente) && componente.Tipo == 'F')
            {
                familias.Add(componente);
            }
        }

        List<string> avisos = new List<string>();

        foreach (ListItem item in cblComponentes.Items)
        {
            if (!item.Selected)
            {
                continue;
            }

            Componente componente;
            if (!arbol.TryGetValue(int.Parse(item.Value), out componente))
            {
                continue;
            }

            foreach (Componente familia in familias)
            {
                if (familia.Id == componente.Id)
                {
                    continue;
                }

                bool cubierto = componente.Tipo == 'P'
                    ? familia.Contiene(componente.Nombre)
                    : EsSubfamilia(familia, componente);

                if (cubierto)
                {
                    item.Selected = false;
                    avisos.Add(componente.Nombre + " ya está incluido en la familia " + familia.Nombre);
                    break;
                }
            }
        }

        if (avisos.Count > 0)
        {
            MostrarMensaje(lblMensaje,
                "Se quitaron componentes repetidos: " + string.Join("; ", avisos.ToArray()) + ".", false);
        }
        else
        {
            OcultarMensaje(lblMensaje);
        }
    }

    private static bool EsSubfamilia(Componente contenedora, Componente candidata)
    {
        Familia familia = contenedora as Familia;
        Familia buscada = candidata as Familia;
        return familia != null && buscada != null && familia.ContieneFamilia(buscada);
    }

    /// Dibuja lo que va a quedar dentro de la familia y la lista plana de
    /// permisos efectivos, para que se vea el efecto del Composite antes de guardar.
    private void RefrescarComposicion()
    {
        Dictionary<int, Componente> arbol;
        try
        {
            arbol = bllPermiso.ConstruirArbol();
        }
        catch (Exception ex)
        {
            litComposicion.Text = "<em>" + HttpUtility.HtmlEncode(ex.Message) + "</em>";
            litEfectivos.Text = string.Empty;
            return;
        }

        List<Componente> elegidos = new List<Componente>();
        foreach (int id in ObtenerSeleccionados())
        {
            Componente componente;
            if (arbol.TryGetValue(id, out componente))
            {
                elegidos.Add(componente);
            }
        }

        if (elegidos.Count == 0)
        {
            litComposicion.Text = "<em>Todavía no se eligió ningún componente.</em>";
            litEfectivos.Text = "<em>Sin permisos.</em>";
            return;
        }

        litComposicion.Text = RenderPermisos.Arbol(elegidos);

        List<Patente> patentes = new List<Patente>();
        foreach (Componente componente in elegidos)
        {
            componente.ListarPatentes(patentes);
        }

        StringBuilder html = new StringBuilder();
        html.Append("<p>");
        foreach (Patente patente in patentes)
        {
            html.Append("<span class=\"label label-default\" style=\"margin-right:4px; display:inline-block; margin-bottom:4px\">");
            html.Append(HttpUtility.HtmlEncode(patente.Nombre));
            html.Append("</span>");
        }
        html.Append("</p><p class=\"help-block\">Total: " + patentes.Count + " permisos efectivos.</p>");

        litEfectivos.Text = html.ToString();
    }

    private List<int> ObtenerSeleccionados()
    {
        List<int> ids = new List<int>();
        foreach (ListItem item in cblComponentes.Items)
        {
            if (item.Selected)
            {
                ids.Add(int.Parse(item.Value));
            }
        }
        return ids;
    }

    private void Limpiar()
    {
        txtNombre.Text = string.Empty;
        foreach (ListItem item in cblComponentes.Items)
        {
            item.Selected = false;
        }
    }

    // ---- Alta de rol. Misma lógica que la de familia, sobre otro juego de controles. ----

    protected void cblComponentesRol_SelectedIndexChanged(object sender, EventArgs e)
    {
        DepurarSeleccionRol();
        RefrescarComposicionRol();
    }

    protected void btnAceptarRol_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        List<int> seleccionados = ObtenerSeleccionadosRol();

        try
        {
            // El rol y sus permisos se graban juntos: si algo falla no queda un
            // rol creado a medias.
            int agregados;
            bllPermiso.AltaRol(txtNombreRol.Text, seleccionados, out agregados);

            string detalle = agregados > 0
                ? " Se le asignaron " + agregados + (agregados == 1 ? " componente." : " componentes.")
                : " Todavía no tiene componentes asignados.";

            LimpiarRol();
            CargarComponentesRol();
            RefrescarComposicionRol();

            MostrarMensaje(lblMensaje, "El rol se creó correctamente." + detalle, false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnLimpiarRol_Click(object sender, EventArgs e)
    {
        LimpiarRol();
        RefrescarComposicionRol();
        OcultarMensaje(lblMensaje);
    }

    private void CargarComponentesRol()
    {
        try
        {
            cblComponentesRol.Items.Clear();

            foreach (Componente componente in bllPermiso.TraerTodos())
            {
                string etiqueta = (componente.Tipo == 'F' ? "[Familia] " : "[Patente] ") + componente.Nombre;
                cblComponentesRol.Items.Add(new ListItem(etiqueta, componente.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void DepurarSeleccionRol()
    {
        Dictionary<int, Componente> arbol;
        try
        {
            arbol = bllPermiso.ConstruirArbol();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            return;
        }

        List<Componente> familias = new List<Componente>();
        foreach (int id in ObtenerSeleccionadosRol())
        {
            Componente componente;
            if (arbol.TryGetValue(id, out componente) && componente.Tipo == 'F')
            {
                familias.Add(componente);
            }
        }

        List<string> avisos = new List<string>();

        foreach (ListItem item in cblComponentesRol.Items)
        {
            if (!item.Selected)
            {
                continue;
            }

            Componente componente;
            if (!arbol.TryGetValue(int.Parse(item.Value), out componente))
            {
                continue;
            }

            foreach (Componente familia in familias)
            {
                if (familia.Id == componente.Id)
                {
                    continue;
                }

                bool cubierto = componente.Tipo == 'P'
                    ? familia.Contiene(componente.Nombre)
                    : EsSubfamilia(familia, componente);

                if (cubierto)
                {
                    item.Selected = false;
                    avisos.Add(componente.Nombre + " ya está incluido en la familia " + familia.Nombre);
                    break;
                }
            }
        }

        if (avisos.Count > 0)
        {
            MostrarMensaje(lblMensaje,
                "Se quitaron componentes repetidos: " + string.Join("; ", avisos.ToArray()) + ".", false);
        }
        else
        {
            OcultarMensaje(lblMensaje);
        }
    }

    private void RefrescarComposicionRol()
    {
        Dictionary<int, Componente> arbol;
        try
        {
            arbol = bllPermiso.ConstruirArbol();
        }
        catch (Exception ex)
        {
            litComposicionRol.Text = "<em>" + HttpUtility.HtmlEncode(ex.Message) + "</em>";
            litEfectivosRol.Text = string.Empty;
            return;
        }

        List<Componente> elegidos = new List<Componente>();
        foreach (int id in ObtenerSeleccionadosRol())
        {
            Componente componente;
            if (arbol.TryGetValue(id, out componente))
            {
                elegidos.Add(componente);
            }
        }

        if (elegidos.Count == 0)
        {
            litComposicionRol.Text = "<em>Todavía no se eligió ningún componente.</em>";
            litEfectivosRol.Text = "<em>Sin permisos.</em>";
            return;
        }

        litComposicionRol.Text = RenderPermisos.Arbol(elegidos);

        List<Patente> patentes = new List<Patente>();
        foreach (Componente componente in elegidos)
        {
            componente.ListarPatentes(patentes);
        }

        StringBuilder html = new StringBuilder();
        html.Append("<p>");
        foreach (Patente patente in patentes)
        {
            html.Append("<span class=\"label label-default\" style=\"margin-right:4px; display:inline-block; margin-bottom:4px\">");
            html.Append(HttpUtility.HtmlEncode(patente.Nombre));
            html.Append("</span>");
        }
        html.Append("</p><p class=\"help-block\">Total: " + patentes.Count + " permisos efectivos.</p>");

        litEfectivosRol.Text = html.ToString();
    }

    private List<int> ObtenerSeleccionadosRol()
    {
        List<int> ids = new List<int>();
        foreach (ListItem item in cblComponentesRol.Items)
        {
            if (item.Selected)
            {
                ids.Add(int.Parse(item.Value));
            }
        }
        return ids;
    }

    private void LimpiarRol()
    {
        txtNombreRol.Text = string.Empty;
        foreach (ListItem item in cblComponentesRol.Items)
        {
            item.Selected = false;
        }
    }
}
