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
            return ruta;
        }
    }
}
