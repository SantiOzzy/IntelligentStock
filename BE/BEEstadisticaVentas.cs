using System;
using System.Collections.Generic;

namespace BE
{
    /// Estadísticas de ventas de un período (WebServiceVentas): totales,
    /// promedio diario, la receta más vendida y el detalle por receta. Salen
    /// del historial de ventas que queda en la bitácora (acción "Venta").
    [Serializable]
    public class BEEstadisticaVentas
    {
        public BEEstadisticaVentas()
        {
            Recetas = new List<BEVentaPorReceta>();
        }

        public DateTime Desde { get; set; }

        public DateTime Hasta { get; set; }

        /// Días corridos del período, incluidos los dos extremos.
        public int Dias { get; set; }

        public decimal UnidadesVendidas { get; set; }

        /// Cantidad de ventas registradas (cada operación de Descontar stock por venta).
        public int OperacionesDeVenta { get; set; }

        /// Unidades vendidas por día del período (los días sin ventas cuentan).
        public decimal PromedioDiario { get; set; }

        /// Nombre de la receta con más unidades vendidas, o vacío si no hubo ventas.
        public string RecetaMasVendida { get; set; }

        /// El día con más unidades vendidas, si hubo ventas.
        public DateTime? DiaConMasVentas { get; set; }

        public decimal UnidadesDelDiaConMasVentas { get; set; }

        /// Una fila por receta vendida, de la más vendida a la menos vendida.
        public List<BEVentaPorReceta> Recetas { get; set; }
    }

    /// Ventas de una receta en el período.
    [Serializable]
    public class BEVentaPorReceta
    {
        public int IdReceta { get; set; }

        public string Receta { get; set; }

        public decimal UnidadesVendidas { get; set; }

        public int OperacionesDeVenta { get; set; }

        public decimal PromedioDiario { get; set; }

        /// Porcentaje sobre el total de unidades vendidas del período.
        public decimal PorcentajeDelTotal { get; set; }
    }
}
