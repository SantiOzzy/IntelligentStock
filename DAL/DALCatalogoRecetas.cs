using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using BE;

namespace DAL
{
    /// Catálogo de recetas en XML (App_Data\CatalogoRecetas.xml).
    ///
    /// Se regenera cada vez que se da de alta, se modifica o se da de baja una
    /// receta, y las pantallas lo leen para mostrar los insumos de cada receta
    /// sin volver a consultar la base. Estructura:
    ///
    ///   &lt;CatalogoRecetas&gt;
    ///     &lt;Receta&gt;
    ///       &lt;IdReceta&gt;1&lt;/IdReceta&gt;
    ///       &lt;Nombre&gt;Hamburguesa completa&lt;/Nombre&gt;
    ///       &lt;Estado&gt;Activa&lt;/Estado&gt;
    ///       &lt;DetalleInsumos&gt;
    ///         &lt;Insumo idInsumo="3" cantidad="0.2" unidad="kg"&gt;Carne picada&lt;/Insumo&gt;
    ///       &lt;/DetalleInsumos&gt;
    ///     &lt;/Receta&gt;
    ///   &lt;/CatalogoRecetas&gt;
    public class DALCatalogoRecetas
    {
        private const string NombreArchivo = "CatalogoRecetas.xml";

        /// Dos usuarios pueden guardar recetas al mismo tiempo: el archivo se
        /// escribe y se lee de a uno.
        private static readonly object candado = new object();

        public static string RutaArchivo
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", NombreArchivo); }
        }

        public bool Existe()
        {
            return File.Exists(RutaArchivo);
        }

        /// Escribe el catálogo completo nodo por nodo con XmlTextWriter. Se graba
        /// primero en un archivo temporal y después se reemplaza el anterior, para
        /// que nadie lea un archivo a medio escribir.
        public void Exportar(List<BEReceta> recetas)
        {
            lock (candado)
            {
                EscribirArchivo(recetas);
            }
        }

        private static void EscribirArchivo(List<BEReceta> recetas)
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
                writer.WriteStartElement("CatalogoRecetas");
                writer.WriteAttributeString("generado", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));

                foreach (BEReceta receta in recetas)
                {
                    writer.WriteStartElement("Receta");
                    writer.WriteElementString("IdReceta", receta.IdReceta.ToString(CultureInfo.InvariantCulture));
                    writer.WriteElementString("Nombre", receta.Nombre);
                    writer.WriteElementString("Estado", receta.Estado);

                    writer.WriteStartElement("DetalleInsumos");
                    foreach (BEDetalleReceta detalle in receta.Detalles)
                    {
                        writer.WriteStartElement("Insumo");
                        writer.WriteAttributeString("idInsumo", detalle.IdInsumo.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("cantidad", detalle.Cantidad.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("unidad", detalle.UnidadDeMedida);
                        writer.WriteString(detalle.NombreInsumo);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();

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
        }

        /// Carga el árbol completo en memoria (DOM) con XmlTextReader.
        public XmlDocument Cargar()
        {
            lock (candado)
            {
                return LeerArchivo();
            }
        }

        private static XmlDocument LeerArchivo()
        {
            XmlDocument documento = new XmlDocument();

            XmlTextReader lector = new XmlTextReader(RutaArchivo);
            try
            {
                lector.WhitespaceHandling = WhitespaceHandling.None;
                documento.Load(lector);
            }
            finally
            {
                lector.Close();
            }

            return documento;
        }
    }
}
