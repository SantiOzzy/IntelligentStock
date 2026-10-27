using System;
using System.Collections.Generic;
using BE;
using BLL;
using Servicios;

// CU-004-023 Consulta de proveedores.
// Al entrar se muestra el listado completo. Los filtros son opcionales y se
// combinan entre sí. Es una consulta: no modifica ningún dato.
public partial class ConsultaProveedor : PaginaBase
{
    private BLLProveedor bllProveedor = new BLLProveedor();

    protected override string PatenteRequerida
    {
        get { return "ConsultaProveedor"; }
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
            List<BEProveedor> proveedores = bllProveedor.Filtrar(txtBusqueda.Text, ddlEstado.SelectedValue);

            gvDetalle.DataSource = proveedores;
            gvDetalle.DataBind();

            if (proveedores.Count == 0)
            {
                MostrarError(lblMensaje, "ERR013");
                litResumen.Text = string.Empty;
            }
            else
            {
                litResumen.Text = Textos.Contar(proveedores.Count, "proveedor encontrado.", "proveedores encontrados.");
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }
}
