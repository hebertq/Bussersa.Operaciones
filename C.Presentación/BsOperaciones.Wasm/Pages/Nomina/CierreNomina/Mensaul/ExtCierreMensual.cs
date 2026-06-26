using Modelo.Entidades.Entradas.Odoo;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.CierreNomina.Mensaul
{
    public static class ExtCierreMensual
    {
        public static List<EmpleadosActivos> GetEmpActivosInss(List<EmpleadosActivos> _PayLoadListLocal, DataTable dataActivos)
        {
            var _PayLoadList = new List<EmpleadosActivos>();
            var ListaInssAct = new List<EmpleadosActivosInss>();

            if (dataActivos.Columns[0].ColumnName == "IdNomina")
                _PayLoadList = DataTableHelper.ConvertTo<EmpleadosActivos>(dataActivos);
            else
            {
                ListaInssAct = (from myRow in dataActivos.AsEnumerable()
                                select new EmpleadosActivosInss()
                                {
                                    NoCedula = myRow.Field<string>("Cedula").Replace("'", ""),
                                    NoInss = myRow.Field<double>("Nss"),
                                    PrimerNombre = myRow.Field<string>("Primer_Nombre").Replace("'", ""),
                                    PrimerApellido = myRow.Field<string>("Primer_Apellido").Replace("'", ""),
                                    SegundoNombre = myRow.Field<string>("Segundo_Nombre"),
                                    SegundoApellido = myRow.Field<string>("Segundo_Apellido")
                                }).ToList();

                var querycedula = (from ea in _PayLoadListLocal
                                   join ei in ListaInssAct on ea.NoCedula.Trim().ToLower() equals ei.NoCedula.Trim().ToLower()
                                   select new EmpleadosActivos
                                   {
                                       IdNomina = ea.IdNomina,
                                       NoCedula = ea.NoCedula,
                                       NombreCompleto = ea.NombreCompleto,
                                       NombreCorto = $"{ei.PrimerNombre} {ei.PrimerApellido}",
                                       NoInss = ea.NoInss != ei.NoInss ? ei.NoInss : ea.NoInss,
                                       NoContrato = ea.NoContrato,
                                       ActivoInss = true,
                                       Accion = ea.NoInss != ei.NoInss ? AccionesEmp.NoInss : ea.Accion,
                                   }).ToList();

                var querynoinss = (from ea in _PayLoadListLocal
                                   join ei in ListaInssAct on ea.NoInss equals ei.NoInss
                                   select new EmpleadosActivos
                                   {
                                       IdNomina = ea.IdNomina,
                                       NoCedula = ea.NoCedula != ei.NoCedula ? ei.NoCedula : ea.NoCedula,
                                       NombreCompleto = ea.NombreCompleto,
                                       NombreCorto = $"{ei.PrimerNombre} {ei.PrimerApellido}",
                                       NoInss = ea.NoInss,
                                       NoContrato = ea.NoContrato,
                                       ActivoInss = true,
                                       Accion = ea.NoCedula != ei.NoCedula ? AccionesEmp.Cedula : ea.Accion,
                                   }).ToList();

                var unionList = (from a in querycedula.Where(x => x.Accion != AccionesEmp.NoInss)
                                 join b in querynoinss.Where(x => x.Accion != AccionesEmp.Cedula) on a.NoInss equals b.NoInss
                                 select a).ToList();

                foreach (var item in querycedula.Where(x => x.Accion != AccionesEmp.Ninguna))
                    unionList.Add(item);

                foreach (var item in querynoinss.Where(x => x.Accion != AccionesEmp.Ninguna))
                    unionList.Add(item);

                var matchadmin = ListaInssAct.Join(unionList, x => x.NoInss, y => y.NoInss, (x, y) => new { NoInss = x.NoInss });
                var filteredListadmin = ListaInssAct.Where(x => !matchadmin.Contains(new { NoInss = x.NoInss })).ToList();


                var queryadmin = (from ei in filteredListadmin
                                  select new EmpleadosActivos
                                  {
                                      IdNomina = 0,
                                      NoCedula = ei.NoCedula,
                                      NombreCompleto = $"{ei.PrimerNombre} {ei.SegundoNombre} {ei.PrimerApellido} {ei.SegundoApellido}",
                                      NombreCorto = $"{ei.PrimerNombre} {ei.PrimerApellido}",
                                      NoInss = ei.NoInss,
                                      NoContrato = 0,
                                      ActivoInss = true,
                                      Accion = AccionesEmp.NoActivoAdmin
                                  }).ToList();

                foreach (var item2 in queryadmin)
                {
                    item2.NombreCompleto = item2.NombreCompleto.Replace("'", "");
                    foreach (var item in _PayLoadListLocal.Where(x => x.NoInss.Equals(item2.NoInss)))
                    {
                        item2.IdNomina = item.IdNomina;
                    }
                }


                foreach (var item in queryadmin)
                    unionList.Add(item);

                var matchinss = _PayLoadListLocal.Join(unionList, x => x.NoCedula, y => y.NoCedula, (x, y) => new { NoCedula = x.NoCedula });
                var filteredListainss = _PayLoadListLocal.Where(x => !matchinss.Contains(new { NoCedula = x.NoCedula })).ToList();


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
