using System;
using System.Web;

namespace Servicios
{
    /// Idioma en el que el usuario quiere ver el sitio. Se guarda en una
    /// cookie (no en la sesión) para que sobreviva a un cierre de sesión, y se
    /// puede cambiar en cualquier página agregando "?idioma=en" a la URL (el
    /// selector de Site.master arma ese enlace).
    public static class PreferenciaIdioma
    {
        private const string NombreCookie = "idioma";

        public static readonly string[] Disponibles = { "es", "en", "pt", "fr", "it" };

        public static string Actual
        {
            get
            {
                HttpContext contexto = HttpContext.Current;
                if (contexto == null)
                {
                    return Traductor.IdiomaPorDefecto;
                }

                string deQuery = contexto.Request.QueryString["idioma"];
                if (EsValido(deQuery))
                {
                    return deQuery;
                }

                HttpCookie cookie = contexto.Request.Cookies[NombreCookie];
                if (cookie != null && EsValido(cookie.Value))
                {
                    return cookie.Value;
                }

                return Traductor.IdiomaPorDefecto;
            }
        }

        /// Si la URL trae "?idioma=xx", lo guarda en una cookie para que las
        /// páginas siguientes (que ya no van a tener ese parámetro) sigan
        /// mostrándose en ese idioma.
        public static void GuardarSiVieneEnLaUrl()
        {
            HttpContext contexto = HttpContext.Current;
            if (contexto == null)
            {
                return;
            }

            string deQuery = contexto.Request.QueryString["idioma"];
            if (!EsValido(deQuery))
            {
                return;
            }

            HttpCookie cookie = new HttpCookie(NombreCookie, deQuery);
            cookie.Expires = DateTime.Now.AddYears(1);
            contexto.Response.Cookies.Add(cookie);
        }

        private static bool EsValido(string idioma)
        {
            if (string.IsNullOrEmpty(idioma))
            {
                return false;
            }

            foreach (string disponible in Disponibles)
            {
                if (string.Equals(disponible, idioma, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
