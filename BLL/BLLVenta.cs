using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// CU-006-037 Descontar stock por venta.
    ///
    /// Cada venta descuenta del stock lo que consume la receta (R8) y queda
    /// registrada en la bitácora con la acción "Venta". Ese registro es, además,
    /// el historial de consumo con el que se entrena la predicción de demanda
    /// (CU-006-033) y el que usa el reporte gerencial, por eso tiene un formato
    /// fijo que se puede volver a leer:
    ///
    ///   Venta de 3 x Pizza muzzarella (receta N° 2) del 11/10/2026.
    ///   Insumos: Harina 000 (N° 3) 0.75 kg; Muzzarella (N° 5) 0.6 kg.
    ///
    /// Las cantidades van con punto decimal, sin importar la cultura del servidor.
    public class BLLVenta
    {
        public const string AccionVenta = "Venta";

        /// Unidades que se pueden vender en una sola operación.
        public const decimal CantidadMaxima = 10000m;

        /// Hasta cuántos días hacia atrás se puede registrar una venta.
        public const int DiasMaximosHaciaAtras = 365;

        /// La descripción del evento es VARCHAR(500): una venta con muchos
        /// insumos se reparte en varios eventos.
        private const int LargoMaximoDescripcion = 480;

        private static readonly Regex FormatoEncabezado = new Regex(
            @"^Venta de (?<cantidad>\d+(?:\.\d+)?) x (?<receta>.*?) \(receta N° (?<id>\d+)\) del (?<fecha>\d{2}/\d{2}/\d{4})",
            RegexOptions.Compiled);

        private static readonly Regex FormatoContinuacion = new Regex(
            @"^Venta \(continuación\) de (?<receta>.*?) \(receta N° (?<id>\d+)\) del (?<fecha>\d{2}/\d{2}/\d{4})",
            RegexOptions.Compiled);

        private static readonly Regex FormatoInsumo = new Regex(
            @"(?<nombre>[^:;]+?) \(N° (?<id>\d+)\) (?<cantidad>\d+(?:\.\d+)?) ?(?<unidad>[^;.]*)[;.]",
            RegexOptions.Compiled);

        private readonly DALVenta dalVenta = new DALVenta();
        private readonly DALInsumo dalInsumo = new DALInsumo();
        private readonly DALEvento dalEvento = new DALEvento();
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

        /// Lo que se va a descontar de cada insumo, sin grabar nada, para
        /// mostrarlo antes de confirmar.
        public List<BEConsumoVenta> Previsualizar(int idReceta, decimal cantidad)
        {
            BEReceta receta = ObtenerRecetaVendible(idReceta);
            ValidarCantidad(cantidad);

            List<BEConsumoVenta> consumos = new List<BEConsumoVenta>();
            foreach (BEDetalleReceta detalle in receta.Detalles)
            {
                BEInsumo insumo = dalInsumo.ObtenerPorId(detalle.IdInsumo);

                BEConsumoVenta consumo = new BEConsumoVenta();
                consumo.IdInsumo = detalle.IdInsumo;
                consumo.NombreInsumo = detalle.NombreInsumo;
                consumo.UnidadDeMedida = detalle.UnidadDeMedida;
                consumo.ConsumoUnitario = detalle.Cantidad;
                consumo.ConsumoTotal = Math.Round(detalle.Cantidad * cantidad, 2);
                consumo.StockAnterior = insumo == null ? 0m : insumo.Stock;
                consumo.StockResultante = consumo.StockAnterior - consumo.ConsumoTotal;
                consumos.Add(consumo);
            }
            return consumos;
        }

        /// Registra la venta: descuenta el stock de todos los insumos de la
        /// receta en una sola transacción y deja el evento en la bitácora.
        public List<BEConsumoVenta> Registrar(int idReceta, decimal cantidad, DateTime? fecha)
        {
            BEReceta receta = ObtenerRecetaVendible(idReceta);
            ValidarCantidad(cantidad);

            DateTime fechaVenta = fecha.HasValue ? fecha.Value.Date : DateTime.Today;
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

            List<BEConsumoVenta> consumos = null;
            bool hayInactivos = false;
            bool descontado = bllDV.EjecutarControlado(
                () => dalVenta.Descontar(idReceta, cantidad, out consumos, out hayInactivos), "Insumo");

            if (!descontado)
            {
                List<string> faltantes = new List<string>();
                foreach (BEConsumoVenta consumo in consumos)
                {
                    faltantes.Add(consumo.NombreInsumo + " (hay " + consumo.StockAnteriorTexto + ", hacen falta " +
                                  consumo.ConsumoTotalTexto + ")");
                }

                if (hayInactivos)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                        "La receta lleva insumos dados de baja. Revísela desde Modificación de receta."));
                }

                throw new ApplicationException(AdministradorErrores.Mensaje("ERR018",
                    "No alcanza el stock de: " + string.Join("; ", faltantes.ToArray()) + ". No se descontó nada."));
            }

            // La cantidad por unidad sale de la receta.
            foreach (BEConsumoVenta consumo in consumos)
            {
                foreach (BEDetalleReceta detalle in receta.Detalles)
                {
                    if (detalle.IdInsumo == consumo.IdInsumo)
                    {
                        consumo.ConsumoUnitario = detalle.Cantidad;
                    }
                }
            }

            RegistrarEnBitacora(receta, cantidad, fechaVenta, consumos);

            // Patrón Observer: avisa si algún insumo cruzó su stock mínimo.
            foreach (BEConsumoVenta consumo in consumos)
            {
                BEInsumo actualizado = dalInsumo.ObtenerPorId(consumo.IdInsumo);
                controladorStock.RegistrarDescuento(actualizado, consumo.StockAnterior);
            }

            return consumos;
        }

        /// Ventas con fecha de venta entre desde y hasta (inclusive), leídas de
        /// la bitácora.
        public List<BEVenta> TraerVentas(DateTime desde, DateTime hasta)
        {
            desde = desde.Date;
            hasta = hasta.Date;

            List<BEVenta> ventas = new List<BEVenta>();

            // El evento se graba el día que se registra la venta, que nunca es
            // anterior a la fecha de la venta: alcanza con buscar desde 'desde'.
            foreach (BEEvento evento in dalEvento.TraerEventosPorAccionDesde(AccionVenta, desde))
            {
                BEVenta venta = Interpretar(evento.Descripcion);
                if (venta != null && venta.Fecha >= desde && venta.Fecha <= hasta)
                {
                    ventas.Add(venta);
                }
            }

            return ventas;
        }

        /// Lee una venta del texto del evento. Devuelve null si el texto no
        /// tiene el formato esperado.
        public static BEVenta Interpretar(string descripcion)
        {
            if (string.IsNullOrEmpty(descripcion))
            {
                return null;
            }

            BEVenta venta = new BEVenta();
            Match encabezado = FormatoEncabezado.Match(descripcion);
            if (encabezado.Success)
            {
                venta.Cantidad = decimal.Parse(encabezado.Groups["cantidad"].Value, CultureInfo.InvariantCulture);
            }
            else
            {
                encabezado = FormatoContinuacion.Match(descripcion);
                if (!encabezado.Success)
                {
                    return null;
                }
                venta.Cantidad = 0m;
            }

            DateTime fecha;
            if (!DateTime.TryParseExact(encabezado.Groups["fecha"].Value, "dd/MM/yyyy",
                                        CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            {
                return null;
            }

            venta.Fecha = fecha;
            venta.IdReceta = int.Parse(encabezado.Groups["id"].Value, CultureInfo.InvariantCulture);
            venta.NombreReceta = encabezado.Groups["receta"].Value;

            int inicio = descripcion.IndexOf("Insumos:", StringComparison.Ordinal);
            if (inicio >= 0)
            {
                foreach (Match item in FormatoInsumo.Matches(descripcion.Substring(inicio + "Insumos:".Length)))
                {
                    BEConsumoVenta consumo = new BEConsumoVenta();
                    consumo.IdInsumo = int.Parse(item.Groups["id"].Value, CultureInfo.InvariantCulture);
                    consumo.NombreInsumo = item.Groups["nombre"].Value.Trim();
                    consumo.UnidadDeMedida = item.Groups["unidad"].Value.Trim();
                    consumo.ConsumoTotal = decimal.Parse(item.Groups["cantidad"].Value, CultureInfo.InvariantCulture);
                    venta.Consumos.Add(consumo);
                }
            }

            return venta;
        }

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

        private void RegistrarEnBitacora(BEReceta receta, decimal cantidad, DateTime fecha, List<BEConsumoVenta> consumos)
        {
            string nombre = receta.Nombre.Replace("(", "[").Replace(")", "]");
            string encabezado = "Venta de " + cantidad.ToString("0.##", CultureInfo.InvariantCulture) + " x " + nombre +
                                " (receta N° " + receta.IdReceta + ") del " +
                                fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ". Insumos: ";
            string continuacion = "Venta (continuación) de " + nombre + " (receta N° " + receta.IdReceta + ") del " +
                                  fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + ". Insumos: ";

            StringBuilder actual = new StringBuilder(encabezado);
            bool hayItems = false;

            for (int i = 0; i < consumos.Count; i++)
            {
                BEConsumoVenta consumo = consumos[i];
                string item = Limpiar(consumo.NombreInsumo) + " (N° " + consumo.IdInsumo + ") " +
                              consumo.ConsumoTotal.ToString("0.##", CultureInfo.InvariantCulture) + " " +
                              Limpiar(consumo.UnidadDeMedida);

                if (hayItems && actual.Length + item.Length + 2 > LargoMaximoDescripcion)
                {
                    actual.Append(".");
                    bllEvento.RegistrarDelUsuarioActual(AccionVenta, actual.ToString());
                    actual = new StringBuilder(continuacion);
                    hayItems = false;
                }

                if (hayItems)
                {
                    actual.Append("; ");
                }
                actual.Append(item);
                hayItems = true;
            }

            actual.Append(".");
            bllEvento.RegistrarDelUsuarioActual(AccionVenta, actual.ToString());
        }

        /// Saca del nombre los caracteres que separan los datos del evento.
        private static string Limpiar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }
            return texto.Replace(";", ",").Replace(":", " ").Replace("(", "[").Replace(")", "]").Replace(".", " ").Trim();
        }

        #endregion
    }
}
