using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-012 Consultar usuario.
public partial class ConsultaUsuario : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "ConsultaUsuario"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarRoles();
            Buscar();
        }
    }

    protected void btnBuscar_Click(object sender, EventArgs e)
    {
        Buscar();
    }

    protected void btnLimpiar_Click(object sender, EventArgs e)
    {
        txtBusqueda.Text = string.Empty;
        ddlRol.SelectedIndex = 0;
        ddlEstado.SelectedIndex = 0;
        Buscar();
    }

    private void Buscar()
    {
        OcultarMensaje(lblMensaje);

        try
        {
            int idRol;
            if (!int.TryParse(ddlRol.SelectedValue, out idRol))
            {
                idRol = 0;
            }

            List<BEUsuario> usuarios = bllUsuario.Filtrar(txtBusqueda.Text, idRol, ddlEstado.SelectedValue);

            gvUsuarios.DataSource = usuarios;
            gvUsuarios.DataBind();

            if (usuarios.Count == 0)
            {
                MostrarError(lblMensaje, "ERR013");
                litResumen.Text = string.Empty;
            }
            else
            {
                litResumen.Text = Textos.Contar(usuarios.Count, "usuario encontrado.", "usuarios encontrados.");
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
            ddlRol.Items.Clear();
            ddlRol.Items.Add(new ListItem("(todos)", "0"));

            foreach (BERol rol in bllPermiso.TraerRoles())
            {
                ddlRol.Items.Add(new ListItem(rol.Nombre, rol.Id.ToString()));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }
}
