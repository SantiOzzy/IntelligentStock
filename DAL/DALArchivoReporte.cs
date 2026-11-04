using System;
using System.IO;

namespace DAL
{
    /// Guarda en el servidor los archivos de los reportes gerenciales
    /// (CU-006-038), en la carpeta App_Data\Reportes del sitio.
    public class DALArchivoReporte
    {
        public static string Carpeta
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Reportes"); }
        }

        /// Escribe el archivo y devuelve su ruta completa.
        public string Guardar(string nombreArchivo, byte[] contenido)
        {
            string carpeta = Carpeta;
            if (!Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }

            string ruta = Path.Combine(carpeta, Path.GetFileName(nombreArchivo));
            File.WriteAllBytes(ruta, contenido);
            BorrarViejos(carpeta);
            return ruta;
        }

        /// Días que se guardan los reportes generados. Ya se descargaron al
        /// generarlos: no hace falta que la carpeta crezca para siempre.
        public const int DiasDeRetencion = 30;

        private static void BorrarViejos(string carpeta)
        {
            DateTime limite = DateTime.Now.AddDays(-DiasDeRetencion);
            foreach (string archivo in Directory.GetFiles(carpeta))
            {
                try
                {
                    if (File.GetLastWriteTime(archivo) < limite)
                    {
                        File.Delete(archivo);
                    }
                }
                catch (IOException)
                {
                    // Si un archivo está en uso se borra la próxima vez.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
