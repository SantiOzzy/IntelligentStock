using System;
using System.Collections.Generic;
using System.Xml;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de recetas (CU-005-028 a CU-005-031).
    ///
    /// Además de la base, cada cambio regenera el catálogo de recetas en XML
    /// (App_Data\CatalogoRecetas.xml), que es el que leen las pantallas para
    /// mostrar los insumos de cada receta.
    public class BLLReceta
    {
        private const string TablaReceta = "Receta";
        private const string TablaDetalle = "DetalleReceta";

        public const int LargoNombre = 100;

        /// Máximo que admite la columna cantidad, DECIMAL(8,2).
        public const decimal CantidadMaxima = 999999.99m;

        private readonly DALReceta dalReceta = new DALReceta();
        private readonly DALCatalogoRecetas dalCatalogo = new DALCatalogoRecetas();
        private readonly BLLInsumo bllInsumo = new BLLInsumo();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        #region Consultas

        public List<BEReceta> TraerRecetas()
        {
            return dalReceta.TraerRecetas();
        }

        /// Recetas habilitadas (las dadas de baja no se ofrecen).
        public List<BEReceta> TraerActivas()
        {
            return TraerRecetas().FindAll(delegate(BEReceta receta) { return receta.Activo; });
        }

        /// Una receta con su detalle de insumos.
        public BEReceta ObtenerPorId(int idReceta)
        {
            return idReceta <= 0 ? null : dalReceta.ObtenerPorId(idReceta);
        }

        /// CU-005-031 Consultar receta. Filtra por nombre y por estado.
        public List<BEReceta> Filtrar(string texto, string estado)
        {
            string busqueda = texto == null ? string.Empty : texto.Trim();
            List<BEReceta> resultado = new List<BEReceta>();

            // Si se escribió un número, también se busca la receta con ese código.
            int codigo;
            bool buscaCodigo = int.TryParse(busqueda, out codigo);

            foreach (BEReceta receta in TraerRecetas())
            {
                if (!string.IsNullOrEmpty(estado) && receta.Estado != estado)
                {
                    continue;
                }

                if (busqueda.Length > 0 &&
                    receta.Nombre.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) < 0 &&
                    !(buscaCodigo && receta.IdReceta == codigo))
                {
                    continue;
                }

                resultado.Add(receta);
            }

            return resultado;
        }

        #endregion

        #region ABM

        /// CU-005-028 Alta de receta. La receta y su detalle se graban juntos, en
        /// una sola transacción. Devuelve el id generado.
        public int Alta(BEReceta receta)
        {
            Normalizar(receta);
            ValidarDatos(receta);
            VerificarNombreUnico(receta.Nombre, 0);

            int id = bllDV.EjecutarControlado(() => dalReceta.Crear(receta), TablaReceta, TablaDetalle);
            receta.IdReceta = id;

            ActualizarCatalogo();

            bllEvento.RegistrarDelUsuarioActual("Alta de receta",
                "Se dio de alta la receta " + receta.Nombre + " (id " + id + ") con " +
                receta.Detalles.Count + " insumo(s).");

            return id;
        }

        /// CU-005-029 Modificar receta. Se reemplaza el detalle completo.
        public void Modificar(BEReceta receta)
        {
            Normalizar(receta);
            ValidarDatos(receta);

            BEReceta anterior = dalReceta.ObtenerPorId(receta.IdReceta);
            if (anterior == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!anterior.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La receta se encuentra dada de baja."));
            }

            VerificarNombreUnico(receta.Nombre, receta.IdReceta);

            bllDV.EjecutarControlado(() => dalReceta.Modificar(receta), TablaReceta, TablaDetalle);
            ActualizarCatalogo();

            bllEvento.RegistrarDelUsuarioActual("Modificación de receta",
                "Se modificó la receta " + receta.Nombre + " (id " + receta.IdReceta + "). Ahora lleva " +
                receta.Detalles.Count + " insumo(s).");
        }

        /// CU-005-030 Baja de receta. Es lógica: la receta deja de poder venderse
        /// pero se conserva su detalle.
        public void Baja(int idReceta)
        {
            BEReceta receta = ObtenerPorId(idReceta);
            if (receta == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!receta.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La receta ya se encuentra dada de baja."));
            }

            bllDV.EjecutarControlado(() => dalReceta.Baja(idReceta), TablaReceta);
            ActualizarCatalogo();

            bllEvento.RegistrarDelUsuarioActual("Baja de receta",
                "Se dio de baja la receta " + receta.Nombre + " (id " + idReceta + ").");
        }

        /// Vuelve a habilitar una receta dada de baja. Todos sus insumos tienen
        /// que estar activos: una receta activa no puede llevar un insumo que ya
        /// no se usa. Se hace desde Baja de receta, marcando "Mostrar también las
        /// dadas de baja".
        public void Reactivar(int idReceta)
        {
            BEReceta receta = ObtenerPorId(idReceta);
            if (receta == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (receta.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La receta ya se encuentra activa."));
            }

            List<string> inactivos = new List<string>();
            foreach (BEDetalleReceta detalle in receta.Detalles)
            {
                BEInsumo insumo = bllInsumo.ObtenerPorId(detalle.IdInsumo);
                if (insumo == null || !insumo.Activo)
                {
                    inactivos.Add(detalle.NombreInsumo);
                }
            }

            if (inactivos.Count > 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La receta lleva insumos dados de baja: " + string.Join(", ", inactivos.ToArray()) +
                    ". Reactívelos primero desde Baja de insumo."));
            }

            bllDV.EjecutarControlado(() => dalReceta.Activar(idReceta), TablaReceta);
            ActualizarCatalogo();

            bllEvento.RegistrarDelUsuarioActual("Reactivación de receta",
                "Se reactivó la receta " + receta.Nombre + " (id " + idReceta + ").");
        }

        #endregion

        #region Detalle en edición

        /// Agrega un insumo al detalle que se está armando en pantalla, o le
        /// actualiza la cantidad si ya estaba. Devuelve true si lo actualizó.
        public bool AgregarAlDetalle(List<BEDetalleReceta> detalles, int idInsumo, decimal cantidad)
        {
            cantidad = Math.Round(cantidad, 2);
            ValidarCantidad(cantidad, null);

            BEInsumo insumo = bllInsumo.ObtenerPorId(idInsumo);
            if (insumo == null || !insumo.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Seleccione un insumo activo."));
            }

            foreach (BEDetalleReceta detalle in detalles)
            {
                if (detalle.IdInsumo == idInsumo)
                {
                    detalle.Cantidad = cantidad;
                    return true;
                }
            }

            detalles.Add(new BEDetalleReceta(insumo.IdInsumo, insumo.Nombre, insumo.UnidadDeMedida, cantidad));
            detalles.Sort(delegate(BEDetalleReceta a, BEDetalleReceta b)
            {
                return string.Compare(a.NombreInsumo, b.NombreInsumo, StringComparison.OrdinalIgnoreCase);
            });
            return false;
        }

        public void QuitarDelDetalle(List<BEDetalleReceta> detalles, int idInsumo)
        {
            detalles.RemoveAll(delegate(BEDetalleReceta detalle) { return detalle.IdInsumo == idInsumo; });
        }

        #endregion

        #region Catálogo XML

        /// Devuelve el catálogo de recetas como documento XML. Si todavía no
        /// existe el archivo, lo genera.
        public XmlDocument ObtenerCatalogo()
        {
            if (!dalCatalogo.Existe())
            {
                dalCatalogo.Exportar(dalReceta.TraerRecetasConDetalle());
            }
            return dalCatalogo.Cargar();
        }

        /// Vuelve a generar el catálogo desde la base y lo devuelve. Se usa
        /// cuando el archivo quedó desactualizado (por ejemplo, después de
        /// restaurar la base desde un backup).
        public XmlDocument RegenerarCatalogo()
        {
            try
            {
                dalCatalogo.Exportar(dalReceta.TraerRecetasConDetalle());
            }
            catch (UnauthorizedAccessException)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR022",
                    "No se pudo actualizar el catálogo de recetas (App_Data\\CatalogoRecetas.xml)."));
            }
            catch (System.IO.IOException)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR015",
                    "No se pudo actualizar el catálogo de recetas (App_Data\\CatalogoRecetas.xml)."));
            }
            return dalCatalogo.Cargar();
        }

        /// Regenera el catálogo XML con el estado actual de la base. Si no se
        /// puede escribir el archivo, la receta igual queda grabada: el catálogo
        /// se vuelve a generar en el próximo cambio o cuando alguien lo pida.
        private void ActualizarCatalogo()
        {
            try
            {
                dalCatalogo.Exportar(dalReceta.TraerRecetasConDetalle());
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Validaciones internas

        private static void Normalizar(BEReceta receta)
        {
            if (receta == null)
            {
                return;
            }

            receta.Nombre = receta.Nombre == null ? string.Empty : receta.Nombre.Trim();
            foreach (BEDetalleReceta detalle in receta.Detalles)
            {
                detalle.Cantidad = Math.Round(detalle.Cantidad, 2);
            }
        }

        /// Controla el nombre, que haya al menos un insumo, que ninguno esté
        /// repetido ni dado de baja y que las cantidades sean válidas. De paso
        /// refresca nombre y unidad de cada insumo con lo que hay en la base.
        private void ValidarDatos(BEReceta receta)
        {
            if (receta == null || string.IsNullOrEmpty(receta.Nombre))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            if (receta.Nombre.Length > LargoNombre)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El nombre no puede superar los " + LargoNombre + " caracteres."));
            }

            if (receta.Detalles.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR017",
                    "La receta tiene que llevar al menos un insumo."));
            }

            Dictionary<int, BEInsumo> insumos = new Dictionary<int, BEInsumo>();
            foreach (BEInsumo insumo in bllInsumo.TraerInsumos())
            {
                insumos[insumo.IdInsumo] = insumo;
            }

            HashSet<int> vistos = new HashSet<int>();
            foreach (BEDetalleReceta detalle in receta.Detalles)
            {
                BEInsumo insumo;
                if (!insumos.TryGetValue(detalle.IdInsumo, out insumo))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR013",
                        "Uno de los insumos de la receta ya no existe."));
                }

                if (!insumo.Activo)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                        "El insumo " + insumo.Nombre + " está dado de baja: quítelo de la receta."));
                }

                if (!vistos.Add(detalle.IdInsumo))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                        "El insumo " + insumo.Nombre + " está repetido en la receta."));
                }

                ValidarCantidad(detalle.Cantidad, insumo.Nombre);

                detalle.NombreInsumo = insumo.Nombre;
                detalle.UnidadDeMedida = insumo.UnidadDeMedida;
            }
        }

        private static void ValidarCantidad(decimal cantidad, string nombreInsumo)
        {
            string deInsumo = string.IsNullOrEmpty(nombreInsumo) ? string.Empty : " de " + nombreInsumo;

            if (cantidad <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad" + deInsumo + " tiene que ser mayor que cero."));
            }

            if (cantidad > CantidadMaxima)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad" + deInsumo + " supera el máximo admitido."));
            }
        }

        /// Dos recetas no pueden llamarse igual (sin distinguir mayúsculas).
        private void VerificarNombreUnico(string nombre, int idExcluido)
        {
            foreach (BEReceta otra in dalReceta.TraerRecetas())
            {
                if (otra.IdReceta != idExcluido &&
                    string.Equals(otra.Nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase))
                {
                    string detalle = "Ya existe una receta llamada " + otra.Nombre;
                    detalle += otra.Activo ? "." : ", dada de baja. Puede reactivarla desde Baja de receta.";
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR010", detalle));
                }
            }
        }

        #endregion
    }
}
