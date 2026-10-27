using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Xml;
using BE;
using BLL;
using Servicios;

// CU-005-031 Consulta de recetas.
//
// El listado sale de la base. Los insumos de cada receta, en cambio, se leen
// del catálogo XML (App_Data\CatalogoRecetas.xml): al entrar a la pantalla el
// archivo se carga en memoria como XmlDocument y queda guardado en la sesión,
// así las búsquedas siguientes no vuelven a leerlo del disco.
public partial class ConsultaReceta : PaginaBase
{
    private const string ClaveCatalogo = "RecetasEnSesion";

    private BLLReceta bllReceta = new BLLReceta();

    /// Insumos de cada receta, ya armados como texto, indexados por id.
    private Dictionary<string, string> insumosPorReceta;

    /// Cantidad de insumos de cada receta según el XML, para detectar si el
    /// archivo quedó desactualizado respecto de la base.
    private Dictionary<string, int> cantidadPorReceta;

    protected override string PatenteRequerida
    {
        get { return "ConsultaReceta"; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarCatalogo();
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

    private void CargarCatalogo()
    {
        try
        {
            Session[ClaveCatalogo] = bllReceta.ObtenerCatalogo();
        }
        catch (Exception)
        {
            Session[ClaveCatalogo] = null;
        }
    }

    private void Buscar()
    {
        OcultarMensaje(lblMensaje);

        try
        {
            List<BEReceta> recetas = bllReceta.Filtrar(txtBusqueda.Text, ddlEstado.SelectedValue);

            LeerInsumosDelCatalogo(Session[ClaveCatalogo] as XmlDocument);

            // Si el catálogo no se puede regenerar (por ejemplo, el sitio no tiene
            // permiso de escritura en App_Data) la grilla se muestra igual: lo
            // único que falta es el detalle de insumos de las recetas nuevas.
            string avisoCatalogo = null;
            if (!CatalogoAlDia(recetas))
            {
                try
                {
                    Session[ClaveCatalogo] = bllReceta.RegenerarCatalogo();
                    LeerInsumosDelCatalogo(Session[ClaveCatalogo] as XmlDocument);
                }
                catch (Exception ex)
                {
                    avisoCatalogo = ex.Message;
                }
            }

            gvDetalle.DataSource = recetas;
            gvDetalle.DataBind();

            if (recetas.Count == 0)
            {
                MostrarError(lblMensaje, "ERR013");
                litResumen.Text = string.Empty;
            }
            else
            {
                litResumen.Text = Textos.Contar(recetas.Count, "receta encontrada.", "recetas encontradas.");

                if (avisoCatalogo != null)
                {
                    MostrarMensaje(lblMensaje, avisoCatalogo, true);
                }
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    /// Recorre los nodos del catálogo: cada <Receta> tiene su <IdReceta> y
    /// dentro de <DetalleInsumos> un nodo <Insumo> por cada ingrediente.
    private void LeerInsumosDelCatalogo(XmlDocument catalogo)
    {
        insumosPorReceta = new Dictionary<string, string>();
        cantidadPorReceta = new Dictionary<string, int>();
        if (catalogo == null || catalogo.DocumentElement == null)
        {
            return;
        }

        foreach (XmlNode receta in catalogo.DocumentElement.ChildNodes)
        {
            XmlNode id = receta["IdReceta"];
            XmlNode detalle = receta["DetalleInsumos"];
            if (id == null || detalle == null)
            {
                continue;
            }

            List<string> insumos = new List<string>();
            foreach (XmlNode insumo in detalle.ChildNodes)
            {
                string cantidad = Atributo(insumo, "cantidad");
                decimal valor;
                if (decimal.TryParse(cantidad, NumberStyles.Number, CultureInfo.InvariantCulture, out valor))
                {
                    cantidad = valor.ToString("0.##");
                }

                insumos.Add(HttpUtility.HtmlEncode(insumo.InnerText + ": " + cantidad + " " + Atributo(insumo, "unidad")));
            }

            insumosPorReceta[id.InnerText] = insumos.Count == 0 ? "-" : string.Join("<br />", insumos.ToArray());
            cantidadPorReceta[id.InnerText] = insumos.Count;
        }
    }

    /// El XML está al día si tiene todas las recetas listadas, con la misma
    /// cantidad de insumos que figura en la base.
    private bool CatalogoAlDia(List<BEReceta> recetas)
    {
        foreach (BEReceta receta in recetas)
        {
            int cantidad;
            if (!cantidadPorReceta.TryGetValue(receta.IdReceta.ToString(), out cantidad) ||
                cantidad != receta.CantidadInsumos)
            {
                return false;
            }
        }
        return true;
    }

    private static string Atributo(XmlNode nodo, string nombre)
    {
        XmlAttribute atributo = nodo.Attributes == null ? null : nodo.Attributes[nombre];
        return atributo == null ? string.Empty : atributo.Value;
    }

    /// Se llama desde la grilla para cada fila.
    protected string InsumosDeLaReceta(object idReceta)
    {
        string texto;
        if (insumosPorReceta != null &&
            insumosPorReceta.TryGetValue(Convert.ToString(idReceta), out texto))
        {
            return texto;
        }
        return "-";
    }
}
