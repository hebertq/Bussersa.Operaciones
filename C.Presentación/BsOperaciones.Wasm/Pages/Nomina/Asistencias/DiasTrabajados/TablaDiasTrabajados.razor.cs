using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Modelo.Entidades.Entradas.Odoo;
using Utilidades.Interfaces;
using Utilidades.ClasesGenericas;
using Modelo.ClasesGenericas;
using OdooDiasTrabajados = Modelo.Entidades.Entradas.Odoo.DiasTrabajados;

namespace BsOperaciones.Pages.Nomina.Asistencias.DiasTrabajados
{
    public partial class TablaDiasTrabajados : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }

        [Parameter] public EventCallback<OdooDiasTrabajados> OnUpdate { get; set; }
        [Parameter] public EventCallback<OdooDiasTrabajados> OnDelete { get; set; }
        [Parameter] public EventCallback<OdooDiasTrabajados> OnCreate { get; set; }
        [Parameter] public string OperacionName { get; set; }
        [Parameter] public IList<DiasxempleadosOpera?> _PayLoadList { get; set; }
        [Parameter] public bool BotonEnabled { get; set; }

        public bool isloaddata { set; get; } = false;

        // --- MÉTODOS DE COMUNICACIÓN CON EL PADRE ---

        protected async Task OnUpdateChange(OdooDiasTrabajados payload) => await OnUpdate.InvokeAsync(payload);

        protected async Task OnCreateChange(DiasxempleadosOpera master)
        {
            var nuevo = new OdooDiasTrabajados { id = master.id, fecha = DateTime.Now };
            await OnCreate.InvokeAsync(nuevo);
        }

        protected async Task OnDeleteChange(OdooDiasTrabajados payload) => await OnDelete.InvokeAsync(payload);

        // --- LÓGICA DE EXPORTACIÓN (MANTENIENDO EL MAPEO ORIGINAL) ---

        protected async Task DownloadDetailFile(DiasxempleadosOpera? master)
        {
            if (master?.detalleall == null || !master.detalleall.Any())
            {
                Snackbar.Add("No hay detalles para exportar", Severity.Warning);
                return;
            }

            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                var datosExcel = master.detalleall.Select(x => new
                {
                    x.fecha,
                    x.id,
                    x.nombre,
                    x.entrada,
                    x.salida,
                    x.comida,
                    x.dias,
                    x.horasextras,
                    x.bono
                }).ToList();

                var exceldata = DataExcel.CreateExcel(datosExcel, $"Detalle_{master.id}");
                string base64Data = Convert.ToBase64String(exceldata.Data);
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, $"Detalle_Asistencia_{master.id}.xlsx");
            }
            catch (Exception ex) { Snackbar.Add("Error: " + ex.Message, Severity.Error); }
            finally { isloaddata = false; }
        }

        protected async Task DownloadFile()
        {
            if (_PayLoadList == null || !_PayLoadList.Any()) return;
            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                // 1. Crear el Consolidado
                var consolidadoList = _PayLoadList.Select(x => new
                {
                    x.area,
                    x.tipoempleado,
                    x.id,
                    x.nombre,
                    x.dias,
                    x.horasextras,
                    x.bono
                }).ToList();

                // 2. Crear el Detalle aplanando todos los detalles de todos los empleados
                var detalleList = _PayLoadList
                    .Where(x => x != null && x.detalleall != null)
                    .SelectMany(x => x.detalleall.Select(d => new
                    {
                        d.fecha,
                        d.id,
                        d.nombre,
                        d.entrada,
                        d.salida,
                        d.comida,
                        d.dias,
                        d.horasextras,
                        d.bono
                    }))
                    .ToList();

                // 3. Generar hojas de excel individuales
                var consolidadoExcel = DataExcel.CreateExcel(consolidadoList, "Consolidado");
                var detalleExcel = DataExcel.CreateExcel(detalleList, "Detalle");

                // 4. Unir ambas hojas en un único archivo base64
                var listSheets = new List<ExcelArray> { consolidadoExcel, detalleExcel };
                string base64Data = DataExcel.CreateExcel(listSheets);

                // 5. Descargar el archivo resultante
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, $"Consolidado_y_Detalle_{OperacionName?.Replace(" ", "_")}.xlsx");
            }
            catch (Exception ex) { Snackbar.Add("Error: " + ex.Message, Severity.Error); }
            finally { isloaddata = false; }
        }
    }
}
