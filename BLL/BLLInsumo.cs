using System;
using System.Collections.Generic;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de insumos (CU-005-024 a CU-005-027) y del ajuste
    /// manual de stock (CU-005-032).
    public class BLLInsumo
    {
        private const string Tabla = "Insumo";

        /// Acción con la que queda cada ajuste en la bitácora. El historial de
        /// ajustes se arma leyendo los eventos con esta acción.
        public const string AccionAjuste = "Ajuste de stock";

        /// Largos máximos de las columnas de la tabla Insumo.
        public const int LargoNombre = 100;
        public const int LargoDescripcion = 500;
        public const int LargoUnidad = 50;

        /// El motivo del ajuste viaja dentro de la descripción del evento.
        public const int LargoMotivo = 200;

        /// Máximo que admite una columna DECIMAL(10,2). Vale para el stock y
        /// para el precio unitario.
        public const decimal StockMaximo = 99999999.99m;

        /// Tope razonable para la vida útil (100 años).
        public const int VidaUtilMaxima = 36500;

        private readonly DALInsumo dalInsumo = new DALInsumo();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();
        private readonly ControladorStock controladorStock = new ControladorStock();

        public BLLInsumo()
        {
            // Patrón Observer: estas son las acciones que se disparan cuando un
            // descuento deja al insumo en su stock mínimo o por debajo.
            controladorStock.Suscribir(new NotificadorEmailStock());
            controladorStock.Suscribir(new BitacoraAlertaStock());
        }

        #region Consultas

        public List<BEInsumo> TraerInsumos()
        {
            return dalInsumo.TraerInsumos();
        }

        /// Insumos habilitados para operar (los dados de baja no se ofrecen).
        public List<BEInsumo> TraerActivos()
        {
            return TraerInsumos().FindAll(delegate(BEInsumo insumo) { return insumo.Activo; });
        }

        public BEInsumo ObtenerPorId(int idInsumo)
        {
            return idInsumo <= 0 ? null : dalInsumo.ObtenerPorId(idInsumo);
        }

        /// Insumos activos cuyo nombre contiene el texto. Si alguno coincide
        /// exactamente queda primero. Lo usa el control BuscadorInsumo.
        public List<BEInsumo> BuscarActivosPorNombre(string texto)
        {
            List<BEInsumo> resultado = new List<BEInsumo>();
            string busqueda = texto == null ? string.Empty : texto.Trim();
            if (busqueda.Length == 0)
            {
                return resultado;
            }

            foreach (BEInsumo insumo in TraerActivos())
            {
                if (string.Equals(insumo.Nombre, busqueda, StringComparison.OrdinalIgnoreCase))
                {
                    resultado.Insert(0, insumo);
                }
                else if (Contiene(insumo.Nombre, busqueda))
                {
                    resultado.Add(insumo);
                }
            }

            return resultado;
        }

        /// CU-005-027 Consultar insumo. Filtra por nombre o descripción, por
        /// estado y, opcionalmente, solo los que están en su mínimo o por debajo.
        public List<BEInsumo> Filtrar(string texto, string estado, bool soloBajoMinimo)
        {
            string busqueda = texto == null ? string.Empty : texto.Trim();
            List<BEInsumo> resultado = new List<BEInsumo>();

            // Si se escribió un número, también se busca el insumo con ese código.
            int codigo;
            bool buscaCodigo = int.TryParse(busqueda, out codigo);

            foreach (BEInsumo insumo in TraerInsumos())
            {
                if (!string.IsNullOrEmpty(estado) && insumo.Estado != estado)
                {
                    continue;
                }

                if (soloBajoMinimo && !insumo.BajoMinimo)
                {
                    continue;
                }

                if (busqueda.Length > 0 &&
                    !Contiene(insumo.Nombre, busqueda) &&
                    !Contiene(insumo.Descripcion, busqueda) &&
                    !(buscaCodigo && insumo.IdInsumo == codigo))
                {
                    continue;
                }

                resultado.Add(insumo);
            }

            return resultado;
        }

        /// Valor en riesgo ante una falla de la cadena de frío: lo que vale el
        /// stock de los insumos activos que son perecederos (los que tienen
        /// días de vida útil cargados). Lo muestra el control ImpactoEconomico
        /// al procesar una alerta térmica.
        public decimal CalcularValorStockPerecedero()
        {
            decimal total = 0m;

            foreach (BEInsumo insumo in TraerActivos())
            {
                if (insumo.EsPerecedero)
                {
                    total += insumo.ValorStock;
                }
            }

            return Math.Round(total, 2);
        }

        #endregion

        #region ABM

        /// CU-005-024 Alta de insumo. Devuelve el id generado.
        public int Alta(BEInsumo insumo)
        {
            Normalizar(insumo);
            ValidarDatos(insumo);

            if (insumo.Stock < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El stock inicial no puede ser negativo."));
            }

            if (insumo.Stock > StockMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El stock inicial supera el máximo admitido."));
            }

            VerificarNombreUnico(insumo.Nombre, 0);

            int id = bllDV.EjecutarControlado(() => dalInsumo.Crear(insumo), Tabla);
            insumo.IdInsumo = id;

            bllEvento.RegistrarDelUsuarioActual("Alta de insumo",
                "Se dio de alta el insumo " + insumo.Nombre + " (id " + id + ") con un stock inicial de " +
                insumo.StockTexto + " y un precio unitario de " + insumo.PrecioTexto + ".");

            return id;
        }

        /// CU-005-025 Modificar insumo. El stock no se toca: solo cambia por
        /// ajuste, venta o compra.
        public void Modificar(BEInsumo insumo)
        {
            Normalizar(insumo);
            ValidarDatos(insumo);

            BEInsumo anterior = dalInsumo.ObtenerPorId(insumo.IdInsumo);
            if (anterior == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!anterior.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El insumo se encuentra dado de baja."));
            }

            VerificarNombreUnico(insumo.Nombre, insumo.IdInsumo);

            // Cambiar la unidad (por ejemplo, de kg a g) no convierte las
            // cantidades: el stock y las cantidades de las recetas quedarían
            // expresados en la unidad equivocada.
            // Si no tenía unidad cargada (datos viejos) se la puede completar.
            if (!string.IsNullOrEmpty(anterior.UnidadDeMedida) &&
                !string.Equals(anterior.UnidadDeMedida, insumo.UnidadDeMedida, StringComparison.OrdinalIgnoreCase))
            {
                bool enRecetas = EstaEnAlgunaReceta(insumo.IdInsumo);
                if (anterior.Stock > 0 || enRecetas)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                        "No se puede cambiar la unidad de medida de " + anterior.Nombre + " (" +
                        anterior.UnidadDeMedida + ") porque " +
                        (anterior.Stock > 0 ? "tiene stock cargado" : "forma parte de recetas") +
                        ": las cantidades quedarían en la unidad equivocada. Si la unidad está mal, " +
                        "deje el stock en cero con un ajuste y quítelo de las recetas antes de cambiarla."));
                }
            }

            bllDV.EjecutarControlado(() => dalInsumo.Modificar(insumo), Tabla);

            // El catálogo XML de recetas muestra el nombre y la unidad de cada insumo.
            if (anterior.Nombre != insumo.Nombre || anterior.UnidadDeMedida != insumo.UnidadDeMedida)
            {
                ActualizarCatalogoRecetas();
            }

            string descripcion = "Se modificaron los datos del insumo " + insumo.Nombre + " (id " + insumo.IdInsumo + ").";
            if (anterior.PrecioUnitario != insumo.PrecioUnitario)
            {
                descripcion += " Precio unitario anterior: " + anterior.PrecioTexto +
                               ". Precio unitario nuevo: " + insumo.PrecioTexto + ".";
            }

            bllEvento.RegistrarDelUsuarioActual("Modificación de insumo", descripcion);
        }

        /// CU-005-026 Baja de insumo. Es lógica, y no se permite si el insumo
        /// todavía forma parte de una receta activa o de una orden de compra pendiente.
        public void Baja(int idInsumo)
        {
            BEInsumo insumo = ObtenerPorId(idInsumo);
            if (insumo == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!insumo.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El insumo ya se encuentra dado de baja."));
            }

            List<string> recetas = dalInsumo.TraerRecetasActivasQueLoUsan(idInsumo);
            if (recetas.Count > 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR008",
                    "El insumo " + insumo.Nombre + " forma parte de las recetas activas: " +
                    string.Join(", ", recetas.ToArray()) + ". Quítelo de esas recetas antes de darlo de baja."));
            }

            int ordenes = dalInsumo.ContarOrdenesPendientesQueLoIncluyen(idInsumo);
            if (ordenes > 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR008",
                    "El insumo " + insumo.Nombre + " figura en " + ordenes +
                    " orden(es) de compra pendiente(s)."));
            }

            bllDV.EjecutarControlado(() => dalInsumo.Baja(idInsumo), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Baja de insumo",
                "Se dio de baja el insumo " + insumo.Nombre + " (id " + idInsumo + ").");
        }

        /// Vuelve a habilitar un insumo dado de baja. Se hace desde Baja de
        /// insumo, marcando "Mostrar también los dados de baja".
        public void Reactivar(int idInsumo)
        {
            BEInsumo insumo = ObtenerPorId(idInsumo);
            if (insumo == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (insumo.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El insumo ya se encuentra activo."));
            }

            bllDV.EjecutarControlado(() => dalInsumo.Activar(idInsumo), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Reactivación de insumo",
                "Se reactivó el insumo " + insumo.Nombre + " (id " + idInsumo + ").");
        }

        /// Si el insumo figura en alguna receta, activa o dada de baja (una receta
        /// dada de baja se puede reactivar y volvería con sus cantidades).
        private static bool EstaEnAlgunaReceta(int idInsumo)
        {
            foreach (BEReceta receta in new DALReceta().TraerRecetasConDetalle())
            {
                foreach (BEDetalleReceta detalle in receta.Detalles)
                {
                    if (detalle.IdInsumo == idInsumo)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// Regenera el catálogo XML de recetas. Se usa la DAL directamente porque
        /// BLLReceta ya depende de BLLInsumo. Si falla, el insumo igual queda
        /// modificado: el catálogo se regenera en el próximo cambio de recetas.
        private static void ActualizarCatalogoRecetas()
        {
            try
            {
                new DALCatalogoRecetas().Exportar(new DALReceta().TraerRecetasConDetalle());
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Ajuste de stock

        /// CU-005-032 Registrar ajuste de stock. Suma o resta la cantidad (R8),
        /// recalcula el dígito verificador y deja el ajuste en la bitácora. Si es
        /// un decremento, avisa al ControladorStock (patrón Observer) por si el
        /// insumo quedó en su mínimo. Devuelve el insumo con el stock actualizado.
        public BEInsumo AjustarStock(int idInsumo, TipoAjusteStock tipo, decimal cantidad, string motivo)
        {
            motivo = motivo == null ? string.Empty : motivo.Trim();
            cantidad = Math.Round(cantidad, 2);

            if (idInsumo <= 0 || motivo.Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            if (cantidad <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad a ajustar tiene que ser mayor que cero."));
            }

            if (motivo.Length > LargoMotivo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El motivo no puede superar los " + LargoMotivo + " caracteres."));
            }

            BEInsumo insumo = dalInsumo.ObtenerPorId(idInsumo);
            if (insumo == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!insumo.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El insumo se encuentra dado de baja."));
            }

            if (tipo == TipoAjusteStock.Decremento && cantidad > insumo.Stock)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR018",
                    "Stock actual de " + insumo.Nombre + ": " + insumo.StockTexto + "."));
            }

            if (tipo == TipoAjusteStock.Incremento && insumo.Stock + cantidad > StockMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El stock resultante supera el máximo admitido."));
            }

            decimal stockAnterior = insumo.Stock;
            decimal movimiento = tipo == TipoAjusteStock.Decremento ? -cantidad : cantidad;

            // El procedimiento vuelve a controlar que el stock no quede negativo,
            // por si otro usuario descontó entre la lectura y la grabación.
            if (!bllDV.EjecutarControlado(() => dalInsumo.AjustarStock(idInsumo, movimiento), Tabla))
            {
                // Algo cambió entre la lectura y la grabación: se vuelve a leer
                // para informar el motivo real.
                BEInsumo actual = dalInsumo.ObtenerPorId(idInsumo);
                if (actual == null || !actual.Activo)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                        "El insumo se dio de baja mientras se registraba el ajuste."));
                }
                if (tipo == TipoAjusteStock.Decremento && cantidad > actual.Stock)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR018",
                        "Stock actual de " + actual.Nombre + ": " + actual.StockTexto + "."));
                }
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR008",
                    "No se pudo actualizar el stock del insumo. Intente nuevamente."));
            }

            BEInsumo actualizado = dalInsumo.ObtenerPorId(idInsumo);

            bllEvento.RegistrarDelUsuarioActual(AccionAjuste,
                ArmarDescripcionAjuste(actualizado, tipo, cantidad, stockAnterior, motivo));

            if (tipo == TipoAjusteStock.Decremento)
            {
                controladorStock.RegistrarDescuento(actualizado, stockAnterior);
            }

            return actualizado;
        }

        /// Últimos ajustes registrados, reconstruidos desde la bitácora.
        public List<BEAjusteStock> TraerUltimosAjustes(int cantidad)
        {
            List<BEAjusteStock> ajustes = new List<BEAjusteStock>();

            foreach (BEEvento evento in bllEvento.TraerUltimosPorAccion(AccionAjuste, cantidad))
            {
                ajustes.Add(LeerDescripcionAjuste(evento));
            }

            return ajustes;
        }

        // Formato del evento:
        //   Insumo: Harina 000 (id 3) | Tipo: Decremento | Cantidad: 2,5 kg |
        //   Stock anterior: 10 kg | Stock nuevo: 7,5 kg | Pérdida: $ 3.000,00 | Motivo: ...
        // La pérdida solo figura en los decrementos de insumos con precio. El
        // motivo va último porque es texto libre. La columna descripción de la
        // tabla Evento es VARCHAR: no se usan caracteres fuera del juego latino.
        private static string ArmarDescripcionAjuste(BEInsumo insumo, TipoAjusteStock tipo, decimal cantidad,
            decimal stockAnterior, string motivo)
        {
            string descripcion = "Insumo: " + SinSeparador(insumo.Nombre) + " (id " + insumo.IdInsumo + ")" +
                   " | Tipo: " + tipo +
                   " | Cantidad: " + cantidad.ToString("0.##") + " " + SinSeparador(insumo.UnidadDeMedida) +
                   " | Stock anterior: " + stockAnterior.ToString("0.##") + " " + SinSeparador(insumo.UnidadDeMedida) +
                   " | Stock nuevo: " + insumo.StockTexto;

            if (tipo == TipoAjusteStock.Decremento && insumo.PrecioUnitario > 0)
            {
                descripcion += " | Pérdida: " + (cantidad * insumo.PrecioUnitario).ToString("C");
            }

            return descripcion + " | Motivo: " + motivo;
        }

        private static BEAjusteStock LeerDescripcionAjuste(BEEvento evento)
        {
            BEAjusteStock ajuste = new BEAjusteStock();
            ajuste.FechaHora = evento.FechaHora;
            ajuste.Usuario = evento.Usuario;

            string descripcion = evento.Descripcion ?? string.Empty;
            int inicioMotivo = descripcion.IndexOf(" | Motivo: ", StringComparison.Ordinal);
            string datos = inicioMotivo >= 0 ? descripcion.Substring(0, inicioMotivo) : descripcion;
            ajuste.Motivo = inicioMotivo >= 0 ? descripcion.Substring(inicioMotivo + " | Motivo: ".Length) : string.Empty;

            foreach (string parte in datos.Split(new string[] { " | " }, StringSplitOptions.None))
            {
                int separador = parte.IndexOf(": ", StringComparison.Ordinal);
                if (separador <= 0)
                {
                    continue;
                }

                string clave = parte.Substring(0, separador);
                string valor = parte.Substring(separador + 2);

                switch (clave)
                {
                    case "Insumo":
                        int inicioId = valor.LastIndexOf(" (id ", StringComparison.Ordinal);
                        ajuste.Insumo = inicioId > 0 ? valor.Substring(0, inicioId) : valor;
                        break;
                    case "Tipo":
                        ajuste.Tipo = valor;
                        break;
                    case "Cantidad":
                        ajuste.Cantidad = valor;
                        break;
                }
            }

            // Un evento que no respeta el formato se muestra igual, con su texto.
            if (string.IsNullOrEmpty(ajuste.Insumo))
            {
                ajuste.Insumo = descripcion;
            }

            return ajuste;
        }

        private static string SinSeparador(string texto)
        {
            return texto == null ? string.Empty : texto.Replace("|", "/");
        }

        #endregion

        #region Validaciones internas

        private static void Normalizar(BEInsumo insumo)
        {
            if (insumo == null)
            {
                return;
            }

            insumo.Nombre = Recortar(insumo.Nombre);
            insumo.UnidadDeMedida = Recortar(insumo.UnidadDeMedida);
            insumo.Descripcion = Recortar(insumo.Descripcion);
            if (insumo.Descripcion.Length == 0)
            {
                insumo.Descripcion = null;
            }

            // Las columnas guardan dos decimales.
            insumo.Stock = Math.Round(insumo.Stock, 2);
            insumo.StockMinimo = Math.Round(insumo.StockMinimo, 2);
            insumo.PrecioUnitario = Math.Round(insumo.PrecioUnitario, 2);
        }

        private static string Recortar(string valor)
        {
            return valor == null ? string.Empty : valor.Trim();
        }

        private static void ValidarDatos(BEInsumo insumo)
        {
            if (insumo == null ||
                string.IsNullOrEmpty(insumo.Nombre) ||
                string.IsNullOrEmpty(insumo.UnidadDeMedida))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            ValidarLargo(insumo.Nombre, LargoNombre, "El nombre");
            ValidarLargo(insumo.UnidadDeMedida, LargoUnidad, "La unidad de medida");
            ValidarLargo(insumo.Descripcion, LargoDescripcion, "La descripción");

            if (insumo.StockMinimo < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El stock mínimo no puede ser negativo."));
            }

            if (insumo.StockMinimo > StockMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El stock mínimo supera el máximo admitido."));
            }

            if (insumo.PrecioUnitario < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El precio unitario no puede ser negativo."));
            }

            if (insumo.PrecioUnitario > StockMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El precio unitario supera el máximo admitido."));
            }

            if (insumo.DiasDeVidaUtil.HasValue &&
                (insumo.DiasDeVidaUtil.Value <= 0 || insumo.DiasDeVidaUtil.Value > VidaUtilMaxima))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Los días de vida útil tienen que ser un número entre 1 y " + VidaUtilMaxima + "."));
            }
        }

        private static void ValidarLargo(string valor, int largoMaximo, string campo)
        {
            if (valor != null && valor.Length > largoMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    campo + " no puede superar los " + largoMaximo + " caracteres."));
            }
        }

        /// Dos insumos no pueden llamarse igual (sin distinguir mayúsculas).
        private void VerificarNombreUnico(string nombre, int idExcluido)
        {
            foreach (BEInsumo otro in dalInsumo.TraerInsumos())
            {
                if (otro.IdInsumo != idExcluido &&
                    string.Equals(otro.Nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase))
                {
                    string detalle = "Ya existe un insumo llamado " + otro.Nombre;
                    detalle += otro.Activo ? "." : ", dado de baja. Puede reactivarlo desde Baja de insumo.";
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR010", detalle));
                }
            }
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return !string.IsNullOrEmpty(valor) &&
                   valor.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
