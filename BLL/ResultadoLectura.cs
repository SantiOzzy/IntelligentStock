using System;

namespace BLL
{
    /// Qué pasó con una lectura de temperatura que mandó un sensor.
    public enum TipoResultadoLectura
    {
        /// La temperatura está dentro de lo permitido: no se graba nada.
        SinNovedad,
        /// Se generó una alerta térmica nueva.
        AlertaGenerada,
        /// El sensor ya tenía una alerta abierta y la lectura fue más alta: se
        /// actualizó la temperatura alcanzada.
        AlertaActualizada,
        /// El sensor ya tenía una alerta abierta con una temperatura igual o mayor.
        AlertaYaAbierta,
        /// No hay ningún sensor activo con esa dirección MAC.
        SensorNoRegistrado,
        /// La MAC o la temperatura recibidas no son válidas.
        DatosInvalidos,
        /// R9: la petición no vino de una IP autorizada.
        AccesoDenegado,
        /// No se pudo procesar (por ejemplo, la base de datos no responde).
        Error
    }

    /// Respuesta que devuelve el servicio de telemetría al sistema externo.
    public class ResultadoLectura
    {
        public ResultadoLectura()
        {
        }

        public ResultadoLectura(TipoResultadoLectura tipo, int idAlerta, string mensaje)
        {
            Tipo = tipo;
            IdAlerta = idAlerta;
            Mensaje = mensaje;
        }

        public TipoResultadoLectura Tipo { get; set; }

        /// Alerta generada o actualizada (0 si no hubo alerta).
        public int IdAlerta { get; set; }

        public string Mensaje { get; set; }
    }
}
