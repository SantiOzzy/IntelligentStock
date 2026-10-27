using System;
using System.Collections.Generic;
using System.Text;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de proveedores (CU-004-020 a CU-004-023).
    ///
    /// Igual que en el resto del sistema, cada operación que escribe en la base
    /// valida los datos, graba, recalcula el dígito verificador de la tabla y
    /// deja el evento en la bitácora.
    public class BLLProveedor
    {
        private const string Tabla = "Proveedor";

        /// Largos máximos de las columnas de la tabla Proveedor.
        public const int LargoNombre = 100;
        public const int LargoRazonSocial = 100;
        public const int LargoCorreo = 100;
        public const int LargoNumero = 20;

        private readonly DALProveedor dalProveedor = new DALProveedor();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        #region Consultas

        public List<BEProveedor> TraerProveedores()
        {
            return dalProveedor.TraerProveedores();
        }

        /// Proveedores habilitados para operar (los dados de baja no se ofrecen).
        public List<BEProveedor> TraerActivos()
        {
            return TraerProveedores().FindAll(delegate(BEProveedor proveedor) { return proveedor.Activo; });
        }

        public BEProveedor ObtenerPorCuit(string cuit)
        {
            if (string.IsNullOrEmpty(cuit))
            {
                return null;
            }
            return dalProveedor.ObtenerPorCuit(NormalizarCuit(cuit));
        }

        /// CU-004-023 Consultar proveedor. Filtra por CUIT, nombre o razón social
        /// y por estado. El CUIT se encuentra con o sin guiones.
        public List<BEProveedor> Filtrar(string texto, string estado)
        {
            string busqueda = texto == null ? string.Empty : texto.Trim();
            bool buscaPorNumero = EsNumeroDeCuit(busqueda);
            string digitosBuscados = SoloDigitos(busqueda);

            List<BEProveedor> resultado = new List<BEProveedor>();

            foreach (BEProveedor proveedor in TraerProveedores())
            {
                if (!string.IsNullOrEmpty(estado) && proveedor.Estado != estado)
                {
                    continue;
                }

                if (busqueda.Length > 0)
                {
                    bool coincide = Contiene(proveedor.Nombre, busqueda) ||
                                    Contiene(proveedor.RazonSocial, busqueda) ||
                                    Contiene(proveedor.Cuit, busqueda) ||
                                    (buscaPorNumero &&
                                     SoloDigitos(proveedor.Cuit).IndexOf(digitosBuscados, StringComparison.Ordinal) >= 0);
                    if (!coincide)
                    {
                        continue;
                    }
                }

                resultado.Add(proveedor);
            }

            return resultado;
        }

        #endregion

        #region ABM

        /// CU-004-020 Alta de proveedor.
        public void Alta(BEProveedor proveedor)
        {
            Normalizar(proveedor);
            ValidarDatos(proveedor);

            BEProveedor existente = dalProveedor.ObtenerPorCuit(proveedor.Cuit);
            if (existente != null)
            {
                string detalle = "Ya existe un proveedor con el CUIT " + proveedor.Cuit +
                                 " (" + existente.Nombre + ")";
                detalle += existente.Activo ? "." : ", dado de baja. Puede reactivarlo desde Baja de proveedor.";
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR010", detalle));
            }

            VerificarCorreoUnico(proveedor.Correo, proveedor.Cuit);

            bllDV.EjecutarControlado(() => dalProveedor.Crear(proveedor), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Alta de proveedor",
                "Se dio de alta al proveedor " + proveedor.Nombre + " (CUIT " + proveedor.Cuit + ").");
        }

        /// CU-004-021 Modificar proveedor. El CUIT es la clave y no cambia.
        public void Modificar(BEProveedor proveedor)
        {
            Normalizar(proveedor);
            ValidarDatos(proveedor);

            BEProveedor anterior = dalProveedor.ObtenerPorCuit(proveedor.Cuit);
            if (anterior == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!anterior.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El proveedor se encuentra dado de baja."));
            }

            VerificarCorreoUnico(proveedor.Correo, proveedor.Cuit);

            bllDV.EjecutarControlado(() => dalProveedor.Modificar(proveedor), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Modificación de proveedor",
                "Se modificaron los datos del proveedor " + proveedor.Nombre + " (CUIT " + proveedor.Cuit + ").");
        }

        /// CU-004-022 Baja de proveedor. Es lógica: se conserva el registro para
        /// no perder el historial de órdenes de compra.
        public void Baja(string cuit)
        {
            BEProveedor proveedor = ObtenerPorCuit(cuit);
            if (proveedor == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!proveedor.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El proveedor ya se encuentra dado de baja."));
            }

            // La baja no puede dejar órdenes de compra pendientes a nombre de un
            // proveedor que ya no opera.
            int pendientes = dalProveedor.ContarOrdenesPendientes(proveedor.Cuit);
            if (pendientes > 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR008",
                    "El proveedor " + proveedor.Nombre + " tiene " + pendientes +
                    " orden(es) de compra pendiente(s): anúlelas o regístrelas como recibidas antes de darlo de baja."));
            }

            bllDV.EjecutarControlado(() => dalProveedor.Baja(proveedor.Cuit), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Baja de proveedor",
                "Se dio de baja al proveedor " + proveedor.Nombre + " (CUIT " + proveedor.Cuit + ").");
        }

        /// Vuelve a habilitar un proveedor dado de baja (por ejemplo, si se dio de
        /// baja por error o si se vuelve a trabajar con él). Se hace desde Baja de
        /// proveedor, marcando "Mostrar también los dados de baja".
        public void Reactivar(string cuit)
        {
            BEProveedor proveedor = ObtenerPorCuit(cuit);
            if (proveedor == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (proveedor.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El proveedor ya se encuentra activo."));
            }

            bllDV.EjecutarControlado(() => dalProveedor.Activar(proveedor.Cuit), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Reactivación de proveedor",
                "Se reactivó al proveedor " + proveedor.Nombre + " (CUIT " + proveedor.Cuit + ").");
        }

        #endregion

        #region CUIT

        /// Lleva el CUIT al formato XX-XXXXXXXX-X, que es como se guarda. Se acepta
        /// que el usuario lo escriba con o sin guiones. Si no tiene 11 dígitos se
        /// devuelve tal cual, y la validación se encarga de rechazarlo.
        public static string NormalizarCuit(string cuit)
        {
            if (string.IsNullOrEmpty(cuit))
            {
                return string.Empty;
            }

            string limpio = cuit.Trim();
            if (!EsNumeroDeCuit(limpio))
            {
                return limpio;
            }

            string digitos = SoloDigitos(limpio);
            if (digitos.Length != 11)
            {
                return limpio;
            }

            return digitos.Substring(0, 2) + "-" + digitos.Substring(2, 8) + "-" + digitos.Substring(10, 1);
        }

        /// Controla el dígito verificador del CUIT (módulo 11, con los
        /// multiplicadores 5-4-3-2-7-6-5-4-3-2 que usa la AFIP).
        public static bool EsCuitValido(string cuit)
        {
            if (string.IsNullOrEmpty(cuit) || !EsNumeroDeCuit(cuit))
            {
                return false;
            }

            string digitos = SoloDigitos(cuit);
            if (digitos.Length != 11)
            {
                return false;
            }

            int[] multiplicadores = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int suma = 0;
            for (int i = 0; i < multiplicadores.Length; i++)
            {
                suma += (digitos[i] - '0') * multiplicadores[i];
            }

            int verificador = 11 - (suma % 11);
            if (verificador == 11)
            {
                verificador = 0;
            }
            else if (verificador == 10)
            {
                // Con resto 10 la AFIP no emite el número: no hay CUIT válido así.
                return false;
            }

            return verificador == digitos[10] - '0';
        }

        /// Solo dígitos y los separadores que se suelen usar al escribir un CUIT.
        private static bool EsNumeroDeCuit(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return false;
            }

            bool hayDigito = false;
            foreach (char c in texto)
            {
                if (char.IsDigit(c))
                {
                    hayDigito = true;
                }
                else if (c != '-' && c != ' ' && c != '.')
                {
                    return false;
                }
            }
            return hayDigito;
        }

        private static string SoloDigitos(string texto)
        {
            StringBuilder digitos = new StringBuilder();
            if (texto != null)
            {
                foreach (char c in texto)
                {
                    if (char.IsDigit(c))
                    {
                        digitos.Append(c);
                    }
                }
            }
            return digitos.ToString();
        }

        #endregion

        #region Validaciones internas

        /// Saca espacios sobrantes y deja el CUIT en su formato de guardado.
        private static void Normalizar(BEProveedor proveedor)
        {
            if (proveedor == null)
            {
                return;
            }

            proveedor.Cuit = NormalizarCuit(proveedor.Cuit);
            proveedor.Nombre = Recortar(proveedor.Nombre);
            proveedor.RazonSocial = Recortar(proveedor.RazonSocial);
            proveedor.Correo = Recortar(proveedor.Correo);
            proveedor.Numero = Recortar(proveedor.Numero);

            // El teléfono es opcional: si viene vacío se guarda NULL.
            if (proveedor.Numero.Length == 0)
            {
                proveedor.Numero = null;
            }
        }

        private static string Recortar(string valor)
        {
            return valor == null ? string.Empty : valor.Trim();
        }

        private static void ValidarDatos(BEProveedor proveedor)
        {
            if (proveedor == null ||
                string.IsNullOrEmpty(proveedor.Cuit) ||
                string.IsNullOrEmpty(proveedor.Nombre) ||
                string.IsNullOrEmpty(proveedor.RazonSocial) ||
                string.IsNullOrEmpty(proveedor.Correo))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            if (SoloDigitos(proveedor.Cuit).Length != 11 || !EsNumeroDeCuit(proveedor.Cuit))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El CUIT debe tener 11 dígitos, con o sin guiones (por ejemplo 20-12345678-6)."));
            }

            if (!EsCuitValido(proveedor.Cuit))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El CUIT " + proveedor.Cuit + " no es válido: el último número no corresponde a los " +
                    "anteriores. Revise que esté bien escrito."));
            }

            ValidarLargo(proveedor.Nombre, LargoNombre, "El nombre");
            ValidarLargo(proveedor.RazonSocial, LargoRazonSocial, "La razón social");
            ValidarLargo(proveedor.Correo, LargoCorreo, "El correo electrónico");
            ValidarLargo(proveedor.Numero, LargoNumero, "El teléfono");

            ValidarCorreo(proveedor.Correo);
            ValidarTelefono(proveedor.Numero);
        }

        private static void ValidarLargo(string valor, int largoMaximo, string campo)
        {
            if (valor != null && valor.Length > largoMaximo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    campo + " no puede superar los " + largoMaximo + " caracteres."));
            }
        }

        private static void ValidarCorreo(string correo)
        {
            int arroba = correo.IndexOf('@');
            int punto = correo.LastIndexOf('.');

            bool valido = arroba > 0 &&
                          arroba == correo.LastIndexOf('@') &&
                          punto > arroba + 1 &&
                          punto < correo.Length - 1 &&
                          correo.IndexOf(' ') < 0;

            if (!valido)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El correo debe tener texto antes de la @, y texto con un punto (el dominio) después de la @."));
            }
        }

        /// El teléfono es opcional. Si se carga, solo admite números y los
        /// caracteres habituales para escribirlo: espacios, guiones, paréntesis y +.
        private static void ValidarTelefono(string numero)
        {
            if (string.IsNullOrEmpty(numero))
            {
                return;
            }

            int digitos = 0;
            foreach (char c in numero)
            {
                if (char.IsDigit(c))
                {
                    digitos++;
                }
                else if (c != ' ' && c != '-' && c != '(' && c != ')' && c != '+')
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                        "El teléfono solo puede contener números, espacios, guiones, paréntesis y el signo +."));
                }
            }

            if (digitos < 6)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El teléfono debe tener al menos 6 números."));
            }
        }

        /// El correo de contacto no se puede repetir entre proveedores (en el
        /// diccionario de datos figura como UNIQUE). Se excluye al propio proveedor
        /// para que la modificación pueda conservar su correo.
        private void VerificarCorreoUnico(string correo, string cuitPropio)
        {
            foreach (BEProveedor otro in dalProveedor.TraerProveedores())
            {
                if (otro.Cuit != cuitPropio &&
                    string.Equals(otro.Correo, correo, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(AdministradorErrores.Mensaje("ERR010",
                        "El correo " + correo + " ya está registrado para el proveedor " + otro.Nombre + "."));
                }
            }
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return !string.IsNullOrEmpty(valor) &&
                   valor.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
