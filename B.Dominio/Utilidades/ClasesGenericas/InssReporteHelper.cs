using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Report;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace Utilidades.ClasesGenericas
{
    public class InssReporteRow
    {
        public string Nss { get; set; } = string.Empty;
        public string PNombre { get; set; } = string.Empty;
        public string PApellido { get; set; } = string.Empty;
        public int Nomina { get; set; } = 1;
        public string TipoNovedad { get; set; } = "03";
        public string Fecha { get; set; } = string.Empty;
        public decimal SalDevengado { get; set; }
        public decimal SalMensual { get; set; }
        public string Semanas { get; set; } = string.Empty;
        public string CentroCosto { get; set; } = string.Empty;
        public string TipoEmpleo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
    }

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

        /// <summary>
        /// Obtiene los rangos de fechas (Inicio, Fin) de cada semana del mes de acuerdo al estándar del INSS de Nicaragua
        /// computado por sábados (de domingo a sábado). Devuelve 4 o 5 semanas según la cantidad de sábados en el mes.
        /// </summary>
        public static List<(DateTime Inicio, DateTime Fin)> ObtenerSemanasDelMes(int anio, int mes)
        {
            var primerDia = new DateTime(anio, mes, 1);
            int diasEnMes = DateTime.DaysInMonth(anio, mes);
            var ultimoDia = new DateTime(anio, mes, diasEnMes);

            var sabados = new List<DateTime>();
            for (int d = 1; d <= diasEnMes; d++)
            {
                var fecha = new DateTime(anio, mes, d);
                if (fecha.DayOfWeek == DayOfWeek.Saturday)
                {
                    sabados.Add(fecha);
                }
            }

            var semanas = new List<(DateTime Inicio, DateTime Fin)>();
            if (sabados.Count == 4)
            {
                semanas.Add((primerDia, sabados[0]));
                semanas.Add((sabados[0].AddDays(1), sabados[1]));
                semanas.Add((sabados[1].AddDays(1), sabados[2]));
                semanas.Add((sabados[2].AddDays(1), ultimoDia));
            }
            else if (sabados.Count >= 5)
            {
                semanas.Add((primerDia, sabados[0]));
                semanas.Add((sabados[0].AddDays(1), sabados[1]));
                semanas.Add((sabados[1].AddDays(1), sabados[2]));
                semanas.Add((sabados[2].AddDays(1), sabados[3]));
                semanas.Add((sabados[3].AddDays(1), ultimoDia));
            }
            else
            {
                // Fallback por bloques de 7 días si fuera un caso anómalo
                semanas.Add((primerDia, primerDia.AddDays(6)));
                semanas.Add((primerDia.AddDays(7), primerDia.AddDays(13)));
                semanas.Add((primerDia.AddDays(14), primerDia.AddDays(20)));
                semanas.Add((primerDia.AddDays(21), ultimoDia));
            }

            return semanas;
        }

        /// <summary>
        /// Calcula la cadena binaria de 5 posiciones de semanas trabajadas para el INSS ('11111', '11110', '00100', etc.)
        /// asegurando que NUNCA se marquen semanas anteriores a la fecha de ingreso ni posteriores al retiro.
        /// </summary>
        public static string CalcularSemanasInss(DateTime? fecInicio, DateTime? fecFin, decimal diasLab, int anio, int mes)
        {
            var primerDiaMes = new DateTime(anio, mes, 1);
            int diasEnMes = DateTime.DaysInMonth(anio, mes);
            var ultimoDiaMes = new DateTime(anio, mes, diasEnMes);

            var semanasMes = ObtenerSemanasDelMes(anio, mes);
            int totalSemanasMes = semanasMes.Count; // 4 o 5

            if (diasLab <= 0) return "00000";

            // Si laboró más de 28 días, es mes completo:
            if (diasLab > 28)
            {
                return totalSemanasMes == 4 ? "11110" : "11111";
            }

            // Fecha efectiva de ingreso y retiro en el mes actual
            DateTime ingresoEfectivo = fecInicio.HasValue && fecInicio.Value >= primerDiaMes 
                ? fecInicio.Value.Date 
                : primerDiaMes;

            DateTime finEfectivo = fecFin.HasValue && fecFin.Value <= ultimoDiaMes 
                ? fecFin.Value.Date 
                : ultimoDiaMes;

            // Determinar qué semanas del mes caen dentro del período de contratación del empleado
            var elegibles = new List<int>();
            for (int i = 0; i < semanasMes.Count; i++)
            {
                var sem = semanasMes[i];
                // Una semana que terminó antes del ingreso NO puede marcarse
                bool antesIngreso = sem.Fin.Date < ingresoEfectivo;
                // Una semana que inició después de la fecha de retiro/corte NO puede marcarse
                bool despuesFin = sem.Inicio.Date > finEfectivo;

                if (!antesIngreso && !despuesFin)
                {
                    elegibles.Add(i);
                }
            }

            if (!elegibles.Any())
            {
                // Fallback seguro: tomar la última semana elegible dentro del mes
                elegibles.Add(Math.Min(totalSemanasMes - 1, Math.Max(0, (ingresoEfectivo.Day - 1) / 7)));
            }

            // Cantidad de semanas a marcar según días efectivamente laborados (INSS ~ 7 días por semana)
            int semanasPorDias = diasLab switch
            {
                <= 7 => 1,
                <= 14 => 2,
                <= 21 => 3,
                <= 28 => 4,
                _ => totalSemanasMes
            };

            int aMarcar = Math.Min(semanasPorDias, elegibles.Count);
            if (aMarcar <= 0 && elegibles.Any()) aMarcar = 1;

            var resultado = new char[5] { '0', '0', '0', '0', '0' };
            var seleccionadas = elegibles.Take(aMarcar);
            foreach (int idx in seleccionadas)
            {
                if (idx >= 0 && idx < 5)
                {
                    resultado[idx] = '1';
                }
            }

            return new string(resultado);
        }

        public static List<InssReporteRow> ObtenerFilasInss(List<NominaMensualReportar> nominaList, int? anio = null, int? mes = null)
        {
            return ObtenerFilasInss(nominaList, null, anio, mes);
        }

        public static List<InssReporteRow> ObtenerFilasInss(
            List<NominaMensualReportar> nominaList, 
            IEnumerable<EmpleadosActivos>? listaActivos, 
            int? anio = null, 
            int? mes = null)
        {
            var list = new List<InssReporteRow>();
            if (nominaList == null) return list;

            var activosList = listaActivos?.Where(x => x != null).ToList();

            foreach (var item in nominaList)
            {
                // 1. Obtener nombres preliminares dividiendo el nombre de nómina
                var (pNombre, pApellido) = SplitNombreCompleto(item.emp_nombre);

                EmpleadosActivos? match = null;
                // 2. Si se suministró la lista de asegurados activos (cargada del archivo de activos del INSS),
                // tomar primer nombre y primer apellido del registro oficial del INSS para evitar rechazos en el portal Turing/Newton.
                if (activosList != null && activosList.Any())
                {
                    string cleanNss = CleanInss(item.emp_noinss);
                    bool tieneNss = !string.IsNullOrWhiteSpace(cleanNss) && cleanNss != "0";

                    // Coincidencia 1: Por número de NSS
                    if (tieneNss)
                    {
                        match = activosList.FirstOrDefault(a => 
                            a.NoInss > 0 && CleanInss(a.NoInss.ToString(CultureInfo.InvariantCulture)) == cleanNss);
                    }

                    // Coincidencia 2: Por IdNomina
                    if (match == null && item.emp_nomina > 0)
                    {
                        match = activosList.FirstOrDefault(a => a.IdNomina > 0 && a.IdNomina == item.emp_nomina);
                    }

                    // Coincidencia 3: Por Nombre Completo normalizado
                    if (match == null && !string.IsNullOrWhiteSpace(item.emp_nombre))
                    {
                        string nomNorm = item.emp_nombre.Trim().ToLowerInvariant().Replace(" ", "");
                        match = activosList.FirstOrDefault(a => 
                            !string.IsNullOrWhiteSpace(a.NombreCompleto) && 
                            a.NombreCompleto.Trim().ToLowerInvariant().Replace(" ", "") == nomNorm);
                    }

                    if (match != null)
                    {
                        if (!string.IsNullOrWhiteSpace(match.PrimerNombre))
                        {
                            pNombre = match.PrimerNombre.Trim();
                        }
                        else if (!string.IsNullOrWhiteSpace(match.NombreCorto))
                        {
                            var (ncNom, _) = SplitNombreCompleto(match.NombreCorto);
                            if (!string.IsNullOrWhiteSpace(ncNom)) pNombre = ncNom;
                        }

                        if (!string.IsNullOrWhiteSpace(match.PrimerApellido))
                        {
                            pApellido = match.PrimerApellido.Trim();
                        }
                        else if (!string.IsNullOrWhiteSpace(match.NombreCorto))
                        {
                            var (_, ncApe) = SplitNombreCompleto(match.NombreCorto);
                            if (!string.IsNullOrWhiteSpace(ncApe)) pApellido = ncApe;
                        }
                    }
                }

                decimal dias = item.emp_diaslab ?? 0;
                bool esMayor28 = dias > 28;

                int anioCalc = anio ?? (item.anyo_mes_cierre.HasValue ? item.anyo_mes_cierre.Value / 100 : DateTime.Now.Year);
                int mesCalc = mes ?? (item.anyo_mes_cierre.HasValue ? item.anyo_mes_cierre.Value % 100 : DateTime.Now.Month);

                // REGLA CRÍTICA OFICIAL INSS:
                // Si el colaborador tiene fecha de ingreso reportada ante el INSS en el archivo de activos cargado,
                // esa fecha oficial reportada MANDA sobre cualquier fecha interna para que el portal no rechace con
                // "SE HA MARCADO UNA SEMANA ANTERIOR A LA FECHA DEL MOVIMIENTO".
                DateTime? fecInicioEfectivo = item.fec_inicio;
                if (match?.FechaIngreso.HasValue == true)
                {
                    if (match.FechaIngreso.Value.Year == anioCalc && match.FechaIngreso.Value.Month == mesCalc)
                    {
                        fecInicioEfectivo = match.FechaIngreso.Value;
                    }
                }

                bool esNuevoIngreso = fecInicioEfectivo.HasValue &&
                                      fecInicioEfectivo.Value.Year == anioCalc &&
                                      fecInicioEfectivo.Value.Month == mesCalc;

                // Cálculo congruente de semanas sin marcar antes de la fecha de ingreso
                string semanasStr = CalcularSemanasInss(fecInicioEfectivo, item.fec_fin, dias, anioCalc, mesCalc);

                string tipoNovedad;
                string fechaStr;

                if (esNuevoIngreso)
                {
                    tipoNovedad = "01"; // Alta / Ingreso en el mes
                    fechaStr = fecInicioEfectivo.Value.ToString("yyyy-MM-dd");
                }
                else if (esMayor28)
                {
                    tipoNovedad = "03"; // Mes completo / Cambio ordinario
                    fechaStr = $"{anioCalc:D4}-{mesCalc:D2}-01";
                }
                else
                {
                    tipoNovedad = "04"; // Período incompleto (colaborador previo con ausencias/días parciales)
                    
                    // Si ingresó en este mes, la fecha del movimiento 04 debe coincidir con su fecha de ingreso reportada
                    // para evitar el error de semanas anteriores a la fecha del movimiento en el portal del INSS.
                    if (esNuevoIngreso)
                    {
                        fechaStr = fecInicioEfectivo.Value.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        fechaStr = $"{anioCalc:D4}-{mesCalc:D2}-01";
                    }
                }

                // REGLA OFICIAL INSS: Sal-Devengado a cotizar ante el INSS (monto deducible sujeto a aporte)
                // Se reporta el salario deducible (emp_ingresos_deducir) equivalente al salario fijo o completado con extras/otros ingresos
                decimal salDevengado = (item.emp_ingresos_deducir.HasValue && item.emp_ingresos_deducir.Value > 0)
                    ? item.emp_ingresos_deducir.Value
                    : (item.emp_sal_fijo ?? item.emp_sal_basico ?? 0m);
                if (salDevengado < 0) salDevengado = 0m;

                // REGLA OFICIAL INSS (Turing | Newton):
                // Cuando se reporta Novedad 04 (período incompleto / ausencias / subsidios / días <= 28),
                // el campo sal-mensual debe ser estrictamente 0.00 según los requisitos del INSS.
                // Para Novedad 01 (Ingreso) y Novedad 03 (Mes completo), sal-mensual es el salario mensual pactado (emp_sal_fijo).
                decimal salMensual = tipoNovedad == "04" ? 0m : (item.emp_sal_fijo ?? 0m);

                list.Add(new InssReporteRow
                {
                    Nss = CleanInss(item.emp_noinss),
                    PNombre = pNombre,
                    PApellido = pApellido,
                    Nomina = 1, // Número de nómina de la empresa (Nómina Patronal 1, no número de empleado)
                    TipoNovedad = tipoNovedad,
                    Fecha = fechaStr,
                    SalDevengado = Math.Round(salDevengado, 2),
                    SalMensual = Math.Round(salMensual, 2),
                    Semanas = semanasStr,
                    CentroCosto = "",
                    TipoEmpleo = "",
                    Puesto = ""
                });
            }

            return list;
        }

        public static DataTable CrearDataTableInss(List<NominaMensualReportar> nominaList, int? anio = null, int? mes = null)
        {
            return CrearDataTableInss(nominaList, null, anio, mes);
        }

        public static DataTable CrearDataTableInss(
            List<NominaMensualReportar> nominaList, 
            IEnumerable<EmpleadosActivos>? listaActivos, 
            int? anio = null, 
            int? mes = null)
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

            var filas = ObtenerFilasInss(nominaList, listaActivos, anio, mes);
            foreach (var fila in filas)
            {
                dt.Rows.Add(
                    fila.Nss,
                    fila.PNombre,
                    fila.PApellido,
                    fila.Nomina,
                    fila.TipoNovedad,
                    fila.Fecha,
                    fila.SalDevengado,
                    fila.SalMensual,
                    fila.Semanas,
                    fila.CentroCosto,
                    fila.TipoEmpleo,
                    fila.Puesto
                );
            }

            return dt;
        }

        public static string GenerarCsvInss(List<NominaMensualReportar> nominaList, int? anio = null, int? mes = null)
        {
            return GenerarCsvInss(nominaList, null, anio, mes);
        }

        public static string GenerarCsvInss(
            List<NominaMensualReportar> nominaList, 
            IEnumerable<EmpleadosActivos>? listaActivos, 
            int? anio = null, 
            int? mes = null)
        {
            var dt = CrearDataTableInss(nominaList, listaActivos, anio, mes);
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
            return GenerarExcelInssBytes(nominaList, null, anio, mes);
        }

        public static byte[] GenerarExcelInssBytes(
            List<NominaMensualReportar> nominaList, 
            IEnumerable<EmpleadosActivos>? listaActivos, 
            int? anio = null, 
            int? mes = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pack = new ExcelPackage();
            var ws = pack.Workbook.Worksheets.Add("NovedadesINSS");
            var dt = CrearDataTableInss(nominaList, listaActivos, anio, mes);

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
