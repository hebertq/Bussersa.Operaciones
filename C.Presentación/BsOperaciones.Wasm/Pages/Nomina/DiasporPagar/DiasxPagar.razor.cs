using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using Utilidades.Interfaces;
using Utilidades.ClasesGenericas;
using HostService.Interfaces;

namespace BsOperaciones.Pages.Nomina.DiasporPagar
{
    public partial class DiasxPagar : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected IUserInfo _Iuser { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IOdooService OdooService { get; set; }

        public List<diasxpagarperiodo> PayLoadList { get; set; } = new();
        public List<Combos> PayLoadOper { get; set; } = new();

        public DateTime FchaDesde { set; get; } = DateTime.Now.AddDays(-15);
        public DateTime FchaHasta { set; get; } = DateTime.Now;

        // Wrappers para MudDatePicker
        protected DateTime? _fchaDesdeWrapper { get => FchaDesde; set => FchaDesde = value ?? DateTime.Now.AddDays(-15); }
        protected DateTime? _fchaHastaWrapper { get => FchaHasta; set => FchaHasta = value ?? DateTime.Now; }

        public int operacion { set; get; } = 0;
        public bool isloaddata { set; get; } = false;
        protected string _searchString = "";

        protected override async Task OnInitializedAsync()
        {
            isloaddata = true;
            try
            {
                var regop = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
                PayLoadOper = regop.Model.Where(x => x.id > 0).ToList();
            }
            catch (Exception ex) { Snackbar.Add("Error al cargar operaciones: " + ex.Message, Severity.Error); }
            finally { isloaddata = false; }
        }

        protected async Task OnChangeCliente(int value)
        {
            operacion = value;
            if (operacion > 0)
            {
                await GetAllMarcadas();
            }
        }

        private async Task GetAllMarcadas()
        {
            if (operacion == 0)
            {
                Snackbar.Add("Seleccione una operación primero.", Severity.Warning);
                return;
            }

            isloaddata = true;
            try
            {
                typeeinout rango = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = operacion };
                var registros = await _mediator.Send(new GetAllDiasxPagarQuery(rango));

                PayLoadList = registros.Model;

                if (!PayLoadList.Any())
                    Snackbar.Add("No se encontraron registros en este rango de fechas.", Severity.Info);
                else
                    Snackbar.Add($"{PayLoadList.Count} registros cargados correctamente.", Severity.Success);
            }
            catch (Exception ex) { Snackbar.Add("Error al consultar datos: " + ex.Message, Severity.Error); }
            finally { isloaddata = false; StateHasChanged(); }
        }

        protected async Task DownloadFile()
        {
            if (!PayLoadList.Any()) return;

            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                var datosParaExcel = PayLoadList.Select(x => new
                {
                    x.area,
                    x.tipoempleado,
                    x.id,
                    x.nombre,
                    x.dias_habiles,
                    x.diastrabajados,
                    x.diasferiados,
                    x.aguinaldo,
                    x.vacdes,
                    x.vacpag,
                    x.subsidios,
                    x.justificados,
                    x.injustificados,
                    x.cuarentena,
                    x.suspension,
                    x.septimo,
                    x.totaldias,
                    x.hexpagar,
                    x.bono,
                    x.otros_ingresos,
                    x.otras_deducciones
                }).ToList();

                typeeinout rango = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = operacion };
                var detailsResult = await _mediator.Send(new GetAllDiasTrabajadosOperacionQuery(rango));
                var detailsList = new List<object>();

                if (detailsResult?.Model != null)
                {
                    foreach (var emp in detailsResult.Model)
                    {
                        if (emp?.detalleall == null) continue;
                        foreach (var mark in emp.detalleall)
                        {
                            detailsList.Add(new
                            {
                                Empleado_ID = emp.id,
                                Nombre = emp.nombre,
                                Fecha = mark.fecha.ToString("yyyy-MM-dd"),
                                Entrada = mark.entrada,
                                Salida = mark.salida,
                                Comida = mark.comida,
                                Dias = mark.dias,
                                HorasExtras = mark.horasextras,
                                Bono = mark.bono
                            });
                        }
                    }
                }

                var request = new MultiSheetExcelRequest
                {
                    Hojas = new List<ExcelRequest>
                    {
                        new ExcelRequest { Hoja = "Desgloce", Datos = Modelo.Validaciones.Util.ToDictionaryList(datosParaExcel), IncludeHeader = false },
                        new ExcelRequest { Hoja = "Detalle Marcaciones", Datos = Modelo.Validaciones.Util.ToDictionaryList(detailsList), IncludeHeader = false }
                    }
                };

                var response = await OdooService.GenerateExcel(request);
                if (!response.Respuesta.ExisteError && response.Model != null)
                {
                    string base64Data = response.Model.File;
                    string cliente = PayLoadOper.FirstOrDefault(x => x.id == operacion)?.nombre.Replace(" ", "_") ?? "General";
                    string nombreArchivo = $"Nomina_{cliente}_{DateTime.Now:ddMMyy}.xlsx";

                    await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, nombreArchivo);
                    Snackbar.Add("Excel generado exitosamente.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar Excel: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al exportar: " + ex.Message, Severity.Error);
            }
            finally { isloaddata = false; }
        }

        protected Func<diasxpagarperiodo, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            if (x.id.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
            if (x.nombre.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
            if (x.area?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) return true;
            return false;
        };
    }
}
