using System;

namespace BE
{
    /// Usuario del sistema. Se corresponde con la tabla Usuario.
    public class BEUsuario
    {
        /// Intentos fallidos consecutivos con los que la cuenta se bloquea sola.
        /// Ojo si se sube: las cuentas ya bloqueadas por intentos con el
        /// contador por debajo del máximo nuevo pasarían a verse como
        /// bloqueadas por un administrador (ver BloqueadoManualmente).
        public const int MaximoIntentosFallidos = 3;

        public BEUsuario()
        {
        }

        public BEUsuario(int dni, int idRol, string nombre, string apellido, string nombreUsuario)
        {
            DNI = dni;
            IdRol = idRol;
            Nombre = nombre;
            Apellido = apellido;
            NombreUsuario = nombreUsuario;
        }

        public int DNI { get; set; }

        public int IdRol { get; set; }

        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public DateTime? FechaNacimiento { get; set; }

        public string CorreoElectronico { get; set; }

        public string NumeroTelefono { get; set; }

        public string NombreUsuario { get; set; }

        /// Contrasena encriptada con SHA-256 (columna contrasena de la tabla Usuario).
        public string Clave { get; set; }

        public bool Activo { get; set; }

        public bool Bloqueado { get; set; }

        public int IntentosFallidos { get; set; }

        /// Dígito verificador horizontal del registro.
        public string DVH { get; set; }

        public BERol Rol { get; set; }

        public string NombreCompleto
        {
            get { return Apellido + ", " + Nombre; }
        }

        public string NombreRol
        {
            get { return Rol != null ? Rol.Nombre : string.Empty; }
        }

        /// La cuenta se bloqueó sola por agotar los intentos de inicio de sesión.
        /// El bloqueo que hace un administrador (CU-003-014) deja el contador en
        /// cero, así que un bloqueo con el contador por debajo del máximo es
        /// siempre manual. La diferencia importa porque el bloqueo por intentos
        /// lo puede levantar el propio usuario con "¿Olvidaste tu contraseña?",
        /// y el manual solo un administrador.
        public bool BloqueadoPorIntentos
        {
            get { return Bloqueado && IntentosFallidos >= MaximoIntentosFallidos; }
        }

        /// Bloqueada por un administrador desde Bloquear usuario.
        public bool BloqueadoManualmente
        {
            get { return Bloqueado && IntentosFallidos < MaximoIntentosFallidos; }
        }

        /// Por qué está bloqueada la cuenta (para la grilla de Desbloquear usuario).
        public string MotivoBloqueo
        {
            get
            {
                if (!Bloqueado)
                {
                    return string.Empty;
                }
                return BloqueadoPorIntentos
                    ? "Intentos fallidos (" + IntentosFallidos + ")"
                    : "Lo bloqueó un administrador";
            }
        }

        public string Estado
        {
            get
            {
                if (!Activo)
                {
                    return "Inactivo";
                }
                return Bloqueado ? "Bloqueado" : "Activo";
            }
        }

        /// Atajo que delega la pregunta en el Composite de permisos del rol.
        public bool TienePermiso(string nombrePatente)
        {
            return Rol != null && Rol.TienePermiso(nombrePatente);
        }
    }
}
