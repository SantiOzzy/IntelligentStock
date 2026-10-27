using System;

namespace BE
{
    /// Proveedor de insumos. Se corresponde con la tabla Proveedor.
    public class BEProveedor
    {
        public BEProveedor()
        {
        }

        public BEProveedor(string cuit, string nombre, string razonSocial, string correo, string numero)
        {
            Cuit = cuit;
            Nombre = nombre;
            RazonSocial = razonSocial;
            Correo = correo;
            Numero = numero;
            Activo = true;
        }

        /// Clave del registro, con el formato XX-XXXXXXXX-X.
        public string Cuit { get; set; }

        /// Nombre comercial.
        public string Nombre { get; set; }

        public string RazonSocial { get; set; }

        public string Correo { get; set; }

        /// Teléfono de contacto (columna numero de la tabla).
        public string Numero { get; set; }

        public bool Activo { get; set; }

        public string Estado
        {
            get { return Activo ? "Activo" : "Inactivo"; }
        }

        /// Texto para mostrar el proveedor en una lista desplegable.
        public string Descripcion
        {
            get { return Nombre + " (CUIT " + Cuit + ")"; }
        }
    }
}
