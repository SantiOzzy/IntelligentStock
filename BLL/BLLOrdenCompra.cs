using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Órdenes de compra: generación predictiva (CU-006-033), consulta
    /// (CU-006-034), anulación (CU-006-035) y recepción con la factura del
    /// proveedor (CU-006-036).
    public class BLLOrdenCompra
    {
        private const string TablaOrden = "OrdenCompra";
        private const string TablaDetalle = "DetalleOrdenCompra";
        private const string TablaInsumo = "Insumo";

        /// Días que se proyectan por defecto (Web.config, clave "DiasProyeccionCompra").
        public const int DiasProyeccionPorDefecto = 7;

        /// La columna estado es VARCHAR(50) y guarda "Anulada: motivo" o
        /// "Recibida: comprobante".
        public const int LargoEstado = 50;
        public const int LargoComprobante = 40;
        public const int LargoMotivo = 200;

        /// Lo máximo que admite la columna cantidad, DECIMAL(8,2).
        public const decimal CantidadMaxima = 999999.99m;

        private static readonly Regex FormatoComprobante = new Regex(@"^[A-Za-z0-9][A-Za-z0-9 \-/]*$", RegexOptions.Compiled);

        private readonly DALOrdenCompra dalOrden = new DALOrdenCompra();
        private readonly BLLProveedor bllProveedor = new BLLProveedor();
        private readonly BLLInsumo bllInsumo = new BLLInsumo();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        /// Días a proyectar en la generación predictiva.
        public static int DiasProyeccion
        {
            get
            {
                int dias;
                if (int.TryParse(ConfigurationManager.AppSettings["DiasProyeccionCompra"], out dias) && dias >= 1 && dias <= 90)
                {
                    return dias;
                }
                return DiasProyeccionPorDefecto;
            }
        }

        #region Consultas

        public List<BEOrdenCompra> TraerOrdenes()
        {
            return dalOrden.TraerOrdenes();
        }

        public List<BEOrdenCompra> TraerPendientes()
        {
            return TraerOrdenes().FindAll(delegate(BEOrdenCompra o) { return o.Pendiente; });
        }

        public BEOrdenCompra ObtenerPorId(int idOrden)
        {
            return idOrden <= 0 ? null : dalOrden.ObtenerPorId(idOrden);
        }

        /// CU-006-034 Consultar orden de compra. Los filtros vacíos no filtran.
        public List<BEOrdenCompra> Filtrar(string cuit, string estado, DateTime? desde, DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha desde no puede ser posterior a la fecha hasta."));
            }

            List<BEOrdenCompra> resultado = new List<BEOrdenCompra>();
            foreach (BEOrdenCompra orden in TraerOrdenes())
            {
                if (!string.IsNullOrEmpty(cuit) && orden.CuitProveedor != cuit)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(estado) && orden.EstadoBase != estado)
                {
                    continue;
                }
                if (desde.HasValue && orden.Fecha.Date < desde.Value.Date)
                {
                    continue;
                }
                if (hasta.HasValue && orden.Fecha.Date > hasta.Value.Date)
                {
                    continue;
                }
                resultado.Add(orden);
            }
            return resultado;
        }

        /// Compras por proveedor en un período (WebServiceGastoProveedores y
        /// reporte gerencial). Lo recibido se valoriza al precio unitario
        /// actual de cada insumo.
        public List<BEGastoProveedor> GastoPorProveedor(DateTime desde, DateTime hasta)
        {
            Dictionary<int, decimal> importePorOrden = new Dictionary<int, decimal>();
            foreach (BEDetalleOrdenCompra detalle in dalOrden.TraerDetalle(null))
            {
                decimal previo;
                importePorOrden.TryGetValue(detalle.IdOrdenCompra, out previo);
                importePorOrden[detalle.IdOrdenCompra] = previo + detalle.Importe;
            }

            Dictionary<string, BEGastoProveedor> porProveedor = new Dictionary<string, BEGastoProveedor>();
            List<BEGastoProveedor> lista = new List<BEGastoProveedor>();

            foreach (BEOrdenCompra orden in Filtrar(null, null, desde, hasta))
            {
                BEGastoProveedor gasto;
                if (!porProveedor.TryGetValue(orden.CuitProveedor, out gasto))
                {
                    gasto = new BEGastoProveedor();
                    gasto.Cuit = orden.CuitProveedor;
                    gasto.Proveedor = string.IsNullOrEmpty(orden.NombreProveedor) ? orden.CuitProveedor : orden.NombreProveedor;
                    porProveedor[orden.CuitProveedor] = gasto;
                    lista.Add(gasto);
                }

                switch (orden.EstadoBase)
                {
                    case BEOrdenCompra.EstadoPendiente:
                        gasto.OrdenesPendientes++;
                        break;
                    case BEOrdenCompra.EstadoRecibida:
                        gasto.OrdenesRecibidas++;
                        decimal importe;
                        if (importePorOrden.TryGetValue(orden.IdOrdenCompra, out importe))
                        {
                            gasto.ImporteRecibido += importe;
                        }
                        break;
                    default:
                        gasto.OrdenesAnuladas++;
                        break;
                }
            }

            lista.Sort(delegate(BEGastoProveedor a, BEGastoProveedor b) { return b.ImporteRecibido.CompareTo(a.ImporteRecibido); });
            return lista;
        }

        #endregion

        #region CU-006-033 Generar orden de compra predictiva

        /// Cantidades a pedir de cada insumo según la predicción de demanda de
        /// los próximos 'dias' días. Lanza #ERR019 / #ERR005 si la predicción no
        /// se puede hacer y #ERR020 si no hace falta pedir nada.
        public List<BESugerenciaCompra> CalcularSugerencias(int dias, out ResultadoPrediccion prediccion)
        {
            prediccion = new BLLPrediccionDemanda().Predecir(dias);
            Dictionary<int, decimal> pendientes = dalOrden.TraerCantidadesPendientes();

            List<BESugerenciaCompra> sugerencias = new List<BESugerenciaCompra>();
            foreach (BEInsumo insumo in bllInsumo.TraerActivos())
            {
                decimal pendiente;
                pendientes.TryGetValue(insumo.IdInsumo, out pendiente);

                decimal consumo = prediccion.Consumo(insumo.IdInsumo);

                // Lo que se va a consumir, más el mínimo de seguridad, menos lo que
                // hay y lo que ya está pedido.
                decimal faltante = consumo + insumo.StockMinimo - insumo.Stock - pendiente;
                if (faltante <= 0)
                {
                    continue;
                }

                BESugerenciaCompra sugerencia = new BESugerenciaCompra();
                sugerencia.IdInsumo = insumo.IdInsumo;
                sugerencia.NombreInsumo = insumo.Nombre;
                sugerencia.UnidadDeMedida = insumo.UnidadDeMedida;
                sugerencia.StockActual = insumo.Stock;
                sugerencia.StockMinimo = insumo.StockMinimo;
                sugerencia.CantidadPendiente = pendiente;
                sugerencia.ConsumoPrevisto = consumo;
                sugerencia.CantidadSugerida = Math.Min(CantidadMaxima, Math.Ceiling(faltante * 100m) / 100m);
                sugerencia.PrecioUnitario = insumo.PrecioUnitario;
                sugerencias.Add(sugerencia);
            }

            if (sugerencias.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR020"));
            }

            sugerencias.Sort(delegate(BESugerenciaCompra a, BESugerenciaCompra b)
            {
                return string.Compare(a.NombreInsumo, b.NombreInsumo, StringComparison.CurrentCultureIgnoreCase);
            });
            return sugerencias;
        }

        /// Genera la orden con las cantidades que sugiere la predicción y la deja
        /// Pendiente. Devuelve la orden grabada con su detalle.
        public BEOrdenCompra GenerarPredictiva(string cuit, DateTime? fecha, out List<BESugerenciaCompra> sugerencias,
                                               out ResultadoPrediccion prediccion)
        {
            if (string.IsNullOrEmpty(cuit))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002", "Elija el proveedor."));
            }

            BEProveedor proveedor = bllProveedor.ObtenerPorCuit(cuit);
            if (proveedor == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }
            if (!proveedor.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El proveedor " + proveedor.Nombre + " está dado de baja."));
            }

            DateTime fechaOrden = fecha.HasValue ? fecha.Value.Date : DateTime.Today;
            if (fechaOrden < DateTime.Today)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de la orden no puede ser anterior a hoy."));
            }
            if (fechaOrden > DateTime.Today.AddDays(60))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de la orden no puede estar a más de 60 días."));
            }

            int dias = DiasProyeccion;
            sugerencias = CalcularSugerencias(dias, out prediccion);

            BEOrdenCompra orden = new BEOrdenCompra();
            orden.CuitProveedor = proveedor.Cuit;
            orden.NombreProveedor = proveedor.Nombre;
            orden.Fecha = fechaOrden;
            orden.Estado = BEOrdenCompra.EstadoPendiente;
            foreach (BESugerenciaCompra sugerencia in sugerencias)
            {
                BEDetalleOrdenCompra detalle = new BEDetalleOrdenCompra();
                detalle.IdInsumo = sugerencia.IdInsumo;
                detalle.NombreInsumo = sugerencia.NombreInsumo;
                detalle.UnidadDeMedida = sugerencia.UnidadDeMedida;
                detalle.Cantidad = sugerencia.CantidadSugerida;
                detalle.PrecioUnitario = sugerencia.PrecioUnitario;
                orden.Detalles.Add(detalle);
            }
            orden.CantidadItems = orden.Detalles.Count;

            orden.IdOrdenCompra = bllDV.EjecutarControlado(() => dalOrden.Crear(orden), TablaOrden, TablaDetalle);

            bllEvento.RegistrarDelUsuarioActual("Generación de orden de compra",
                "Se generó la orden de compra " + orden.Numero + " para " + orden.ProveedorTexto + " con " +
                orden.Detalles.Count + (orden.Detalles.Count == 1 ? " insumo" : " insumos") +
                " (importe estimado " + orden.ImporteEstimado.ToString("C") + "), según la demanda prevista para los próximos " +
                dias + " días. " + prediccion.Descripcion);

            return orden;
        }

        #endregion

        #region CU-006-035 Anular orden de compra

        public void Anular(int idOrden, string motivo)
        {
            motivo = motivo == null ? string.Empty : motivo.Trim();

            if (idOrden <= 0 || motivo.Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Elija la orden e ingrese el motivo de la anulación."));
            }
            if (motivo.Length > LargoMotivo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El motivo no puede superar los " + LargoMotivo + " caracteres."));
            }

            BEOrdenCompra orden = ObtenerPorId(idOrden);
            if (orden == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }
            if (!orden.Pendiente)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La orden " + orden.Numero + " está " + orden.EstadoBase.ToLower() + ": solo se anulan órdenes pendientes."));
            }

            // En el estado entra una versión corta del motivo; el texto completo
            // queda en la bitácora.
            string estado = Recortar(BEOrdenCompra.EstadoAnulada + BEOrdenCompra.Separador + motivo, LargoEstado);

            if (!bllDV.EjecutarControlado(() => dalOrden.Anular(idOrden, estado), TablaOrden))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La orden " + orden.Numero + " cambió de estado mientras tanto."));
            }

            bllEvento.RegistrarDelUsuarioActual("Anulación de orden de compra",
                "Se anuló la orden de compra " + orden.Numero + " de " + orden.ProveedorTexto + ". Motivo: " + motivo);
        }

        #endregion

        #region CU-006-036 Registrar factura de proveedor

        /// Registra la factura de una orden pendiente: la orden pasa a Recibida
        /// (con el comprobante) y lo pedido de cada insumo se suma al stock (R8),
        /// todo junto. El importe es opcional y queda en la bitácora.
        public BEOrdenCompra RegistrarFactura(int idOrden, string comprobante, DateTime? fecha, decimal? importe)
        {
            comprobante = comprobante == null ? string.Empty : Regex.Replace(comprobante.Trim(), @"\s+", " ");

            if (idOrden <= 0 || comprobante.Length == 0 || !fecha.HasValue)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }
            if (comprobante.Length > LargoComprobante || !FormatoComprobante.IsMatch(comprobante))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El número de comprobante admite hasta " + LargoComprobante + " letras, números, espacios, " +
                    "guiones o barras (por ejemplo A-0001-00001234)."));
            }
            if (importe.HasValue && (importe.Value < 0 || importe.Value > 9999999999.99m))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El importe tiene que ser un número positivo."));
            }

            BEOrdenCompra orden = ObtenerPorId(idOrden);
            if (orden == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }
            if (!orden.Pendiente)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La orden " + orden.Numero + " está " + orden.EstadoBase.ToLower() + ": solo se reciben órdenes pendientes."));
            }
            if (orden.Detalles.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR017",
                    "La orden " + orden.Numero + " no tiene insumos."));
            }

            DateTime fechaComprobante = fecha.Value.Date;
            if (fechaComprobante > DateTime.Today)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha del comprobante no puede ser posterior a hoy."));
            }
            if (fechaComprobante < orden.Fecha.Date)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha del comprobante no puede ser anterior a la de la orden (" + orden.FechaTexto + ")."));
            }

            // El mismo comprobante no se puede registrar dos veces para el mismo proveedor.
            foreach (BEOrdenCompra otra in TraerOrdenes())
            {
                if (otra.CuitProveedor == orden.CuitProveedor && otra.EstadoBase == BEOrdenCompra.EstadoRecibida &&
                    string.Equals(otra.DetalleEstado, comprobante, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR006",
                        "El comprobante " + comprobante + " ya se registró con la orden " + otra.Numero + "."));
                }
            }

            string estado = BEOrdenCompra.EstadoRecibida + BEOrdenCompra.Separador + comprobante;

            if (!bllDV.EjecutarControlado(() => dalOrden.Recibir(idOrden, estado), TablaOrden, TablaInsumo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La orden " + orden.Numero + " cambió de estado mientras tanto."));
            }

            StringBuilder ingresos = new StringBuilder();
            foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
            {
                if (ingresos.Length > 0)
                {
                    ingresos.Append("; ");
                }
                ingresos.Append(detalle.NombreInsumo).Append(" ").Append(detalle.CantidadTexto);
            }

            bllEvento.RegistrarDelUsuarioActual("Factura de proveedor",
                Recortar("Se registró el comprobante " + comprobante + " del " + fechaComprobante.ToString("dd/MM/yyyy") +
                         " de " + orden.ProveedorTexto + " para la orden " + orden.Numero +
                         (importe.HasValue ? ", por " + importe.Value.ToString("C") : string.Empty) +
                         ". Ingresó al stock: " + ingresos + ".", 500));

            orden.Estado = estado;
            return orden;
        }

        #endregion

        private static string Recortar(string texto, int largo)
        {
            return texto.Length <= largo ? texto : texto.Substring(0, largo - 3) + "...";
        }
    }
}
