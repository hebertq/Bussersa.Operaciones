using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Report;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.CierreNomina.Mensaul
{
    public partial class ValidacionFinalCierre : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }

        [Parameter] public List<EmpleadosActivos> ListaInss { get; set; } = new();
        [Parameter] public List<NominaMensualReportar> ListaNomina { get; set; } = new();

        protected string _searchString = "";
        protected bool isloading { get; set; } = false;

        // 1. Empleados que están en el padrón/activos de INSS pero no en la nómina calculada
        protected List<EmpleadosActivos> sinNomina => (ListaInss != null && ListaNomina != null && ListaNomina.Any())
            ? ListaInss
                .Where(i => i != null && i.ActivoInss == true && !ListaNomina.Any(n => 
                    (i.IdNomina > 0 && n.emp_nomina == i.IdNomina) ||
                    (i.NoInss > 0 && InssReporteHelper.CleanInss(n.emp_noinss) == InssReporteHelper.CleanInss(i.NoInss.ToString()))))
                .ToList() 
            : new();

        // 2. Empleados que están en la nómina pero tienen INSS en 0 (siendo que están marcados como activos)
        protected List<NominaMensualReportar> sinCobroInss => (ListaInss != null && ListaInss.Any() && ListaNomina != null && ListaNomina.Any())
            ? ListaNomina
                .Where(n => (n.emp_deduc_inss == 0 || n.emp_deduc_inss == null) &&
                             ListaInss.Any(i => i != null && i.ActivoInss == true &&
                                ((i.IdNomina > 0 && i.IdNomina == n.emp_nomina) ||
                                 (i.NoInss > 0 && InssReporteHelper.CleanInss(n.emp_noinss) == InssReporteHelper.CleanInss(i.NoInss.ToString())))))
                .ToList()
            : new();

        protected List<AuditoriaInssModel> ObtenerListaConsolidada()
        {
            var consolidado = new List<AuditoriaInssModel>();

            // Agregar los que no tienen nómina
            consolidado.AddRange(sinNomina.Select(x => new AuditoriaInssModel
            {
                NoInss = InssReporteHelper.CleanInss(x.NoInss.ToString()),
                Cedula = x.NoCedula ?? "",
                Nombre = !string.IsNullOrWhiteSpace(x.NombreCompleto) 
                    ? x.NombreCompleto 
                    : (!string.IsNullOrWhiteSpace(x.NombreCorto) 
                        ? x.NombreCorto 
                        : $"Asegurado No. {InssReporteHelper.CleanInss(x.NoInss.ToString())}"),
                ErrorTipo = "SIN PAGO",
                EstadoNomina = "No generado",
                Observacion = x.IdNomina > 0 
                    ? "Asegurado activo en INSS pero no generó recibo de nómina en el período." 
                    : "Asegurado activo en INSS pero no vinculado a empleado en Odoo."
            }));

            // Agregar los que tienen nómina pero INSS 0
            consolidado.AddRange(sinCobroInss.Select(x => {
                var inssMatch = ListaInss.FirstOrDefault(i => 
                    (i.IdNomina > 0 && i.IdNomina == x.emp_nomina) ||
                    (i.NoInss > 0 && InssReporteHelper.CleanInss(x.emp_noinss) == InssReporteHelper.CleanInss(i.NoInss.ToString())));
                return new AuditoriaInssModel
                {
                    NoInss = InssReporteHelper.CleanInss(x.emp_noinss),
                    Cedula = inssMatch?.NoCedula ?? "",
                    Nombre = x.emp_nombre,
                    ErrorTipo = "INSS EN CERO",
                    EstadoNomina = "Deducción omitida",
                    Observacion = "Registrado en nómina pero con aporte INSS de C$ 0.00."
                };
            }));

            return consolidado;
        }

        protected IEnumerable<AuditoriaInssModel> ListaFiltrada
        {
            get
            {
                var list = ObtenerListaConsolidada();
                if (string.IsNullOrWhiteSpace(_searchString)) return list;
                return list.Where(x =>
                    (x.Nombre != null && x.Nombre.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (x.NoInss != null && x.NoInss.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Cedula != null && x.Cedula.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (x.ErrorTipo != null && x.ErrorTipo.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Observacion != null && x.Observacion.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                );
            }
        }

        protected async Task DescargarAuditoria()
        {
            var data = ObtenerListaConsolidada();
            if (!data.Any()) return;
            isloading = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                var excelData = DataExcel.CreateExcel(data, "Errores_INSS");
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", Convert.ToBase64String(excelData.Data), "Auditoria_Critica_INSS.xlsx");
            }
            finally
            {
                isloading = false;
            }
        }

        public class AuditoriaInssModel
        {
            public string NoInss { get; set; } = string.Empty;
            public string Cedula { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string ErrorTipo { get; set; } = string.Empty;
            public string EstadoNomina { get; set; } = string.Empty;
            public string Observacion { get; set; } = string.Empty;
        }
    }
}
