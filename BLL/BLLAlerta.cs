using System;
using System.Collections.Generic;
using System.IO;
using BE;
using DAL;
using Servicios;

namespace BLL
{
    /// Reglas de negocio de las alertas térmicas (CU-007-043). Las alertas las
    /// genera el servicio web de telemetría cuando un sensor informa una
    /// temperatura por encima de la máxima permitida, y las atiende el técnico.
    public class BLLAlerta
    {
        private const string Tabla = "Alerta";

        /// Largo de la columna observaciones.
        public const int LargoObservaciones = 500;

        /// Los avisos de alerta nueva van a los usuarios con este rol.
        private const string RolDestinatario = "Tecnico";

        /// Lecturas fuera de este rango se consideran un error del dispositivo.
        private const double LecturaMinimaValida = -100d;
        private const double LecturaMaximaValida = 150d;

        /// En la bitácora las observaciones se recortan para no pasar el largo
        /// de la descripción del evento.
        private const int LargoObservacionesEnBitacora = 250;

        private readonly DALAlerta dalAlerta = new DALAlerta();
        private readonly DALSensor dalSensor = new DALSensor();
        private readonly DALUsuario dalUsuario = new DALUsuario();
        private readonly BLLDigitoVerificador bllDV = new BLLDigitoVerificador();
        private readonly BLLEvento bllEvento = new BLLEvento();

        #region Consultas

        public List<BEAlerta> TraerAlertas()
        {
            return dalAlerta.TraerAlertas();
        }

        /// Alertas que todavía hay que atender (pendientes o en revisión).
        public List<BEAlerta> TraerAbiertas()
        {
            return TraerAlertas().FindAll(delegate(BEAlerta alerta) { return alerta.Abierta; });
        }

        public BEAlerta ObtenerPorId(int idAlerta)
        {
            return idAlerta <= 0 ? null : dalAlerta.ObtenerPorId(idAlerta);
        }

        /// Valor en riesgo ante una falla de frío: el stock de insumos
        /// perecederos valorizado a su precio unitario. Los sensores no están
        /// asociados a insumos puntuales, así que se toma todo lo perecedero.
        public decimal CalcularValorEnRiesgo()
        {
            return new BLLInsumo().CalcularValorStockPerecedero();
        }

        #endregion

        #region Lecturas de los sensores

        /// Procesa una lectura que llega del sistema externo de telemetría. La
        /// autenticación del origen (R9) la hace antes el servicio web. No hay
        /// usuario con sesión: los eventos quedan a nombre del sistema.
        public ResultadoLectura RegistrarLectura(string macAddress, double temperatura)
        {
            string mac = BLLSensor.NormalizarMac(macAddress);
            if (mac == null)
            {
                return new ResultadoLectura(TipoResultadoLectura.DatosInvalidos, 0,
                    "La dirección MAC recibida no es válida.");
            }

            if (double.IsNaN(temperatura) || double.IsInfinity(temperatura) ||
                temperatura < LecturaMinimaValida || temperatura > LecturaMaximaValida)
            {
                return new ResultadoLectura(TipoResultadoLectura.DatosInvalidos, 0,
                    "La temperatura recibida está fuera del rango que puede medir un sensor.");
            }

            temperatura = Math.Round(temperatura, 1);

            BESensor sensor = dalSensor.ObtenerActivoPorMac(mac);
            if (sensor == null)
            {
                // Un dispositivo dado de baja puede seguir mandando lecturas: se
                // registra una vez cada hora por MAC, no en cada lectura.
                if (LimitadorDeEventos.PermitirRechazo("mac:" + mac, TimeSpan.FromHours(1)))
                {
                    bllEvento.Registrar(null, "Lectura de sensor no registrado",
                        "Llegó una lectura de " + temperatura.ToString("0.0") + " °C del dispositivo " + mac +
                        ", que no corresponde a ningún sensor activo. Las lecturas siguientes de ese " +
                        "dispositivo no se registran durante una hora.", CriticidadEvento.Advertencia);
                }

                return new ResultadoLectura(TipoResultadoLectura.SensorNoRegistrado, 0,
                    "No hay ningún sensor activo con la MAC " + mac + ".");
            }

            if (temperatura <= sensor.TemperaturaMaxima)
            {
                return new ResultadoLectura(TipoResultadoLectura.SinNovedad, 0,
                    "Temperatura dentro de lo permitido (máxima " + sensor.TemperaturaMaximaTexto + ").");
            }

            string resultado = null;
            int idAlerta;
            try
            {
                // Si la lectura no cambió nada (el sensor ya tenía abierta una
                // alerta con una temperatura igual o mayor) no se recalculan los
                // dígitos: un sensor que sigue caliente manda muchas lecturas así.
                idAlerta = bllDV.EjecutarControlado(delegate
                {
                    string queHizo;
                    int id = dalAlerta.RegistrarLecturaFueraDeRango(sensor.IdSensor, temperatura,
                        BEAlerta.EstadoPendiente, out queHizo);
                    resultado = queHizo;
                    return id;
                }, delegate(int id) { return resultado != DALAlerta.ResultadoSinCambios; }, Tabla);
            }
            catch (ApplicationException ex)
            {
                // Con la tabla Alerta alterada la alerta no se graba (R4), pero
                // la lectura no se puede perder: queda en la bitácora.
                // Una vez cada 10 minutos por sensor, para no llenar la bitácora.
                if (AdministradorErrores.CodigoDelMensaje(ex.Message) == "ERR007" &&
                    LimitadorDeEventos.Permitir("alerta-no-registrada:" + mac, TimeSpan.FromMinutes(10)))
                {
                    bllEvento.Registrar(null, "Alerta térmica no registrada",
                        "El sensor " + mac + " informó " + temperatura.ToString("0.0") + " °C (máxima " +
                        sensor.TemperaturaMaximaTexto + "), pero la alerta no se pudo grabar porque la tabla " +
                        "Alerta tiene inconsistencias de dígito verificador. Las lecturas siguientes de ese " +
                        "sensor no se registran durante 10 minutos. [#ERR007]", CriticidadEvento.Grave);
                }
                throw;
            }

            if (resultado == DALAlerta.ResultadoSinCambios)
            {
                return new ResultadoLectura(TipoResultadoLectura.AlertaYaAbierta, idAlerta,
                    "El sensor ya tiene abierta la alerta N° " + idAlerta + ".");
            }

            BEAlerta alerta = dalAlerta.ObtenerPorId(idAlerta);
            string descripcion = alerta != null
                ? alerta.Descripcion
                : "Temperatura de " + temperatura.ToString("0.0") + " °C en el sensor " + mac;

            if (resultado == DALAlerta.ResultadoActualizada)
            {
                bllEvento.Registrar(null, "Alerta térmica",
                    "Alerta N° " + idAlerta + " sigue abierta y la temperatura subió. " + descripcion + ".",
                    CriticidadEvento.Advertencia);

                return new ResultadoLectura(TipoResultadoLectura.AlertaActualizada, idAlerta,
                    "Se actualizó la temperatura alcanzada de la alerta N° " + idAlerta + ".");
            }

            bllEvento.Registrar(null, "Alerta térmica",
                "Se generó la alerta N° " + idAlerta + ". " + descripcion + ".",
                CriticidadEvento.Advertencia);

            AvisarATecnicos(idAlerta, descripcion);

            return new ResultadoLectura(TipoResultadoLectura.AlertaGenerada, idAlerta,
                "Se generó la alerta N° " + idAlerta + ".");
        }

        /// Avisa por correo a los técnicos activos. Si el SMTP no está
        /// configurado en el Web.config, EnviadorCorreo no envía nada.
        private void AvisarATecnicos(int idAlerta, string descripcion)
        {
            try
            {
                string asunto = "IntelligentStock: alerta térmica N° " + idAlerta;
                string cuerpo = "ALERTA TÉRMICA\n\n" + descripcion + ".\n\n" +
                                "Ingrese a Telemetría IoT > Procesar alerta térmica para atenderla.";

                foreach (BEUsuario usuario in dalUsuario.TraerUsuarios())
                {
                    if (!usuario.Activo || usuario.Bloqueado ||
                        string.IsNullOrEmpty(usuario.CorreoElectronico) ||
                        !string.Equals(usuario.NombreRol, RolDestinatario, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        EnviadorCorreo.Enviar(usuario.CorreoElectronico, asunto, cuerpo);
                    }
                    catch (Exception)
                    {
                        // Un correo que no sale no frena el aviso a los demás.
                    }
                }
            }
            catch (Exception)
            {
                // El aviso por correo nunca impide registrar la alerta.
            }
        }

        #endregion

        #region Procesamiento

        /// CU-007-043 Procesar alerta térmica. Cambia el estado de una alerta
        /// abierta y guarda las observaciones del técnico. Devuelve la alerta
        /// actualizada.
        public BEAlerta Procesar(int idAlerta, string nuevoEstado, string observaciones)
        {
            observaciones = observaciones == null ? string.Empty : observaciones.Trim();

            if (idAlerta <= 0 || string.IsNullOrEmpty(nuevoEstado) || observaciones.Length == 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR002"));
            }

            if (Array.IndexOf(BEAlerta.Estados, nuevoEstado) < 0)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "El estado elegido no es válido."));
            }

            if (observaciones.Length > LargoObservaciones)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Las observaciones no pueden superar los " + LargoObservaciones + " caracteres."));
            }

            BEAlerta alerta = dalAlerta.ObtenerPorId(idAlerta);
            if (alerta == null)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR013"));
            }

            if (!alerta.Abierta)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La alerta N° " + idAlerta + " ya está " + alerta.Estado.ToLower() + " y no admite cambios."));
            }

            if (alerta.Estado == nuevoEstado && alerta.Observaciones == observaciones)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La alerta ya se encuentra en estado " + nuevoEstado + " con esas mismas observaciones."));
            }

            string estadoAnterior = alerta.Estado;

            // El procedimiento solo actualiza si la alerta sigue abierta: dos
            // técnicos al mismo tiempo no pueden reabrir una alerta ya cerrada.
            if (!bllDV.EjecutarControlado(() => dalAlerta.Procesar(idAlerta, nuevoEstado, observaciones), Tabla))
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR021",
                    "La alerta N° " + idAlerta + " fue cerrada por otro usuario mientras tanto."));
            }

            string observacionesEvento = observaciones.Length > LargoObservacionesEnBitacora
                ? observaciones.Substring(0, LargoObservacionesEnBitacora) + "..."
                : observaciones;

            bllEvento.RegistrarDelUsuarioActual("Procesamiento de alerta térmica",
                "Alerta N° " + idAlerta + " del sensor " + alerta.MacSensor + ": estado anterior " + estadoAnterior +
                ", estado nuevo " + nuevoEstado + ". Observaciones: " + observacionesEvento);

            return dalAlerta.ObtenerPorId(idAlerta);
        }

        #endregion

        #region Exportación XML

        /// Genera AlertasGeneradas.xml con todas las alertas y devuelve la ruta
        /// del archivo.
        public string ExportarXml()
        {
            List<BEAlerta> alertas = TraerAlertas();
            string ruta;

            try
            {
                ruta = new DALAlertasXml().Exportar(alertas);
            }
            catch (UnauthorizedAccessException)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR022",
                    "No se pudo escribir en " + Path.GetDirectoryName(DALAlertasXml.RutaArchivo) + "."));
            }
            catch (IOException ex)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR015", ex.Message));
            }

            bllEvento.RegistrarDelUsuarioActual("Exportación de alertas",
                "Se exportaron " + alertas.Count + " alertas térmicas al archivo " + Path.GetFileName(ruta) + ".");

            return ruta;
        }

        #endregion
    }
}
