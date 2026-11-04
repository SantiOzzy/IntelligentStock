using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// R4 - Registrar en bitacora de eventos.
    /// Toda accion relevante del sistema pasa por aca: se guarda el usuario
    /// que la ejecuto, la fecha y hora, la accion, un detalle y la criticidad.
    ///
    /// Política de la bitácora (CU-003-016):
    ///   - Los eventos de los últimos 30 días (Web.config, "DiasEventosActivos")
    ///     están activos: son los que muestra la bitácora.
    ///   - Los anteriores pasan a eliminados con eliminación lógica (columna
    ///     activo = 0): no se borran de la tabla. Siguen disponibles para lo que
    ///     el sistema vuelve a leer de la bitácora (las ventas de la predicción
    ///     de demanda y del reporte gerencial, las altas y bajas de suscripción,
    ///     los ajustes de stock) y se ven en la pantalla tildando "Incluir
    ///     eventos eliminados".
    ///   - Antes de marcarlos se archivan en un XML por mes
    ///     (App_Data\Bitacora\Bitacora_AAAA-MM.xml, ver DALArchivoBitacora),
    ///     que se descarga desde la bitácora.
    public class BLLEvento
    {
        /// Cantidad de eventos que se muestran al abrir la bitacora.
        public const int CantidadInicial = 200;

        /// Días que un evento queda activo si no se configura otro valor.
        public const int DiasActivosPorDefecto = 30;

        public const string AccionMantenimiento = "Mantenimiento de bitácora";

        private static readonly object candadoMantenimiento = new object();

        /// Día en que se hizo el último mantenimiento. Los eventos vencen por
        /// fecha, así que alcanza con revisarlos una vez por día.
        private static DateTime ultimoMantenimiento = DateTime.MinValue;

        private readonly DALEvento dalEvento = new DALEvento();
        private readonly DALArchivoBitacora dalArchivo = new DALArchivoBitacora();

        /// Cantidad de días que los eventos quedan activos.
        public static int DiasActivos
        {
            get
            {
                int dias;
                if (!int.TryParse(ConfigurationManager.AppSettings["DiasEventosActivos"], out dias) || dias <= 0)
                {
                    dias = DiasActivosPorDefecto;
                }
                return dias;
            }
        }

        /// Desde qué fecha los eventos están activos: los de antes pasan a eliminados.
        public static DateTime InicioVentanaActiva
        {
            get { return DateTime.Today.AddDays(-DiasActivos); }
        }

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
                AplicarPolitica();
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
            Registrar(SessionManager.ObtenerInstancia().DniActual, accion, descripcion, CriticidadEvento.Informativa);
        }

        public void RegistrarDelUsuarioActual(string accion, string descripcion, CriticidadEvento criticidad)
        {
            Registrar(SessionManager.ObtenerInstancia().DniActual, accion, descripcion, criticidad);
        }

        /// Se controla al registrar un evento, para no necesitar un proceso
        /// aparte, pero una sola vez por día.
        private void AplicarPolitica()
        {
            if (ultimoMantenimiento == DateTime.Today)
            {
                return;
            }

            lock (candadoMantenimiento)
            {
                if (ultimoMantenimiento == DateTime.Today)
                {
                    return;
                }

                // Se anota antes de empezar: el mantenimiento registra su propio
                // evento (y así no vuelve a entrar acá), y si algo falla se
                // reintenta al día siguiente y no en cada evento.
                ultimoMantenimiento = DateTime.Today;
                EliminarEventosVencidos();
            }
        }

        /// Pasa a eliminados (eliminación lógica) los eventos de más de
        /// DiasActivos días, después de archivarlos en el XML de su mes.
        /// Devuelve cuántos eventos se marcaron.
        public int EliminarEventosVencidos()
        {
            DateTime limite = InicioVentanaActiva;
            List<BEEvento> vencidos = dalEvento.TraerEventosVencidos(limite);
            if (vencidos.Count == 0)
            {
                return 0;
            }

            // Primero el archivo: si no se puede escribir, los eventos siguen
            // activos y no se pierde nada.
            List<string> archivos = dalArchivo.Archivar(vencidos);

            int hastaId = 0;
            foreach (BEEvento evento in vencidos)
            {
                hastaId = Math.Max(hastaId, evento.IdEvento);
            }

            int marcados = dalEvento.EliminarEventosLogicamente(limite, hastaId);

            string destino = archivos.Count <= 3
                ? string.Join(", ", archivos)
                : archivos[0] + " a " + archivos[archivos.Count - 1] + " (" + archivos.Count + " archivos)";

            Registrar(null, AccionMantenimiento,
                (marcados == 1 ? "1 evento anterior" : marcados + " eventos anteriores") + " al " +
                limite.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) +
                (marcados == 1 ? " pasó" : " pasaron") + " a eliminados (eliminación lógica, " +
                DiasActivos + " días activos) después de archivarse en " + destino + ".");

            return marcados;
        }

        /// Los N eventos activos mas recientes. Es la vista inicial de la bitacora.
        public List<BEEvento> TraerUltimos(int cantidad)
        {
            return TraerUltimos(cantidad, false);
        }

        /// Los N eventos mas recientes; con incluirEliminados, también los que
        /// ya salieron de la ventana de días activos.
        public List<BEEvento> TraerUltimos(int cantidad, bool incluirEliminados)
        {
            return dalEvento.TraerUltimosEventos(cantidad, incluirEliminados);
        }

        /// Los N eventos mas recientes de una acción determinada, activos o
        /// eliminados: son datos que el sistema vuelve a leer.
        public List<BEEvento> TraerUltimosPorAccion(string accion, int cantidad)
        {
            return dalEvento.TraerUltimosEventosPorAccion(accion, cantidad);
        }

        /// Eventos de una acción desde una fecha, del más viejo al más nuevo,
        /// activos o eliminados.
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
            return Filtrar(fechaInicio, fechaFin, nombreUsuario, texto, criticidad, false);
        }

        public List<BEEvento> Filtrar(DateTime fechaInicio, DateTime fechaFin, string nombreUsuario, string texto,
            CriticidadEvento? criticidad, bool incluirEliminados)
        {
            List<BEEvento> eventos = dalEvento.FiltrarEventos(fechaInicio.Date,
                fechaFin.Date.AddDays(1).AddSeconds(-1), incluirEliminados);

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

        /// Archivos XML mensuales de la bitácora, del más nuevo al más viejo.
        public List<string> ListarArchivos()
        {
            return dalArchivo.Listar();
        }

        /// Filtrado de eventos de un mes archivado, con una consulta XPath sobre
        /// el XML (no pasa por la base). Los filtros vacíos no se aplican.
        /// consulta devuelve la expresión XPath que se usó.
        public List<BEEvento> FiltrarArchivo(string nombreArchivo, string nombreUsuario, CriticidadEvento? criticidad,
            string texto, out string consulta)
        {
            consulta = DALArchivoBitacora.ConsultaXPath(nombreUsuario,
                criticidad.HasValue ? criticidad.Value.ToString() : null, texto);

            List<BEEvento> eventos = dalArchivo.Filtrar(nombreArchivo, consulta);
            if (eventos == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013",
                    "No se encontró el archivo " + nombreArchivo + "."));
            }
            return eventos;
        }

        /// Contenido de un archivo mensual, o null si no existe.
        public byte[] LeerArchivo(string nombreArchivo)
        {
            return dalArchivo.Leer(nombreArchivo);
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return !string.IsNullOrEmpty(valor) &&
                   valor.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
