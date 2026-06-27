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

        // 1. Empleados que están en el Excel de INSS pero no en la nómina calculada
        protected List<EmpleadosActivos> sinNomina => ListaInss?
            .Where(i => i.ActivoInss == true && !ListaNomina.Any(n => n.emp_nomina == i.IdNomina))
            .ToList() ?? new();

        // 2. Empleados que están en la nómina pero tienen INSS en 0 (siendo que están marcados como activos)
        protected List<NominaMensualReportar> sinCobroInss => ListaNomina?
            .Where(n => (n.emp_deduc_inss == 0 || n.emp_deduc_inss == null) &&
                         ListaInss.Any(i => i.IdNomina == n.emp_nomina && i.ActivoInss == true))
            .ToList() ?? new();

        protected List<AuditoriaInssModel> ObtenerListaConsolidada()
        {
            var consolidado = new List<AuditoriaInssModel>();

            // Agregar los que no tienen nómina
            consolidado.AddRange(sinNomina.Select(x => new AuditoriaInssModel
            {
                NoInss = x.NoInss.ToString(),
                Nombre = x.NombreCompleto,
                ErrorTipo = "SIN PAGO",
                EstadoNomina = "No generado"
            }));

            // Agregar los que tienen nómina pero INSS 0
            consolidado.AddRange(sinCobroInss.Select(x => new AuditoriaInssModel
            {
                NoInss = x.emp_noinss,
                Nombre = x.emp_nombre,
                ErrorTipo = "INSS EN CERO",
                EstadoNomina = "Deducción omitida"
            }));

            return consolidado;
        }

        protected bool isloading { get; set; } = false;

        protected async Task DescargarAuditoria()
        {
            var data = ObtenerListaConsolidada();
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
            public string NoInss { get; set; }
            public string Nombre { get; set; }
            public string ErrorTipo { get; set; }
            public string EstadoNomina { get; set; }
        }
    }
}
