using System;
using System.Globalization;
using System.IO;
using System.Xml;
using BE;

namespace DAL
{
    /// Guarda las facturas de venta de los planes (CU-008-044) en la carpeta
    /// App_Data\Facturas del sitio. No hay una tabla de facturas: el
    /// comprobante es el PDF, y sus datos quedan además en la bitácora. A
    /// diferencia de los reportes, las facturas no se borran nunca (son
    /// documentos comerciales).
    ///
    /// En la misma carpeta quedan los datos de facturación de cada suscripción
    /// (Cliente_Suscripcion_N.xml): con ellos se emite la factura de cada abono
    /// mensual que se cobra con la tarjeta, sin volver a pedírselos al cliente.
    public class DALArchivoFactura
    {
        private const string PrefijoCliente = "Cliente_Suscripcion_";

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

        /// Último número de comprobante emitido de ese tipo (A o B) y punto de
        /// venta, según los PDF de la carpeta (Factura_A_0001-00000012.pdf). 0 si
        /// todavía no hay ninguno.
        public int UltimoNumero(string tipo, int puntoDeVenta)
        {
            if (!Directory.Exists(Carpeta))
            {
                return 0;
            }

            string prefijo = "Factura_" + tipo + "_" + puntoDeVenta.ToString("0000", CultureInfo.InvariantCulture) + "-";
            int ultimo = 0;
            foreach (string ruta in Directory.GetFiles(Carpeta, prefijo + "*.pdf"))
            {
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                int numero;
                if (int.TryParse(nombre.Substring(prefijo.Length), NumberStyles.None, CultureInfo.InvariantCulture, out numero) &&
                    numero > ultimo)
                {
                    ultimo = numero;
                }
            }
            return ultimo;
        }

        /// Guarda los datos de facturación del cliente de una suscripción (los
        /// de la última compra). La tarjeta queda solo con la marca y los
        /// últimos cuatro números, que son los que se imprimen en la factura.
        public void GuardarDatosDeFacturacion(int idSuscripcion, BECompraPlan compra)
        {
            if (!Directory.Exists(Carpeta))
            {
                Directory.CreateDirectory(Carpeta);
            }

            XmlWriterSettings configuracion = new XmlWriterSettings();
            configuracion.Indent = true;
            string ruta = RutaDatosDeFacturacion(idSuscripcion);
            using (XmlWriter writer = XmlWriter.Create(ruta, configuracion))
            {
                writer.WriteStartElement("Cliente");
                writer.WriteAttributeString("suscripcion", idSuscripcion.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("RazonSocial", compra.RazonSocial ?? string.Empty);
                writer.WriteElementString("Documento", compra.DocumentoCliente ?? string.Empty);
                writer.WriteElementString("CondicionIva", compra.CondicionIva ?? string.Empty);
                writer.WriteElementString("Domicilio", compra.Domicilio ?? string.Empty);
                writer.WriteElementString("Correo", compra.Correo ?? string.Empty);
                writer.WriteElementString("MarcaTarjeta", compra.MarcaTarjeta ?? string.Empty);
                writer.WriteElementString("UltimosDigitos", compra.UltimosDigitos ?? string.Empty);
                writer.WriteElementString("Titular", compra.TitularTarjeta ?? string.Empty);
                writer.WriteEndElement();
            }
        }

        /// Datos de facturación guardados de la suscripción, o null si no hay
        /// (por ejemplo, si se dio de alta por el servicio web, sin factura).
        public BECompraPlan LeerDatosDeFacturacion(int idSuscripcion)
        {
            string ruta = RutaDatosDeFacturacion(idSuscripcion);
            if (!File.Exists(ruta))
            {
                return null;
            }

            XmlDocument documento = new XmlDocument();
            documento.Load(ruta);
            XmlElement raiz = documento.DocumentElement;
            if (raiz == null)
            {
                return null;
            }

            BECompraPlan datos = new BECompraPlan();
            datos.RazonSocial = Texto(raiz, "RazonSocial");
            datos.DocumentoCliente = Texto(raiz, "Documento");
            datos.CondicionIva = Texto(raiz, "CondicionIva");
            datos.Domicilio = Texto(raiz, "Domicilio");
            datos.Correo = Texto(raiz, "Correo");
            datos.MarcaTarjeta = Texto(raiz, "MarcaTarjeta");
            datos.UltimosDigitos = Texto(raiz, "UltimosDigitos");
            datos.TitularTarjeta = Texto(raiz, "Titular");
            return datos;
        }

        private static string RutaDatosDeFacturacion(int idSuscripcion)
        {
            return Path.Combine(Carpeta, PrefijoCliente + idSuscripcion.ToString(CultureInfo.InvariantCulture) + ".xml");
        }

        private static string Texto(XmlElement raiz, string nombre)
        {
            XmlNode nodo = raiz.SelectSingleNode(nombre);
            return nodo == null ? string.Empty : nodo.InnerText;
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
