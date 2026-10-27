using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace Servicios
{
    /// Envío de correo por SMTP. La configuración vive en el Web.config
    /// (claves "Smtp*"); si no está completa, Enviar() no hace nada, para que
    /// el sistema pueda seguir funcionando en un ambiente donde todavía no se
    /// configuró una cuenta de correo real.
    public static class EnviadorCorreo
    {
        /// Tiempo máximo de espera del servidor de correo. El envío se hace
        /// dentro del pedido del usuario: si el SMTP no responde, la pantalla no
        /// puede quedar colgada los 100 segundos que espera SmtpClient por defecto.
        private const int MilisegundosDeEspera = 10000;

        /// Hay un servidor SMTP y un remitente configurados en el Web.config.
        public static bool EstaConfigurado
        {
            get
            {
                return !string.IsNullOrEmpty(ConfigurationManager.AppSettings["SmtpHost"]) &&
                       !string.IsNullOrEmpty(ConfigurationManager.AppSettings["SmtpRemitente"]);
            }
        }

        public static void Enviar(string destinatario, string asunto, string cuerpo)
        {
            string host = ConfigurationManager.AppSettings["SmtpHost"];
            string remitente = ConfigurationManager.AppSettings["SmtpRemitente"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(remitente) ||
                string.IsNullOrEmpty(destinatario))
            {
                return;
            }

            int puerto;
            if (!int.TryParse(ConfigurationManager.AppSettings["SmtpPuerto"], out puerto))
            {
                puerto = 587;
            }

            bool usarSsl;
            if (!bool.TryParse(ConfigurationManager.AppSettings["SmtpUsarSSL"], out usarSsl))
            {
                usarSsl = true;
            }

            string usuario = ConfigurationManager.AppSettings["SmtpUsuario"];
            string clave = ConfigurationManager.AppSettings["SmtpClave"];

            using (SmtpClient cliente = new SmtpClient(host, puerto))
            {
                cliente.EnableSsl = usarSsl;
                cliente.Timeout = MilisegundosDeEspera;
                if (!string.IsNullOrEmpty(usuario))
                {
                    cliente.Credentials = new NetworkCredential(usuario, clave);
                }

                using (MailMessage mensaje = new MailMessage(remitente, destinatario, asunto, cuerpo))
                {
                    cliente.Send(mensaje);
                }
            }
        }
    }
}
