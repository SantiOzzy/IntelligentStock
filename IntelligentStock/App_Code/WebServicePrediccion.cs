using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Web.Services.Protocols;
using BE;
using BLL;
using Servicios;

/// Servicio web de predicción de demanda (CU-006-033).
///
/// Es el puente entre la aplicación y el motor predictivo de ML.NET: recibe la
/// cantidad de días a proyectar y devuelve, para cada insumo que haga falta
/// pedir, la cantidad sugerida. Usa la misma lógica que el formulario
/// GenerarOrdenCompra.aspx (BLLOrdenCompra.CalcularSugerencias). Como los demás
/// servicios web, solo atiende a los orígenes autorizados (R9).
[WebService(Namespace = "http://smartsoft.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class WebServicePrediccion : WebService
{
    [WebMethod(Description = "Devuelve las cantidades sugeridas a comprar de cada insumo según la demanda prevista " +
                             "por el modelo de ML.NET para los próximos días indicados (entre 1 y 90).")]
    public List<BESugerenciaCompra> ObtenerSugerenciasCompra(int diasProyeccion)
    {
        ValidarOrigen("WebServicePrediccion");

        try
        {
            ResultadoPrediccion prediccion;
            return new BLLOrdenCompra().CalcularSugerencias(diasProyeccion, out prediccion);
        }
        catch (ApplicationException ex)
        {
            // Si no hace falta pedir nada, la respuesta es una lista vacía.
            if (AdministradorErrores.CodigoDelMensaje(ex.Message) == "ERR020")
            {
                return new List<BESugerenciaCompra>();
            }
            throw new SoapException(ex.Message, SoapException.ServerFaultCode);
        }
    }

    /// R9: el pedido tiene que venir de un origen autorizado.
    private void ValidarOrigen(string servicio)
    {
        string ip = Context.Request.UserHostAddress;
        if (AutenticadorSistemaExterno.EsOrigenAutorizado(ip))
        {
            return;
        }

        // Una vez cada 10 minutos por IP, para que no se pueda llenar la bitácora.
        if (LimitadorDeEventos.PermitirRechazo("prediccion-ip:" + ip, TimeSpan.FromMinutes(10)))
        {
            new BLLEvento().Registrar(null, "Acceso denegado a servicio web",
                servicio + " rechazó un pedido enviado desde la IP " + ip + ", que no está autorizada (R9). " +
                "Los rechazos siguientes desde esa IP no se registran durante 10 minutos.", CriticidadEvento.Advertencia);
        }

        throw new SoapException("El origen de la petición no está autorizado.", SoapException.ClientFaultCode);
    }
}
