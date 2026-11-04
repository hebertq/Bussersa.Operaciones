using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Command;
using Modelo.Report;
using Utilidades.ClasesGenericas;
using Modelo.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.CierreNomina.Mensaul
{
    public partial class ConciliacionMontos : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected HostService.Interfaces.IOdooService OdooService { get; set; }

        [Parameter] public int Anio { get; set; }
        [Parameter] public EventCallback<int> AnioChanged { get; set; }

        [Parameter] public int Mes { get; set; }
        [Parameter] public EventCallback<int> MesChanged { get; set; }

        [Parameter] public List<NominaMensualReportar> NominaFinalGlobal { get; set; }
        [Parameter] public EventCallback<List<NominaMensualReportar>> OnNominaGenerated { get; set; }
        [Parameter] public EventCallback<bool> OnProcessing { get; set; }

        protected int _anioSeleccionado
        {
            get => Anio;
            set { Anio = value; AnioChanged.InvokeAsync(value); }
        }
        protected int _mesSeleccionado
        {
            get => Mes;
            set { Mes = value; MesChanged.InvokeAsync(value); }
        }

        protected List<NominaMensualReportar> _nominaFinal => NominaFinalGlobal;

        protected int activeTabIndex = 0;
        protected void SetActiveTab(int index)
        {
            activeTabIndex = index;
            StateHasChanged();
        }

        protected async Task ConfirmarEjecucionCierre()
        {
            string nombreMes = new DateTime(2000, _mesSeleccionado, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES")).ToUpper();
            bool? result = await DialogService.ShowMessageBox(
                "Confirmar Cierre Mensual",
                (MarkupString)$"¿Ejecutar el cierre definitivo de nómina para el período: <b>{nombreMes} {_anioSeleccionado}</b>?<br/><br/><span class='text-muted'>Parámetros enviados al API: Año={_anioSeleccionado}, Mes={_mesSeleccionado}</span>",
                yesText: "Ejecutar Cierre", cancelText: "Cancelar");

            if (result == true) await EjecutarCierreMensual();
        }

        private async Task EjecutarCierreMensual()
        {
            await OnProcessing.InvokeAsync(true);
            try
            {
                var response = await _mediator.Send(new CerrarMesNominaCommand(_anioSeleccionado, _mesSeleccionado));
                if (!response.Respuesta.ExisteError)
                {
                    var resultado = response.Model ?? new();
                    await OnNominaGenerated.InvokeAsync(resultado); // Notifica al padre
                    Snackbar.Add("Cierre finalizado con éxito.", Severity.Success);
                }
                else Snackbar.Add(response.Respuesta.MensajeError, Severity.Error);
            }
            finally
            {
                await OnProcessing.InvokeAsync(false);
            }
        }

        protected async Task DescargarNominaInss()
        {
            await OnProcessing.InvokeAsync(true);
            await Task.Delay(50);
            try
            {
                var reporteExcel = _nominaFinal.Select(x => new
                {
                    Mes = x.anyo_mes_cierre,
                    Inicio = x.fec_inicio?.ToString("yyyy-MM-dd"),
                    Fin = x.fec_fin?.ToString("yyyy-MM-dd"),
                    EmpId = x.emp_nomina,
                    No_Inss = decimal.TryParse(x.emp_noinss, out decimal vInss) ? Convert.ToInt64(vInss) : 0,
                    Nombre = x.emp_nombre,
                    Novedad = (x.emp_diaslab ?? 0) > 28 ? "03" : "04",
                    Semanas = InssReporteHelper.CalcularSemanasInss(x.fec_inicio, x.fec_fin, x.emp_diaslab ?? 0, _anioSeleccionado, _mesSeleccionado),
                    DiasLab = x.emp_diaslab,
                    ExtrasLab = x.emp_no_hras_extra,
                    SalBase = x.emp_sal_fijo,
                    SalBasico = x.emp_sal_basico,
                    HrasExtras = x.emp_pago_hras_extras,
                    Trasnporte = x.emp_viatico_trasporte,
                    Alimnetacion = x.emp_viatico_alimentacion,
                    Combustible = x.emp_viatico_combustible,
                    Depreciacion = x.emb_depre_vehiculo,
                    Vacaciones = x.emp_vacaciones,
                    Aguinaldo = x.emp_aguinaldo,
                    Prestaciones = x.emp_prestaciones,
                    Otros_Ingresos = x.emp_otros_ingresos,
                    TotalIngresos = x.emp_total_ingresos,
                    Deducibles = x.emp_ingresos_deducir,
                    Inss = x.emp_deduc_inss,
                    Ir = x.emp_ir_reportar,
                    Otras_Deduciones = x.emp_otras_deduciones,
                    Prestamos = x.emp_prestamos,
                    TotalDeduciones = x.emp_total_deducciones,
                    TotalRecibir = x.emp_pago_recibir
                }).ToList();

                var request = new MultiSheetExcelRequest
                {
                    Hojas = new List<ExcelRequest>
                    {
                        new ExcelRequest { Hoja = "Nomina_Cierre_INSS", Datos = Modelo.Validaciones.Util.ToDictionaryList(reporteExcel), IncludeHeader = true }
                    }
                };

                var response = await OdooService.GenerateExcel(request);
                if (!response.Respuesta.ExisteError && response.Model != null)
                {
                    await JS.InvokeVoidAsync("downloadFile", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Model.File, $"PLANILLA_INSS_{_mesSeleccionado}_{_anioSeleccionado}.xlsx");
                    Snackbar.Add("Excel generado con éxito", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar Excel: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al generar el Excel: " + ex.Message, Severity.Error);
            }       
            finally
            {
                await OnProcessing.InvokeAsync(false);
            }
        }

        protected async Task DescargarInssCsv()
        {
            if (_nominaFinal == null || !_nominaFinal.Any()) return;
            await OnProcessing.InvokeAsync(true);
            await Task.Delay(50);
            try
            {
                string csvContent = InssReporteHelper.GenerarCsvInss(_nominaFinal, _anioSeleccionado, _mesSeleccionado);
                string base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvContent));
                await JS.InvokeVoidAsync("downloadFile", "text/csv;charset=utf-8;", base64Data, $"NOVEDADES_INSS_{_mesSeleccionado}_{_anioSeleccionado}.csv");
                Snackbar.Add("Archivo CSV para INSS generado exitosamente.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al generar CSV INSS: " + ex.Message, Severity.Error);
            }
            finally
            {
                await OnProcessing.InvokeAsync(false);
            }
        }

        protected async Task DescargarInssExcel()
        {
            if (_nominaFinal == null || !_nominaFinal.Any()) return;
            await OnProcessing.InvokeAsync(true);
            await Task.Delay(50);
            try
            {
                byte[] excelBytes = InssReporteHelper.GenerarExcelInssBytes(_nominaFinal, _anioSeleccionado, _mesSeleccionado);
                string base64Data = Convert.ToBase64String(excelBytes);
                await JS.InvokeVoidAsync("downloadFile", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64Data, $"NOVEDADES_INSS_{_mesSeleccionado}_{_anioSeleccionado}.xlsx");
                Snackbar.Add("Plantilla Excel para INSS generada exitosamente.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al generar Excel INSS: " + ex.Message, Severity.Error);
            }
            finally
            {
                await OnProcessing.InvokeAsync(false);
            }
        }
    }
}