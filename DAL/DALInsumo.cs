using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de la tabla Insumo.
    public class DALInsumo
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Graba el insumo y devuelve el id generado.
        public int Crear(BEInsumo insumo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Nombre", insumo.Nombre),
                new SqlParameter("@Descripcion", (object)insumo.Descripcion ?? DBNull.Value),
                new SqlParameter("@UnidadDeMedida", insumo.UnidadDeMedida),
                ParametroDecimal("@Stock", insumo.Stock, 10),
                ParametroDecimal("@StockMinimo", insumo.StockMinimo, 10),
                new SqlParameter("@DiasDeVidaUtil", (object)insumo.DiasDeVidaUtil ?? DBNull.Value),
                ParametroDecimal("@PrecioUnitario", insumo.PrecioUnitario, 10)
            };

            return dalCon.EjecutarYTraerId("CrearInsumo", parametros);
        }

        public void Modificar(BEInsumo insumo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdInsumo", insumo.IdInsumo),
                new SqlParameter("@Nombre", insumo.Nombre),
                new SqlParameter("@Descripcion", (object)insumo.Descripcion ?? DBNull.Value),
                new SqlParameter("@UnidadDeMedida", insumo.UnidadDeMedida),
                ParametroDecimal("@StockMinimo", insumo.StockMinimo, 10),
                new SqlParameter("@DiasDeVidaUtil", (object)insumo.DiasDeVidaUtil ?? DBNull.Value),
                ParametroDecimal("@PrecioUnitario", insumo.PrecioUnitario, 10)
            };

            dalCon.EjecutarProcAlmacenado("ModificarInsumo", parametros);
        }

        /// Baja lógica: marca el insumo como inactivo.
        public void Baja(int idInsumo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdInsumo", idInsumo)
            };

            dalCon.EjecutarProcAlmacenado("BajaInsumo", parametros);
        }

        /// Deshace la baja lógica.
        public void Activar(int idInsumo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdInsumo", idInsumo)
            };

            dalCon.EjecutarProcAlmacenado("ActivarInsumo", parametros);
        }

        /// R8 - Recalcular stock físico. Suma la cantidad (negativa para restar).
        /// Devuelve false si el insumo no está activo o si el stock quedaría negativo.
        public bool AjustarStock(int idInsumo, decimal cantidad)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdInsumo", idInsumo),
                ParametroDecimal("@Cantidad", cantidad, 10)
            };

            return dalCon.EjecutarYTraerId("AjustarStockInsumo", parametros) > 0;
        }

        public List<BEInsumo> TraerInsumos()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerInsumos", null);

            List<BEInsumo> lista = new List<BEInsumo>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public BEInsumo ObtenerPorId(int idInsumo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdInsumo", idInsumo)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerInsumoPorId", parametros);
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        /// Nombres de las recetas activas que llevan el insumo.
        public List<string> TraerRecetasActivasQueLoUsan(int idInsumo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdInsumo", idInsumo)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerRecetasActivasConInsumo", parametros);

            List<string> nombres = new List<string>();
            foreach (DataRow fila in tabla.Rows)
            {
                nombres.Add(Convert.ToString(fila["nombre"]));
            }
            return nombres;
        }

        public int ContarOrdenesPendientesQueLoIncluyen(int idInsumo)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdInsumo", idInsumo)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("ContarOrdenesPendientesConInsumo", parametros);
            return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0][0]);
        }

        /// Parámetro DECIMAL con la precisión de la columna, para que el motor no
        /// lo infiera a partir del valor.
        private static SqlParameter ParametroDecimal(string nombre, decimal valor, byte precision)
        {
            SqlParameter parametro = new SqlParameter(nombre, SqlDbType.Decimal);
            parametro.Precision = precision;
            parametro.Scale = 2;
            parametro.Value = valor;
            return parametro;
        }

        private static BEInsumo Mapear(DataRow fila)
        {
            BEInsumo insumo = new BEInsumo();
            insumo.IdInsumo = Convert.ToInt32(fila["id_insumo"]);
            insumo.Nombre = Convert.ToString(fila["nombre"]);
            insumo.Descripcion = Convert.ToString(fila["descripcion"]);
            insumo.UnidadDeMedida = Convert.ToString(fila["unidadDeMedida"]);
            insumo.Stock = fila["stock"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["stock"]);
            insumo.StockMinimo = fila["stockMinimo"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["stockMinimo"]);
            insumo.DiasDeVidaUtil = fila["diasDeVidaUtil"] == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(fila["diasDeVidaUtil"]);
            insumo.Activo = fila["activo"] != DBNull.Value && Convert.ToBoolean(fila["activo"]);
            insumo.PrecioUnitario = fila["precioUnitario"] == DBNull.Value ? 0m : Convert.ToDecimal(fila["precioUnitario"]);
            return insumo;
        }
    }
}
