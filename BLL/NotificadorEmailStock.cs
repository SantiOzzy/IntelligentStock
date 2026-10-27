using System;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Patrón Observer - observador concreto.
    /// Ante un quiebre de stock envía un correo de urgencia a los usuarios con
    /// perfil Encargado, que son los que generan las órdenes de compra.
    public class NotificadorEmailStock : IObserverStock
    {
        private const string RolDestinatario = "Encargado";

        private readonly DALUsuario dalUsuario = new DALUsuario();

        public void Actualizar(BEInsumo insumo)
        {
            string asunto = "IntelligentStock: stock crítico de " + insumo.Nombre;
            string cuerpo =
                "ALERTA: el insumo " + insumo.Nombre + " alcanzó un nivel crítico.\n\n" +
                "Stock disponible: " + insumo.StockTexto + "\n" +
                "Stock mínimo: " + insumo.StockMinimoTexto + "\n\n" +
                "Gestione una orden de compra urgente.";

            foreach (BEUsuario usuario in dalUsuario.TraerUsuarios())
            {
                if (!usuario.Activo || usuario.Bloqueado ||
                    string.IsNullOrEmpty(usuario.CorreoElectronico) ||
                    !string.Equals(usuario.NombreRol, RolDestinatario, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    // Si el SMTP no está configurado en el Web.config, no envía nada.
                    EnviadorCorreo.Enviar(usuario.CorreoElectronico, asunto, cuerpo);
                }
                catch (Exception)
                {
                    // Un correo que no sale no frena el aviso a los demás.
                }
            }
        }
    }
}
