using System;

namespace BE
{
    /// Sensor IoT de temperatura instalado en un equipo de frío. Se corresponde
    /// con la tabla Sensor.
    [Serializable]
    public class BESensor
    {
        public BESensor()
        {
        }

        public BESensor(string macAddress, string modelo, double temperaturaMaxima)
        {
            MacAddress = macAddress;
            Modelo = modelo;
            TemperaturaMaxima = temperaturaMaxima;
            Activo = true;
        }

        public int IdSensor { get; set; }

        /// Identificador de red del dispositivo, con el formato AA:BB:CC:DD:EE:FF.
        public string MacAddress { get; set; }

        public string Modelo { get; set; }

        /// Temperatura máxima permitida (°C). Una lectura por encima genera una alerta.
        public double TemperaturaMaxima { get; set; }

        public bool Activo { get; set; }

        /// Fecha y hora de la última alerta que generó el sensor (no se guarda:
        /// la calcula el procedimiento a partir de la tabla Alerta).
        public DateTime? UltimaAlerta { get; set; }

        /// Alertas del sensor que todavía no se resolvieron ni se descartaron.
        public int AlertasAbiertas { get; set; }

        public string Estado
        {
            get { return Activo ? "Activo" : "Inactivo"; }
        }

        public string ModeloTexto
        {
            get { return string.IsNullOrEmpty(Modelo) ? "(sin modelo)" : Modelo; }
        }

        public string TemperaturaMaximaTexto
        {
            get { return TemperaturaMaxima.ToString("0.0") + " °C"; }
        }

        public string UltimaAlertaTexto
        {
            get { return UltimaAlerta.HasValue ? UltimaAlerta.Value.ToString("dd/MM/yyyy HH:mm") : "Sin alertas"; }
        }

        /// Texto para mostrar el sensor en una lista desplegable.
        public string Etiqueta
        {
            get { return MacAddress + " - " + ModeloTexto + " (máx. " + TemperaturaMaximaTexto + ")"; }
        }
    }
}
