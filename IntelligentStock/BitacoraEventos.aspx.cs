using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-003-016 Consultar bitacora.
//
// Al abrir se muestran los 200 eventos activos mas recientes. Los filtros son
// opcionales y se combinan entre si. Los eventos de más de 30 días están
// eliminados (eliminación lógica): se ven tildando "Incluir eventos
// eliminados", y cada mes archivado se puede descargar como XML.
public partial class BitacoraEventos : PaginaBase
{
    private const string ClaveEventos = "EventosBitacora";

    /// Posición de la columna Estado en la grilla.
    private const int ColumnaEstado = 6;

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

        litPolitica.Text = "Los eventos de los últimos " + BLLEvento.DiasActivos +
                           " días están activos. Los anteriores pasan a eliminados (no se borran) " +
                           "después de archivarse en el XML de su mes.";

        CargarUsuarios();
        CargarArchivos();
        MostrarUltimosEventos();
    }

    private void CargarArchivos()
    {
        ddlArchivo.Items.Clear();

        try
        {
            foreach (string nombre in bllEvento.ListarArchivos())
            {
                ddlArchivo.Items.Add(new ListItem(nombre, nombre));
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }

        pnlArchivos.Visible = ddlArchivo.Items.Count > 0;
        lblSinArchivos.Visible = ddlArchivo.Items.Count == 0;
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
            bool incluirEliminados = chkIncluirEliminados.Checked;
            List<BEEvento> eventos = bllEvento.TraerUltimos(BLLEvento.CantidadInicial, incluirEliminados);
            Enlazar(eventos);
            litResumen.Text = "Mostrando los últimos " + BLLEvento.CantidadInicial +
                              (incluirEliminados ? " eventos registrados, activos y eliminados (" : " eventos activos (") +
                              eventos.Count + " encontrados).";
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnFiltrar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

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
                ddlUsuario.SelectedValue, txtTexto.Text, criticidad, chkIncluirEliminados.Checked);

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
        chkIncluirEliminados.Checked = false;

        OcultarMensaje(lblMensaje);
        MostrarUltimosEventos();
    }

    protected void gvEventos_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvEventos.PageIndex = e.NewPageIndex;
        gvEventos.DataSource = Session[ClaveEventos];
        gvEventos.DataBind();
    }

    // Los eventos eliminados se ven atenuados.
    protected void gvEventos_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        BEEvento evento = e.Row.DataItem as BEEvento;
        if (e.Row.RowType == DataControlRowType.DataRow && evento != null && !evento.Activo)
        {
            e.Row.CssClass = "evento-eliminado";
        }
    }

    protected void btnDescargarArchivo_Click(object sender, EventArgs e)
    {
        string nombre = ddlArchivo.SelectedValue;
        byte[] contenido = null;
        try
        {
            contenido = bllEvento.LeerArchivo(nombre);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            return;
        }

        if (contenido == null)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR013", "No se encontró el archivo " + nombre + "."), true);
            CargarArchivos();
            return;
        }

        bllEvento.RegistrarDelUsuarioActual("Descarga de bitácora archivada",
            "Se descargó el archivo histórico " + nombre + ".");

        Response.Clear();
        Response.ContentType = "application/xml";
        Response.AppendHeader("Content-Disposition", "attachment; filename=" + nombre);
        Response.BinaryWrite(contenido);
        Response.Flush();
        Response.SuppressContent = true;
        Context.ApplicationInstance.CompleteRequest();
    }

    // La lista queda en sesion para poder paginar sin volver a pegarle a la base.
    private void Enlazar(List<BEEvento> eventos)
    {
        gvEventos.Columns[ColumnaEstado].Visible = chkIncluirEliminados.Checked;
        Session[ClaveEventos] = eventos;
        gvEventos.PageIndex = 0;
        gvEventos.DataSource = eventos;
        gvEventos.DataBind();
    }
}
