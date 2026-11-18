using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// CU-006-037 Descontar stock por venta.
    ///
    /// Cada venta descuenta del stock lo que consume la receta (R8) y queda en
    /// las tablas Venta y ConsumoVenta, que son el historial de consumo con el
    /// que se entrena la predicción de demanda (CU-006-033) y el que usan el
    /// reporte gerencial y el servicio web de ventas. Además queda auditada en
    /// la bitácora (quién la cargó y cuándo).
    ///
    /// IntelligentStock recibe las ventas de tres formas, que pasan todas por
    /// las mismas reglas:
    ///   - un pedido: una o varias recetas vendidas juntas (por ejemplo, lo que
    ///     pidió una mesa),
    ///   - el cierre del día: todas las recetas vendidas en un día, de una vez,
    ///   - el servicio web WebServiceVentas, que el punto de venta del local
    ///     puede llamar por cada venta (R9: solo desde orígenes autorizados).
    ///
    /// La venta ya ocurrió: si el stock del sistema no alcanza, no se rechaza.
    /// El insumo queda en cero y se avisa la diferencia, que se corrige con un
    /// ajuste de stock (CU-005-032).
    public class BLLVenta
    {
        /// Acción con la que cada registro de ventas queda en la bitácora.
        public const string AccionVenta = "Registro de ventas";

        /// Por dónde llegó la venta. No se guarda en la tabla Venta: queda
        /// escrito en el evento de la bitácora, junto con quién la cargó.
        private const string CanalPedido = "carga manual";
        private const string CanalCierre = "cierre del día";
        private const string CanalServicioWeb = "punto de venta, servicio web";

        /// Unidades que se pueden registrar de una receta en una sola venta.
        public const decimal CantidadMaxima = 10000m;

        /// Recetas distintas que puede tener un pedido.
        public const int RecetasMaximasPorPedido = 50;

        /// Hasta cuántos días hacia atrás se puede registrar una venta.
        public const int DiasMaximosHaciaAtras = 365;

        private const int LargoMaximoDescripcion = 500;

        private readonly DALVenta dalVenta = new DALVenta();
        private readonly DALInsumo dalInsumo = new DALInsumo();
        private readonly BLLReceta bllReceta = new BLLReceta();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();
        private readonly ControladorStock controladorStock = new ControladorStock();

        public BLLVenta()
        {
            // Patrón Observer: igual que en el ajuste de stock, si una venta deja
            // a un insumo en su mínimo se avisa por correo y queda en la bitácora.
            controladorStock.Suscribir(new NotificadorEmailStock());
            controladorStock.Suscribir(new BitacoraAlertaStock());
        }

        /// Recetas que se pueden vender: activas y con insumos cargados.
        public List<BEReceta> TraerRecetasVendibles()
        {
            List<BEReceta> recetas = new List<BEReceta>();
            foreach (BEReceta receta in bllReceta.TraerActivas())
            {
                if (receta.CantidadInsumos > 0)
                {
                    recetas.Add(receta);
                }
            }
            return recetas;
        }

        #region Pedido

        /// Agrega una receta al pedido que se está armando y devuelve el pedido.
        /// Si la receta ya estaba, suma la cantidad. No graba nada: el pedido se
        /// registra completo con RegistrarPedido.
        public List<BEVenta> AgregarAlPedido(List<BEVenta> pedido, int idReceta, decimal cantidad)
        {
            if (pedido == null)
            {
                pedido = new List<BEVenta>();
            }

            BEReceta receta = ObtenerRecetaVendible(idReceta);
            ValidarCantidad(cantidad);

            BEVenta existente = pedido.Find(delegate(BEVenta v) { return v.IdReceta == receta.IdReceta; });
            if (existente != null)
            {
                ValidarCantidad(existente.Cantidad + cantidad);
                existente.Cantidad += cantidad;
                return pedido;
            }

            if (pedido.Count >= RecetasMaximasPorPedido)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Un pedido puede tener hasta " + RecetasMaximasPorPedido + " recetas distintas."));
            }

            BEVenta venta = new BEVenta();
            venta.IdReceta = receta.IdReceta;
            venta.NombreReceta = receta.Nombre;
            venta.Cantidad = cantidad;
            pedido.Add(venta);
            return pedido;
        }

        #endregion

        #region Vista previa

        /// Lo que se va a descontar de cada insumo por una o varias ventas juntas
        /// (un pedido o el cierre del día), sumado por insumo, sin grabar nada,
        /// para mostrarlo antes de confirmar.
        public List<BEConsumoVenta> Previsualizar(List<BEVenta> ventas)
        {
            Dictionary<int, BEConsumoVenta> porInsumo = new Dictionary<int, BEConsumoVenta>();
            List<BEConsumoVenta> consumos = new List<BEConsumoVenta>();
            Dictionary<int, BEReceta> recetas = new Dictionary<int, BEReceta>();

            foreach (BEVenta venta in ventas)
            {
                BEReceta receta;
                if (!recetas.TryGetValue(venta.IdReceta, out receta))
                {
                    receta = bllReceta.ObtenerPorId(venta.IdReceta);
                    recetas[venta.IdReceta] = receta;
                }
                if (receta == null)
                {
                    continue;
                }

                foreach (BEDetalleReceta detalle in receta.Detalles)
                {
                    BEConsumoVenta consumo;
                    if (!porInsumo.TryGetValue(detalle.IdInsumo, out consumo))
                    {
                        BEInsumo insumo = dalInsumo.ObtenerPorId(detalle.IdInsumo);
                        consumo = new BEConsumoVenta();
                        consumo.IdInsumo = detalle.IdInsumo;
                        consumo.NombreInsumo = detalle.NombreInsumo;
                        consumo.UnidadDeMedida = detalle.UnidadDeMedida;
                        consumo.StockAnterior = insumo == null ? 0m : insumo.Stock;
                        consumo.StockMinimo = insumo == null ? 0m : insumo.StockMinimo;
                        porInsumo[detalle.IdInsumo] = consumo;
                        consumos.Add(consumo);
                    }
                    // Igual que ROUND de SQL Server, que es el que descuenta de verdad.
                    consumo.ConsumoTotal += Math.Round(detalle.Cantidad * venta.Cantidad, 2, MidpointRounding.AwayFromZero);
                }
            }

            foreach (BEConsumoVenta consumo in consumos)
            {
                consumo.StockResultante = Math.Max(0m, consumo.StockAnterior - consumo.ConsumoTotal);
            }
            consumos.Sort(delegate(BEConsumoVenta a, BEConsumoVenta b)
            {
                return string.Compare(a.NombreInsumo, b.NombreInsumo, StringComparison.CurrentCultureIgnoreCase);
            });
            return consumos;
        }

        /// Ventas ya registradas con esa fecha, para avisar antes de cargar un
        /// cierre del día que podría repetirlas.
        public List<BEVenta> TraerVentasDelDia(DateTime fecha)
        {
            return dalVenta.TraerVentas(fecha.Date, fecha.Date);
        }

        #endregion

        #region Registro

        /// Un pedido: una o varias recetas vendidas juntas, en una sola
        /// operación. Cada receta queda como una venta (una fila de Venta).
        public List<BEConsumoVenta> RegistrarPedido(List<BEVenta> pedido, DateTime? fecha)
        {
            if (pedido == null || pedido.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Agregue al pedido al menos una receta."));
            }
            if (pedido.Count > RecetasMaximasPorPedido)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Un pedido puede tener hasta " + RecetasMaximasPorPedido + " recetas distintas."));
            }

            DateTime dia = ValidarFecha(fecha);

            // La misma receta dos veces se registra una sola vez, sumada.
            List<BEVenta> ventas = new List<BEVenta>();
            foreach (BEVenta linea in pedido)
            {
                BEVenta existente = ventas.Find(delegate(BEVenta v) { return v.IdReceta == linea.IdReceta; });
                if (existente != null)
                {
                    existente.Cantidad += linea.Cantidad;
                    continue;
                }

                BEVenta venta = new BEVenta();
                venta.IdReceta = linea.IdReceta;
                venta.Cantidad = linea.Cantidad;
                venta.Fecha = dia;
                ventas.Add(venta);
            }

            return RegistrarVentas(ventas, CanalPedido, true, null);
        }

        /// Cierre del día: todas las recetas vendidas en un día, en una sola
        /// operación. Las líneas sin cantidad no se registran.
        public List<BEConsumoVenta> RegistrarCierreDelDia(List<BEVenta> ventas, DateTime? fecha)
        {
            DateTime dia = ValidarFecha(fecha);
            List<BEVenta> conCantidad = new List<BEVenta>();
            foreach (BEVenta venta in ventas)
            {
                if (venta.Cantidad != 0)
                {
                    venta.Fecha = dia;
                    conCantidad.Add(venta);
                }
            }
            if (conCantidad.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Ingrese la cantidad vendida de al menos una receta."));
            }

            return RegistrarVentas(conCantidad, CanalCierre, true,
                "Cierre del día del " + dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }

        /// Una venta que informa el punto de venta por el servicio web. La receta
        /// se indica por su número o por su nombre. No hay usuario con sesión: el
        /// registro queda en la bitácora a nombre del sistema.
        public List<BEConsumoVenta> RegistrarDesdePuntoDeVenta(string receta, decimal cantidad, DateTime fecha)
        {
            BEReceta encontrada = BuscarRecetaVendible(receta, IndiceDeRecetas());
            if (encontrada == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013",
                    "No hay una receta activa con insumos que se llame \"" + receta + "\"."));
            }

            BEVenta venta = new BEVenta();
            venta.IdReceta = encontrada.IdReceta;
            venta.Cantidad = cantidad;
            venta.Fecha = ValidarFecha(fecha);
            return RegistrarVentas(new List<BEVenta> { venta }, CanalServicioWeb, false, null);
        }

        /// Valida las ventas, las graba y descuenta el stock en una sola
        /// transacción (con el control de dígitos verificadores), deja el
        /// registro en la bitácora y avisa por el Observer los insumos que
        /// quedaron en su mínimo. Devuelve lo descontado de cada insumo.
        private List<BEConsumoVenta> RegistrarVentas(List<BEVenta> ventas, string canal, bool conUsuario, string titulo)
        {
            Dictionary<int, BEReceta> recetas = new Dictionary<int, BEReceta>();
            foreach (BEVenta venta in ventas)
            {
                if (!recetas.ContainsKey(venta.IdReceta))
                {
                    recetas[venta.IdReceta] = ObtenerRecetaVendible(venta.IdReceta);
                }
                venta.NombreReceta = recetas[venta.IdReceta].Nombre;
                ValidarCantidad(venta.Cantidad);
            }

            List<BEConsumoVenta> consumos = null;
            bool registrado = bllDV.EjecutarControlado(
                () => dalVenta.Registrar(ventas, out consumos),
                ok => ok,
                "Insumo", "Venta", "ConsumoVenta");

            if (!registrado)
            {
                List<string> inactivos = new List<string>();
                foreach (BEConsumoVenta consumo in consumos)
                {
                    inactivos.Add(consumo.NombreInsumo);
                }
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "Hay recetas que llevan insumos dados de baja (" + string.Join(", ", inactivos.ToArray()) +
                    "). Revíselas desde Modificación de receta. No se registró nada."));
            }

            RegistrarEnBitacora(ventas, canal, titulo, consumos, conUsuario);

            // Patrón Observer: avisa si algún insumo cruzó su stock mínimo.
            foreach (BEConsumoVenta consumo in consumos)
            {
                BEInsumo actualizado = dalInsumo.ObtenerPorId(consumo.IdInsumo);
                if (actualizado != null)
                {
                    consumo.StockMinimo = actualizado.StockMinimo;
                }
                controladorStock.RegistrarDescuento(actualizado, consumo.StockAnterior);
            }

            return consumos;
        }

        /// Por ejemplo: "Venta de 3 recetas (10 unidades) del 17/11/2026: 4 x
        /// Empanada de carne, 4 x Empanada de pollo, 2 x Flan (carga manual).
        /// Se descontó el stock de 6 insumos."
        private void RegistrarEnBitacora(List<BEVenta> ventas, string canal, string titulo,
                                         List<BEConsumoVenta> consumos, bool conUsuario)
        {
            decimal unidades = 0;
            List<string> recetas = new List<string>();
            foreach (BEVenta venta in ventas)
            {
                unidades += venta.Cantidad;
                recetas.Add(venta.Cantidad.ToString("0.##") + " x " + venta.NombreReceta);
            }

            StringBuilder texto = new StringBuilder();
            if (titulo != null)
            {
                texto.Append(titulo).Append(": ").Append(ventas.Count).Append(ventas.Count == 1 ? " receta" : " recetas")
                     .Append(" (").Append(unidades.ToString("0.##")).Append(" unidades)");
            }
            else if (ventas.Count == 1)
            {
                texto.Append("Venta de ").Append(recetas[0]).Append(" del ").Append(ventas[0].Fecha.ToString("dd/MM/yyyy"));
            }
            else
            {
                texto.Append("Venta de ").Append(ventas.Count).Append(" recetas (").Append(unidades.ToString("0.##"))
                     .Append(" unidades) del ").Append(ventas[0].Fecha.ToString("dd/MM/yyyy")).Append(": ")
                     .Append(string.Join(", ", recetas.ToArray()));
            }
            StringBuilder cola = new StringBuilder();
            cola.Append(" (").Append(canal).Append("). Se descontó el stock de ")
                .Append(consumos.Count).Append(consumos.Count == 1 ? " insumo." : " insumos.");

            List<string> faltantes = new List<string>();
            foreach (BEConsumoVenta consumo in consumos)
            {
                if (consumo.SinStock)
                {
                    faltantes.Add(consumo.NombreInsumo + " (" + consumo.FaltanteTexto + ")");
                }
            }
            if (faltantes.Count > 0)
            {
                cola.Append(" El stock del sistema no alcanzaba y quedó en cero: ")
                    .Append(string.Join("; ", faltantes.ToArray()))
                    .Append(". Corregirlo con un ajuste de stock.");
            }

            // Si no entra todo, se recorta la lista de recetas y no los faltantes.
            string final = Recortar(cola.ToString(), LargoMaximoDescripcion / 2);
            string descripcion = Recortar(texto.ToString(), LargoMaximoDescripcion - final.Length) + final;
            CriticidadEvento criticidad = faltantes.Count > 0 ? CriticidadEvento.Advertencia : CriticidadEvento.Informativa;
            if (conUsuario)
            {
                bllEvento.RegistrarDelUsuarioActual(AccionVenta, descripcion, criticidad);
            }
            else
            {
                bllEvento.Registrar(null, AccionVenta, descripcion, criticidad);
            }
        }

        #endregion

        #region Consultas

        /// Ventas con fecha de venta entre desde y hasta (inclusive).
        public List<BEVenta> TraerVentas(DateTime desde, DateTime hasta)
        {
            return dalVenta.TraerVentas(desde.Date, hasta.Date);
        }

        /// Ventas registradas entre desde y hasta (inclusive), de una receta o
        /// de todas (idReceta 0), de la más nueva a la más vieja, con lo que
        /// descontó cada una. Es la consulta de la pestaña "Ventas registradas".
        public List<BEVenta> ConsultarVentas(DateTime desde, DateTime hasta, int idReceta)
        {
            desde = desde.Date;
            hasta = hasta.Date;
            if (desde > hasta)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha desde no puede ser posterior a la fecha hasta."));
            }
            if ((hasta - desde).TotalDays >= DiasMaximosEstadisticas)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El período no puede superar un año."));
            }

            List<BEVenta> ventas = dalVenta.TraerVentas(desde, hasta);
            if (idReceta > 0)
            {
                ventas = ventas.FindAll(delegate(BEVenta v) { return v.IdReceta == idReceta; });
            }
            ventas.Sort(delegate(BEVenta a, BEVenta b)
            {
                int c = b.Fecha.CompareTo(a.Fecha);
                return c != 0 ? c : b.IdVenta.CompareTo(a.IdVenta);
            });
            return ventas;
        }

        /// Período máximo de las estadísticas de ventas.
        public const int DiasMaximosEstadisticas = 366;

        /// Estadísticas de ventas entre desde y hasta (inclusive): unidades y
        /// operaciones, promedio diario, receta más vendida, día de más ventas
        /// y el detalle por receta. Las publica WebServiceVentas.
        public BEEstadisticaVentas Estadisticas(DateTime desde, DateTime hasta)
        {
            desde = desde.Date;
            hasta = hasta.Date;

            if (desde > hasta)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de inicio no puede ser posterior a la fecha de fin."));
            }
            if ((hasta - desde).TotalDays >= DiasMaximosEstadisticas)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El período no puede superar un año."));
            }

            BEEstadisticaVentas estadistica = new BEEstadisticaVentas();
            estadistica.Desde = desde;
            estadistica.Hasta = hasta;
            estadistica.Dias = (int)(hasta - desde).TotalDays + 1;

            Dictionary<int, BEVentaPorReceta> porReceta = new Dictionary<int, BEVentaPorReceta>();
            Dictionary<DateTime, decimal> porDia = new Dictionary<DateTime, decimal>();

            foreach (BEVenta venta in TraerVentas(desde, hasta))
            {
                BEVentaPorReceta fila;
                if (!porReceta.TryGetValue(venta.IdReceta, out fila))
                {
                    fila = new BEVentaPorReceta();
                    fila.IdReceta = venta.IdReceta;
                    porReceta[venta.IdReceta] = fila;
                }
                fila.Receta = venta.NombreReceta;
                fila.UnidadesVendidas += venta.Cantidad;
                fila.OperacionesDeVenta++;

                decimal delDia;
                porDia.TryGetValue(venta.Fecha.Date, out delDia);
                porDia[venta.Fecha.Date] = delDia + venta.Cantidad;

                estadistica.UnidadesVendidas += venta.Cantidad;
                estadistica.OperacionesDeVenta++;
            }

            estadistica.PromedioDiario = Math.Round(estadistica.UnidadesVendidas / estadistica.Dias, 2);

            foreach (BEVentaPorReceta fila in porReceta.Values)
            {
                fila.PromedioDiario = Math.Round(fila.UnidadesVendidas / estadistica.Dias, 2);
                fila.PorcentajeDelTotal = estadistica.UnidadesVendidas == 0
                    ? 0
                    : Math.Round(fila.UnidadesVendidas * 100 / estadistica.UnidadesVendidas, 1);
                estadistica.Recetas.Add(fila);
            }
            estadistica.Recetas.Sort(delegate(BEVentaPorReceta a, BEVentaPorReceta b)
            {
                return b.UnidadesVendidas.CompareTo(a.UnidadesVendidas);
            });
            estadistica.RecetaMasVendida = estadistica.Recetas.Count > 0 ? estadistica.Recetas[0].Receta : string.Empty;

            foreach (KeyValuePair<DateTime, decimal> dia in porDia)
            {
                if (!estadistica.DiaConMasVentas.HasValue || dia.Value > estadistica.UnidadesDelDiaConMasVentas)
                {
                    estadistica.DiaConMasVentas = dia.Key;
                    estadistica.UnidadesDelDiaConMasVentas = dia.Value;
                }
            }

            return estadistica;
        }

        #endregion

        #region Privados

        private BEReceta ObtenerRecetaVendible(int idReceta)
        {
            if (idReceta <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Elija la receta vendida."));
            }

            BEReceta receta = bllReceta.ObtenerPorId(idReceta);
            if (receta == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!receta.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La receta " + receta.Nombre + " está dada de baja."));
            }

            if (receta.Detalles.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR017",
                    "La receta " + receta.Nombre + " no tiene insumos cargados."));
            }

            return receta;
        }

        private static DateTime ValidarFecha(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Ingrese la fecha de la venta."));
            }

            DateTime fechaVenta = fecha.Value.Date;
            if (fechaVenta > DateTime.Today)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de la venta no puede ser posterior a hoy."));
            }
            if (fechaVenta < DateTime.Today.AddDays(-DiasMaximosHaciaAtras))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de la venta no puede tener más de un año."));
            }
            return fechaVenta;
        }

        private static void ValidarCantidad(decimal cantidad)
        {
            if (cantidad <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad vendida tiene que ser mayor que cero."));
            }

            if (cantidad > CantidadMaxima)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad vendida no puede superar " + CantidadMaxima.ToString("0") + " unidades."));
            }

            if (decimal.Round(cantidad, 2) != cantidad)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad vendida admite hasta dos decimales."));
            }
        }

        /// Recetas que se pueden vender, por nombre normalizado y por número.
        private Dictionary<string, BEReceta> IndiceDeRecetas()
        {
            Dictionary<string, BEReceta> indice = new Dictionary<string, BEReceta>();
            foreach (BEReceta receta in TraerRecetasVendibles())
            {
                indice["#" + receta.IdReceta.ToString(CultureInfo.InvariantCulture)] = receta;
                string nombre = Normalizar(receta.Nombre);
                if (!indice.ContainsKey(nombre))
                {
                    indice[nombre] = receta;
                }
            }
            return indice;
        }

        private static BEReceta BuscarRecetaVendible(string texto, Dictionary<string, BEReceta> indice)
        {
            texto = (texto ?? string.Empty).Trim();
            BEReceta receta;
            int numero;
            string sinNumeral = texto.TrimStart('#').Replace("N°", string.Empty).Trim();
            if (int.TryParse(sinNumeral, NumberStyles.None, CultureInfo.InvariantCulture, out numero) &&
                indice.TryGetValue("#" + numero.ToString(CultureInfo.InvariantCulture), out receta))
            {
                return receta;
            }
            return indice.TryGetValue(Normalizar(texto), out receta) ? receta : null;
        }

        /// Minúsculas, sin acentos y sin espacios de más, para comparar nombres.
        private static string Normalizar(string texto)
        {
            string descompuesto = (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder limpio = new StringBuilder();
            bool espacio = false;
            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    espacio = limpio.Length > 0;
                    continue;
                }
                if (espacio)
                {
                    limpio.Append(' ');
                    espacio = false;
                }
                limpio.Append(c);
            }
            return limpio.ToString();
        }

        private static string Recortar(string texto, int largo)
        {
            return texto.Length <= largo ? texto : texto.Substring(0, largo - 3) + "...";
        }

        #endregion
    }
}
