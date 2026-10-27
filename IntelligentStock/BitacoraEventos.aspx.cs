using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-016 Consultar bitacora.
//
// Al abrir se muestran los 200 eventos mas recientes. Los filtros son opcionales
// y se combinan entre si.
public partial class BitacoraEventos : PaginaBase
{
    private const string ClaveEventos = "EventosBitacora";

    private BLLEvento bllEvento = new BLLEvento();
    private BLLUsuario bllUsuario = new BLLUsuario();

    protected override string PatenteRequerida
    {
        get { return "BitacoraEventos"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (IsPostBack)
        {
            return;
        }

        CargarUsuarios();
        MostrarUltimosEventos();
    }

    private void CargarUsuarios()
    {
        ddlUsuario.Items.Clear();
        ddlUsuario.Items.Add(new ListItem("(todos)", string.Empty));

        try
        {
            foreach (BEUsuario usuario in bllUsuario.TraerUsuarios())
            {
                ddlUsuario.Items.Add(new ListItem(usuario.NombreUsuario, usuario.NombreUsuario));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    private void MostrarUltimosEventos()
    {
        try
        {
            List<BEEvento> eventos = bllEvento.TraerUltimos(BLLEvento.CantidadInicial);
            Enlazar(eventos);
            litResumen.Text = "Mostrando los últimos " + BLLEvento.CantidadInicial +
                              " eventos registrados (" + eventos.Count + " encontrados).";
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnFiltrar_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);

        DateTime desde;
        DateTime hasta;

        // Si no cargan fechas se toma un rango amplio para que el resto de los
        // filtros funcione igual.
        if (!DateTime.TryParse(txtDesde.Text, out desde))
        {
            desde = new DateTime(2000, 1, 1);
        }

        if (!DateTime.TryParse(txtHasta.Text, out hasta))
        {
            hasta = DateTime.Today;
        }

        if (hasta < desde)
        {
            MostrarMensaje(lblMensaje, "La fecha hasta no puede ser anterior a la fecha desde.", true);
            return;
        }

        CriticidadEvento? criticidad = null;
        if (!string.IsNullOrEmpty(ddlCriticidad.SelectedValue))
        {
            criticidad = (CriticidadEvento)Enum.Parse(typeof(CriticidadEvento), ddlCriticidad.SelectedValue);
        }

        try
        {
            List<BEEvento> eventos = bllEvento.Filtrar(desde, hasta,
                ddlUsuario.SelectedValue, txtTexto.Text, criticidad);

            Enlazar(eventos);

            if (eventos.Count == 0)
            {
                MostrarError(lblMensaje, "ERR013");
                litResumen.Text = string.Empty;
            }
            else
            {
                litResumen.Text = Textos.Contar(eventos.Count, "evento encontrado.", "eventos encontrados.");
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnLimpiar_Click(object sender, EventArgs e)
    {
        txtDesde.Text = string.Empty;
        txtHasta.Text = string.Empty;
        txtTexto.Text = string.Empty;
        ddlUsuario.SelectedIndex = 0;
        ddlCriticidad.SelectedIndex = 0;

        OcultarMensaje(lblMensaje);
        MostrarUltimosEventos();
    }

    protected void gvEventos_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvEventos.PageIndex = e.NewPageIndex;
        gvEventos.DataSource = Session[ClaveEventos];
        gvEventos.DataBind();
    }

    // La lista queda en sesion para poder paginar sin volver a pegarle a la base.
    private void Enlazar(List<BEEvento> eventos)
    {
        Session[ClaveEventos] = eventos;
        gvEventos.PageIndex = 0;
        gvEventos.DataSource = eventos;
        gvEventos.DataBind();
    }
}
