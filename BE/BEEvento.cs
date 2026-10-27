using System;

namespace BE
{
    /// Nivel de criticidad de un evento de la bitácora.
    public enum CriticidadEvento
    {
        Informativa,
        Advertencia,
        Grave
    }

    /// Registro de la bitacora de eventos (tabla Evento).
    public class BEEvento
    {
        public BEEvento()
        {
        }

        public BEEvento(int? dniUsuario, string accion, string descripcion)
            : this(dniUsuario, accion, descripcion, CriticidadEvento.Informativa)
        {
        }

        public BEEvento(int? dniUsuario, string accion, string descripcion, CriticidadEvento criticidad)
        {
            DniUsuario = dniUsuario;
            Accion = accion;
            Descripcion = descripcion;
            Criticidad = criticidad;
        }

        public int IdEvento { get; set; }

        public int? DniUsuario { get; set; }

        /// Nombre de usuario resuelto por join contra la tabla Usuario.
        public string NombreUsuario { get; set; }

        public DateTime FechaHora { get; set; }

        public string Accion { get; set; }

        public string Descripcion { get; set; }

        public CriticidadEvento Criticidad { get; set; }

        public string Fecha
        {
            get { return FechaHora.ToString("dd/MM/yyyy"); }
        }

        public string Hora
        {
            get { return FechaHora.ToString("HH:mm:ss"); }
        }

        public string Usuario
        {
            get { return string.IsNullOrEmpty(NombreUsuario) ? "(sistema)" : NombreUsuario; }
        }
    }
}
