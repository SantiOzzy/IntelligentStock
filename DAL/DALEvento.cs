using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de la bitacora de eventos (tabla Evento).
    public class DALEvento
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// R4 - Registrar en bitacora de eventos.
        public void RegistrarEvento(BEEvento evento)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DniUsuario", (object)evento.DniUsuario ?? DBNull.Value),
                new SqlParameter("@Accion", evento.Accion),
                new SqlParameter("@Descripcion", (object)evento.Descripcion ?? DBNull.Value),
                new SqlParameter("@Criticidad", evento.Criticidad.ToString())
            };

            dalCon.EjecutarProcAlmacenado("RegistrarEvento", parametros);
        }

        /// Cantidad total de eventos en la bitácora.
        public int ContarEventos()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("ContarEventos", null);
            return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0][0]);
        }

        /// Aplica la política de retención: conserva solo los más recientes.
        public void PurgarEventosAntiguos(int cantidadARetener)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@CantidadARetener", cantidadARetener)
            };

            dalCon.EjecutarProcAlmacenado("PurgarEventosAntiguos", parametros);
        }

        /// Ultimos N eventos registrados, del mas reciente al mas antiguo.
        public List<BEEvento> TraerUltimosEventos(int cantidad)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Cantidad", cantidad)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerUltimosEventos", parametros);
            return Mapear(tabla);
        }

        /// Ultimos N eventos de una acción determinada (por ejemplo, los ajustes de stock).
        public List<BEEvento> TraerUltimosEventosPorAccion(string accion, int cantidad)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@Cantidad", cantidad)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerUltimosEventosPorAccion", parametros);
            return Mapear(tabla);
        }

        /// Eventos de una acción desde una fecha, del más viejo al más nuevo.
        public List<BEEvento> TraerEventosPorAccionDesde(string accion, DateTime desde)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Accion", accion),
                new SqlParameter("@Desde", desde)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerEventosPorAccionDesde", parametros);
            return Mapear(tabla);
        }

        public List<BEEvento> TraerEventos()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerEventos", null);
            return Mapear(tabla);
        }

        public List<BEEvento> FiltrarEventos(DateTime fechaInicio, DateTime fechaFin)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@FechaInicio", fechaInicio),
                new SqlParameter("@FechaFin", fechaFin)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("FiltrarEventos", parametros);
            return Mapear(tabla);
        }

        private static List<BEEvento> Mapear(DataTable tabla)
        {
            List<BEEvento> lista = new List<BEEvento>();

            foreach (DataRow fila in tabla.Rows)
            {
                BEEvento evento = new BEEvento();
                evento.IdEvento = Convert.ToInt32(fila["id_evento"]);
                evento.DniUsuario = fila["dni_usuario"] == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(fila["dni_usuario"]);
                evento.NombreUsuario = Convert.ToString(fila["nombreUsuario"]);
                evento.FechaHora = Convert.ToDateTime(fila["fecha"]);
                evento.Accion = Convert.ToString(fila["accion"]);
                evento.Descripcion = Convert.ToString(fila["descripcion"]);

                CriticidadEvento criticidad;
                evento.Criticidad = fila.Table.Columns.Contains("criticidad") &&
                                     Enum.TryParse(Convert.ToString(fila["criticidad"]), true, out criticidad)
                    ? criticidad
                    : CriticidadEvento.Informativa;

                lista.Add(evento);
            }

            return lista;
        }
    }
}
