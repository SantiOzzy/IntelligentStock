using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using BE;
using Microsoft.ML;
using Microsoft.ML.Data;
using Servicios;

namespace BLL
{
    /// Predicción de demanda con ML.NET (CU-006-033).
    ///
    /// Idea general, para explicarla en pocas palabras: el modelo aprende de las
    /// ventas registradas cuánto se consume de cada insumo por día, según el día
    /// de la semana y según cuánto se venía consumiendo en los días anteriores.
    /// Con eso estima el consumo de cada uno de los próximos días y los suma.
    ///
    /// Datos: cada venta queda en la bitácora con lo que se consumió de cada
    /// insumo (ver BLLVenta). Con eso se arma, para cada insumo y cada día, una
    /// fila con:
    ///   - el insumo y el día de la semana (datos de categoría),
    ///   - el consumo promedio de los 7 y de los 28 días anteriores,
    ///   - y, como valor a aprender, lo que se consumió ese día.
    ///
    /// Modelo: regresión lineal entrenada con el algoritmo SDCA de ML.NET. Antes
    /// de entrenar, el insumo y el día se pasan a columnas de 0 y 1 (one-hot) y
    /// los números se normalizan entre 0 y 1. Un 20% de las filas se separa
    /// para medir el error del modelo con datos que no vio al entrenar.
    public class BLLPrediccionDemanda
    {
        /// 28 días para poder calcular el primer promedio más 7 para aprender.
        public const int DiasMinimosDeHistorial = 35;

        /// Hasta cuánto historial se usa para entrenar.
        public const int DiasDeHistorialAUsar = 365;

        /// Filas mínimas para entrenar.
        public const int FilasMinimas = 14;

        private readonly BLLVenta bllVenta = new BLLVenta();

        /// Estima el consumo de cada insumo para los próximos 'dias' días
        /// (desde mañana). Lanza #ERR019 si no hay historial suficiente y
        /// #ERR005 si falla ML.NET.
        public ResultadoPrediccion Predecir(int dias)
        {
            if (dias < 1 || dias > 90)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR025",
                    "Los días a proyectar tienen que estar entre 1 y 90."));
            }

            DateTime hoy = DateTime.Today;
            DateTime ayer = hoy.AddDays(-1);

            // El día de hoy no se usa: todavía puede tener ventas por registrar.
            List<BEVenta> ventas = bllVenta.TraerVentas(hoy.AddDays(-DiasDeHistorialAUsar), ayer);
            HistorialConsumo historial = HistorialConsumo.Armar(ventas, ayer);

            if (historial.Dias < DiasMinimosDeHistorial)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR019",
                    historial.Dias == 0
                        ? "Todavía no hay ventas registradas."
                        : "Hay " + historial.Dias + " días de ventas registradas y hacen falta al menos " +
                          DiasMinimosDeHistorial + "."));
            }

            List<FilaConsumo> filas = historial.ArmarFilasDeEntrenamiento();
            if (filas.Count < FilasMinimas)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR019",
                    "Hay muy pocos datos de consumo para entrenar el modelo."));
            }

            ResultadoPrediccion resultado = new ResultadoPrediccion();
            resultado.DiasProyectados = dias;
            resultado.DiasDeHistorial = historial.Dias;
            resultado.FilasDeEntrenamiento = filas.Count;

            try
            {
                Entrenar(filas, resultado, historial, hoy, dias);
            }
            catch (ApplicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(AdministradorErrores.Mensaje("ERR005", ex.Message));
            }

            return resultado;
        }

        private static void Entrenar(List<FilaConsumo> filas, ResultadoPrediccion resultado,
                                     HistorialConsumo historial, DateTime hoy, int dias)
        {
            CargarBibliotecaNativa();

            MLContext ml = new MLContext(seed: 1);
            IDataView datos = ml.Data.LoadFromEnumerable(filas);

            // El insumo y el día de la semana son categorías: cada valor pasa a
            // ser una columna de 0 y 1. Los números se llevan a la escala 0..1.
            var pipeline = ml.Transforms.Categorical.OneHotEncoding("InsumoCodificado", "Insumo")
                .Append(ml.Transforms.Categorical.OneHotEncoding("DiaCodificado", "DiaSemana"))
                .Append(ml.Transforms.Concatenate("Features", "InsumoCodificado", "DiaCodificado",
                                                  "Promedio7", "Promedio28"))
                .Append(ml.Transforms.NormalizeMinMax("Features"))
                .Append(ml.Regression.Trainers.Sdca(labelColumnName: "Consumo", featureColumnName: "Features"));

            // Con suficientes filas se separa un 20% para medir el error con
            // datos que el modelo no vio. Con pocas, se mide sobre las mismas.
            IDataView entrenamiento = datos;
            IDataView prueba = datos;
            if (filas.Count >= 50)
            {
                DataOperationsCatalog.TrainTestData particion = ml.Data.TrainTestSplit(datos, testFraction: 0.2, seed: 1);
                entrenamiento = particion.TrainSet;
                prueba = particion.TestSet;
            }

            ITransformer modelo = pipeline.Fit(entrenamiento);

            RegressionMetrics metricas = ml.Regression.Evaluate(modelo.Transform(prueba), labelColumnName: "Consumo");
            resultado.ErrorMedioAbsoluto = metricas.MeanAbsoluteError;
            resultado.CoeficienteR2 = metricas.RSquared;

            PredictionEngine<FilaConsumo, PrediccionConsumo> motor =
                ml.Model.CreatePredictionEngine<FilaConsumo, PrediccionConsumo>(modelo);

            foreach (int idInsumo in historial.Insumos)
            {
                float promedio7 = historial.Promedio(idInsumo, hoy, 7);
                float promedio28 = historial.Promedio(idInsumo, hoy, 28);

                double total = 0;
                for (int k = 1; k <= dias; k++)
                {
                    DateTime dia = hoy.AddDays(k);
                    FilaConsumo entrada = new FilaConsumo();
                    entrada.Insumo = idInsumo.ToString(CultureInfo.InvariantCulture);
                    entrada.DiaSemana = HistorialConsumo.DiaDeLaSemana(dia);
                    entrada.Promedio7 = promedio7;
                    entrada.Promedio28 = promedio28;

                    PrediccionConsumo prediccion = motor.Predict(entrada);

                    // Un consumo negativo no tiene sentido: se toma como cero.
                    if (!float.IsNaN(prediccion.Consumo) && prediccion.Consumo > 0)
                    {
                        total += prediccion.Consumo;
                    }
                }

                resultado.ConsumoPorInsumo[idInsumo] = Math.Round((decimal)total, 2);
            }
        }

        #region Biblioteca nativa de ML.NET

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string ruta);

        private static readonly object candadoNativa = new object();
        private static bool nativaCargada;

        /// ML.NET hace algunas cuentas con una biblioteca nativa de Windows
        /// (CpuMathNative.dll), que no es un ensamblado .NET. En un sitio web
        /// ASP.NET no la encuentra sola, porque no busca bibliotecas nativas en
        /// la carpeta Bin. BLL.csproj la copia a Bin\x64 y Bin\x86 y acá se
        /// carga la que corresponde a la arquitectura del proceso (IIS Express
        /// puede correr en 32 o en 64 bits). Una vez cargada, ML.NET la usa.
        private static void CargarBibliotecaNativa()
        {
            lock (candadoNativa)
            {
                if (nativaCargada)
                {
                    return;
                }
                nativaCargada = true;

                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    return;
                }

                string arquitectura = Environment.Is64BitProcess ? "x64" : "x86";
                string[] carpetas =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin", arquitectura),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, arquitectura),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin"),
                    AppDomain.CurrentDomain.BaseDirectory
                };

                foreach (string carpeta in carpetas)
                {
                    string ruta = Path.Combine(carpeta, "CpuMathNative.dll");
                    if (File.Exists(ruta))
                    {
                        LoadLibrary(ruta);
                        return;
                    }
                }
            }
        }

        #endregion
    }

    /// Salida del modelo: ML.NET devuelve la estimación en la columna "Score".
    public class PrediccionConsumo
    {
        [ColumnName("Score")]
        public float Consumo;
    }

    /// Resultado de una predicción: el consumo estimado de cada insumo y los
    /// datos del entrenamiento, para informarlos.
    public class ResultadoPrediccion
    {
        private readonly Dictionary<int, decimal> consumoPorInsumo = new Dictionary<int, decimal>();

        public Dictionary<int, decimal> ConsumoPorInsumo
        {
            get { return consumoPorInsumo; }
        }

        public int DiasProyectados { get; set; }

        public int DiasDeHistorial { get; set; }

        public int FilasDeEntrenamiento { get; set; }

        /// Error medio absoluto de la predicción diaria, en la unidad de cada insumo.
        public double ErrorMedioAbsoluto { get; set; }

        public double CoeficienteR2 { get; set; }

        public decimal Consumo(int idInsumo)
        {
            decimal consumo;
            return consumoPorInsumo.TryGetValue(idInsumo, out consumo) ? consumo : 0m;
        }

        public string Descripcion
        {
            get
            {
                return "Modelo de regresión (ML.NET, SDCA) entrenado con " + DiasDeHistorial + " días de ventas (" +
                       FilasDeEntrenamiento + " filas); error medio absoluto por día: " +
                       ErrorMedioAbsoluto.ToString("0.##") + ".";
            }
        }
    }
}
