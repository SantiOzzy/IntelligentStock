using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de la suscripción SaaS (CU-008-044 y CU-008-045).
    ///
    /// La compra del cliente es la compra de un plan de IntelligentStock: el
    /// local elige el plan y cuántos meses paga por adelantado (con descuento
    /// a partir de 3 meses), paga al contado o con tarjeta de crédito (en 1, 3
    /// o 6 cuotas sin interés) y recibe la factura A o B en PDF, por correo.
    ///
    /// El cobro con tarjeta lo hace la pasarela de cobros externa
    /// (Servicios.PasarelaPagos, simulada), que primero cambia la tarjeta por un
    /// token. El token se guarda encriptado con AES-256 (R6) y se desencripta
    /// (R7) para cobrar el abono de cada mes, para mostrarlo enmascarado y para
    /// pedirle a la pasarela que detenga los cobros en la baja. La factura la
    /// autoriza el servicio de factura electrónica
    /// (Servicios.FacturacionElectronica, simulado).
    ///
    /// Cobro mensual: la columna pagadoHasta guarda el día en que vence lo
    /// pagado. Con tarjeta, ese día se cobra el abono del mes siguiente con el
    /// token guardado, se emite la factura y se manda por correo
    /// (CobrarAbonosVencidos). Lo revisa el primer inicio de sesión de cada
    /// día (RevisarCobrosDelDia). Si la pasarela rechaza el cobro, se avisa
    /// por correo y en la bitácora y se vuelve a intentar al día siguiente.
    public class BLLSuscripcion
    {
        private const string Tabla = "Suscripcion";

        /// Acción con la que queda la baja en la bitácora.
        private const string AccionBaja = "Baja de suscripción";

        /// Acción con la que queda el alta (la compra) en la bitácora.
        private const string AccionAlta = "Alta de suscripción";

        /// Acción con la que queda cada cobro mensual (o su rechazo) en la bitácora.
        public const string AccionCobro = "Cobro mensual de suscripción";

        /// Acción con la que queda la factura en la bitácora.
        public const string AccionFactura = "Factura de venta";

        /// Meses atrasados que se cobran como máximo en una revisión (si nadie
        /// entró al sistema en mucho tiempo, el resto se cobra en las siguientes).
        private const int MesesMaximosPorRevision = 12;

        /// Revisión diaria de los cobros: una sola por día y de a un pedido a la vez.
        private static readonly object candadoCobros = new object();
        private static DateTime ultimaRevisionDeCobros = DateTime.MinValue;
        private static DateTime proximoIntentoDeCobros = DateTime.MinValue;

        /// Dos facturas al mismo tiempo no pueden tomar el mismo número.
        private static readonly object candadoNumeracion = new object();

        /// Meses que se pueden pagar por adelantado y su descuento (en %).
        public static readonly int[] MesesPermitidos = new int[] { 1, 3, 6, 12 };
        private static readonly decimal[] DescuentosPorMeses = new decimal[] { 0m, 5m, 10m, 15m };

        /// Cuotas sin interés con tarjeta de crédito.
        public static readonly int[] CuotasPermitidas = new int[] { 1, 3, 6 };

        public static readonly string[] CondicionesIva = new string[]
        {
            BECompraPlan.CondicionResponsableInscripto, BECompraPlan.CondicionMonotributista,
            BECompraPlan.CondicionExento, BECompraPlan.CondicionConsumidorFinal
        };

        public const int LargoRazonSocial = 100;
        public const int LargoDomicilio = 150;
        public const int LargoCorreo = 100;

        /// Hasta cuánto tiempo hacia adelante se puede programar el inicio.
        public const int DiasMaximosHastaElInicio = 365;

        /// Planes comerciales y su abono mensual en dólares (USD), sin IVA.
        private static readonly string[] NombresPlanes = new string[] { "Plata", "Oro", "Platino" };
        private static readonly decimal[] PreciosPlanes = new decimal[] { 50m, 100m, 200m };

        private readonly DALSuscripcion dalSuscripcion = new DALSuscripcion();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();
        private readonly DALArchivoFactura dalArchivoFactura = new DALArchivoFactura();

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

        #endregion

        #region Consultas

        public List<BESuscripcion> TraerSuscripciones()
        {
            List<BESuscripcion> suscripciones = dalSuscripcion.TraerSuscripciones();
            foreach (BESuscripcion suscripcion in suscripciones)
            {
                Completar(suscripcion);
            }
            return suscripciones;
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
            return Alta(plan, token, fechaInicio, SessionManager.ObtenerInstancia().DniActual);
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
            // Un mes, con el IVA, igual que una compra de un mes con tarjeta.
            decimal abono = Calcular(plan, 1).Total;

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
            ControlarQueNoHayaOtraActiva(existentes);

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
            suscripcion.PagadoHasta = inicio.AddMonths(1);

            // Si la grabación falla, en un sistema real habría que pedirle a la
            // pasarela que anule el cobro; la pasarela simulada no lo necesita.
            suscripcion.IdSuscripcion = bllDV.EjecutarControlado(() => dalSuscripcion.Crear(suscripcion), Tabla);
            suscripcion.CodigoTransaccion = respuesta.CodigoTransaccion;
            Completar(suscripcion);

            bllEvento.Registrar(dniUsuario, AccionAlta,
                "Se activó la suscripción N° " + suscripcion.IdSuscripcion + " al plan " + plan + " por 1 mes (" +
                BECompraPlan.Importe(abono) + " con IVA) desde el " + suscripcion.FechaInicioTexto +
                ", pedida por el servicio web (sin factura). Transacción de la pasarela: " +
                respuesta.CodigoTransaccion + ".");

            return suscripcion;
        }

        private static void ControlarQueNoHayaOtraActiva(List<BESuscripcion> existentes)
        {
            BESuscripcion activa = existentes.Find(delegate(BESuscripcion s) { return s.Activa; });
            if (activa != null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "Ya hay una suscripción activa (N° " + activa.IdSuscripcion + ", plan " + activa.TipoPlan +
                    "). Dela de baja antes de contratar otra."));
            }
        }

        /// CU-008-045 Baja de suscripción, a nombre del usuario con la sesión activa.
        public DateTime Baja(int idSuscripcion, out bool pasarelaNotificada)
        {
            return Baja(idSuscripcion, SessionManager.ObtenerInstancia().DniActual, out pasarelaNotificada);
        }

        public DateTime Baja(int idSuscripcion, int? dniUsuario)
        {
            bool pasarelaNotificada;
            return Baja(idSuscripcion, dniUsuario, out pasarelaNotificada);
        }

        /// CU-008-045 Baja de suscripción. Le pide a la pasarela que detenga los
        /// cobros y la marca como Cancelada (baja lógica). Devuelve hasta qué
        /// día sigue habilitado el servicio (el último día ya pagado). Si
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

            // Al contado no hay cobros automáticos: no hay nada que avisarle a la pasarela.
            bool alContado = suscripcion.MedioPago == BECompraPlan.MedioContado;
            if (alContado)
            {
                pasarelaNotificada = true;
            }

            string token = alContado ? null : TokenDesencriptado(suscripcion);
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

            // El servicio sigue hasta el último día ya pagado: al contado, el
            // final de los meses comprados; con tarjeta, el del último abono
            // cobrado.
            DateTime cierre = suscripcion.PagadoHasta == DateTime.MinValue
                ? DateTime.Today
                : suscripcion.UltimoDiaPagado;

            bllEvento.Registrar(dniUsuario, AccionBaja,
                "Se canceló la suscripción N° " + idSuscripcion + " (plan " + suscripcion.TipoPlan +
                (alContado
                    ? ", pagada al contado). No había cobros automáticos; el servicio sigue habilitado hasta el "
                    : "). Se detuvieron los cobros; el servicio sigue habilitado hasta el ") +
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

        #region Compra del plan

        /// Calcula la compra: precio mensual x meses, descuento por pago
        /// adelantado, neto gravado, IVA 21 % y total. Los importes se redondean
        /// a dos decimales en cada paso, como en la factura.
        public static BECompraPlan Calcular(string plan, int meses)
        {
            string nombre = NombreDelPlan(plan);
            if (nombre == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025", "El plan elegido no existe."));
            }

            int indice = Array.IndexOf(MesesPermitidos, meses);
            if (indice < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Se pueden pagar por adelantado 1, 3, 6 o 12 meses."));
            }

            BECompraPlan compra = new BECompraPlan();
            compra.Plan = nombre;
            compra.Meses = meses;
            compra.PrecioMensual = PrecioDelPlan(nombre);
            compra.Subtotal = Redondear(compra.PrecioMensual * meses);
            compra.PorcentajeDescuento = DescuentosPorMeses[indice];
            compra.Descuento = Redondear(compra.Subtotal * compra.PorcentajeDescuento / 100m);
            compra.NetoGravado = compra.Subtotal - compra.Descuento;
            compra.Iva = Redondear(compra.NetoGravado * BECompraPlan.AlicuotaIva);
            compra.Total = compra.NetoGravado + compra.Iva;
            compra.Cuotas = 1;
            return compra;
        }

        /// Porcentaje de descuento por pagar esa cantidad de meses por adelantado.
        public static decimal DescuentoPorMeses(int meses)
        {
            int indice = Array.IndexOf(MesesPermitidos, meses);
            return indice < 0 ? 0m : DescuentosPorMeses[indice];
        }

        /// CU-008-044 Compra de un plan (alta de suscripción), a nombre del
        /// usuario con la sesión activa.
        ///
        /// Patrón Facade: la pantalla llama solo a este método, que coordina los
        /// subsistemas de la compra (TarjetaCredito, PasarelaPagos,
        /// FacturacionElectronica, GeneradorFactura, DALArchivoFactura,
        /// EnviadorCorreo, el dígito verificador y la bitácora) en el orden
        /// correcto. La pantalla no conoce a ninguno de ellos.
        ///
        /// El pedido trae el plan, los meses, la fecha de inicio, los datos de
        /// facturación del cliente, el medio de pago y las cuotas. Los datos de
        /// la tarjeta llegan aparte porque no se guardan: se validan, se le
        /// pasan a la pasarela y se descartan.
        ///
        /// Pasos: validar, calcular, controlar que no haya otra suscripción
        /// activa, verificar el dígito verificador (R4), cobrar (con tarjeta),
        /// grabar la suscripción (R10, R3), pedir el CAE, generar la factura en
        /// PDF, registrar la venta en la bitácora (R2) y mandar la factura por
        /// correo. Si el correo no sale, la compra queda hecha igual y la
        /// factura se puede descargar o reenviar.
        public BECompraPlan Comprar(BECompraPlan pedido, string numeroTarjeta, string vencimientoTarjeta,
                                    string codigoSeguridad)
        {
            if (pedido == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            int? dniUsuario = SessionManager.ObtenerInstancia().DniActual;

            // 1. Datos del cliente y de la compra
            BECompraPlan compra = Calcular(pedido.Plan, pedido.Meses);
            compra.RazonSocial = (pedido.RazonSocial ?? string.Empty).Trim();
            compra.CondicionIva = pedido.CondicionIva;
            compra.Domicilio = (pedido.Domicilio ?? string.Empty).Trim();
            compra.Correo = (pedido.Correo ?? string.Empty).Trim();
            compra.MedioPago = pedido.MedioPago;
            compra.Cuotas = pedido.MedioPago == BECompraPlan.MedioTarjeta ? pedido.Cuotas : 1;
            compra.TitularTarjeta = (pedido.TitularTarjeta ?? string.Empty).Trim();
            compra.FechaInicio = pedido.FechaInicio == DateTime.MinValue ? DateTime.Today : pedido.FechaInicio.Date;
            compra.DocumentoCliente = ValidarCliente(compra, pedido.DocumentoCliente);

            if (compra.FechaInicio < DateTime.Today)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de inicio no puede ser anterior a hoy."));
            }
            if (compra.FechaInicio > DateTime.Today.AddDays(DiasMaximosHastaElInicio))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de inicio no puede estar a más de un año."));
            }

            if (compra.MedioPago != BECompraPlan.MedioContado && compra.MedioPago != BECompraPlan.MedioTarjeta)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Elija el medio de pago: contado o tarjeta de crédito."));
            }

            // 2. Tarjeta: se valida acá antes de llamar a la pasarela, para dar
            // un mensaje claro de qué dato está mal.
            int mesVencimiento = 0, anioVencimiento = 0;
            if (compra.MedioPago == BECompraPlan.MedioTarjeta)
            {
                if (Array.IndexOf(CuotasPermitidas, compra.Cuotas) < 0)
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                        "Con tarjeta se puede pagar en 1, 3 o 6 cuotas sin interés."));
                }

                ValidarTarjeta(compra, numeroTarjeta, vencimientoTarjeta, codigoSeguridad,
                               out mesVencimiento, out anioVencimiento);
            }

            // 3. Una sola suscripción activa
            ControlarQueNoHayaOtraActiva(TraerSuscripciones());

            // R4 antes de cobrar: si la tabla no está íntegra la suscripción no
            // se va a poder grabar, y no tiene sentido cobrar.
            bllDV.VerificarAntesDeModificar(Tabla);

            // R6: si la clave AES del Web.config falta o es inválida, la compra
            // se corta acá, sin haber cobrado.
            Encriptacion.EncriptarAES256("control");

            // 4. Cobro
            string tokenEncriptado = null;
            if (compra.MedioPago == BECompraPlan.MedioTarjeta)
            {
                RespuestaPasarela tokenizacion = PasarelaPagos.Tokenizar(numeroTarjeta, mesVencimiento, anioVencimiento,
                                                                        codigoSeguridad, compra.TitularTarjeta);
                ControlarOrigenDeLaPasarela(tokenizacion, dniUsuario, compra.Plan);
                if (!tokenizacion.Aprobada)
                {
                    RegistrarRechazo(dniUsuario, compra.Plan, tokenizacion.Motivo);
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR024", tokenizacion.Motivo));
                }

                string concepto = "IntelligentStock - plan " + compra.Plan + " - " + compra.Meses +
                                  (compra.Meses == 1 ? " mes" : " meses") +
                                  (compra.Cuotas > 1 ? " - " + compra.Cuotas + " cuotas" : string.Empty);
                RespuestaPasarela cobro = PasarelaPagos.Cobrar(tokenizacion.Token, compra.Total, concepto);
                ControlarOrigenDeLaPasarela(cobro, dniUsuario, compra.Plan);
                if (!cobro.Aprobada)
                {
                    RegistrarRechazo(dniUsuario, compra.Plan, cobro.Motivo);
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR024", cobro.Motivo));
                }

                compra.CodigoTransaccion = cobro.CodigoTransaccion;
                tokenEncriptado = Encriptacion.EncriptarAES256(tokenizacion.Token);
            }

            // 5. Suscripción
            BESuscripcion suscripcion = new BESuscripcion();
            suscripcion.TipoPlan = compra.Plan;
            suscripcion.TokenPago = tokenEncriptado;
            suscripcion.Estado = BESuscripcion.EstadoActiva;
            suscripcion.FechaInicio = compra.FechaInicio;
            suscripcion.PagadoHasta = compra.PagadoHasta;

            // Si la grabación falla, en un sistema real habría que pedirle a la
            // pasarela que anule el cobro; la pasarela simulada no lo necesita.
            compra.IdSuscripcion = bllDV.EjecutarControlado(() => dalSuscripcion.Crear(suscripcion), Tabla);

            // Datos de facturación para las facturas de los abonos mensuales.
            try
            {
                dalArchivoFactura.GuardarDatosDeFacturacion(compra.IdSuscripcion, compra);
            }
            catch (Exception ex)
            {
                bllEvento.Registrar(dniUsuario, AccionFactura,
                    "No se pudieron guardar los datos de facturación de la suscripción N° " + compra.IdSuscripcion +
                    " (" + ex.Message + "): las facturas de los abonos mensuales se van a tener que emitir a mano.",
                    CriticidadEvento.Advertencia);
            }

            bllEvento.Registrar(dniUsuario, AccionAlta,
                "Se activó la suscripción N° " + compra.IdSuscripcion + " al plan " + compra.Plan + " por " +
                compra.Meses + (compra.Meses == 1 ? " mes" : " meses") + " (pagados hasta el " +
                compra.PagadoHasta.AddDays(-1).ToString("dd/MM/yyyy") + "), " +
                (compra.MedioPago == BECompraPlan.MedioTarjeta
                    ? "con tarjeta. Transacción de la pasarela: " + compra.CodigoTransaccion + "."
                    : "al contado."));

            // 6. Factura: CAE, PDF y bitácora
            EmitirFactura(compra, dniUsuario);

            // 7. Correo
            EnviarFactura(compra);
            return compra;
        }

        private void EmitirFactura(BECompraPlan compra, int? dniUsuario)
        {
            DatosEmisor emisor = DatosEmisor.Leer();
            compra.PuntoDeVenta = emisor.PuntoDeVenta;
            compra.FechaEmision = DateTime.Today;

            // El número es el siguiente al último emitido del mismo tipo y
            // punto de venta; hasta que se guarda el PDF nadie más puede tomarlo.
            lock (candadoNumeracion)
            {
                compra.NumeroComprobante = dalArchivoFactura.UltimoNumero(compra.TipoFactura, compra.PuntoDeVenta) + 1;
                AutorizarYGuardarFactura(compra, emisor, dniUsuario);
            }
        }

        private void AutorizarYGuardarFactura(BECompraPlan compra, DatosEmisor emisor, int? dniUsuario)
        {
            RespuestaCae cae = FacturacionElectronica.SolicitarCae(compra.TipoFactura, compra.PuntoDeVenta,
                compra.NumeroComprobante, compra.DocumentoCliente, compra.Total, compra.FechaEmision);

            if (!AutenticadorSistemaExterno.EsOrigenAutorizado(cae.IpOrigen) || !cae.Aprobado)
            {
                // La venta ya se cobró y la suscripción está activa: no se
                // deshace. Queda registrado para emitir la factura a mano.
                string motivo = cae.Aprobado
                    ? "la respuesta llegó desde la IP no autorizada " + cae.IpOrigen
                    : cae.Motivo;
                bllEvento.Registrar(dniUsuario, AccionFactura,
                    "No se pudo autorizar la factura " + (compra.EsAbonoMensual ? "del abono mensual " : string.Empty) +
                    "de la suscripción N° " + compra.IdSuscripcion + ": " + motivo +
                    " Hay que emitirla a mano. [#ERR024]", CriticidadEvento.Grave);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR024",
                    (compra.EsAbonoMensual ? "El abono quedó cobrado" : "La compra quedó registrada") +
                    " (suscripción N° " + compra.IdSuscripcion + "), pero el servicio de " +
                    "factura electrónica no autorizó la factura: " + motivo));
            }

            compra.Cae = cae.Cae;
            compra.VencimientoCae = cae.Vencimiento;

            byte[] pdf = GeneradorFactura.Generar(compra, emisor);
            compra.ArchivoFactura = GeneradorFactura.NombreDelArchivo(compra);
            dalArchivoFactura.Guardar(compra.ArchivoFactura, pdf);

            // Hasta 500 caracteres (columna descripcion de Evento).
            string descripcion =
                "Factura " + compra.TipoFactura + " " + compra.NumeroFactura + " a " + compra.RazonSocial + " (" +
                compra.DocumentoCliente + ", " + compra.CondicionIva + "): " +
                (compra.EsAbonoMensual ? "abono mensual del " + compra.FechaInicio.ToString("dd/MM/yyyy") + ", " : string.Empty) +
                "plan " + compra.Plan + " x " + compra.Meses + (compra.Meses == 1 ? " mes" : " meses") +
                ", neto " + BECompraPlan.Importe(compra.NetoGravado) + ", IVA " +
                BECompraPlan.Importe(compra.Iva) + ", total " + BECompraPlan.Importe(compra.Total) + ". Pago: " +
                (compra.MedioPago == BECompraPlan.MedioTarjeta
                    ? compra.MarcaTarjeta + " *" + compra.UltimosDigitos + " en " + compra.Cuotas + " cuota(s)"
                    : "contado") +
                ". CAE " + compra.Cae + ". Archivo " + compra.ArchivoFactura + ".";
            bllEvento.Registrar(dniUsuario, AccionFactura,
                descripcion.Length > 500 ? descripcion.Substring(0, 497) + "..." : descripcion);
        }

        /// Manda la factura por correo con el PDF adjunto. Si no hay SMTP
        /// configurado o no hay conexión (por ejemplo, en la facultad sin
        /// internet), no corta la compra: deja el motivo en la compra y un
        /// evento en la bitácora, y la factura se puede descargar o reenviar.
        public bool EnviarFactura(BECompraPlan compra)
        {
            return EnviarFactura(compra, SessionManager.ObtenerInstancia().DniActual);
        }

        private bool EnviarFactura(BECompraPlan compra, int? dniUsuario)
        {
            compra.FacturaEnviada = false;
            compra.MotivoNoEnviada = null;

            byte[] pdf = LeerFactura(compra.ArchivoFactura);
            if (pdf == null)
            {
                compra.MotivoNoEnviada = "no se encontró el archivo de la factura";
            }
            else if (!EnviadorCorreo.EstaConfigurado)
            {
                compra.MotivoNoEnviada = "el servidor de correo no está configurado en el Web.config";
            }
            else
            {
                try
                {
                    string detalle = compra.EsAbonoMensual
                        ? "Se cobró el abono mensual del plan " + compra.Plan + " de IntelligentStock, del " +
                          compra.FechaInicio.ToString("dd/MM/yyyy") + " al " +
                          compra.PagadoHasta.AddDays(-1).ToString("dd/MM/yyyy") + "."
                        : "Gracias por contratar el plan " + compra.Plan + " de IntelligentStock por " + compra.Meses +
                          (compra.Meses == 1 ? " mes" : " meses") + ", desde el " + compra.FechaInicio.ToString("dd/MM/yyyy") + ".";
                    EnviadorCorreo.Enviar(compra.Correo,
                        "IntelligentStock - Factura " + compra.TipoFactura + " " + compra.NumeroFactura,
                        "Hola, " + compra.RazonSocial + ".\n\n" + detalle +
                        " Adjuntamos la factura " + compra.TipoFactura + " " + compra.NumeroFactura + " por " +
                        BECompraPlan.Importe(compra.Total) + ".\n\n" +
                        "Forma de pago: " + compra.DescripcionMedioPago + ".\n\n" +
                        "Este es un comprobante simulado, sin validez fiscal, generado para el trabajo final de ingeniería.",
                        pdf, compra.ArchivoFactura, "application/pdf");
                    compra.FacturaEnviada = true;
                }
                catch (Exception ex)
                {
                    compra.MotivoNoEnviada = "no se pudo conectar con el servidor de correo (" + ex.Message + ")";
                }
            }

            if (!compra.FacturaEnviada)
            {
                bllEvento.Registrar(dniUsuario, AccionFactura,
                    "La factura " + compra.TipoFactura + " " + compra.NumeroFactura + " no se mandó a " + compra.Correo +
                    ": " + compra.MotivoNoEnviada + ".", CriticidadEvento.Advertencia);
            }
            return compra.FacturaEnviada;
        }

        /// Contenido del PDF de una factura, o null si no existe.
        public byte[] LeerFactura(string archivo)
        {
            return dalArchivoFactura.Leer(archivo);
        }

        /// Valida los datos de facturación y devuelve el documento normalizado
        /// (CUIT con guiones, o DNI).
        private static string ValidarCliente(BECompraPlan compra, string documento)
        {
            documento = (documento ?? string.Empty).Trim();

            if (compra.RazonSocial.Length == 0 || documento.Length == 0 || string.IsNullOrEmpty(compra.CondicionIva) ||
                compra.Correo.Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            if (compra.RazonSocial.Length > LargoRazonSocial || compra.Domicilio.Length > LargoDomicilio ||
                compra.Correo.Length > LargoCorreo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La razón social admite hasta " + LargoRazonSocial + " caracteres, el domicilio hasta " +
                    LargoDomicilio + " y el correo hasta " + LargoCorreo + "."));
            }

            if (Array.IndexOf(CondicionesIva, compra.CondicionIva) < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La condición frente al IVA no es válida."));
            }

            int arroba = compra.Correo.IndexOf('@');
            int punto = compra.Correo.LastIndexOf('.');
            if (arroba <= 0 || punto < arroba + 2 || punto == compra.Correo.Length - 1 || compra.Correo.Contains(" "))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."));
            }

            // Un consumidor final se puede identificar con el DNI; el resto, con CUIT.
            string soloDigitos = documento.Replace("-", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty);
            if (compra.CondicionIva == BECompraPlan.CondicionConsumidorFinal && soloDigitos.Length >= 7 &&
                soloDigitos.Length <= 8 && EsNumero(soloDigitos))
            {
                return soloDigitos;
            }

            if (!BLLProveedor.EsCuitValido(documento))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    compra.CondicionIva == BECompraPlan.CondicionConsumidorFinal
                        ? "Ingrese un DNI de 7 u 8 números o un CUIT válido (por ejemplo 20-12345678-6)."
                        : "El CUIT no es válido: tiene que tener 11 números y el último tiene que corresponder a los anteriores."));
            }
            return BLLProveedor.NormalizarCuit(documento);
        }

        private static void ValidarTarjeta(BECompraPlan compra, string numero, string vencimiento, string codigo,
                                           out int mes, out int anio)
        {
            if (string.IsNullOrEmpty(numero) || string.IsNullOrEmpty(vencimiento) || string.IsNullOrEmpty(codigo) ||
                compra.TitularTarjeta.Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            compra.MarcaTarjeta = TarjetaCredito.Marca(numero);
            if (compra.MarcaTarjeta == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Se aceptan tarjetas Visa, Mastercard y American Express: revise el número."));
            }

            if (!TarjetaCredito.PasaLuhn(numero))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El número de tarjeta no es válido: el dígito de control no coincide. Revise que esté bien escrito."));
            }

            if (!TarjetaCredito.LeerVencimiento(vencimiento, out mes, out anio))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El vencimiento se escribe con el mes y el año, por ejemplo 08/29."));
            }

            if (TarjetaCredito.EstaVencida(mes, anio, DateTime.Today))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025", "La tarjeta está vencida."));
            }

            if (anio > DateTime.Today.Year + 20)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El año de vencimiento no es válido."));
            }

            if (!TarjetaCredito.CodigoSeguridadValido(codigo, compra.MarcaTarjeta))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    compra.MarcaTarjeta == TarjetaCredito.AmericanExpress
                        ? "El código de seguridad de American Express tiene 4 números (está en el frente de la tarjeta)."
                        : "El código de seguridad tiene 3 números (está en el dorso de la tarjeta)."));
            }

            compra.UltimosDigitos = TarjetaCredito.UltimosDigitos(numero);
        }

        /// R9: la respuesta de la pasarela solo se da por buena si viene de un origen autorizado.
        private void ControlarOrigenDeLaPasarela(RespuestaPasarela respuesta, int? dniUsuario, string plan)
        {
            if (!AutenticadorSistemaExterno.EsOrigenAutorizado(respuesta.IpOrigen))
            {
                RegistrarRechazo(dniUsuario, plan, "la respuesta de la pasarela llegó desde la IP no autorizada " +
                                                   respuesta.IpOrigen);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR024",
                    "La respuesta de la pasarela no proviene de un origen autorizado."));
            }
        }

        private static decimal Redondear(decimal valor)
        {
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }

        private static bool EsNumero(string texto)
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

        #endregion

        #region Cobro mensual

        /// Revisa los cobros del día: se llama en cada inicio de sesión, pero
        /// trabaja una sola vez por día (como el mantenimiento de la bitácora).
        /// Nunca corta el inicio de sesión: si algo falla, queda en la bitácora
        /// y se vuelve a intentar una hora después.
        public static void RevisarCobrosDelDia()
        {
            if (ultimaRevisionDeCobros == DateTime.Today || DateTime.Now < proximoIntentoDeCobros)
            {
                return;
            }

            // Si otro inicio de sesión ya está cobrando, no se lo espera.
            if (!Monitor.TryEnter(candadoCobros))
            {
                return;
            }

            try
            {
                if (ultimaRevisionDeCobros == DateTime.Today)
                {
                    return;
                }

                ultimaRevisionDeCobros = DateTime.Today;
                try
                {
                    new BLLSuscripcion().CobrarAbonosVencidos(DateTime.Today);
                }
                catch (Exception ex)
                {
                    ultimaRevisionDeCobros = DateTime.MinValue;
                    proximoIntentoDeCobros = DateTime.Now.AddHours(1);
                    new BLLEvento().Registrar(null, AccionCobro,
                        "No se pudieron revisar los cobros mensuales de las suscripciones: " + ex.Message +
                        " Se vuelve a intentar en una hora.", CriticidadEvento.Advertencia);
                }
            }
            finally
            {
                Monitor.Exit(candadoCobros);
            }
        }

        /// Cobra el abono de cada suscripción activa con tarjeta cuyo período
        /// pagado ya venció (pagadoHasta igual a hoy o anterior): un mes por
        /// cobro, con su factura. Si quedaron varios meses sin cobrar (nadie
        /// entró al sistema), se cobra uno por uno. Si la pasarela rechaza un
        /// cobro, esa suscripción se deja para el día siguiente. Devuelve
        /// cuántos abonos se cobraron.
        public int CobrarAbonosVencidos(DateTime hoy)
        {
            List<BESuscripcion> vencidas = dalSuscripcion.TraerSuscripciones().FindAll(delegate(BESuscripcion s)
            {
                return s.Activa && s.ConTarjeta && s.PagadoHasta != DateTime.MinValue && s.PagadoHasta <= hoy.Date;
            });
            if (vencidas.Count == 0)
            {
                return 0;
            }

            // R4: si la tabla Suscripcion no está íntegra no se cobra ninguna
            // (la excepción la registra RevisarCobrosDelDia, que reintenta).
            bllDV.VerificarAntesDeModificar(Tabla);

            int cobrados = 0;
            foreach (BESuscripcion suscripcion in vencidas)
            {
                try
                {
                    for (int mes = 0; mes < MesesMaximosPorRevision && suscripcion.PagadoHasta <= hoy.Date; mes++)
                    {
                        if (!CobrarUnMes(suscripcion))
                        {
                            break;
                        }
                        cobrados++;
                    }
                }
                catch (Exception ex)
                {
                    bllEvento.Registrar(null, AccionCobro,
                        "No se pudo cobrar el abono de la suscripción N° " + suscripcion.IdSuscripcion + ": " +
                        ex.Message + " Se vuelve a intentar mañana.", CriticidadEvento.Advertencia);
                }
            }
            return cobrados;
        }

        /// Cobra el abono del mes que empieza en suscripcion.PagadoHasta, lo
        /// registra (R4 antes, R3 después) y emite la factura. Devuelve false si
        /// no se cobró.
        private bool CobrarUnMes(BESuscripcion suscripcion)
        {
            DateTime desde = suscripcion.PagadoHasta;
            DateTime hasta = desde.AddMonths(1);
            string periodo = "del " + desde.ToString("dd/MM/yyyy") + " al " + hasta.AddDays(-1).ToString("dd/MM/yyyy");

            string token = TokenDesencriptado(suscripcion);
            if (token == null)
            {
                bllEvento.Registrar(null, AccionCobro,
                    "No se pudo cobrar el abono " + periodo + " de la suscripción N° " + suscripcion.IdSuscripcion +
                    ": no se pudo desencriptar el token de pago (¿cambió la clave AES del Web.config?). " +
                    "Hay que cobrarlo por otro medio.", CriticidadEvento.Grave);
                return false;
            }

            // R4 antes de cobrar: si la tabla no está íntegra el cobro no se va
            // a poder registrar, y no hay que cobrar.
            bllDV.VerificarAntesDeModificar(Tabla);

            BECompraPlan abono = Calcular(suscripcion.TipoPlan, 1);
            abono.IdSuscripcion = suscripcion.IdSuscripcion;
            abono.FechaInicio = desde;
            abono.MedioPago = BECompraPlan.MedioTarjeta;
            abono.EsAbonoMensual = true;

            RespuestaPasarela respuesta = PasarelaPagos.Cobrar(token, abono.Total,
                "IntelligentStock - plan " + abono.Plan + " - abono " + periodo);

            // R9: la respuesta solo se da por buena si viene de un origen autorizado.
            if (!AutenticadorSistemaExterno.EsOrigenAutorizado(respuesta.IpOrigen))
            {
                bllEvento.Registrar(null, AccionCobro,
                    "No se cobró el abono " + periodo + " de la suscripción N° " + suscripcion.IdSuscripcion +
                    ": la respuesta de la pasarela llegó desde la IP no autorizada " + respuesta.IpOrigen +
                    ". Se vuelve a intentar mañana. [#ERR024]", CriticidadEvento.Advertencia);
                return false;
            }

            BECompraPlan cliente = LeerDatosDeFacturacion(suscripcion.IdSuscripcion);
            if (!respuesta.Aprobada)
            {
                bllEvento.Registrar(null, AccionCobro,
                    "La pasarela rechazó el cobro del abono " + periodo + " de la suscripción N° " +
                    suscripcion.IdSuscripcion + " (plan " + abono.Plan + ", " + BECompraPlan.Importe(abono.Total) + "): " +
                    respuesta.Motivo + " Se vuelve a intentar mañana. [#ERR024]", CriticidadEvento.Advertencia);
                AvisarRechazo(cliente, abono, periodo, respuesta.Motivo);
                return false;
            }

            bool registrado = bllDV.EjecutarControlado(
                () => dalSuscripcion.RegistrarCobro(suscripcion.IdSuscripcion, desde, hasta), Tabla);
            if (!registrado)
            {
                // La dieron de baja o ya se registró ese mes mientras se cobraba.
                bllEvento.Registrar(null, AccionCobro,
                    "Se cobró el abono " + periodo + " de la suscripción N° " + suscripcion.IdSuscripcion +
                    " (transacción " + respuesta.CodigoTransaccion + "), pero la suscripción ya no estaba activa o " +
                    "ese mes ya figuraba cobrado: hay que pedirle a la pasarela que devuelva el cobro.",
                    CriticidadEvento.Grave);
                return false;
            }

            suscripcion.PagadoHasta = hasta;
            bllEvento.Registrar(null, AccionCobro,
                "Se cobró el abono " + periodo + " de la suscripción N° " + suscripcion.IdSuscripcion + " (plan " +
                abono.Plan + "): " + BECompraPlan.Importe(abono.Total) + " con IVA, con la tarjeta guardada. Transacción de " +
                "la pasarela: " + respuesta.CodigoTransaccion + ". Próximo cobro: " + hasta.ToString("dd/MM/yyyy") + ".");

            // Factura del abono, con los datos de facturación de la compra.
            if (cliente == null)
            {
                bllEvento.Registrar(null, AccionFactura,
                    "No hay datos de facturación guardados para la suscripción N° " + suscripcion.IdSuscripcion +
                    ": la factura del abono " + periodo + " hay que emitirla a mano.", CriticidadEvento.Advertencia);
                return true;
            }

            abono.RazonSocial = cliente.RazonSocial;
            abono.DocumentoCliente = cliente.DocumentoCliente;
            abono.CondicionIva = cliente.CondicionIva;
            abono.Domicilio = cliente.Domicilio;
            abono.Correo = cliente.Correo;
            abono.MarcaTarjeta = cliente.MarcaTarjeta;
            abono.UltimosDigitos = cliente.UltimosDigitos;
            abono.TitularTarjeta = cliente.TitularTarjeta;
            abono.CodigoTransaccion = respuesta.CodigoTransaccion;

            try
            {
                EmitirFactura(abono, null);
                EnviarFactura(abono, null);
            }
            catch (ApplicationException)
            {
                // El abono ya está cobrado y registrado. EmitirFactura dejó en
                // la bitácora que la factura hay que emitirla a mano.
            }
            return true;
        }

        private BECompraPlan LeerDatosDeFacturacion(int idSuscripcion)
        {
            try
            {
                return dalArchivoFactura.LeerDatosDeFacturacion(idSuscripcion);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// Le avisa al cliente, al correo de facturación, que no se pudo cobrar el
        /// abono. Si no hay correo configurado, queda solo en la bitácora.
        private void AvisarRechazo(BECompraPlan cliente, BECompraPlan abono, string periodo, string motivo)
        {
            if (cliente == null || string.IsNullOrEmpty(cliente.Correo) || !EnviadorCorreo.EstaConfigurado)
            {
                return;
            }

            try
            {
                EnviadorCorreo.Enviar(cliente.Correo,
                    "IntelligentStock - No se pudo cobrar el abono del plan " + abono.Plan,
                    "Hola, " + cliente.RazonSocial + ".\n\n" +
                    "No pudimos cobrar el abono mensual del plan " + abono.Plan + " de IntelligentStock " + periodo +
                    " (" + BECompraPlan.Importe(abono.Total) + " con IVA) con la tarjeta " + cliente.MarcaTarjeta +
                    " terminada en " + cliente.UltimosDigitos + ": " + motivo + "\n\n" +
                    "Vamos a volver a intentar el cobro mañana. El servicio sigue funcionando normalmente.\n\n" +
                    "Este es un aviso simulado, generado para el trabajo final de ingeniería.");
            }
            catch (Exception ex)
            {
                bllEvento.Registrar(null, AccionCobro,
                    "No se pudo avisar por correo a " + cliente.Correo + " que se rechazó el cobro del abono: " +
                    ex.Message, CriticidadEvento.Advertencia);
            }
        }

        #endregion

        #region Auxiliares

        private static void Completar(BESuscripcion suscripcion)
        {
            suscripcion.AbonoMensual = PrecioDelPlan(suscripcion.TipoPlan);

            bool alContado = suscripcion.MedioPago == BECompraPlan.MedioContado;
            string token = TokenDesencriptado(suscripcion);
            suscripcion.TokenEnmascarado = alContado
                ? "(contado)"
                : token == null ? "(no disponible)" : Enmascarar(token);
        }

        /// R7: devuelve el token en claro, o null si no se puede desencriptar.
        private static string TokenDesencriptado(BESuscripcion suscripcion)
        {
            if (string.IsNullOrEmpty(suscripcion.TokenPago))
            {
                return null;
            }

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
