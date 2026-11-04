using System;
using System.IO;

namespace DAL
{
    /// Guarda las facturas de venta de los planes (CU-008-044) en la carpeta
    /// App_Data\Facturas del sitio. No hay una tabla de facturas: el
    /// comprobante es el PDF, y sus datos quedan además en la bitácora. A
    /// diferencia de los reportes, las facturas no se borran nunca (son
    /// documentos comerciales).
    public class DALArchivoFactura
    {
        public static string Carpeta
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Facturas"); }
        }

        /// Escribe el archivo y devuelve su ruta completa.
        public string Guardar(string nombreArchivo, byte[] contenido)
        {
            if (!Directory.Exists(Carpeta))
            {
                Directory.CreateDirectory(Carpeta);
            }

            string ruta = Path.Combine(Carpeta, Path.GetFileName(nombreArchivo));
            File.WriteAllBytes(ruta, contenido);
            return ruta;
        }

        /// Contenido del archivo, o null si no existe. Path.GetFileName evita
        /// que un nombre armado a mano ("..\..\Web.config") salga de la carpeta.
        public byte[] Leer(string nombreArchivo)
        {
            if (string.IsNullOrEmpty(nombreArchivo))
            {
                return null;
            }

            string ruta = Path.Combine(Carpeta, Path.GetFileName(nombreArchivo));
            return File.Exists(ruta) ? File.ReadAllBytes(ruta) : null;
        }
    }
}
