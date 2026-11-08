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

        public class SettlementReportExportDto
        {
            public string Lote { get; set; } = string.Empty;
            public string Colaborador { get; set; } = string.Empty;
            public string Cedula { get; set; } = string.Empty;
            public string Cliente { get; set; } = string.Empty;
            public string Fecha_Ingreso { get; set; } = string.Empty;
            public string Fecha_Salida { get; set; } = string.Empty;
            public string Fecha_Liquidacion { get; set; } = string.Empty;
            public decimal Salario_Base { get; set; }
            public decimal Dias_Laborados { get; set; }
            public decimal Salario_Ordinario { get; set; }
            public decimal Horas_Extras { get; set; }
            public decimal Dias_Vacaciones { get; set; }
            public decimal Monto_Vacaciones { get; set; }
            public decimal Dias_Aguinaldo { get; set; }
            public decimal Monto_Aguinaldo { get; set; }
            public decimal Dias_Indemnizacion { get; set; }
            public decimal Monto_Indemnizacion { get; set; }
            public decimal Otros_Ingresos { get; set; }
            public decimal Total_Devengado { get; set; }
            public decimal INSS_Laboral { get; set; }
            public decimal IR_Laboral { get; set; }
            public decimal Prestamos { get; set; }
            public decimal Otras_Deducciones { get; set; }
            public decimal Total_Deducciones { get; set; }
            public decimal Total_Neto_Pagar { get; set; }
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

                var exportList = LiquidacionesList.Select(x => new SettlementReportExportDto
                {
                    Lote = x.lote_nombre,
                    Colaborador = x.empleado_nombre,
                    Cedula = x.cedula,
                    Cliente = x.cliente_nombre,
                    Fecha_Ingreso = x.fecha_ingreso?.ToString("yyyy-MM-dd") ?? "",
                    Fecha_Salida = x.fecha_salida?.ToString("yyyy-MM-dd") ?? "",
                    Fecha_Liquidacion = x.fecha_liquidacion.ToString("yyyy-MM-dd"),
                    Salario_Base = x.salario_base,
                    Dias_Laborados = x.dias_laborados,
                    Salario_Ordinario = x.salario_ordinario_monto,
                    Horas_Extras = x.horas_extras_monto,
                    Dias_Vacaciones = x.dias_vacaciones,
                    Monto_Vacaciones = x.vacaciones_monto,
                    Dias_Aguinaldo = x.dias_aguinaldo,
                    Monto_Aguinaldo = x.aguinaldo_monto,
                    Dias_Indemnizacion = x.dias_indemnizacion,
                    Monto_Indemnizacion = x.indemnizacion_monto,
                    Otros_Ingresos = x.otros_ingresos,
                    Total_Devengado = x.total_devengado,
                    INSS_Laboral = x.inss_laboral,
                    IR_Laboral = x.ir_laboral,
                    Prestamos = x.prestamos_deduccion,
                    Otras_Deducciones = x.otras_deducciones,
                    Total_Deducciones = x.total_deducciones,
                    Total_Neto_Pagar = x.total_neto
                }).ToList();

                var exceldata = DataExcel.CreateExcel(exportList, "Liquidaciones");
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
