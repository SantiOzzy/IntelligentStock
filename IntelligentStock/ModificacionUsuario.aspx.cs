using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-010 Modificar usuario.
// Además de los datos personales, acá se cambia el rol, que es lo que reasigna
// las familias y patentes del usuario.
public partial class ModificacionUsuario : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "ModificacionUsuario"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        txtFechaNacimiento.Attributes["max"] =
            DateTime.Today.AddYears(-BLLUsuario.EdadMinima).ToString("yyyy-MM-dd");
        txtFechaNacimiento.Attributes["min"] =
            DateTime.Today.AddYears(-BLLUsuario.EdadMaxima).ToString("yyyy-MM-dd");

        if (!IsPostBack)
        {
            CargarUsuarios();
            CargarRoles();
        }
    }

    protected void btnBuscar_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);

        int dni;
        if (!int.TryParse(ddlUsuarios.SelectedValue, out dni))
        {
            MostrarMensaje(lblMensaje, "Seleccione un usuario de la lista.", true);
            return;
        }

        try
        {
            BEUsuario usuario = bllUsuario.ObtenerPorDNI(dni);

            if (usuario == null)
            {
                MostrarError(lblMensaje, "ERR013");
                pnlDatos.Visible = false;
                return;
            }

            MostrarDatos(usuario);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void ddlRol_SelectedIndexChanged(object sender, EventArgs e)
    {
        MostrarPermisosDelRol();
    }

    protected void btnGuardar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            BEUsuario usuario = new BEUsuario();
            usuario.DNI = int.Parse(txtDni.Text);
            usuario.IdRol = int.Parse(ddlRol.SelectedValue);
            usuario.Nombre = txtNombre.Text.Trim();
            usuario.Apellido = txtApellido.Text.Trim();
            usuario.CorreoElectronico = txtCorreo.Text.Trim();
            usuario.NumeroTelefono = txtTelefono.Text.Trim();
            usuario.NombreUsuario = txtUsuario.Text.Trim();

            DateTime fecha;
            if (DateTime.TryParse(txtFechaNacimiento.Text, out fecha))
            {
                usuario.FechaNacimiento = fecha;
            }

            bllUsuario.Modificar(usuario);

            // Si el que se modificó a sí mismo cambió de rol, hay que refrescar la
            // sesión o seguiría navegando con los permisos viejos.
            RefrescarSesionSiEsElPropioUsuario(usuario.DNI);

            // Se vuelve a armar la lista (el nombre pudo cambiar) y queda elegido
            // el mismo usuario, que es el que se sigue viendo en pantalla.
            CargarUsuarios();
            ListItem elegido = ddlUsuarios.Items.FindByValue(usuario.DNI.ToString());
            if (elegido != null)
            {
                ddlUsuarios.ClearSelection();
                elegido.Selected = true;
            }

            MostrarMensaje(lblMensaje, "Los datos del usuario se actualizaron correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        pnlDatos.Visible = false;
        OcultarMensaje(lblMensaje);
    }

    protected void btnRestablecerClave_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            int dni = int.Parse(txtDni.Text);
            bool avisado = bllUsuario.RestablecerClave(dni, txtNuevaClave.Text, txtConfirmarClave.Text);

            txtNuevaClave.Text = string.Empty;
            txtConfirmarClave.Text = string.Empty;

            MostrarMensaje(lblMensaje, avisado
                ? "La contraseña se restableció correctamente y se avisó al usuario por correo."
                : "La contraseña se restableció correctamente. No se pudo avisar por correo (el envío de " +
                  "correo no está configurado o falló): comuníquele la contraseña nueva al usuario.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarDatos(BEUsuario usuario)
    {
        pnlDatos.Visible = true;

        txtDni.Text = usuario.DNI.ToString();
        txtNombre.Text = usuario.Nombre;
        txtApellido.Text = usuario.Apellido;
        txtCorreo.Text = usuario.CorreoElectronico;
        txtTelefono.Text = usuario.NumeroTelefono;
        txtUsuario.Text = usuario.NombreUsuario;
        txtFechaNacimiento.Text = usuario.FechaNacimiento.HasValue
            ? usuario.FechaNacimiento.Value.ToString("yyyy-MM-dd")
            : string.Empty;

        ListItem item = ddlRol.Items.FindByValue(usuario.IdRol.ToString());
        if (item != null)
        {
            ddlRol.ClearSelection();
            item.Selected = true;
        }

        // Nadie puede cambiarse el rol a sí mismo: si el WebMaster se pasara a un
        // perfil sin permisos de seguridad, nadie podría volver a administrarlo.
        bool esLaPropiaCuenta = UsuarioActual != null && UsuarioActual.DNI == usuario.DNI;
        ddlRol.Enabled = !esLaPropiaCuenta;
        lblAvisoRol.Visible = esLaPropiaCuenta;

        MostrarPermisosDelRol();
    }

    private void CargarUsuarios()
    {
        try
        {
            ddlUsuarios.Items.Clear();
            ddlUsuarios.Items.Add(new ListItem("(seleccione)", string.Empty));

            foreach (BEUsuario usuario in bllUsuario.TraerUsuarios())
            {
                string texto = usuario.NombreUsuario + " - " + usuario.NombreCompleto +
                               " (DNI " + usuario.DNI + ")";
                ddlUsuarios.Items.Add(new ListItem(texto, usuario.DNI.ToString()));
            }
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
            // Solo se ofrecen los roles que no tienen permisos que el propio
            // usuario no tiene (un Encargado no puede crear un WebMaster).
            List<BERol> roles = bllUsuario.FiltrarRolesAsignables(bllPermiso.TraerRoles());

            ddlRol.DataSource = roles;
            ddlRol.DataTextField = "Nombre";
            ddlRol.DataValueField = "Id";
            ddlRol.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarPermisosDelRol()
    {
        if (ddlRol.Items.Count == 0)
        {
            litPermisos.Text = "<em>No hay roles cargados en el sistema.</em>";
            return;
        }

        try
        {
            int idRol = int.Parse(ddlRol.SelectedValue);

            foreach (BERol rol in bllPermiso.TraerRoles())
            {
                if (rol.Id == idRol)
                {
                    litPermisos.Text = RenderPermisos.ArbolDelRol(rol);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            litPermisos.Text = "<em>" + ex.Message + "</em>";
        }
    }

    private void RefrescarSesionSiEsElPropioUsuario(int dni)
    {
        BEUsuario enSesion = UsuarioActual;

        if (enSesion == null || enSesion.DNI != dni)
        {
            return;
        }

        BEUsuario actualizado = bllUsuario.ObtenerPorDNI(dni);
        bllUsuario.CargarRol(actualizado);
        SessionManager.UsuarioActual = actualizado;
    }
}
