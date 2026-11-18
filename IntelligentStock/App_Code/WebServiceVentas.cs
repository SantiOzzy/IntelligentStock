using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Services;
using System.Web.Services.Protocols;
using BE;
using BLL;
using Servicios;

/// Servicio web de ventas.
///
/// RegistrarVenta es la puerta de entrada del punto de venta del local: cada
/// vez que vende (o al cerrar el día, con los totales) informa la receta, la
/// cantidad y la fecha, y IntelligentStock descuenta del stock lo que lleva la
/// receta, igual que Descontar stock por venta (CU-006-037). Para probarlo
/// desde el mismo equipo: abrir WebServiceVentas.asmx, elegir RegistrarVenta,
/// completar una receta activa, una cantidad y una fecha, y presionar "Invocar".
///
/// ObtenerEstadisticasVentas devuelve, para un rango de fechas, las unidades
/// vendidas y la cantidad de ventas, el promedio diario, la receta más
/// vendida, el día de más ventas y el detalle por receta. Sirve, por ejemplo,
/// para que un tablero o el sistema contable consulte cómo vende el local sin
/// entrar al sistema.
///
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

    [WebMethod(Description = "Registra una venta informada por el punto de venta y descuenta del stock lo que lleva la " +
                             "receta. La receta se indica por su nombre o su número; la cantidad acepta punto o coma " +
                             "decimal (por ejemplo 3 o 0,5) y la fecha va como dd/mm/aaaa o aaaa-mm-dd (vacía: hoy).")]
    public ResultadoVentaInformada RegistrarVenta(string receta, string cantidad, string fecha)
    {
        ValidarOrigen("WebServiceVentas");

        decimal unidades;
        if (!Numeros.TryParseDecimal(cantidad, out unidades))
        {
            return ResultadoVentaInformada.Rechazada("La cantidad recibida no es un número.");
        }

        DateTime dia = DateTime.Today;
        if (!string.IsNullOrEmpty(fecha) && fecha.Trim().Length > 0 &&
            !DateTime.TryParseExact(fecha.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" },
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dia))
        {
            return ResultadoVentaInformada.Rechazada("La fecha recibida no es válida (dd/mm/aaaa o aaaa-mm-dd).");
        }

        try
        {
            List<BEConsumoVenta> consumos = new BLLVenta().RegistrarDesdePuntoDeVenta(receta, unidades, dia);
            List<string> sinStock = new List<string>();
            foreach (BEConsumoVenta consumo in consumos)
            {
                if (consumo.SinStock)
                {
                    sinStock.Add(consumo.NombreInsumo);
                }
            }

            ResultadoVentaInformada resultado = new ResultadoVentaInformada();
            resultado.Registrada = true;
            resultado.InsumosSinStock = sinStock.ToArray();
            resultado.Mensaje = "Venta registrada: se descontó el stock de " + consumos.Count +
                                (consumos.Count == 1 ? " insumo." : " insumos.") +
                                (sinStock.Count > 0
                                    ? " No alcanzaba el stock de " + string.Join(", ", sinStock.ToArray()) + ": quedó en cero."
                                    : string.Empty);
            return resultado;
        }
        catch (ApplicationException ex)
        {
            // Datos que no se pueden registrar (receta inexistente, cantidad o fecha fuera de rango).
            return ResultadoVentaInformada.Rechazada(ex.Message);
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

/// Respuesta de WebServiceVentas.RegistrarVenta al punto de venta.
public class ResultadoVentaInformada
{
    /// Si la venta quedó registrada y se descontó el stock.
    public bool Registrada { get; set; }

    public string Mensaje { get; set; }

    /// Insumos cuyo stock no alcanzaba: la venta se registró igual y quedaron en cero.
    public string[] InsumosSinStock { get; set; }

    public static ResultadoVentaInformada Rechazada(string mensaje)
    {
        ResultadoVentaInformada resultado = new ResultadoVentaInformada();
        resultado.Registrada = false;
        resultado.Mensaje = mensaje;
        resultado.InsumosSinStock = new string[0];
        return resultado;
    }
}
