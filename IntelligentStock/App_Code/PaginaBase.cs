using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using BE;
using BLL;
using Servicios;

/// R11 - Verificar permisos de acceso.
/// Clase base de todos los formularios del sistema. Pertenece a la capa de
/// presentacion. Antes de que la pagina se cargue controla que exista una
/// sesion activa y que el rol del usuario tenga la patente que la habilita.
public abstract class PaginaBase : Page
{
    /// Patente que habilita el formulario. Vacio si solo requiere sesion.
    protected virtual string PatenteRequerida
    {
        get { return string.Empty; }
    }

    /// Los formularios publicos (inicio de sesión) devuelven false.
    protected virtual bool RequiereSesion
    {
        get { return true; }
    }

    /// Formularios a los que se puede llegar con la base inconsistente.
    protected virtual bool PermitidoConBaseInconsistente
    {
        get { return false; }
    }

    protected BEUsuario UsuarioActual
    {
        get { return SessionManager.UsuarioActual; }
    }

    protected override void OnPreInit(EventArgs e)
    {
        base.OnPreInit(e);
        PreferenciaIdioma.GuardarSiVieneEnLaUrl();
        Autorizar();
    }

    /// Multi-idioma: se resuelve acá, en el servidor, no con una funcionalidad
    /// del navegador. Se traduce el HTML ya armado de la página, justo antes de
    /// mandarlo al navegador, así entran también la barra de navegación, los
    /// títulos, los textos sueltos del .aspx y los mensajes que arma cada
    /// pantalla en tiempo de ejecución. Antes se recorrían los controles uno
    /// por uno y quedaba sin traducir todo lo que no fuera un control de
    /// servidor (por ejemplo, el encabezado).
    ///
    /// Traductor tiene caché en disco, así que cada texto se le pide a la API
    /// una sola vez por idioma.
    protected override void Render(HtmlTextWriter writer)
    {
        string idioma = PreferenciaIdioma.Actual;

        if (idioma == Traductor.IdiomaPorDefecto)
        {
            base.Render(writer);
            return;
        }

        StringWriter texto = new StringWriter();
        base.Render(new HtmlTextWriter(texto));

        writer.Write(Traductor.TraducirHtml(texto.ToString(), idioma));
    }

    private void Autorizar()
    {
        // La base quedo marcada como inconsistente durante el inicio de sesión.
        if (SessionManager.BaseDeDatosInconsistente && !PermitidoConBaseInconsistente)
        {
            Redirigir("~/ReparacionDV.aspx");
            return;
        }

        BEUsuario usuario = SessionManager.UsuarioActual;

        // Si mientras tenía la sesión abierta le dieron de baja la cuenta, un
        // administrador se la bloqueó o le cambiaron el rol, se aplica ya, en
        // este pedido, y no recién cuando vuelva a iniciar sesión. Se controla
        // también en las páginas públicas (el panel principal de IniciarSesion).
        if (usuario != null)
        {
            string motivoDeCierre = ActualizarUsuarioDeLaSesion(usuario);
            if (motivoDeCierre != null)
            {
                new BLLEvento().Registrar(usuario.DNI, "Cierre de sesión forzado",
                    "Se cerró la sesión del usuario " + usuario.NombreUsuario + " porque " +
                    (motivoDeCierre == MotivoCierreBloqueo
                        ? "un administrador bloqueó la cuenta"
                        : "la cuenta fue dada de baja") + " mientras estaba conectado.",
                    CriticidadEvento.Advertencia);
                SessionManager.CerrarSesion();
                Redirigir("~/IniciarSesion.aspx?motivo=" + motivoDeCierre);
                return;
            }
            usuario = SessionManager.UsuarioActual;
        }

        if (!RequiereSesion)
        {
            return;
        }

        if (usuario == null)
        {
            Redirigir("~/IniciarSesion.aspx?motivo=sesion");
            return;
        }

        string patente = PatenteRequerida;
        if (string.IsNullOrEmpty(patente))
        {
            return;
        }

        if (!usuario.TienePermiso(patente))
        {
            // DNI 0 es el usuario de contingencia: puede estar operando
            // justamente porque la base de datos no está disponible, así que
            // no se intenta escribir la bitácora (fallaría igual).
            if (usuario.DNI != 0)
            {
                new BLLEvento().Registrar(usuario.DNI, "Acceso denegado",
                    "El usuario intentó acceder a " + patente +
                    " sin contar con la patente correspondiente. [#ERR009]",
                    CriticidadEvento.Advertencia);
            }

            Redirigir("~/IniciarSesion.aspx?motivo=permiso&f=" +
                HttpUtility.UrlEncode(patente));
        }
    }

    private const string MotivoCierreBaja = "cuenta";
    private const string MotivoCierreBloqueo = "bloqueo";

    /// Vuelve a leer la cuenta de la base. Devuelve el motivo para cortar la
    /// sesión ("cuenta" si ya no existe o está dada de baja, "bloqueo" si la
    /// bloqueó un administrador) o null si puede seguir. Si cambió el rol,
    /// recarga sus permisos en la sesión. Si la base no responde no se corta la
    /// sesión por eso: el error lo va a informar la operación que se intente.
    ///
    /// El bloqueo por intentos fallidos no corta la sesión abierta: cualquiera
    /// que conozca el nombre de usuario del administrador podría escribir mal
    /// la clave tres veces y así echarlo del sistema (y él ya no podría
    /// desbloquearse). Ese bloqueo impide el próximo inicio de sesión. El
    /// bloqueo manual sí la corta, porque es una decisión de un administrador.
    private static string ActualizarUsuarioDeLaSesion(BEUsuario usuario)
    {
        // DNI 0: usuario de contingencia, que no está en la base.
        if (usuario.DNI == 0)
        {
            return null;
        }

        BLLUsuario bllUsuario = new BLLUsuario();
        BEUsuario actual;
        try
        {
            actual = bllUsuario.ObtenerPorDNI(usuario.DNI);
        }
        catch (Exception)
        {
            return null;
        }

        if (actual == null || !actual.Activo)
        {
            return MotivoCierreBaja;
        }

        if (actual.BloqueadoManualmente)
        {
            return MotivoCierreBloqueo;
        }

        if (actual.IdRol != usuario.IdRol)
        {
            try
            {
                bllUsuario.CargarRol(actual);
                SessionManager.UsuarioActual = actual;
            }
            catch (Exception)
            {
                // Si no se pudo recargar el rol se sigue con el anterior.
            }
        }

        return null;
    }

    /// Corta la ejecución de la página y redirige. Con Redirect(url, false) la
    /// página seguía su ciclo de vida completo (Page_Load, los eventos del
    /// postback y el Render): un usuario sin permiso podía disparar igual el
    /// botón Aceptar armando el POST a mano, y el HTML de la página viajaba en
    /// el cuerpo de la respuesta 302. Con endResponse = true ASP.NET termina el
    /// pedido acá (la ThreadAbortException que lanza es la forma normal de
    /// cortar y la maneja el propio ASP.NET).
    private void Redirigir(string url)
    {
        Response.Redirect(url, true);
    }

    /// Muestra un mensaje del catalogo centralizado de errores.
    protected void MostrarError(Label etiqueta, string codigo)
    {
        if (etiqueta == null)
        {
            return;
        }
        etiqueta.CssClass = AdministradorErrores.CssAlerta(codigo);
        etiqueta.Text = AdministradorErrores.Mensaje(codigo);
        etiqueta.Visible = true;
    }

    /// Muestra un mensaje ya armado por la capa de negocio. Si es un error del
    /// catálogo (termina en "[#ERRxxx]") se pinta con el color de su categoría:
    /// celeste los avisos, amarillo las advertencias y rojo los graves.
    protected void MostrarMensaje(Label etiqueta, string texto, bool esError)
    {
        if (etiqueta == null)
        {
            return;
        }

        string codigo = esError ? AdministradorErrores.CodigoDelMensaje(texto) : null;
        if (codigo == "ERR007" && MarcarBaseInconsistente())
        {
            texto += " Al pasar a otra pantalla el sistema lo va a llevar a Reparar dígito verificador.";
        }

        if (codigo != null)
        {
            etiqueta.CssClass = AdministradorErrores.CssAlerta(codigo);
        }
        else
        {
            etiqueta.CssClass = esError ? "alert alert-danger" : "alert alert-success";
        }
        etiqueta.Text = texto;
        etiqueta.Visible = true;
    }

    /// Una operación se detuvo porque encontró una tabla alterada (R4). Si
    /// el que estaba operando es un administrador, se marca la sesión igual
    /// que cuando la inconsistencia aparece al iniciar sesión, para que la
    /// próxima pantalla sea Reparar dígito verificador con el detalle
    /// completo. Devuelve true si la marcó.
    private static bool MarcarBaseInconsistente()
    {
        BEUsuario usuario = SessionManager.UsuarioActual;
        if (usuario == null || SessionManager.BaseDeDatosInconsistente)
        {
            return false;
        }

        try
        {
            if (!new BLLUsuario().EsAdministrador(usuario))
            {
                return false;
            }

            List<Inconsistencia> inconsistencias = new BLLDigitoVerificador().VerificarIntegridad();
            if (inconsistencias.Count == 0)
            {
                return false;
            }

            SessionManager.Inconsistencias = inconsistencias;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    protected void OcultarMensaje(Label etiqueta)
    {
        if (etiqueta != null)
        {
            etiqueta.Visible = false;
            etiqueta.Text = string.Empty;
        }
    }
}
