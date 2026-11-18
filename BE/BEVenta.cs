using System;
using System.Collections.Generic;

namespace BE
{
    /// Venta registrada (CU-006-037), tabla Venta: la receta, la cantidad
    /// vendida, la fecha de la venta, de dónde llegó el dato y lo que se
    /// consumió de cada insumo (tabla ConsumoVenta). Es el historial con el que
    /// se entrena la predicción de demanda.
    [Serializable]
    public class BEVenta
    {
        private readonly List<BEConsumoVenta> consumos = new List<BEConsumoVenta>();

        public int IdVenta { get; set; }

        public DateTime Fecha { get; set; }

        public int IdReceta { get; set; }

        public string NombreReceta { get; set; }

        public decimal Cantidad { get; set; }

        /// De dónde llegó la venta: carga manual, cierre del día, archivo del
        /// punto de venta o servicio web (ver las constantes de BLLVenta).
        public string Origen { get; set; }

        /// Cuándo se registró en el sistema (la fecha de la venta puede ser anterior).
        public DateTime FechaRegistro { get; set; }

        /// Número de línea en el archivo importado (0 si no viene de un archivo).
        public int Linea { get; set; }

        /// Por qué no se puede registrar (solo en la vista previa de una importación).
        public string Error { get; set; }

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
