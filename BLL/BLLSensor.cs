using System;
using System.Collections.Generic;
using System.Text;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de los sensores IoT (CU-007-039 a CU-007-042).
    public class BLLSensor
    {
        private const string Tabla = "Sensor";

        /// Largo de la columna modelo.
        public const int LargoModelo = 100;

        /// Rango razonable para un equipo de frío: desde un freezer (-60 °C)
        /// hasta una heladera o cámara de conservación.
        public const double TemperaturaMinimaAdmitida = -60d;
        public const double TemperaturaMaximaAdmitida = 60d;

        private readonly DALSensor dalSensor = new DALSensor();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        #region Consultas

        public List<BESensor> TraerSensores()
        {
            return dalSensor.TraerSensores();
        }

        /// Sensores que siguen monitoreando (los dados de baja no se ofrecen).
        public List<BESensor> TraerActivos()
        {
            return TraerSensores().FindAll(delegate(BESensor sensor) { return sensor.Activo; });
        }

        public BESensor ObtenerPorId(int idSensor)
        {
            return idSensor <= 0 ? null : dalSensor.ObtenerPorId(idSensor);
        }

        /// Sensor activo con esa dirección MAC (escrita en cualquier formato), o null.
        public BESensor ObtenerActivoPorMac(string macAddress)
        {
            string mac = NormalizarMac(macAddress);
            return mac == null ? null : dalSensor.ObtenerActivoPorMac(mac);
        }

        /// CU-007-042 Consultar sensor IoT. Filtra por MAC o modelo y por estado.
        public List<BESensor> Filtrar(string texto, string estado)
        {
            string busqueda = texto == null ? string.Empty : texto.Trim();

            // La MAC se puede buscar escrita con guiones o sin separadores. Solo se
            // compara así si lo escrito parece parte de una MAC (dígitos
            // hexadecimales y separadores), para que un modelo no coincida de más.
            string busquedaMac = SoloHexadecimal(busqueda);
            if (busquedaMac.Length < 2 ||
                busquedaMac.Length != busqueda.Replace(":", string.Empty).Replace("-", string.Empty)
                                              .Replace(".", string.Empty).Replace(" ", string.Empty).Length)
            {
                busquedaMac = string.Empty;
            }

            // Si se escribió un número, también se busca el sensor con ese código.
            int codigo;
            bool buscaCodigo = int.TryParse(busqueda, out codigo);

            List<BESensor> resultado = new List<BESensor>();
            foreach (BESensor sensor in TraerSensores())
            {
                if (!string.IsNullOrEmpty(estado) && sensor.Estado != estado)
                {
                    continue;
                }

                if (busqueda.Length > 0 &&
                    !Contiene(sensor.MacAddress, busqueda) &&
                    !Contiene(sensor.Modelo, busqueda) &&
                    !(busquedaMac.Length > 0 && Contiene(SoloHexadecimal(sensor.MacAddress), busquedaMac)) &&
                    !(buscaCodigo && sensor.IdSensor == codigo))
                {
                    continue;
                }

                resultado.Add(sensor);
            }

            return resultado;
        }

        #endregion

        #region ABM

        /// CU-007-039 Alta de sensor IoT. Devuelve el id generado.
        public int Alta(BESensor sensor)
        {
            NormalizarYValidar(sensor);
            VerificarMacDisponible(sensor.MacAddress, 0);

            int id = bllDV.EjecutarControlado(() => dalSensor.Crear(sensor), Tabla);
            sensor.IdSensor = id;

            bllEvento.RegistrarDelUsuarioActual("Alta de sensor",
                "Se dio de alta el sensor " + sensor.MacAddress + " (id " + id + ", modelo " + sensor.ModeloTexto +
                ") con una temperatura máxima permitida de " + sensor.TemperaturaMaximaTexto + ".");

            return id;
        }

        /// CU-007-040 Modificar sensor IoT.
        public void Modificar(BESensor sensor)
        {
            NormalizarYValidar(sensor);

            BESensor anterior = dalSensor.ObtenerPorId(sensor.IdSensor);
            if (anterior == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!anterior.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El sensor se encuentra dado de baja."));
            }

            VerificarMacDisponible(sensor.MacAddress, sensor.IdSensor);

            bllDV.EjecutarControlado(() => dalSensor.Modificar(sensor), Tabla);

            StringBuilder cambios = new StringBuilder();
            AgregarCambio(cambios, "MAC", anterior.MacAddress, sensor.MacAddress);
            AgregarCambio(cambios, "modelo", anterior.ModeloTexto, sensor.ModeloTexto);
            AgregarCambio(cambios, "temperatura máxima", anterior.TemperaturaMaximaTexto, sensor.TemperaturaMaximaTexto);

            bllEvento.RegistrarDelUsuarioActual("Modificación de sensor",
                "Se modificaron los datos del sensor " + sensor.MacAddress + " (id " + sensor.IdSensor + ")." +
                (cambios.Length > 0 ? " Cambios: " + cambios + "." : string.Empty));
        }

        /// CU-007-041 Baja de sensor IoT. Es lógica, y no se permite mientras el
        /// sensor tenga alertas sin resolver: primero hay que procesarlas.
        public void Baja(int idSensor)
        {
            BESensor sensor = ObtenerPorId(idSensor);
            if (sensor == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!sensor.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El sensor ya se encuentra dado de baja."));
            }

            if (sensor.AlertasAbiertas > 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR008",
                    "El sensor " + sensor.MacAddress + " tiene " + sensor.AlertasAbiertas +
                    " alerta(s) térmica(s) sin resolver. Procéselas antes de darlo de baja."));
            }

            bllDV.EjecutarControlado(() => dalSensor.Baja(idSensor), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Baja de sensor",
                "Se dio de baja el sensor " + sensor.MacAddress + " (id " + idSensor + ").");
        }

        /// Vuelve a habilitar un sensor dado de baja, siempre que su MAC no la
        /// esté usando otro sensor activo (#ERR023). Se hace desde Baja de
        /// sensor, marcando "Mostrar también los dados de baja".
        public void Reactivar(int idSensor)
        {
            BESensor sensor = ObtenerPorId(idSensor);
            if (sensor == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (sensor.Activo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "El sensor ya se encuentra activo."));
            }

            VerificarMacDisponible(sensor.MacAddress, idSensor);

            bllDV.EjecutarControlado(() => dalSensor.Activar(idSensor), Tabla);
            bllEvento.RegistrarDelUsuarioActual("Reactivación de sensor",
                "Se reactivó el sensor " + sensor.MacAddress + " (id " + idSensor + ").");
        }

        #endregion

        #region Validaciones

        /// Lleva la dirección MAC al formato AA:BB:CC:DD:EE:FF. Acepta que se
        /// escriba con dos puntos, guiones, puntos o sin separadores, en
        /// mayúsculas o minúsculas. Devuelve null si no es una MAC válida.
        public static string NormalizarMac(string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress))
            {
                return null;
            }

            string limpia = macAddress.Trim().Replace(":", string.Empty).Replace("-", string.Empty)
                                      .Replace(".", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

            if (limpia.Length != 12 || SoloHexadecimal(limpia).Length != 12)
            {
                return null;
            }

            StringBuilder mac = new StringBuilder(17);
            for (int i = 0; i < 12; i += 2)
            {
                if (mac.Length > 0)
                {
                    mac.Append(':');
                }
                mac.Append(limpia, i, 2);
            }
            return mac.ToString();
        }

        private static void NormalizarYValidar(BESensor sensor)
        {
            if (sensor == null || string.IsNullOrEmpty(sensor.MacAddress) || sensor.MacAddress.Trim().Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            string mac = NormalizarMac(sensor.MacAddress);
            if (mac == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La dirección MAC tiene que tener 12 dígitos hexadecimales, por ejemplo 00:1A:2B:3C:4D:5E."));
            }
            sensor.MacAddress = mac;

            sensor.Modelo = sensor.Modelo == null ? null : sensor.Modelo.Trim();
            if (string.IsNullOrEmpty(sensor.Modelo))
            {
                sensor.Modelo = null;
            }
            else if (sensor.Modelo.Length > LargoModelo)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El modelo no puede superar los " + LargoModelo + " caracteres."));
            }

            if (double.IsNaN(sensor.TemperaturaMaxima) ||
                sensor.TemperaturaMaxima < TemperaturaMinimaAdmitida ||
                sensor.TemperaturaMaxima > TemperaturaMaximaAdmitida)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "La temperatura máxima permitida tiene que estar entre " + TemperaturaMinimaAdmitida +
                    " °C y " + TemperaturaMaximaAdmitida + " °C."));
            }

            sensor.TemperaturaMaxima = Math.Round(sensor.TemperaturaMaxima, 1);
        }

        /// #ERR023: la MAC no puede estar asignada a otro sensor activo.
        private void VerificarMacDisponible(string macAddress, int idExcluido)
        {
            BESensor otro = dalSensor.ObtenerActivoPorMac(macAddress);
            if (otro != null && otro.IdSensor != idExcluido)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR023",
                    "La MAC " + macAddress + " corresponde al sensor " + otro.IdSensor + " (" + otro.ModeloTexto + ")."));
            }
        }

        private static void AgregarCambio(StringBuilder cambios, string campo, string antes, string despues)
        {
            if (antes == despues)
            {
                return;
            }

            if (cambios.Length > 0)
            {
                cambios.Append("; ");
            }
            cambios.Append(campo).Append(" de ").Append(antes).Append(" a ").Append(despues);
        }

        private static string SoloHexadecimal(string texto)
        {
            StringBuilder hex = new StringBuilder();
            if (texto == null)
            {
                return string.Empty;
            }

            foreach (char c in texto.ToUpperInvariant())
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))
                {
                    hex.Append(c);
                }
            }
            return hex.ToString();
        }

        private static bool Contiene(string valor, string busqueda)
        {
            return !string.IsNullOrEmpty(valor) &&
                   valor.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion
    }
}
