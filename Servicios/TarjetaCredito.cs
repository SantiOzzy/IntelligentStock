using System;
using System.Globalization;
using System.Text;

namespace Servicios
{
    /// Validaciones de una tarjeta de crédito antes de mandarla a la pasarela:
    /// número (dígito de control de Luhn), marca, vencimiento y código de
    /// seguridad. Ninguno de estos datos se guarda: la pasarela devuelve un
    /// token y el sistema trabaja solo con ese token.
    public static class TarjetaCredito
    {
        public const string Visa = "Visa";
        public const string Mastercard = "Mastercard";
        public const string AmericanExpress = "American Express";

        /// Saca espacios y guiones: "4111 1111-1111 1111" -> "4111111111111111".
        public static string Normalizar(string numero)
        {
            StringBuilder limpio = new StringBuilder();
            foreach (char c in numero ?? string.Empty)
            {
                if (c != ' ' && c != '-')
                {
                    limpio.Append(c);
                }
            }
            return limpio.ToString();
        }

        /// Visa, Mastercard o American Express según los primeros números, o
        /// null si no es ninguna de las tres.
        public static string Marca(string numero)
        {
            numero = Normalizar(numero);
            if (numero.Length < 4 || !SoloDigitos(numero))
            {
                return null;
            }

            int dos = int.Parse(numero.Substring(0, 2), CultureInfo.InvariantCulture);
            int cuatro = int.Parse(numero.Substring(0, 4), CultureInfo.InvariantCulture);

            if (numero[0] == '4' && (numero.Length == 13 || numero.Length == 16 || numero.Length == 19))
            {
                return Visa;
            }
            if (((dos >= 51 && dos <= 55) || (cuatro >= 2221 && cuatro <= 2720)) && numero.Length == 16)
            {
                return Mastercard;
            }
            if ((dos == 34 || dos == 37) && numero.Length == 15)
            {
                return AmericanExpress;
            }
            return null;
        }

        /// Algoritmo de Luhn (ISO/IEC 7812): desde la derecha, se duplica uno
        /// de cada dos dígitos (restando 9 si da más de 9) y la suma tiene que
        /// ser múltiplo de 10. Detecta cualquier error en un solo dígito y la
        /// mayoría de las inversiones de dos dígitos seguidos.
        public static bool PasaLuhn(string numero)
        {
            numero = Normalizar(numero);
            if (numero.Length < 13 || numero.Length > 19 || !SoloDigitos(numero))
            {
                return false;
            }

            int suma = 0;
            bool duplicar = false;
            for (int i = numero.Length - 1; i >= 0; i--)
            {
                int digito = numero[i] - '0';
                if (duplicar)
                {
                    digito *= 2;
                    if (digito > 9)
                    {
                        digito -= 9;
                    }
                }
                suma += digito;
                duplicar = !duplicar;
            }
            return suma % 10 == 0;
        }

        /// Interpreta "MM/AA" o "MM/AAAA". Devuelve false si no tiene ese formato.
        public static bool LeerVencimiento(string texto, out int mes, out int anio)
        {
            mes = 0;
            anio = 0;
            string[] partes = (texto ?? string.Empty).Trim().Split('/');
            if (partes.Length != 2 ||
                !int.TryParse(partes[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out mes) ||
                !int.TryParse(partes[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out anio))
            {
                return false;
            }

            if (partes[1].Trim().Length == 2)
            {
                anio += 2000;
            }
            return mes >= 1 && mes <= 12 && anio >= 2000 && anio <= 2099;
        }

        /// La tarjeta vale hasta el último día del mes de vencimiento.
        public static bool EstaVencida(int mes, int anio, DateTime hoy)
        {
            DateTime ultimoDia = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));
            return ultimoDia < hoy.Date;
        }

        /// Tres dígitos (cuatro en American Express).
        public static bool CodigoSeguridadValido(string codigo, string marca)
        {
            codigo = (codigo ?? string.Empty).Trim();
            int largo = marca == AmericanExpress ? 4 : 3;
            return codigo.Length == largo && SoloDigitos(codigo);
        }

        public static string UltimosDigitos(string numero)
        {
            numero = Normalizar(numero);
            return numero.Length <= 4 ? numero : numero.Substring(numero.Length - 4);
        }

        private static bool SoloDigitos(string texto)
        {
            foreach (char c in texto)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }
            return texto.Length > 0;
        }
    }
}
