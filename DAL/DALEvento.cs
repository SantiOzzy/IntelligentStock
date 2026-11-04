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

        /// Eventos todavía activos con fecha anterior al límite: los que la
        /// política de la bitácora tiene que pasar a eliminados.
        public List<BEEvento> TraerEventosVencidos(DateTime limite)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Limite", limite)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerEventosVencidos", parametros);
            return Mapear(tabla);
        }

        /// Eliminación lógica (activo = 0) de los eventos anteriores al límite.
        /// hastaId acota la actualización a los que ya se archivaron. Devuelve
        /// cuántos eventos se marcaron.
        public int EliminarEventosLogicamente(DateTime limite, int hastaId)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Limite", limite),
                new SqlParameter("@HastaId", hastaId)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("EliminarEventosLogicamente", parametros);
            return tabla.Rows.Count == 0 ? 0 : Convert.ToInt32(tabla.Rows[0][0]);
        }

        /// Ultimos N eventos registrados, del mas reciente al mas antiguo. Sin
        /// incluirEliminados, solo los activos (los de los últimos 30 días).
        public List<BEEvento> TraerUltimosEventos(int cantidad, bool incluirEliminados)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Cantidad", cantidad),
                new SqlParameter("@IncluirEliminados", incluirEliminados)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerUltimosEventos", parametros);
            return Mapear(tabla);
        }

        /// Ultimos N eventos de una acción determinada (por ejemplo, los ajustes
        /// de stock), activos o eliminados: son datos que el sistema vuelve a leer.
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

        /// Eventos de una acción desde una fecha, del más viejo al más nuevo,
        /// activos o eliminados (con "Venta" es el historial de la predicción).
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

        public List<BEEvento> FiltrarEventos(DateTime fechaInicio, DateTime fechaFin, bool incluirEliminados)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@FechaInicio", fechaInicio),
                new SqlParameter("@FechaFin", fechaFin),
                new SqlParameter("@IncluirEliminados", incluirEliminados)
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

                evento.Activo = !fila.Table.Columns.Contains("activo") || Convert.ToBoolean(fila["activo"]);

                lista.Add(evento);
            }

            return lista;
        }
    }
}
