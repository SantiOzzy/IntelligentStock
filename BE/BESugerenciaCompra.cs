using System;
using System.Collections.Generic;

namespace BE
{
    /// Cantidad a pedir de un insumo según la predicción de demanda (CU-006-033).
    ///
    /// Sugerido = consumo previsto para los próximos días + stock mínimo
    ///            - stock actual - lo que ya está pedido en órdenes pendientes.
    ///
    /// Un perecedero no se pide para más días de los que dura (su vida útil):
    /// lo que sobrara se tiraría.
    ///
    /// Además propone a qué proveedor pedírselo: el que lo facturó más barato
    /// en las órdenes recibidas de los últimos días (ver BLLOrdenCompra). El
    /// usuario puede elegir otro.
    [Serializable]
    public class BESugerenciaCompra
    {
        private List<BEPrecioProveedor> precios = new List<BEPrecioProveedor>();

        public int IdInsumo { get; set; }

        public string NombreInsumo { get; set; }

        public string UnidadDeMedida { get; set; }

        public decimal StockActual { get; set; }

        public decimal StockMinimo { get; set; }

        /// Cantidad que ya está pedida en órdenes pendientes.
        public decimal CantidadPendiente { get; set; }

        /// Consumo que predice el modelo para el período proyectado.
        public decimal ConsumoPrevisto { get; set; }

        public decimal CantidadSugerida { get; set; }

        /// Lo que el usuario decide pedir (empieza siendo lo sugerido).
        public decimal CantidadAPedir { get; set; }

        /// Días de consumo que cubre la sugerencia: los proyectados, o menos si
        /// la vida útil del insumo es más corta.
        public int DiasCubiertos { get; set; }

        public bool LimitadoPorVidaUtil { get; set; }

        /// Si está marcado para pedirlo (en la pantalla se puede desmarcar).
        public bool Incluir { get; set; }

        /// Proveedor al que se le va a pedir (el sugerido o el que eligió el
        /// usuario). Vacío si todavía no hay ninguno.
        public string CuitProveedor { get; set; }

        public string NombreProveedor { get; set; }

        /// Por qué se sugiere ese proveedor ("Mejor precio de los últimos 60
        /// días", "Se le pidió la última vez"...).
        public string MotivoProveedor { get; set; }

        /// Último precio que facturó cada proveedor por este insumo, del más
        /// barato al más caro.
        public List<BEPrecioProveedor> Precios
        {
            get { return precios; }
            set { precios = value ?? new List<BEPrecioProveedor>(); }
        }

        /// Precio unitario actual del insumo: el de la última factura de
        /// cualquier proveedor. Se usa si el proveedor elegido nunca lo facturó.
        public decimal PrecioReferencia { get; set; }

        /// Precio estimado con el proveedor elegido.
        public decimal PrecioUnitario { get; set; }

        /// Fecha del precio del proveedor elegido (null si es el de referencia).
        public DateTime? FechaPrecio { get; set; }

        /// El precio del proveedor elegido es de los últimos días.
        public bool PrecioReciente { get; set; }

        public decimal ImporteEstimado
        {
            get { return CantidadAPedir * PrecioUnitario; }
        }

        /// Último precio de ese proveedor para este insumo, o null si nunca lo facturó.
        public BEPrecioProveedor PrecioDe(string cuit)
        {
            return string.IsNullOrEmpty(cuit) ? null : precios.Find(delegate(BEPrecioProveedor p) { return p.CuitProveedor == cuit; });
        }

        /// Pasa a pedírselo a ese proveedor, con su último precio (o el de referencia).
        public void ElegirProveedor(string cuit, string nombre)
        {
            CuitProveedor = cuit;
            NombreProveedor = nombre;
            BEPrecioProveedor precio = PrecioDe(cuit);
            PrecioUnitario = precio == null ? PrecioReferencia : precio.PrecioUnitario;
            FechaPrecio = precio == null ? (DateTime?)null : precio.Fecha;
            PrecioReciente = precio != null && precio.Reciente;
        }

        public string StockActualTexto
        {
            get { return StockActual.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string StockMinimoTexto
        {
            get { return StockMinimo.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string CantidadPendienteTexto
        {
            get { return CantidadPendiente.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string ConsumoPrevistoTexto
        {
            get { return ConsumoPrevisto.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string CantidadSugeridaTexto
        {
            get { return CantidadSugerida.ToString("0.##") + " " + UnidadDeMedida; }
        }

        public string ImporteEstimadoTexto
        {
            get { return ImporteEstimado.ToString("C"); }
        }

        public string DiasCubiertosTexto
        {
            get
            {
                return DiasCubiertos + (DiasCubiertos == 1 ? " día" : " días") +
                       (LimitadoPorVidaUtil ? " (vida útil)" : string.Empty);
            }
        }

        /// Consumo previsto y para cuántos días: "12 kg en 7 días".
        public string ConsumoPrevistoPeriodoTexto
        {
            get { return ConsumoPrevistoTexto + " en " + DiasCubiertosTexto; }
        }

        /// "$ 1.250,00 (02/11/2026)", o "(precio de referencia)" si el proveedor
        /// elegido nunca lo facturó.
        public string PrecioTexto
        {
            get
            {
                if (!FechaPrecio.HasValue)
                {
                    return PrecioUnitario.ToString("C") + " (precio de referencia)";
                }
                return PrecioUnitario.ToString("C") + " (" + FechaPrecio.Value.ToString("dd/MM/yyyy") +
                       (PrecioReciente ? ")" : ", desactualizado)");
            }
        }

        /// Lo que cobraron los otros proveedores, para comparar.
        public string OtrosPreciosTexto
        {
            get
            {
                List<string> otros = new List<string>();
                foreach (BEPrecioProveedor precio in precios)
                {
                    if (precio.CuitProveedor != CuitProveedor)
                    {
                        otros.Add(precio.Texto);
                    }
                }
                return otros.Count == 0 ? string.Empty : "Otros: " + string.Join("; ", otros.ToArray());
            }
        }
    }
}
