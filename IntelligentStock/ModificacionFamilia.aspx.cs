using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-005 Modificar familia.
// Permite renombrarla y, sobre todo, cambiar qué componentes cuelgan de ella.
// Como una familia puede contener otras familias, acá es donde realmente se
// arma el árbol del Composite.
public partial class ModificacionFamilia : PaginaBase
{
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "ModificacionFamilia"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarFamilias();
            CargarRolesMod();
        }
    }

    protected void ddlFamilias_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarFamiliaSeleccionada();
    }

    protected void btnRenombrar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            bllPermiso.Modificar(IdSeleccionado(), txtNombre.Text);

            CargarFamilias();
            SeleccionarEnLista(IdSeleccionado());
            CargarFamiliaSeleccionada();

            MostrarMensaje(lblMensaje, "El nombre de la familia se actualizó.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnAgregar_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);

        int idPadre = IdSeleccionado();

        if (EsComponenteDelRolActual(idPadre))
        {
            MostrarMensaje(lblMensaje,
                "No puede modificar la composición de una familia que forma parte de los " +
                "permisos de su propio rol actual.", true);
            return;
        }

        List<int> elegidos = new List<int>();
        foreach (ListItem item in lstDisponibles.Items)
        {
            if (item.Selected)
            {
                elegidos.Add(int.Parse(item.Value));
            }
        }

        if (elegidos.Count == 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione al menos un componente de la lista de disponibles.", true);
            return;
        }

        // Se procesan de a uno para poder informar cuáles entraron y cuáles se
        // rechazaron por estar repetidos, sin cortar en el primer problema.
        List<string> hechos = new List<string>();
        List<string> rechazados = new List<string>();

        foreach (int idHijo in elegidos)
        {
            try
            {
                hechos.Add(bllPermiso.AgregarHijo(idPadre, idHijo));
            }
            catch (Exception ex)
            {
                rechazados.Add(ex.Message);
            }
        }

        CargarFamiliaSeleccionada();

        if (hechos.Count == 0)
        {
            MostrarMensaje(lblMensaje, string.Join(" ", rechazados.ToArray()), true);
            return;
        }

        string mensaje = "Se agregaron " + hechos.Count + " componente(s) a la familia. " +
                         string.Join(" ", hechos.ToArray());
        if (rechazados.Count > 0)
        {
            mensaje += " No se agregaron " + rechazados.Count + ": " +
                       string.Join(" ", rechazados.ToArray());
        }

        MostrarMensaje(lblMensaje, mensaje, rechazados.Count > 0);
    }

    protected void btnQuitar_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);

        int idPadre = IdSeleccionado();

        if (EsComponenteDelRolActual(idPadre))
        {
            MostrarMensaje(lblMensaje,
                "No puede modificar la composición de una familia que forma parte de los " +
                "permisos de su propio rol actual.", true);
            return;
        }

        int quitados = 0;

        try
        {
            foreach (ListItem item in lstAsignados.Items)
            {
                if (item.Selected)
                {
                    bllPermiso.QuitarHijo(idPadre, int.Parse(item.Value));
                    quitados++;
                }
            }

            if (quitados == 0)
            {
                MostrarMensaje(lblMensaje, "Seleccione al menos un componente de la familia.", true);
                return;
            }

            CargarFamiliaSeleccionada();
            MostrarMensaje(lblMensaje,
                "Se quitaron " + quitados + " componente(s) de la familia.", false);
        }
        catch (Exception ex)
        {
            CargarFamiliaSeleccionada();
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarFamilias()
    {
        try
        {
            int seleccionado = IdSeleccionado();

            ddlFamilias.Items.Clear();
            ddlFamilias.Items.Add(new ListItem("(seleccione una familia)", "0"));

            foreach (Familia familia in bllPermiso.TraerFamilias())
            {
                ddlFamilias.Items.Add(new ListItem(familia.Nombre, familia.Id.ToString()));
            }

            if (seleccionado > 0)
            {
                SeleccionarEnLista(seleccionado);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarFamiliaSeleccionada()
    {
        int id = IdSeleccionado();

        if (id == 0)
        {
            pnlEdicion.Visible = false;
            return;
        }

        try
        {
            Componente componente = bllPermiso.ObtenerComponente(id);

            if (componente == null)
            {
                pnlEdicion.Visible = false;
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            pnlEdicion.Visible = true;
            txtNombre.Text = componente.Nombre;

            CargarListas(componente);
            litArbol.Text = RenderPermisos.Arbol(new Componente[] { componente });
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // Arma las dos listas: a la izquierda lo que se puede agregar, a la derecha
    // lo que ya cuelga de la familia. Se excluye a la familia misma para no
    // ofrecer que se contenga a sí misma.
    private void CargarListas(Componente familia)
    {
        List<int> yaAsignados = new List<int>();

        lstAsignados.Items.Clear();
        foreach (Componente hijo in familia.Hijos)
        {
            yaAsignados.Add(hijo.Id);
            lstAsignados.Items.Add(new ListItem(Etiquetar(hijo), hijo.Id.ToString()));
        }

        lstDisponibles.Items.Clear();
        foreach (Componente componente in bllPermiso.TraerTodos())
        {
            if (componente.Id == familia.Id || yaAsignados.Contains(componente.Id))
            {
                continue;
            }
            lstDisponibles.Items.Add(new ListItem(Etiquetar(componente), componente.Id.ToString()));
        }
    }

    private static string Etiquetar(Componente componente)
    {
        return (componente.Tipo == 'F' ? "[Familia] " : "[Patente] ") + componente.Nombre;
    }

    private int IdSeleccionado()
    {
        int id;
        return int.TryParse(ddlFamilias.SelectedValue, out id) ? id : 0;
    }

    /// <summary>
    /// Un usuario no puede modificar la composición de una familia/rol que forma
    /// parte de sus propios permisos: sería una forma indirecta de auto-escalar
    /// (o auto-degradar) su propio acceso, saltando el control de "no puede
    /// cambiar su propio rol".
    /// </summary>
    private bool EsComponenteDelRolActual(int idComponente)
    {
        BEUsuario actual = SessionManager.UsuarioActual;
        if (actual == null)
        {
            return false;
        }

        foreach (BERol rol in bllPermiso.TraerRoles())
        {
            if (rol.Id != actual.IdRol)
            {
                continue;
            }

            HashSet<int> componentes = new HashSet<int>();
            foreach (Componente componente in rol.Permisos)
            {
                AcumularConHijos(componente, componentes);
            }
            return componentes.Contains(idComponente);
        }

        return false;
    }

    private static void AcumularConHijos(Componente componente, HashSet<int> destino)
    {
        if (componente == null || !destino.Add(componente.Id))
        {
            return;
        }

        foreach (Componente hijo in componente.Hijos)
        {
            AcumularConHijos(hijo, destino);
        }
    }

    private void SeleccionarEnLista(int id)
    {
        ListItem item = ddlFamilias.Items.FindByValue(id.ToString());
        if (item != null)
        {
            ddlFamilias.ClearSelection();
            item.Selected = true;
        }
    }

    // ---- Modificar rol: renombrar y cambiar composición. ----

    protected void ddlRolMod_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarRolSeleccionado();
    }

    protected void btnRenombrarRolMod_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            bllPermiso.ModificarRol(IdRolSeleccionado(), txtNombreRolMod.Text);

            CargarRolesMod();
            SeleccionarRolEnLista(IdRolSeleccionado());
            CargarRolSeleccionado();

            MostrarMensaje(lblMensaje, "El nombre del rol se actualizó.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnAgregarRolMod_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);

        int idRol = IdRolSeleccionado();

        if (EsRolProtegido(idRol))
        {
            MostrarMensaje(lblMensaje, MensajeRolProtegido(idRol), true);
            return;
        }

        List<int> elegidos = new List<int>();
        foreach (ListItem item in lstDisponiblesRolMod.Items)
        {
            if (item.Selected)
            {
                elegidos.Add(int.Parse(item.Value));
            }
        }

        if (elegidos.Count == 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione al menos un componente de la lista de disponibles.", true);
            return;
        }

        try
        {
            foreach (int id in elegidos)
            {
                bllPermiso.AsignarARol(idRol, id);
            }

            CargarRolSeleccionado();
            MostrarMensaje(lblMensaje, "Se agregaron " + elegidos.Count + " componente(s) al rol.", false);
        }
        catch (Exception ex)
        {
            CargarRolSeleccionado();
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnQuitarRolMod_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);

        int idRol = IdRolSeleccionado();

        if (EsRolProtegido(idRol))
        {
            MostrarMensaje(lblMensaje, MensajeRolProtegido(idRol), true);
            return;
        }

        int quitados = 0;

        try
        {
            foreach (ListItem item in lstAsignadosRolMod.Items)
            {
                if (item.Selected)
                {
                    bllPermiso.QuitarDeRol(idRol, int.Parse(item.Value));
                    quitados++;
                }
            }

            if (quitados == 0)
            {
                MostrarMensaje(lblMensaje, "Seleccione al menos un componente otorgado por el rol.", true);
                return;
            }

            CargarRolSeleccionado();
            MostrarMensaje(lblMensaje, "Se quitaron " + quitados + " componente(s) del rol.", false);
        }
        catch (Exception ex)
        {
            CargarRolSeleccionado();
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarRolesMod()
    {
        try
        {
            int seleccionado = IdRolSeleccionado();

            ddlRolMod.Items.Clear();
            ddlRolMod.Items.Add(new ListItem("(seleccione un rol)", "0"));

            foreach (BERol rol in bllPermiso.TraerRoles())
            {
                ddlRolMod.Items.Add(new ListItem(rol.Nombre, rol.Id.ToString()));
            }

            if (seleccionado > 0)
            {
                SeleccionarRolEnLista(seleccionado);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarRolSeleccionado()
    {
        int idRol = IdRolSeleccionado();

        if (idRol == 0)
        {
            pnlEdicionRolMod.Visible = false;
            return;
        }

        try
        {
            BERol rol = bllPermiso.ObtenerRol(idRol);

            if (rol == null)
            {
                pnlEdicionRolMod.Visible = false;
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            pnlEdicionRolMod.Visible = true;
            txtNombreRolMod.Text = rol.Nombre;

            bool esWebMaster = EsWebMaster(rol);
            bool protegido = EsRolProtegido(idRol);

            txtNombreRolMod.Enabled = !esWebMaster;
            btnRenombrarRolMod.Enabled = !esWebMaster;
            lstDisponiblesRolMod.Enabled = !protegido;
            lstAsignadosRolMod.Enabled = !protegido;
            btnAgregarRolMod.Enabled = !protegido;
            btnQuitarRolMod.Enabled = !protegido;

            lblAvisoRolMod.Visible = protegido;
            lblAvisoRolMod.Text = MensajeRolProtegido(idRol);

            CargarListasRol(rol);
            litArbolRolMod.Text = RenderPermisos.ArbolDelRol(rol);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarListasRol(BERol rol)
    {
        List<int> yaAsignados = new List<int>();

        lstAsignadosRolMod.Items.Clear();
        foreach (Componente componente in rol.Permisos)
        {
            yaAsignados.Add(componente.Id);
            lstAsignadosRolMod.Items.Add(new ListItem(Etiquetar(componente), componente.Id.ToString()));
        }

        lstDisponiblesRolMod.Items.Clear();
        foreach (Componente componente in bllPermiso.TraerTodos())
        {
            if (yaAsignados.Contains(componente.Id))
            {
                continue;
            }
            lstDisponiblesRolMod.Items.Add(new ListItem(Etiquetar(componente), componente.Id.ToString()));
        }
    }

    private static bool EsWebMaster(BERol rol)
    {
        return rol != null && string.Equals(rol.Nombre, "WebMaster", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>El WebMaster es intocable, y nadie puede tocar los permisos de su propio rol.</summary>
    private bool EsRolProtegido(int idRol)
    {
        BERol rol = bllPermiso.ObtenerRol(idRol);
        if (EsWebMaster(rol))
        {
            return true;
        }

        BEUsuario actual = SessionManager.UsuarioActual;
        return actual != null && actual.IdRol == idRol;
    }

    private string MensajeRolProtegido(int idRol)
    {
        BERol rol = bllPermiso.ObtenerRol(idRol);
        if (EsWebMaster(rol))
        {
            return "El rol WebMaster es parte del sistema y no se puede modificar.";
        }
        return "No puede modificar los permisos del rol de su propia cuenta.";
    }

    private int IdRolSeleccionado()
    {
        int id;
        return int.TryParse(ddlRolMod.SelectedValue, out id) ? id : 0;
    }

    private void SeleccionarRolEnLista(int id)
    {
        ListItem item = ddlRolMod.Items.FindByValue(id.ToString());
        if (item != null)
        {
            ddlRolMod.ClearSelection();
            item.Selected = true;
        }
    }
}
