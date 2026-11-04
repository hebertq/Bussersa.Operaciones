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
using Modelo.Entidades.Entradas.Odoo;

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
        [Parameter] public List<EmpleadosActivos>? ListaActivos { get; set; }
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
        private string _searchString = "";

        private Func<NominaMensualReportar, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString))
                return true;

            if (x.emp_nombre != null && x.emp_nombre.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                return true;

            if (x.emp_noinss != null && x.emp_noinss.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        };

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
                    await OnNominaGenerated.InvokeAsync(resultado);
                    Snackbar.Add("Cierre de nómina finalizado con éxito.", Severity.Success);
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
            if (_nominaFinal == null || !_nominaFinal.Any()) return;
            await OnProcessing.InvokeAsync(true);
            await Task.Delay(50);
            try
            {
                var reporteExcel = _nominaFinal.Select(x =>
                {
                    decimal salBase = x.emp_sal_fijo ?? 0m;
                    decimal salBasico = x.emp_sal_basico ?? 0m;
                    decimal horaExtraTotal = x.emp_pago_hras_extras ?? 0m;
                    decimal cantHorasExtra = x.emp_no_hras_extra ?? 0m;

                    decimal hrasExtrasRep = 0m;
                    decimal extrasLabRep = 0m;

                    // Si el básico no completa el salario fijo pactado en contrato:
                    // Se reporta en horas extras únicamente la cantidad requerida para completar el básico hasta el salario fijo
                    if (salBasico < salBase)
                    {
                        decimal faltante = salBase - salBasico;
                        if (horaExtraTotal > 0m && faltante > 0m)
                        {
                            hrasExtrasRep = Math.Min(horaExtraTotal, faltante);
                            if (cantHorasExtra > 0m)
                            {
                                extrasLabRep = Math.Round((cantHorasExtra * hrasExtrasRep) / horaExtraTotal, 2);
                            }
                        }
                    }

                    // El excedente de horas extras devengadas se redistribuye en 3 partes iguales (centavo a centavo)
                    // entre Transporte, Alimentación y Depreciación para blindar legalmente a la empresa contra multas del MITRAB e INSS
                    decimal excesoExtras = Math.Max(0m, horaExtraTotal - hrasExtrasRep);
                    decimal p1 = Math.Round(excesoExtras / 3.0m, 2);
                    decimal p2 = Math.Round(excesoExtras / 3.0m, 2);
                    decimal p3 = excesoExtras - (p1 + p2);

                    decimal transporteFinal = (x.emp_viatico_trasporte ?? 0m) + p1;
                    decimal alimentacionFinal = (x.emp_viatico_alimentacion ?? 0m) + p2;
                    decimal depreciacionFinal = (x.emb_depre_vehiculo ?? 0m) + p3;

                    // Base deducible sujeta a cotización del INSS (salario fijo completado o base contractual)
                    decimal deduciblesFinal = (x.emp_ingresos_deducir.HasValue && x.emp_ingresos_deducir.Value > 0m)
                        ? x.emp_ingresos_deducir.Value
                        : (salBasico + hrasExtrasRep);

                    DateTime? fecInicioEfectivo = x.fec_inicio;
                    if (ListaActivos != null && ListaActivos.Any())
                    {
                        string cleanNss = InssReporteHelper.CleanInss(x.emp_noinss);
                        var matchActivo = ListaActivos.FirstOrDefault(a => 
                            (a.NoInss > 0 && InssReporteHelper.CleanInss(a.NoInss.ToString(System.Globalization.CultureInfo.InvariantCulture)) == cleanNss)
                            || (a.IdNomina > 0 && a.IdNomina == x.emp_nomina));

                        if (matchActivo?.FechaIngreso.HasValue == true)
                        {
                            if (matchActivo.FechaIngreso.Value.Year == _anioSeleccionado && matchActivo.FechaIngreso.Value.Month == _mesSeleccionado)
                            {
                                fecInicioEfectivo = matchActivo.FechaIngreso.Value;
                            }
                        }
                    }

                    string semanasCalculadas = InssReporteHelper.CalcularSemanasInss(fecInicioEfectivo, x.fec_fin, x.emp_diaslab ?? 0, _anioSeleccionado, _mesSeleccionado);

                    return new
                    {
                        Mes = x.anyo_mes_cierre,
                        Inicio = fecInicioEfectivo?.ToString("yyyy-MM-dd"),
                        Fin = x.fec_fin?.ToString("yyyy-MM-dd"),
                        EmpId = x.emp_nomina,
                        No_Inss = decimal.TryParse(x.emp_noinss, out decimal vInss) ? Convert.ToInt64(vInss) : 0,
                        Nombre = x.emp_nombre,
                        Novedad = (x.emp_diaslab ?? 0) > 28 ? "03" : "04",
                        Semanas = semanasCalculadas,
                        DiasLab = x.emp_diaslab,
                        ExtrasLab = extrasLabRep,
                        SalBase = x.emp_sal_fijo,
                        SalBasico = x.emp_sal_basico,
                        HrasExtras = hrasExtrasRep,
                        Trasnporte = transporteFinal,
                        Alimnetacion = alimentacionFinal,
                        Combustible = x.emp_viatico_combustible,
                        Depreciacion = depreciacionFinal,
                        Vacaciones = x.emp_vacaciones,
                        Aguinaldo = x.emp_aguinaldo,
                        Prestaciones = x.emp_prestaciones,
                        Otros_Ingresos = x.emp_otros_ingresos,
                        TotalIngresos = x.emp_total_ingresos,
                        Deducibles = deduciblesFinal,
                        Inss = x.emp_deduc_inss,
                        Ir = x.emp_ir_reportar,
                        Otras_Deduciones = x.emp_otras_deduciones,
                        Prestamos = x.emp_prestamos,
                        TotalDeduciones = x.emp_total_deducciones,
                        TotalRecibir = x.emp_pago_recibir
                    };
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
    }
}
