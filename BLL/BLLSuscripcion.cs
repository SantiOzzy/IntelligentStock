using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de la suscripción SaaS (CU-008-044 y CU-008-045).
    ///
    /// El cobro lo hace la pasarela de cobros externa (Servicios.PasarelaPagos,
    /// simulada). El token de pago se guarda encriptado con AES-256 (R6) y se
    /// desencripta (R7) para mostrarlo enmascarado y para pedirle a la
    /// pasarela que detenga los cobros en la baja.
    public class BLLSuscripcion
    {
        private const string Tabla = "Suscripcion";

        /// Acción con la que queda la baja en la bitácora.
        private const string AccionBaja = "Baja de suscripción";

        /// Cuántos eventos de baja se leen para saber hasta cuándo sigue el servicio.
        private const int EventosDeBajaALeer = 500;

        /// "Se canceló la suscripción N° 5 (...) hasta el 09/11/2026."
        private static readonly Regex FormatoEventoDeBaja =
            new Regex(@"suscripci\S*n N\S*\s*(\d+)\b.*?hasta el (\d{2}/\d{2}/\d{4})", RegexOptions.Compiled);

        /// Hasta cuánto tiempo hacia adelante se puede programar el inicio.
        public const int DiasMaximosHastaElInicio = 365;

        /// Planes comerciales y su abono mensual en pesos.
        private static readonly string[] NombresPlanes = new string[] { "Básico", "Profesional", "Empresarial" };
        private static readonly decimal[] PreciosPlanes = new decimal[] { 15000m, 30000m, 60000m };

        private readonly DALSuscripcion dalSuscripcion = new DALSuscripcion();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        #region Planes

        public static List<KeyValuePair<string, decimal>> TraerPlanes()
        {
            List<KeyValuePair<string, decimal>> planes = new List<KeyValuePair<string, decimal>>();
            for (int i = 0; i < NombresPlanes.Length; i++)
            {
                planes.Add(new KeyValuePair<string, decimal>(NombresPlanes[i], PreciosPlanes[i]));
            }
            return planes;
        }

        /// Abono mensual del plan, o 0 si el plan no existe.
        public static decimal PrecioDelPlan(string plan)
        {
            int indice = IndiceDelPlan(plan);
            return indice < 0 ? 0m : PreciosPlanes[indice];
        }

        /// Nombre del plan tal como figura en la lista, aunque llegue escrito en
        /// minúsculas o sin acento (por ejemplo, "basico" desde el servicio web).
        /// Devuelve null si el plan no existe.
        public static string NombreDelPlan(string plan)
        {
            int indice = IndiceDelPlan(plan);
            return indice < 0 ? null : NombresPlanes[indice];
        }

        private static int IndiceDelPlan(string plan)
        {
            if (string.IsNullOrEmpty(plan))
            {
                return -1;
            }

            CompareInfo comparador = CultureInfo.InvariantCulture.CompareInfo;
            for (int i = 0; i < NombresPlanes.Length; i++)
            {
                if (comparador.Compare(NombresPlanes[i], plan.Trim(),
                        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        /// La suscripción se cobra por mes desde la fecha de inicio (el primer mes
        /// se cobra en el alta). Al cancelar, el servicio sigue hasta el próximo
        /// vencimiento mensual: el cierre del período que ya se pagó. Si todavía
        /// no empezó, se respeta el primer mes completo, que ya está cobrado.
        public static DateTime CalcularCierreDelPeriodo(DateTime fechaInicio, DateTime hoy)
        {
            DateTime inicio = fechaInicio.Date;

            int meses = 1;
            while (inicio.AddMonths(meses) <= hoy.Date)
            {
                meses++;
            }
            return inicio.AddMonths(meses);
        }

        #endregion

        #region Consultas

        public List<BESuscripcion> TraerSuscripciones()
        {
            List<BESuscripcion> suscripciones = dalSuscripcion.TraerSuscripciones();
            Dictionary<int, DateTime> finDelServicio = null;

            foreach (BESuscripcion suscripcion in suscripciones)
            {
                Completar(suscripcion);

                if (!suscripcion.Activa)
                {
                    if (finDelServicio == null)
                    {
                        finDelServicio = LeerFinesDeServicio();
                    }

                    DateTime hasta;
                    if (finDelServicio.TryGetValue(suscripcion.IdSuscripcion, out hasta))
                    {
                        suscripcion.ServicioHasta = hasta;
                    }
                }
            }
            return suscripciones;
        }

        /// La tabla Suscripcion no guarda la fecha de baja (no se agregan
        /// columnas): el cierre del período queda escrito en el evento de baja de
        /// la bitácora, y de ahí se lee. Si el evento ya se depuró, la grilla
        /// muestra la suscripción como "Dada de baja" sin fecha.
        private Dictionary<int, DateTime> LeerFinesDeServicio()
        {
            Dictionary<int, DateTime> fines = new Dictionary<int, DateTime>();
            try
            {
                // Se recorren del más nuevo al más viejo: queda el más reciente.
                foreach (BEEvento evento in bllEvento.TraerUltimosPorAccion(AccionBaja, EventosDeBajaALeer))
                {
                    Match m = FormatoEventoDeBaja.Match(evento.Descripcion ?? string.Empty);
                    DateTime hasta;
                    if (m.Success &&
                        DateTime.TryParseExact(m.Groups[2].Value, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out hasta))
                    {
                        int id = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                        if (!fines.ContainsKey(id))
                        {
                            fines[id] = hasta;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Sin la bitácora la grilla igual se muestra, sin la fecha.
            }
            return fines;
        }

        /// Suscripciones que se pueden dar de baja.
        public List<BESuscripcion> TraerActivas()
        {
            return TraerSuscripciones().FindAll(delegate(BESuscripcion s) { return s.Activa; });
        }

        public BESuscripcion ObtenerPorId(int idSuscripcion)
        {
            BESuscripcion suscripcion = idSuscripcion <= 0 ? null : dalSuscripcion.ObtenerPorId(idSuscripcion);
            if (suscripcion != null)
            {
                Completar(suscripcion);
            }
            return suscripcion;
        }

        #endregion

        #region Alta y baja

        /// CU-008-044 Alta de suscripción, a nombre del usuario con la sesión activa.
        public BESuscripcion Alta(string plan, string token, DateTime? fechaInicio)
        {
            return Alta(plan, token, fechaInicio, SessionManager.DniActual);
        }

        /// CU-008-044 Alta de suscripción. Valida los datos, cobra el primer
        /// período en la pasarela y, si lo aprueba, graba la suscripción como
        /// Activa con el token encriptado. El DNI es el del usuario que la pide
        /// (lo indica el servicio web cuando la solicitud llega por ahí).
        public BESuscripcion Alta(string plan, string token, DateTime? fechaInicio, int? dniUsuario)
        {
            plan = plan == null ? string.Empty : plan.Trim();
            token = token == null ? string.Empty : token.Trim();

            if (plan.Length == 0 || token.Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            if (NombreDelPlan(plan) == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El plan elegido no existe."));
            }

            plan = NombreDelPlan(plan);
            decimal abono = PrecioDelPlan(plan);

            if (!PasarelaPagos.EsTokenConFormatoValido(token))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El token de pago tiene que empezar con \"tok_\" seguido de 16 a 60 letras o números."));
            }

            DateTime inicio = fechaInicio.HasValue ? fechaInicio.Value.Date : DateTime.Today;
            if (inicio < DateTime.Today)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de inicio no puede ser anterior a hoy."));
            }

            if (inicio > DateTime.Today.AddDays(DiasMaximosHastaElInicio))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de inicio no puede estar a más de un año."));
            }

            // R6: se encripta antes de cobrar. Si la clave AES del Web.config
            // falta o es inválida, la operación se corta acá, sin haber cobrado.
            string tokenEncriptado = Encriptacion.EncriptarAES256(token);

            List<BESuscripcion> existentes = TraerSuscripciones();

            BESuscripcion activa = existentes.Find(delegate(BESuscripcion s) { return s.Activa; });
            if (activa != null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "Ya hay una suscripción activa (N° " + activa.IdSuscripcion + ", plan " + activa.TipoPlan +
                    "). Dela de baja antes de contratar otra."));
            }

            // Un token de pago se usa una sola vez.
            foreach (BESuscripcion existente in existentes)
            {
                if (TokenDesencriptado(existente) == token)
                {
                    RegistrarRechazo(dniUsuario, plan, "el token de pago ya fue utilizado");
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                        "El token de pago ya fue utilizado en la suscripción N° " + existente.IdSuscripcion +
                        ". Cada pago genera un token nuevo en la pasarela."));
                }
            }

            // R4 antes de cobrar: si la tabla no está íntegra la suscripción no
            // se va a poder grabar, y no tiene sentido cobrar.
            bllDV.VerificarAntesDeModificar(Tabla);

            RespuestaPasarela respuesta = PasarelaPagos.Cobrar(token, abono, "IntelligentStock - plan " + plan);

            // R9: la respuesta solo se da por buena si viene de un origen autorizado.
            if (!AutenticadorSistemaExterno.EsOrigenAutorizado(respuesta.IpOrigen))
            {
                RegistrarRechazo(dniUsuario, plan, "la respuesta de la pasarela llegó desde la IP no autorizada " +
                                                   respuesta.IpOrigen);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR024",
                    "La respuesta de la pasarela no proviene de un origen autorizado."));
            }

            if (!respuesta.Aprobada)
            {
                RegistrarRechazo(dniUsuario, plan, respuesta.Motivo);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR024", respuesta.Motivo));
            }

            BESuscripcion suscripcion = new BESuscripcion();
            suscripcion.TipoPlan = plan;
            suscripcion.TokenPago = tokenEncriptado;
            suscripcion.Estado = BESuscripcion.EstadoActiva;
            suscripcion.FechaInicio = inicio;

            // Si la grabación falla, en un sistema real habría que pedirle a la
            // pasarela que anule el cobro; la pasarela simulada no lo necesita.
            suscripcion.IdSuscripcion = bllDV.EjecutarControlado(() => dalSuscripcion.Crear(suscripcion), Tabla);
            suscripcion.CodigoTransaccion = respuesta.CodigoTransaccion;
            Completar(suscripcion);

            bllEvento.Registrar(dniUsuario, "Alta de suscripción",
                "Se activó la suscripción N° " + suscripcion.IdSuscripcion + " al plan " + plan + " (" +
                suscripcion.AbonoTexto + " por mes) desde el " + suscripcion.FechaInicioTexto +
                ". Transacción de la pasarela: " + respuesta.CodigoTransaccion + ".");

            return suscripcion;
        }

        /// CU-008-045 Baja de suscripción, a nombre del usuario con la sesión activa.
        public DateTime Baja(int idSuscripcion, out bool pasarelaNotificada)
        {
            return Baja(idSuscripcion, SessionManager.DniActual, out pasarelaNotificada);
        }

        public DateTime Baja(int idSuscripcion, int? dniUsuario)
        {
            bool pasarelaNotificada;
            return Baja(idSuscripcion, dniUsuario, out pasarelaNotificada);
        }

        /// CU-008-045 Baja de suscripción. Le pide a la pasarela que detenga los
        /// cobros y la marca como Cancelada (baja lógica). Devuelve hasta qué
        /// fecha sigue habilitado el servicio (cierre del período en curso). Si
        /// el token no se puede desencriptar (por ejemplo, porque cambió la clave
        /// AES del Web.config) la suscripción se cancela igual en el sistema y
        /// pasarelaNotificada queda en false: hay que pedir la baja de los cobros
        /// a la pasarela por otro medio.
        public DateTime Baja(int idSuscripcion, int? dniUsuario, out bool pasarelaNotificada)
        {
            pasarelaNotificada = false;

            BESuscripcion suscripcion = ObtenerPorId(idSuscripcion);
            if (suscripcion == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!suscripcion.Activa)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La suscripción N° " + idSuscripcion + " ya está " + suscripcion.Estado.ToLower() + "."));
            }

            // R4 antes de avisarle a la pasarela: si la baja no se va a poder
            // grabar, no hay que cortar los cobros.
            bllDV.VerificarAntesDeModificar(Tabla);

            string token = TokenDesencriptado(suscripcion);
            if (!string.IsNullOrEmpty(token))
            {
                RespuestaPasarela respuesta = PasarelaPagos.CancelarCobroRecurrente(token);
                if (!AutenticadorSistemaExterno.EsOrigenAutorizado(respuesta.IpOrigen))
                {
                    throw new ApplicationException(
                        "La respuesta de la pasarela de cobros no proviene de un origen autorizado. " +
                        "La suscripción sigue activa.");
                }
                if (!respuesta.Aprobada)
                {
                    throw new ApplicationException(
                        "La pasarela de cobros no confirmó la cancelación de los cobros: " + respuesta.Motivo +
                        " La suscripción sigue activa.");
                }
                pasarelaNotificada = true;
            }

            bllDV.EjecutarControlado(
                () => dalSuscripcion.CambiarEstado(idSuscripcion, BESuscripcion.EstadoCancelada), Tabla);

            DateTime cierre = CalcularCierreDelPeriodo(suscripcion.FechaInicio, DateTime.Today);

            bllEvento.Registrar(dniUsuario, AccionBaja,
                "Se canceló la suscripción N° " + idSuscripcion + " (plan " + suscripcion.TipoPlan +
                "). Se detuvieron los cobros; el servicio sigue habilitado hasta el " +
                cierre.ToString("dd/MM/yyyy") + ".");

            if (!pasarelaNotificada)
            {
                bllEvento.Registrar(dniUsuario, "Baja de suscripción sin aviso a la pasarela",
                    "No se pudo desencriptar el token de pago de la suscripción N° " + idSuscripcion +
                    " (¿cambió la clave AES del Web.config?). Hay que pedir la baja de los cobros a la " +
                    "pasarela por otro medio.", CriticidadEvento.Advertencia);
            }

            return cierre;
        }

        #endregion

        #region Auxiliares

        private static void Completar(BESuscripcion suscripcion)
        {
            suscripcion.AbonoMensual = PrecioDelPlan(suscripcion.TipoPlan);

            if (suscripcion.Activa && suscripcion.FechaInicio > DateTime.MinValue)
            {
                suscripcion.ProximoCobro = CalcularCierreDelPeriodo(suscripcion.FechaInicio, DateTime.Today);
            }

            string token = TokenDesencriptado(suscripcion);
            suscripcion.TokenEnmascarado = token == null ? "(no disponible)" : Enmascarar(token);
        }

        /// R7: devuelve el token en claro, o null si no se puede desencriptar.
        private static string TokenDesencriptado(BESuscripcion suscripcion)
        {
            try
            {
                return Encriptacion.DesencriptarAES256(suscripcion.TokenPago);
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// Deja ver solo el prefijo y los últimos cuatro caracteres.
        private static string Enmascarar(string token)
        {
            if (token.Length <= 8)
            {
                return "****";
            }
            return token.Substring(0, 4) + "****" + token.Substring(token.Length - 4);
        }

        private void RegistrarRechazo(int? dniUsuario, string plan, string motivo)
        {
            bllEvento.Registrar(dniUsuario, "Pago rechazado",
                "No se pudo activar la suscripción al plan " + plan + ": " + motivo + ". [#ERR024]",
                CriticidadEvento.Advertencia);
        }

        #endregion
    }
}
