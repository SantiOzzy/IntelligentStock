using System;

namespace BE
{
    /// Tipo de ajuste manual del stock de un insumo.
    public enum TipoAjusteStock
    {
        Incremento,
        Decremento
    }

    /// Ajuste de stock registrado (CU-005-032). No tiene tabla propia: cada
    /// ajuste queda asentado como un evento de la bitácora y se reconstruye
    /// a partir de él para mostrar el historial.
    [Serializable]
    public class BEAjusteStock
    {
        public DateTime FechaHora { get; set; }

        public string Usuario { get; set; }

        public string Insumo { get; set; }

        public string Tipo { get; set; }

        /// Cantidad con su unidad de medida, tal como quedó registrada.
        public string Cantidad { get; set; }

        public string Motivo { get; set; }

        public string Fecha
        {
            get { return FechaHora.ToString("dd/MM/yyyy HH:mm"); }
        }
    }
}
