using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Web.Services.Protocols;
using BE;
using BLL;
using Servicios;

/// Servicio web de gasto por proveedor (CU-006-038).
///
/// Calcula, para un rango de fechas, cuántas órdenes tiene cada proveedor
/// (pendientes, recibidas y anuladas) y el importe de lo recibido, ordenado de
/// mayor a menor. Es la misma información del reporte gerencial "Órdenes por
/// proveedor". Lo recibido se valoriza al precio unitario actual de cada
/// insumo. Como los demás servicios web, solo atiende a orígenes autorizados (R9).
[WebService(Namespace = "http://smartsoft.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class WebServiceGastoProveedores : WebService
{
    [WebMethod(Description = "Devuelve el gasto agrupado por proveedor entre las fechas indicadas (inclusive).")]
    public List<BEGastoProveedor> ObtenerGastoPorProveedor(DateTime fechaInicio, DateTime fechaFin)
    {
        ValidarOrigen("WebServiceGastoProveedores");

        if (fechaInicio.Date > fechaFin.Date)
        {
            throw new SoapException(AdministradorErrores.Mensaje("ERR025",
                "La fecha de inicio no puede ser posterior a la fecha de fin."), SoapException.ClientFaultCode);
        }

        try
        {
            return new BLLOrdenCompra().GastoPorProveedor(fechaInicio, fechaFin);
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
        if (LimitadorDeEventos.PermitirRechazo("gasto-ip:" + ip, TimeSpan.FromMinutes(10)))
        {
            new BLLEvento().Registrar(null, "Acceso denegado a servicio web",
                servicio + " rechazó un pedido enviado desde la IP " + ip + ", que no está autorizada (R9). " +
                "Los rechazos siguientes desde esa IP no se registran durante 10 minutos.", CriticidadEvento.Advertencia);
        }

        throw new SoapException("El origen de la petición no está autorizado.", SoapException.ClientFaultCode);
    }
}
