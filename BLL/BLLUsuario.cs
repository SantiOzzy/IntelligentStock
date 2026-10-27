using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de usuarios: login, ABM, bloqueo y clave.
    public class BLLUsuario
    {
        /// Intentos fallidos consecutivos antes de bloquear la cuenta.
        public const int MaximoIntentosFallidos = BEUsuario.MaximoIntentosFallidos;

        /// Nombre del rol con privilegios de administracion del sistema.
        public const string RolAdministrador = "WebMaster";

        /// Patente que habilita la reparacion del dígito verificador.
        public const string PatenteReparacion = "ReparacionDV";

        /// Edades admitidas para la fecha de nacimiento de un usuario.
        public const int EdadMinima = 18;
        public const int EdadMaxima = 100;

        private readonly DALUsuario dalUsuario = new DALUsuario();
        private readonly BLLPermiso bllPermiso = new BLLPermiso();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        #region Inicio de sesión

        /// CU-001-001 Iniciar sesion.
        /// Valida credenciales, verifica la integridad de la base de datos y
        /// devuelve el usuario con su arbol de permisos cargado.
        public ResultadoLogin Login(string nombreUsuario, string clave)
        {
            if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(clave))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            // El usuario de contingencia se resuelve antes de tocar la base de
            // datos: tiene que poder entrar siempre, exista o no la base, así
            // que no depende de que la consulta funcione ni de que la cuenta
            // esté cargada. No hay ninguna cuenta real con ese nombre porque el
            // alta de usuario lo rechaza.
            ResultadoLogin contingencia = IntentarLoginDeContingencia(nombreUsuario, clave);
            if (contingencia != null)
            {
                return contingencia;
            }

            // Si el motor de base de datos no responde (por ejemplo, porque la
            // base IntelligentStock no existe), la excepción sube tal cual: el
            // único acceso que quedaba era el de contingencia, y ya se probó.
            BEUsuario usuario = dalUsuario.ValidarUsuario(nombreUsuario.Trim());

            if (usuario == null)
            {
                bllEvento.Registrar(null, "Inicio de sesión fallido",
                    "Intento de acceso con un nombre de usuario inexistente: " + nombreUsuario.Trim() + ".",
                    CriticidadEvento.Advertencia);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR003"));
            }

            if (!usuario.Activo)
            {
                bllEvento.Registrar(usuario.DNI, "Inicio de sesión fallido",
                    "La cuenta se encuentra dada de baja.", CriticidadEvento.Advertencia);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR004"));
            }

            if (usuario.BloqueadoManualmente)
            {
                bllEvento.Registrar(usuario.DNI, "Inicio de sesión fallido",
                    "La cuenta se encuentra bloqueada por un administrador.", CriticidadEvento.Advertencia);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR004",
                    "Un administrador del sistema bloqueó la cuenta: comuníquese con él para que la desbloquee."));
            }

            if (usuario.Bloqueado)
            {
                bllEvento.Registrar(usuario.DNI, "Inicio de sesión fallido",
                    "La cuenta se encuentra bloqueada por intentos fallidos.", CriticidadEvento.Advertencia);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR004",
                    "Se bloqueó por superar los intentos permitidos: use \"¿Olvidaste tu contraseña?\" " +
                    "para definir una contraseña nueva o comuníquese con el administrador del sistema."));
            }

            // R4 - Verificar dígito verificador. Se hace antes de cualquier
            // escritura para no pisar los digitos de una base ya alterada.
            List<Inconsistencia> inconsistencias = bllDV.VerificarIntegridad();
            bool baseConsistente = inconsistencias.Count == 0;

            // R5 - Encriptar datos mediante SHA-256.
            if (Encriptacion.EncriptarSHA256(clave) != usuario.Clave)
            {
                RegistrarIntentoFallido(usuario, baseConsistente);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR003"));
            }

            CargarRol(usuario);

            ResultadoLogin resultado = new ResultadoLogin();
            resultado.Usuario = usuario;
            resultado.Inconsistencias = inconsistencias;
            resultado.EsAdministrador = EsAdministrador(usuario);

            if (!baseConsistente)
            {
                bllEvento.Registrar(usuario.DNI, "Inconsistencia en la base de datos",
                    "Se detectaron " + inconsistencias.Count +
                    " inconsistencias de dígito verificador durante el inicio de sesión. [#ERR007]",
                    CriticidadEvento.Grave);
                return resultado;
            }

            if (usuario.IntentosFallidos != 0)
            {
                bllDV.EjecutarControlado(() => dalUsuario.ModificarContFallido(usuario.NombreUsuario, 0), "Usuario");
                usuario.IntentosFallidos = 0;
            }

            bllEvento.Registrar(usuario.DNI, "Inicio de sesión",
                "El usuario " + usuario.NombreUsuario + " inició sesión con el perfil " + usuario.NombreRol + ".");

            return resultado;
        }

        /// Usuario hardcodeado en el Web.config (claves "UsuarioContingencia" /
        /// "ClaveContingenciaSHA256") para poder entrar cuando la base de datos
        /// no existe o el motor no responde. Solo trae las patentes mínimas
        /// para llegar a BackUp/Restore/ReparacionDV (que no dependen de que
        /// exista la base IntelligentStock) y a la bitácora. No se registra
        /// nada en la bitácora: para eso también hace falta la base.
        private static ResultadoLogin IntentarLoginDeContingencia(string nombreUsuario, string clave)
        {
            string usuarioConfigurado = ConfigurationManager.AppSettings["UsuarioContingencia"];
            string claveConfigurada = ConfigurationManager.AppSettings["ClaveContingenciaSHA256"];

            if (string.IsNullOrEmpty(usuarioConfigurado) || string.IsNullOrEmpty(claveConfigurada))
            {
                return null;
            }

            if (!string.Equals(nombreUsuario.Trim(), usuarioConfigurado, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!string.Equals(Encriptacion.EncriptarSHA256(clave), claveConfigurada, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            BERol rol = new BERol(0, "Contingencia");
            rol.Agregar(new Patente(0, "BackUp"));
            rol.Agregar(new Patente(0, "Restore"));
            rol.Agregar(new Patente(0, "ReparacionDV"));
            rol.Agregar(new Patente(0, "BitacoraEventos"));

            BEUsuario usuario = new BEUsuario();
            usuario.DNI = 0;
            usuario.NombreUsuario = usuarioConfigurado;
            usuario.Nombre = "Usuario de contingencia";
            usuario.Apellido = string.Empty;
            usuario.Activo = true;
            usuario.Bloqueado = false;
            usuario.Rol = rol;

            ResultadoLogin resultado = new ResultadoLogin();
            resultado.Usuario = usuario;
            resultado.EsAdministrador = true;
            return resultado;
        }

        /// CU-001-002 Cerrar sesion.
        public void Logout(BEUsuario usuario)
        {
            // DNI 0 es el usuario de contingencia: no tiene fila en la tabla
            // Usuario y puede estar operando justamente porque la base no
            // está disponible, así que no se intenta escribir la bitácora.
            if (usuario != null && usuario.DNI != 0)
            {
                bllEvento.Registrar(usuario.DNI, "Cierre de sesión",
                    "El usuario " + usuario.NombreUsuario + " cerró su sesión.");
            }

            SessionManager.CerrarSesion();
        }

        private void RegistrarIntentoFallido(BEUsuario usuario, bool baseConsistente)
        {
            // Si la base esta alterada no se escribe nada para no enmascarar la inconsistencia.
            if (!baseConsistente)
            {
                bllEvento.Registrar(usuario.DNI, "Inicio de sesión fallido",
                    "Credenciales incorrectas. No se actualizó el contador por inconsistencia de integridad.",
                    CriticidadEvento.Advertencia);
                return;
            }

            // Se vuelve a leer la cuenta dentro del candado: entre la lectura del
            // inicio de sesión y esta escritura un administrador pudo haberla
            // bloqueado, y sumar un intento sobre ese bloqueo lo convertiría en
            // un bloqueo "por intentos" (que el usuario puede levantar solo).
            int intentos = 0;
            bool contado = bllDV.EjecutarControlado(delegate
            {
                BEUsuario actual = dalUsuario.ObtenerPorDNI(usuario.DNI);
                if (actual == null || !actual.Activo || actual.Bloqueado)
                {
                    return false;
                }

                intentos = actual.IntentosFallidos + 1;
                dalUsuario.ModificarContFallido(actual.NombreUsuario, intentos);
                if (intentos >= MaximoIntentosFallidos)
                {
                    dalUsuario.ModificarBloqueo(actual.DNI, true);
                }
                return true;
            }, "Usuario");

            if (!contado)
            {
                bllEvento.Registrar(usuario.DNI, "Inicio de sesión fallido",
                    "Credenciales incorrectas.", CriticidadEvento.Advertencia);
                return;
            }

            if (intentos >= MaximoIntentosFallidos)
            {
                bllEvento.Registrar(usuario.DNI, "Bloqueo de usuario",
                    "La cuenta se bloqueó automáticamente tras " + intentos + " intentos fallidos.",
                    CriticidadEvento.Grave);
                return;
            }

            bllEvento.Registrar(usuario.DNI, "Inicio de sesión fallido",
                "Credenciales incorrectas. Intento " + intentos + " de " + MaximoIntentosFallidos + ".",
                CriticidadEvento.Advertencia);
        }

        /// Carga el rol del usuario con su arbol de permisos (Composite).
        public void CargarRol(BEUsuario usuario)
        {
            if (usuario == null)
            {
                return;
            }

            if (usuario.Rol == null)
            {
                usuario.Rol = new BERol(usuario.IdRol, string.Empty);
            }

            bllPermiso.CargarPermisos(usuario.Rol);
        }

        public bool EsAdministrador(BEUsuario usuario)
        {
            if (usuario == null || usuario.Rol == null)
            {
                return false;
            }

            return string.Equals(usuario.Rol.Nombre, RolAdministrador, StringComparison.OrdinalIgnoreCase) ||
                   usuario.Rol.TienePermiso(PatenteReparacion);
        }

        #endregion

        #region Consultas

        public List<BEUsuario> TraerUsuarios()
        {
            return dalUsuario.TraerUsuarios();
        }

        public BEUsuario ObtenerPorDNI(int dni)
        {
            return dalUsuario.ObtenerPorDNI(dni);
        }

        /// Filtra el listado de usuarios por texto libre, rol y estado.
        public List<BEUsuario> Filtrar(string texto, int idRol, string estado)
        {
            List<BEUsuario> usuarios = TraerUsuarios();
            List<BEUsuario> resultado = new List<BEUsuario>();

            foreach (BEUsuario usuario in usuarios)
            {
                if (idRol > 0 && usuario.IdRol != idRol)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(estado) && usuario.Estado != estado)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(texto))
                {
                    string busqueda = texto.Trim();
                    bool coincide = Contiene(usuario.NombreUsuario, busqueda) ||
                                    Contiene(usuario.Nombre, busqueda) ||
                                    Contiene(usuario.Apellido, busqueda) ||
                                    Contiene(usuario.CorreoElectronico, busqueda) ||
                                    usuario.DNI.ToString().IndexOf(busqueda, StringComparison.Ordinal) >= 0;
                    if (!coincide)
                    {
                        continue;
                    }
                }

                resultado.Add(usuario);
            }

            return resultado;
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return !string.IsNullOrEmpty(valor) &&
                   valor.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

        #region ABM

        /// CU-003-009 Alta de usuario.
        public void Alta(BEUsuario usuario, string claveEnClaro)
        {
            ValidarObligatorios(usuario);
            ValidarComplejidadClave(claveEnClaro);

            // Nadie puede crear una cuenta con más permisos que los suyos.
            VerificarQueElActorCubraElRol(usuario.IdRol);

            if (EsNombreReservado(usuario.NombreUsuario))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                    "El nombre de usuario " + usuario.NombreUsuario + " está reservado por el sistema."));
            }

            BEUsuario existentePorDni = dalUsuario.ObtenerPorDNI(usuario.DNI);
            if (existentePorDni != null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                    "Ya existe un usuario con el DNI " + usuario.DNI +
                    (existentePorDni.Activo ? "." : ", dado de baja. Puede reactivarlo desde Baja de usuario.")));
            }

            if (dalUsuario.ValidarUsuario(usuario.NombreUsuario) != null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                    "Ya existe el nombre de usuario " + usuario.NombreUsuario + "."));
            }

            VerificarCorreoUnico(usuario.CorreoElectronico, usuario.DNI);

            usuario.Clave = Encriptacion.EncriptarSHA256(claveEnClaro);
            bllDV.EjecutarControlado(() => dalUsuario.Crear(usuario), "Usuario");
            bllEvento.RegistrarDelUsuarioActual("Alta de usuario",
                "Se dio de alta al usuario " + usuario.NombreUsuario + " (DNI " + usuario.DNI + ").");
        }

        /// CU-003-010 Modificar usuario.
        public void Modificar(BEUsuario usuario)
        {
            ValidarObligatorios(usuario);

            BEUsuario anterior = dalUsuario.ObtenerPorDNI(usuario.DNI);
            if (anterior == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            // Nadie puede modificar una cuenta con más permisos que los suyos, ni
            // pasar a alguien a un rol con más permisos que los suyos.
            VerificarQueElActorCubraElRol(anterior.IdRol);
            if (usuario.IdRol != anterior.IdRol)
            {
                VerificarQueElActorCubraElRol(usuario.IdRol);
            }

            if (EsNombreReservado(usuario.NombreUsuario))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                    "El nombre de usuario " + usuario.NombreUsuario + " está reservado por el sistema."));
            }

            BEUsuario existente = dalUsuario.ValidarUsuario(usuario.NombreUsuario);
            if (existente != null && existente.DNI != usuario.DNI)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                    "El nombre de usuario " + usuario.NombreUsuario + " pertenece a otra cuenta."));
            }

            if (anterior.Activo)
            {
                VerificarCorreoUnico(usuario.CorreoElectronico, usuario.DNI);
            }

            // Nadie puede cambiarse el rol a sí mismo: si el WebMaster se pasara a
            // un perfil sin permisos de seguridad, el sistema quedaría sin nadie
            // que pueda administrarlo.
            if (anterior.IdRol != usuario.IdRol)
            {
                VerificarQueNoSeaLaPropiaCuenta(usuario.DNI,
                    "No puede cambiar su propio rol. Pídaselo a otro usuario con perfil de administración.");

                VerificarQueQuedeAlgunAdministrador(anterior, usuario.IdRol);
            }

            bllDV.EjecutarControlado(() => dalUsuario.Modificar(usuario), "Usuario");
            bllEvento.RegistrarDelUsuarioActual("Modificación de usuario",
                "Se modificaron los datos del usuario " + usuario.NombreUsuario + " (DNI " + usuario.DNI + ").");
        }

        /// CU-003-011 Baja de usuario.
        public void Baja(int dni)
        {
            VerificarQueNoSeaLaPropiaCuenta(dni);

            BEUsuario usuario = dalUsuario.ObtenerPorDNI(dni);
            if (usuario == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            VerificarQueElActorPuedaTocarAdministrador(usuario);

            if (!usuario.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El usuario ya se encuentra dado de baja."));
            }

            VerificarQueQuedeAlgunAdministrador(usuario, 0);

            bllDV.EjecutarControlado(() => dalUsuario.Baja(dni), "Usuario");
            bllEvento.RegistrarDelUsuarioActual("Baja de usuario",
                "Se dio de baja al usuario " + usuario.NombreUsuario + " (DNI " + dni + ").");
        }

        /// Vuelve a habilitar una cuenta dada de baja (por ejemplo, si se dio de
        /// baja por error). Se hace desde Baja de usuario.
        public void Reactivar(int dni)
        {
            BEUsuario usuario = dalUsuario.ObtenerPorDNI(dni);
            if (usuario == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            VerificarQueElActorPuedaTocarAdministrador(usuario);

            if (usuario.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El usuario ya se encuentra activo."));
            }

            VerificarCorreoUnico(usuario.CorreoElectronico, usuario.DNI);

            // El procedimiento de reactivación limpia el bloqueo y el contador.
            // Si la cuenta la había bloqueado un administrador, ese bloqueo se
            // conserva: reactivar no reemplaza a Desbloquear usuario.
            bool bloqueoManual = usuario.BloqueadoManualmente;
            bllDV.EjecutarControlado(delegate
            {
                dalUsuario.Activar(dni);
                if (bloqueoManual)
                {
                    dalUsuario.ModificarBloqueo(dni, true);
                    dalUsuario.ModificarContFallido(usuario.NombreUsuario, 0);
                }
            }, "Usuario");
            bllEvento.RegistrarDelUsuarioActual("Reactivación de usuario",
                "Se reactivó al usuario " + usuario.NombreUsuario + " (DNI " + dni + ").");
        }

        /// CU-003-014 Bloquear usuario.
        public void Bloquear(int dni)
        {
            VerificarQueNoSeaLaPropiaCuenta(dni);

            BEUsuario usuario = dalUsuario.ObtenerPorDNI(dni);
            if (usuario == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            VerificarQueElActorPuedaTocarAdministrador(usuario);

            if (usuario.BloqueadoManualmente)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El usuario ya se encuentra bloqueado por un administrador."));
            }

            // Una cuenta bloqueada por intentos fallidos también se puede
            // bloquear acá: así el bloqueo pasa a ser manual y el usuario (o
            // quien haya provocado los intentos) ya no lo puede levantar con la
            // recuperación de contraseña.
            bool estabaBloqueadoPorIntentos = usuario.BloqueadoPorIntentos;

            if (!estabaBloqueadoPorIntentos)
            {
                VerificarQueQuedeAlgunAdministrador(usuario, 0);
            }

            // El contador en cero es lo que marca el bloqueo como manual (ver
            // BEUsuario.BloqueadoManualmente).
            bllDV.EjecutarControlado(delegate
            {
                dalUsuario.ModificarBloqueo(dni, true);
                dalUsuario.ModificarContFallido(usuario.NombreUsuario, 0);
            }, "Usuario");

            // Los enlaces de recuperación que estuvieran pendientes no sirven más.
            dalUsuario.InvalidarTokensDeUsuario(dni);

            bllEvento.RegistrarDelUsuarioActual("Bloqueo de usuario",
                (estabaBloqueadoPorIntentos
                    ? "Se bloqueó manualmente al usuario " + usuario.NombreUsuario + " (DNI " + dni +
                      "), que ya estaba bloqueado por intentos fallidos."
                    : "Se bloqueó al usuario " + usuario.NombreUsuario + " (DNI " + dni + ").") +
                " Solo un administrador puede desbloquearlo.",
                CriticidadEvento.Advertencia);
        }

        /// CU-003-015 Desbloquear usuario.
        public void Desbloquear(int dni)
        {
            BEUsuario usuario = dalUsuario.ObtenerPorDNI(dni);
            if (usuario == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            VerificarQueElActorPuedaTocarAdministrador(usuario);

            if (!usuario.Bloqueado)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El usuario no se encuentra bloqueado."));
            }

            bllDV.EjecutarControlado(delegate
            {
                dalUsuario.ModificarBloqueo(dni, false);
                dalUsuario.ModificarContFallido(usuario.NombreUsuario, 0);
            }, "Usuario");
            bllEvento.RegistrarDelUsuarioActual("Desbloqueo de usuario",
                "Se desbloqueó al usuario " + usuario.NombreUsuario + " (DNI " + dni + ").");
        }

        /// CU-003-013 Cambiar contraseña.
        public void CambiarClave(BEUsuario usuario, string claveActual, string claveNueva, string claveConfirmacion)
        {
            if (usuario == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR004"));
            }

            if (string.IsNullOrEmpty(claveActual) || string.IsNullOrEmpty(claveNueva) ||
                string.IsNullOrEmpty(claveConfirmacion))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            // Se busca por DNI y no por el nombre guardado en la sesión, porque un
            // administrador pudo haberle cambiado el nombre de usuario mientras tanto.
            BEUsuario porDni = dalUsuario.ObtenerPorDNI(usuario.DNI);
            BEUsuario almacenado = porDni == null ? null : dalUsuario.ValidarUsuario(porDni.NombreUsuario);
            if (almacenado == null || Encriptacion.EncriptarSHA256(claveActual) != almacenado.Clave)
            {
                bllEvento.Registrar(usuario.DNI, "Cambio de contraseña fallido",
                    "Se ingresó mal la contraseña actual al intentar cambiarla.", CriticidadEvento.Advertencia);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR003"));
            }

            ValidarComplejidadClave(claveNueva);

            if (claveNueva != claveConfirmacion)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR014"));
            }

            bllDV.EjecutarControlado(
                () => dalUsuario.CambiarClave(almacenado.NombreUsuario, Encriptacion.EncriptarSHA256(claveNueva)),
                "Usuario");
            bllEvento.Registrar(usuario.DNI, "Cambio de contraseña",
                "El usuario " + almacenado.NombreUsuario + " cambió su contraseña.");

            EnviarCorreoDeConfirmacion(almacenado, false);
        }

        /// Notifica por correo que la contraseña cambió, para que el usuario
        /// pueda reaccionar si no fue él. Si falla el envío (por ejemplo, sin
        /// SMTP configurado) no se revierte el cambio: ya quedó aplicado y
        /// registrado en la bitácora.
        /// Devuelve true si el correo salió (o al menos se intentó con el SMTP
        /// configurado), para que la pantalla no prometa un aviso que no se envió.
        private static bool EnviarCorreoDeConfirmacion(BEUsuario usuario, bool loRestablecioUnAdministrador)
        {
            if (usuario == null || string.IsNullOrEmpty(usuario.CorreoElectronico) ||
                !EnviadorCorreo.EstaConfigurado)
            {
                return false;
            }

            string motivo = loRestablecioUnAdministrador
                ? "Un administrador del sistema restableció la contraseña de su cuenta (" + usuario.NombreUsuario +
                  ") el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ". Pídale la contraseña nueva y " +
                  "cámbiela desde Cambiar contraseña la próxima vez que ingrese."
                : "Le confirmamos que la contraseña de su cuenta (" + usuario.NombreUsuario +
                  ") se cambió el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + ". Si no fue usted quien " +
                  "hizo este cambio, comuníquese de inmediato con el administrador del sistema.";

            try
            {
                EnviadorCorreo.Enviar(usuario.CorreoElectronico,
                    "IntelligentStock: cambio de contraseña",
                    "Hola " + usuario.Nombre + ",\n\n" + motivo);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// Restablece la clave de otro usuario (función del administrador).
        /// Devuelve true si se le avisó por correo.
        public bool RestablecerClave(int dni, string claveNueva, string claveConfirmacion)
        {
            BEUsuario usuario = dalUsuario.ObtenerPorDNI(dni);
            if (usuario == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            VerificarQueElActorPuedaTocarAdministrador(usuario);

            ValidarComplejidadClave(claveNueva);

            if (claveNueva != claveConfirmacion)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR014"));
            }

            bllDV.EjecutarControlado(
                () => dalUsuario.CambiarClave(usuario.NombreUsuario, Encriptacion.EncriptarSHA256(claveNueva)),
                "Usuario");
            bllEvento.RegistrarDelUsuarioActual("Cambio de contraseña",
                "Se restableció la contraseña del usuario " + usuario.NombreUsuario + ".");

            return EnviarCorreoDeConfirmacion(usuario, true);
        }

        #endregion

        #region Recuperación de contraseña

        /// Minutos que sigue sirviendo el enlace de recuperación desde que se envía.
        public const int MinutosVigenciaEnlaceRecuperacion = 60;

        /// CU-003-013 Cambiar contraseña (variante sin administrador).
        /// El usuario pide el enlace desde la pantalla de inicio de sesión y lo
        /// recibe en su correo, así que no necesita que un administrador le
        /// restablezca la clave.
        ///
        /// Nunca informa si el correo existe o no: responder distinto según el
        /// caso le permitiría a un intruso averiguar qué cuentas hay dadas de
        /// alta. El formulario muestra siempre el mismo aviso.
        public void SolicitarRecuperacionDeClave(string correo, string urlDeLaPantallaDeLogin)
        {
            if (string.IsNullOrEmpty(correo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            BEUsuario usuario = BuscarPorCorreo(correo.Trim());

            if (usuario == null)
            {
                bllEvento.Registrar(null, "Recuperación de contraseña",
                    "Se pidió un enlace de recuperación para un correo que no corresponde a ninguna cuenta.",
                    CriticidadEvento.Advertencia);
                return;
            }

            if (!usuario.Activo)
            {
                bllEvento.Registrar(usuario.DNI, "Recuperación de contraseña",
                    "Se pidió un enlace de recuperación para una cuenta dada de baja. No se envió.",
                    CriticidadEvento.Advertencia);
                return;
            }

            // Una cuenta que bloqueó un administrador no se puede destrabar por
            // esta vía: si no, el bloqueo manual no serviría de nada. No se le
            // dice nada distinto a quien lo pidió (no se revela si la cuenta
            // existe); el aviso le llega al dueño, a su correo.
            if (usuario.BloqueadoManualmente)
            {
                bllEvento.Registrar(usuario.DNI, "Recuperación de contraseña",
                    "Se pidió un enlace de recuperación para una cuenta bloqueada por un administrador. " +
                    "No se envió el enlace.", CriticidadEvento.Advertencia);

                // Un aviso por hora como máximo: si no, cualquiera que conozca el
                // correo podría llenarle la casilla al dueño de la cuenta.
                if (LimitadorDeEventos.Permitir("aviso-bloqueo:" + usuario.DNI, TimeSpan.FromHours(1)))
                {
                    EnviarAvisoDeCuentaBloqueada(usuario);
                }
                return;
            }

            // Un enlace nuevo deja sin efecto los anteriores.
            dalUsuario.InvalidarTokensDeUsuario(usuario.DNI);

            string token = Encriptacion.GenerarTokenAleatorio();

            BETokenRecuperacion registro = new BETokenRecuperacion();
            registro.TokenHash = Encriptacion.EncriptarSHA256(token);
            registro.DniUsuario = usuario.DNI;
            registro.FechaExpiracion = DateTime.Now.AddMinutes(MinutosVigenciaEnlaceRecuperacion);
            dalUsuario.CrearTokenRecuperacion(registro);

            bllEvento.Registrar(usuario.DNI, "Recuperación de contraseña",
                "Se envió un enlace de recuperación de contraseña al correo del usuario " +
                usuario.NombreUsuario + ".", CriticidadEvento.Advertencia);

            EnviarCorreoDeRecuperacion(usuario, ArmarEnlace(urlDeLaPantallaDeLogin, token));
        }

        /// Comprueba que el enlace exista, no se haya usado y no esté vencido.
        /// Devuelve el usuario dueño del enlace.
        public BEUsuario ValidarTokenDeRecuperacion(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            BETokenRecuperacion registro =
                dalUsuario.ObtenerTokenRecuperacion(Encriptacion.EncriptarSHA256(token.Trim()));

            if (registro == null || !registro.EsUtilizable)
            {
                bllEvento.Registrar(null, "Recuperación de contraseña",
                    "Se intentó usar un enlace de recuperación inválido, usado o vencido.",
                    CriticidadEvento.Advertencia);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El enlace de recuperación no es válido, ya se utilizó o venció. Pida uno nuevo."));
            }

            BEUsuario usuario = dalUsuario.ObtenerPorDNI(registro.DniUsuario);
            if (usuario == null || !usuario.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR004"));
            }

            // El enlace pudo haberse pedido antes de que un administrador
            // bloqueara la cuenta.
            if (usuario.BloqueadoManualmente)
            {
                bllEvento.Registrar(usuario.DNI, "Recuperación de contraseña",
                    "Se intentó usar un enlace de recuperación de una cuenta bloqueada por un administrador.",
                    CriticidadEvento.Advertencia);
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR004",
                    "Un administrador del sistema bloqueó la cuenta: comuníquese con él para que la desbloquee."));
            }

            return usuario;
        }

        /// Define la contraseña nueva a partir del enlace recibido por correo.
        /// El enlace sirve una sola vez.
        public void RecuperarClaveConToken(string token, string claveNueva, string claveConfirmacion)
        {
            BEUsuario usuario = ValidarTokenDeRecuperacion(token);

            ValidarComplejidadClave(claveNueva);

            if (claveNueva != claveConfirmacion)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR014"));
            }

            // Si la cuenta había quedado bloqueada por intentos fallidos, se
            // libera: el dueño ya demostró tener acceso a su correo y el sentido
            // de esta función es no depender de un administrador. La cuenta se
            // vuelve a leer dentro del candado, por si un administrador la
            // bloqueó mientras tanto: ese bloqueo no se toca.
            try
            {
                bllDV.EjecutarControlado(delegate
                {
                    BEUsuario actual = dalUsuario.ObtenerPorDNI(usuario.DNI);
                    if (actual == null || !actual.Activo)
                    {
                        throw new ApplicationException(AdministradorErrores.Mensaje("ERR004"));
                    }
                    if (actual.BloqueadoManualmente)
                    {
                        throw new ApplicationException(AdministradorErrores.Mensaje("ERR004",
                            "Un administrador del sistema bloqueó la cuenta: comuníquese con él para que la desbloquee."));
                    }

                    dalUsuario.CambiarClave(actual.NombreUsuario, Encriptacion.EncriptarSHA256(claveNueva));
                    if (actual.BloqueadoPorIntentos)
                    {
                        dalUsuario.ModificarBloqueo(actual.DNI, false);
                    }
                    dalUsuario.ModificarContFallido(actual.NombreUsuario, 0);
                }, "Usuario");
            }
            catch (ApplicationException ex)
            {
                // A quien no inició sesión no se le muestra qué tabla está alterada.
                if (AdministradorErrores.CodigoDelMensaje(ex.Message) == "ERR007")
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR007",
                        "El sistema no se encuentra disponible en este momento. Por favor avise al " +
                        "administrador del sistema."));
                }
                throw;
            }
            dalUsuario.MarcarTokenRecuperacionUsado(Encriptacion.EncriptarSHA256(token.Trim()));
            bllEvento.Registrar(usuario.DNI, "Cambio de contraseña",
                "El usuario " + usuario.NombreUsuario +
                " definió una contraseña nueva con el enlace de recuperación enviado a su correo.",
                CriticidadEvento.Advertencia);

            EnviarCorreoDeConfirmacion(usuario, false);
        }

        /// Busca la cuenta cuyo correo coincide con el ingresado. Si hay más de
        /// una (el correo no es único: puede estar en una cuenta dada de baja y
        /// en otra activa) se prefiere la activa.
        private BEUsuario BuscarPorCorreo(string correo)
        {
            BEUsuario encontrado = null;
            foreach (BEUsuario usuario in dalUsuario.TraerUsuarios())
            {
                if (!string.IsNullOrEmpty(usuario.CorreoElectronico) &&
                    string.Equals(usuario.CorreoElectronico.Trim(), correo, StringComparison.OrdinalIgnoreCase))
                {
                    if (usuario.Activo)
                    {
                        return usuario;
                    }
                    if (encontrado == null)
                    {
                        encontrado = usuario;
                    }
                }
            }
            return encontrado;
        }

        /// Pega el token a la dirección de la pantalla de inicio de sesión,
        /// respetando si ya traía otros parámetros.
        private static string ArmarEnlace(string urlDeLaPantallaDeLogin, string token)
        {
            if (string.IsNullOrEmpty(urlDeLaPantallaDeLogin))
            {
                return token;
            }

            string separador = urlDeLaPantallaDeLogin.IndexOf('?') >= 0 ? "&" : "?";
            return urlDeLaPantallaDeLogin + separador + "token=" + token;
        }

        private static void EnviarAvisoDeCuentaBloqueada(BEUsuario usuario)
        {
            if (string.IsNullOrEmpty(usuario.CorreoElectronico))
            {
                return;
            }

            try
            {
                EnviadorCorreo.Enviar(usuario.CorreoElectronico,
                    "IntelligentStock: recuperación de contraseña",
                    "Hola " + usuario.Nombre + ",\n\n" +
                    "Recibimos un pedido para recuperar la contraseña de su cuenta (" +
                    usuario.NombreUsuario + "), pero un administrador del sistema bloqueó esa cuenta, " +
                    "así que no se puede recuperar la contraseña por este medio.\n\n" +
                    "Comuníquese con el administrador para que la desbloquee. Si no fue usted quien " +
                    "hizo el pedido, avísele también.");
            }
            catch (Exception)
            {
            }
        }

        private static void EnviarCorreoDeRecuperacion(BEUsuario usuario, string enlace)
        {
            if (string.IsNullOrEmpty(usuario.CorreoElectronico))
            {
                return;
            }

            try
            {
                EnviadorCorreo.Enviar(usuario.CorreoElectronico,
                    "IntelligentStock: recuperación de contraseña",
                    "Hola " + usuario.Nombre + ",\n\n" +
                    "Recibimos un pedido para recuperar la contraseña de su cuenta (" +
                    usuario.NombreUsuario + ").\n\n" +
                    "Para definir una contraseña nueva, ingrese a este enlace:\n" + enlace + "\n\n" +
                    "El enlace sirve una sola vez y vence en " +
                    MinutosVigenciaEnlaceRecuperacion + " minutos.\n\n" +
                    "Si no fue usted quien lo pidió, ignore este mensaje: su contraseña " +
                    "actual sigue funcionando.");
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Validaciones internas

        private static void VerificarQueNoSeaLaPropiaCuenta(int dni)
        {
            VerificarQueNoSeaLaPropiaCuenta(dni, null);
        }

        private static void VerificarQueNoSeaLaPropiaCuenta(int dni, string detalle)
        {
            int? actual = SessionManager.DniActual;
            if (actual.HasValue && actual.Value == dni)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR012", detalle));
            }
        }

        /// Impide dejar el sistema sin ningún administrador. Se controla antes de
        /// bajar, bloquear o cambiarle el rol al último WebMaster activo.
        private void VerificarQueQuedeAlgunAdministrador(BEUsuario afectado, int nuevoIdRol)
        {
            if (afectado == null || !EsRolAdministrador(afectado.IdRol))
            {
                return;
            }

            // Si el nuevo rol también es de administración no hay riesgo.
            if (nuevoIdRol > 0 && EsRolAdministrador(nuevoIdRol))
            {
                return;
            }

            int quedan = 0;
            foreach (BEUsuario otro in dalUsuario.TraerUsuarios())
            {
                if (otro.DNI != afectado.DNI && otro.Activo && !otro.Bloqueado &&
                    EsRolAdministrador(otro.IdRol))
                {
                    quedan++;
                }
            }

            if (quedan == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR011",
                    "Es el único usuario con perfil " + RolAdministrador +
                    " activo: el sistema quedaría sin administrador."));
            }
        }

        /// Bloquear, desbloquear, dar de baja, reactivar o restablecer la
        /// contraseña de una cuenta solo lo puede hacer alguien cuyo rol tenga
        /// todos los permisos del rol de esa cuenta.
        private void VerificarQueElActorPuedaTocarAdministrador(BEUsuario afectado)
        {
            if (afectado == null)
            {
                return;
            }

            VerificarQueElActorCubraElRol(afectado.IdRol);
        }

        /// Un usuario solo puede administrar cuentas (y asignar roles) que no
        /// tengan permisos que su propio rol no tiene. Si no, un perfil con la
        /// patente de usuarios podría crearse, o tomar, una cuenta con más
        /// permisos que los suyos: un WebMaster, o un Técnico con Restore y la
        /// reparación del dígito verificador, y quedarse con todo el sistema.
        private void VerificarQueElActorCubraElRol(int idRol)
        {
            BERol rolDestino;
            string faltante = PatenteQueElActorNoTiene(idRol, bllPermiso.TraerRoles(), out rolDestino);
            if (faltante != null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR009",
                    "Su perfil no puede administrar cuentas con el rol " +
                    (rolDestino != null ? rolDestino.Nombre : idRol.ToString()) +
                    ": ese rol tiene permisos que su perfil no tiene (por ejemplo, " +
                    MenuSistema.DescribirPatente(faltante) + ")."));
            }
        }

        /// Devuelve una patente del rol indicado que el actor no tiene, o null
        /// si el actor tiene todas.
        private static string PatenteQueElActorNoTiene(int idRol, List<BERol> roles, out BERol rolDestino)
        {
            rolDestino = roles.Find(delegate(BERol r) { return r.Id == idRol; });

            BEUsuario actor = SessionManager.UsuarioActual;
            BERol rolActor = actor == null ? null : roles.Find(delegate(BERol r) { return r.Id == actor.IdRol; });

            if (rolDestino == null)
            {
                return null;
            }

            foreach (Patente patente in rolDestino.ObtenerPatentes())
            {
                if (rolActor == null || !rolActor.TienePermiso(patente.Nombre))
                {
                    return patente.Nombre;
                }
            }
            return null;
        }

        /// Para las pantallas: de la lista de roles, los que el usuario con la
        /// sesión activa puede asignar (los que no tienen permisos que él no tiene).
        public List<BERol> FiltrarRolesAsignables(List<BERol> roles)
        {
            return roles.FindAll(delegate(BERol rol)
            {
                BERol destino;
                return PatenteQueElActorNoTiene(rol.Id, roles, out destino) == null;
            });
        }

        /// El nombre del usuario de contingencia del Web.config no se puede usar
        /// para una cuenta real: el inicio de sesión lo resuelve antes que a ellas.
        /// El correo identifica la cuenta en la recuperación de contraseña: si
        /// dos usuarios activos compartieran el correo, el enlace le llegaría
        /// siempre a uno solo de los dos. Las cuentas dadas de baja no cuentan
        /// (por eso la base no tiene un UNIQUE sobre la columna).
        private void VerificarCorreoUnico(string correo, int dniPropio)
        {
            if (string.IsNullOrEmpty(correo))
            {
                return;
            }

            foreach (BEUsuario otro in dalUsuario.TraerUsuarios())
            {
                if (otro.DNI != dniPropio && otro.Activo && !string.IsNullOrEmpty(otro.CorreoElectronico) &&
                    string.Equals(otro.CorreoElectronico.Trim(), correo.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                        "El correo electrónico " + correo.Trim() + " ya lo usa otro usuario activo (" +
                        otro.NombreUsuario + ")."));
                }
            }
        }

        private static bool EsNombreReservado(string nombreUsuario)
        {
            string reservado = ConfigurationManager.AppSettings["UsuarioContingencia"];
            return !string.IsNullOrEmpty(reservado) && !string.IsNullOrEmpty(nombreUsuario) &&
                   string.Equals(nombreUsuario.Trim(), reservado.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private bool EsRolAdministrador(int idRol)
        {
            foreach (BERol rol in bllPermiso.TraerRoles())
            {
                if (rol.Id == idRol)
                {
                    return string.Equals(rol.Nombre, RolAdministrador, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        private static void ValidarObligatorios(BEUsuario usuario)
        {
            if (usuario == null ||
                usuario.DNI <= 0 ||
                usuario.IdRol <= 0 ||
                string.IsNullOrEmpty(usuario.Nombre) ||
                string.IsNullOrEmpty(usuario.Apellido) ||
                string.IsNullOrEmpty(usuario.NombreUsuario))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            if (usuario.DNI < 1000000 || usuario.DNI > 99999999)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El DNI debe tener 7 u 8 dígitos."));
            }

            if (usuario.NombreUsuario.Trim().Length < 4)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El nombre de usuario debe tener al menos 4 caracteres."));
            }

            ValidarFechaNacimiento(usuario.FechaNacimiento);
            ValidarCorreo(usuario.CorreoElectronico);
        }

        /// La fecha de nacimiento no puede ser futura ni de ayer: se exige una
        /// edad razonable para alguien que va a operar el sistema.
        public static void ValidarFechaNacimiento(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "La fecha de nacimiento es obligatoria."));
            }

            DateTime valor = fecha.Value.Date;

            if (valor > DateTime.Today)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de nacimiento no puede ser posterior a hoy."));
            }

            int edad = DateTime.Today.Year - valor.Year;
            if (valor > DateTime.Today.AddYears(-edad))
            {
                edad--;
            }

            if (edad < EdadMinima)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El usuario debe tener al menos " + EdadMinima + " años."));
            }

            if (edad > EdadMaxima)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La fecha de nacimiento no es válida: la edad supera los " + EdadMaxima + " años."));
            }
        }

        /// Mínimo 8 caracteres, con al menos una mayúscula, una minúscula y un número.
        private static readonly Regex RegexComplejidadClave =
            new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");

        public static void ValidarComplejidadClave(string clave)
        {
            if (string.IsNullOrEmpty(clave) || !RegexComplejidadClave.IsMatch(clave))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La contraseña debe tener al menos 8 caracteres, con una mayúscula, " +
                    "una minúscula y un número."));
            }
        }

        private static void ValidarCorreo(string correo)
        {
            if (string.IsNullOrEmpty(correo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002",
                    "El correo electrónico es obligatorio."));
            }

            correo = correo.Trim();
            int arroba = correo.IndexOf('@');
            int punto = correo.LastIndexOf('.');

            bool valido = arroba > 0 &&
                          punto > arroba + 1 &&
                          punto < correo.Length - 1 &&
                          correo.IndexOf(' ') < 0;

            if (!valido)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."));
            }
        }

        #endregion
    }
}
