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
    ///
    /// La predicción propone qué insumos hacen falta. Como cada proveedor
    /// vende lo suyo, el usuario arma una orden por proveedor: elige cuáles de
    /// los insumos sugeridos le pide a cada uno (el sistema propone el que se
    /// usó la última vez) y puede ajustar las cantidades. La orden se le envía
    /// al proveedor por correo y, al llegar la mercadería, se registra lo que
    /// llegó de verdad y a qué precio.
    ///
    /// Qué se puede hacer con una orden según su estado lo deciden las clases
    /// del patrón State (EstadoOrdenCompra y sus subclases): esta clase es el
    /// contexto que les delega la anulación, la recepción y el armado del
    /// gasto por proveedor.
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

        /// Un motivo de anulación más corto que esto no explica nada ("x", "no").
        public const int LargoMinimoMotivo = 5;

        /// Lo máximo que admite la columna cantidad, DECIMAL(8,2).
        public const decimal CantidadMaxima = 999999.99m;

        /// Importe máximo de una factura.
        public const decimal ImporteMaximo = 9999999999.99m;

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
        /// actual de cada insumo, que se actualiza con el de cada factura.
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

                // State: cada estado sabe cómo cuenta la orden en el gasto del proveedor.
                decimal importe;
                importePorOrden.TryGetValue(orden.IdOrdenCompra, out importe);
                EstadoOrdenCompra.De(orden).AcumularEn(gasto, importe);
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
            Dictionary<int, string> habitual = dalOrden.TraerProveedorHabitual();
            Dictionary<string, string> proveedores = new Dictionary<string, string>();
            foreach (BEProveedor proveedor in bllProveedor.TraerActivos())
            {
                proveedores[proveedor.Cuit] = proveedor.Nombre;
            }

            List<BESugerenciaCompra> sugerencias = new List<BESugerenciaCompra>();
            foreach (BEInsumo insumo in bllInsumo.TraerActivos())
            {
                decimal pendiente;
                pendientes.TryGetValue(insumo.IdInsumo, out pendiente);

                // Un perecedero se pide solo para los días que dura: lo que se
                // compre para después se tira.
                int diasCubiertos = dias;
                bool limitado = false;
                if (insumo.DiasDeVidaUtil.HasValue && insumo.DiasDeVidaUtil.Value > 0 && insumo.DiasDeVidaUtil.Value < dias)
                {
                    diasCubiertos = insumo.DiasDeVidaUtil.Value;
                    limitado = true;
                }
                decimal consumo = prediccion.Consumo(insumo.IdInsumo, diasCubiertos);

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
                sugerencia.CantidadAPedir = sugerencia.CantidadSugerida;
                sugerencia.PrecioUnitario = insumo.PrecioUnitario;
                sugerencia.DiasCubiertos = diasCubiertos;
                sugerencia.LimitadoPorVidaUtil = limitado;

                string cuit;
                if (habitual.TryGetValue(insumo.IdInsumo, out cuit) && proveedores.ContainsKey(cuit))
                {
                    sugerencia.CuitProveedorHabitual = cuit;
                    sugerencia.ProveedorHabitual = proveedores[cuit];
                }
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

        /// Genera la orden con lo que el usuario eligió pedirle a este proveedor
        /// de la propuesta (los insumos marcados, con la cantidad que haya
        /// ajustado) y la deja Pendiente. Si se pide, se la envía al proveedor por
        /// correo con la orden en PDF; si no se puede enviar, la orden queda
        /// generada igual y 'resultadoCorreo' dice por qué.
        public BEOrdenCompra GenerarPredictiva(string cuit, DateTime? fecha, List<BESugerenciaCompra> pedido,
                                               bool enviarPorCorreo, out string resultadoCorreo)
        {
            resultadoCorreo = null;
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

            if (pedido == null || pedido.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "Marque al menos un insumo para pedirle a " + proveedor.Nombre + "."));
            }

            BEOrdenCompra orden = new BEOrdenCompra();
            orden.CuitProveedor = proveedor.Cuit;
            orden.NombreProveedor = proveedor.Nombre;
            orden.Fecha = fechaOrden;
            orden.Estado = EstadoOrdenCompra.Pendiente.Nombre;

            List<string> cambios = new List<string>();
            List<int> incluidos = new List<int>();
            foreach (BESugerenciaCompra item in pedido)
            {
                BEInsumo insumo = bllInsumo.ObtenerPorId(item.IdInsumo);
                if (insumo == null || !insumo.Activo)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                        "El insumo " + item.NombreInsumo + " ya no está activo."));
                }
                if (incluidos.Contains(item.IdInsumo))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR006",
                        "El insumo " + insumo.Nombre + " figura dos veces en el pedido."));
                }
                ValidarCantidadPedida(insumo.Nombre, item.CantidadAPedir);
                incluidos.Add(item.IdInsumo);

                BEDetalleOrdenCompra detalle = new BEDetalleOrdenCompra();
                detalle.IdInsumo = insumo.IdInsumo;
                detalle.NombreInsumo = insumo.Nombre;
                detalle.UnidadDeMedida = insumo.UnidadDeMedida;
                detalle.Cantidad = item.CantidadAPedir;
                detalle.PrecioUnitario = insumo.PrecioUnitario;
                orden.Detalles.Add(detalle);

                if (item.CantidadAPedir != item.CantidadSugerida)
                {
                    cambios.Add(insumo.Nombre + " (sugerido " + item.CantidadSugerida.ToString("0.##") + ", pedido " +
                                item.CantidadAPedir.ToString("0.##") + " " + insumo.UnidadDeMedida + ")");
                }
            }
            orden.CantidadItems = orden.Detalles.Count;

            orden.IdOrdenCompra = bllDV.EjecutarControlado(() => dalOrden.Crear(orden), TablaOrden, TablaDetalle);

            bllEvento.RegistrarDelUsuarioActual("Generación de orden de compra",
                Recortar("Se generó la orden de compra " + orden.Numero + " para " + orden.ProveedorTexto + " con " +
                         orden.Detalles.Count + (orden.Detalles.Count == 1 ? " insumo" : " insumos") +
                         " (importe estimado " + orden.ImporteEstimado.ToString("C") + "), según la demanda prevista para los próximos " +
                         DiasProyeccion + " días." +
                         (cambios.Count > 0 ? " Cantidades cambiadas: " + string.Join("; ", cambios.ToArray()) + "." : string.Empty), 500));

            if (enviarPorCorreo)
            {
                resultadoCorreo = EnviarAlProveedor(orden, proveedor);
            }

            return orden;
        }

        /// Le manda la orden al proveedor por correo, con la orden en PDF.
        /// Devuelve lo que pasó, para mostrarlo: un error de envío no anula la orden.
        private string EnviarAlProveedor(BEOrdenCompra orden, BEProveedor proveedor)
        {
            if (string.IsNullOrEmpty(proveedor.Correo))
            {
                return "La orden no se envió por correo: " + proveedor.Nombre +
                       " no tiene un correo cargado (se puede agregar en Modificación de proveedor).";
            }
            if (!EnviadorCorreo.EstaConfigurado)
            {
                return "La orden no se envió por correo: el envío de correos no está configurado en el sistema.";
            }

            BEReporte documento = new BEReporte();
            documento.Titulo = "Orden de compra " + orden.Numero;
            documento.Subtitulo = "Proveedor: " + orden.ProveedorTexto + ". Fecha: " + orden.FechaTexto + ".";
            documento.Columnas.AddRange(new[] { "Insumo", "Cantidad", "Unidad" });
            foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
            {
                documento.AgregarFila(detalle.NombreInsumo, detalle.Cantidad.ToString("0.##"), detalle.UnidadDeMedida ?? string.Empty);
            }
            documento.Pie = "Por favor, confirme la entrega respondiendo este correo.";

            StringBuilder cuerpo = new StringBuilder();
            cuerpo.Append("Estimados de ").Append(proveedor.Nombre).Append(":\r\n\r\n")
                  .Append("Les enviamos la orden de compra ").Append(orden.Numero).Append(" del ").Append(orden.FechaTexto)
                  .Append(" con el siguiente pedido:\r\n\r\n");
            foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
            {
                cuerpo.Append("  - ").Append(detalle.NombreInsumo).Append(": ").Append(detalle.CantidadTexto).Append("\r\n");
            }
            cuerpo.Append("\r\nSe adjunta la orden en PDF. Por favor, confirmen la entrega respondiendo este correo.\r\n\r\nMuchas gracias.");

            try
            {
                string extension, tipoMime;
                byte[] pdf = ExportadorReporte.Exportar(documento, ExportadorReporte.FormatoPdf, out extension, out tipoMime);
                EnviadorCorreo.Enviar(proveedor.Correo, "Orden de compra " + orden.Numero, cuerpo.ToString(),
                                      pdf, "Orden de compra " + orden.IdOrdenCompra + extension, tipoMime);
            }
            catch (Exception ex)
            {
                bllEvento.RegistrarDelUsuarioActual("Envío de orden de compra",
                    Recortar("No se pudo enviar la orden de compra " + orden.Numero + " a " + proveedor.Correo + ": " + ex.Message, 500),
                    CriticidadEvento.Advertencia);
                return "La orden quedó generada, pero no se pudo enviar por correo a " + proveedor.Correo + " (" + ex.Message +
                       "). Avísele al proveedor por otro medio.";
            }

            bllEvento.RegistrarDelUsuarioActual("Envío de orden de compra",
                "Se envió la orden de compra " + orden.Numero + " por correo a " + proveedor.Nombre + " (" + proveedor.Correo + ").");
            return "Se le envió la orden por correo a " + proveedor.Nombre + " (" + proveedor.Correo + ").";
        }

        private static void ValidarCantidadPedida(string insumo, decimal cantidad)
        {
            if (cantidad <= 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad a pedir de " + insumo + " tiene que ser mayor que cero."));
            }
            if (cantidad > CantidadMaxima)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad a pedir de " + insumo + " no puede superar " + CantidadMaxima.ToString("0.##") + "."));
            }
            if (decimal.Round(cantidad, 2) != cantidad)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad a pedir de " + insumo + " admite hasta dos decimales."));
            }
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
            if (motivo.Length < LargoMinimoMotivo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Explique el motivo de la anulación (al menos " + LargoMinimoMotivo + " caracteres)."));
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
            // State: el estado actual de la orden decide si se puede anular (si no,
            // lanza #ERR021) y cómo queda guardada. En la columna entra una versión
            // corta del motivo; el texto completo queda en la bitácora.
            string estado = Recortar(EstadoOrdenCompra.De(orden).Anular(orden, motivo), LargoEstado);

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

        /// Precio unitario máximo (columna precioUnitario, DECIMAL(10,2)).
        public const decimal PrecioMaximo = 99999999.99m;

        /// Diferencia que se tolera entre la suma de los renglones y el importe
        /// de la factura antes de avisar (impuestos, redondeos).
        public const decimal ToleranciaImporte = 0.01m;

        /// Registra la factura de una orden pendiente: la orden pasa a Recibida
        /// (con el comprobante) y al stock ingresa lo que llegó de cada insumo
        /// (R8), que puede ser menos que lo pedido, todo junto. El precio
        /// facturado de cada insumo pasa a ser su precio unitario. El importe
        /// total de la factura y las diferencias con lo pedido quedan en la
        /// bitácora. 'aviso' informa si la suma de los renglones no coincide
        /// con el importe de la factura (no impide registrarla).
        public BEOrdenCompra RegistrarFactura(int idOrden, string comprobante, DateTime? fecha, decimal? importe,
                                              List<BEDetalleOrdenCompra> recibido, out string aviso)
        {
            aviso = null;
            comprobante = comprobante == null ? string.Empty : Regex.Replace(comprobante.Trim(), @"\s+", " ");

            if (idOrden <= 0 || comprobante.Length == 0 || !fecha.HasValue || !importe.HasValue)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }
            if (comprobante.Length > LargoComprobante || !FormatoComprobante.IsMatch(comprobante))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El número de comprobante admite hasta " + LargoComprobante + " letras, números, espacios, " +
                    "guiones o barras (por ejemplo A-0001-00001234)."));
            }
            if (importe.Value <= 0 || importe.Value > ImporteMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El importe total tiene que ser mayor que cero."));
            }
            if (decimal.Round(importe.Value, 2) != importe.Value)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El importe total admite hasta dos decimales."));
            }

            BEOrdenCompra orden = ObtenerPorId(idOrden);
            if (orden == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }
            // State: el estado actual de la orden decide si se puede recibir (si no,
            // lanza #ERR021) y cómo queda guardada.
            string estado = EstadoOrdenCompra.De(orden).Recibir(orden, comprobante);
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
                if (otra.CuitProveedor == orden.CuitProveedor && EstadoOrdenCompra.De(otra) == EstadoOrdenCompra.Recibida &&
                    string.Equals(otra.DetalleEstado, comprobante, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR006",
                        "El comprobante " + comprobante + " ya se registró con la orden " + otra.Numero + "."));
                }
            }

            // Lo que llegó de cada renglón de la orden, con su precio facturado.
            List<BEDetalleOrdenCompra> llegado = new List<BEDetalleOrdenCompra>();
            List<string> diferencias = new List<string>();
            List<string> precios = new List<string>();
            decimal sumaRenglones = 0;
            foreach (BEDetalleOrdenCompra pedido in orden.Detalles)
            {
                BEDetalleOrdenCompra recibidoDelInsumo = recibido == null ? null
                    : recibido.Find(delegate(BEDetalleOrdenCompra d) { return d.IdInsumo == pedido.IdInsumo; });
                decimal cantidad = recibidoDelInsumo == null ? pedido.Cantidad : recibidoDelInsumo.Cantidad;
                decimal precio = recibidoDelInsumo == null ? pedido.PrecioUnitario : recibidoDelInsumo.PrecioUnitario;
                ValidarRecibido(pedido.NombreInsumo, cantidad, precio);

                BEDetalleOrdenCompra linea = new BEDetalleOrdenCompra();
                linea.IdOrdenCompra = idOrden;
                linea.IdInsumo = pedido.IdInsumo;
                linea.NombreInsumo = pedido.NombreInsumo;
                linea.UnidadDeMedida = pedido.UnidadDeMedida;
                linea.Cantidad = cantidad;
                linea.PrecioUnitario = precio;
                llegado.Add(linea);
                sumaRenglones += cantidad * precio;

                if (cantidad != pedido.Cantidad)
                {
                    diferencias.Add(pedido.NombreInsumo + " (pedido " + pedido.Cantidad.ToString("0.##") + ", recibido " +
                                    cantidad.ToString("0.##") + " " + pedido.UnidadDeMedida + ")");
                }
                if (precio > 0 && precio != pedido.PrecioUnitario)
                {
                    precios.Add(pedido.NombreInsumo + " " + pedido.PrecioUnitario.ToString("C") + " a " + precio.ToString("C"));
                }
            }

            bool llegoAlgo = false;
            foreach (BEDetalleOrdenCompra linea in llegado)
            {
                llegoAlgo = llegoAlgo || linea.Cantidad > 0;
            }
            if (!llegoAlgo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "No llegó ningún insumo de la orden " + orden.Numero + ": si no se va a recibir, anúlela."));
            }

            if (!bllDV.EjecutarControlado(() => dalOrden.Recibir(idOrden, estado, llegado), TablaOrden, TablaDetalle, TablaInsumo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La orden " + orden.Numero + " cambió de estado mientras tanto."));
            }

            StringBuilder ingresos = new StringBuilder();
            foreach (BEDetalleOrdenCompra linea in llegado)
            {
                if (linea.Cantidad <= 0)
                {
                    continue;
                }
                if (ingresos.Length > 0)
                {
                    ingresos.Append("; ");
                }
                ingresos.Append(linea.NombreInsumo).Append(" ").Append(linea.CantidadTexto);
            }

            bllEvento.RegistrarDelUsuarioActual("Factura de proveedor",
                Recortar("Se registró el comprobante " + comprobante + " del " + fechaComprobante.ToString("dd/MM/yyyy") +
                         " de " + orden.ProveedorTexto + " para la orden " + orden.Numero +
                         ", por " + importe.Value.ToString("C") +
                         ". Ingresó al stock: " + ingresos + "." +
                         (diferencias.Count > 0 ? " Llegó distinto de lo pedido: " + string.Join("; ", diferencias.ToArray()) + "." : string.Empty) +
                         (precios.Count > 0 ? " Precios actualizados: " + string.Join("; ", precios.ToArray()) + "." : string.Empty), 500));

            if (Math.Abs(sumaRenglones - importe.Value) > Math.Max(1m, importe.Value * ToleranciaImporte))
            {
                aviso = "La suma de los renglones (" + sumaRenglones.ToString("C") + ") no coincide con el importe de la factura (" +
                        importe.Value.ToString("C") + "): puede deberse a impuestos o a un precio mal cargado.";
            }

            orden.Estado = estado;
            orden.Detalles.Clear();
            orden.Detalles.AddRange(llegado);
            return orden;
        }

        #endregion

        private static void ValidarRecibido(string insumo, decimal cantidad, decimal precio)
        {
            if (cantidad < 0 || cantidad > CantidadMaxima)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad recibida de " + insumo + " tiene que estar entre 0 y " + CantidadMaxima.ToString("0.##") + "."));
            }
            if (decimal.Round(cantidad, 2) != cantidad)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La cantidad recibida de " + insumo + " admite hasta dos decimales."));
            }
            if (precio < 0 || precio > PrecioMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El precio unitario de " + insumo + " tiene que estar entre 0 y " + PrecioMaximo.ToString("N2") + "."));
            }
            if (decimal.Round(precio, 2) != precio)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El precio unitario de " + insumo + " admite hasta dos decimales."));
            }
        }

        private static string Recortar(string texto, int largo)
        {
            return texto.Length <= largo ? texto : texto.Substring(0, largo - 3) + "...";
        }
    }
}
