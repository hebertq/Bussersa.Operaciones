using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using Utilidades.Interfaces;
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

        public IEnumerable<int> _selectedOperaciones { set; get; } = new HashSet<int>();
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

        protected async Task OnSelectedOperacionesChanged(IEnumerable<int> values)
        {
            _selectedOperaciones = values ?? new HashSet<int>();
            await GetAllMarcadas();
        }

        protected async Task OnChangeCliente(int value)
        {
            if (value > 0)
            {
                _selectedOperaciones = new HashSet<int> { value };
                await GetAllMarcadas();
            }
        }

        private async Task GetAllMarcadas()
        {
            if (!_selectedOperaciones.Any())
            {
                PayLoadList.Clear();
                StateHasChanged();
                return;
            }

            isloaddata = true;
            try
            {
                var masterList = new List<diasxpagarperiodo>();
                var opList = _selectedOperaciones.Where(x => x > 0).ToList();

                foreach (var opId in opList)
                {
                    typeeinout rango = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = opId };
                    var registros = await _mediator.Send(new GetAllDiasxPagarQuery(rango));

                    if (registros?.Model != null && registros.Model.Any())
                    {
                        string opNombre = PayLoadOper.FirstOrDefault(x => x.id == opId)?.nombre ?? "";
                        foreach (var item in registros.Model)
                        {
                            if (string.IsNullOrEmpty(item.area)) item.area = opNombre;
                            masterList.Add(item);
                        }
                    }
                }

                PayLoadList = masterList;

                if (!PayLoadList.Any())
                    Snackbar.Add("No se encontraron registros en este rango de fechas.", Severity.Info);
                else
                    Snackbar.Add($"{PayLoadList.Count} registros cargados de {opList.Count} cliente(s).", Severity.Success);
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

                var detailsList = new List<object>();
                var opList = _selectedOperaciones.Where(x => x > 0).ToList();
                if (!opList.Any())
                {
                    opList = PayLoadOper.Where(o => PayLoadList.Any(p => p.area == o.nombre)).Select(o => o.id).ToList();
                }

                foreach (var opId in opList)
                {
                    typeeinout rango = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = opId };
                    var detailsResult = await _mediator.Send(new GetAllDiasTrabajadosOperacionQuery(rango));

                    if (detailsResult?.Model != null)
                    {
                        string opNombre = PayLoadOper.FirstOrDefault(x => x.id == opId)?.nombre ?? "";
                        foreach (var emp in detailsResult.Model)
                        {
                            if (emp?.detalleall == null) continue;
                            foreach (var mark in emp.detalleall)
                            {
                                detailsList.Add(new
                                {
                                    Cliente = opNombre,
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
                    string nombreArchivo = $"Nomina_Consolidada_{DateTime.Now:ddMMyy}.xlsx";

                    await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, nombreArchivo);
                    Snackbar.Add("Excel consolidado generado exitosamente.", Severity.Success);
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
