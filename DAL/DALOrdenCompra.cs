using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using BE;

namespace DAL
{
    /// Acceso a datos de las tablas OrdenCompra y DetalleOrdenCompra.
    public class DALOrdenCompra
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Graba la orden con su detalle en un solo procedimiento (misma
        /// transacción). Devuelve el número de la orden nueva.
        public int Crear(BEOrdenCompra orden)
        {
            StringBuilder xml = new StringBuilder("<Detalle>");
            foreach (BEDetalleOrdenCompra detalle in orden.Detalles)
            {
                xml.Append("<Insumo idInsumo=\"")
                   .Append(detalle.IdInsumo.ToString(CultureInfo.InvariantCulture))
                   .Append("\" cantidad=\"")
                   .Append(detalle.Cantidad.ToString(CultureInfo.InvariantCulture))
                   .Append("\" />");
            }
            xml.Append("</Detalle>");

            SqlParameter parametroDetalle = new SqlParameter("@Detalle", SqlDbType.Xml);
            parametroDetalle.Value = xml.ToString();

            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Cuit", orden.CuitProveedor),
                new SqlParameter("@Fecha", orden.Fecha.Date),
                parametroDetalle
            };

            return dalCon.EjecutarYTraerId("CrearOrdenCompra", parametros);
        }

        /// Todas las órdenes, de la más nueva a la más vieja (sin el detalle).
        public List<BEOrdenCompra> TraerOrdenes()
        {
            List<BEOrdenCompra> ordenes = new List<BEOrdenCompra>();
            foreach (DataRow fila in dalCon.ConsultaProcAlmacenado("TraerOrdenesCompra", null).Rows)
            {
                ordenes.Add(Mapear(fila));
            }
            return ordenes;
        }

        /// Una orden con su detalle.
        public BEOrdenCompra ObtenerPorId(int idOrden)
        {
            SqlParameter[] parametros = new SqlParameter[] { new SqlParameter("@IdOrden", idOrden) };
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerOrdenCompraPorId", parametros);
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            BEOrdenCompra orden = Mapear(tabla.Rows[0]);
            orden.Detalles.AddRange(TraerDetalle(idOrden));
            return orden;
        }

        /// Detalle de una orden, o de todas si idOrden es null.
        public List<BEDetalleOrdenCompra> TraerDetalle(int? idOrden)
        {
            SqlParameter parametro = new SqlParameter("@IdOrden", SqlDbType.Int);
            parametro.Value = idOrden.HasValue ? (object)idOrden.Value : DBNull.Value;

            List<BEDetalleOrdenCompra> detalles = new List<BEDetalleOrdenCompra>();
            foreach (DataRow fila in dalCon.ConsultaProcAlmacenado("TraerDetalleOrdenCompra", new SqlParameter[] { parametro }).Rows)
            {
                BEDetalleOrdenCompra detalle = new BEDetalleOrdenCompra();
                detalle.IdOrdenCompra = Convert.ToInt32(fila["id_ordenCompra"]);
                detalle.IdInsumo = Convert.ToInt32(fila["id_insumo"]);
                detalle.NombreInsumo = Convert.ToString(fila["nombre"]);
                detalle.UnidadDeMedida = Convert.ToString(fila["unidadDeMedida"]);
                detalle.Cantidad = fila["cantidad"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["cantidad"]);
                detalle.PrecioUnitario = fila["precioUnitario"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["precioUnitario"]);
                detalles.Add(detalle);
            }
            return detalles;
        }

        /// Lo que ya está pedido en órdenes pendientes, por id de insumo.
        public Dictionary<int, decimal> TraerCantidadesPendientes()
        {
            Dictionary<int, decimal> pendientes = new Dictionary<int, decimal>();
            foreach (DataRow fila in dalCon.ConsultaProcAlmacenado("TraerCantidadesPendientesPorInsumo", null).Rows)
            {
                pendientes[Convert.ToInt32(fila["id_insumo"])] =
                    fila["cantidad"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["cantidad"]);
            }
            return pendientes;
        }

        /// Pasa la orden a Anulada si sigue Pendiente. Devuelve false si no.
        public bool Anular(int idOrden, string estado)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdOrden", idOrden),
                new SqlParameter("@Estado", estado)
            };
            return dalCon.EjecutarYTraerId("AnularOrdenCompra", parametros) > 0;
        }

        /// Suma al stock lo pedido y pasa la orden a Recibida, todo junto, si
        /// sigue Pendiente. Devuelve false si no.
        public bool Recibir(int idOrden, string estado)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdOrden", idOrden),
                new SqlParameter("@Estado", estado)
            };
            return dalCon.EjecutarYTraerId("RecibirOrdenCompra", parametros) > 0;
        }

        private static BEOrdenCompra Mapear(DataRow fila)
        {
            BEOrdenCompra orden = new BEOrdenCompra();
            orden.IdOrdenCompra = Convert.ToInt32(fila["id_ordenCompra"]);
            orden.CuitProveedor = Convert.ToString(fila["cuitProveedor"]);
            orden.NombreProveedor = fila["nombreProveedor"] == DBNull.Value ? string.Empty : Convert.ToString(fila["nombreProveedor"]);
            orden.Fecha = fila["fecha"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(fila["fecha"]);
            orden.Estado = fila["estado"] == DBNull.Value ? string.Empty : Convert.ToString(fila["estado"]);
            orden.CantidadItems = Convert.ToInt32(fila["cantidadItems"]);
            return orden;
        }
    }
}
