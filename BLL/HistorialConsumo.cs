using System;
using System.Collections.Generic;
using System.Globalization;
using BE;

namespace BLL
{
    /// Consumo diario de cada insumo armado a partir de las ventas registradas.
    /// Es la preparación de los datos para el modelo de predicción: los días
    /// sin ventas cuentan como consumo cero.
    public class HistorialConsumo
    {
        private readonly Dictionary<int, Dictionary<DateTime, double>> consumo =
            new Dictionary<int, Dictionary<DateTime, double>>();

        private DateTime primerDia;
        private DateTime ultimoDia;

        /// Días corridos desde la primera venta hasta el último día considerado.
        public int Dias { get; private set; }

        public IEnumerable<int> Insumos
        {
            get { return consumo.Keys; }
        }

        public static HistorialConsumo Armar(List<BEVenta> ventas, DateTime ultimoDia)
        {
            HistorialConsumo historial = new HistorialConsumo();
            historial.ultimoDia = ultimoDia.Date;
            historial.primerDia = DateTime.MaxValue;

            foreach (BEVenta venta in ventas)
            {
                if (venta.Fecha > historial.ultimoDia)
                {
                    continue;
                }

                if (venta.Fecha < historial.primerDia)
                {
                    historial.primerDia = venta.Fecha;
                }

                foreach (BEConsumoVenta item in venta.Consumos)
                {
                    Dictionary<DateTime, double> porDia;
                    if (!historial.consumo.TryGetValue(item.IdInsumo, out porDia))
                    {
                        porDia = new Dictionary<DateTime, double>();
                        historial.consumo[item.IdInsumo] = porDia;
                    }

                    double previo;
                    porDia.TryGetValue(venta.Fecha, out previo);
                    porDia[venta.Fecha] = previo + (double)item.ConsumoTotal;
                }
            }

            historial.Dias = historial.primerDia == DateTime.MaxValue
                ? 0
                : (historial.ultimoDia - historial.primerDia).Days + 1;
            return historial;
        }

        public double ConsumoDelDia(int idInsumo, DateTime dia)
        {
            Dictionary<DateTime, double> porDia;
            double valor;
            if (consumo.TryGetValue(idInsumo, out porDia) && porDia.TryGetValue(dia.Date, out valor))
            {
                return valor;
            }
            return 0;
        }

        /// Promedio diario de los 'dias' días anteriores a 'fecha' (sin incluirla).
        public float Promedio(int idInsumo, DateTime fecha, int dias)
        {
            double suma = 0;
            for (int k = 1; k <= dias; k++)
            {
                suma += ConsumoDelDia(idInsumo, fecha.Date.AddDays(-k));
            }
            return (float)(suma / dias);
        }

        /// Una fila por insumo y por día, desde que hay 28 días previos para
        /// calcular los promedios.
        public List<FilaConsumo> ArmarFilasDeEntrenamiento()
        {
            List<FilaConsumo> filas = new List<FilaConsumo>();
            if (Dias == 0)
            {
                return filas;
            }

            foreach (int idInsumo in consumo.Keys)
            {
                for (DateTime dia = primerDia.AddDays(28); dia <= ultimoDia; dia = dia.AddDays(1))
                {
                    FilaConsumo fila = new FilaConsumo();
                    fila.Insumo = idInsumo.ToString(CultureInfo.InvariantCulture);
                    fila.DiaSemana = DiaDeLaSemana(dia);
                    fila.Promedio7 = Promedio(idInsumo, dia, 7);
                    fila.Promedio28 = Promedio(idInsumo, dia, 28);
                    fila.Consumo = (float)ConsumoDelDia(idInsumo, dia);
                    filas.Add(fila);
                }
            }

            return filas;
        }

        /// 1 = lunes ... 7 = domingo.
        public static string DiaDeLaSemana(DateTime dia)
        {
            int numero = dia.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)dia.DayOfWeek;
            return numero.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// Fila de datos del modelo: un insumo en un día. ML.NET la lee por
    /// reflexión, por eso es pública y con campos simples.
    public class FilaConsumo
    {
        /// Id del insumo como texto: es una categoría, no un número.
        public string Insumo;

        /// 1 = lunes ... 7 = domingo.
        public string DiaSemana;

        public float Promedio7;

        public float Promedio28;

        /// Lo que se consumió ese día (el valor que el modelo aprende a estimar).
        public float Consumo;
    }

}
