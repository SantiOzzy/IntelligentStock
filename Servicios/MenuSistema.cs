using System;
using System.Collections.Generic;
using BE;

namespace Servicios
{
    /// Opcion del menu principal. Se habilita segun la patente asociada.
    public class OpcionMenu
    {
        public OpcionMenu(string texto, string url, string patente)
        {
            Texto = texto;
            Url = url;
            Patente = patente;
        }

        public string Texto { get; private set; }

        public string Url { get; private set; }

        /// Patente que habilita la opcion.
        public string Patente { get; private set; }
    }

    /// Agrupacion de opciones del menu principal.
    public class ModuloMenu
    {
        public ModuloMenu(string titulo, params OpcionMenu[] opciones)
        {
            Titulo = titulo;
            Opciones = new List<OpcionMenu>(opciones);
        }

        public string Titulo { get; private set; }

        public List<OpcionMenu> Opciones { get; private set; }
    }

    /// Mapa de navegacion del sistema. Es la unica definicion del menu:
    /// tanto la barra superior como el panel principal se arman a partir de aca,
    /// filtrando las opciones con el Composite de permisos del rol.
    public static class MenuSistema
    {
        private static readonly List<ModuloMenu> modulos = new List<ModuloMenu>
        {
            new ModuloMenu("Seguridad",
                new OpcionMenu("Alta de usuario", "~/AltaUsuario.aspx", "AltaUsuario"),
                new OpcionMenu("Modificación de usuario", "~/ModificacionUsuario.aspx", "ModificacionUsuario"),
                new OpcionMenu("Baja de usuario", "~/BajaUsuario.aspx", "BajaUsuario"),
                new OpcionMenu("Consultar usuario", "~/ConsultaUsuario.aspx", "ConsultaUsuario"),
                new OpcionMenu("Bloquear usuario", "~/BloquearUsuario.aspx", "BloquearUsuario"),
                new OpcionMenu("Desbloquear usuario", "~/DesbloquearUsuario.aspx", "DesbloquearUsuario"),
                new OpcionMenu("Cambiar contraseña", "~/CambiarContraseña.aspx", "CambiarContraseña")),

            new ModuloMenu("Permisos",
                new OpcionMenu("Alta de familia", "~/AltaFamilia.aspx", "AltaFamilia"),
                new OpcionMenu("Modificación de familia", "~/ModificacionFamilia.aspx", "ModificacionFamilia"),
                new OpcionMenu("Baja de familia", "~/BajaFamilia.aspx", "BajaFamilia"),
                new OpcionMenu("Consultar familia", "~/ConsultaFamilia.aspx", "ConsultaFamilia"),
                new OpcionMenu("Consultar patente", "~/ConsultaPatente.aspx", "ConsultaPatente")),

            // Reparar dígito verificador no figura en el menú a propósito: a esa
            // pantalla solo se llega cuando el login detecta una inconsistencia.
            new ModuloMenu("Base de datos",
                new OpcionMenu("Bitácora de eventos", "~/BitacoraEventos.aspx", "BitacoraEventos"),
                new OpcionMenu("BackUp", "~/BackUp.aspx", "BackUp"),
                new OpcionMenu("Restore", "~/Restore.aspx", "Restore")),

            new ModuloMenu("Proveedores",
                new OpcionMenu("Alta de proveedor", "~/AltaProveedor.aspx", "AltaProveedor"),
                new OpcionMenu("Modificación de proveedor", "~/ModificacionProveedor.aspx", "ModificacionProveedor"),
                new OpcionMenu("Baja de proveedor", "~/BajaProveedor.aspx", "BajaProveedor"),
                new OpcionMenu("Consultar proveedor", "~/ConsultaProveedor.aspx", "ConsultaProveedor")),

            new ModuloMenu("Insumos y recetas",
                new OpcionMenu("Alta de insumo", "~/AltaInsumo.aspx", "AltaInsumo"),
                new OpcionMenu("Modificación de insumo", "~/ModificacionInsumo.aspx", "ModificacionInsumo"),
                new OpcionMenu("Baja de insumo", "~/BajaInsumo.aspx", "BajaInsumo"),
                new OpcionMenu("Consultar insumo", "~/ConsultaInsumo.aspx", "ConsultaInsumo"),
                new OpcionMenu("Alta de receta", "~/AltaReceta.aspx", "AltaReceta"),
                new OpcionMenu("Modificación de receta", "~/ModificacionReceta.aspx", "ModificacionReceta"),
                new OpcionMenu("Baja de receta", "~/BajaReceta.aspx", "BajaReceta"),
                new OpcionMenu("Consultar receta", "~/ConsultaReceta.aspx", "ConsultaReceta"),
                new OpcionMenu("Registrar ajuste de stock", "~/AjusteStock.aspx", "AjusteStock")),

            new ModuloMenu("Operaciones",
                new OpcionMenu("Generar orden de compra", "~/GenerarOrdenCompra.aspx", "GenerarOrdenCompra"),
                new OpcionMenu("Consultar orden de compra", "~/ConsultarOrdenCompra.aspx", "ConsultarOrdenCompra"),
                new OpcionMenu("Anular orden de compra", "~/AnularOrdenCompra.aspx", "AnularOrdenCompra"),
                new OpcionMenu("Registrar factura de proveedor", "~/RegistrarFacturaProveedor.aspx", "RegistrarFacturaProveedor"),
                new OpcionMenu("Descontar stock por venta", "~/DescontarStockVenta.aspx", "DescontarStockVenta"),
                new OpcionMenu("Exportar reporte gerencial", "~/ExportarReporteGerencial.aspx", "ExportarReporteGerencial")),

            new ModuloMenu("Telemetría IoT",
                new OpcionMenu("Alta de sensor", "~/AltaSensor.aspx", "AltaSensor"),
                new OpcionMenu("Modificación de sensor", "~/ModificacionSensor.aspx", "ModificacionSensor"),
                new OpcionMenu("Baja de sensor", "~/BajaSensor.aspx", "BajaSensor"),
                new OpcionMenu("Consultar sensor", "~/ConsultaSensor.aspx", "ConsultaSensor"),
                new OpcionMenu("Procesar alerta térmica", "~/ProcesarAlertaTermica.aspx", "ProcesarAlertaTermica")),

            new ModuloMenu("Suscripción",
                new OpcionMenu("Alta de suscripción", "~/AltaSuscripcion.aspx", "AltaSuscripcion"),
                new OpcionMenu("Baja de suscripción", "~/BajaSuscripcion.aspx", "BajaSuscripcion"))
        };

        public static IEnumerable<ModuloMenu> Modulos
        {
            get { return modulos; }
        }

        /// Devuelve unicamente los modulos y opciones para los que el usuario
        /// tiene permiso. Los modulos que quedan vacios no se devuelven.
        public static List<ModuloMenu> ObtenerMenuDe(BEUsuario usuario)
        {
            List<ModuloMenu> permitidos = new List<ModuloMenu>();

            if (usuario == null)
            {
                return permitidos;
            }

            foreach (ModuloMenu modulo in modulos)
            {
                List<OpcionMenu> opciones = new List<OpcionMenu>();

                foreach (OpcionMenu opcion in modulo.Opciones)
                {
                    if (usuario.TienePermiso(opcion.Patente))
                    {
                        opciones.Add(opcion);
                    }
                }

                if (opciones.Count > 0)
                {
                    permitidos.Add(new ModuloMenu(modulo.Titulo, opciones.ToArray()));
                }
            }

            return permitidos;
        }

        /// Patentes que no figuran en el menú porque no se llega a ellas navegando.
        private static readonly Dictionary<string, string> FueraDelMenu =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "ReparacionDV", "Reparar dígito verificador" }
            };

        /// Busca el texto descriptivo de una patente dentro del mapa de navegación.
        public static string DescribirPatente(string patente)
        {
            string fuera;
            if (FueraDelMenu.TryGetValue(patente ?? string.Empty, out fuera))
            {
                return fuera;
            }

            foreach (ModuloMenu modulo in modulos)
            {
                foreach (OpcionMenu opcion in modulo.Opciones)
                {
                    if (string.Equals(opcion.Patente, patente, StringComparison.OrdinalIgnoreCase))
                    {
                        return opcion.Texto;
                    }
                }
            }
            return patente;
        }
    }
}
