using System;
using System.Collections.Generic;

namespace BE
{
    /// Venta registrada (CU-006-037), tabla Venta: la receta, la cantidad
    /// vendida, la fecha de la venta y lo que se consumió de cada insumo (tabla
    /// ConsumoVenta). Es el historial con el que se entrena la predicción de
    /// demanda. También es cada línea de un pedido antes de registrarlo.
    [Serializable]
    public class BEVenta
    {
        private readonly List<BEConsumoVenta> consumos = new List<BEConsumoVenta>();

        public int IdVenta { get; set; }

        public DateTime Fecha { get; set; }

        public int IdReceta { get; set; }

        public string NombreReceta { get; set; }

        public decimal Cantidad { get; set; }

        public List<BEConsumoVenta> Consumos
        {
            get { return consumos; }
        }

        public string FechaTexto
        {
            get { return Fecha.ToString("dd/MM/yyyy"); }
        }

        public string CantidadTexto
        {
            get { return Cantidad.ToString("0.##"); }
        }
    }
}
