using System;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-006 Baja de familia.
public partial class BajaFamilia : PaginaBase
{
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "BajaFamilia"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarFamilias();
            CargarRolesBaja();
        }
    }

    protected void ddlFamilias_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        MostrarDetalle();
    }

    protected void btnEliminar_Click(object sender, EventArgs e)
    {
        try
        {
            // La BLL borra primero las relaciones y las asignaciones a roles, y
            // recalcula el DV de las tres tablas afectadas.
            bllPermiso.Baja(IdSeleccionado());

            CargarFamilias();
            pnlDetalle.Visible = false;
            MostrarMensaje(lblMensaje, "La familia se eliminó correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        ddlFamilias.SelectedIndex = 0;
        pnlDetalle.Visible = false;
        OcultarMensaje(lblMensaje);
    }

    private void CargarFamilias()
    {
        try
        {
            ddlFamilias.Items.Clear();
            ddlFamilias.Items.Add(new ListItem("(seleccione una familia)", "0"));

            foreach (Familia familia in bllPermiso.TraerFamilias())
            {
                ddlFamilias.Items.Add(new ListItem(familia.Nombre, familia.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarDetalle()
    {
        int id = IdSeleccionado();

        if (id == 0)
        {
            pnlDetalle.Visible = false;
            return;
        }

        try
        {
            Componente componente = bllPermiso.ObtenerComponente(id);

            if (componente == null)
            {
                pnlDetalle.Visible = false;
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            pnlDetalle.Visible = true;
            litUso.Text = bllPermiso.DescribirUso(id);
            litArbol.Text = RenderPermisos.Arbol(new Componente[] { componente });
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private int IdSeleccionado()
    {
        int id;
        return int.TryParse(ddlFamilias.SelectedValue, out id) ? id : 0;
    }

    // ---- Baja de rol. ----

    protected void ddlRolesBaja_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        MostrarDetalleRol();
    }

    protected void btnEliminarRolBaja_Click(object sender, EventArgs e)
    {
        try
        {
            bllPermiso.BajaRol(IdRolBajaSeleccionado());

            CargarRolesBaja();
            pnlDetalleRolBaja.Visible = false;
            MostrarMensaje(lblMensaje, "El rol se eliminó correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnCancelarRolBaja_Click(object sender, EventArgs e)
    {
        ddlRolesBaja.SelectedIndex = 0;
        pnlDetalleRolBaja.Visible = false;
        OcultarMensaje(lblMensaje);
    }

    private void CargarRolesBaja()
    {
        try
        {
            ddlRolesBaja.Items.Clear();
            ddlRolesBaja.Items.Add(new ListItem("(seleccione un rol)", "0"));

            foreach (BERol rol in bllPermiso.TraerRoles())
            {
                ddlRolesBaja.Items.Add(new ListItem(rol.Nombre, rol.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarDetalleRol()
    {
        int idRol = IdRolBajaSeleccionado();

        if (idRol == 0)
        {
            pnlDetalleRolBaja.Visible = false;
            return;
        }

        try
        {
            BERol rol = bllPermiso.ObtenerRol(idRol);

            if (rol == null)
            {
                pnlDetalleRolBaja.Visible = false;
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            pnlDetalleRolBaja.Visible = true;
            litArbolRolBaja.Text = RenderPermisos.ArbolDelRol(rol);

            bool esWebMaster = string.Equals(rol.Nombre, "WebMaster", StringComparison.OrdinalIgnoreCase);
            BEUsuario actual = SessionManager.UsuarioActual;
            bool esRolPropio = actual != null && actual.IdRol == idRol;

            btnEliminarRolBaja.Enabled = !esWebMaster && !esRolPropio;

            if (esWebMaster)
            {
                litUsoRolBaja.Text = "El rol WebMaster es parte del sistema y no se puede eliminar.";
            }
            else if (esRolPropio)
            {
                litUsoRolBaja.Text = "No puede eliminar el rol de su propia cuenta.";
            }
            else
            {
                litUsoRolBaja.Text = bllPermiso.DescribirUsoDeRol(idRol);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private int IdRolBajaSeleccionado()
    {
        int id;
        return int.TryParse(ddlRolesBaja.SelectedValue, out id) ? id : 0;
    }
}
