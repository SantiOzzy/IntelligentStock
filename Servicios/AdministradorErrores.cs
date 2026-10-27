using System;
using System.Collections.Generic;

namespace Servicios
{
    /// Categoria del error segun la administracion centralizada de errores.
    public enum CategoriaError
    {
        Aviso,
        Leve,
        Advertencia,
        Grave
    }

    /// Definicion de un error del catalogo centralizado.
    public class DefinicionError
    {
        public DefinicionError(string codigo, string descripcion, CategoriaError categoria)
            : this(codigo, descripcion, categoria, false)
        {
        }

        public DefinicionError(string codigo, string descripcion, CategoriaError categoria,
            bool detalleReemplazaDescripcion)
        {
            Codigo = codigo;
            Descripcion = descripcion;
            Categoria = categoria;
            DetalleReemplazaDescripcion = detalleReemplazaDescripcion;
        }

        public string Codigo { get; private set; }

        public string Descripcion { get; private set; }

        public CategoriaError Categoria { get; private set; }

        /// En algunos errores el texto general no agrega nada (o confunde)
        /// cuando la operación ya explica el problema puntual: por ejemplo,
        /// "La fecha de inicio no puede ser anterior a hoy." no necesita ir
        /// precedido de una frase general. En esos casos, si hay detalle, se
        /// muestra solo el detalle con el código.
        public bool DetalleReemplazaDescripcion { get; private set; }

        public string CssAlerta
        {
            get
            {
                switch (Categoria)
                {
                    case CategoriaError.Aviso: return "alert alert-info";
                    case CategoriaError.Leve: return "alert alert-warning";
                    case CategoriaError.Advertencia: return "alert alert-warning";
                    default: return "alert alert-danger";
                }
            }
        }
    }

    /// Administracion centralizada de errores. Todos los formularios informan
    /// al usuario a traves de este catalogo, de manera que el codigo y el texto
    /// del mensaje esten definidos en un unico lugar.
    public static class AdministradorErrores
    {
        private static readonly Dictionary<string, DefinicionError> catalogo =
            new Dictionary<string, DefinicionError>(StringComparer.OrdinalIgnoreCase);

        static AdministradorErrores()
        {
            Registrar("ERR001", "No es posible establecer la conexión con el motor de base de datos.", CategoriaError.Grave);
            Registrar("ERR002", "Debe completar todos los campos obligatorios del formulario.", CategoriaError.Aviso, true);
            Registrar("ERR003", "Las credenciales de acceso ingresadas son incorrectas.", CategoriaError.Advertencia);
            Registrar("ERR004", "La cuenta de usuario no se encuentra activa.", CategoriaError.Advertencia);
            Registrar("ERR005", "Falló la ejecución del servicio de predicción de ML.NET.", CategoriaError.Grave);
            Registrar("ERR006", "El número de comprobante ya se encuentra registrado para el proveedor.", CategoriaError.Advertencia);
            Registrar("ERR007", "Se detectó una falla de integridad en el cálculo de los dígitos verificadores.", CategoriaError.Grave);
            Registrar("ERR008", "Se detectó un conflicto de integridad al ejecutar la transacción en la base de datos.", CategoriaError.Advertencia);
            Registrar("ERR009", "El perfil del usuario carece de los permisos necesarios para la funcionalidad.", CategoriaError.Advertencia);
            Registrar("ERR010", "El nombre del registro ya existe en la base de datos.", CategoriaError.Advertencia, true);
            Registrar("ERR011", "No se permite alterar ni dar de baja un registro estructural predeterminado.", CategoriaError.Grave);
            Registrar("ERR012", "No se puede ejecutar una operación restrictiva sobre la propia cuenta en uso.", CategoriaError.Advertencia);
            Registrar("ERR013", "No se encontraron registros que coincidan con los filtros de búsqueda aplicados.", CategoriaError.Aviso);
            Registrar("ERR014", "La confirmación de la contraseña no coincide con la nueva clave ingresada.", CategoriaError.Advertencia);
            Registrar("ERR015", "Falló la escritura al intentar generar el archivo físico en el servidor.", CategoriaError.Grave);
            Registrar("ERR016", "El archivo de copia de seguridad seleccionado es inválido o se encuentra corrompido.", CategoriaError.Grave);
            Registrar("ERR017", "Faltan elementos asociados en la grilla de detalle del registro.", CategoriaError.Advertencia, true);
            Registrar("ERR018", "La cantidad a descontar supera el stock físico actual del insumo.", CategoriaError.Advertencia);
            Registrar("ERR019", "El volumen de datos históricos es insuficiente para calcular la predicción de demanda.", CategoriaError.Advertencia);
            Registrar("ERR020", "Los niveles de stock actuales no requieren la generación de nuevos pedidos.", CategoriaError.Aviso);
            Registrar("ERR021", "El estado actual del registro no permite ejecutar la operación solicitada.", CategoriaError.Advertencia);
            Registrar("ERR022", "No hay permisos de escritura en el directorio de destino del archivo generado.", CategoriaError.Leve);
            Registrar("ERR023", "El identificador de red ingresado ya se encuentra asignado a otro dispositivo activo.", CategoriaError.Advertencia);
            Registrar("ERR024", "El servicio externo de cobros rechazó la transacción y no fue posible activar la suscripción.", CategoriaError.Advertencia);

            // Agregado al catálogo del documento: un dato está completo pero no
            // tiene el formato o el valor esperado (una fecha pasada, una MAC mal
            // escrita, un número negativo, etc.). ERR002 queda solo para los
            // campos obligatorios vacíos.
            Registrar("ERR025", "Uno de los datos ingresados no tiene un formato o un valor válido.", CategoriaError.Aviso, true);
        }

        private static void Registrar(string codigo, string descripcion, CategoriaError categoria)
        {
            Registrar(codigo, descripcion, categoria, false);
        }

        private static void Registrar(string codigo, string descripcion, CategoriaError categoria,
            bool detalleReemplazaDescripcion)
        {
            catalogo[codigo] = new DefinicionError(codigo, descripcion, categoria, detalleReemplazaDescripcion);
        }

        public static DefinicionError Obtener(string codigo)
        {
            DefinicionError definicion;
            if (catalogo.TryGetValue(codigo, out definicion))
            {
                return definicion;
            }
            return new DefinicionError(codigo, "Error no catalogado.", CategoriaError.Grave);
        }

        /// Devuelve el mensaje listo para mostrar, con el codigo entre corchetes.
        public static string Mensaje(string codigo)
        {
            DefinicionError definicion = Obtener(codigo);
            return definicion.Descripcion + " [#" + definicion.Codigo + "]";
        }

        /// Devuelve el mensaje con un detalle adicional propio de la operacion.
        public static string Mensaje(string codigo, string detalle)
        {
            DefinicionError definicion = Obtener(codigo);
            if (string.IsNullOrEmpty(detalle))
            {
                return Mensaje(codigo);
            }

            if (definicion.DetalleReemplazaDescripcion)
            {
                return detalle + " [#" + definicion.Codigo + "]";
            }
            return definicion.Descripcion + " " + detalle + " [#" + definicion.Codigo + "]";
        }

        /// Busca el código "[#ERRxxx]" al final de un mensaje ya armado. Sirve
        /// para mostrar con el color de su categoría los errores que llegan
        /// desde la capa de negocio como texto. Devuelve null si no tiene código.
        public static string CodigoDelMensaje(string mensaje)
        {
            if (string.IsNullOrEmpty(mensaje))
            {
                return null;
            }

            int inicio = mensaje.LastIndexOf("[#", StringComparison.Ordinal);
            if (inicio < 0)
            {
                return null;
            }

            int fin = mensaje.IndexOf(']', inicio);
            if (fin < 0)
            {
                return null;
            }

            string codigo = mensaje.Substring(inicio + 2, fin - inicio - 2);
            return catalogo.ContainsKey(codigo) ? codigo : null;
        }

        public static string CssAlerta(string codigo)
        {
            return Obtener(codigo).CssAlerta;
        }

        public static IEnumerable<DefinicionError> Catalogo
        {
            get { return catalogo.Values; }
        }
    }
}
