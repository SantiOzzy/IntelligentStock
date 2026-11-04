using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using BE;

namespace DAL
{
    /// Archivo histórico de la bitácora: un XML por mes en App_Data\Bitacora
    /// (Bitacora_AAAA-MM.xml).
    ///
    /// Es la estrategia de almacenamiento de la bitácora (CU-003-016): cuando
    /// un evento sale de la ventana de 30 días y pasa a eliminado (eliminación
    /// lógica), antes se copia al archivo del mes en que ocurrió. El archivo es
    /// una copia fuera de la base que se puede descargar, guardar aparte o abrir
    /// con cualquier programa, y que no se pierde si se restaura un backup más
    /// viejo. Estructura:
    ///
    ///   &lt;Bitacora periodo="2026-09" actualizado="2026-10-15T08:00:00"&gt;
    ///     &lt;Evento id="1532" fecha="2026-09-14T10:22:31" criticidad="Informativa"
    ///             archivado="2026-10-15T08:00:00"&gt;
    ///       &lt;Usuario dni="30111222"&gt;admin&lt;/Usuario&gt;
    ///       &lt;Accion&gt;Inicio de sesión&lt;/Accion&gt;
    ///       &lt;Descripcion&gt;El usuario admin inició sesión.&lt;/Descripcion&gt;
    ///     &lt;/Evento&gt;
    ///   &lt;/Bitacora&gt;
    public class DALArchivoBitacora
    {
        private const string Prefijo = "Bitacora_";
        private const string FormatoFecha = "yyyy-MM-ddTHH:mm:ss";

        /// El mantenimiento y una descarga pueden coincidir: los archivos se
        /// tocan de a uno.
        private static readonly object candado = new object();

        public static string Carpeta
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Bitacora"); }
        }

        /// Agrega cada evento al archivo de su mes y devuelve los nombres de los
        /// archivos que tocó, ordenados. Un evento que ya está en el archivo
        /// (mismo código y misma fecha, por ejemplo porque se restauró un backup
        /// en el que todavía estaba activo) no se repite.
        public List<string> Archivar(List<BEEvento> eventos)
        {
            Dictionary<string, List<BEEvento>> porMes = new Dictionary<string, List<BEEvento>>();
            foreach (BEEvento evento in eventos)
            {
                string periodo = evento.FechaHora.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                if (!porMes.ContainsKey(periodo))
                {
                    porMes[periodo] = new List<BEEvento>();
                }
                porMes[periodo].Add(evento);
            }

            List<string> periodos = new List<string>(porMes.Keys);
            periodos.Sort(StringComparer.Ordinal);

            List<string> archivos = new List<string>();
            lock (candado)
            {
                if (!Directory.Exists(Carpeta))
                {
                    Directory.CreateDirectory(Carpeta);
                }

                foreach (string periodo in periodos)
                {
                    string nombre = Prefijo + periodo + ".xml";
                    AgregarAlArchivo(Path.Combine(Carpeta, nombre), periodo, porMes[periodo]);
                    archivos.Add(nombre);
                }
            }

            return archivos;
        }

        /// Nombres de los archivos disponibles, del mes más nuevo al más viejo.
        public List<string> Listar()
        {
            List<string> nombres = new List<string>();
            if (!Directory.Exists(Carpeta))
            {
                return nombres;
            }

            foreach (string ruta in Directory.GetFiles(Carpeta, Prefijo + "*.xml"))
            {
                nombres.Add(Path.GetFileName(ruta));
            }

            nombres.Sort(StringComparer.Ordinal);
            nombres.Reverse();
            return nombres;
        }

        /// Contenido del archivo, o null si no existe. Path.GetFileName evita
        /// que un nombre armado a mano ("..\..\Web.config") salga de la carpeta.
        public byte[] Leer(string nombreArchivo)
        {
            if (string.IsNullOrEmpty(nombreArchivo))
            {
                return null;
            }

            lock (candado)
            {
                string ruta = Path.Combine(Carpeta, Path.GetFileName(nombreArchivo));
                return File.Exists(ruta) ? File.ReadAllBytes(ruta) : null;
            }
        }

        private static void AgregarAlArchivo(string ruta, string periodo, List<BEEvento> eventos)
        {
            XmlDocument documento = new XmlDocument();
            XmlElement raiz;

            if (File.Exists(ruta))
            {
                documento.Load(ruta);
                raiz = documento.DocumentElement;
            }
            else
            {
                documento.AppendChild(documento.CreateXmlDeclaration("1.0", "utf-8", null));
                raiz = documento.CreateElement("Bitacora");
                raiz.SetAttribute("periodo", periodo);
                documento.AppendChild(raiz);
            }

            // Los eventos que ya están en el archivo se buscan con XPath.
            HashSet<string> yaArchivados = new HashSet<string>();
            foreach (XmlElement nodo in documento.SelectNodes("/Bitacora/Evento"))
            {
                yaArchivados.Add(nodo.GetAttribute("id") + "|" + nodo.GetAttribute("fecha"));
            }

            string ahora = DateTime.Now.ToString(FormatoFecha, CultureInfo.InvariantCulture);
            foreach (BEEvento evento in eventos)
            {
                string id = evento.IdEvento.ToString(CultureInfo.InvariantCulture);
                string fecha = evento.FechaHora.ToString(FormatoFecha, CultureInfo.InvariantCulture);
                if (!yaArchivados.Add(id + "|" + fecha))
                {
                    continue;
                }

                XmlElement nodo = documento.CreateElement("Evento");
                nodo.SetAttribute("id", id);
                nodo.SetAttribute("fecha", fecha);
                nodo.SetAttribute("criticidad", evento.Criticidad.ToString());
                nodo.SetAttribute("archivado", ahora);

                XmlElement usuario = documento.CreateElement("Usuario");
                if (evento.DniUsuario.HasValue)
                {
                    usuario.SetAttribute("dni", evento.DniUsuario.Value.ToString(CultureInfo.InvariantCulture));
                }
                usuario.InnerText = evento.Usuario;
                nodo.AppendChild(usuario);

                XmlElement accion = documento.CreateElement("Accion");
                accion.InnerText = evento.Accion ?? string.Empty;
                nodo.AppendChild(accion);

                XmlElement descripcion = documento.CreateElement("Descripcion");
                descripcion.InnerText = evento.Descripcion ?? string.Empty;
                nodo.AppendChild(descripcion);

                raiz.AppendChild(nodo);
            }

            raiz.SetAttribute("actualizado", ahora);

            // Se graba en un temporal y después se reemplaza el anterior, para
            // no dejar un archivo a medias si algo falla en el medio.
            string temporal = ruta + ".tmp";
            XmlWriterSettings opciones = new XmlWriterSettings();
            opciones.Indent = true;
            opciones.Encoding = new UTF8Encoding(false);
            using (XmlWriter writer = XmlWriter.Create(temporal, opciones))
            {
                documento.Save(writer);
            }

            if (File.Exists(ruta))
            {
                File.Delete(ruta);
            }
            File.Move(temporal, ruta);
        }
    }
}
