using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de la tabla Sensor.
    public class DALSensor
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Graba el sensor y devuelve el id generado.
        public int Crear(BESensor sensor)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@MacAddress", sensor.MacAddress),
                new SqlParameter("@Modelo", (object)sensor.Modelo ?? DBNull.Value),
                ParametroTemperatura("@TemperaturaMaxima", sensor.TemperaturaMaxima)
            };

            return dalCon.EjecutarYTraerId("CrearSensor", parametros);
        }

        public void Modificar(BESensor sensor)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSensor", sensor.IdSensor),
                new SqlParameter("@MacAddress", sensor.MacAddress),
                new SqlParameter("@Modelo", (object)sensor.Modelo ?? DBNull.Value),
                ParametroTemperatura("@TemperaturaMaxima", sensor.TemperaturaMaxima)
            };

            dalCon.EjecutarProcAlmacenado("ModificarSensor", parametros);
        }

        /// Baja lógica: marca el sensor como inactivo y se conservan sus alertas.
        public void Baja(int idSensor)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSensor", idSensor)
            };

            dalCon.EjecutarProcAlmacenado("BajaSensor", parametros);
        }

        /// Deshace la baja lógica.
        public void Activar(int idSensor)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSensor", idSensor)
            };

            dalCon.EjecutarProcAlmacenado("ActivarSensor", parametros);
        }

        public List<BESensor> TraerSensores()
        {
            return Mapear(dalCon.ConsultaProcAlmacenado("TraerSensores", null));
        }

        public BESensor ObtenerPorId(int idSensor)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdSensor", idSensor)
            };

            List<BESensor> lista = Mapear(dalCon.ConsultaProcAlmacenado("TraerSensorPorId", parametros));
            return lista.Count == 0 ? null : lista[0];
        }

        /// Sensor activo que tiene asignada la dirección MAC, o null si no hay.
        public BESensor ObtenerActivoPorMac(string macAddress)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@MacAddress", macAddress)
            };

            List<BESensor> lista = Mapear(dalCon.ConsultaProcAlmacenado("TraerSensorActivoPorMac", parametros));
            return lista.Count == 0 ? null : lista[0];
        }

        private static SqlParameter ParametroTemperatura(string nombre, double valor)
        {
            SqlParameter parametro = new SqlParameter(nombre, SqlDbType.Float);
            parametro.Value = valor;
            return parametro;
        }

        private static List<BESensor> Mapear(DataTable tabla)
        {
            List<BESensor> lista = new List<BESensor>();

            foreach (DataRow fila in tabla.Rows)
            {
                BESensor sensor = new BESensor();
                sensor.IdSensor = Convert.ToInt32(fila["id_sensor"]);
                sensor.MacAddress = Convert.ToString(fila["macAddress"]);
                sensor.Modelo = fila["modelo"] == DBNull.Value ? null : Convert.ToString(fila["modelo"]);
                sensor.TemperaturaMaxima = fila["temperaturaMaximaPermitida"] == DBNull.Value
                    ? 0d
                    : Convert.ToDouble(fila["temperaturaMaximaPermitida"]);
                sensor.Activo = fila["activo"] != DBNull.Value && Convert.ToBoolean(fila["activo"]);
                sensor.UltimaAlerta = fila["ultimaAlerta"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(fila["ultimaAlerta"]);
                sensor.AlertasAbiertas = fila["alertasAbiertas"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(fila["alertasAbiertas"]);
                lista.Add(sensor);
            }

            return lista;
        }
    }
}
