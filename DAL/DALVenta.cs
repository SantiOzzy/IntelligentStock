using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Descuento de stock por venta (CU-006-037).
    public class DALVenta
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Descuenta el consumo de la receta vendida en una sola transacción.
        /// Si todo salió bien devuelve true y en 'consumos' cada insumo con su
        /// stock anterior y el nuevo. Si a algún insumo no le alcanza el stock
        /// (o está dado de baja) no descuenta nada, devuelve false y en
        /// 'consumos' quedan esos insumos.
        public bool Descontar(int idReceta, decimal cantidad, out List<BEConsumoVenta> consumos, out bool hayInactivos)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdReceta", idReceta),
                new SqlParameter("@Cantidad", cantidad)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("DescontarStockPorVenta", parametros);

            consumos = new List<BEConsumoVenta>();
            hayInactivos = false;
            bool ok = true;

            foreach (DataRow fila in tabla.Rows)
            {
                string resultado = Convert.ToString(fila["resultado"]);
                if (resultado != "OK")
                {
                    ok = false;
                }
                if (resultado == "I")
                {
                    hayInactivos = true;
                }

                BEConsumoVenta consumo = new BEConsumoVenta();
                consumo.IdInsumo = Convert.ToInt32(fila["id_insumo"]);
                consumo.NombreInsumo = Convert.ToString(fila["nombre"]);
                consumo.UnidadDeMedida = Convert.ToString(fila["unidadDeMedida"]);
                consumo.StockAnterior = Convert.ToDecimal(fila["stockAnterior"]);
                consumo.ConsumoTotal = Convert.ToDecimal(fila["consumo"]);
                consumo.StockResultante = ok ? Convert.ToDecimal(fila["stockNuevo"]) : consumo.StockAnterior - consumo.ConsumoTotal;
                consumos.Add(consumo);
            }

            return ok;
        }
    }
}
