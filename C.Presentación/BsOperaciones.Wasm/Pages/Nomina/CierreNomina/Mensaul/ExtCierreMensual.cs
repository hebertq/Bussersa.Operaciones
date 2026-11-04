using Modelo.Entidades.Entradas.Odoo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.CierreNomina.Mensaul
{
    public static class ExtCierreMensual
    {
        private static string NormalizeKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            return name.Trim().ToLowerInvariant()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace(".", "")
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u");
        }

        private static string GetStringVal(DataRow row, params string[] columnNames)
        {
            foreach (var col in columnNames)
            {
                if (row.Table.Columns.Contains(col) && row[col] != DBNull.Value && row[col] != null)
                {
                    string str = row[col].ToString()?.Replace("'", "").Trim();
                    if (!string.IsNullOrWhiteSpace(str)) return str;
                }
            }

            foreach (var col in columnNames)
            {
                string normTarget = NormalizeKey(col);
                var matchCol = row.Table.Columns.Cast<DataColumn>().FirstOrDefault(c => NormalizeKey(c.ColumnName) == normTarget);
                if (matchCol != null && row[matchCol] != DBNull.Value && row[matchCol] != null)
                {
                    string str = row[matchCol].ToString()?.Replace("'", "").Trim();
                    if (!string.IsNullOrWhiteSpace(str)) return str;
                }
            }
            return "";
        }

        private static double GetDoubleVal(DataRow row, params string[] columnNames)
        {
            foreach (var col in columnNames)
            {
                if (row.Table.Columns.Contains(col) && row[col] != DBNull.Value && row[col] != null)
                {
                    string str = row[col].ToString()?.Trim();
                    if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double res)) return res;
                    if (double.TryParse(str, out double resLocal)) return resLocal;
                }
            }

            foreach (var col in columnNames)
            {
                string normTarget = NormalizeKey(col);
                var matchCol = row.Table.Columns.Cast<DataColumn>().FirstOrDefault(c => NormalizeKey(c.ColumnName) == normTarget);
                if (matchCol != null && row[matchCol] != DBNull.Value && row[matchCol] != null)
                {
                    string str = row[matchCol].ToString()?.Trim();
                    if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double res)) return res;
                    if (double.TryParse(str, out double resLocal)) return resLocal;
                }
            }
            return 0;
        }

        private static DateTime? GetDateTimeVal(DataRow row, params string[] columnNames)
        {
            foreach (var col in columnNames)
            {
                if (row.Table.Columns.Contains(col) && row[col] != DBNull.Value && row[col] != null)
                {
                    var val = row[col];
                    if (val is DateTime dt) return dt;
                    string str = val.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
                        if (DateTime.TryParse(str, out var d2)) return d2;
                        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl) && dbl > 30000 && dbl < 70000)
                        {
                            try { return DateTime.FromOADate(dbl); } catch { }
                        }
                    }
                }
            }

            foreach (var col in columnNames)
            {
                string normTarget = NormalizeKey(col);
                var matchCol = row.Table.Columns.Cast<DataColumn>().FirstOrDefault(c => NormalizeKey(c.ColumnName) == normTarget);
                if (matchCol != null && row[matchCol] != DBNull.Value && row[matchCol] != null)
                {
                    var val = row[matchCol];
                    if (val is DateTime dt) return dt;
                    string str = val.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
                        if (DateTime.TryParse(str, out var d2)) return d2;
                        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl) && dbl > 30000 && dbl < 70000)
                        {
                            try { return DateTime.FromOADate(dbl); } catch { }
                        }
                    }
                }
            }
            return null;
        }

        public static List<EmpleadosActivos> GetEmpActivosInss(List<EmpleadosActivos> _PayLoadListLocal, DataTable dataActivos)
        {
            var _PayLoadList = new List<EmpleadosActivos>();
            var ListaInssAct = new List<EmpleadosActivosInss>();

            if (dataActivos != null && dataActivos.Columns.Count > 0 && dataActivos.Columns[0].ColumnName == "IdNomina")
            {
                _PayLoadList = DataTableHelper.ConvertTo<EmpleadosActivos>(dataActivos);
                foreach (var emp in _PayLoadList)
                {
                    if (string.IsNullOrWhiteSpace(emp.PrimerNombre) && !string.IsNullOrWhiteSpace(emp.NombreCompleto))
                    {
                        var (pn, pa) = InssReporteHelper.SplitNombreCompleto(emp.NombreCompleto);
                        emp.PrimerNombre = pn;
                        emp.PrimerApellido = pa;
                    }
                }
            }
            else if (dataActivos != null && dataActivos.Rows.Count > 0)
            {
                ListaInssAct = (from myRow in dataActivos.AsEnumerable()
                                select new EmpleadosActivosInss()
                                {
                                    NoCedula = GetStringVal(myRow, "Cedula", "Cédula", "NoCedula", "Identificacion", "No_Cedula", "No. Cedula", "No. Cédula", "Num_Cedula"),
                                    NoInss = GetDoubleVal(myRow, "Nss", "NoInss", "NSS", "No_INSS", "No Inss", "No. Inss", "No. INSS", "Afiliacion", "Afiliación", "No_Afiliacion", "No. Afiliacion", "Asegurado", "Num_Afiliacion"),
                                    PrimerNombre = GetStringVal(myRow, "Primer_Nombre", "Primer Nombre", "PrimerNombre", "p-nombre", "p_nombre", "pnombre", "PNombre", "Nombre 1", "Nombre1"),
                                    PrimerApellido = GetStringVal(myRow, "Primer_Apellido", "Primer Apellido", "PrimerApellido", "p-apellido", "p_apellido", "papellido", "PApellido", "Apellido 1", "Apellido1"),
                                    SegundoNombre = GetStringVal(myRow, "Segundo_Nombre", "Segundo Nombre", "SegundoNombre", "s-nombre", "s_nombre", "snombre", "SNombre", "Nombre 2", "Nombre2"),
                                    SegundoApellido = GetStringVal(myRow, "Segundo_Apellido", "Segundo Apellido", "SegundoApellido", "s-apellido", "s_apellido", "sapellido", "SApellido", "Apellido 2", "Apellido2"),
                                    FechaIngreso = GetDateTimeVal(myRow, "Fecha de Ingreso", "Fecha_de_Ingreso", "FechaIngreso", "Fecha_Ingreso", "fec_ingreso", "fec_inicio", "Fecha Inicio", "Ingreso", "FecIngreso")
                                }).ToList();

                // Post-proceso para completar nombres si vinieron en una columna combinada
                foreach (var empInss in ListaInssAct)
                {
                    if (string.IsNullOrWhiteSpace(empInss.PrimerNombre) || string.IsNullOrWhiteSpace(empInss.PrimerApellido))
                    {
                        var row = dataActivos.AsEnumerable().FirstOrDefault(r => 
                            (!string.IsNullOrWhiteSpace(empInss.NoCedula) && GetStringVal(r, "Cedula", "NoCedula") == empInss.NoCedula) ||
                            (empInss.NoInss > 0 && GetDoubleVal(r, "Nss", "NoInss") == empInss.NoInss));

                        if (row != null)
                        {
                            string colNomComp = GetStringVal(row, "Nombre_Completo", "Nombre Completo", "NombreCompleto", "Nombres_y_Apellidos", "Nombres y Apellidos", "Asegurado", "Trabajador", "Empleado", "Nombre", "Nombres");
                            if (!string.IsNullOrWhiteSpace(colNomComp))
                            {
                                var (pn, pa) = InssReporteHelper.SplitNombreCompleto(colNomComp);
                                if (string.IsNullOrWhiteSpace(empInss.PrimerNombre)) empInss.PrimerNombre = pn;
                                if (string.IsNullOrWhiteSpace(empInss.PrimerApellido)) empInss.PrimerApellido = pa;
                            }
                        }
                    }
                }

                var querycedula = (from ea in _PayLoadListLocal
                                   where !string.IsNullOrWhiteSpace(ea.NoCedula)
                                   join ei in ListaInssAct.Where(x => !string.IsNullOrWhiteSpace(x.NoCedula))
                                        on ea.NoCedula.Trim().ToLower() equals ei.NoCedula.Trim().ToLower()
                                   select new EmpleadosActivos
                                   {
                                       IdNomina = ea.IdNomina,
                                       NoCedula = ea.NoCedula,
                                       NombreCompleto = ea.NombreCompleto,
                                       NombreCorto = $"{ei.PrimerNombre} {ei.PrimerApellido}".Trim(),
                                       PrimerNombre = ei.PrimerNombre,
                                       PrimerApellido = ei.PrimerApellido,
                                       SegundoNombre = ei.SegundoNombre,
                                       SegundoApellido = ei.SegundoApellido,
                                       NoInss = (ea.NoInss != ei.NoInss && ei.NoInss > 0) ? ei.NoInss : ea.NoInss,
                                       NoContrato = ea.NoContrato,
                                       FechaIngreso = ei.FechaIngreso ?? ea.FechaIngreso,
                                       ActivoInss = true,
                                       Accion = (ea.NoInss != ei.NoInss && ei.NoInss > 0) ? AccionesEmp.NoInss : ea.Accion,
                                   }).ToList();

                var querynoinss = (from ea in _PayLoadListLocal
                                   where ea.NoInss > 0
                                   join ei in ListaInssAct.Where(x => x.NoInss > 0)
                                        on ea.NoInss equals ei.NoInss
                                   select new EmpleadosActivos
                                   {
                                       IdNomina = ea.IdNomina,
                                       NoCedula = (!string.IsNullOrWhiteSpace(ea.NoCedula) && !string.IsNullOrWhiteSpace(ei.NoCedula) && ea.NoCedula.Trim().ToLower() != ei.NoCedula.Trim().ToLower()) ? ei.NoCedula : ea.NoCedula,
                                       NombreCompleto = ea.NombreCompleto,
                                       NombreCorto = $"{ei.PrimerNombre} {ei.PrimerApellido}".Trim(),
                                       PrimerNombre = ei.PrimerNombre,
                                       PrimerApellido = ei.PrimerApellido,
                                       SegundoNombre = ei.SegundoNombre,
                                       SegundoApellido = ei.SegundoApellido,
                                       NoInss = ea.NoInss,
                                       NoContrato = ea.NoContrato,
                                       FechaIngreso = ei.FechaIngreso ?? ea.FechaIngreso,
                                       ActivoInss = true,
                                       Accion = (!string.IsNullOrWhiteSpace(ea.NoCedula) && !string.IsNullOrWhiteSpace(ei.NoCedula) && ea.NoCedula.Trim().ToLower() != ei.NoCedula.Trim().ToLower()) ? AccionesEmp.Cedula : ea.Accion,
                                   }).ToList();

                var unionList = (from a in querycedula.Where(x => x.Accion != AccionesEmp.NoInss)
                                 join b in querynoinss.Where(x => x.Accion != AccionesEmp.Cedula) on a.NoInss equals b.NoInss
                                 select a).ToList();

                foreach (var item in querycedula.Where(x => x.Accion != AccionesEmp.Ninguna))
                    unionList.Add(item);

                foreach (var item in querynoinss.Where(x => x.Accion != AccionesEmp.Ninguna))
                    unionList.Add(item);

                var unionInssSet = unionList.Select(x => x.NoInss).Where(n => n > 0).ToHashSet();
                var filteredListadmin = ListaInssAct.Where(x => x.NoInss == 0 || !unionInssSet.Contains(x.NoInss)).ToList();

                var queryadmin = (from ei in filteredListadmin
                                  select new EmpleadosActivos
                                  {
                                      IdNomina = 0,
                                      NoCedula = ei.NoCedula,
                                      NombreCompleto = $"{ei.PrimerNombre} {ei.SegundoNombre} {ei.PrimerApellido} {ei.SegundoApellido}".Replace("  ", " ").Trim(),
                                      NombreCorto = $"{ei.PrimerNombre} {ei.PrimerApellido}".Trim(),
                                      PrimerNombre = ei.PrimerNombre,
                                      PrimerApellido = ei.PrimerApellido,
                                      SegundoNombre = ei.SegundoNombre,
                                      SegundoApellido = ei.SegundoApellido,
                                      NoInss = ei.NoInss,
                                      NoContrato = 0,
                                      FechaIngreso = ei.FechaIngreso,
                                      ActivoInss = true,
                                      Accion = AccionesEmp.NoActivoAdmin
                                  }).ToList();

                foreach (var item2 in queryadmin)
                {
                    item2.NombreCompleto = item2.NombreCompleto.Replace("'", "");
                    
                    var match = _PayLoadListLocal.FirstOrDefault(x => x.NoInss == item2.NoInss && x.NoInss > 0)
                                ?? _PayLoadListLocal.FirstOrDefault(x => !string.IsNullOrEmpty(x.NoCedula) && !string.IsNullOrEmpty(item2.NoCedula) && x.NoCedula.Trim().ToLower() == item2.NoCedula.Trim().ToLower())
                                ?? _PayLoadListLocal.FirstOrDefault(x => !string.IsNullOrEmpty(x.NombreCompleto) && !string.IsNullOrEmpty(item2.NombreCompleto) && x.NombreCompleto.Replace(" ", "").Trim().ToLower() == item2.NombreCompleto.Replace(" ", "").Trim().ToLower());

                    if (match != null)
                    {
                        item2.IdNomina = match.IdNomina;
                        if (string.IsNullOrWhiteSpace(item2.NombreCompleto))
                            item2.NombreCompleto = match.NombreCompleto;
                    }
                }

                foreach (var item in queryadmin)
                    unionList.Add(item);

                var unionCedulasSet = unionList.Select(u => u.NoCedula?.Trim().ToLower()).Where(c => !string.IsNullOrEmpty(c)).ToHashSet();
                var filteredListainss = _PayLoadListLocal.Where(x => string.IsNullOrEmpty(x.NoCedula) || !unionCedulasSet.Contains(x.NoCedula.Trim().ToLower())).ToList();

                var queryinss = (from ei in filteredListainss
                                 select new EmpleadosActivos
                                 {
                                     IdNomina = ei.IdNomina,
                                     NoCedula = ei.NoCedula,
                                     NombreCompleto = ei.NombreCompleto,
                                     NombreCorto = ei.NombreCorto,
                                     PrimerNombre = !string.IsNullOrWhiteSpace(ei.PrimerNombre) ? ei.PrimerNombre : InssReporteHelper.SplitNombreCompleto(ei.NombreCompleto).pNombre,
                                     PrimerApellido = !string.IsNullOrWhiteSpace(ei.PrimerApellido) ? ei.PrimerApellido : InssReporteHelper.SplitNombreCompleto(ei.NombreCompleto).pApellido,
                                     SegundoNombre = ei.SegundoNombre,
                                     SegundoApellido = ei.SegundoApellido,
                                     NoInss = ei.NoInss,
                                     NoContrato = ei.NoContrato,
                                     FechaIngreso = ei.FechaIngreso,
                                     ActivoInss = false,
                                     Accion = AccionesEmp.NoActivoInss
                                 }).ToList();

                foreach (var item in queryinss)
                    unionList.Add(item);

                _PayLoadList = unionList;
            }
            
            return _PayLoadList;
        }
    }
}
