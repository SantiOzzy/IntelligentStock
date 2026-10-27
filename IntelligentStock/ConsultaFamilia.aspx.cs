using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-007 Consultar familia.
// El TreeView es la forma más directa de mostrar el Composite: cada nodo se
// dibuja igual sin importar si es familia o patente, y los hijos salen solos.
public partial class ConsultaFamilia : PaginaBase
{
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "ConsultaFamilia"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarArbol();
            CargarGrilla();
            CargarRoles();
            MostrarPermisosDelRol();
            CargarFamilias();
            MostrarEstructuraDeLaFamilia();
        }
    }

    protected void ddlRoles_SelectedIndexChanged(object sender, EventArgs e)
    {
        MostrarPermisosDelRol();
    }

    protected void ddlFamiliasConsulta_SelectedIndexChanged(object sender, EventArgs e)
    {
        MostrarEstructuraDeLaFamilia();
    }

    private void CargarArbol()
    {
        try
        {
            tvFamilias.Nodes.Clear();

            foreach (Componente raiz in bllPermiso.TraerRaices())
            {
                tvFamilias.Nodes.Add(ConstruirNodo(raiz, 0));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private static TreeNode ConstruirNodo(Componente componente, int nivel)
    {
        string prefijo = componente.Tipo == 'F' ? "[F] " : "[P] ";
        TreeNode nodo = new TreeNode(prefijo + componente.Nombre, componente.Id.ToString());

        // Corte por las dudas: si quedara una relación circular grabada en la base,
        // esto evita que el recorrido no termine nunca.
        if (nivel < 10)
        {
            foreach (Componente hijo in componente.Hijos)
            {
                nodo.ChildNodes.Add(ConstruirNodo(hijo, nivel + 1));
            }
        }

        return nodo;
    }

    private void CargarGrilla()
    {
        try
        {
            gvComponentes.DataSource = bllPermiso.TraerTodos();
            gvComponentes.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarRoles()
    {
        try
        {
            ddlRoles.Items.Clear();

            foreach (BERol rol in bllPermiso.TraerRoles())
            {
                ddlRoles.Items.Add(new ListItem(rol.Nombre, rol.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarPermisosDelRol()
    {
        if (ddlRoles.Items.Count == 0)
        {
            return;
        }

        try
        {
            int idRol = int.Parse(ddlRoles.SelectedValue);

            foreach (BERol rol in bllPermiso.TraerRoles())
            {
                if (rol.Id != idRol)
                {
                    continue;
                }

                litArbolRol.Text = RenderPermisos.ArbolDelRol(rol);
                litPatentesRol.Text = RenderPermisos.PatentesEfectivas(rol);
                return;
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarFamilias()
    {
        try
        {
            ddlFamiliasConsulta.Items.Clear();

            foreach (Familia familia in bllPermiso.TraerFamilias())
            {
                ddlFamiliasConsulta.Items.Add(new ListItem(familia.Nombre, familia.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarEstructuraDeLaFamilia()
    {
        if (ddlFamiliasConsulta.Items.Count == 0)
        {
            litArbolFamilia.Text = "<em>No hay familias cargadas.</em>";
            litPatentesFamilia.Text = string.Empty;
            return;
        }

        try
        {
            int idFamilia = int.Parse(ddlFamiliasConsulta.SelectedValue);
            Componente familia = bllPermiso.ObtenerComponente(idFamilia);

            litArbolFamilia.Text = RenderPermisos.Arbol(new Componente[] { familia });
            litPatentesFamilia.Text = RenderPermisos.PatentesEfectivas(familia);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }
}
