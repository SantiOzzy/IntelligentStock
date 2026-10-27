using System;
using BE;
using BLL;
using Servicios;

// CU-005-030 Baja de receta.
// Se listan las recetas activas. El usuario elige una en la grilla, presiona
// "Dar de baja" y confirma. La baja es lógica: la receta deja de poder
// venderse, pero se conserva su detalle.
public partial class BajaReceta : PaginaBase
{
    private BLLReceta bllReceta = new BLLReceta();

    protected override string PatenteRequerida
    {
        get { return "BajaReceta"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarActivas();
        }
    }

    protected void gvDetalle_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        PrepararBoton();
    }

    protected void chkMostrarInactivos_CheckedChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarActivas();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (gvDetalle.SelectedIndex < 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione una receta de la grilla.", true);
            return;
        }

        int idReceta = Convert.ToInt32(gvDetalle.SelectedDataKey.Values["IdReceta"]);
        string nombre = gvDetalle.SelectedRow.Cells[1].Text;

        try
        {
            bool activo = Convert.ToBoolean(gvDetalle.SelectedDataKey.Values["Activo"]);
            if (activo)
            {
                bllReceta.Baja(idReceta);
            }
            else
            {
                bllReceta.Reactivar(idReceta);
            }

            CargarActivas();
            MostrarMensaje(lblMensaje, "La receta " + nombre + " se " +
                (activo ? "dio de baja" : "reactivó") + " correctamente.", false);
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

    private void CargarActivas()
    {
        try
        {
            gvDetalle.SelectedIndex = -1;
            gvDetalle.DataSource = chkMostrarInactivos.Checked ? bllReceta.TraerRecetas() : bllReceta.TraerActivas();
            gvDetalle.DataBind();
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
        bool hayElegido = gvDetalle.SelectedIndex >= 0;
        bool activo = !hayElegido || Convert.ToBoolean(gvDetalle.SelectedDataKey.Values["Activo"]);

        btnAceptar.Enabled = hayElegido;
        btnAceptar.Text = activo ? "Dar de baja" : "Reactivar";
        btnAceptar.CssClass = activo ? "btn btn-danger" : "btn btn-success";
        btnAceptar.OnClientClick = activo
            ? "return confirm('¿Confirma la baja de la receta seleccionada?');"
            : "return confirm('¿Confirma la reactivación de la receta seleccionada?');";
    }
}
