using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Servicios
{
    /// Respuesta del servicio de factura electrónica.
    public class RespuestaCae
    {
        public bool Aprobado { get; set; }

        /// Código de Autorización Electrónico (14 dígitos).
        public string Cae { get; set; }

        /// Hasta cuándo vale el CAE.
        public DateTime Vencimiento { get; set; }

        public string Motivo { get; set; }

        /// IP desde la que respondió el servicio (se controla con R9).
        public string IpOrigen { get; set; }
    }

    /// Factura electrónica (CAE de ARCA) a través de un proveedor de
    /// facturación electrónica, simulado.
    ///
    /// Cada factura necesita el CAE de ARCA (ex AFIP). Para pedirlo al servicio
    /// web de factura electrónica (WSFEv1) hay que autenticarse en el servicio
    /// WSAA con un certificado digital X.509 emitido para una CUIT. La empresa
    /// no guarda ese certificado en el sistema: delega la factura electrónica en
    /// un proveedor de facturación, al que autoriza en ARCA con su clave fiscal
    /// (Administrador de Relaciones). El proveedor tiene el certificado, firma
    /// el ticket de acceso para WSAA, llama a FECAESolicitar de WSFEv1 y
    /// devuelve el CAE y su vencimiento; este servicio le envía los datos de
    /// cada comprobante (tipo, punto de venta, número, documento del cliente,
    /// importe y fecha). El certificado es la firma fiscal de la empresa y no
    /// debe quedar en el servidor web, que está expuesto a internet (ver
    /// "Factura electrónica y certificado digital" en las estrategias de
    /// programación).
    ///
    /// Para el trabajo la conexión con el proveedor está simulada: controla los
    /// datos, devuelve un CAE ficticio de 14 dígitos con vencimiento a 10 días y
    /// la factura lleva la leyenda "sin validez fiscal". Igual que con la
    /// pasarela de cobros, la respuesta se acepta solo si viene de un origen
    /// autorizado (R9).
    public static class FacturacionElectronica
    {
        private const string IpServicioSimulado = "127.0.0.1";

        /// Días de validez del CAE.
        public const int DiasDeVigenciaCae = 10;

        public static RespuestaCae SolicitarCae(string tipoComprobante, int puntoDeVenta, int numero,
                                                string documentoCliente, decimal total, DateTime fecha)
        {
            RespuestaCae respuesta = new RespuestaCae();
            respuesta.IpOrigen = IpServicioSimulado;

            if (tipoComprobante != "A" && tipoComprobante != "B")
            {
                respuesta.Motivo = "El tipo de comprobante no es válido.";
                return respuesta;
            }
            if (puntoDeVenta <= 0 || numero <= 0)
            {
                respuesta.Motivo = "El punto de venta y el número de comprobante tienen que ser mayores que cero.";
                return respuesta;
            }
            if (string.IsNullOrEmpty(documentoCliente))
            {
                respuesta.Motivo = "Falta el documento del cliente.";
                return respuesta;
            }
            if (total <= 0)
            {
                respuesta.Motivo = "El importe total tiene que ser mayor que cero.";
                return respuesta;
            }

            byte[] azar = new byte[13];
            using (RandomNumberGenerator generador = RandomNumberGenerator.Create())
            {
                generador.GetBytes(azar);
            }

            StringBuilder cae = new StringBuilder("7");
            foreach (byte b in azar)
            {
                cae.Append((b % 10).ToString(CultureInfo.InvariantCulture));
            }

            respuesta.Aprobado = true;
            respuesta.Cae = cae.ToString();
            respuesta.Vencimiento = fecha.Date.AddDays(DiasDeVigenciaCae);
            return respuesta;
        }
    }
}
