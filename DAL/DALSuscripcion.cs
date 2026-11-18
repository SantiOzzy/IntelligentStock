using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de la tabla Suscripcion.
    public class DALSuscripcion
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Graba la suscripción y devuelve el id generado. El token ya viene
        /// encriptado desde la BLL.
        public int Crear(BESuscripcion suscripcion)
        {
            SqlParameter fecha = new SqlParameter("@FechaInicio", SqlDbType.Date);
            fecha.Value = suscripcion.FechaInicio.Date;
            SqlParameter pagadoHasta = new SqlParameter("@PagadoHasta", SqlDbType.Date);
            pagadoHasta.Value = suscripcion.PagadoHasta.Date;

            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@TipoPlan", suscripcion.TipoPlan),
                // Al contado no hay token: se guarda NULL.
                new SqlParameter("@TokenPago", string.IsNullOrEmpty(suscripcion.TokenPago)
                    ? (object)DBNull.Value
                    : suscripcion.TokenPago),
                new SqlParameter("@Estado", suscripcion.Estado),
                fecha,
                pagadoHasta
            };

            return dalCon.EjecutarYTraerId("CrearSuscripcion", parametros);
        }

        public void CambiarEstado(int idSuscripcion, string estado)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSuscripcion", idSuscripcion),
                new SqlParameter("@Estado", estado)
            };

            dalCon.EjecutarProcAlmacenado("CambiarEstadoSuscripcion", parametros);
        }

        /// Registra que se cobró el abono de un mes: lo pagado pasa de
        /// pagadoHastaAnterior a pagadoHasta. Devuelve false si la suscripción ya
        /// no está activa o si ese cobro ya se había registrado.
        public bool RegistrarCobro(int idSuscripcion, DateTime pagadoHastaAnterior, DateTime pagadoHasta)
        {
            SqlParameter anterior = new SqlParameter("@PagadoHastaAnterior", SqlDbType.Date);
            anterior.Value = pagadoHastaAnterior.Date;
            SqlParameter nuevo = new SqlParameter("@PagadoHasta", SqlDbType.Date);
            nuevo.Value = pagadoHasta.Date;

            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSuscripcion", idSuscripcion),
                anterior,
                nuevo
            };

            return dalCon.EjecutarYTraerId("RegistrarCobroSuscripcion", parametros) > 0;
        }

        public List<BESuscripcion> TraerSuscripciones()
        {
            return Mapear(dalCon.ConsultaProcAlmacenado("TraerSuscripciones", null));
        }

        public BESuscripcion ObtenerPorId(int idSuscripcion)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSuscripcion", idSuscripcion)
            };

            List<BESuscripcion> lista = Mapear(dalCon.ConsultaProcAlmacenado("TraerSuscripcionPorId", parametros));
            return lista.Count == 0 ? null : lista[0];
        }

        private static List<BESuscripcion> Mapear(DataTable tabla)
        {
            List<BESuscripcion> lista = new List<BESuscripcion>();

            foreach (DataRow fila in tabla.Rows)
            {
                BESuscripcion suscripcion = new BESuscripcion();
                suscripcion.IdSuscripcion = Convert.ToInt32(fila["id_suscripcion"]);
                suscripcion.TipoPlan = Convert.ToString(fila["tipoPlan"]);
                suscripcion.TokenPago = Convert.ToString(fila["tokenPago"]);
                suscripcion.Estado = Convert.ToString(fila["estado"]);
                suscripcion.FechaInicio = fila["fechaInicio"] == DBNull.Value
                    ? DateTime.MinValue
                    : Convert.ToDateTime(fila["fechaInicio"]);
                suscripcion.PagadoHasta = fila["pagadoHasta"] == DBNull.Value
                    ? DateTime.MinValue
                    : Convert.ToDateTime(fila["pagadoHasta"]);
                lista.Add(suscripcion);
            }

            return lista;
        }
    }
}
