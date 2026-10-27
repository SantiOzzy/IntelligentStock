using System;
using System.Web.Services;
using BE;
using BLL;
using Servicios;

/// Servicio web de pagos de la suscripción SaaS.
///
/// Permite que un sistema externo (por ejemplo, el portal de facturación)
/// active o cancele la suscripción en nombre de un usuario de IntelligentStock.
/// Antes de procesar la solicitud verifica la IP de origen (R9) y que el
/// usuario exista, esté activo y tenga la patente correspondiente. La lógica
/// es la misma que usan AltaSuscripcion.aspx y BajaSuscripcion.aspx: todo pasa
/// por BLLSuscripcion, que cobra a través de la pasarela de cobros.
[WebService(Namespace = "http://smartsoft.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class WebServicePagos : WebService
{
    [WebMethod(Description = "Activa una suscripción al plan indicado (Básico, Profesional o Empresarial), " +
                             "cobrando con el token de pago. idUsuario es el DNI del usuario que la solicita.")]
    public bool ProcesarAltaSuscripcion(int idUsuario, string tipoPlan, string tokenPago)
    {
        if (!Autorizar(idUsuario, "AltaSuscripcion"))
        {
            return false;
        }

        try
        {
            new BLLSuscripcion().Alta(tipoPlan, tokenPago, null, idUsuario);
            return true;
        }
        catch (Exception ex)
        {
            Rechazar(idUsuario, "alta de suscripción", ex.Message);
            return false;
        }
    }

    [WebMethod(Description = "Cancela la suscripción indicada y detiene los cobros. " +
                             "idUsuario es el DNI del usuario que la solicita.")]
    public bool ProcesarBajaSuscripcion(int idUsuario, int idSuscripcion)
    {
        if (!Autorizar(idUsuario, "BajaSuscripcion"))
        {
            return false;
        }

        try
        {
            new BLLSuscripcion().Baja(idSuscripcion, idUsuario);
            return true;
        }
        catch (Exception ex)
        {
            Rechazar(idUsuario, "baja de suscripción", ex.Message);
            return false;
        }
    }

    // R9 y control del usuario que hace la solicitud.
    private bool Autorizar(int idUsuario, string patente)
    {
        string ip = Context.Request.UserHostAddress;
        BLLEvento bllEvento = new BLLEvento();

        if (!AutenticadorSistemaExterno.EsOrigenAutorizado(ip))
        {
            // Una vez cada 10 minutos por IP, para que no se pueda llenar la bitácora.
            if (LimitadorDeEventos.PermitirRechazo("pagos-ip:" + ip, TimeSpan.FromMinutes(10)))
            {
                bllEvento.Registrar(null, "Acceso denegado a servicio web",
                    "WebServicePagos rechazó una solicitud enviada desde la IP " + ip +
                    ", que no está autorizada (R9). Los rechazos siguientes desde esa IP no se " +
                    "registran durante 10 minutos.", CriticidadEvento.Advertencia);
            }
            return false;
        }

        try
        {
            BLLUsuario bllUsuario = new BLLUsuario();
            BEUsuario usuario = bllUsuario.ObtenerPorDNI(idUsuario);

            if (usuario == null || !usuario.Activo || usuario.Bloqueado)
            {
                bllEvento.Registrar(null, "Acceso denegado a servicio web",
                    "WebServicePagos rechazó una solicitud a nombre del DNI " + idUsuario +
                    ": el usuario no existe, no está activo o está bloqueado.", CriticidadEvento.Advertencia);
                return false;
            }

            bllUsuario.CargarRol(usuario);
            if (!usuario.TienePermiso(patente))
            {
                bllEvento.Registrar(usuario.DNI, "Acceso denegado a servicio web",
                    "WebServicePagos rechazó la solicitud: el usuario no tiene la patente " + patente + ". [#ERR009]",
                    CriticidadEvento.Advertencia);
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void Rechazar(int idUsuario, string operacion, string motivo)
    {
        new BLLEvento().Registrar(idUsuario, "Solicitud rechazada por servicio web",
            "WebServicePagos no pudo procesar la " + operacion + ": " + motivo, CriticidadEvento.Advertencia);
    }
}
