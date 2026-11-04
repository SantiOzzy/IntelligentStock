using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace DAL
{
    /// Backup y restore de la base de datos.
    public class DALRespaldo
    {
        private readonly DALConexion dalCon = new DALConexion();

        public void RealizarBackUp(string ruta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@RutaBackUp", ruta)
            };

            dalCon.EjecutarProcAlmacenadoSinTransaccion("RealizarBackUp", parametros);
        }

        /// Carpeta de backups configurada en la instancia. El archivo lo escribe
        /// el motor, no la aplicación, así que conviene proponer una ruta sobre la
        /// que la cuenta del servicio ya tenga permiso.
        public string ObtenerRutaPorDefecto()
        {
            try
            {
                System.Data.DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerRutaBackUpPorDefecto", null);
                if (tabla.Rows.Count > 0 && tabla.Rows[0]["ruta"] != DBNull.Value)
                {
                    return Convert.ToString(tabla.Rows[0]["ruta"]);
                }
            }
            catch (Exception)
            {
                // Versiones viejas del motor no exponen InstanceDefaultBackupPath.
            }

            return string.Empty;
        }

        /// Cuentas con las que suele correr el servicio de SQL Server cuando no
        /// se puede preguntar cuál es (instancia por defecto y SQL Express).
        private static readonly string[] CuentasHabituales =
        {
            @"NT Service\MSSQL$SQLEXPRESS", @"NT Service\MSSQLSERVER"
        };

        /// Cuenta de Windows con la que corre el servicio del motor (por ejemplo
        /// NT Service\MSSQL$SQLEXPRESS). Vacío si no se puede averiguar.
        public string ObtenerCuentaDelServicio()
        {
            try
            {
                DataTable tabla = dalCon.ConsultaEnMaster(
                    "SELECT TOP 1 service_account FROM sys.dm_server_services " +
                    "WHERE servicename LIKE 'SQL Server (%' ORDER BY servicename");
                if (tabla.Rows.Count > 0 && tabla.Rows[0][0] != DBNull.Value)
                {
                    return Convert.ToString(tabla.Rows[0][0]);
                }
            }
            catch (Exception)
            {
                // Sin permiso VIEW SERVER STATE no se puede consultar.
            }
            return string.Empty;
        }

        /// El BACKUP y el RESTORE los hace el motor con la cuenta de su servicio,
        /// no la aplicación. Si la carpeta está dentro del perfil del usuario
        /// (Escritorio, Documentos), Windows no deja que esa cuenta la lea ni la
        /// escriba y el motor responde "Operating system error 5 (Acceso
        /// denegado)". Acá se crea la carpeta (si hace falta) y se le da permiso
        /// de modificación a la cuenta del servicio, heredable a los archivos.
        /// Devuelve false si no se pudo dar el permiso (por ejemplo, porque la
        /// carpeta es de otro usuario).
        public bool PrepararCarpetaParaElMotor(string carpeta)
        {
            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            List<string> cuentas = new List<string>();
            string delMotor = ObtenerCuentaDelServicio();
            if (!string.IsNullOrEmpty(delMotor))
            {
                // LocalSystem ya puede leer y escribir en cualquier carpeta.
                if (delMotor.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                    delMotor.EndsWith(@"\SYSTEM", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                cuentas.Add(delMotor);
            }
            cuentas.AddRange(CuentasHabituales);

            bool alguna = false;
            try
            {
                DirectorySecurity seguridad = Directory.GetAccessControl(carpeta);
                foreach (string cuenta in cuentas)
                {
                    try
                    {
                        IdentityReference identidad = new NTAccount(cuenta).Translate(typeof(SecurityIdentifier));
                        seguridad.AddAccessRule(new FileSystemAccessRule(identidad, FileSystemRights.Modify,
                            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                            PropagationFlags.None, AccessControlType.Allow));
                        alguna = true;
                    }
                    catch (IdentityNotMappedException)
                    {
                        // Esa cuenta no existe en este equipo (otra instancia).
                    }
                }
                if (alguna)
                {
                    Directory.SetAccessControl(carpeta, seguridad);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            return alguna;
        }

        /// Restaura la base a partir de un archivo .bak. Se conecta a master,
        /// deja la base en modo monousuario, restaura y la devuelve a multiusuario.
        /// Si la base no existe (por ejemplo, se borró por error, que es justo el
        /// caso del usuario de contingencia) se restaura directamente. El paso a
        /// monousuario y el RESTORE van en el mismo lote, para que ninguna otra
        /// conexión ocupe el único lugar en el medio.
        public void RealizarRestore(string ruta)
        {
            string baseDeDatos = DALConexion.NombreBaseDeDatos;
            string nombreLiteral = "N'" + baseDeDatos.Replace("'", "''") + "'";

            try
            {
                dalCon.EjecutarComandoEnMaster(
                    "IF DB_ID(" + nombreLiteral + ") IS NOT NULL " +
                    "ALTER DATABASE [" + baseDeDatos + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    "RESTORE DATABASE [" + baseDeDatos + "] FROM DISK = N'" + ruta.Replace("'", "''") + "' WITH REPLACE;");
            }
            finally
            {
                // Las conexiones del pool a la base anterior quedaron cortadas por
                // el ROLLBACK IMMEDIATE: se descartan para que la próxima consulta
                // abra una conexión nueva.
                SqlConnection.ClearAllPools();

                // Asegura que la base quede disponible incluso si el restore falla
                // (con ROLLBACK IMMEDIATE, por si otra conexión tomó el único lugar).
                try
                {
                    dalCon.EjecutarComandoEnMaster(
                        "IF DB_ID(" + nombreLiteral + ") IS NOT NULL " +
                        "ALTER DATABASE [" + baseDeDatos + "] SET MULTI_USER WITH ROLLBACK IMMEDIATE;");
                }
                catch
                {
                    // La base ya quedo en multiusuario.
                }
            }
        }

        /// Ejecuta un script .sql de restauracion separando los lotes por GO.
        /// Todos los lotes van por la misma conexión: así el "USE [base]" del
        /// script sigue valiendo para los lotes siguientes (con una conexión por
        /// lote, cada uno volvía a arrancar en master).
        public void EjecutarScript(string contenido)
        {
            try
            {
                dalCon.EjecutarLotesEnMaster(SepararLotes(contenido));
            }
            finally
            {
                SqlConnection.ClearAllPools();
            }
        }

        private static List<string> SepararLotes(string contenido)
        {
            List<string> lotes = new List<string>();
            if (string.IsNullOrEmpty(contenido))
            {
                return lotes;
            }

            string[] lineas = contenido.Replace("\r\n", "\n").Split('\n');
            System.Text.StringBuilder lote = new System.Text.StringBuilder();

            foreach (string linea in lineas)
            {
                if (string.Equals(linea.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
                {
                    lotes.Add(lote.ToString());
                    lote.Length = 0;
                }
                else
                {
                    lote.AppendLine(linea);
                }
            }

            lotes.Add(lote.ToString());
            return lotes;
        }
    }
}
