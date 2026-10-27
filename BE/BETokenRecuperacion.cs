using System;

namespace BE
{
    /// Enlace de un solo uso que habilita a un usuario a definir una contraseña
    /// nueva sin intervención del administrador. Del enlace enviado por correo
    /// solo se guarda su hash SHA-256, nunca el valor original.
    public class BETokenRecuperacion
    {
        public string TokenHash { get; set; }

        public int DniUsuario { get; set; }

        public DateTime FechaExpiracion { get; set; }

        public bool Usado { get; set; }

        /// Sirve una sola vez y mientras no se haya vencido.
        public bool EsUtilizable
        {
            get { return !Usado && FechaExpiracion > DateTime.Now; }
        }
    }
}
