using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-005-028 Alta de receta.
// El detalle se arma en pantalla (queda en el ViewState hasta que se graba):
// se elige el insumo con el BuscadorInsumo, se indica la cantidad y se agrega.
// Al aceptar, la BLL graba la receta y su detalle en una sola transacción,
// recalcula los dígitos verificadores, regenera el catálogo XML de recetas y
// deja el evento en la bitácora.
public partial class AltaReceta : PaginaBase
{
    private const string ClaveDetalle = "DetalleReceta";

    private BLLReceta bllReceta = new BLLReceta();

    protected override string PatenteRequerida
    {
        get { return "AltaReceta"; }
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
            EnlazarDetalle(new List<BEDetalleReceta>());
        }
    }

    protected void btnAgregar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
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

            if (actualizado)
            {
                MostrarMensaje(lblMensaje, "El insumo " + nombre + " ya estaba en la receta: se actualizó la cantidad.", false);
            }
            else
            {
                OcultarMensaje(lblMensaje);
            }
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
        OcultarMensaje(lblMensaje);
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            BEReceta receta = new BEReceta();
            receta.Nombre = txtNombre.Text;
            receta.Detalles = new List<BEDetalleReceta>(Detalle);

            bllReceta.Alta(receta);

            txtNombre.Text = string.Empty;
            txtCantidad.Text = string.Empty;
            ucBuscadorInsumo.Limpiar();
            EnlazarDetalle(new List<BEDetalleReceta>());

            MostrarMensaje(lblMensaje,
                "La receta " + receta.Nombre + " se dio de alta correctamente con " +
                receta.Detalles.Count + " insumo(s).", false);
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

    private void EnlazarDetalle(List<BEDetalleReceta> detalle)
    {
        Detalle = detalle;
        gvDetalle.DataSource = detalle;
        gvDetalle.DataBind();
    }
}
