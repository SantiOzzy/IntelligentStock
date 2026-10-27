<%@ Application Language="C#" %>
<%@ Import Namespace="IntelligentStock" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="System.Web.Routing" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }

    // ASP.NET rechaza los formularios que traen texto parecido a HTML (por
    // ejemplo "<a" en unas observaciones) para evitar ataques de inyección de
    // código. En lugar de la pantalla de error técnica, se muestra un aviso
    // claro y un enlace para volver al formulario (el navegador conserva lo
    // que se había escrito).
    void Application_Error(object sender, EventArgs e)
    {
        Exception error = Server.GetLastError();
        if (error is HttpUnhandledException && error.InnerException != null)
        {
            error = error.InnerException;
        }

        if (!(error is HttpRequestValidationException))
        {
            return;
        }

        Server.ClearError();
        Response.Clear();
        Response.StatusCode = 400;
        // Sin esto IIS reemplaza la respuesta por su propia página de error 400.
        Response.TrySkipIisCustomErrors = true;
        Response.ContentType = "text/html";
        Response.Charset = "utf-8";
        Response.Write(
            "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\" />" +
            "<title>Dato no permitido - IntelligentStock</title>" +
            "<style>body{font-family:Segoe UI,Arial,sans-serif;margin:40px;color:#333}" +
            ".aviso{max-width:640px;padding:15px;border:1px solid #faebcc;background:#fcf8e3;color:#8a6d3b;border-radius:4px}" +
            "a{color:#337ab7}</style></head><body><div class=\"aviso\">" +
            "<strong>Uno de los datos ingresados no está permitido.</strong><br />" +
            "Por seguridad, el sistema no acepta textos que se parezcan a código HTML " +
            "(por ejemplo, el signo &lt; seguido de una letra). Corrija el dato y vuelva a intentarlo." +
            "<br /><br /><a href=\"javascript:history.back()\">Volver al formulario</a>" +
            "</div></body></html>");
        Context.ApplicationInstance.CompleteRequest();
    }

</script>
