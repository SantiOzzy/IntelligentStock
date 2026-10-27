using System;
using System.Collections.Generic;

namespace Servicios
{
    /// Evita que un mismo hecho repetido llene la bitácora. Los servicios web
    /// registran un evento cada vez que rechazan un pedido (IP no autorizada,
    /// sensor desconocido); como cualquiera puede mandar pedidos, sin un límite
    /// alguien podría generar miles de eventos y, con la política de retención,
    /// hacer que se borren los eventos viejos que sí importan. Con este control
    /// el mismo hecho (misma clave) se registra como mucho una vez por intervalo.
    public static class LimitadorDeEventos
    {
        /// Tope de claves recordadas, para que la memoria no crezca sin límite.
        private const int MaximoDeClaves = 1000;

        private static readonly Dictionary<string, DateTime> ultimoRegistro =
            new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Queue<DateTime>> registrosRecientes =
            new Dictionary<string, Queue<DateTime>>(StringComparer.OrdinalIgnoreCase);

        private static readonly object candado = new object();

        /// Tope general para los rechazos de los servicios web: aunque lleguen
        /// pedidos desde muchas IP distintas, no se registran más de estos por hora.
        public const int TopeDeRechazosPorHora = 30;

        /// Devuelve true si el evento con esa clave se puede registrar ahora, es
        /// decir, si no se registró otro igual dentro del intervalo.
        public static bool Permitir(string clave, TimeSpan intervalo)
        {
            lock (candado)
            {
                DateTime ahora = DateTime.Now;
                DateTime ultimo;
                if (ultimoRegistro.TryGetValue(clave ?? string.Empty, out ultimo) && ahora - ultimo < intervalo)
                {
                    return false;
                }

                if (ultimoRegistro.Count >= MaximoDeClaves)
                {
                    ultimoRegistro.Clear();
                }

                ultimoRegistro[clave ?? string.Empty] = ahora;
                return true;
            }
        }

        /// Devuelve true si todavía no se registraron "maximo" eventos con esa
        /// clave dentro de la ventana de tiempo (y cuenta este). Sirve como tope
        /// general, para que cambiar de IP no alcance para llenar la bitácora.
        public static bool DentroDelTope(string clave, int maximo, TimeSpan ventana)
        {
            lock (candado)
            {
                DateTime ahora = DateTime.Now;
                Queue<DateTime> registros;
                if (!registrosRecientes.TryGetValue(clave ?? string.Empty, out registros))
                {
                    registros = new Queue<DateTime>();
                    registrosRecientes[clave ?? string.Empty] = registros;
                }

                while (registros.Count > 0 && ahora - registros.Peek() >= ventana)
                {
                    registros.Dequeue();
                }

                if (registros.Count >= maximo)
                {
                    return false;
                }

                registros.Enqueue(ahora);
                return true;
            }
        }

        /// Combina los dos controles para un rechazo de un servicio web: una vez
        /// por intervalo para la misma clave y, en total, no más del tope por hora.
        public static bool PermitirRechazo(string clave, TimeSpan intervalo)
        {
            return Permitir(clave, intervalo) &&
                   DentroDelTope("rechazos-servicios-web", TopeDeRechazosPorHora, TimeSpan.FromHours(1));
        }
    }
}
