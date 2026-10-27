using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Servicios
{
    /// Respuesta de la pasarela de cobros.
    public class RespuestaPasarela
    {
        public bool Aprobada { get; set; }

        /// Identificador de la operación en la pasarela (solo si se aprobó).
        public string CodigoTransaccion { get; set; }

        /// Motivo del rechazo (solo si no se aprobó).
        public string Motivo { get; set; }

        /// IP desde la que respondió la pasarela. Se controla con R9 antes de
        /// dar la respuesta por buena.
        public string IpOrigen { get; set; }
    }

    /// Pasarela de cobros externa (simulada).
    ///
    /// En producción esta clase llamaría a la API del procesador de pagos. Para
    /// el trabajo se simula con estas reglas:
    ///   - el token tiene que tener el formato "tok_" seguido de 16 a 60 letras
    ///     o números, como los que devuelve el formulario de pago;
    ///   - si el token contiene la palabra "rechaz" la pasarela rechaza el
    ///     cobro (sirve para probar el error #ERR024);
    ///   - en cualquier otro caso el cobro se aprueba.
    public static class PasarelaPagos
    {
        /// La pasarela simulada responde desde el propio servidor.
        private const string IpPasarelaSimulada = "127.0.0.1";

        private static readonly Regex FormatoToken = new Regex("^tok_[A-Za-z0-9]{16,60}$", RegexOptions.Compiled);

        public static bool EsTokenConFormatoValido(string token)
        {
            return !string.IsNullOrEmpty(token) && FormatoToken.IsMatch(token);
        }

        /// Cobra el primer período del plan y deja programado el cobro recurrente.
        public static RespuestaPasarela Cobrar(string token, decimal monto, string concepto)
        {
            if (!EsTokenConFormatoValido(token))
            {
                return Rechazo("El token de pago no es válido.");
            }

            if (monto <= 0)
            {
                return Rechazo("El monto a cobrar tiene que ser mayor que cero.");
            }

            if (token.IndexOf("rechaz", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Rechazo("El medio de pago fue rechazado por la entidad emisora.");
            }

            return Aprobacion();
        }

        /// Detiene los cobros futuros asociados al token.
        public static RespuestaPasarela CancelarCobroRecurrente(string token)
        {
            if (!EsTokenConFormatoValido(token))
            {
                return Rechazo("El token de pago no es válido.");
            }

            return Aprobacion();
        }

        private static RespuestaPasarela Aprobacion()
        {
            byte[] azar = new byte[3];
            using (RandomNumberGenerator generador = RandomNumberGenerator.Create())
            {
                generador.GetBytes(azar);
            }

            RespuestaPasarela respuesta = new RespuestaPasarela();
            respuesta.Aprobada = true;
            respuesta.CodigoTransaccion = "SIM-" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) +
                                          "-" + BitConverter.ToString(azar).Replace("-", string.Empty);
            respuesta.IpOrigen = IpPasarelaSimulada;
            return respuesta;
        }

        private static RespuestaPasarela Rechazo(string motivo)
        {
            RespuestaPasarela respuesta = new RespuestaPasarela();
            respuesta.Aprobada = false;
            respuesta.Motivo = motivo;
            respuesta.IpOrigen = IpPasarelaSimulada;
            return respuesta;
        }
    }
}
