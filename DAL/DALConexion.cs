using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Servicios;

namespace DAL
{
    /// Unico punto de acceso al motor de base de datos. Todas las clases DAL
    /// resuelven sus operaciones a traves de esta clase, de manera que la
    /// cadena de conexion y el manejo de transacciones queden centralizados.
    public class DALConexion
    {
        private const string CadenaPorDefecto =
            "Data Source=localhost;Initial Catalog=IntelligentStock;Integrated Security=True";

        private const string CadenaMasterPorDefecto =
            "Data Source=localhost;Initial Catalog=master;Integrated Security=True";

        public static string CadenaConexion
        {
            get
            {
                ConnectionStringSettings config =
                    ConfigurationManager.ConnectionStrings["IntelligentStockConnection"];
                return config == null ? CadenaPorDefecto : config.ConnectionString;
            }
        }

        public static string CadenaConexionMaster
        {
            get
            {
                ConnectionStringSettings config =
                    ConfigurationManager.ConnectionStrings["IntelligentStockMasterConnection"];
                return config == null ? CadenaMasterPorDefecto : config.ConnectionString;
            }
        }

        public static string NombreBaseDeDatos
        {
            get
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(CadenaConexion);
                return string.IsNullOrEmpty(builder.InitialCatalog) ? "IntelligentStock" : builder.InitialCatalog;
            }
        }

        #region Procedimientos almacenados

        /// Ejecuta un procedimiento almacenado de consulta y devuelve su resultado.
        public DataTable ConsultaProcAlmacenado(string nombreProc, SqlParameter[] parametros)
        {
            DataTable tabla = new DataTable();
            try
            {
                using (SqlConnection con = new SqlConnection(CadenaConexion))
                using (SqlCommand command = new SqlCommand(nombreProc, con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    AgregarParametros(command, parametros);
                    con.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(tabla);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
            return tabla;
        }

        /// Ejecuta un procedimiento almacenado de escritura dentro de una transaccion.
        public void EjecutarProcAlmacenado(string nombreProc, SqlParameter[] parametros)
        {
            using (SqlConnection con = new SqlConnection(CadenaConexion))
            {
                SqlTransaction tran = null;
                try
                {
                    con.Open();
                    tran = con.BeginTransaction();
                    using (SqlCommand command = new SqlCommand(nombreProc, con, tran))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        AgregarParametros(command, parametros);
                        command.ExecuteNonQuery();
                    }
                    tran.Commit();
                }
                catch (SqlException ex)
                {
                    Revertir(tran);
                    throw Traducir(ex);
                }
                catch
                {
                    Revertir(tran);
                    throw;
                }
            }
        }

        /// Ejecuta un procedimiento almacenado SIN transaccion. Es obligatorio para
        /// las sentencias que SQL Server no admite dentro de una transaccion, como
        /// BACKUP DATABASE.
        public void EjecutarProcAlmacenadoSinTransaccion(string nombreProc, SqlParameter[] parametros)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CadenaConexion))
                using (SqlCommand command = new SqlCommand(nombreProc, con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 0;
                    AgregarParametros(command, parametros);
                    con.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
        }

        /// Ejecuta un procedimiento almacenado que devuelve el identificador generado.
        public int EjecutarYTraerId(string nombreProc, SqlParameter[] parametros)
        {
            using (SqlConnection con = new SqlConnection(CadenaConexion))
            {
                SqlTransaction tran = null;
                try
                {
                    con.Open();
                    tran = con.BeginTransaction();
                    int idGenerado;
                    using (SqlCommand command = new SqlCommand(nombreProc, con, tran))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        AgregarParametros(command, parametros);
                        object resultado = command.ExecuteScalar();
                        idGenerado = resultado == null || resultado == DBNull.Value
                            ? 0
                            : Convert.ToInt32(resultado);
                    }
                    tran.Commit();
                    return idGenerado;
                }
                catch (SqlException ex)
                {
                    Revertir(tran);
                    throw Traducir(ex);
                }
                catch
                {
                    Revertir(tran);
                    throw;
                }
            }
        }

        #endregion

        #region Comandos directos

        /// Ejecuta una sentencia SQL directa sobre la base de la aplicacion.
        public void EjecutarComando(string comando)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CadenaConexion))
                using (SqlCommand command = new SqlCommand(comando, con))
                {
                    command.CommandTimeout = 0;
                    con.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
        }

        /// Ejecuta una sentencia SQL conectandose a la base master (backup y restore).
        public void EjecutarComandoEnMaster(string comando)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CadenaConexionMaster))
                using (SqlCommand command = new SqlCommand(comando, con))
                {
                    command.CommandTimeout = 0;
                    con.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
        }

        /// Ejecuta varios lotes (los de un script separado por GO) uno detrás de
        /// otro sobre una misma conexión a master.
        public void EjecutarLotesEnMaster(List<string> lotes)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CadenaConexionMaster))
                {
                    con.Open();
                    foreach (string lote in lotes)
                    {
                        if (lote == null || lote.Trim().Length == 0)
                        {
                            continue;
                        }

                        using (SqlCommand command = new SqlCommand(lote, con))
                        {
                            command.CommandTimeout = 0;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
        }

        /// Ejecuta una consulta SQL parametrizada y devuelve el resultado.
        public DataTable ConsultaSQL(string consulta, SqlParameter[] parametros)
        {
            DataTable tabla = new DataTable();
            try
            {
                using (SqlConnection con = new SqlConnection(CadenaConexion))
                using (SqlCommand command = new SqlCommand(consulta, con))
                {
                    AgregarParametros(command, parametros);
                    con.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(tabla);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
            return tabla;
        }

        /// Ejecuta la misma sentencia parametrizada una vez por cada juego de
        /// parametros, dentro de una unica transaccion.
        public void EjecutarLote(string sentencia, List<SqlParameter[]> lotes)
        {
            if (lotes == null || lotes.Count == 0)
            {
                return;
            }

            List<KeyValuePair<string, SqlParameter[]>> sentencias = new List<KeyValuePair<string, SqlParameter[]>>();
            foreach (SqlParameter[] parametros in lotes)
            {
                sentencias.Add(new KeyValuePair<string, SqlParameter[]>(sentencia, parametros));
            }
            EjecutarSentencias(sentencias);
        }

        /// Ejecuta varias sentencias parametrizadas (pueden ser distintas)
        /// dentro de una unica transaccion. Se utiliza para actualizar los
        /// dígitos verificadores de las filas y columnas que cambiaron.
        public void EjecutarSentencias(List<KeyValuePair<string, SqlParameter[]>> sentencias)
        {
            if (sentencias == null || sentencias.Count == 0)
            {
                return;
            }

            using (SqlConnection con = new SqlConnection(CadenaConexion))
            {
                SqlTransaction tran = null;
                try
                {
                    con.Open();
                    tran = con.BeginTransaction();

                    foreach (KeyValuePair<string, SqlParameter[]> sentencia in sentencias)
                    {
                        using (SqlCommand command = new SqlCommand(sentencia.Key, con, tran))
                        {
                            AgregarParametros(command, sentencia.Value);
                            command.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
                catch (SqlException ex)
                {
                    Revertir(tran);
                    throw Traducir(ex);
                }
                catch
                {
                    Revertir(tran);
                    throw;
                }
            }
        }

        #endregion

        #region Lectura de tablas para el dígito verificador

        /// Devuelve el contenido completo de una tabla.
        public DataTable TraerTabla(string tabla)
        {
            return TraerTablaOrdenada(tabla, null);
        }

        /// Devuelve el contenido de una tabla con un orden estable. El orden es
        /// imprescindible para que el dígito verificador sea reproducible.
        public DataTable TraerTablaOrdenada(string tabla, string ordenarPor)
        {
            ValidarNombreDeTabla(tabla);

            string consulta = "SELECT * FROM [" + tabla + "]";
            if (!string.IsNullOrEmpty(ordenarPor))
            {
                consulta += " ORDER BY " + ordenarPor;
            }
            return ConsultaSQL(consulta, null);
        }

        /// Devuelve los nombres de las columnas que forman la clave primaria de la tabla.
        public List<string> ObtenerClavePrimaria(string tabla)
        {
            ValidarNombreDeTabla(tabla);

            const string consulta =
                "SELECT KU.COLUMN_NAME " +
                "FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC " +
                "INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KU " +
                "  ON TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME " +
                "WHERE TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND TC.TABLE_NAME = @Tabla " +
                "ORDER BY KU.ORDINAL_POSITION";

            DataTable resultado = ConsultaSQL(consulta, new SqlParameter[]
            {
                new SqlParameter("@Tabla", tabla)
            });

            List<string> columnas = new List<string>();
            foreach (DataRow fila in resultado.Rows)
            {
                columnas.Add(Convert.ToString(fila[0]));
            }
            return columnas;
        }

        /// Indica si la tabla existe en la base de datos.
        public bool ExisteTabla(string tabla)
        {
            const string consulta =
                "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @Tabla";

            DataTable resultado = ConsultaSQL(consulta, new SqlParameter[]
            {
                new SqlParameter("@Tabla", tabla)
            });

            return resultado.Rows.Count > 0 && Convert.ToInt32(resultado.Rows[0][0]) > 0;
        }

        #endregion

        #region Utilitarios

        private static void AgregarParametros(SqlCommand command, SqlParameter[] parametros)
        {
            if (parametros == null)
            {
                return;
            }

            foreach (SqlParameter parametro in parametros)
            {
                if (parametro.Value == null)
                {
                    parametro.Value = DBNull.Value;
                }
                command.Parameters.Add(parametro);
            }
        }

        private static void Revertir(SqlTransaction tran)
        {
            if (tran == null)
            {
                return;
            }
            try
            {
                tran.Rollback();
            }
            catch
            {
                // La transaccion ya fue cerrada por el motor.
            }
        }

        /// Traduce los errores del motor a los codigos de la administracion
        /// centralizada de errores.
        private static Exception Traducir(SqlException ex)
        {
            switch (ex.Number)
            {
                case 2627:
                case 2601:
                    return new ApplicationException(AdministradorErrores.Mensaje("ERR010"), ex);

                case 547:
                    return new ApplicationException(AdministradorErrores.Mensaje("ERR008"), ex);

                // 3201 y 3013: el motor no pudo abrir el archivo de backup, casi
                // siempre porque la cuenta del servicio de SQL Server no tiene
                // permiso de escritura sobre la carpeta elegida.
                // 15105: la ruta no es accesible para el motor.
                case 3201:
                case 3013:
                case 15105:
                    return new ApplicationException(
                        AdministradorErrores.Mensaje("ERR022", "Detalle del motor: " + ex.Message), ex);

                // 4060 / 18456: la base no existe o el usuario no tiene acceso.
                case 4060:
                case 18456:
                    return new ApplicationException(
                        AdministradorErrores.Mensaje("ERR001", "Detalle del motor: " + ex.Message), ex);

                default:
                    // Solo los errores de red o de conexión son "no hay conexión";
                    // el resto (un procedimiento que falta, un bloqueo, un dato que
                    // no entra) es un problema al ejecutar la transacción.
                    string codigo = EsErrorDeConexion(ex) ? "ERR001" : "ERR008";
                    return new ApplicationException(
                        AdministradorErrores.Mensaje(codigo, "Detalle del motor: " + ex.Message), ex);
            }
        }

        /// Errores que indican que no se pudo llegar al motor: tiempo de espera
        /// agotado, servidor inexistente o caído, conexión cortada.
        private static bool EsErrorDeConexion(SqlException ex)
        {
            switch (ex.Number)
            {
                case -2:     // tiempo de espera agotado
                case -1:     // error al conectar
                case 2:      // no se encontró el servidor
                case 40:     // no se pudo abrir la conexión
                case 53:     // ruta de red no encontrada
                case 64:     // la conexión se cortó
                case 121:    // tiempo de espera del semáforo
                case 233:    // no hay proceso en el otro extremo
                case 10053:
                case 10054:
                case 10060:
                case 10061:
                    return true;
                default:
                    // Severidad 20 o más: error fatal que cierra la conexión.
                    return ex.Class >= 20;
            }
        }

        /// Evita cualquier intento de inyeccion en los lugares donde el nombre
        /// de la tabla se concatena a la consulta.
        private static void ValidarNombreDeTabla(string tabla)
        {
            if (string.IsNullOrEmpty(tabla))
            {
                throw new ArgumentException("Debe indicar el nombre de la tabla.");
            }

            foreach (char caracter in tabla)
            {
                if (!char.IsLetterOrDigit(caracter) && caracter != '_')
                {
                    throw new ArgumentException("El nombre de tabla " + tabla + " no es valido.");
                }
            }
        }

        #endregion
    }
}
