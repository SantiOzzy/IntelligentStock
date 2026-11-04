using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using BE;

namespace Servicios
{
    /// Estrategia Excel del reporte gerencial (patrón Strategy, ver ExportadorReporte):
    /// un .xlsx es un ZIP con algunos archivos XML adentro; se arman esos archivos
    /// y se comprimen con System.IO.Compression.
    public class FormatoReporteExcel : IFormatoReporte
    {
        public string Nombre
        {
            get { return ExportadorReporte.FormatoExcel; }
        }

        public string Extension
        {
            get { return ".xlsx"; }
        }

        public string TipoMime
        {
            get { return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; }
        }

        public byte[] Generar(BEReporte reporte)
        {
            return GenerarExcel(reporte);
        }

        private static byte[] GenerarExcel(BEReporte reporte)
        {
            using (MemoryStream memoria = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(memoria, ZipArchiveMode.Create, true))
                {
                    Escribir(zip, "[Content_Types].xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                        "</Types>");

                    Escribir(zip, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                        "</Relationships>");

                    Escribir(zip, "xl/workbook.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                        "<sheets><sheet name=\"Reporte\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

                    Escribir(zip, "xl/_rels/workbook.xml.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                        "</Relationships>");

                    // Estilo 0: normal. Estilo 1: negrita (títulos y encabezados).
                    Escribir(zip, "xl/styles.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                        "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                        "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                        "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                        "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                        "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
                        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                        "</styleSheet>");

                    Escribir(zip, "xl/worksheets/sheet1.xml", HojaExcel(reporte));
                }
                return memoria.ToArray();
            }
        }

        private static string HojaExcel(BEReporte reporte)
        {
            StringBuilder hoja = new StringBuilder();
            hoja.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            hoja.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

            // Ancho de cada columna según el texto más largo.
            int columnas = reporte.Columnas.Count;
            hoja.Append("<cols>");
            for (int c = 0; c < columnas; c++)
            {
                int largo = reporte.Columnas[c].Length;
                foreach (string[] fila in reporte.Filas)
                {
                    if (c < fila.Length && fila[c] != null)
                    {
                        largo = Math.Max(largo, fila[c].Length);
                    }
                }
                int ancho = Math.Min(80, Math.Max(10, largo + 2));
                hoja.Append("<col min=\"" + (c + 1) + "\" max=\"" + (c + 1) + "\" width=\"" + ancho + "\" customWidth=\"1\"/>");
            }
            hoja.Append("</cols><sheetData>");

            int numero = 1;
            FilaExcel(hoja, numero++, new[] { reporte.Titulo }, true, false);
            FilaExcel(hoja, numero++, new[] { reporte.Subtitulo }, false, false);
            numero++;
            FilaExcel(hoja, numero++, reporte.Columnas.ToArray(), true, false);
            foreach (string[] fila in reporte.Filas)
            {
                FilaExcel(hoja, numero++, fila, false, true);
            }
            if (!string.IsNullOrEmpty(reporte.Pie))
            {
                numero++;
                FilaExcel(hoja, numero, new[] { reporte.Pie }, false, false);
            }

            hoja.Append("</sheetData></worksheet>");
            return hoja.ToString();
        }

        private static void FilaExcel(StringBuilder hoja, int numero, string[] valores, bool negrita, bool convertirNumeros)
        {
            hoja.Append("<row r=\"" + numero + "\">");
            for (int c = 0; c < valores.Length; c++)
            {
                string referencia = LetraColumna(c) + numero;
                string valor = valores[c] ?? string.Empty;
                decimal numeroCelda;

                if (convertirNumeros && EsNumero(valor, out numeroCelda))
                {
                    hoja.Append("<c r=\"" + referencia + "\"><v>" +
                                numeroCelda.ToString(CultureInfo.InvariantCulture) + "</v></c>");
                }
                else
                {
                    hoja.Append("<c r=\"" + referencia + "\" t=\"inlineStr\"" + (negrita ? " s=\"1\"" : string.Empty) +
                                "><is><t xml:space=\"preserve\">" + SecurityElement.Escape(valor) + "</t></is></c>");
                }
            }
            hoja.Append("</row>");
        }

        /// Solo los números "pelados" (sin símbolo de moneda ni letras) se
        /// guardan como número, para que se puedan sumar en Excel.
        private static bool EsNumero(string valor, out decimal numero)
        {
            numero = 0;
            if (string.IsNullOrEmpty(valor) || valor.Length > 20)
            {
                return false;
            }
            foreach (char c in valor)
            {
                if (!char.IsDigit(c) && c != ',' && c != '.' && c != '-')
                {
                    return false;
                }
            }
            return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.CurrentCulture, out numero);
        }

        private static string LetraColumna(int indice)
        {
            string letras = string.Empty;
            indice++;
            while (indice > 0)
            {
                int resto = (indice - 1) % 26;
                letras = (char)('A' + resto) + letras;
                indice = (indice - 1) / 26;
            }
            return letras;
        }

        private static void Escribir(ZipArchive zip, string nombre, string contenido)
        {
            ZipArchiveEntry entrada = zip.CreateEntry(nombre, CompressionLevel.Optimal);
            using (StreamWriter escritor = new StreamWriter(entrada.Open(), new UTF8Encoding(false)))
            {
                escritor.Write(contenido);
            }
        }
    }
}
