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

        /// Carpeta que propone la pantalla de respaldo. Si el motor informa su
        /// carpeta de backups se usa esa; si no, se cae en App_Data\Backups.
        public string RutaPorDefecto(string rutaAlternativa)
        {
            string delMotor = dalRespaldo.ObtenerRutaPorDefecto();
            return string.IsNullOrEmpty(delMotor) ? rutaAlternativa : delMotor.TrimEnd('\\');
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
            // motor: que exista no garantiza que SQL Server pueda escribir en ella.
            try
            {
                if (!Directory.Exists(carpetaDestino))
                {
                    Directory.CreateDirectory(carpetaDestino);
                }
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

            dalRespaldo.RealizarBackUp(rutaCompleta);

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
                if (motor != null && (motor.Number == 3201 || motor.Number == 3013))
                {
                    bool porPermisos = motor.Message.IndexOf("error 5", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       motor.Message.IndexOf("(5)", StringComparison.Ordinal) >= 0;
                    throw new ApplicationException(AdministradorErrores.Mensaje(porPermisos ? "ERR022" : "ERR016",
                        "Detalle del motor: " + motor.Message), ex);
                }
                throw;
            }

            bllEvento.RegistrarDelUsuarioActual("Restore de base de datos",
                "Se restauró la base de datos desde el archivo " + Path.GetFileName(rutaArchivo) + ".");
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
