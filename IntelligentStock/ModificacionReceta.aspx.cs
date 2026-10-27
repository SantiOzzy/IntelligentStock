using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-005-029 Modificación de receta.
// Al elegir la receta se cargan su nombre y su detalle. Los cambios del detalle
// se van haciendo en pantalla (ViewState) y recién se graban al presionar
// "Guardar cambios", todo junto y en una sola transacción.
public partial class ModificacionReceta : PaginaBase
{
    private const string ClaveDetalle = "DetalleReceta";

    private BLLReceta bllReceta = new BLLReceta();

    protected override string PatenteRequerida
    {
        get { return "ModificacionReceta"; }
    }

    private List<BEDetalleReceta> Detalle
    {
        get
        {
            List<BEDetalleReceta> detalle = ViewState[ClaveDetalle] as List<BEDetalleReceta>;
            return detalle ?? new List<BEDetalleReceta>();
        }
        set { ViewState[ClaveDetalle] = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarRecetas();
            LimpiarDatos();
        }
    }

    protected void ddlReceta_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarRecetaSeleccionada();
    }

    protected void btnAgregar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        if (IdSeleccionado() == 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione una receta de la lista.", true);
            return;
        }

        int idInsumo = ucBuscadorInsumo.IdInsumoSeleccionado;
        if (idInsumo == 0)
        {
            MostrarMensaje(lblMensaje, "Elija un insumo de la lista.", true);
            return;
        }

        decimal cantidad;
        if (!Numeros.TryParseDecimal(txtCantidad.Text, out cantidad))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                "La cantidad tiene que ser un número."), true);
            return;
        }

        try
        {
            List<BEDetalleReceta> detalle = Detalle;
            bool actualizado = bllReceta.AgregarAlDetalle(detalle, idInsumo, cantidad);
            string nombre = ucBuscadorInsumo.NombreInsumo;

            EnlazarDetalle(detalle);
            ucBuscadorInsumo.Limpiar();
            txtCantidad.Text = string.Empty;

            MostrarMensaje(lblMensaje,
                (actualizado
                    ? "Se actualizó la cantidad de " + nombre + "."
                    : "Se agregó " + nombre + " a la receta.") +
                " Presione \"Guardar cambios\" para grabarlo.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void gvDetalle_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName != "Quitar")
        {
            return;
        }

        List<BEDetalleReceta> detalle = Detalle;
        bllReceta.QuitarDelDetalle(detalle, Convert.ToInt32(e.CommandArgument));
        EnlazarDetalle(detalle);
        MostrarMensaje(lblMensaje, "Se quitó el insumo. Presione \"Guardar cambios\" para grabarlo.", false);
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        int idReceta = IdSeleccionado();
        if (idReceta == 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione una receta de la lista.", true);
            return;
        }

        try
        {
            BEReceta receta = new BEReceta();
            receta.IdReceta = idReceta;
            receta.Nombre = txtNombre.Text;
            receta.Detalles = new List<BEDetalleReceta>(Detalle);

            bllReceta.Modificar(receta);

            // Se vuelve a armar la lista porque el nombre pudo cambiar.
            CargarRecetas();
            SeleccionarEnLista(idReceta);
            CargarRecetaSeleccionada();

            MostrarMensaje(lblMensaje, "Los cambios de la receta se guardaron correctamente.", false);
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

    private void CargarRecetas()
    {
        try
        {
            ddlReceta.Items.Clear();
            ddlReceta.Items.Add(new ListItem("(seleccione)", "0"));

            foreach (BEReceta receta in bllReceta.TraerActivas())
            {
                ddlReceta.Items.Add(new ListItem(receta.Nombre, receta.IdReceta.ToString()));
            }

            // Si no hay nada para elegir, se dice en la misma lista.
            if (ddlReceta.Items.Count == 1)
            {
                ddlReceta.Items[0].Text = "(no hay recetas activas)";
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarRecetaSeleccionada()
    {
        int idReceta = IdSeleccionado();

        if (idReceta == 0)
        {
            LimpiarDatos();
            return;
        }

        try
        {
            BEReceta receta = bllReceta.ObtenerPorId(idReceta);

            if (receta == null)
            {
                LimpiarDatos();
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            txtNombre.Text = receta.Nombre;
            txtCantidad.Text = string.Empty;
            ucBuscadorInsumo.Limpiar();
            EnlazarDetalle(receta.Detalles);
            HabilitarEdicion(true);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // Hasta que no se elige una receta no se puede editar nada.
    private void LimpiarDatos()
    {
        txtNombre.Text = string.Empty;
        txtCantidad.Text = string.Empty;
        ucBuscadorInsumo.Limpiar();
        EnlazarDetalle(new List<BEDetalleReceta>());
        HabilitarEdicion(false);
    }

    private void HabilitarEdicion(bool habilitar)
    {
        txtNombre.Enabled = habilitar;
        txtCantidad.Enabled = habilitar;
        btnAgregar.Enabled = habilitar;
        btnAceptar.Enabled = habilitar;
    }

    private void EnlazarDetalle(List<BEDetalleReceta> detalle)
    {
        Detalle = detalle;
        gvDetalle.DataSource = detalle;
        gvDetalle.DataBind();
    }

    private int IdSeleccionado()
    {
        int id;
        return int.TryParse(ddlReceta.SelectedValue, out id) ? id : 0;
    }

    private void SeleccionarEnLista(int idReceta)
    {
        ListItem item = ddlReceta.Items.FindByValue(idReceta.ToString());
        if (item != null)
        {
            ddlReceta.ClearSelection();
            item.Selected = true;
        }
    }
}
