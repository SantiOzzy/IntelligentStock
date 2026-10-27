using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using BE;

namespace DAL
{
    /// Exportación estructurada de alertas (App_Data\AlertasGeneradas.xml).
    ///
    /// Es el registro auditable de las alertas térmicas: se escribe nodo por
    /// nodo con XmlTextWriter a partir de la tabla Alerta. Estructura:
    ///
    ///   &lt;HistorialAlertas generado="2026-10-09T14:30:00"&gt;
    ///     &lt;Alerta&gt;
    ///       &lt;IdAlerta&gt;1&lt;/IdAlerta&gt;
    ///       &lt;Sensor&gt;00:1A:2B:3C:4D:5E&lt;/Sensor&gt;
    ///       &lt;Descripcion&gt;Temperatura de 8,5 °C en el sensor ...&lt;/Descripcion&gt;
    ///       &lt;FechaHora&gt;09/10/2026 14:30:00&lt;/FechaHora&gt;
    ///       &lt;Estado&gt;Resuelta&lt;/Estado&gt;
    ///       &lt;Observaciones&gt;Se cerró la puerta de la cámara.&lt;/Observaciones&gt;
    ///     &lt;/Alerta&gt;
    ///   &lt;/HistorialAlertas&gt;
    public class DALAlertasXml
    {
        private const string NombreArchivo = "AlertasGeneradas.xml";

        /// Dos usuarios pueden exportar al mismo tiempo: el archivo se escribe de a uno.
        private static readonly object candado = new object();

        public static string RutaArchivo
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", NombreArchivo); }
        }

        /// Escribe el archivo completo y devuelve su ruta. Se graba primero en un
        /// archivo temporal y después se reemplaza el anterior.
        public string Exportar(List<BEAlerta> alertas)
        {
            lock (candado)
            {
                string ruta = RutaArchivo;
                string carpeta = Path.GetDirectoryName(ruta);
                if (!Directory.Exists(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                }

                string temporal = ruta + ".tmp";

                XmlTextWriter writer = new XmlTextWriter(temporal, Encoding.UTF8);
                try
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("HistorialAlertas");
                    writer.WriteAttributeString("generado",
                        DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));

                    foreach (BEAlerta alerta in alertas)
                    {
                        writer.WriteStartElement("Alerta");
                        writer.WriteElementString("IdAlerta", alerta.IdAlerta.ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString("Sensor", alerta.MacSensor);
                        writer.WriteElementString("Descripcion", alerta.Descripcion);
                        writer.WriteElementString("FechaHora",
                            alerta.FechaHora.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                        writer.WriteElementString("Estado", alerta.Estado);
                        writer.WriteElementString("Observaciones", alerta.Observaciones ?? string.Empty);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                }
                finally
                {
                    writer.Close();
                }

                if (File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
                File.Move(temporal, ruta);

                return ruta;
            }
        }
    }
}
