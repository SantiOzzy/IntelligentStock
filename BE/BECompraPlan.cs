using System;
using System.Globalization;

namespace BE
{
    /// Compra de un plan de IntelligentStock (CU-008-044): lo que el local paga
    /// por adelantado, cómo lo paga y la factura que se le emite.
    ///
    /// No es una tabla. De la compra se guarda en la base solo la suscripción
    /// (tabla Suscripcion); el detalle queda en la factura en PDF y en el evento
    /// "Factura de venta" de la bitácora.
    [Serializable]
    public class BECompraPlan
    {
        public const string MedioContado = "Contado";
        public const string MedioTarjeta = "Tarjeta de crédito";

        public const string CondicionResponsableInscripto = "Responsable Inscripto";
        public const string CondicionMonotributista = "Monotributista";
        public const string CondicionExento = "Exento";
        public const string CondicionConsumidorFinal = "Consumidor Final";

        public const decimal AlicuotaIva = 0.21m;

        // ----- Plan y cálculo -----

        public string Plan { get; set; }

        /// Abono mensual del plan, sin IVA.
        public decimal PrecioMensual { get; set; }

        /// Meses que se pagan por adelantado (1, 3, 6 o 12).
        public int Meses { get; set; }

        public DateTime FechaInicio { get; set; }

        /// Hasta cuándo cubre lo que se paga (inicio + meses).
        public DateTime PagadoHasta
        {
            get { return FechaInicio.AddMonths(Meses); }
        }

        /// Precio mensual x meses.
        public decimal Subtotal { get; set; }

        /// Descuento por pago adelantado, en porcentaje (0, 5, 10 o 15).
        public decimal PorcentajeDescuento { get; set; }

        public decimal Descuento { get; set; }

        /// Subtotal menos el descuento: la base sobre la que se calcula el IVA.
        public decimal NetoGravado { get; set; }

        public decimal Iva { get; set; }

        public decimal Total { get; set; }

        // ----- Cliente (receptor de la factura) -----

        public string RazonSocial { get; set; }

        /// CUIT o CUIL con guiones, o DNI para un consumidor final.
        public string DocumentoCliente { get; set; }

        public string CondicionIva { get; set; }

        public string Domicilio { get; set; }

        /// Correo al que se manda la factura.
        public string Correo { get; set; }

        /// Factura A al Responsable Inscripto y al Monotributista (con la
        /// leyenda de la Ley 27.618); B al Exento y al Consumidor Final.
        public string TipoFactura
        {
            get
            {
                return CondicionIva == CondicionResponsableInscripto || CondicionIva == CondicionMonotributista
                    ? "A"
                    : "B";
            }
        }

        // ----- Pago -----

        public string MedioPago { get; set; }

        /// Cuotas sin interés con tarjeta (1, 3 o 6). Con contado, 1.
        public int Cuotas { get; set; }

        public decimal ImporteCuota
        {
            get { return Cuotas <= 1 ? Total : Math.Round(Total / Cuotas, 2, MidpointRounding.AwayFromZero); }
        }

        /// Visa, Mastercard o American Express. Solo con tarjeta.
        public string MarcaTarjeta { get; set; }

        /// Últimos cuatro números de la tarjeta. El número completo y el código
        /// de seguridad nunca se guardan: se le pasan a la pasarela y se
        /// descartan.
        public string UltimosDigitos { get; set; }

        public string TitularTarjeta { get; set; }

        /// Código de la operación en la pasarela (solo con tarjeta).
        public string CodigoTransaccion { get; set; }

        // ----- Resultado -----

        public int IdSuscripcion { get; set; }

        public int PuntoDeVenta { get; set; }

        /// Número de comprobante: es el número de la suscripción, porque cada
        /// compra genera una sola suscripción y una sola factura.
        public int NumeroComprobante
        {
            get { return IdSuscripcion; }
        }

        /// "0001-00000012".
        public string NumeroFactura
        {
            get
            {
                return PuntoDeVenta.ToString("0000", CultureInfo.InvariantCulture) + "-" +
                       NumeroComprobante.ToString("00000000", CultureInfo.InvariantCulture);
            }
        }

        public DateTime FechaEmision { get; set; }

        /// Código de Autorización Electrónico que devuelve el servicio de
        /// factura electrónica (simulado) y su vencimiento.
        public string Cae { get; set; }

        public DateTime VencimientoCae { get; set; }

        /// Nombre del archivo PDF de la factura (en App_Data\Facturas).
        public string ArchivoFactura { get; set; }

        /// Si la factura se pudo mandar por correo.
        public bool FacturaEnviada { get; set; }

        /// Motivo por el que no se mandó (sin SMTP configurado, sin conexión...).
        public string MotivoNoEnviada { get; set; }

        public string DescripcionMedioPago
        {
            get
            {
                if (MedioPago != MedioTarjeta)
                {
                    return "Contado (efectivo o transferencia)";
                }

                string texto = MarcaTarjeta + " terminada en " + UltimosDigitos;
                return Cuotas > 1
                    ? texto + ", " + Cuotas + " cuotas sin interés de " + ImporteCuota.ToString("C")
                    : texto + ", en un pago";
            }
        }
    }
}
