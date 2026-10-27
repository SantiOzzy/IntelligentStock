using System;
using System.Collections.Generic;
using BE;
using BLL;
using Servicios;

// CU-005-027 Consulta de insumos.
// Al entrar se muestra el catálogo completo. Los filtros son opcionales y se
// combinan entre sí. La columna de stock usa el componente IndicadorStock,
// que la pinta según lo cerca que esté el insumo de su stock mínimo.
public partial class ConsultaInsumo : PaginaBase
{
    private BLLInsumo bllInsumo = new BLLInsumo();

    protected override string PatenteRequerida
    {
        get { return "ConsultaInsumo"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            Buscar();
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        Buscar();
    }

    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/IniciarSesion.aspx", false);
        Context.ApplicationInstance.CompleteRequest();
    }

    private void Buscar()
    {
        OcultarMensaje(lblMensaje);

        try
        {
            List<BEInsumo> insumos = bllInsumo.Filtrar(txtBusqueda.Text, ddlEstado.SelectedValue, chkBajoMinimo.Checked);

            gvDetalle.DataSource = insumos;
            gvDetalle.DataBind();

            if (insumos.Count == 0)
            {
                MostrarError(lblMensaje, "ERR013");
                litResumen.Text = string.Empty;
            }
            else
            {
                litResumen.Text = Textos.Contar(insumos.Count, "insumo encontrado.", "insumos encontrados.");
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }
}
