using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-009 Alta de usuario.
// El rol se elige acá: es lo que determina, vía Composite, a qué formularios
// va a poder entrar el usuario nuevo.
public partial class AltaUsuario : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "AltaUsuario"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // El navegador no deja elegir una fecha fuera del rango; la BLL vuelve a
        // controlarlo igual, porque el atributo se puede saltear.
        txtFechaNacimiento.Attributes["max"] =
            DateTime.Today.AddYears(-BLLUsuario.EdadMinima).ToString("yyyy-MM-dd");
        txtFechaNacimiento.Attributes["min"] =
            DateTime.Today.AddYears(-BLLUsuario.EdadMaxima).ToString("yyyy-MM-dd");

        if (!IsPostBack)
        {
            CargarRoles();
            MostrarPermisosDelRol();
        }
    }

    protected void ddlRol_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Cambiar el rol hace un postback y el navegador no vuelve a mostrar las
        // contraseñas ya escritas: se reponen para no tener que tipearlas de nuevo.
        ConservarClaves();
        MostrarPermisosDelRol();
    }

    // Las cajas de contraseña tienen EnableViewState="false": el valor se
    // repone solo en esta respuesta y no queda guardado en el ViewState.
    private void ConservarClaves()
    {
        txtClave.Attributes["value"] = txtClave.Text;
        txtConfirmacion.Attributes["value"] = txtConfirmacion.Text;
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        // Si el alta falla, las contraseñas siguen escritas (con el último valor).
        ConservarClaves();

        if (!IsValid)
        {
            return;
        }

        try
        {
            BEUsuario usuario = new BEUsuario();
            usuario.DNI = int.Parse(txtDni.Text.Trim());
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

            // La BLL encripta la clave, valida duplicados, recalcula el DV de
            // Usuario y deja el evento en la bitácora.
            bllUsuario.Alta(usuario, txtClave.Text);

            Limpiar();
            MostrarMensaje(lblMensaje,
                "El usuario " + usuario.NombreUsuario + " se dio de alta correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnLimpiar_Click(object sender, EventArgs e)
    {
        Limpiar();
        OcultarMensaje(lblMensaje);
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

    // Muestra el árbol de familias y patentes del rol elegido, para que quede
    // claro qué se le está dando al usuario antes de confirmar el alta.
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

            litPermisos.Text = "<em>Sin permisos asignados.</em>";
        }
        catch (Exception ex)
        {
            litPermisos.Text = "<em>" + ex.Message + "</em>";
        }
    }

    private void Limpiar()
    {
        txtDni.Text = string.Empty;
        txtNombre.Text = string.Empty;
        txtApellido.Text = string.Empty;
        txtFechaNacimiento.Text = string.Empty;
        txtCorreo.Text = string.Empty;
        txtTelefono.Text = string.Empty;
        txtUsuario.Text = string.Empty;
        txtClave.Text = string.Empty;
        txtClave.Attributes.Remove("value");
        txtConfirmacion.Attributes.Remove("value");
        txtConfirmacion.Text = string.Empty;

        if (ddlRol.Items.Count > 0)
        {
            ddlRol.SelectedIndex = 0;
        }

        MostrarPermisosDelRol();
    }
}
