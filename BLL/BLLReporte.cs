using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// CU-006-038 Exportar reporte gerencial. Arma los datos del reporte elegido;
    /// el archivo (PDF, Excel o CSV) lo genera ExportadorReporte.
    public class BLLReporte
    {
        public const string ConsumoPorInsumo = "Consumo por insumo";
        public const string VentasPorReceta = "Ventas por receta";
        public const string OrdenesPorProveedor = "Órdenes por proveedor";
        public const string AlertasTermicas = "Alertas térmicas";
        public const string AjustesDeStock = "Ajustes de stock";

        public static readonly string[] Tipos =
        {
            ConsumoPorInsumo, VentasPorReceta, OrdenesPorProveedor, AlertasTermicas, AjustesDeStock
        };

        /// Período máximo de un reporte.
        public const int DiasMaximos = 366;

        private readonly BLLVenta bllVenta = new BLLVenta();
        private readonly BLLInsumo bllInsumo = new BLLInsumo();
        private readonly BLLOrdenCompra bllOrden = new BLLOrdenCompra();
        private readonly BLLAlerta bllAlerta = new BLLAlerta();
        private readonly BLLEvento bllEvento = new BLLEvento();
        private readonly DALArchivoReporte dalArchivo = new DALArchivoReporte();

        /// Genera el reporte, lo guarda en App_Data\Reportes con el formato
        /// elegido y deja el evento en la bitácora. Devuelve la ruta del archivo.
        public string Exportar(string tipo, DateTime? desde, DateTime? hasta, string formato, out string tipoMime)
        {
            if (Array.IndexOf(ExportadorReporte.Formatos, formato) < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025", "El formato elegido no existe."));
            }

            BEReporte reporte = Generar(tipo, desde, hasta);

            string extension;
            byte[] contenido;
            try
            {
                contenido = ExportadorReporte.Exportar(reporte, formato, out extension, out tipoMime);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR015", ex.Message));
            }

            string nombre = "Reporte " + tipo + " " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + extension;
            string ruta;
            try
            {
                ruta = dalArchivo.Guardar(NombreDeArchivo(nombre), contenido);
            }
            catch (UnauthorizedAccessException)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR022",
                    "No se pudo escribir en " + DALArchivoReporte.Carpeta + "."));
            }
            catch (IOException ex)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR015", ex.Message));
            }

            bllEvento.RegistrarDelUsuarioActual("Exportación de reporte gerencial",
                "Se exportó el reporte \"" + tipo + "\" del " + desde.Value.ToString("dd/MM/yyyy") + " al " +
                hasta.Value.ToString("dd/MM/yyyy") + " en formato " + formato + " (" + reporte.Filas.Count +
                (reporte.Filas.Count == 1 ? " fila" : " filas") + "): " + Path.GetFileName(ruta) + ".");

            return ruta;
        }

        /// Nombre de archivo sin acentos, espacios ni caracteres raros, para
        /// que se descargue igual en cualquier navegador.
        private static string NombreDeArchivo(string texto)
        {
            string sinAcentos = texto.Normalize(NormalizationForm.FormD);
            StringBuilder nombre = new StringBuilder();
            foreach (char c in sinAcentos)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                if (char.IsLetterOrDigit(c) && c < 128 || c == '.' || c == '-')
                {
                    nombre.Append(c);
                }
                else if (c == ' ' && nombre.Length > 0 && nombre[nombre.Length - 1] != '_')
                {
                    nombre.Append('_');
                }
            }
            return nombre.ToString();
        }

        public BEReporte Generar(string tipo, DateTime? desde, DateTime? hasta)
        {
            if (string.IsNullOrEmpty(tipo) || !desde.HasValue || !hasta.HasValue)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }
            if (Array.IndexOf(Tipos, tipo) < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025", "El tipo de reporte no existe."));
            }

            DateTime inicio = desde.Value.Date;
            DateTime fin = hasta.Value.Date;
            if (inicio > fin)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha desde no puede ser posterior a la fecha hasta."));
            }
            if ((fin - inicio).Days > DiasMaximos)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El período no puede superar un año."));
            }

            BEReporte reporte = new BEReporte();
            reporte.Titulo = "IntelligentStock - " + tipo;
            reporte.Subtitulo = "Período: " + inicio.ToString("dd/MM/yyyy") + " al " + fin.ToString("dd/MM/yyyy") +
                                ". Emitido el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ".";

            switch (tipo)
            {
                case ConsumoPorInsumo:
                    ArmarConsumoPorInsumo(reporte, inicio, fin);
                    break;
                case VentasPorReceta:
                    ArmarVentasPorReceta(reporte, inicio, fin);
                    break;
                case OrdenesPorProveedor:
                    ArmarOrdenesPorProveedor(reporte, inicio, fin);
                    break;
                case AlertasTermicas:
                    ArmarAlertas(reporte, inicio, fin);
                    break;
                default:
                    ArmarAjustes(reporte, inicio, fin);
                    break;
            }

            if (reporte.Filas.Count == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            return reporte;
        }

        private void ArmarConsumoPorInsumo(BEReporte reporte, DateTime inicio, DateTime fin)
        {
            Dictionary<int, BEInsumo> insumos = new Dictionary<int, BEInsumo>();
            foreach (BEInsumo insumo in bllInsumo.TraerInsumos())
            {
                insumos[insumo.IdInsumo] = insumo;
            }

            Dictionary<int, decimal> consumo = new Dictionary<int, decimal>();
            Dictionary<int, string> nombres = new Dictionary<int, string>();
            foreach (BEVenta venta in bllVenta.TraerVentas(inicio, fin))
            {
                foreach (BEConsumoVenta item in venta.Consumos)
                {
                    decimal previo;
                    consumo.TryGetValue(item.IdInsumo, out previo);
                    consumo[item.IdInsumo] = previo + item.ConsumoTotal;
                    nombres[item.IdInsumo] = item.NombreInsumo;
                }
            }

            int dias = (fin - inicio).Days + 1;
            decimal total = 0;
            reporte.Columnas.AddRange(new[] { "Insumo", "Unidad", "Consumo", "Promedio diario", "Valorizado", "Stock actual" });

            List<int> ids = new List<int>(consumo.Keys);
            ids.Sort(delegate(int a, int b) { return consumo[b].CompareTo(consumo[a]); });
            foreach (int id in ids)
            {
                BEInsumo insumo;
                insumos.TryGetValue(id, out insumo);
                decimal valorizado = insumo == null ? 0m : consumo[id] * insumo.PrecioUnitario;
                total += valorizado;

                reporte.AgregarFila(
                    insumo == null ? nombres[id] : insumo.Nombre,
                    insumo == null ? string.Empty : insumo.UnidadDeMedida,
                    consumo[id].ToString("0.##"),
                    (consumo[id] / dias).ToString("0.##"),
                    valorizado.ToString("C"),
                    insumo == null ? "-" : insumo.StockValor);
            }

            reporte.Pie = "Consumo valorizado al precio unitario actual de cada insumo: " + total.ToString("C") + ".";
        }

        private void ArmarVentasPorReceta(BEReporte reporte, DateTime inicio, DateTime fin)
        {
            Dictionary<int, decimal> cantidades = new Dictionary<int, decimal>();
            Dictionary<int, int> operaciones = new Dictionary<int, int>();
            Dictionary<int, string> nombres = new Dictionary<int, string>();

            foreach (BEVenta venta in bllVenta.TraerVentas(inicio, fin))
            {
                if (venta.Cantidad <= 0)
                {
                    continue;
                }

                decimal cantidad;
                cantidades.TryGetValue(venta.IdReceta, out cantidad);
                cantidades[venta.IdReceta] = cantidad + venta.Cantidad;

                int veces;
                operaciones.TryGetValue(venta.IdReceta, out veces);
                operaciones[venta.IdReceta] = veces + 1;

                nombres[venta.IdReceta] = venta.NombreReceta;
            }

            reporte.Columnas.AddRange(new[] { "Receta", "Unidades vendidas", "Operaciones de venta" });

            List<int> ids = new List<int>(cantidades.Keys);
            ids.Sort(delegate(int a, int b) { return cantidades[b].CompareTo(cantidades[a]); });
            decimal total = 0;
            foreach (int id in ids)
            {
                total += cantidades[id];
                reporte.AgregarFila(nombres[id], cantidades[id].ToString("0.##"), operaciones[id].ToString());
            }

            reporte.Pie = "Total de unidades vendidas: " + total.ToString("0.##") + ".";
        }

        private void ArmarOrdenesPorProveedor(BEReporte reporte, DateTime inicio, DateTime fin)
        {
            reporte.Columnas.AddRange(new[] { "Proveedor", "CUIT", "Pendientes", "Recibidas", "Anuladas", "Importe recibido" });

            decimal total = 0;
            foreach (BEGastoProveedor gasto in bllOrden.GastoPorProveedor(inicio, fin))
            {
                total += gasto.ImporteRecibido;
                reporte.AgregarFila(gasto.Proveedor, gasto.Cuit, gasto.OrdenesPendientes.ToString(),
                    gasto.OrdenesRecibidas.ToString(), gasto.OrdenesAnuladas.ToString(), gasto.ImporteRecibido.ToString("C"));
            }

            reporte.Pie = "Lo recibido se valoriza al precio unitario actual de cada insumo. Total: " + total.ToString("C") + ".";
        }

        private void ArmarAlertas(BEReporte reporte, DateTime inicio, DateTime fin)
        {
            reporte.Columnas.AddRange(new[] { "N°", "Fecha", "Sensor", "Temperatura", "Estado", "Observaciones" });

            foreach (BEAlerta alerta in bllAlerta.TraerAlertas())
            {
                if (alerta.FechaHora.Date < inicio || alerta.FechaHora.Date > fin)
                {
                    continue;
                }
                reporte.AgregarFila(alerta.IdAlerta.ToString(), alerta.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                    alerta.SensorTexto, alerta.TemperaturaTexto, alerta.Estado, alerta.Observaciones ?? string.Empty);
            }

            reporte.Pie = "Alertas en el período: " + reporte.Filas.Count + ".";
        }

        private void ArmarAjustes(BEReporte reporte, DateTime inicio, DateTime fin)
        {
            reporte.Columnas.AddRange(new[] { "Fecha", "Usuario", "Detalle" });

            foreach (BEEvento evento in bllEvento.TraerEventosPorAccion(BLLInsumo.AccionAjuste, inicio))
            {
                if (evento.FechaHora.Date > fin)
                {
                    continue;
                }
                reporte.AgregarFila(evento.FechaHora.ToString("dd/MM/yyyy HH:mm"), evento.Usuario, evento.Descripcion);
            }

            reporte.Pie = "Ajustes en el período: " + reporte.Filas.Count + ".";
        }
    }
}
