using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;
using Utilidades.Interfaces;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            public int No_Empleado { get; set; }
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
            public decimal Viatico_Transporte { get; set; }
            public decimal Viatico_Alimentacion { get; set; }
            public decimal Viatico_Combustible { get; set; }
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
                    No_Empleado = x.no_empleado,
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
                    Viatico_Transporte = x.viatico_transporte,
                    Viatico_Alimentacion = x.viatico_alimentacion,
                    Viatico_Combustible = x.viatico_combustible,
                    Otros_Ingresos = x.otros_ingresos,
                    Total_Devengado = x.total_devengado,
                    INSS_Laboral = x.inss_laboral,
                    IR_Laboral = x.ir_laboral,
                    Prestamos = x.prestamos_deduccion,
                    Otras_Deducciones = x.otras_deducciones,
                    Total_Deducciones = x.total_deducciones,
                    Total_Neto_Pagar = x.total_neto
                }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var pack = new ExcelPackage();
                var ws = pack.Workbook.Worksheets.Add("Liquidaciones");

                // Encabezados exactos requeridos
                string[] headers = new[]
                {
                    "Lote", "No empleado", "Colaborador", "Cédula", "Cliente",
                    "Fecha ingreso", "Fecha salida", "Fecha liquidación",
                    "Salario base", "Días laborados", "Salario ordinario", "Horas extras",
                    "Días vacaciones", "Monto vacaciones", "Días aguinaldo", "Monto aguinaldo",
                    "Días indemnización", "Monto indemnización",
                    "Viático transporte", "Viático alimentación", "Viático combustible", "Otros ingresos",
                    "Total devengado", "INSS laboral", "IR laboral", "Préstamos deducción",
                    "Otras deducciones", "Total deducciones", "Total neto"
                };

                // Título corporativo
                ws.Cells["A2:G2"].Merge = true;
                ws.Cells["A2"].Value = "BUSSERSA - REPORTE DE LIQUIDACIONES";
                ws.Cells["A2"].Style.Font.Size = 13;
                ws.Cells["A2"].Style.Font.Bold = true;

                ws.Cells["A3:G3"].Merge = true;
                ws.Cells["A3"].Value = $"Período: {periodoNombre} | Generado: {DateTime.Now:yyyy-MM-dd HH:mm}";
                ws.Cells["A3"].Style.Font.Size = 10;
                ws.Cells["A3"].Style.Font.Italic = true;

                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cells[4, c + 1];
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(24, 43, 73));
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int row = 5;
                foreach (var item in exportList)
                {
                    ws.Cells[row, 1].Value = item.Lote;
                    ws.Cells[row, 2].Value = item.No_Empleado;
                    ws.Cells[row, 3].Value = item.Colaborador;
                    ws.Cells[row, 4].Value = item.Cedula;
                    ws.Cells[row, 5].Value = item.Cliente;
                    ws.Cells[row, 6].Value = item.Fecha_Ingreso;
                    ws.Cells[row, 7].Value = item.Fecha_Salida;
                    ws.Cells[row, 8].Value = item.Fecha_Liquidacion;
                    ws.Cells[row, 9].Value = item.Salario_Base;
                    ws.Cells[row, 10].Value = item.Dias_Laborados;
                    ws.Cells[row, 11].Value = item.Salario_Ordinario;
                    ws.Cells[row, 12].Value = item.Horas_Extras;
                    ws.Cells[row, 13].Value = item.Dias_Vacaciones;
                    ws.Cells[row, 14].Value = item.Monto_Vacaciones;
                    ws.Cells[row, 15].Value = item.Dias_Aguinaldo;
                    ws.Cells[row, 16].Value = item.Monto_Aguinaldo;
                    ws.Cells[row, 17].Value = item.Dias_Indemnizacion;
                    ws.Cells[row, 18].Value = item.Monto_Indemnizacion;
                    ws.Cells[row, 19].Value = item.Viatico_Transporte;
                    ws.Cells[row, 20].Value = item.Viatico_Alimentacion;
                    ws.Cells[row, 21].Value = item.Viatico_Combustible;
                    ws.Cells[row, 22].Value = item.Otros_Ingresos;
                    ws.Cells[row, 23].Value = item.Total_Devengado;
                    ws.Cells[row, 24].Value = item.INSS_Laboral;
                    ws.Cells[row, 25].Value = item.IR_Laboral;
                    ws.Cells[row, 26].Value = item.Prestamos;
                    ws.Cells[row, 27].Value = item.Otras_Deducciones;
                    ws.Cells[row, 28].Value = item.Total_Deducciones;
                    ws.Cells[row, 29].Value = item.Total_Neto_Pagar;
                    row++;
                }

                if (row > 5)
                {
                    // No empleado centrado
                    ws.Cells[5, 2, row - 1, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    // Fechas en formato texto garantizado yyyy-MM-dd
                    ws.Cells[5, 6, row - 1, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[5, 6, row - 1, 8].Style.Numberformat.Format = "@";
                    // Columnas numéricas con formato moneda/número
                    ws.Cells[5, 9, row - 1, 29].Style.Numberformat.Format = "#,##0.00";
                }

                ws.Cells[4, 1, Math.Max(5, row - 1), headers.Length].AutoFitColumns();

                byte[] excelBytes = pack.GetAsByteArray();
                string base64Data = Convert.ToBase64String(excelBytes);
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
                   (x.cliente_nombre?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   x.no_empleado.ToString().Contains(_searchString);
        };
    }
}
