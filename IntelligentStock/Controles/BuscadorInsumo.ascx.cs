using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using BE;
using BLL;

// Componente personalizado BuscadorInsumo.
//
// Muestra la lista de insumos activos (con su unidad y el stock actual) y un
// cuadro de texto que la va filtrando a medida que se escribe, así el empleado
// no tiene que recorrer una lista desplegable enorme ni acordarse del nombre
// exacto: escribe parte del nombre y elige de lo que queda. Las páginas que lo
// usan solo le piden IdInsumoSeleccionado; la validación de que el insumo siga
// activo la hace la capa de negocio al operar.
public partial class Controles_BuscadorInsumo : UserControl
{
    private BLLInsumo bllInsumo = new BLLInsumo();

    /// Id del insumo elegido en la lista, o 0 si no se eligió ninguno.
    public int IdInsumoSeleccionado
    {
        get
        {
            int id;
            return int.TryParse(lstInsumos.SelectedValue, out id) ? id : 0;
        }
    }

    /// Nombre del insumo elegido.
    public string NombreInsumo
    {
        get
        {
            int id = IdInsumoSeleccionado;
            if (id == 0)
            {
                return string.Empty;
            }

            BEInsumo insumo = bllInsumo.ObtenerPorId(id);
            return insumo == null ? string.Empty : insumo.Nombre;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            CargarOpciones();
        }
    }

    /// Deja el buscador vacío, por ejemplo después de agregar el insumo a una grilla.
    public void Limpiar()
    {
        txtNombreInsumo.Text = string.Empty;
        lstInsumos.ClearSelection();
        lblResultado.Text = string.Empty;
    }

    /// Vuelve a leer los insumos activos (por ejemplo, después de un ajuste de
    /// stock, para que la lista muestre el stock nuevo). Conserva la selección.
    public void RecargarOpciones()
    {
        int elegido = IdInsumoSeleccionado;
        CargarOpciones();

        ListItem item = lstInsumos.Items.FindByValue(elegido.ToString());
        if (item != null)
        {
            item.Selected = true;
        }
    }

    private void CargarOpciones()
    {
        lstInsumos.Items.Clear();
        lblResultado.Text = string.Empty;

        try
        {
            foreach (BEInsumo insumo in bllInsumo.TraerActivos())
            {
                lstInsumos.Items.Add(new ListItem(
                    insumo.Etiqueta + " - stock: " + insumo.StockValor, insumo.IdInsumo.ToString()));
            }

            if (lstInsumos.Items.Count == 0)
            {
                MostrarResultado("No hay insumos activos cargados.");
            }
        }
        catch (Exception ex)
        {
            MostrarResultado(ex.Message);
        }
    }

    private void MostrarResultado(string texto)
    {
        lblResultado.Text = HttpUtility.HtmlEncode(texto);
        lblResultado.CssClass = "help-block text-danger";
    }
}
