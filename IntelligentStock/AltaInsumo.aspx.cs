using System;
using BE;
using BLL;
using Servicios;

// CU-005-024 Alta de insumo.
// La BLL controla que el nombre no esté repetido, graba, recalcula el dígito
// verificador de la tabla Insumo y deja el evento en la bitácora.
public partial class AltaInsumo : PaginaBase
{
    private BLLInsumo bllInsumo = new BLLInsumo();

    protected override string PatenteRequerida
    {
        get { return "AltaInsumo"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        decimal stock;
        decimal stockMinimo;
        if (!Numeros.TryParseDecimal(txtStock.Text, out stock) ||
            !Numeros.TryParseDecimal(txtStockMinimo.Text, out stockMinimo))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                "El stock inicial y el stock mínimo tienen que ser números."), true);
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
            insumo.Nombre = txtNombre.Text;
            insumo.Descripcion = txtDescripcion.Text;
            insumo.UnidadDeMedida = txtUnidad.Text;
            insumo.Stock = stock;
            insumo.StockMinimo = stockMinimo;
            insumo.DiasDeVidaUtil = vidaUtil;
            insumo.PrecioUnitario = precio;

            bllInsumo.Alta(insumo);

            Limpiar();
            MostrarMensaje(lblMensaje,
                "El insumo " + insumo.Nombre + " se dio de alta correctamente.", false);
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

    private void Limpiar()
    {
        txtNombre.Text = string.Empty;
        txtDescripcion.Text = string.Empty;
        txtUnidad.Text = string.Empty;
        txtStock.Text = string.Empty;
        txtStockMinimo.Text = string.Empty;
        txtPrecio.Text = string.Empty;
        txtVidaUtil.Text = string.Empty;
    }
}
