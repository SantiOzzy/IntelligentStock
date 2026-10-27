using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BE;

namespace DAL
{
    /// Acceso a datos de los dígitos verificadores.
    ///
    /// La tabla DV guarda el digito de cada tabla completa (DVH y DVV).
    /// La tabla auxiliar DVH_Fila guarda el digito horizontal de cada registro
    /// y DVV_Columna el digito vertical de cada columna, lo que permite
    /// determinar la fila y la columna exacta donde esta la inconsistencia.
    /// La tabla Usuario ademas conserva su propio DVH en la columna homonima.
    public class DALDigitoVerificador
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// La estructura de las tablas no cambia mientras el sistema funciona:
        /// qué tablas existen y cuál es su clave primaria se consulta una sola
        /// vez y queda en memoria, en lugar de preguntarlo en cada verificación.
        private static readonly HashSet<string> tablasExistentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<string>> clavesPrimarias =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly object candadoEstructura = new object();

        #region Dígito verificador de la tabla completa

        /// Devuelve el listado de tablas sometidas a control de integridad.
        public List<string> ObtenerTablasControladas()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerTablasDV", null);

            List<string> tablas = new List<string>();
            foreach (DataRow fila in tabla.Rows)
            {
                tablas.Add(Convert.ToString(fila["tabla"]));
            }
            return tablas;
        }

        public ObjetoDV ObtenerDV(string tabla)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Tabla", tabla)
            };

            DataTable resultado = dalCon.ConsultaProcAlmacenado("TraerDVTabla", parametros);
            if (resultado.Rows.Count == 0)
            {
                return null;
            }

            DataRow fila = resultado.Rows[0];
            return new ObjetoDV(
                Convert.ToString(fila["tabla"]),
                Convert.ToString(fila["DVH"]),
                Convert.ToString(fila["DVV"]));
        }

        public void GuardarDVHTabla(string tabla, string dvh)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DVH", dvh),
                new SqlParameter("@Tabla", tabla)
            };

            dalCon.EjecutarProcAlmacenado("RecalcularDVHTabla", parametros);
        }

        public void GuardarDVVTabla(string tabla, string dvv)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@DVV", dvv),
                new SqlParameter("@Tabla", tabla)
            };

            dalCon.EjecutarProcAlmacenado("RecalcularDVVTabla", parametros);
        }

        #endregion

        #region Dígito verificador horizontal por fila

        /// Devuelve el DVH almacenado de cada fila, indexado por clave primaria.
        public Dictionary<string, string> ObtenerDVHFilas(string tabla)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Tabla", tabla)
            };

            DataTable resultado = dalCon.ConsultaProcAlmacenado("TraerDVHFilas", parametros);

            Dictionary<string, string> digitos = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (DataRow fila in resultado.Rows)
            {
                digitos[Convert.ToString(fila["clave"])] = Convert.ToString(fila["DVH"]);
            }
            return digitos;
        }

        /// Deja guardados los digitos horizontales de una tabla. Solo se graban
        /// las diferencias con lo que ya estaba (en un alta, una fila nueva; en
        /// una modificación, la fila que cambió), en vez de borrar y volver a
        /// insertar todas las filas en cada operación.
        public void GuardarDVHFilas(string tabla, Dictionary<string, string> digitos)
        {
            Dictionary<string, string> anteriores = ObtenerDVHFilas(tabla);
            List<KeyValuePair<string, SqlParameter[]>> cambios = new List<KeyValuePair<string, SqlParameter[]>>();

            // Primero las bajas, después las modificaciones y las altas.
            foreach (KeyValuePair<string, string> par in anteriores)
            {
                if (!digitos.ContainsKey(par.Key))
                {
                    cambios.Add(Sentencia("DELETE FROM DVH_Fila WHERE tabla = @Tabla AND clave = @Clave",
                        tabla, "@Clave", par.Key, null));
                }
            }

            foreach (KeyValuePair<string, string> par in digitos)
            {
                string anterior;
                if (!anteriores.TryGetValue(par.Key, out anterior))
                {
                    cambios.Add(Sentencia("INSERT INTO DVH_Fila (tabla, clave, DVH) VALUES (@Tabla, @Clave, @DVH)",
                        tabla, "@Clave", par.Key, par.Value));
                }
                else if (anterior != par.Value)
                {
                    cambios.Add(Sentencia("UPDATE DVH_Fila SET DVH = @DVH WHERE tabla = @Tabla AND clave = @Clave",
                        tabla, "@Clave", par.Key, par.Value));
                }
            }

            dalCon.EjecutarSentencias(cambios);
        }

        #endregion

        #region Dígito verificador vertical por columna

        /// Devuelve el DVV almacenado de cada columna, indexado por nombre de columna.
        public Dictionary<string, string> ObtenerDVVColumnas(string tabla)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Tabla", tabla)
            };

            DataTable resultado = dalCon.ConsultaProcAlmacenado("TraerDVVColumnas", parametros);

            Dictionary<string, string> digitos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow fila in resultado.Rows)
            {
                digitos[Convert.ToString(fila["columna"])] = Convert.ToString(fila["DVV"]);
            }
            return digitos;
        }

        /// Deja guardados los digitos verticales de una tabla, grabando solo
        /// las columnas cuyo digito cambió.
        public void GuardarDVVColumnas(string tabla, Dictionary<string, string> digitos)
        {
            Dictionary<string, string> anteriores = ObtenerDVVColumnas(tabla);
            List<KeyValuePair<string, SqlParameter[]>> cambios = new List<KeyValuePair<string, SqlParameter[]>>();

            foreach (KeyValuePair<string, string> par in anteriores)
            {
                if (!digitos.ContainsKey(par.Key))
                {
                    cambios.Add(Sentencia("DELETE FROM DVV_Columna WHERE tabla = @Tabla AND columna = @Columna",
                        tabla, "@Columna", par.Key, null, "@DVV"));
                }
            }

            foreach (KeyValuePair<string, string> par in digitos)
            {
                string anterior;
                if (!anteriores.TryGetValue(par.Key, out anterior))
                {
                    cambios.Add(Sentencia("INSERT INTO DVV_Columna (tabla, columna, DVV) VALUES (@Tabla, @Columna, @DVV)",
                        tabla, "@Columna", par.Key, par.Value, "@DVV"));
                }
                else if (anterior != par.Value)
                {
                    cambios.Add(Sentencia("UPDATE DVV_Columna SET DVV = @DVV WHERE tabla = @Tabla AND columna = @Columna",
                        tabla, "@Columna", par.Key, par.Value, "@DVV"));
                }
            }

            dalCon.EjecutarSentencias(cambios);
        }

        private static KeyValuePair<string, SqlParameter[]> Sentencia(string sql, string tabla, string nombreClave,
                                                                       string clave, string digito, string nombreDigito = "@DVH")
        {
            List<SqlParameter> parametros = new List<SqlParameter>();
            parametros.Add(new SqlParameter("@Tabla", tabla));
            parametros.Add(new SqlParameter(nombreClave, clave));
            if (digito != null)
            {
                parametros.Add(new SqlParameter(nombreDigito, digito));
            }
            return new KeyValuePair<string, SqlParameter[]>(sql, parametros.ToArray());
        }

        #endregion

        #region Lectura del contenido de las tablas

        public DataTable ObtenerContenido(string tabla)
        {
            List<string> clave = ObtenerClavePrimaria(tabla);

            string orden = null;
            if (clave.Count > 0)
            {
                orden = "[" + string.Join("], [", clave.ToArray()) + "]";
            }

            return dalCon.TraerTablaOrdenada(tabla, orden);
        }

        public List<string> ObtenerClavePrimaria(string tabla)
        {
            List<string> clave;
            lock (candadoEstructura)
            {
                if (clavesPrimarias.TryGetValue(tabla, out clave))
                {
                    return new List<string>(clave);
                }
            }

            clave = dalCon.ObtenerClavePrimaria(tabla);
            lock (candadoEstructura)
            {
                clavesPrimarias[tabla] = clave;
            }
            return new List<string>(clave);
        }

        public bool ExisteTabla(string tabla)
        {
            lock (candadoEstructura)
            {
                if (tablasExistentes.Contains(tabla))
                {
                    return true;
                }
            }

            // Solo se recuerda que existe: si no existiera, se vuelve a
            // preguntar la próxima vez (por ejemplo, después de un restore).
            bool existe = dalCon.ExisteTabla(tabla);
            if (existe)
            {
                lock (candadoEstructura)
                {
                    tablasExistentes.Add(tabla);
                }
            }
            return existe;
        }

        #endregion
    }
}
