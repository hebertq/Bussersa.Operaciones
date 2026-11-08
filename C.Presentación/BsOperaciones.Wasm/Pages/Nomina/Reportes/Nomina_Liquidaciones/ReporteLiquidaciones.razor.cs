using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;
using Utilidades.Interfaces;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.Reportes.Nomina_Liquidaciones
{
    public partial class ReporteLiquidaciones : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IUserInfo _Iuser { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        public List<SettlementReportRecord> AllLiquidaciones { get; set; } = new();
        public List<SettlementReportRecord> LiquidacionesList { get; set; } = new();
        public List<ClientFilterItem> ClientesList { get; set; } = new();

        public bool isloaddata { get; set; } = false;
        protected string _searchString = "";
        protected int anio = DateTime.Now.Year;
        protected int mes = 0; // 0 = Todos los meses
        protected int clienteId = 0; // 0 = Todos los clientes
        protected List<int> YearsList = new();

        public class ClientFilterItem
        {
            public int id { get; set; }
            public string nombre { get; set; } = string.Empty;
        }

        protected override async Task OnInitializedAsync()
        {
            int currentYear = DateTime.Now.Year;
            YearsList = new List<int> { currentYear, currentYear - 1, currentYear - 2 };
            await OnConsultar();
        }

        protected async Task OnConsultar()
        {
            isloaddata = true;
            try
            {
                var response = await _mediator.Send(new GetReporteLiquidacionesQuery(anio, mes, clienteId));
                if (response.Model != null && response.Model.Count > 0)
                {
                    AllLiquidaciones = response.Model;
                    LiquidacionesList = response.Model;

                    // Extraer clientes únicos para el combo si no está filtrado
                    if (clienteId == 0)
                    {
                        ClientesList = AllLiquidaciones
                            .Where(x => x.cliente_id > 0)
                            .GroupBy(x => x.cliente_id)
                            .Select(g => new ClientFilterItem
                            {
                                id = g.Key,
                                nombre = g.First().cliente_nombre
                            })
                            .OrderBy(x => x.nombre)
                            .ToList();
                    }
                }
                else
                {
                    AllLiquidaciones = new List<SettlementReportRecord>();
                    LiquidacionesList = new List<SettlementReportRecord>();
                    Snackbar.Add("No se encontraron liquidaciones para los filtros seleccionados.", Severity.Info);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al consultar liquidaciones: {ex.Message}", Severity.Error);
            }
            finally
            {
                isloaddata = false;
            }
        }

        protected async Task DownloadFile()
        {
            if (!LiquidacionesList.Any()) return;
            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                string periodoNombre = mes > 0 ? $"{anio}_{mes:00}" : $"{anio}_Anual";
                string nombre = $"Reporte_Liquidaciones_{periodoNombre}";
                var exceldata = DataExcel.CreateExcel(LiquidacionesList, "Liquidaciones");
                string base64Data = Convert.ToBase64String(exceldata.Data);
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, $"{nombre}.xlsx");
                Snackbar.Add("Descarga de Excel generada exitosamente.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Fallo al exportar Excel: {ex.Message}", Severity.Error);
            }
            finally
            {
                isloaddata = false;
            }
        }

        protected Func<SettlementReportRecord, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            return (x.empleado_nombre?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (x.cedula?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (x.lote_nombre?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (x.cliente_nombre?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false);
        };
    }
}
