using System;
using System.Collections.Generic;
using System.Globalization;
using BE;
using BLL;
using Servicios;

// CU-006-037 Descontar stock por venta.
//
// El usuario elige la receta vendida y la cantidad; la grilla muestra lo que se
// va a descontar de cada insumo y cómo queda el stock. Al confirmar, la BLL
// descuenta todos los insumos en una sola transacción (si alguno no alcanza no
// se descuenta nada, #ERR018), recalcula el dígito verificador y deja la venta
// en la bitácora, que es el historial que usa la predicción de demanda. Si un
// insumo queda en su mínimo, el ControladorStock (Observer) avisa por correo.
public partial class DescontarStockVenta : PaginaBase
{
    private BLLVenta bllVenta = new BLLVenta();

    protected override string PatenteRequerida
    {
        get { return "DescontarStockVenta"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            txtFecha.Text = DateTime.Today.ToString("yyyy-MM-dd");
            CargarRecetas();
        }
    }

    protected void ddlReceta_SelectedIndexChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        Previsualizar();
    }

    protected void txtCantidad_TextChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        Previsualizar();
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        int idReceta;
        int.TryParse(ddlReceta.SelectedValue, out idReceta);

        decimal cantidad;
        if (!LeerCantidad(out cantidad))
        {
            return;
        }

        DateTime? fecha = LeerFecha(txtFecha.Text);
        if (!string.IsNullOrEmpty(txtFecha.Text) && !fecha.HasValue)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        try
        {
            List<BEConsumoVenta> consumos = bllVenta.Registrar(idReceta, cantidad, fecha);

            gvDetalle.DataSource = consumos;
            gvDetalle.DataBind();

            string mensaje = "Se registró la venta de " + cantidad.ToString("0.##") + " x " + ddlReceta.SelectedItem.Text +
                             " y se descontó el stock de " + Textos.Contar(consumos.Count, "insumo", "insumos") + ".";

            List<string> enMinimo = new List<string>();
            foreach (BEConsumoVenta consumo in consumos)
            {
                BEInsumo insumo = new BLLInsumo().ObtenerPorId(consumo.IdInsumo);
                if (insumo != null && insumo.BajoMinimo)
                {
                    enMinimo.Add(insumo.Nombre);
                }
            }
            if (enMinimo.Count > 0)
            {
                mensaje += " Atención: quedaron en su stock mínimo o por debajo: " + string.Join(", ", enMinimo.ToArray()) + ".";
            }

            txtCantidad.Text = string.Empty;
            MostrarMensaje(lblMensaje, mensaje, false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            Previsualizar();
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
            foreach (BEReceta receta in bllVenta.TraerRecetasVendibles())
            {
                ddlReceta.Items.Add(new System.Web.UI.WebControls.ListItem(receta.Nombre, receta.IdReceta.ToString()));
            }

            if (ddlReceta.Items.Count == 1)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR013",
                    "No hay recetas activas con insumos cargados."), true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Muestra lo que se descontaría, sin grabar nada.
    private void Previsualizar()
    {
        gvDetalle.DataSource = null;
        gvDetalle.DataBind();

        int idReceta;
        decimal cantidad;
        if (!int.TryParse(ddlReceta.SelectedValue, out idReceta) || idReceta <= 0 ||
            !Numeros.TryParseDecimal(txtCantidad.Text, out cantidad) || cantidad <= 0)
        {
            return;
        }

        try
        {
            List<BEConsumoVenta> consumos = bllVenta.Previsualizar(idReceta, cantidad);
            gvDetalle.DataSource = consumos;
            gvDetalle.DataBind();

            bool falta = false;
            for (int i = 0; i < consumos.Count && i < gvDetalle.Rows.Count; i++)
            {
                if (consumos[i].SinStock)
                {
                    gvDetalle.Rows[i].CssClass = "danger";
                    falta = true;
                }
            }

            if (falta && !lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR018",
                    "Los insumos marcados en rojo no alcanzan para esta venta."), true);
            }
        }
        catch (Exception ex)
        {
            if (!lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, ex.Message, true);
            }
        }
    }

    private bool LeerCantidad(out decimal cantidad)
    {
        if (!Numeros.TryParseDecimal(txtCantidad.Text, out cantidad))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Ingrese la cantidad vendida."), true);
            return false;
        }
        return true;
    }

    private static DateTime? LeerFecha(string texto)
    {
        DateTime fecha;
        if (DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
        {
            return fecha;
        }
        return null;
    }
}
