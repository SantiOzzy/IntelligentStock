using System;
using System.Configuration;
using System.IO;
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
            Enviar(destinatario, asunto, cuerpo, null, null, null);
        }

        /// Igual que el anterior, con un archivo adjunto (por ejemplo, la
        /// factura en PDF). Si el servidor de correo no responde o no hay
        /// conexión, lanza SmtpException: quien lo llama decide qué mostrar.
        public static void Enviar(string destinatario, string asunto, string cuerpo,
                                  byte[] adjunto, string nombreAdjunto, string tipoAdjunto)
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
            // Google muestra la contraseña de aplicación en grupos de 4 letras
            // separados por espacios: se aceptan escritos así.
            string clave = (ConfigurationManager.AppSettings["SmtpClave"] ?? string.Empty).Replace(" ", string.Empty);

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
                    if (adjunto != null)
                    {
                        mensaje.Attachments.Add(new Attachment(new MemoryStream(adjunto), nombreAdjunto,
                            tipoAdjunto ?? "application/octet-stream"));
                    }
                    cliente.Send(mensaje);
                }
            }
        }
    }
}
