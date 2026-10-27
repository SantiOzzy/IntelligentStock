using System;

namespace BE
{
    /// Insumo o materia prima del catálogo. Se corresponde con la tabla Insumo.
    [Serializable]
    public class BEInsumo
    {
        public BEInsumo()
        {
        }

        public BEInsumo(int idInsumo, string nombre)
        {
            IdInsumo = idInsumo;
            Nombre = nombre;
        }

        public int IdInsumo { get; set; }

        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        /// Unidad en la que se mide (kg, lt, unidad, etc.).
        public string UnidadDeMedida { get; set; }

        /// Cantidad disponible. Solo cambia por ajuste, venta o compra.
        public decimal Stock { get; set; }

        public decimal StockMinimo { get; set; }

        /// Días de vida útil desde el ingreso. Es opcional.
        public int? DiasDeVidaUtil { get; set; }

        /// Costo de una unidad de medida del insumo (por ejemplo, el precio
        /// de un kg). Se usa para mostrar el impacto económico de las pérdidas.
        public decimal PrecioUnitario { get; set; }

        public bool Activo { get; set; }

        public string Estado
        {
            get { return Activo ? "Activo" : "Inactivo"; }
        }

        /// El stock llegó (o quedó por debajo) del mínimo de seguridad.
        public bool BajoMinimo
        {
            get { return Stock <= StockMinimo; }
        }

        /// Cantidades listas para mostrar en una grilla, sin ceros de más.
        public string StockValor
        {
            get { return Stock.ToString("0.##"); }
        }

        public string StockMinimoValor
        {
            get { return StockMinimo.ToString("0.##"); }
        }

        /// Cantidad con su unidad, por ejemplo "2,5 kg".
        public string StockTexto
        {
            get { return Stock.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string StockMinimoTexto
        {
            get { return StockMinimo.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string VidaUtilTexto
        {
            get { return DiasDeVidaUtil.HasValue ? DiasDeVidaUtil.Value + " días" : "-"; }
        }

        /// Precio en formato moneda, por ejemplo "$ 1.250,00".
        public string PrecioTexto
        {
            get { return PrecioUnitario.ToString("C"); }
        }

        /// Lleva fecha de vencimiento: es mercadería que se pierde si se corta
        /// la cadena de frío.
        public bool EsPerecedero
        {
            get { return DiasDeVidaUtil.HasValue; }
        }

        /// Valor del stock disponible (stock por precio unitario).
        public decimal ValorStock
        {
            get { return Stock * PrecioUnitario; }
        }

        /// Texto para mostrar el insumo en una lista desplegable.
        public string Etiqueta
        {
            get { return Nombre + " (" + UnidadDeMedida + ")"; }
        }
    }
}
