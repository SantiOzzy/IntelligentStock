using System;

namespace BE
{
    /// Suscripción del local al servicio SaaS. Se corresponde con la tabla
    /// Suscripcion.
    [Serializable]
    public class BESuscripcion
    {
        /// Mientras está Activa la pasarela cobra el abono todos los meses.
        /// Al darla de baja pasa a Cancelada: se detienen los cobros y el
        /// servicio sigue hasta el cierre del período ya pagado.
        public const string EstadoActiva = "Activa";
        public const string EstadoCancelada = "Cancelada";

        public int IdSuscripcion { get; set; }

        public string TipoPlan { get; set; }

        /// Token de pago encriptado con AES-256 (R6), tal como se guarda.
        public string TokenPago { get; set; }

        public string Estado { get; set; }

        public DateTime FechaInicio { get; set; }

        /// Abono mensual del plan (no se guarda: depende del plan).
        public decimal AbonoMensual { get; set; }

        /// Token desencriptado (R7) y enmascarado para mostrar, por ejemplo
        /// "tok_****8f2a". No se guarda.
        public string TokenEnmascarado { get; set; }

        /// Código de la operación que informó la pasarela en el alta. No se guarda.
        public string CodigoTransaccion { get; set; }

        /// Para las canceladas: hasta qué día sigue el servicio que ya se pagó
        /// (cierre del período en el que se dio de baja). Null si no se conoce.
        public DateTime? ServicioHasta { get; set; }

        /// Para las activas: próxima fecha de cobro mensual.
        public DateTime? ProximoCobro { get; set; }

        public bool Activa
        {
            get { return Estado == EstadoActiva; }
        }

        /// Situación del servicio para mostrar en la grilla.
        public string VigenciaTexto
        {
            get
            {
                if (Activa)
                {
                    return ProximoCobro.HasValue
                        ? "Próximo cobro: " + ProximoCobro.Value.ToString("dd/MM/yyyy")
                        : "Activa";
                }

                if (!ServicioHasta.HasValue)
                {
                    return "Dada de baja";
                }

                return ServicioHasta.Value.Date > DateTime.Today
                    ? "Dada de baja: el servicio sigue hasta el " + ServicioHasta.Value.ToString("dd/MM/yyyy")
                    : "Dada de baja: el servicio terminó el " + ServicioHasta.Value.ToString("dd/MM/yyyy");
            }
        }

        public string FechaInicioTexto
        {
            get { return FechaInicio.ToString("dd/MM/yyyy"); }
        }

        public string AbonoTexto
        {
            get { return AbonoMensual.ToString("C"); }
        }
    }
}
