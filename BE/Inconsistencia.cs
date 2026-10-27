using System;

namespace BE
{
    /// Tipo de inconsistencia detectada al verificar los dígitos verificadores.
    public enum TipoInconsistencia
    {
        CeldaAlterada,
        FilaAlterada,
        FilaAgregada,
        FilaEliminada,
        TablaAlterada,
        SinDigitos
    }

    /// Detalle de una inconsistencia de integridad. Permite informarle al
    /// WebMaster en que tabla, en que fila y en que columna esta el problema.
    public class Inconsistencia
    {
        public Inconsistencia()
        {
            Tabla = string.Empty;
            ClaveFila = string.Empty;
            Columna = string.Empty;
            DVEsperado = string.Empty;
            DVCalculado = string.Empty;
        }

        public string Tabla { get; set; }

        /// Numero de fila dentro de la tabla (base 1). Vale 0 si no aplica.
        public int NumeroFila { get; set; }

        /// Clave primaria del registro afectado, con formato columna = valor.
        public string ClaveFila { get; set; }

        /// Columna afectada. Cadena vacia si no se pudo aislar una unica columna.
        public string Columna { get; set; }

        public string DVEsperado { get; set; }

        public string DVCalculado { get; set; }

        public TipoInconsistencia Tipo { get; set; }

        /// Faltan los dígitos de esta tabla pero no los de las demás: no es una
        /// base recién creada, alguien los borró.
        public bool Sospechosa { get; set; }

        public string TipoTexto
        {
            get
            {
                switch (Tipo)
                {
                    case TipoInconsistencia.CeldaAlterada: return "Celda alterada";
                    case TipoInconsistencia.FilaAlterada: return "Fila alterada";
                    case TipoInconsistencia.FilaAgregada: return "Fila agregada";
                    case TipoInconsistencia.FilaEliminada: return "Fila eliminada";
                    case TipoInconsistencia.SinDigitos: return "Sin dígitos verificadores";
                    default: return "Tabla alterada";
                }
            }
        }

        public string FilaTexto
        {
            get { return NumeroFila > 0 ? NumeroFila.ToString() : "-"; }
        }

        public string ColumnaTexto
        {
            get { return string.IsNullOrEmpty(Columna) ? "(indeterminada)" : Columna; }
        }

        public string Descripcion
        {
            get
            {
                switch (Tipo)
                {
                    case TipoInconsistencia.CeldaAlterada:
                        return "Se alteró el valor de la columna " + Columna + " en la fila " + NumeroFila +
                               " [" + ClaveFila + "] de la tabla " + Tabla + ".";
                    case TipoInconsistencia.FilaAlterada:
                        return "Se alteró la fila " + NumeroFila + " [" + ClaveFila + "] de la tabla " + Tabla +
                               ". No fue posible aislar una única columna.";
                    case TipoInconsistencia.FilaAgregada:
                        return "Se agregó la fila [" + ClaveFila + "] a la tabla " + Tabla +
                               " sin recalcular el dígito verificador.";
                    case TipoInconsistencia.FilaEliminada:
                        return "Se eliminó la fila [" + ClaveFila + "] de la tabla " + Tabla +
                               " sin recalcular el dígito verificador.";
                    case TipoInconsistencia.SinDigitos:
                        return Sospechosa
                            ? "La tabla " + Tabla + " no tiene dígitos verificadores guardados, pero el resto " +
                              "de las tablas sí: alguien los borró. Antes de recalcular, revise sus datos o " +
                              "restaure una copia de seguridad."
                            : "La tabla " + Tabla + " no tiene dígitos verificadores guardados. Es normal " +
                              "en una base recién creada o actualizada con el script.";
                    default:
                        return "El dígito verificador de la tabla " + Tabla +
                               " no coincide con el almacenado en la base de datos.";
                }
            }
        }

        /// Texto corto de ubicacion para mostrar en la grilla de reparacion.
        public string Ubicacion
        {
            get { return "Tabla " + Tabla + " / Fila " + FilaTexto + " / Columna " + ColumnaTexto; }
        }
    }
}
