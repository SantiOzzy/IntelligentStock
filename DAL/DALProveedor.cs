using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de la tabla Proveedor.
    public class DALProveedor
    {
        private readonly DALConexion dalCon = new DALConexion();

        public void Crear(BEProveedor proveedor)
        {
            dalCon.EjecutarProcAlmacenado("CrearProveedor", Parametros(proveedor));
        }

        public void Modificar(BEProveedor proveedor)
        {
            dalCon.EjecutarProcAlmacenado("ModificarProveedor", Parametros(proveedor));
        }

        /// Baja lógica: marca el proveedor como inactivo.
        public void Baja(string cuit)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Cuit", cuit)
            };

            dalCon.EjecutarProcAlmacenado("BajaProveedor", parametros);
        }

        /// Deshace la baja lógica.
        public void Activar(string cuit)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Cuit", cuit)
            };

            dalCon.EjecutarProcAlmacenado("ActivarProveedor", parametros);
        }

        public List<BEProveedor> TraerProveedores()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerProveedores", null);

            List<BEProveedor> lista = new List<BEProveedor>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        public BEProveedor ObtenerPorCuit(string cuit)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Cuit", cuit)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerProveedorPorCuit", parametros);
            return tabla.Rows.Count == 0 ? null : Mapear(tabla.Rows[0]);
        }

        /// Cantidad de órdenes de compra del proveedor que todavía están pendientes.
        public int ContarOrdenesPendientes(string cuit)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Cuit", cuit)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("ContarOrdenesPendientesDeProveedor", parametros);
            return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0][0]);
        }

        private static SqlParameter[] Parametros(BEProveedor proveedor)
        {
            return new SqlParameter[]
            {
                new SqlParameter("@Cuit", proveedor.Cuit),
                new SqlParameter("@Nombre", proveedor.Nombre),
                new SqlParameter("@RazonSocial", (object)proveedor.RazonSocial ?? DBNull.Value),
                new SqlParameter("@Correo", (object)proveedor.Correo ?? DBNull.Value),
                new SqlParameter("@Numero", (object)proveedor.Numero ?? DBNull.Value)
            };
        }

        private static BEProveedor Mapear(DataRow fila)
        {
            BEProveedor proveedor = new BEProveedor();
            proveedor.Cuit = Convert.ToString(fila["cuit"]);
            proveedor.Nombre = Convert.ToString(fila["nombre"]);
            proveedor.RazonSocial = Convert.ToString(fila["razonSocial"]);
            proveedor.Correo = Convert.ToString(fila["correo"]);
            proveedor.Numero = Convert.ToString(fila["numero"]);
            proveedor.Activo = fila["activo"] != DBNull.Value && Convert.ToBoolean(fila["activo"]);
            return proveedor;
        }
    }
}
