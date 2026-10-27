using System;

namespace BE
{
    /// Alerta térmica generada por un sensor IoT cuando informa una temperatura
    /// por encima de la máxima permitida. Se corresponde con la tabla Alerta.
    [Serializable]
    public class BEAlerta
    {
        /// Estados por los que pasa una alerta. Nace Pendiente; el técnico la
        /// pasa a En revisión mientras la atiende y la cierra como Resuelta o
        /// Descartada (falsa alarma, sensor mal ubicado, etc.).
        public const string EstadoPendiente = "Pendiente";
        public const string EstadoEnRevision = "En revisión";
        public const string EstadoResuelta = "Resuelta";
        public const string EstadoDescartada = "Descartada";

        public static readonly string[] Estados = new string[]
        {
            EstadoPendiente, EstadoEnRevision, EstadoResuelta, EstadoDescartada
        };

        public int IdAlerta { get; set; }

        public int IdSensor { get; set; }

        public DateTime FechaHora { get; set; }

        public double TemperaturaAlcanzada { get; set; }

        public string Estado { get; set; }

        public string Observaciones { get; set; }

        /// Datos del sensor que la generó (vienen del JOIN con la tabla Sensor).
        public string MacSensor { get; set; }

        public string ModeloSensor { get; set; }

        public double TemperaturaMaximaSensor { get; set; }

        /// Sigue pendiente de atención: no se resolvió ni se descartó.
        public bool Abierta
        {
            get { return EstaAbierta(Estado); }
        }

        public static bool EstaAbierta(string estado)
        {
            return estado != EstadoResuelta && estado != EstadoDescartada;
        }

        public string FechaHoraTexto
        {
            get { return FechaHora.ToString("dd/MM/yyyy HH:mm"); }
        }

        public string TemperaturaTexto
        {
            get { return TemperaturaAlcanzada.ToString("0.0") + " °C"; }
        }

        public string SensorTexto
        {
            get
            {
                return string.IsNullOrEmpty(ModeloSensor)
                    ? MacSensor
                    : MacSensor + " (" + ModeloSensor + ")";
            }
        }

        /// Cuántos grados se pasó de la máxima permitida.
        public double Exceso
        {
            get { return TemperaturaAlcanzada - TemperaturaMaximaSensor; }
        }

        /// Descripción legible de la alerta (se usa también en el XML exportado).
        public string Descripcion
        {
            get
            {
                return "Temperatura de " + TemperaturaTexto + " en el sensor " + SensorTexto +
                       " (máxima permitida " + TemperaturaMaximaSensor.ToString("0.0") + " °C)";
            }
        }

        /// Texto para mostrar la alerta en una lista desplegable.
        public string Etiqueta
        {
            get
            {
                return "N° " + IdAlerta + " - " + FechaHoraTexto + " - " + SensorTexto + " - " +
                       TemperaturaTexto + " - " + Estado;
            }
        }
    }
}
