using System;
using System.Web.Services;
using System.Web.Services.Protocols;
using BE;
using BLL;
using Servicios;

/// Servicio web de estadísticas de ventas.
///
/// Devuelve, para un rango de fechas, las unidades vendidas y la cantidad de
/// ventas, el promedio diario, la receta más vendida, el día de más ventas y
/// el detalle por receta (unidades, ventas, promedio diario y porcentaje del
/// total). Los datos salen del historial de ventas que registra Descontar
/// stock por venta (CU-006-037), el mismo que usan la predicción de demanda y
/// el reporte gerencial. Sirve, por ejemplo, para que un tablero o el sistema
/// contable de la empresa consulte cómo vende el local sin entrar al sistema.
/// Como los demás servicios web, solo atiende a orígenes autorizados (R9).
[WebService(Namespace = "http://smartsoft.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class WebServiceVentas : WebService
{
    [WebMethod(Description = "Devuelve las estadísticas de ventas entre las fechas indicadas (inclusive, hasta un año): " +
                             "unidades vendidas, cantidad de ventas, promedio diario, receta más vendida, día de más " +
                             "ventas y el detalle por receta.")]
    public BEEstadisticaVentas ObtenerEstadisticasVentas(DateTime fechaInicio, DateTime fechaFin)
    {
        ValidarOrigen("WebServiceVentas");

        try
        {
            return new BLLVenta().Estadisticas(fechaInicio, fechaFin);
        }
        catch (ApplicationException ex)
        {
            // Fechas inválidas: es un error de quien llama.
            throw new SoapException(ex.Message, SoapException.ClientFaultCode);
        }
        catch (Exception ex)
        {
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
        if (LimitadorDeEventos.PermitirRechazo("ventas-ip:" + ip, TimeSpan.FromMinutes(10)))
        {
            new BLLEvento().Registrar(null, "Acceso denegado a servicio web",
                servicio + " rechazó un pedido enviado desde la IP " + ip + ", que no está autorizada (R9). " +
                "Los rechazos siguientes desde esa IP no se registran durante 10 minutos.", CriticidadEvento.Advertencia);
        }

        throw new SoapException("El origen de la petición no está autorizado.", SoapException.ClientFaultCode);
    }
}
