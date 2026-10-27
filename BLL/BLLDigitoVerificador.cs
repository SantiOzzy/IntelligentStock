using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// R3 - Actualizar dígito verificador / R4 - Verificar dígito verificador.
    ///
    /// Esquema utilizado:
    ///   - DVH de fila: SHA-256 de la concatenacion de todas las columnas del
    ///     registro (excepto las columnas de control). Se guarda en DVH_Fila y,
    ///     para la tabla Usuario, tambien en su columna DVH.
    ///   - DVV de columna: SHA-256 de la concatenacion de todos los valores de
    ///     esa columna. Se guarda en DVV_Columna.
    ///   - DV de la tabla: DVH = SHA-256 de la concatenacion de los DVH de las
    ///     filas; DVV = SHA-256 de la concatenacion de los DVV de las columnas.
    ///     Se guardan en la tabla DV.
    ///
    /// Cruzando las filas con DVH incorrecto contra las columnas con DVV
    /// incorrecto se obtiene la celda exacta que fue alterada.
    ///
    /// Las operaciones del sistema escriben a través de EjecutarControlado:
    /// primero se verifica que las tablas que se van a tocar estén íntegras
    /// (R4; si no, la operación se detiene con #ERR007), después se graba y
    /// recién ahí se recalculan sus dígitos (R3). Si se recalculara sin
    /// verificar antes, cualquier alta o modificación normal sobre una tabla
    /// "blanquearía" una alteración hecha por fuera del sistema en otra fila.
    ///
    /// Los dígitos solo se recalculan sin verificar en dos casos, los dos
    /// explícitos y registrados en la bitácora: la reparación que hace el
    /// administrador (CU-003-019) y el restore de la base (CU-003-018).
    public class BLLDigitoVerificador
    {
        /// Columnas que no participan del calculo por ser de control.
        private static readonly string[] ColumnasDeControl = new string[] { "DVH" };

        /// Tablas sometidas al control de integridad. La lista está fija acá y
        /// no se toma de las filas de la tabla DV: si alguien borrara la fila de
        /// una tabla en DV, esa tabla dejaría de controlarse sin que nadie se
        /// entere. Evento, DV, DVH_Fila, DVV_Columna y TokenRecuperacion quedan
        /// afuera a propósito: la bitácora y los enlaces de recuperación cambian
        /// todo el tiempo, y las otras son justamente las tablas de control.
        private static readonly string[] TablasControladas = new string[]
        {
            "Usuario", "Rol", "Familia", "RolFamilia", "RelacionFamilia",
            "Proveedor", "Insumo", "OrdenCompra", "DetalleOrdenCompra",
            "Receta", "DetalleReceta", "Sensor", "Alerta", "Suscripcion"
        };

        private readonly DALDigitoVerificador dalDV = new DALDigitoVerificador();
        private readonly DALUsuario dalUsuario = new DALUsuario();

        /// Un recálculo o una verificación por vez. Desde que existe el servicio
        /// web de telemetría puede haber escrituras simultáneas sin usuario de
        /// por medio (dos sensores, o un sensor y un técnico): si dos recálculos
        /// de la misma tabla se cruzan, uno puede pisar al otro con dígitos
        /// viejos o chocar al insertar las filas de DVH_Fila.
        private static readonly object candado = new object();

        #region Recalculo

        /// Recalcula y persiste todos los dígitos verificadores de una tabla,
        /// aceptando como buenos sus datos actuales. Es privado: las
        /// operaciones pasan por EjecutarControlado, que verifica antes, y la
        /// reparación y el restore usan RecalcularBaseDeDatos.
        private void RecalcularTabla(string tabla)
        {
            lock (candado)
            {
                RecalcularTablaSinBloqueo(tabla);
            }
        }

        private void RecalcularTablaSinBloqueo(string tabla)
        {
            if (string.IsNullOrEmpty(tabla) || !dalDV.ExisteTabla(tabla))
            {
                return;
            }

            DataTable contenido = dalDV.ObtenerContenido(tabla);
            List<string> clavePrimaria = dalDV.ObtenerClavePrimaria(tabla);

            Dictionary<string, string> dvhFilas = CalcularDVHFilas(contenido, clavePrimaria);
            Dictionary<string, string> dvvColumnas = CalcularDVVColumnas(contenido);

            dalDV.GuardarDVHFilas(tabla, dvhFilas);
            dalDV.GuardarDVVColumnas(tabla, dvvColumnas);
            dalDV.GuardarDVHTabla(tabla, ConsolidarDVH(dvhFilas));
            dalDV.GuardarDVVTabla(tabla, ConsolidarDVV(contenido, dvvColumnas));

            // La tabla Usuario conserva ademas el DVH dentro de cada registro.
            if (string.Equals(tabla, "Usuario", StringComparison.OrdinalIgnoreCase))
            {
                GuardarDVHEnUsuarios(contenido, clavePrimaria, dvhFilas);
            }
        }

        /// Recalcula los dígitos verificadores de todas las tablas controladas.
        /// Acepta como buenos los datos actuales: solo lo usan la reparación del
        /// administrador y el restore.
        public void RecalcularBaseDeDatos()
        {
            foreach (string tabla in ObtenerTablasAControlar())
            {
                RecalcularTabla(tabla);
            }
        }

        /// R4 + R10 + R3 de una operación del sistema, bajo el mismo candado:
        /// verifica las tablas que se van a modificar, ejecuta la escritura y
        /// recalcula sus dígitos. Si alguna tabla no está íntegra no se escribe
        /// nada y se informa #ERR007.
        public void EjecutarControlado(Action escritura, params string[] tablas)
        {
            EjecutarControlado<bool>(delegate { escritura(); return true; }, null, tablas);
        }

        /// Igual que el anterior, para escrituras que devuelven un valor (por
        /// ejemplo, el id generado).
        public T EjecutarControlado<T>(Func<T> escritura, params string[] tablas)
        {
            return EjecutarControlado<T>(escritura, null, tablas);
        }

        /// Igual que el anterior; huboCambios dice, a partir del resultado, si
        /// la escritura cambió algo (si no, no hace falta recalcular). Lo usa la
        /// telemetría: un sensor que sigue caliente manda lecturas que no
        /// cambian la alerta abierta.
        public T EjecutarControlado<T>(Func<T> escritura, Func<T, bool> huboCambios, params string[] tablas)
        {
            lock (candado)
            {
                VerificarAntesDeModificarSinBloqueo(tablas);

                T resultado;
                try
                {
                    resultado = escritura();
                }
                catch
                {
                    // La escritura pudo haber quedado a medias. Las tablas
                    // estaban íntegras un instante antes, así que recalcular
                    // no tapa ninguna alteración y evita una falsa alarma.
                    try
                    {
                        RecalcularTablas(tablas);
                    }
                    catch (Exception)
                    {
                        // Se informa el error original, no el del recálculo.
                    }
                    throw;
                }

                if (huboCambios == null || huboCambios(resultado))
                {
                    RecalcularDespuesDeGrabar(tablas);
                }
                return resultado;
            }
        }

        /// Si el recálculo falla después de una escritura ya grabada (por
        /// ejemplo, por un corte momentáneo con la base), se reintenta una vez.
        /// Si vuelve a fallar, queda un evento propio en la bitácora, para que
        /// el administrador no lo confunda con una alteración.
        private void RecalcularDespuesDeGrabar(string[] tablas)
        {
            try
            {
                RecalcularTablas(tablas);
                return;
            }
            catch (Exception)
            {
                System.Threading.Thread.Sleep(500);
            }

            try
            {
                RecalcularTablas(tablas);
            }
            catch (Exception)
            {
                try
                {
                    new BLLEvento().RegistrarDelUsuarioActual("Recálculo de dígitos fallido",
                        "Los datos de " + string.Join(", ", tablas) + " se grabaron, pero no se pudieron actualizar " +
                        "sus dígitos verificadores. Las inconsistencias que aparezcan en esas tablas se deben a " +
                        "esta falla: hay que recalcularlos desde Reparar dígito verificador. [#ERR007]",
                        CriticidadEvento.Grave);
                }
                catch (Exception)
                {
                }

                throw new ApplicationException(AdministradorErrores.Mensaje("ERR007",
                    "Los datos se grabaron, pero no se pudieron actualizar los dígitos verificadores de " +
                    string.Join(", ", tablas) + ". Un administrador tiene que recalcularlos desde Reparar " +
                    "dígito verificador."));
            }
        }

        /// Verificación previa para las operaciones que, antes de grabar,
        /// hacen algo que no se puede deshacer (por ejemplo, cobrar con la
        /// pasarela de pagos): conviene saber antes si la grabación va a poder
        /// hacerse. EjecutarControlado vuelve a verificar al grabar.
        public void VerificarAntesDeModificar(params string[] tablas)
        {
            lock (candado)
            {
                VerificarAntesDeModificarSinBloqueo(tablas);
            }
        }

        private void VerificarAntesDeModificarSinBloqueo(string[] tablas)
        {
            foreach (string tabla in tablas)
            {
                if (string.IsNullOrEmpty(tabla) || !dalDV.ExisteTabla(tabla))
                {
                    continue;
                }

                List<Inconsistencia> inconsistencias = VerificarTablaSinBloqueo(tabla);
                if (inconsistencias.Count == 0)
                {
                    continue;
                }

                // Una vez cada 10 minutos por tabla: con una tabla alterada,
                // cada intento (o cada lectura de un sensor) volvería a
                // registrarlo y la política de retención terminaría borrando
                // de la bitácora los eventos que sirven para investigar.
                if (LimitadorDeEventos.Permitir("dv-detenida:" + tabla, TimeSpan.FromMinutes(10)))
                {
                    new BLLEvento().RegistrarDelUsuarioActual("Operación detenida por inconsistencia",
                        "Se detuvo una operación sobre la tabla " + tabla + " porque tiene " + inconsistencias.Count +
                        (inconsistencias.Count == 1 ? " inconsistencia" : " inconsistencias") +
                        " de dígito verificador. Primera: " + inconsistencias[0].Descripcion + " Las operaciones " +
                        "detenidas siguientes sobre esa tabla no se registran durante 10 minutos. [#ERR007]",
                        CriticidadEvento.Grave);
                }

                throw new ApplicationException(AdministradorErrores.Mensaje("ERR007",
                    "Se detuvo la operación: los datos de la tabla " + tabla + " no coinciden con sus " +
                    "dígitos verificadores (pudieron haberse modificado por fuera del sistema). Un " +
                    "administrador tiene que revisarlo desde Reparar dígito verificador."));
            }
        }

        private void RecalcularTablas(string[] tablas)
        {
            foreach (string tabla in tablas)
            {
                RecalcularTablaSinBloqueo(tabla);
            }
        }

        private static List<string> ObtenerTablasAControlar()
        {
            return new List<string>(TablasControladas);
        }

        #endregion

        #region Verificacion

        /// Verifica la integridad de todas las tablas controladas y devuelve
        /// el detalle de cada inconsistencia encontrada.
        public List<Inconsistencia> VerificarIntegridad()
        {
            List<Inconsistencia> inconsistencias = new List<Inconsistencia>();

            int tablasExistentes = 0;
            foreach (string tabla in ObtenerTablasAControlar())
            {
                if (!dalDV.ExisteTabla(tabla))
                {
                    continue;
                }
                tablasExistentes++;
                inconsistencias.AddRange(VerificarTabla(tabla));
            }

            // Si faltan los dígitos de todas las tablas, es una base recién
            // creada. Si faltan solo los de algunas, alguien los borró: se
            // marca para que el administrador no recalcule sin revisar.
            int sinDigitos = inconsistencias.FindAll(
                delegate(Inconsistencia i) { return i.Tipo == TipoInconsistencia.SinDigitos; }).Count;
            if (sinDigitos > 0 && sinDigitos < tablasExistentes)
            {
                foreach (Inconsistencia inconsistencia in inconsistencias)
                {
                    if (inconsistencia.Tipo == TipoInconsistencia.SinDigitos)
                    {
                        inconsistencia.Sospechosa = true;
                    }
                }
            }

            return inconsistencias;
        }

        public List<Inconsistencia> VerificarTabla(string tabla)
        {
            lock (candado)
            {
                return VerificarTablaSinBloqueo(tabla);
            }
        }

        private List<Inconsistencia> VerificarTablaSinBloqueo(string tabla)
        {
            List<Inconsistencia> inconsistencias = new List<Inconsistencia>();

            DataTable contenido = dalDV.ObtenerContenido(tabla);
            List<string> clavePrimaria = dalDV.ObtenerClavePrimaria(tabla);

            Dictionary<string, string> dvhCalculados = CalcularDVHFilas(contenido, clavePrimaria);
            Dictionary<string, string> dvvCalculados = CalcularDVVColumnas(contenido);

            string dvhTablaCalculado = ConsolidarDVH(dvhCalculados);
            string dvvTablaCalculado = ConsolidarDVV(contenido, dvvCalculados);

            ObjetoDV dvAlmacenado = dalDV.ObtenerDV(tabla);

            bool tablaCoincide = dvAlmacenado != null &&
                                 dvAlmacenado.DVH == dvhTablaCalculado &&
                                 dvAlmacenado.DVV == dvvTablaCalculado;

            // Caso normal: los dígitos de la tabla completa coinciden y no hace
            // falta leer los de cada fila y cada columna (solo sirven para
            // ubicar la celda alterada cuando algo no coincide).
            if (tablaCoincide)
            {
                if (string.Equals(tabla, "Usuario", StringComparison.OrdinalIgnoreCase))
                {
                    inconsistencias.AddRange(VerificarDVHDeUsuarios(contenido, clavePrimaria, dvhCalculados,
                        NumerarFilas(contenido, clavePrimaria)));
                }
                return inconsistencias;
            }

            Dictionary<string, string> dvhAlmacenados = dalDV.ObtenerDVHFilas(tabla);
            Dictionary<string, string> dvvAlmacenados = dalDV.ObtenerDVVColumnas(tabla);

            // No hay dígitos guardados: es una base recién creada (o recién
            // actualizada con el script) o alguien los borró. Antes se
            // calculaban solos en este punto, pero entonces bastaba con vaciar
            // la tabla DV para que cualquier alteración pasara desapercibida.
            // Ahora se informa y el administrador los calcula a conciencia
            // desde Reparar dígito verificador (queda en la bitácora).
            if (SinCalcular(dvAlmacenado, dvhAlmacenados, dvvAlmacenados))
            {
                inconsistencias.Add(new Inconsistencia
                {
                    Tabla = tabla,
                    Tipo = TipoInconsistencia.SinDigitos,
                    DVCalculado = dvhTablaCalculado
                });
                return inconsistencias;
            }

            Dictionary<string, int> numerosDeFila = NumerarFilas(contenido, clavePrimaria);

            // La tabla Usuario guarda su DVH dentro del propio registro. Se controla
            // siempre, porque esa columna no participa del resto de los digitos.
            if (string.Equals(tabla, "Usuario", StringComparison.OrdinalIgnoreCase))
            {
                inconsistencias.AddRange(
                    VerificarDVHDeUsuarios(contenido, clavePrimaria, dvhCalculados, numerosDeFila));
            }

            int inconsistenciasPrevias = inconsistencias.Count;

            List<string> filasAlteradas = new List<string>();
            List<string> filasAgregadas = new List<string>();
            List<string> filasEliminadas = new List<string>();

            foreach (KeyValuePair<string, string> par in dvhCalculados)
            {
                string almacenado;
                if (!dvhAlmacenados.TryGetValue(par.Key, out almacenado))
                {
                    filasAgregadas.Add(par.Key);
                }
                else if (almacenado != par.Value)
                {
                    filasAlteradas.Add(par.Key);
                }
            }

            foreach (KeyValuePair<string, string> par in dvhAlmacenados)
            {
                if (!dvhCalculados.ContainsKey(par.Key))
                {
                    filasEliminadas.Add(par.Key);
                }
            }

            List<string> columnasAlteradas = new List<string>();
            foreach (KeyValuePair<string, string> par in dvvCalculados)
            {
                string almacenado;
                if (dvvAlmacenados.TryGetValue(par.Key, out almacenado) && almacenado != par.Value)
                {
                    columnasAlteradas.Add(par.Key);
                }
            }

            // Cruce fila x columna: la interseccion identifica la celda alterada.
            foreach (string clave in filasAlteradas)
            {
                if (columnasAlteradas.Count > 0)
                {
                    foreach (string columna in columnasAlteradas)
                    {
                        inconsistencias.Add(new Inconsistencia
                        {
                            Tabla = tabla,
                            ClaveFila = clave,
                            NumeroFila = ObtenerNumero(numerosDeFila, clave),
                            Columna = columna,
                            Tipo = TipoInconsistencia.CeldaAlterada,
                            DVEsperado = dvhAlmacenados[clave],
                            DVCalculado = dvhCalculados[clave]
                        });
                    }
                }
                else
                {
                    inconsistencias.Add(new Inconsistencia
                    {
                        Tabla = tabla,
                        ClaveFila = clave,
                        NumeroFila = ObtenerNumero(numerosDeFila, clave),
                        Tipo = TipoInconsistencia.FilaAlterada,
                        DVEsperado = dvhAlmacenados[clave],
                        DVCalculado = dvhCalculados[clave]
                    });
                }
            }

            foreach (string clave in filasAgregadas)
            {
                inconsistencias.Add(new Inconsistencia
                {
                    Tabla = tabla,
                    ClaveFila = clave,
                    NumeroFila = ObtenerNumero(numerosDeFila, clave),
                    Tipo = TipoInconsistencia.FilaAgregada,
                    DVCalculado = dvhCalculados[clave]
                });
            }

            foreach (string clave in filasEliminadas)
            {
                inconsistencias.Add(new Inconsistencia
                {
                    Tabla = tabla,
                    ClaveFila = clave,
                    Tipo = TipoInconsistencia.FilaEliminada,
                    DVEsperado = dvhAlmacenados[clave]
                });
            }

            if (inconsistencias.Count == inconsistenciasPrevias)
            {
                inconsistencias.Add(new Inconsistencia
                {
                    Tabla = tabla,
                    Tipo = TipoInconsistencia.TablaAlterada,
                    DVEsperado = dvAlmacenado == null ? string.Empty : dvAlmacenado.DVH,
                    DVCalculado = dvhTablaCalculado
                });
            }

            return inconsistencias;
        }

        #endregion

        #region Calculo

        private Dictionary<string, string> CalcularDVHFilas(DataTable contenido, List<string> clavePrimaria)
        {
            Dictionary<string, string> digitos = new Dictionary<string, string>(StringComparer.Ordinal);

            int indice = 0;
            foreach (DataRow fila in contenido.Rows)
            {
                indice++;
                string clave = ConstruirClave(contenido, fila, clavePrimaria, indice);
                digitos[clave] = CalcularDVHFila(contenido, fila);
            }

            return digitos;
        }

        private string CalcularDVHFila(DataTable contenido, DataRow fila)
        {
            StringBuilder datos = new StringBuilder();

            foreach (DataColumn columna in contenido.Columns)
            {
                if (EsColumnaDeControl(columna.ColumnName))
                {
                    continue;
                }
                datos.Append(Normalizar(fila[columna]));
                datos.Append('|');
            }

            return Encriptacion.EncriptarSHA256(datos.ToString());
        }

        private Dictionary<string, string> CalcularDVVColumnas(DataTable contenido)
        {
            Dictionary<string, string> digitos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (DataColumn columna in contenido.Columns)
            {
                if (EsColumnaDeControl(columna.ColumnName))
                {
                    continue;
                }

                StringBuilder datos = new StringBuilder();
                foreach (DataRow fila in contenido.Rows)
                {
                    datos.Append(Normalizar(fila[columna]));
                    datos.Append('|');
                }

                digitos[columna.ColumnName] = Encriptacion.EncriptarSHA256(datos.ToString());
            }

            return digitos;
        }

        /// DVH de la tabla: hash de la concatenacion de los DVH de las filas.
        private static string ConsolidarDVH(Dictionary<string, string> dvhFilas)
        {
            List<string> claves = new List<string>(dvhFilas.Keys);
            claves.Sort(StringComparer.Ordinal);

            StringBuilder datos = new StringBuilder();
            foreach (string clave in claves)
            {
                datos.Append(dvhFilas[clave]);
            }

            return Encriptacion.EncriptarSHA256(datos.ToString());
        }

        /// DVV de la tabla: hash de la concatenacion de los DVV de las columnas.
        private static string ConsolidarDVV(DataTable contenido, Dictionary<string, string> dvvColumnas)
        {
            StringBuilder datos = new StringBuilder();

            foreach (DataColumn columna in contenido.Columns)
            {
                string digito;
                if (dvvColumnas.TryGetValue(columna.ColumnName, out digito))
                {
                    datos.Append(digito);
                }
            }

            return Encriptacion.EncriptarSHA256(datos.ToString());
        }

        /// Normaliza el valor de una celda para que el hash sea reproducible
        /// independientemente de la cultura del servidor.
        private static string Normalizar(object valor)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return string.Empty;
            }

            if (valor is DateTime)
            {
                return ((DateTime)valor).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            }

            if (valor is bool)
            {
                return ((bool)valor) ? "1" : "0";
            }

            if (valor is decimal)
            {
                return ((decimal)valor).ToString(CultureInfo.InvariantCulture);
            }

            if (valor is double)
            {
                return ((double)valor).ToString("R", CultureInfo.InvariantCulture);
            }

            if (valor is float)
            {
                return ((float)valor).ToString("R", CultureInfo.InvariantCulture);
            }

            if (valor is byte[])
            {
                return BitConverter.ToString((byte[])valor).Replace("-", string.Empty);
            }

            return Convert.ToString(valor, CultureInfo.InvariantCulture);
        }

        private static bool EsColumnaDeControl(string columna)
        {
            foreach (string control in ColumnasDeControl)
            {
                if (string.Equals(columna, control, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string ConstruirClave(DataTable contenido, DataRow fila, List<string> clavePrimaria, int indice)
        {
            if (clavePrimaria == null || clavePrimaria.Count == 0)
            {
                return "fila=" + indice;
            }

            StringBuilder clave = new StringBuilder();
            for (int i = 0; i < clavePrimaria.Count; i++)
            {
                if (i > 0)
                {
                    clave.Append(" | ");
                }
                clave.Append(clavePrimaria[i]);
                clave.Append("=");
                clave.Append(Normalizar(fila[clavePrimaria[i]]));
            }

            return clave.ToString();
        }

        private static Dictionary<string, int> NumerarFilas(DataTable contenido, List<string> clavePrimaria)
        {
            Dictionary<string, int> numeros = new Dictionary<string, int>(StringComparer.Ordinal);

            int indice = 0;
            foreach (DataRow fila in contenido.Rows)
            {
                indice++;
                numeros[ConstruirClave(contenido, fila, clavePrimaria, indice)] = indice;
            }

            return numeros;
        }

        private static int ObtenerNumero(Dictionary<string, int> numeros, string clave)
        {
            int numero;
            return numeros.TryGetValue(clave, out numero) ? numero : 0;
        }

        private static bool SinCalcular(ObjetoDV dv, Dictionary<string, string> dvhFilas, Dictionary<string, string> dvvColumnas)
        {
            bool dvVacio = dv == null || (string.IsNullOrEmpty(dv.DVH) && string.IsNullOrEmpty(dv.DVV));
            return dvVacio && dvhFilas.Count == 0 && dvvColumnas.Count == 0;
        }

        #endregion

        #region Columna DVH de la tabla Usuario

        private void GuardarDVHEnUsuarios(DataTable contenido, List<string> clavePrimaria, Dictionary<string, string> dvhFilas)
        {
            int indice = 0;
            foreach (DataRow fila in contenido.Rows)
            {
                indice++;
                string clave = ConstruirClave(contenido, fila, clavePrimaria, indice);
                string dvh;

                // Solo los usuarios cuyo dígito cambió (en una operación común,
                // uno solo), en vez de reescribirlos todos.
                if (dvhFilas.TryGetValue(clave, out dvh) &&
                    (!contenido.Columns.Contains("DVH") || Normalizar(fila["DVH"]) != dvh))
                {
                    dalUsuario.ActualizarDVH(Convert.ToInt32(fila["dni"]), dvh);
                }
            }
        }

        private List<Inconsistencia> VerificarDVHDeUsuarios(
            DataTable contenido,
            List<string> clavePrimaria,
            Dictionary<string, string> dvhCalculados,
            Dictionary<string, int> numerosDeFila)
        {
            List<Inconsistencia> inconsistencias = new List<Inconsistencia>();

            if (!contenido.Columns.Contains("DVH"))
            {
                return inconsistencias;
            }

            int indice = 0;
            foreach (DataRow fila in contenido.Rows)
            {
                indice++;
                string clave = ConstruirClave(contenido, fila, clavePrimaria, indice);

                string calculado;
                if (!dvhCalculados.TryGetValue(clave, out calculado))
                {
                    continue;
                }

                string almacenado = Normalizar(fila["DVH"]);
                if (string.IsNullOrEmpty(almacenado) || almacenado == calculado)
                {
                    continue;
                }

                inconsistencias.Add(new Inconsistencia
                {
                    Tabla = "Usuario",
                    ClaveFila = clave,
                    NumeroFila = ObtenerNumero(numerosDeFila, clave),
                    Columna = "DVH",
                    Tipo = TipoInconsistencia.CeldaAlterada,
                    DVEsperado = almacenado,
                    DVCalculado = calculado
                });
            }

            return inconsistencias;
        }

        #endregion
    }
}
