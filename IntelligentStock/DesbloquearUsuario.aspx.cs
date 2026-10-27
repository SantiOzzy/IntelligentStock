using System;
using System.Collections.Generic;
using BE;
using BLL;
using Servicios;

// CU-003-015 Desbloquear usuario.
// Se elige la cuenta en la grilla y se presiona Desbloquear, como en el resto de las bajas.
public partial class DesbloquearUsuario : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();

    protected override string PatenteRequerida
    {
        get { return "DesbloquearUsuario"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarUsuarios();
        }
    }

    protected void gvUsuarios_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        btnDesbloquear.Enabled = gvUsuarios.SelectedIndex >= 0;
    }

    protected void btnDesbloquear_Click(object sender, EventArgs e)
    {
        if (gvUsuarios.SelectedIndex < 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione un usuario de la grilla.", true);
            return;
        }

        int dni = Convert.ToInt32(gvUsuarios.SelectedDataKey.Value);
        string nombre = gvUsuarios.SelectedRow.Cells[1].Text;

        try
        {
            bllUsuario.Desbloquear(dni);

            CargarUsuarios();
            MostrarMensaje(lblMensaje, "El usuario " + nombre + " quedó desbloqueado.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private void CargarUsuarios()
    {
        try
        {
            List<BEUsuario> candidatos = new List<BEUsuario>();

            foreach (BEUsuario usuario in bllUsuario.TraerUsuarios())
            {
                // Solo las cuentas activas que están bloqueadas.
                if (usuario.Activo && usuario.Bloqueado)
                {
                    candidatos.Add(usuario);
                }
            }

            gvUsuarios.SelectedIndex = -1;
            gvUsuarios.DataSource = candidatos;
            gvUsuarios.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }

        // Hasta que no se elige un usuario no hay nada que procesar.
        btnDesbloquear.Enabled = false;
    }
}
