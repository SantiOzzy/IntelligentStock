using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using BE;

namespace DAL
{
    /// Acceso a datos de las tablas Receta y DetalleReceta.
    ///
    /// La receta y su detalle se graban con un único procedimiento almacenado,
    /// así quedan dentro de la misma transacción. El detalle viaja al motor
    /// como un parámetro XML.
    public class DALReceta
    {
        private readonly DALConexion dalCon = new DALConexion();

        /// Graba la receta con su detalle y devuelve el id generado.
        public int Crear(BEReceta receta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@Nombre", receta.Nombre),
                ParametroDetalle(receta.Detalles)
            };

            return dalCon.EjecutarYTraerId("CrearReceta", parametros);
        }

        /// Actualiza el nombre y reemplaza el detalle completo.
        public void Modificar(BEReceta receta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdReceta", receta.IdReceta),
                new SqlParameter("@Nombre", receta.Nombre),
                ParametroDetalle(receta.Detalles)
            };

            dalCon.EjecutarProcAlmacenado("ModificarReceta", parametros);
        }

        /// Baja lógica: marca la receta como inactiva.
        /// Deshace la baja lógica.
        public void Activar(int idReceta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdReceta", idReceta)
            };

            dalCon.EjecutarProcAlmacenado("ActivarReceta", parametros);
        }

        public void Baja(int idReceta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdReceta", idReceta)
            };

            dalCon.EjecutarProcAlmacenado("BajaReceta", parametros);
        }

        /// Todas las recetas, sin el detalle (trae la cantidad de insumos).
        public List<BEReceta> TraerRecetas()
        {
            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerRecetas", null);

            List<BEReceta> lista = new List<BEReceta>();
            foreach (DataRow fila in tabla.Rows)
            {
                lista.Add(Mapear(fila));
            }
            return lista;
        }

        /// Una receta con su detalle completo.
        public BEReceta ObtenerPorId(int idReceta)
        {
            SqlParameter[] parametros = new SqlParameter[]
            {
                new SqlParameter("@IdReceta", idReceta)
            };

            DataTable tabla = dalCon.ConsultaProcAlmacenado("TraerRecetaPorId", parametros);
            if (tabla.Rows.Count == 0)
            {
                return null;
            }

            BEReceta receta = Mapear(tabla.Rows[0]);

            SqlParameter[] parametrosDetalle = new SqlParameter[]
            {
                new SqlParameter("@IdReceta", idReceta)
            };

            DataTable detalle = dalCon.ConsultaProcAlmacenado("TraerDetalleReceta", parametrosDetalle);
            foreach (DataRow fila in detalle.Rows)
            {
                receta.Detalles.Add(MapearDetalle(fila));
            }

            return receta;
        }

        /// Todas las recetas con su detalle, resuelto con dos consultas en vez de
        /// una por receta. Se usa para armar el catálogo XML.
        public List<BEReceta> TraerRecetasConDetalle()
        {
            List<BEReceta> recetas = TraerRecetas();

            Dictionary<int, BEReceta> porId = new Dictionary<int, BEReceta>();
            foreach (BEReceta receta in recetas)
            {
                porId[receta.IdReceta] = receta;
            }

            DataTable detalle = dalCon.ConsultaProcAlmacenado("TraerDetallesDeRecetas", null);
            foreach (DataRow fila in detalle.Rows)
            {
                BEReceta receta;
                if (porId.TryGetValue(Convert.ToInt32(fila["id_receta"]), out receta))
                {
                    receta.Detalles.Add(MapearDetalle(fila));
                }
            }

            return recetas;
        }

        /// Arma el XML del detalle: <Detalle><Insumo idInsumo="1" cantidad="0.25" /></Detalle>.
        /// Las cantidades van con punto decimal, sin importar la cultura del servidor.
        private static SqlParameter ParametroDetalle(List<BEDetalleReceta> detalles)
        {
            StringBuilder xml = new StringBuilder();
            XmlWriterSettings configuracion = new XmlWriterSettings();
            configuracion.OmitXmlDeclaration = true;

            using (XmlWriter writer = XmlWriter.Create(new StringWriter(xml), configuracion))
            {
                writer.WriteStartElement("Detalle");
                foreach (BEDetalleReceta detalle in detalles)
                {
                    writer.WriteStartElement("Insumo");
                    writer.WriteAttributeString("idInsumo", detalle.IdInsumo.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("cantidad", detalle.Cantidad.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            SqlParameter parametro = new SqlParameter("@Detalle", SqlDbType.Xml);
            parametro.Value = xml.ToString();
            return parametro;
        }

        private static BEReceta Mapear(DataRow fila)
        {
            BEReceta receta = new BEReceta();
            receta.IdReceta = Convert.ToInt32(fila["id_receta"]);
            receta.Nombre = Convert.ToString(fila["nombre"]);
            receta.Activo = fila["activo"] != DBNull.Value && Convert.ToBoolean(fila["activo"]);
            receta.CantidadInsumos = Convert.ToInt32(fila["cantidadInsumos"]);
            return receta;
        }

        private static BEDetalleReceta MapearDetalle(DataRow fila)
        {
            return new BEDetalleReceta(
                Convert.ToInt32(fila["id_insumo"]),
                Convert.ToString(fila["nombreInsumo"]),
                Convert.ToString(fila["unidadDeMedida"]),
                Convert.ToDecimal(fila["cantidad"]));
        }
    }
}
