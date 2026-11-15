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
                var datosExcel = master.detalleall
                    .OrderBy(x => x.fecha)
                    .ThenBy(x => x.entrada)
                    .Select(x => new
                {
                    Semana = x.semana,
                    Fecha = x.fecha,
                    ID = x.id,
                    Nombre = x.nombre,
                    Entrada = x.entrada,
                    Salida = x.salida,
                    Comida = x.comida,
                    HorasDia = x.horas_dia,
                    Dias = x.dias,
                    HorasExtras = x.horasextras,
                    Bono = x.bono,
                    Observacion = x.observacion ?? string.Empty
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
                    .OrderBy(x => x!.id)
                    .SelectMany(x => x!.detalleall
                        .OrderBy(d => d.fecha)
                        .ThenBy(d => d.entrada)
                        .Select(d => new
                    {
                        Semana = d.semana,
                        Fecha = d.fecha,
                        ID = d.id,
                        Nombre = d.nombre,
                        Entrada = d.entrada,
                        Salida = d.salida,
                        Comida = d.comida,
                        HorasDia = d.horas_dia,
                        Dias = d.dias,
                        HorasExtras = d.horasextras,
                        Bono = d.bono,
                        Observacion = d.observacion ?? string.Empty
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
