using System;
using System.Configuration;
using System.Net;

namespace Servicios
{
    /// R9 - Autenticar sistema externo.
    ///
    /// Antes de procesar un mensaje que llega de afuera (las lecturas de los
    /// sensores, la respuesta de la pasarela de cobros) se controla que venga
    /// de una dirección IP autorizada. La lista se configura en el Web.config,
    /// clave "IpsAutorizadasSistemasExternos", separada por punto y coma. Si la
    /// clave no está, solo se acepta el propio servidor (127.0.0.1 y ::1).
    public static class AutenticadorSistemaExterno
    {
        private const string ClaveConfiguracion = "IpsAutorizadasSistemasExternos";
        private const string IpsPorDefecto = "127.0.0.1;::1";

        public static bool EsOrigenAutorizado(string ip)
        {
            IPAddress origen;
            if (string.IsNullOrEmpty(ip) || !IPAddress.TryParse(ip.Trim(), out origen))
            {
                return false;
            }

            origen = Normalizar(origen);

            foreach (string autorizada in IpsAutorizadas())
            {
                IPAddress direccion;
                if (IPAddress.TryParse(autorizada, out direccion) && Normalizar(direccion).Equals(origen))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] IpsAutorizadas()
        {
            string configuradas = ConfigurationManager.AppSettings[ClaveConfiguracion];
            if (string.IsNullOrEmpty(configuradas) || configuradas.Trim().Length == 0)
            {
                configuradas = IpsPorDefecto;
            }

            string[] ips = configuradas.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ips.Length; i++)
            {
                ips[i] = ips[i].Trim();
            }
            return ips;
        }

        // Una IPv4 puede llegar escrita como IPv6 (::ffff:127.0.0.1).
        private static IPAddress Normalizar(IPAddress direccion)
        {
            return direccion.IsIPv4MappedToIPv6 ? direccion.MapToIPv4() : direccion;
        }
    }
}
