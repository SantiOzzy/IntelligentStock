using System;
using System.Collections.Generic;
using BE;

namespace BLL
{
    /// Resultado del inicio de sesión. Ademas del usuario autenticado informa
    /// el estado de integridad de la base de datos verificado durante el login.
    public class ResultadoLogin
    {
        public ResultadoLogin()
        {
            Inconsistencias = new List<Inconsistencia>();
        }

        public BEUsuario Usuario { get; set; }

        public List<Inconsistencia> Inconsistencias { get; set; }

        public bool BaseConsistente
        {
            get { return Inconsistencias == null || Inconsistencias.Count == 0; }
        }

        /// Indica si el usuario tiene perfil de administrador del sistema.
        /// Es quien puede ver el detalle de la inconsistencia y repararla.
        public bool EsAdministrador { get; set; }
    }
}
