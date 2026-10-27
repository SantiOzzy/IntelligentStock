using System;
using System.Web.Services;
using BE;
using BLL;
using Servicios;

/// Servicio web de telemetría IoT.
///
/// Es la puerta de entrada de las lecturas que mandan los sensores de
/// temperatura (en este trabajo, el sistema externo de telemetría es
/// simulado). Cada lectura se autentica por la IP de origen (R9) y, si supera
/// la temperatura máxima permitida del sensor, genera una alerta térmica que
/// después el técnico procesa en ProcesarAlertaTermica.aspx (CU-007-043).
///
/// Para probarlo desde el mismo equipo: abrir WebServiceTelemetria.asmx en el
/// navegador, elegir RegistrarLectura, completar la MAC de un sensor activo y
/// una temperatura, y presionar "Invocar".
///
/// La temperatura se recibe como texto y se lee aceptando punto o coma
/// decimal: si fuera double, el formulario de prueba de ASP.NET tomaría la coma
/// como separador de miles y "8,5" llegaría como 85.
[WebService(Namespace = "http://smartsoft.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class WebServiceTelemetria : WebService
{
    [WebMethod(Description = "Registra una lectura de temperatura (°C) del sensor con la dirección MAC indicada. " +
                             "La temperatura acepta punto o coma decimal, por ejemplo 8.5 o -15,2.")]
    public ResultadoLectura RegistrarLectura(string macAddress, string temperatura)
    {
        string ip = Context.Request.UserHostAddress;

        if (!AutenticadorSistemaExterno.EsOrigenAutorizado(ip))
        {
            // Una vez cada 10 minutos por IP, para que no se pueda llenar la bitácora.
            if (LimitadorDeEventos.PermitirRechazo("telemetria-ip:" + ip, TimeSpan.FromMinutes(10)))
            {
                new BLLEvento().Registrar(null, "Acceso denegado a servicio web",
                    "WebServiceTelemetria rechazó una lectura enviada desde la IP " + ip +
                    ", que no está autorizada (R9). Los rechazos siguientes desde esa IP no se " +
                    "registran durante 10 minutos.", CriticidadEvento.Advertencia);
            }

            return new ResultadoLectura(TipoResultadoLectura.AccesoDenegado, 0,
                "El origen de la petición no está autorizado.");
        }

        double grados;
        if (!Numeros.TryParseDouble(temperatura, out grados))
        {
            return new ResultadoLectura(TipoResultadoLectura.DatosInvalidos, 0,
                "La temperatura recibida no es un número.");
        }

        try
        {
            return new BLLAlerta().RegistrarLectura(macAddress, grados);
        }
        catch (Exception ex)
        {
            return new ResultadoLectura(TipoResultadoLectura.Error, 0, ex.Message);
        }
    }
}
