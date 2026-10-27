using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de la tabla Alerta.
    public class DALAlerta
    {
        /// Lo que hizo el procedimiento RegistrarLecturaFueraDeRango.
        public const string ResultadoNueva = "N";
        public const string ResultadoActualizada = "A";
        public const string ResultadoSinCambios = "S";

        private readonly DALConexion dalCon = new DALConexion();

        /// Registra una lectura por encima de la máxima permitida. Si el sensor
        /// no tiene una alerta abierta se crea una; si ya la tiene, se actualiza
        /// la temperatura alcanzada cuando la lectura nueva es más alta. El
        /// procedimiento lo resuelve dentro de su propia transacción, para que
        /// dos lecturas simultáneas no generen dos alertas. Devuelve el id de la
        /// alerta y en "resultado" una de las constantes Resultado*.
        public int RegistrarLecturaFueraDeRango(int idSensor, double temperatura, string estadoInicial,
            out string resultado)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSensor", idSensor),
                ParametroTemperatura("@Temperatura", temperatura),
                new SqlParameter("@EstadoInicial", estadoInicial)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("RegistrarLecturaFueraDeRango", parametros);
            if (tabla.Rows.Count == 0)
            {
                resultado = ResultadoSinCambios;
                return 0;
            }

            resultado = Convert.ToString(tabla.Rows[0]["resultado"]);
            return Convert.ToInt32(tabla.Rows[0]["id_alerta"]);
        }

        /// CU-007-043: cambia el estado de la alerta y guarda las observaciones.
        /// Devuelve false si la alerta ya no estaba abierta (por ejemplo, otro
        /// técnico la cerró mientras tanto).
        public bool Procesar(int idAlerta, string estado, string observaciones)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdAlerta", idAlerta),
                new SqlParameter("@Estado", estado),
                new SqlParameter("@Observaciones", (object)observaciones ?? DBNull.Value)
            };

            return dalCon.EjecutarYTraerId("ProcesarAlerta", parametros) > 0;
        }

        /// Todas las alertas, de la más reciente a la más antigua.
        public List<BEAlerta> TraerAlertas()
        {
            return Mapear(dalCon.ConsultaProcAlmacenado("TraerAlertas", null));
        }

        public BEAlerta ObtenerPorId(int idAlerta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdAlerta", idAlerta)
            };

            List<BEAlerta> lista = Mapear(dalCon.ConsultaProcAlmacenado("TraerAlertaPorId", parametros));
            return lista.Count == 0 ? null : lista[0];
        }

        private static SqlParameter ParametroTemperatura(string nombre, double valor)
        {
            SqlParameter parametro = new SqlParameter(nombre, SqlDbType.Float);
            parametro.Value = valor;
            return parametro;
        }

        private static List<BEAlerta> Mapear(DataTable tabla)
        {
            List<BEAlerta> lista = new List<BEAlerta>();

            foreach (DataRow fila in tabla.Rows)
            {
                BEAlerta alerta = new BEAlerta();
                alerta.IdAlerta = Convert.ToInt32(fila["id_alerta"]);
                alerta.IdSensor = fila["id_sensor"] == DBNull.Value ? 0 : Convert.ToInt32(fila["id_sensor"]);
                alerta.FechaHora = fila["fechaHora"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(fila["fechaHora"]);
                alerta.TemperaturaAlcanzada = fila["temperaturaAlcanzada"] == DBNull.Value
                    ? 0d
                    : Convert.ToDouble(fila["temperaturaAlcanzada"]);
                alerta.Estado = fila["estado"] == DBNull.Value ? BEAlerta.EstadoPendiente : Convert.ToString(fila["estado"]);
                alerta.Observaciones = fila["observaciones"] == DBNull.Value ? string.Empty : Convert.ToString(fila["observaciones"]);
                alerta.MacSensor = fila["macAddress"] == DBNull.Value ? "(sin sensor)" : Convert.ToString(fila["macAddress"]);
                alerta.ModeloSensor = fila["modelo"] == DBNull.Value ? null : Convert.ToString(fila["modelo"]);
                alerta.TemperaturaMaximaSensor = fila["temperaturaMaximaPermitida"] == DBNull.Value
                    ? 0d
                    : Convert.ToDouble(fila["temperaturaMaximaPermitida"]);
                lista.Add(alerta);
            }

            return lista;
        }
    }
}
