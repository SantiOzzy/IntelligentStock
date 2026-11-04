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

        /// Token que reemplaza a la tarjeta (solo en Tokenizar).
        public string Token { get; set; }

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
    ///
    /// Tarjetas de prueba para Tokenizar: cualquier Visa, Mastercard o American
    /// Express con el dígito de control correcto se aprueba (por ejemplo
    /// 4111 1111 1111 1111 o 5555 5555 5555 4444). La 4000 0000 0000 0002 da
    /// un token que la pasarela rechaza al cobrar.
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

        /// Tarjetas de prueba que la entidad emisora rechaza.
        private static readonly string[] TarjetasRechazadas = new string[] { "4000000000000002" };

        /// Recibe los datos de la tarjeta y devuelve un token que la representa.
        /// El sistema guarda solo el token (encriptado): el número y el código
        /// de seguridad no se guardan en ningún lado. En producción el formulario
        /// mandaría la tarjeta directo a la pasarela, sin pasar por el servidor.
        public static RespuestaPasarela Tokenizar(string numero, int mesVencimiento, int anioVencimiento,
                                                  string codigoSeguridad, string titular)
        {
            numero = TarjetaCredito.Normalizar(numero);
            string marca = TarjetaCredito.Marca(numero);

            if (marca == null || !TarjetaCredito.PasaLuhn(numero))
            {
                return Rechazo("El número de tarjeta no es válido.");
            }
            if (TarjetaCredito.EstaVencida(mesVencimiento, anioVencimiento, DateTime.Today))
            {
                return Rechazo("La tarjeta está vencida.");
            }
            if (!TarjetaCredito.CodigoSeguridadValido(codigoSeguridad, marca))
            {
                return Rechazo("El código de seguridad no es válido.");
            }
            if (string.IsNullOrEmpty(titular) || titular.Trim().Length == 0)
            {
                return Rechazo("Falta el nombre del titular.");
            }

            bool rechazada = Array.IndexOf(TarjetasRechazadas, numero) >= 0;

            RespuestaPasarela respuesta = Aprobacion();
            respuesta.Token = "tok_" + (rechazada ? "rechazada" + Azar(15) : Azar(24));
            return respuesta;
        }

        private static string Azar(int largo)
        {
            const string letras = "abcdefghijklmnopqrstuvwxyz0123456789";
            byte[] bytes = new byte[largo];
            using (RandomNumberGenerator generador = RandomNumberGenerator.Create())
            {
                generador.GetBytes(bytes);
            }
            char[] salida = new char[largo];
            for (int i = 0; i < largo; i++)
            {
                salida[i] = letras[bytes[i] % letras.Length];
            }
            return new string(salida);
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
