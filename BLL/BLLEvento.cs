using System;
using System.Collections.Generic;
using System.Configuration;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// R4 - Registrar en bitacora de eventos.
    /// Toda accion relevante del sistema pasa por aca: se guarda el usuario
    /// que la ejecuto, la fecha y hora, la accion, un detalle y la criticidad.
    public class BLLEvento
    {
        /// Cantidad de eventos que se muestran al abrir la bitacora.
        public const int CantidadInicial = 200;

        /// Cuantos registros conserva la bitácora si no se configura otro valor.
        private const int CantidadARetenerPorDefecto = 5000;

        private readonly DALEvento dalEvento = new DALEvento();

        public void Registrar(int? dniUsuario, string accion, string descripcion)
        {
            Registrar(dniUsuario, accion, descripcion, CriticidadEvento.Informativa);
        }

        public void Registrar(int? dniUsuario, string accion, string descripcion, CriticidadEvento criticidad)
        {
            // DNI 0 es el usuario de contingencia, que no existe en la tabla
            // Usuario: con ese DNI la clave foránea rechazaba el evento y se perdía.
            if (dniUsuario.HasValue && dniUsuario.Value == 0)
            {
                dniUsuario = null;
                descripcion = "(usuario de contingencia) " + descripcion;
            }

            try
            {
                dalEvento.RegistrarEvento(new BEEvento(dniUsuario, accion, descripcion, criticidad));
                AplicarRetencion();
            }
            catch (Exception)
            {
                // La bitacora nunca debe interrumpir la operacion del usuario.
                // El fallo de escritura se corresponde con el error #ERR015.
            }
        }

        /// Registra el evento a nombre del usuario que tiene la sesion activa.
        public void RegistrarDelUsuarioActual(string accion, string descripcion)
        {
            Registrar(SessionManager.DniActual, accion, descripcion, CriticidadEvento.Informativa);
        }

        public void RegistrarDelUsuarioActual(string accion, string descripcion, CriticidadEvento criticidad)
        {
            Registrar(SessionManager.DniActual, accion, descripcion, criticidad);
        }

        /// Política de retención: la bitácora conserva solo los últimos N
        /// eventos (configurable en Web.config, clave "CantidadEventosARetener").
        /// Se controla despues de cada alta para no necesitar un job aparte.
        private void AplicarRetencion()
        {
            int cantidadARetener;
            if (!int.TryParse(ConfigurationManager.AppSettings["CantidadEventosARetener"], out cantidadARetener) ||
                cantidadARetener <= 0)
            {
                cantidadARetener = CantidadARetenerPorDefecto;
            }

            if (dalEvento.ContarEventos() > cantidadARetener)
            {
                dalEvento.PurgarEventosAntiguos(cantidadARetener);
            }
        }

        /// Los N eventos mas recientes. Es la vista inicial de la bitacora.
        public List<BEEvento> TraerUltimos(int cantidad)
        {
            return dalEvento.TraerUltimosEventos(cantidad);
        }

        /// Los N eventos mas recientes de una acción determinada.
        public List<BEEvento> TraerUltimosPorAccion(string accion, int cantidad)
        {
            return dalEvento.TraerUltimosEventosPorAccion(accion, cantidad);
        }

        /// Eventos de una acción desde una fecha, del más viejo al más nuevo.
        public List<BEEvento> TraerEventosPorAccion(string accion, DateTime desde)
        {
            return dalEvento.TraerEventosPorAccionDesde(accion, desde);
        }

        public List<BEEvento> TraerTodos()
        {
            return dalEvento.TraerEventos();
        }

        /// Filtra la bitacora por rango de fechas, usuario, accion y criticidad.
        public List<BEEvento> Filtrar(DateTime fechaInicio, DateTime fechaFin, string nombreUsuario, string texto)
        {
            return Filtrar(fechaInicio, fechaFin, nombreUsuario, texto, null);
        }

        public List<BEEvento> Filtrar(DateTime fechaInicio, DateTime fechaFin, string nombreUsuario, string texto,
            CriticidadEvento? criticidad)
        {
            List<BEEvento> eventos = dalEvento.FiltrarEventos(fechaInicio.Date, fechaFin.Date.AddDays(1).AddSeconds(-1));

            if (!string.IsNullOrEmpty(nombreUsuario))
            {
                eventos = eventos.FindAll(delegate(BEEvento evento)
                {
                    return string.Equals(evento.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (!string.IsNullOrEmpty(texto))
            {
                string busqueda = texto.Trim();
                eventos = eventos.FindAll(delegate(BEEvento evento)
                {
                    return Contiene(evento.Accion, busqueda) || Contiene(evento.Descripcion, busqueda);
                });
            }

            if (criticidad.HasValue)
            {
                CriticidadEvento buscada = criticidad.Value;
                eventos = eventos.FindAll(delegate(BEEvento evento) { return evento.Criticidad == buscada; });
            }

            return eventos;
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return !string.IsNullOrEmpty(valor) &&
                   valor.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
