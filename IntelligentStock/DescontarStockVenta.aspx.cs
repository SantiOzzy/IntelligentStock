using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

// CU-006-037 Descontar stock por venta.
//
// Los pedidos los toma y los factura el punto de venta del local; esta pantalla
// recibe las ventas para descontar del stock lo que lleva cada receta. Hay tres
// formas de cargarlas:
//   - Una venta: la receta, la cantidad y la fecha.
//   - Cierre del día: todas las recetas vendidas en un día, de una vez.
//   - Importar el archivo que exporta el punto de venta (CSV).
// En las tres la grilla muestra antes de confirmar cuánto se descuenta de cada
// insumo y cómo queda el stock. La BLL graba las ventas (tablas Venta y
// ConsumoVenta) y descuenta todo en una sola transacción, recalcula el dígito
// verificador y deja el registro en la bitácora. Si el stock del sistema no
// alcanza, la venta igual se registra y el insumo queda en cero (se avisa para
// corregirlo con un ajuste). Si un insumo queda en su mínimo, el
// ControladorStock (Observer) avisa por correo.
public partial class DescontarStockVenta : PaginaBase
{
    private const int ModoUna = 0;
    private const int ModoCierre = 1;
    private const int ModoArchivo = 2;

    private const string ClaveImportacion = "ImportacionVentas";

    private BLLVenta bllVenta = new BLLVenta();

    protected override string PatenteRequerida
    {
        get { return "DescontarStockVenta"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidadorDeRango.CargarFechas(rvFecha, DateTime.Today.AddDays(-BLLVenta.DiasMaximosHaciaAtras), DateTime.Today);
        ValidadorDeRango.CargarFechas(rvFechaCierre, DateTime.Today.AddDays(-BLLVenta.DiasMaximosHaciaAtras), DateTime.Today);

        if (!IsPostBack)
        {
            txtFecha.Text = DateTime.Today.ToString("yyyy-MM-dd");
            txtFechaCierre.Text = DateTime.Today.ToString("yyyy-MM-dd");
            Session.Remove(ClaveImportacion);
            CargarRecetas();
            CambiarModo(ModoUna);
        }
    }

    protected void lnkModo_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        CambiarModo(int.Parse(((LinkButton)sender).CommandArgument, CultureInfo.InvariantCulture));
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

    protected void txtFechaCierre_TextChanged(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        AvisarVentasDelDia();
    }

    protected void btnPrevisualizarCierre_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        if (!IsValid)
        {
            return;
        }

        List<BEVenta> ventas = LeerCierre();
        if (ventas == null)
        {
            return;
        }
        if (ventas.Count == 0)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Ingrese la cantidad vendida de al menos una receta."), true);
            LimpiarDetalle();
            return;
        }

        AvisarVentasDelDia();
        MostrarVistaPrevia(ventas);
    }

    protected void btnLeerArchivo_Click(object sender, EventArgs e)
    {
        OcultarMensaje(lblMensaje);
        Session.Remove(ClaveImportacion);
        pnlArchivo.Visible = false;
        litAvisosArchivo.Text = string.Empty;
        LimpiarDetalle();

        if (!fuVentas.HasFile)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002", "Elija el archivo de ventas."), true);
            return;
        }

        try
        {
            ResultadoImportacionVentas resultado = bllVenta.LeerArchivo(fuVentas.FileBytes, fuVentas.FileName);
            Session[ClaveImportacion] = resultado;
            MostrarImportacion(resultado);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (!IsValid)
        {
            return;
        }

        try
        {
            switch (mvModo.ActiveViewIndex)
            {
                case ModoCierre:
                    RegistrarCierre();
                    break;
                case ModoArchivo:
                    RegistrarArchivo();
                    break;
                default:
                    RegistrarUna();
                    break;
            }
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

    #region Modos

    private void CambiarModo(int modo)
    {
        mvModo.ActiveViewIndex = modo;
        tabUna.Attributes["class"] = modo == ModoUna ? "active" : string.Empty;
        tabCierre.Attributes["class"] = modo == ModoCierre ? "active" : string.Empty;
        tabArchivo.Attributes["class"] = modo == ModoArchivo ? "active" : string.Empty;

        btnAceptar.ValidationGroup = modo == ModoUna ? "Venta" : modo == ModoCierre ? "Cierre" : string.Empty;
        btnAceptar.CausesValidation = modo != ModoArchivo;
        btnAceptar.Text = modo == ModoUna ? "Descontar stock" : modo == ModoCierre ? "Registrar el cierre del día" : "Registrar las ventas del archivo";
        gvDetalle.Columns[1].Visible = modo == ModoUna;

        LimpiarDetalle();
        if (modo == ModoUna)
        {
            Previsualizar();
        }
        else if (modo == ModoCierre)
        {
            if (gvCierre.Rows.Count == 0)
            {
                CargarCierre();
            }
            AvisarVentasDelDia();
        }
        else
        {
            ResultadoImportacionVentas resultado = Session[ClaveImportacion] as ResultadoImportacionVentas;
            if (resultado != null)
            {
                MostrarImportacion(resultado);
            }
        }
    }

    private void LimpiarDetalle()
    {
        gvDetalle.DataSource = null;
        gvDetalle.DataBind();
        litIndicadores.Text = string.Empty;
    }

    #endregion

    #region Una venta

    private void RegistrarUna()
    {
        int idReceta;
        int.TryParse(ddlReceta.SelectedValue, out idReceta);

        if (idReceta <= 0 || string.IsNullOrEmpty(txtCantidad.Text.Trim()) || string.IsNullOrEmpty(txtFecha.Text.Trim()))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Elija la receta e ingrese la cantidad y la fecha de la venta."), true);
            return;
        }

        decimal cantidad;
        if (!Numeros.TryParseDecimal(txtCantidad.Text, out cantidad))
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La cantidad vendida tiene que ser un número."), true);
            return;
        }

        DateTime? fecha = LeerFecha(txtFecha.Text);
        if (!fecha.HasValue)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        try
        {
            List<BEConsumoVenta> consumos = bllVenta.Registrar(idReceta, cantidad, fecha);
            MostrarResultado(consumos, "Se registró la venta de " + cantidad.ToString("0.##") + " x " +
                                       ddlReceta.SelectedItem.Text, cantidad.ToString("0.##") + " x " + ddlReceta.SelectedItem.Text);
            txtCantidad.Text = string.Empty;
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
            Previsualizar();
        }
    }

    /// Muestra lo que se descontaría con la receta y la cantidad elegidas.
    private void Previsualizar()
    {
        LimpiarDetalle();

        int idReceta;
        decimal cantidad;
        if (!int.TryParse(ddlReceta.SelectedValue, out idReceta) || idReceta <= 0 ||
            !Numeros.TryParseDecimal(txtCantidad.Text, out cantidad) || cantidad <= 0)
        {
            return;
        }

        try
        {
            MostrarConsumos(bllVenta.Previsualizar(idReceta, cantidad), true);
        }
        catch (Exception ex)
        {
            if (!lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, ex.Message, true);
            }
        }
    }

    private void CargarRecetas()
    {
        try
        {
            foreach (BEReceta receta in bllVenta.TraerRecetasVendibles())
            {
                ddlReceta.Items.Add(new ListItem(receta.Nombre, receta.IdReceta.ToString()));
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

    #endregion

    #region Cierre del día

    private void CargarCierre()
    {
        try
        {
            gvCierre.DataSource = bllVenta.TraerRecetasVendibles();
            gvCierre.DataBind();
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Las recetas con cantidad cargada. Devuelve null (y muestra el error) si
    /// alguna cantidad no es válida.
    private List<BEVenta> LeerCierre()
    {
        List<BEVenta> ventas = new List<BEVenta>();
        foreach (GridViewRow fila in gvCierre.Rows)
        {
            TextBox txt = (TextBox)fila.FindControl("txtCantidadReceta");
            string texto = txt == null ? string.Empty : txt.Text.Trim();
            if (texto.Length == 0)
            {
                continue;
            }

            decimal cantidad;
            string receta = HttpUtility.HtmlDecode(fila.Cells[0].Text);
            if (!Numeros.TryParseDecimal(texto, out cantidad) || cantidad < 0)
            {
                MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                    "La cantidad de " + receta + " no es válida."), true);
                return null;
            }
            if (cantidad == 0)
            {
                continue;
            }

            BEVenta venta = new BEVenta();
            venta.IdReceta = (int)gvCierre.DataKeys[fila.RowIndex].Value;
            venta.NombreReceta = receta;
            venta.Cantidad = cantidad;
            ventas.Add(venta);
        }
        return ventas;
    }

    private void RegistrarCierre()
    {
        List<BEVenta> ventas = LeerCierre();
        if (ventas == null)
        {
            return;
        }

        DateTime? fecha = LeerFecha(txtFechaCierre.Text);
        if (!fecha.HasValue)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025", "La fecha no es válida."), true);
            return;
        }

        decimal unidades = 0;
        foreach (BEVenta venta in ventas)
        {
            unidades += venta.Cantidad;
        }

        List<BEConsumoVenta> consumos = bllVenta.RegistrarCierreDelDia(ventas, fecha);

        foreach (GridViewRow fila in gvCierre.Rows)
        {
            TextBox txt = (TextBox)fila.FindControl("txtCantidadReceta");
            if (txt != null)
            {
                txt.Text = string.Empty;
            }
        }
        lblAvisoCierre.Visible = false;

        MostrarResultado(consumos, "Se registró el cierre del " + fecha.Value.ToString("dd/MM/yyyy") + ": " +
                                   Textos.Contar(ventas.Count, "receta", "recetas") + ", " + unidades.ToString("0.##") + " unidades",
                         Textos.Contar(ventas.Count, "receta", "recetas") + " (" + unidades.ToString("0.##") + " u.)");
    }

    /// Avisa si el día elegido ya tiene ventas cargadas, para no contarlas dos veces.
    private void AvisarVentasDelDia()
    {
        lblAvisoCierre.Visible = false;
        DateTime? fecha = LeerFecha(txtFechaCierre.Text);
        if (!fecha.HasValue)
        {
            return;
        }

        try
        {
            List<BEVenta> ventas = bllVenta.TraerVentasDelDia(fecha.Value);
            if (ventas.Count == 0)
            {
                return;
            }

            decimal unidades = 0;
            foreach (BEVenta venta in ventas)
            {
                unidades += venta.Cantidad;
            }
            lblAvisoCierre.Text = "El " + fecha.Value.ToString("dd/MM/yyyy") + " ya tiene " +
                                  Textos.Contar(ventas.Count, "venta registrada", "ventas registradas") + " (" +
                                  unidades.ToString("0.##") + " unidades). Cargue solo lo que falta, para no contarlas dos veces.";
            lblAvisoCierre.Visible = true;
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    #endregion

    #region Archivo del punto de venta

    private void MostrarImportacion(ResultadoImportacionVentas resultado)
    {
        pnlArchivo.Visible = true;
        gvArchivo.DataSource = resultado.Validas;
        gvArchivo.DataBind();
        pnlErrores.Visible = resultado.ConError.Count > 0;
        gvErrores.DataSource = resultado.ConError;
        gvErrores.DataBind();

        string avisos = string.Empty;
        foreach (string aviso in resultado.Avisos)
        {
            avisos += "<div class=\"alert alert-warning\">" + HttpUtility.HtmlEncode(aviso) + "</div>";
        }
        litAvisosArchivo.Text = avisos;

        if (resultado.Validas.Count > 0)
        {
            MostrarVistaPrevia(resultado.Validas);
        }
        else
        {
            LimpiarDetalle();
        }

        if (resultado.Validas.Count == 0)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR025",
                "El archivo " + resultado.NombreArchivo + " no tiene ventas válidas para registrar."), true);
        }
        else if (!lblMensaje.Visible)
        {
            MostrarMensaje(lblMensaje, "Se leyeron " + Textos.Contar(resultado.Validas.Count, "venta", "ventas") +
                                       " (" + resultado.UnidadesValidas.ToString("0.##") + " unidades) de " + resultado.NombreArchivo +
                                       (resultado.ConError.Count > 0
                                           ? " y " + Textos.Contar(resultado.ConError.Count, "línea con errores", "líneas con errores") + ", que no se van a registrar"
                                           : string.Empty) +
                                       ". Revise la grilla y confirme.", false);
        }
    }

    private void RegistrarArchivo()
    {
        ResultadoImportacionVentas resultado = Session[ClaveImportacion] as ResultadoImportacionVentas;
        if (resultado == null || resultado.Validas.Count == 0)
        {
            MostrarMensaje(lblMensaje, AdministradorErrores.Mensaje("ERR002",
                "Primero elija el archivo y presione Leer archivo."), true);
            return;
        }

        List<BEConsumoVenta> consumos = bllVenta.RegistrarImportadas(resultado.Validas, resultado.NombreArchivo);
        Session.Remove(ClaveImportacion);
        pnlArchivo.Visible = false;
        litAvisosArchivo.Text = string.Empty;

        MostrarResultado(consumos, "Se registraron " + Textos.Contar(resultado.Validas.Count, "venta", "ventas") + " (" +
                                   resultado.UnidadesValidas.ToString("0.##") + " unidades) del archivo " + resultado.NombreArchivo,
                         Textos.Contar(resultado.Validas.Count, "venta", "ventas") + " importadas");
    }

    #endregion

    #region Grilla de stock

    private void MostrarVistaPrevia(List<BEVenta> ventas)
    {
        try
        {
            MostrarConsumos(bllVenta.Previsualizar(ventas), true);
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Muestra lo que se descuenta de cada insumo. En la vista previa marca en
    /// rojo lo que no alcanza y en amarillo lo que queda en su mínimo.
    private void MostrarConsumos(List<BEConsumoVenta> consumos, bool vistaPrevia)
    {
        gvDetalle.DataSource = consumos;
        gvDetalle.DataBind();

        int sinStock = 0, enMinimo = 0;
        for (int i = 0; i < consumos.Count && i < gvDetalle.Rows.Count; i++)
        {
            if (consumos[i].SinStock)
            {
                gvDetalle.Rows[i].CssClass = "danger";
                sinStock++;
            }
            else if (consumos[i].QuedaEnMinimo)
            {
                gvDetalle.Rows[i].CssClass = "warning";
                enMinimo++;
            }
        }

        if (vistaPrevia)
        {
            litIndicadores.Text = new Indicadores()
                .Agregar("Insumos que se descuentan", consumos.Count.ToString(), "destacado")
                .Agregar("No alcanza el stock", sinStock.ToString(), sinStock > 0 ? "peligro" : "exito")
                .Agregar("Quedarían en su mínimo", enMinimo.ToString(), enMinimo > 0 ? "aviso" : null)
                .ToString();

            if (sinStock > 0 && !lblMensaje.Visible)
            {
                MostrarMensaje(lblMensaje, "Los insumos marcados en rojo no alcanzan según el stock del sistema. " +
                                           "La venta se puede registrar igual: esos insumos quedan en cero y conviene " +
                                           "revisarlos con un ajuste de stock.", true);
            }
        }
    }

    /// Muestra el resultado de un registro: cómo quedó el stock, los insumos
    /// que quedaron en su mínimo y los que no alcanzaban.
    private void MostrarResultado(List<BEConsumoVenta> consumos, string inicioMensaje, string indicador)
    {
        MostrarConsumos(consumos, false);

        List<string> enMinimo = new List<string>();
        List<string> faltantes = new List<string>();
        foreach (BEConsumoVenta consumo in consumos)
        {
            if (consumo.SinStock)
            {
                faltantes.Add(consumo.NombreInsumo + " (faltó " + consumo.FaltanteTexto + ")");
            }
            else if (consumo.QuedaEnMinimo)
            {
                enMinimo.Add(consumo.NombreInsumo);
            }
        }

        string mensaje = inicioMensaje + " y se descontó el stock de " + Textos.Contar(consumos.Count, "insumo", "insumos") + ".";
        if (enMinimo.Count > 0)
        {
            mensaje += " Quedaron en su stock mínimo o por debajo: " + string.Join(", ", enMinimo.ToArray()) + ".";
        }
        if (faltantes.Count > 0)
        {
            mensaje += " El stock del sistema no alcanzaba y quedó en cero: " + string.Join(", ", faltantes.ToArray()) +
                       ". Revíselo con un ajuste de stock.";
        }

        litIndicadores.Text = new Indicadores()
            .Agregar("Registrado", indicador, "destacado")
            .Agregar("Insumos descontados", consumos.Count.ToString(), "exito")
            .Agregar("En su mínimo o por debajo", enMinimo.Count.ToString(), enMinimo.Count > 0 ? "aviso" : null)
            .Agregar("Sin stock suficiente", faltantes.Count.ToString(), faltantes.Count > 0 ? "peligro" : null)
            .ToString();

        MostrarMensaje(lblMensaje, mensaje, false);
    }

    #endregion

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
