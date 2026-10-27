using System;
using System.Collections.Generic;
using BE;
using BLL;
using Servicios;

// CU-003-014 Bloquear usuario.
// Se elige la cuenta en la grilla y se presiona Bloquear, como en el resto de las bajas.
public partial class BloquearUsuario : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();

    protected override string PatenteRequerida
    {
        get { return "BloquearUsuario"; }
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
        btnBloquear.Enabled = gvUsuarios.SelectedIndex >= 0;
    }

    protected void btnBloquear_Click(object sender, EventArgs e)
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
            bllUsuario.Bloquear(dni);

            CargarUsuarios();
            MostrarMensaje(lblMensaje, "El usuario " + nombre + " quedó bloqueado.", false);
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
            int? propio = SessionManager.DniActual;
            List<BEUsuario> candidatos = new List<BEUsuario>();

            foreach (BEUsuario usuario in bllUsuario.TraerUsuarios())
            {
                // Ni los ya bloqueados por un administrador, ni los dados de baja,
                // ni la propia cuenta. Los bloqueados por intentos fallidos sí
                // aparecen: bloquearlos acá convierte el bloqueo en manual.
                if (usuario.Activo && !usuario.BloqueadoManualmente && !(propio.HasValue && usuario.DNI == propio.Value))
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
        btnBloquear.Enabled = false;
    }
}
