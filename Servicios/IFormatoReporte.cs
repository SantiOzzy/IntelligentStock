using BE;

namespace Servicios
{
    /// Patrón Strategy: un formato de archivo del reporte gerencial. Cada
    /// formato (PDF, Excel, CSV) es una clase que implementa esta interfaz y
    /// sabe armar su archivo; ExportadorReporte elige cuál usar.
    public interface IFormatoReporte
    {
        /// Nombre con el que se elige el formato en la pantalla.
        string Nombre { get; }

        /// Extensión del archivo, con el punto (por ejemplo ".pdf").
        string Extension { get; }

        /// Tipo MIME con el que se descarga el archivo.
        string TipoMime { get; }

        /// Contenido del archivo del reporte en este formato.
        byte[] Generar(BEReporte reporte);
    }
}
