using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text;
using BE;

namespace Servicios
{
    /// Datos del que emite la factura (la empresa que vende IntelligentStock).
    /// Se configuran en el Web.config (claves "FacturaEmisor*"); si faltan, se
    /// usan los de ejemplo.
    public class DatosEmisor
    {
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string Domicilio { get; set; }
        public string CondicionIva { get; set; }
        public string IngresosBrutos { get; set; }
        public string InicioActividades { get; set; }
        public int PuntoDeVenta { get; set; }

        public static DatosEmisor Leer()
        {
            DatosEmisor emisor = new DatosEmisor();
            emisor.RazonSocial = Valor("FacturaEmisorRazonSocial", "IntelligentStock S.A.S.");
            emisor.Cuit = Valor("FacturaEmisorCuit", "30-71834562-2");
            emisor.Domicilio = Valor("FacturaEmisorDomicilio", "Lomas de Zamora, Provincia de Buenos Aires");
            emisor.CondicionIva = Valor("FacturaEmisorCondicionIva", "IVA Responsable Inscripto");
            emisor.IngresosBrutos = Valor("FacturaEmisorIngresosBrutos", emisor.Cuit);
            emisor.InicioActividades = Valor("FacturaEmisorInicioActividades", "01/03/2026");

            int puntoDeVenta;
            emisor.PuntoDeVenta = int.TryParse(ConfigurationManager.AppSettings["FacturaPuntoDeVenta"],
                                               out puntoDeVenta) && puntoDeVenta > 0
                ? puntoDeVenta
                : 1;
            return emisor;
        }

        private static string Valor(string clave, string porDefecto)
        {
            string valor = ConfigurationManager.AppSettings[clave];
            return string.IsNullOrEmpty(valor) ? porDefecto : valor.Trim();
        }
    }

    /// Arma la factura de la compra de un plan en PDF (A4 vertical), con el
    /// formato de las facturas electrónicas argentinas: letra A o B, datos del
    /// emisor y del cliente, detalle con bonificación, IVA discriminado (A) o
    /// IVA contenido (B), forma de pago y CAE. Se escribe "a mano", igual que
    /// los reportes (ExportadorReporte), con la fuente Helvetica.
    public static class GeneradorFactura
    {
        private const float Ancho = 595f;
        private const float Alto = 842f;
        private const float Izq = 30f;
        private const float Der = 565f;

        private static readonly CultureInfo Pesos = CultureInfo.GetCultureInfo("es-AR");

        public static byte[] Generar(BECompraPlan compra, DatosEmisor emisor)
        {
            if (compra == null)
            {
                throw new ArgumentNullException("compra");
            }

            bool letraA = compra.TipoFactura == "A";
            StringBuilder p = new StringBuilder();

            // ORIGINAL
            Rect(p, Izq, 790f, Der - Izq, 22f, false);
            Centro(p, "F2", 11f, Ancho / 2f, 797f, "ORIGINAL");

            // Encabezado: emisor a la izquierda, letra al medio, comprobante a la derecha.
            Rect(p, Izq, 680f, Der - Izq, 110f, false);
            Linea(p, Ancho / 2f, 680f, Ancho / 2f, 736f);
            p.Append("1 g ");
            Rect(p, Ancho / 2f - 24f, 736f, 48f, 54f, true);
            p.Append("0 g ");
            Rect(p, Ancho / 2f - 24f, 736f, 48f, 54f, false);
            Centro(p, "F2", 28f, Ancho / 2f, 758f, compra.TipoFactura);
            Centro(p, "F2", 6.5f, Ancho / 2f, 742f, letraA ? "COD. 01" : "COD. 06");

            Texto(p, "F2", 14f, Izq + 10f, 760f, emisor.RazonSocial);
            Texto(p, "F1", 8f, Izq + 10f, 730f, "Razón social: " + emisor.RazonSocial);
            TextoPartido(p, "F1", 8f, Izq + 10f, 718f, "Domicilio comercial: " + emisor.Domicilio, 220f, 10f);
            Texto(p, "F1", 8f, Izq + 10f, 694f, "Condición frente al IVA: " + emisor.CondicionIva);

            float xd = Ancho / 2f + 30f;
            Texto(p, "F2", 18f, xd, 760f, "FACTURA");
            Texto(p, "F2", 9f, xd, 738f, "Punto de venta: " + compra.PuntoDeVenta.ToString("0000", CultureInfo.InvariantCulture) +
                                         "     Comp. Nro: " + compra.NumeroComprobante.ToString("00000000", CultureInfo.InvariantCulture));
            Texto(p, "F1", 8.5f, xd, 724f, "Fecha de emisión: " + Fecha(compra.FechaEmision));
            Texto(p, "F1", 8.5f, xd, 712f, "CUIT: " + emisor.Cuit);
            Texto(p, "F1", 8.5f, xd, 700f, "Ingresos Brutos: " + emisor.IngresosBrutos);
            Texto(p, "F1", 8.5f, xd, 688f, "Fecha de inicio de actividades: " + emisor.InicioActividades);

            // Período facturado
            Rect(p, Izq, 654f, Der - Izq, 22f, false);
            Texto(p, "F1", 8.5f, Izq + 10f, 661f,
                  "Período facturado desde: " + Fecha(compra.FechaInicio) + "     Hasta: " + Fecha(compra.PagadoHasta.AddDays(-1)) +
                  "     Fecha de vto. para el pago: " + Fecha(compra.FechaEmision));

            // Cliente
            Rect(p, Izq, 590f, Der - Izq, 60f, false);
            Texto(p, "F2", 8.5f, Izq + 10f, 636f, (compra.DocumentoCliente.Contains("-") ? "CUIT: " : "DNI: ") + compra.DocumentoCliente);
            TextoPartido(p, "F1", 8.5f, Izq + 200f, 636f, "Apellido y nombre / Razón social: " + compra.RazonSocial, 325f, 10f);
            Texto(p, "F1", 8.5f, Izq + 10f, 616f, "Condición frente al IVA: " + compra.CondicionIva);
            TextoPartido(p, "F1", 8.5f, Izq + 200f, 616f, "Domicilio: " + (string.IsNullOrEmpty(compra.Domicilio) ? "-" : compra.Domicilio), 325f, 10f);
            Texto(p, "F1", 8.5f, Izq + 10f, 598f, "Condición de venta: " + (compra.MedioPago == BECompraPlan.MedioTarjeta ? "Tarjeta de crédito" : "Contado"));

            // Detalle
            string[] titulos;
            float[] anchos;
            string[] fila;
            string descripcion = "IntelligentStock - plan " + compra.Plan + " (abono mensual, del " + Fecha(compra.FechaInicio) +
                                 " al " + Fecha(compra.PagadoHasta.AddDays(-1)) + ")";
            string codigo = "PLAN-" + Quitar(compra.Plan).ToUpperInvariant().Substring(0, Math.Min(3, compra.Plan.Length));

            if (letraA)
            {
                titulos = new[] { "Código", "Producto / Servicio", "Cant.", "U. medida", "Precio unit.", "% Bonif.", "Subtotal", "Alíc. IVA", "Subtotal c/IVA" };
                anchos = new[] { 52f, 143f, 30f, 45f, 60f, 40f, 60f, 40f, 65f };
                fila = new[] { codigo, descripcion, compra.Meses.ToString(CultureInfo.InvariantCulture), "meses",
                               Numero(compra.PrecioMensual), Numero(compra.PorcentajeDescuento), Numero(compra.NetoGravado),
                               "21%", Numero(compra.Total) };
            }
            else
            {
                decimal precioConIva = Redondear(compra.PrecioMensual * (1 + BECompraPlan.AlicuotaIva));
                decimal subtotalConIva = Redondear(precioConIva * compra.Meses);
                titulos = new[] { "Código", "Producto / Servicio", "Cant.", "U. medida", "Precio unit.", "% Bonif.", "Imp. bonif.", "Subtotal" };
                anchos = new[] { 52f, 178f, 30f, 45f, 65f, 40f, 60f, 65f };
                fila = new[] { codigo, descripcion, compra.Meses.ToString(CultureInfo.InvariantCulture), "meses",
                               Numero(precioConIva), Numero(compra.PorcentajeDescuento),
                               Numero(subtotalConIva - compra.Total), Numero(compra.Total) };
            }

            float y = 570f;
            p.Append("0.88 g ");
            Rect(p, Izq, y - 6f, Der - Izq, 18f, true);
            p.Append("0 g ");
            float x = Izq;
            for (int i = 0; i < titulos.Length; i++)
            {
                if (i <= 1)
                {
                    Texto(p, "F2", 7.5f, x + 3f, y, titulos[i]);
                }
                else
                {
                    Derecha(p, "F2", 7.5f, x + anchos[i] - 3f, y, titulos[i]);
                }
                x += anchos[i];
            }

            y -= 20f;
            x = Izq;
            float yMin = y;
            for (int i = 0; i < fila.Length; i++)
            {
                if (i == 1)
                {
                    float yFin = TextoPartido(p, "F1", 8f, x + 3f, y, fila[i], anchos[i] - 6f, 10f);
                    yMin = Math.Min(yMin, yFin);
                }
                else if (i == 0)
                {
                    Texto(p, "F1", 8f, x + 3f, y, fila[i]);
                }
                else
                {
                    Derecha(p, "F1", 8f, x + anchos[i] - 3f, y, fila[i]);
                }
                x += anchos[i];
            }
            Linea(p, Izq, yMin - 6f, Der, yMin - 6f);

            // Forma de pago
            y = yMin - 26f;
            string pago = compra.MedioPago == BECompraPlan.MedioTarjeta
                ? "Forma de pago: " + compra.DescripcionMedioPago + ". Transacción de la pasarela: " + compra.CodigoTransaccion + "."
                : "Forma de pago: contado (efectivo o transferencia).";
            y = TextoPartido(p, "F1", 8f, Izq + 10f, y, pago, Der - Izq - 20f, 10f);

            // Totales
            float yTot = 300f;
            Rect(p, Izq, yTot - 10f, Der - Izq, 110f, false);
            List<KeyValuePair<string, string>> totales = new List<KeyValuePair<string, string>>();
            if (letraA)
            {
                totales.Add(Par("Importe neto gravado: $", Numero(compra.NetoGravado)));
                totales.Add(Par("IVA 21%: $", Numero(compra.Iva)));
                totales.Add(Par("Importe otros tributos: $", "0,00"));
            }
            else
            {
                totales.Add(Par("Subtotal: $", Numero(compra.Total)));
                totales.Add(Par("Importe otros tributos: $", "0,00"));
            }

            float yt = yTot + 80f;
            foreach (KeyValuePair<string, string> t in totales)
            {
                Derecha(p, "F2", 9f, 470f, yt, t.Key);
                Derecha(p, "F2", 9f, Der - 10f, yt, t.Value);
                yt -= 16f;
            }
            Derecha(p, "F2", 11f, 470f, yTot + 4f, "Importe total: $");
            Derecha(p, "F2", 11f, Der - 10f, yTot + 4f, Numero(compra.Total));

            if (!letraA)
            {
                // Régimen de Transparencia Fiscal al Consumidor: la factura B
                // informa el IVA que contiene el precio.
                Texto(p, "F2", 8f, Izq + 10f, yTot + 80f, "Régimen de Transparencia Fiscal al Consumidor (Ley 27.743)");
                Texto(p, "F1", 8f, Izq + 10f, yTot + 66f, "IVA contenido: $ " + Numero(compra.Iva));
                Texto(p, "F1", 8f, Izq + 10f, yTot + 54f, "Otros impuestos nacionales indirectos: $ 0,00");
            }
            else if (compra.CondicionIva == BECompraPlan.CondicionMonotributista)
            {
                TextoPartido(p, "F1", 7.5f, Izq + 10f, yTot + 80f,
                    "El crédito fiscal discriminado en el presente comprobante sólo podrá ser computado a efectos del " +
                    "Régimen de Sostenimiento e Inclusión Fiscal para Pequeños Contribuyentes de la Ley N° 27.618.",
                    250f, 9f);
            }

            if (compra.PorcentajeDescuento > 0)
            {
                Texto(p, "F1", 8f, Izq + 10f, yTot + 4f,
                      "Incluye " + compra.PorcentajeDescuento.ToString("0.##", Pesos) + "% de bonificación por pago adelantado de " +
                      compra.Meses + " meses.");
            }

            // CAE
            Rect(p, Izq, 60f, Der - Izq, 60f, false);
            Derecha(p, "F2", 9f, Der - 10f, 100f, "CAE N°: " + compra.Cae);
            Derecha(p, "F2", 9f, Der - 10f, 84f, "Fecha de vto. de CAE: " + Fecha(compra.VencimientoCae));
            Texto(p, "F2", 8f, Izq + 10f, 100f, "Comprobante simulado: sin validez fiscal.");
            TextoPartido(p, "F1", 7.5f, Izq + 10f, 88f,
                "Generado por IntelligentStock para el trabajo final de ingeniería. El CAE no fue otorgado por ARCA " +
                "(el servicio de factura electrónica está simulado).", 300f, 9f);
            Centro(p, "F1", 8f, Ancho / 2f, 40f, "Pág. 1/1");

            return ArmarDocumento(p.ToString());
        }

        /// "Factura_A_0001-00000012.pdf"
        public static string NombreDelArchivo(BECompraPlan compra)
        {
            return "Factura_" + compra.TipoFactura + "_" + compra.NumeroFactura + ".pdf";
        }

        #region Dibujo

        private static KeyValuePair<string, string> Par(string a, string b)
        {
            return new KeyValuePair<string, string>(a, b);
        }

        private static decimal Redondear(decimal valor)
        {
            return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        }

        private static string Numero(decimal valor)
        {
            return valor.ToString("N2", Pesos);
        }

        private static string Fecha(DateTime fecha)
        {
            return fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        private static string Quitar(string texto)
        {
            string normal = (texto ?? string.Empty).Normalize(NormalizationForm.FormD);
            StringBuilder salida = new StringBuilder();
            foreach (char c in normal)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    salida.Append(c);
                }
            }
            return salida.ToString();
        }

        private static void Rect(StringBuilder p, float x, float y, float w, float h, bool relleno)
        {
            p.Append(N(x)).Append(' ').Append(N(y)).Append(' ').Append(N(w)).Append(' ').Append(N(h))
             .Append(relleno ? " re f\n" : " re S\n");
        }

        private static void Linea(StringBuilder p, float x1, float y1, float x2, float y2)
        {
            p.Append("0.6 w ").Append(N(x1)).Append(' ').Append(N(y1)).Append(" m ")
             .Append(N(x2)).Append(' ').Append(N(y2)).Append(" l S\n");
        }

        private static void Texto(StringBuilder p, string fuente, float tamanio, float x, float y, string texto)
        {
            p.Append("BT /").Append(fuente).Append(' ').Append(N(tamanio)).Append(" Tf ")
             .Append(N(x)).Append(' ').Append(N(y)).Append(" Td (").Append(Escapar(texto)).Append(") Tj ET\n");
        }

        private static void Derecha(StringBuilder p, string fuente, float tamanio, float xDerecha, float y, string texto)
        {
            Texto(p, fuente, tamanio, xDerecha - AnchoTexto(texto, tamanio, fuente == "F2"), y, texto);
        }

        private static void Centro(StringBuilder p, string fuente, float tamanio, float xCentro, float y, string texto)
        {
            Texto(p, fuente, tamanio, xCentro - AnchoTexto(texto, tamanio, fuente == "F2") / 2f, y, texto);
        }

        /// Escribe el texto en renglones que entran en el ancho dado y devuelve
        /// la altura del último renglón.
        private static float TextoPartido(StringBuilder p, string fuente, float tamanio, float x, float y,
                                          string texto, float ancho, float interlineado)
        {
            string actual = string.Empty;
            foreach (string palabra in (texto ?? string.Empty).Split(' '))
            {
                string prueba = actual.Length == 0 ? palabra : actual + " " + palabra;
                if (actual.Length > 0 && AnchoTexto(prueba, tamanio, fuente == "F2") > ancho)
                {
                    Texto(p, fuente, tamanio, x, y, actual);
                    y -= interlineado;
                    actual = palabra;
                }
                else
                {
                    actual = prueba;
                }
            }
            Texto(p, fuente, tamanio, x, y, actual);
            return y;
        }

        /// Ancho aproximado del texto en Helvetica (en milésimas del tamaño).
        private static float AnchoTexto(string texto, float tamanio, bool negrita)
        {
            float total = 0;
            foreach (char c in texto ?? string.Empty)
            {
                total += AnchoLetra(c);
            }
            return total * tamanio / 1000f * (negrita ? 1.06f : 1f);
        }

        private static float AnchoLetra(char c)
        {
            if (c >= '0' && c <= '9') return 556;
            switch (c)
            {
                case ' ': case '.': case ',': case ':': case '/': case 'f': case 't': case 'I': case '(': case ')': return 278;
                case 'i': case 'j': case 'l': case '\'': return 222;
                case '-': case 'r': return 333;
                case '%': return 889;
                case 'm': case 'M': return 833;
                case 'w': case 'W': return 900;
            }
            if (char.IsUpper(c)) return 690;
            if (c == 'c' || c == 'k' || c == 's' || c == 'v' || c == 'x' || c == 'y' || c == 'z') return 500;
            return 556;
        }

        private static string Escapar(string texto)
        {
            StringBuilder salida = new StringBuilder();
            foreach (char c in texto ?? string.Empty)
            {
                if (c == '(' || c == ')' || c == '\\')
                {
                    salida.Append('\\').Append(c);
                }
                else if (c < 32)
                {
                    salida.Append(' ');
                }
                else
                {
                    salida.Append(c);
                }
            }
            return salida.ToString();
        }

        private static string N(float valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static byte[] ArmarDocumento(string contenidoPagina)
        {
            Encoding winAnsi = Encoding.GetEncoding(1252);
            MemoryStream salida = new MemoryStream();
            List<long> posiciones = new List<long>();

            Action<string> escribir = delegate(string texto)
            {
                byte[] bytes = winAnsi.GetBytes(texto);
                salida.Write(bytes, 0, bytes.Length);
            };

            byte[] contenido = winAnsi.GetBytes(contenidoPagina);

            escribir("%PDF-1.4\n");
            posiciones.Add(salida.Position);
            escribir("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n");
            posiciones.Add(salida.Position);
            escribir("2 0 obj << /Type /Pages /Kids [5 0 R] /Count 1 >> endobj\n");
            posiciones.Add(salida.Position);
            escribir("3 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >> endobj\n");
            posiciones.Add(salida.Position);
            escribir("4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >> endobj\n");
            posiciones.Add(salida.Position);
            escribir("5 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 " + N(Ancho) + " " + N(Alto) +
                     "] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents 6 0 R >> endobj\n");
            posiciones.Add(salida.Position);
            escribir("6 0 obj << /Length " + contenido.Length + " >> stream\n");
            salida.Write(contenido, 0, contenido.Length);
            escribir("\nendstream endobj\n");

            long inicioXref = salida.Position;
            escribir("xref\n0 " + (posiciones.Count + 1) + "\n0000000000 65535 f \n");
            foreach (long posicion in posiciones)
            {
                escribir(posicion.ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
            }
            escribir("trailer << /Size " + (posiciones.Count + 1) + " /Root 1 0 R >>\nstartxref\n" + inicioXref + "\n%%EOF\n");
            return salida.ToArray();
        }

        #endregion
    }
}
