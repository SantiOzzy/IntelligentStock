using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Servicios
{
    /// R5 - Encriptar datos mediante SHA-256.
    /// Encriptacion irreversible utilizada tanto para las contrasenas
    /// como para el calculo de los dígitos verificadores.
    ///
    /// R6 / R7 - Encriptar y desencriptar datos mediante AES-256.
    /// Encriptación reversible para los datos que hay que poder recuperar,
    /// como el token de pago de la suscripción.
    public static class Encriptacion
    {
        /// Clave de 256 bits (32 bytes en Base64) en el Web.config.
        private const string ClaveConfiguracionAES = "ClaveAES256";
        public static string EncriptarSHA256(string texto)
        {
            if (texto == null)
            {
                texto = string.Empty;
            }

            using (SHA256 hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(texto));
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// Genera un valor aleatorio impredecible (32 bytes en hexadecimal) para
        /// usarlo como enlace de recuperación de contraseña. Se usa el generador
        /// criptográfico y no Random, que es predecible a partir de su semilla.
        public static string GenerarTokenAleatorio()
        {
            byte[] bytes = new byte[32];
            using (RandomNumberGenerator generador = RandomNumberGenerator.Create())
            {
                generador.GetBytes(bytes);
            }

            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        /// R6 - Encripta con AES-256 (modo CBC). Cada vez se genera un vector de
        /// inicialización aleatorio, que se guarda delante del texto cifrado:
        /// así el mismo dato encriptado dos veces no da el mismo resultado.
        /// Devuelve IV + cifrado en Base64.
        public static string EncriptarAES256(string texto)
        {
            byte[] datos = Encoding.UTF8.GetBytes(texto ?? string.Empty);

            using (Aes aes = CrearAES())
            {
                aes.GenerateIV();

                using (ICryptoTransform encriptador = aes.CreateEncryptor())
                {
                    byte[] cifrado = encriptador.TransformFinalBlock(datos, 0, datos.Length);

                    byte[] resultado = new byte[aes.IV.Length + cifrado.Length];
                    Buffer.BlockCopy(aes.IV, 0, resultado, 0, aes.IV.Length);
                    Buffer.BlockCopy(cifrado, 0, resultado, aes.IV.Length, cifrado.Length);
                    return Convert.ToBase64String(resultado);
                }
            }
        }

        /// R7 - Desencripta un valor generado por EncriptarAES256. Lanza
        /// CryptographicException si el dato fue alterado o si la clave del
        /// Web.config no es la misma con la que se encriptó.
        public static string DesencriptarAES256(string textoEncriptado)
        {
            if (string.IsNullOrEmpty(textoEncriptado))
            {
                return string.Empty;
            }

            byte[] datos;
            try
            {
                datos = Convert.FromBase64String(textoEncriptado);
            }
            catch (FormatException)
            {
                throw new CryptographicException("El dato no está encriptado con AES-256.");
            }

            using (Aes aes = CrearAES())
            {
                int largoIV = aes.BlockSize / 8;
                if (datos.Length <= largoIV)
                {
                    throw new CryptographicException("El dato encriptado está incompleto.");
                }

                byte[] iv = new byte[largoIV];
                Buffer.BlockCopy(datos, 0, iv, 0, largoIV);
                aes.IV = iv;

                using (ICryptoTransform desencriptador = aes.CreateDecryptor())
                {
                    byte[] texto = desencriptador.TransformFinalBlock(datos, largoIV, datos.Length - largoIV);
                    return Encoding.UTF8.GetString(texto);
                }
            }
        }

        private static Aes CrearAES()
        {
            string claveBase64 = ConfigurationManager.AppSettings[ClaveConfiguracionAES];
            if (string.IsNullOrEmpty(claveBase64))
            {
                throw new InvalidOperationException(
                    "Falta la clave " + ClaveConfiguracionAES + " en el Web.config.");
            }

            byte[] clave;
            try
            {
                clave = Convert.FromBase64String(claveBase64.Trim());
            }
            catch (FormatException)
            {
                clave = null;
            }

            if (clave == null || clave.Length != 32)
            {
                throw new InvalidOperationException(
                    "La clave " + ClaveConfiguracionAES + " del Web.config tiene que ser de 32 bytes en Base64.");
            }

            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = clave;
            return aes;
        }
    }
}
