using System;
using System.Data.SqlClient;
using System.IO;
using DAL;
using Servicios;

namespace BLL
{
    /// CU-003-017 BackUp y CU-003-018 Restore de base de datos.
    public class BLLRespaldo
    {
        private readonly DALRespaldo dalRespaldo = new DALRespaldo();
        private readonly BLLEvento bllEvento = new BLLEvento();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();

        /// Carpeta de las copias de seguridad: App_Data\Respaldos del sitio. La
        /// aplicación la prepara para que el motor pueda escribir y leer ahí
        /// (ver DALRespaldo.PrepararCarpetaParaElMotor), y como está dentro del
        /// sitio la aplicación también puede descargar lo que genera el motor.
        public static string CarpetaDeRespaldos
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Respaldos"); }
        }

        /// Carpeta que propone la pantalla de respaldo.
        public string RutaPorDefecto()
        {
            return CarpetaDeRespaldos;
        }

        /// Carpeta donde la pantalla de restore deja el archivo subido, ya
        /// preparada para que el motor lo pueda leer.
        public string PrepararCarpetaDeRestauracion()
        {
            string carpeta = Path.Combine(CarpetaDeRespaldos, "Restauracion");
            try
            {
                dalRespaldo.PrepararCarpetaParaElMotor(carpeta);
            }
            catch (Exception)
            {
                // Si no se pudo dar el permiso, el motor lo informa al restaurar.
            }
            return carpeta;
        }

        /// Genera el archivo .bak y devuelve la ruta completa.
        public string RealizarBackUp(string carpetaDestino)
        {
            if (string.IsNullOrEmpty(carpetaDestino))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            carpetaDestino = carpetaDestino.Trim().TrimEnd('\\');

            // La carpeta la crea la aplicación, pero el archivo lo escribe el
            // motor: además de crearla se le da permiso a la cuenta del servicio.
            try
            {
                dalRespaldo.PrepararCarpetaParaElMotor(carpetaDestino);
            }
            catch (UnauthorizedAccessException)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR022",
                    "La aplicación no puede crear la carpeta " + carpetaDestino + "."));
            }
            catch (IOException)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR015"));
            }

            string nombreArchivo = DALConexion.NombreBaseDeDatos + "_BackUp_" +
                                   DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bak";
            string rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            try
            {
                dalRespaldo.RealizarBackUp(rutaCompleta);
            }
            catch (ApplicationException ex)
            {
                if (EsFaltaDePermisoDelMotor(ex))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR022",
                        "SQL Server no tiene permiso para escribir en " + carpetaDestino + ". " +
                        ComoDarPermiso(carpetaDestino)), ex);
                }
                throw;
            }

            bllEvento.RegistrarDelUsuarioActual("BackUp de base de datos",
                "Se generó la copia de seguridad " + nombreArchivo + ".");

            return rutaCompleta;
        }

        /// Restaura la base a partir de un archivo .bak.
        public void RestaurarDesdeBak(string rutaArchivo)
        {
            if (string.IsNullOrEmpty(rutaArchivo) || !File.Exists(rutaArchivo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR016"));
            }

            if (!string.Equals(Path.GetExtension(rutaArchivo), ".bak", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR016"));
            }

            try
            {
                dalRespaldo.RealizarRestore(rutaArchivo);
            }
            catch (ApplicationException ex)
            {
                // 3241 / 3183: no es un backup válido o está dañado (#ERR016).
                // 3201 / 3013: el motor no pudo abrir el archivo. Si es por
                // permisos ("error 5" del sistema operativo: la cuenta del servicio
                // de SQL Server no puede leer la carpeta) queda como #ERR022; si
                // no, se informa como archivo inválido. Lo demás sube tal cual.
                SqlException motor = ex.InnerException as SqlException;
                if (motor != null && (motor.Number == 3241 || motor.Number == 3183))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR016",
                        "Detalle del motor: " + motor.Message), ex);
                }
                if (EsFaltaDePermisoDelMotor(ex))
                {
                    string carpeta = Path.GetDirectoryName(rutaArchivo);
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR022",
                        "SQL Server no tiene permiso para leer el archivo en " + carpeta + ". " +
                        ComoDarPermiso(carpeta)), ex);
                }
                if (motor != null && (motor.Number == 3201 || motor.Number == 3013))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR016",
                        "Detalle del motor: " + motor.Message), ex);
                }
                throw;
            }

            bllEvento.RegistrarDelUsuarioActual("Restore de base de datos",
                "Se restauró la base de datos desde el archivo " + Path.GetFileName(rutaArchivo) + ".");
        }

        /// El motor no pudo abrir el archivo por permisos: "Operating system
        /// error 5" (acceso denegado) en los errores 3201 (backup/restore) o 3013.
        private static bool EsFaltaDePermisoDelMotor(ApplicationException ex)
        {
            SqlException motor = ex.InnerException as SqlException;
            if (motor == null || (motor.Number != 3201 && motor.Number != 3013))
            {
                return false;
            }
            foreach (SqlError error in motor.Errors)
            {
                if (error.Message.IndexOf("error 5", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return motor.Message.IndexOf("error 5", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// Explica cómo dar el permiso a mano cuando la aplicación no pudo.
        private string ComoDarPermiso(string carpeta)
        {
            string cuenta = dalRespaldo.ObtenerCuentaDelServicio();
            if (string.IsNullOrEmpty(cuenta))
            {
                cuenta = @"NT Service\MSSQL$SQLEXPRESS";
            }
            return "Para solucionarlo, ejecute una vez en una consola de Windows abierta como administrador: " +
                   "icacls \"" + carpeta + "\" /grant \"" + cuenta + ":(OI)(CI)M\"";
        }

        /// Restaura la base ejecutando un script .sql.
        public void RestaurarDesdeScript(string rutaArchivo)
        {
            if (string.IsNullOrEmpty(rutaArchivo) || !File.Exists(rutaArchivo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR016"));
            }

            if (!string.Equals(Path.GetExtension(rutaArchivo), ".sql", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR016"));
            }

            string contenido = File.ReadAllText(rutaArchivo);
            dalRespaldo.EjecutarScript(contenido);

            bllEvento.RegistrarDelUsuarioActual("Restore de base de datos",
                "Se restauró la base de datos ejecutando el script " + Path.GetFileName(rutaArchivo) + ".");
        }

        /// Recalcula los dígitos verificadores despues de una restauracion.
        public void RecalcularIntegridad()
        {
            bllDV.RecalcularBaseDeDatos();

            bllEvento.RegistrarDelUsuarioActual("Recálculo de dígito verificador",
                "Se recalcularon los dígitos verificadores de todas las tablas controladas.");
        }
    }
}
