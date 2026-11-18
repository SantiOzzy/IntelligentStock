using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using BE;

namespace DAL
{
    /// Ventas (CU-006-037): tablas Venta y ConsumoVenta.
    public class DALVenta
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Registra las ventas y descuenta del stock lo que lleva cada receta,
        /// todo en una sola transacción. Devuelve una fila por insumo con el
        /// total descontado y su stock anterior y nuevo. Si a un insumo no le
        /// alcanzaba, igual se registra y queda en cero (lo informa Faltante).
        /// Si alguna receta lleva un insumo dado de baja no se graba nada,
        /// devuelve false y en 'consumos' quedan esos insumos.
        public bool Registrar(List<BEVenta> ventas, string origen, out List<BEConsumoVenta> consumos)
        {
            SqlParameter parametroVentas = new SqlParameter("@Ventas", SqlDbType.Xml);
            parametroVentas.Value = ArmarXml(ventas);

            SqlParameter[] parametros = new SqlParameter[]
            {
                parametroVentas,
                new SqlParameter("@Origen", origen)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("RegistrarVentas", parametros);

            consumos = new List<BEConsumoVenta>();
            bool ok = true;
            foreach (DataRow fila in tabla.Rows)
            {
                if (Convert.ToString(fila["resultado"]) == "I")
                {
                    ok = false;
                }

                BEConsumoVenta consumo = new BEConsumoVenta();
                consumo.IdInsumo = Convert.ToInt32(fila["id_insumo"]);
                consumo.NombreInsumo = Convert.ToString(fila["nombre"]);
                consumo.UnidadDeMedida = fila["unidadDeMedida"] == DBNull.Value ? string.Empty : Convert.ToString(fila["unidadDeMedida"]);
                consumo.StockAnterior = Convert.ToDecimal(fila["stockAnterior"]);
                consumo.ConsumoTotal = Convert.ToDecimal(fila["consumo"]);
                consumo.StockResultante = Convert.ToDecimal(fila["stockNuevo"]);
                consumos.Add(consumo);
            }

            return ok;
        }

        /// Ventas con fecha entre desde y hasta (inclusive), con sus consumos.
        public List<BEVenta> TraerVentas(DateTime desde, DateTime hasta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Desde", SqlDbType.Date) { Value = desde.Date },
                new SqlParameter("@Hasta", SqlDbType.Date) { Value = hasta.Date }
            };

            List<BEVenta> ventas = new List<BEVenta>();
            BEVenta actual = null;
            foreach (DataRow fila in dalCon.ConsultaProcAlmacenado("TraerVentas", parametros).Rows)
            {
                int idVenta = Convert.ToInt32(fila["id_venta"]);
                if (actual == null || actual.IdVenta != idVenta)
                {
                    actual = new BEVenta();
                    actual.IdVenta = idVenta;
                    actual.IdReceta = Convert.ToInt32(fila["id_receta"]);
                    actual.NombreReceta = Convert.ToString(fila["nombreReceta"]);
                    actual.Cantidad = Convert.ToDecimal(fila["cantidad"]);
                    actual.Fecha = Convert.ToDateTime(fila["fecha"]);
                    actual.Origen = Convert.ToString(fila["origen"]);
                    actual.FechaRegistro = Convert.ToDateTime(fila["fechaRegistro"]);
                    ventas.Add(actual);
                }

                if (fila["id_insumo"] != DBNull.Value)
                {
                    BEConsumoVenta consumo = new BEConsumoVenta();
                    consumo.IdInsumo = Convert.ToInt32(fila["id_insumo"]);
                    consumo.NombreInsumo = Convert.ToString(fila["nombreInsumo"]);
                    consumo.UnidadDeMedida = fila["unidadDeMedida"] == DBNull.Value ? string.Empty : Convert.ToString(fila["unidadDeMedida"]);
                    consumo.ConsumoTotal = Convert.ToDecimal(fila["consumo"]);
                    actual.Consumos.Add(consumo);
                }
            }
            return ventas;
        }

        /// <Ventas><Venta idReceta="2" cantidad="3" fecha="2026-11-17" /></Ventas>.
        /// Números con punto decimal y fechas ISO, sin importar la cultura del servidor.
        private static string ArmarXml(List<BEVenta> ventas)
        {
            StringBuilder xml = new StringBuilder();
            XmlWriterSettings configuracion = new XmlWriterSettings();
            configuracion.OmitXmlDeclaration = true;

            using (XmlWriter writer = XmlWriter.Create(new StringWriter(xml), configuracion))
            {
                writer.WriteStartElement("Ventas");
                foreach (BEVenta venta in ventas)
                {
                    writer.WriteStartElement("Venta");
                    writer.WriteAttributeString("idReceta", venta.IdReceta.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("cantidad", venta.Cantidad.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("fecha", venta.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            return xml.ToString();
        }
    }
}
