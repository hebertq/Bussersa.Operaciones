using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Modelo.Report;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace Utilidades.ClasesGenericas
{
    public static class InssReporteHelper
    {
        public static (string pNombre, string pApellido) SplitNombreCompleto(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto)) return ("", "");
            var parts = nombreCompleto.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return (parts[0], "");
            if (parts.Length == 2) return (parts[0], parts[1]);
            // En nombres de 3 o más partes en Nicaragua:
            // [Nombre1, Nombre2, Apellido1] o [Nombre1, Nombre2, Apellido1, Apellido2]
            // parts[0] es el Primer Nombre y parts[2] es el Primer Apellido
            return (parts[0], parts.Length >= 3 ? parts[2] : parts[1]);
        }

        public static string CleanInss(string noInss)
        {
            if (string.IsNullOrWhiteSpace(noInss)) return "0";
            var raw = noInss.Trim();
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valInv))
            {
                return Convert.ToInt64(valInv).ToString();
            }
            if (decimal.TryParse(raw, out decimal valLoc))
            {
                return Convert.ToInt64(valLoc).ToString();
            }
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? "0" : digits;
        }

        public static DataTable CrearDataTableInss(List<NominaMensualReportar> nominaList, int? anio = null, int? mes = null)
        {
            var dt = new DataTable("NovedadesINSS");
            dt.Columns.Add("nss", typeof(string));
            dt.Columns.Add("p-nombre", typeof(string));
            dt.Columns.Add("p-apellido", typeof(string));
            dt.Columns.Add("nomina", typeof(int));
            dt.Columns.Add("tipo-novedad", typeof(string));
            dt.Columns.Add("fecha", typeof(string));
            dt.Columns.Add("sal-devengado", typeof(decimal));
            dt.Columns.Add("sal-mensual", typeof(decimal));
            dt.Columns.Add("semanas", typeof(string));
            dt.Columns.Add("centro-costo", typeof(string));
            dt.Columns.Add("tipo-empleo", typeof(string));
            dt.Columns.Add("puesto", typeof(string));

            if (nominaList == null) return dt;

            foreach (var item in nominaList)
            {
                var (pNombre, pApellido) = SplitNombreCompleto(item.emp_nombre);
                decimal dias = item.emp_diaslab ?? 0;
                bool esMayor28 = dias > 28;
                string tipoNovedad = esMayor28 ? "03" : "04";

                decimal salDevengado = item.emp_ingresos_deducir ?? item.emp_total_ingresos ?? item.emp_sal_basico ?? 0;
                decimal salMensual = esMayor28 ? (item.emp_sal_fijo ?? 0) : 0m;

                string fechaStr;
                if (item.fec_inicio.HasValue)
                {
                    fechaStr = item.fec_inicio.Value.ToString("yyyy-MM-01");
                }
                else if (anio.HasValue && mes.HasValue)
                {
                    fechaStr = $"{anio.Value:D4}-{mes.Value:D2}-01";
                }
                else
                {
                    fechaStr = DateTime.Now.ToString("yyyy-MM-01");
                }

                string semanasStr = string.IsNullOrWhiteSpace(item.emp_semanas_labs)
                    ? (esMayor28 ? "11111" : "11110")
                    : item.emp_semanas_labs;

                dt.Rows.Add(
                    CleanInss(item.emp_noinss),
                    pNombre,
                    pApellido,
                    item.emp_nomina > 0 ? item.emp_nomina : 1,
                    tipoNovedad,
                    fechaStr,
                    Math.Round(salDevengado, 2),
                    Math.Round(salMensual, 2),
                    semanasStr,
                    "", // centro-costo
                    "", // tipo-empleo
                    ""  // puesto
                );
            }

            return dt;
        }

        public static string GenerarCsvInss(List<NominaMensualReportar> nominaList, int? anio = null, int? mes = null)
        {
            var dt = CrearDataTableInss(nominaList, anio, mes);
            var sb = new StringBuilder();

            // Encabezado
            var columnNames = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            // Filas
            foreach (DataRow row in dt.Rows)
            {
                var fields = dt.Columns.Cast<DataColumn>().Select(c =>
                {
                    var val = row[c];
                    if (val is decimal d) return d.ToString("0.00", CultureInfo.InvariantCulture);
                    return val?.ToString() ?? "";
                });
                sb.AppendLine(string.Join(",", fields));
            }

            return sb.ToString();
        }

        public static byte[] GenerarExcelInssBytes(List<NominaMensualReportar> nominaList, int? anio = null, int? mes = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pack = new ExcelPackage();
            var ws = pack.Workbook.Worksheets.Add("NovedadesINSS");
            var dt = CrearDataTableInss(nominaList, anio, mes);

            ws.Cells["A1"].LoadFromDataTable(dt, true, TableStyles.Light2);

            if (dt.Rows.Count > 0)
            {
                // Formato número para salarios
                ws.Cells[2, 7, dt.Rows.Count + 1, 8].Style.Numberformat.Format = "0.00";
                // Formato texto para columnas que no deben perder ceros
                ws.Cells[2, 1, dt.Rows.Count + 1, 1].Style.Numberformat.Format = "@";
                ws.Cells[2, 5, dt.Rows.Count + 1, 5].Style.Numberformat.Format = "@";
                ws.Cells[2, 9, dt.Rows.Count + 1, 9].Style.Numberformat.Format = "@";
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return pack.GetAsByteArray();
        }
    }
}
