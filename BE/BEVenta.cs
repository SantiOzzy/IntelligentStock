using System;
using System.Collections.Generic;

namespace BE
{
    /// Venta registrada (CU-006-037), tal como queda en la bitácora: la receta,
    /// la cantidad vendida, la fecha de la venta y lo que se consumió de cada
    /// insumo. Es el historial con el que se entrena la predicción de demanda.
    public class BEVenta
    {
        private readonly List<BEConsumoVenta> consumos = new List<BEConsumoVenta>();

        public DateTime Fecha { get; set; }

        public int IdReceta { get; set; }

        public string NombreReceta { get; set; }

        /// Cantidad vendida. Vale 0 en los eventos de continuación, que solo
        /// agregan consumos de una venta con muchos insumos.
        public decimal Cantidad { get; set; }

        public List<BEConsumoVenta> Consumos
        {
            get { return consumos; }
        }
    }
}
