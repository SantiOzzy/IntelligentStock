using System;
using System.Collections.Generic;
using BE;
using BLL;
using Servicios;

// CU-003-011 Baja de usuario.
// Se listan los usuarios activos (menos la propia cuenta, que no se puede dar
// de baja). El usuario elige uno en la grilla, presiona "Dar de baja" y
// confirma. Con la casilla "Mostrar también los dados de baja" aparecen los
// inactivos, y al elegir uno el botón pasa a "Reactivar" (por ejemplo, para
// corregir una baja hecha por error).
public partial class BajaUsuario : PaginaBase
{
    private BLLUsuario bllUsuario = new BLLUsuario();

    protected override string PatenteRequerida
    {
        get { return "BajaUsuario"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarUsuarios();
        }
    }

    protected void chkMostrarInactivos_CheckedChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarUsuarios();
    }

    protected void gvUsuarios_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        PrepararBoton();
    }

    protected void btnDarBaja_Click(object sender, EventArgs e)
    {
        if (gvUsuarios.SelectedIndex < 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione un usuario de la grilla.", true);
            return;
        }

        int dni = Convert.ToInt32(gvUsuarios.SelectedDataKey.Values["DNI"]);
        bool activo = Convert.ToBoolean(gvUsuarios.SelectedDataKey.Values["Activo"]);
        string nombre = gvUsuarios.SelectedRow.Cells[1].Text;

        try
        {
            if (activo)
            {
                // La BLL corta si el usuario intenta darse de baja a sí mismo (#ERR012).
                bllUsuario.Baja(dni);
                CargarUsuarios();
                MostrarMensaje(lblMensaje, "El usuario " + nombre + " se dio de baja correctamente.", false);
            }
            else
            {
                bllUsuario.Reactivar(dni);
                CargarUsuarios();
                MostrarMensaje(lblMensaje, "El usuario " + nombre + " se reactivó correctamente.", false);
            }
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
            List<BEUsuario> lista = new List<BEUsuario>();

            foreach (BEUsuario usuario in bllUsuario.TraerUsuarios())
            {
                // La propia cuenta no se ofrece: no se puede dar de baja a sí mismo.
                if (propio.HasValue && usuario.DNI == propio.Value)
                {
                    continue;
                }

                if (usuario.Activo || chkMostrarInactivos.Checked)
                {
                    lista.Add(usuario);
                }
            }

            gvUsuarios.SelectedIndex = -1;
            gvUsuarios.DataSource = lista;
            gvUsuarios.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }

        PrepararBoton();
    }

    // El mismo botón da de baja o reactiva, según el estado del elegido.
    private void PrepararBoton()
    {
        bool hayElegido = gvUsuarios.SelectedIndex >= 0;
        bool activo = !hayElegido || Convert.ToBoolean(gvUsuarios.SelectedDataKey.Values["Activo"]);

        btnDarBaja.Enabled = hayElegido;
        btnDarBaja.Text = activo ? "Dar de baja" : "Reactivar";
        btnDarBaja.CssClass = activo ? "btn btn-danger" : "btn btn-success";
        btnDarBaja.OnClientClick = activo
            ? "return confirm('¿Confirma la baja del usuario seleccionado?');"
            : "return confirm('¿Confirma la reactivación del usuario seleccionado?');";
    }
}
