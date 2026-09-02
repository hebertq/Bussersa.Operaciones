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
            return 0;
        }

        public static List<EmpleadosActivos> GetEmpActivosInss(List<EmpleadosActivos> _PayLoadListLocal, DataTable dataActivos)
        {
            var _PayLoadList = new List<EmpleadosActivos>();
            var ListaInssAct = new List<EmpleadosActivosInss>();

            if (dataActivos != null && dataActivos.Columns.Count > 0 && dataActivos.Columns[0].ColumnName == "IdNomina")
            {
                _PayLoadList = DataTableHelper.ConvertTo<EmpleadosActivos>(dataActivos);
            }
            else if (dataActivos != null && dataActivos.Rows.Count > 0)
            {
                ListaInssAct = (from myRow in dataActivos.AsEnumerable()
                                select new EmpleadosActivosInss()
                                {
                                    NoCedula = GetStringVal(myRow, "Cedula", "Cédula", "NoCedula"),
                                    NoInss = GetDoubleVal(myRow, "Nss", "NoInss", "NSS", "No_INSS"),
                                    PrimerNombre = GetStringVal(myRow, "Primer_Nombre", "PrimerNombre", "Nombre1"),
                                    PrimerApellido = GetStringVal(myRow, "Primer_Apellido", "PrimerApellido", "Apellido1"),
                                    SegundoNombre = GetStringVal(myRow, "Segundo_Nombre", "SegundoNombre", "Nombre2"),
                                    SegundoApellido = GetStringVal(myRow, "Segundo_Apellido", "SegundoApellido", "Apellido2")
                                }).ToList();

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
                                       NoInss = (ea.NoInss != ei.NoInss && ei.NoInss > 0) ? ei.NoInss : ea.NoInss,
                                       NoContrato = ea.NoContrato,
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
                                       NoInss = ea.NoInss,
                                       NoContrato = ea.NoContrato,
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
                                      NoInss = ei.NoInss,
                                      NoContrato = 0,
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
                                     NoInss = ei.NoInss,
                                     NoContrato = ei.NoContrato,
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
