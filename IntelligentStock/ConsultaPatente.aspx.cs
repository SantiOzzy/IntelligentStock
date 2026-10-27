using System;
using System.Collections.Generic;
using System.Text;
using BE;
using BLL;
using Servicios;

// CU-003-008 Consultar patente.
public partial class ConsultaPatente : PaginaBase
{
    private BLLPermiso bllPermiso = new BLLPermiso();

    protected override string PatenteRequerida
    {
        get { return "ConsultaPatente"; }
    }

    // Lo que se muestra en la grilla no sale de una sola tabla: hay que cruzar la
    // patente con las familias que la contienen y con los roles que la terminan
    // teniendo. Por eso se arma esta fila a medida.
    public class FilaPatente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Familias { get; set; }
        public string Roles { get; set; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            Cargar();
        }
    }

    protected void btnBuscar_Click(object sender, EventArgs e)
    {
        Cargar();
    }

    private void Cargar()
    {
        OcultarMensaje(lblMensaje);

        try
        {
            List<Familia> familias = bllPermiso.TraerFamilias();
            List<BERol> roles = bllPermiso.TraerRoles();
            string busqueda = txtBusqueda.Text.Trim();

            List<FilaPatente> filas = new List<FilaPatente>();

            int idBuscado;
            bool buscaPorId = int.TryParse(busqueda, out idBuscado);

            foreach (Patente patente in bllPermiso.TraerPatentes())
            {
                bool coincideId = buscaPorId && patente.Id == idBuscado;
                bool coincideNombre = patente.Nombre.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0;

                if (busqueda.Length > 0 && !coincideId && !coincideNombre)
                {
                    continue;
                }

                FilaPatente fila = new FilaPatente();
                fila.Id = patente.Id;
                fila.Nombre = patente.Nombre;
                fila.Descripcion = MenuSistema.DescribirPatente(patente.Nombre);
                fila.Familias = BuscarFamilias(familias, patente.Nombre);
                fila.Roles = BuscarRoles(roles, patente.Nombre);

                filas.Add(fila);
            }

            gvPatentes.DataSource = filas;
            gvPatentes.DataBind();

            if (filas.Count == 0)
            {
                MostrarError(lblMensaje, "ERR013");
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje(lblMensaje, ex.Message, true);
        }
    }

    // Contiene() recorre todo el subárbol, así que también aparecen las familias
    // que la incluyen indirectamente a través de otra familia.
    private static string BuscarFamilias(List<Familia> familias, string nombrePatente)
    {
        List<string> nombres = new List<string>();

        foreach (Familia familia in familias)
        {
            if (familia.Contiene(nombrePatente))
            {
                nombres.Add(familia.Nombre);
            }
        }

        return Unir(nombres);
    }

    private static string BuscarRoles(List<BERol> roles, string nombrePatente)
    {
        List<string> nombres = new List<string>();

        foreach (BERol rol in roles)
        {
            if (rol.TienePermiso(nombrePatente))
            {
                nombres.Add(rol.Nombre);
            }
        }

        return Unir(nombres);
    }

    private static string Unir(List<string> nombres)
    {
        if (nombres.Count == 0)
        {
            return "-";
        }

        StringBuilder texto = new StringBuilder();
        for (int i = 0; i < nombres.Count; i++)
        {
            if (i > 0)
            {
                texto.Append(", ");
            }
            texto.Append(nombres[i]);
        }
        return texto.ToString();
    }
}
