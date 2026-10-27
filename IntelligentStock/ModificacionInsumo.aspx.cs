using System;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-005-025 Modificación de insumo.
// Al elegir el insumo en la lista se cargan sus datos. El stock no se edita
// acá: solo cambia por ajuste, venta o compra. Se listan solo los insumos
// activos, porque los dados de baja ya no operan.
public partial class ModificacionInsumo : PaginaBase
{
    private BLLInsumo bllInsumo = new BLLInsumo();

    protected override string PatenteRequerida
    {
        get { return "ModificacionInsumo"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarInsumos();
            LimpiarDatos();
        }
    }

    protected void ddlInsumo_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CargarInsumoSeleccionado();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        int idInsumo = IdSeleccionado();
        if (idInsumo == 0)
        {
            MostrarMensaje(lblMensaje, "Seleccione un insumo de la lista.", true);
            return;
        }

        decimal stockMinimo;
        if (!Numeros.TryParseDecimal(txtStockMinimo.Text, out stockMinimo))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                "El stock mínimo tiene que ser un número."), true);
            return;
        }

        decimal precio;
        if (!Numeros.TryParseDecimal(txtPrecio.Text, out precio))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                "El precio unitario tiene que ser un número."), true);
            return;
        }

        int? vidaUtil = null;
        if (txtVidaUtil.Text.Trim().Length > 0)
        {
            int dias;
            if (!Numeros.TryParseEntero(txtVidaUtil.Text, out dias))
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "Los días de vida útil tienen que ser un número entero."), true);
                return;
            }
            vidaUtil = dias;
        }

        try
        {
            BEInsumo insumo = new BEInsumo();
            insumo.IdInsumo = idInsumo;
            insumo.Nombre = txtNombre.Text;
            insumo.Descripcion = txtDescripcion.Text;
            insumo.UnidadDeMedida = txtUnidad.Text;
            insumo.StockMinimo = stockMinimo;
            insumo.DiasDeVidaUtil = vidaUtil;
            insumo.PrecioUnitario = precio;

            bllInsumo.Modificar(insumo);

            // Se vuelve a armar la lista porque el nombre que muestra pudo cambiar.
            CargarInsumos();
            SeleccionarEnLista(idInsumo);
            CargarInsumoSeleccionado();

            MostrarMensaje(lblMensaje, "Los datos del insumo se actualizaron correctamente.", false);
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

    private void CargarInsumos()
    {
        try
        {
            ddlInsumo.Items.Clear();
            ddlInsumo.Items.Add(new ListItem("(seleccione)", "0"));

            foreach (BEInsumo insumo in bllInsumo.TraerActivos())
            {
                ddlInsumo.Items.Add(new ListItem(insumo.Etiqueta, insumo.IdInsumo.ToString()));
            }

            // Si no hay nada para elegir, se dice en la misma lista.
            if (ddlInsumo.Items.Count == 1)
            {
                ddlInsumo.Items[0].Text = "(no hay insumos activos)";
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void CargarInsumoSeleccionado()
    {
        int idInsumo = IdSeleccionado();

        if (idInsumo == 0)
        {
            LimpiarDatos();
            return;
        }

        try
        {
            BEInsumo insumo = bllInsumo.ObtenerPorId(idInsumo);

            if (insumo == null)
            {
                LimpiarDatos();
                MostrarError(lblMensaje, "ERR013");
                return;
            }

            txtNombre.Text = insumo.Nombre;
            txtDescripcion.Text = insumo.Descripcion;
            txtUnidad.Text = insumo.UnidadDeMedida;
            txtStockMinimo.Text = Numeros.ParaInput(insumo.StockMinimo);
            txtPrecio.Text = Numeros.ParaInput(insumo.PrecioUnitario);
            txtVidaUtil.Text = insumo.DiasDeVidaUtil.HasValue ? insumo.DiasDeVidaUtil.Value.ToString() : string.Empty;
            HabilitarEdicion(true);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // Hasta que no se elige un insumo los campos quedan deshabilitados.
    private void LimpiarDatos()
    {
        txtNombre.Text = string.Empty;
        txtDescripcion.Text = string.Empty;
        txtUnidad.Text = string.Empty;
        txtStockMinimo.Text = string.Empty;
        txtPrecio.Text = string.Empty;
        txtVidaUtil.Text = string.Empty;
        HabilitarEdicion(false);
    }

    private void HabilitarEdicion(bool habilitar)
    {
        txtNombre.Enabled = habilitar;
        txtDescripcion.Enabled = habilitar;
        txtUnidad.Enabled = habilitar;
        txtStockMinimo.Enabled = habilitar;
        txtPrecio.Enabled = habilitar;
        txtVidaUtil.Enabled = habilitar;
        btnAceptar.Enabled = habilitar;
    }

    private int IdSeleccionado()
    {
        int id;
        return int.TryParse(ddlInsumo.SelectedValue, out id) ? id : 0;
    }

    private void SeleccionarEnLista(int idInsumo)
    {
        ListItem item = ddlInsumo.Items.FindByValue(idInsumo.ToString());
        if (item != null)
        {
            ddlInsumo.ClearSelection();
            item.Selected = true;
        }
    }
}
