using System;

namespace BE
{
    /// Suscripción del local al servicio SaaS. Se corresponde con la tabla
    /// Suscripcion.
    ///
    /// Se paga por adelantado una cantidad de meses (la compra, BECompraPlan).
    /// PagadoHasta es el día en que vence lo pagado. Con tarjeta de crédito,
    /// ese día el sistema cobra el abono del mes siguiente con el token
    /// guardado y PagadoHasta avanza un mes (BLLSuscripcion.CobrarAbonosVencidos).
    /// Al contado no hay token: el servicio cubre los meses pagados y después
    /// hay que volver a comprar.
    [Serializable]
    public class BESuscripcion
    {
        /// Mientras está Activa (y se pagó con tarjeta) se cobra el abono todos
        /// los meses, después de los meses pagados por adelantado. Al darla de
        /// baja pasa a Cancelada: se detienen los cobros y el servicio sigue
        /// hasta que vence lo ya pagado.
        public const string EstadoActiva = "Activa";
        public const string EstadoCancelada = "Cancelada";

        public int IdSuscripcion { get; set; }

        public string TipoPlan { get; set; }

        /// Token de pago encriptado con AES-256 (R6), tal como se guarda. Vacío
        /// si se pagó al contado.
        public string TokenPago { get; set; }

        /// Contado o tarjeta de crédito: se deduce del token.
        public string MedioPago
        {
            get { return string.IsNullOrEmpty(TokenPago) ? BECompraPlan.MedioContado : BECompraPlan.MedioTarjeta; }
        }

        public bool ConTarjeta
        {
            get { return MedioPago == BECompraPlan.MedioTarjeta; }
        }

        public string Estado { get; set; }

        public DateTime FechaInicio { get; set; }

        /// Día en que vence lo que ya se pagó (columna pagadoHasta): el servicio
        /// está pagado hasta el día anterior. DateTime.MinValue si no se conoce.
        public DateTime PagadoHasta { get; set; }

        /// Último día cubierto por lo que ya se pagó.
        public DateTime UltimoDiaPagado
        {
            get { return PagadoHasta == DateTime.MinValue ? DateTime.MinValue : PagadoHasta.AddDays(-1); }
        }

        /// Abono mensual del plan (no se guarda: depende del plan).
        public decimal AbonoMensual { get; set; }

        /// Token desencriptado (R7) y enmascarado para mostrar, por ejemplo
        /// "tok_****8f2a". No se guarda.
        public string TokenEnmascarado { get; set; }

        /// Código de la operación que informó la pasarela en el alta. No se guarda.
        public string CodigoTransaccion { get; set; }

        public bool Activa
        {
            get { return Estado == EstadoActiva; }
        }

        /// Activa, con tarjeta y con el abono del mes vencido sin cobrar (por
        /// ejemplo, porque la pasarela rechazó el cobro): se reintenta cada día.
        public bool CobroPendiente
        {
            get { return Activa && ConTarjeta && PagadoHasta != DateTime.MinValue && PagadoHasta <= DateTime.Today; }
        }

        /// Situación del servicio para mostrar en la grilla.
        public string VigenciaTexto
        {
            get
            {
                if (PagadoHasta == DateTime.MinValue)
                {
                    return Activa ? "Activa" : "Dada de baja";
                }

                string ultimoDia = UltimoDiaPagado.ToString("dd/MM/yyyy");
                if (Activa && !ConTarjeta)
                {
                    return PagadoHasta > DateTime.Today
                        ? "Pagada al contado hasta el " + ultimoDia
                        : "Vencida desde el " + PagadoHasta.ToString("dd/MM/yyyy") + ": hay que volver a comprar";
                }

                if (Activa)
                {
                    return CobroPendiente
                        ? "Abono del " + PagadoHasta.ToString("dd/MM/yyyy") + " sin cobrar: se reintenta cada día"
                        : "Próximo cobro: " + PagadoHasta.ToString("dd/MM/yyyy");
                }

                return PagadoHasta > DateTime.Today
                    ? "Dada de baja: el servicio sigue hasta el " + ultimoDia
                    : "Dada de baja: el servicio terminó el " + ultimoDia;
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
