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

    /// Servicio de factura electrónica de ARCA (ex AFIP), simulado.
    ///
    /// En producción, cada factura se autoriza así:
    ///   1. El emisor tiene un certificado digital X.509 que ARCA le emite para
    ///      su CUIT: genera una clave privada RSA, arma el pedido de certificado
    ///      (CSR), lo sube con clave fiscal y lo asocia al servicio "wsfe".
    ///   2. Con ese certificado firma un ticket de acceso (TRA, en formato CMS)
    ///      y lo presenta al servicio de autenticación WSAA, que le devuelve un
    ///      token y una firma válidos por 12 horas.
    ///   3. Con el token y la firma llama a FECAESolicitar del servicio WSFEv1
    ///      con los datos del comprobante (tipo, punto de venta, número, CUIT
    ///      del cliente, importes) y recibe el CAE y su vencimiento.
    ///   4. El CAE se imprime en la factura junto con un código QR.
    ///
    /// Para el trabajo no hay certificado ni conexión con ARCA: este servicio
    /// controla los datos, devuelve un CAE ficticio de 14 dígitos con
    /// vencimiento a 10 días y la factura lleva la leyenda "sin validez fiscal".
    /// Igual que con la pasarela de cobros, la respuesta se acepta solo si
    /// viene de un origen autorizado (R9).
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
