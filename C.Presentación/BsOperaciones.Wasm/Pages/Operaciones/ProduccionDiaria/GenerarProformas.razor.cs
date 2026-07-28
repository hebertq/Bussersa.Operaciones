using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using HostService.Interfaces;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.ClasesGenericas;
using BsOperaciones.Application.Features.Odoo.Queries;

namespace BsOperaciones.Pages.Operaciones.ProduccionDiaria
{
    public partial class GenerarProformas : ComponentBase
    {
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IOdooService OdooService { get; set; } = null!;
        [Inject] private NavigationManager _navigationManager { get; set; } = null!;
        [Inject] private MediatR.IMediator _mediator { get; set; } = null!;

        protected IBrowserFile? archivoExcel;
        protected bool estaCargandoArchivo = false;
        protected bool estaGenerando = false;
        protected List<ProformaGrupoDto> gruposProformas = new();
        protected List<Combos> operacionesList = new();
        protected int? selectedOperacionId;

        protected async Task OnExcelFileSelected(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file == null) return;
            archivoExcel = file;
            estaCargandoArchivo = true;
            gruposProformas.Clear();
            StateHasChanged();

            try
            {
                using (var ms = new MemoryStream())
                {
                    await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
                    ms.Position = 0;

                    IWorkbook workbook = WorkbookFactory.Create(ms);
                    IFormulaEvaluator evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
                    
                    // Try to find the "Consolidado" sheet, or fallback to the first sheet
                    ISheet sheet = workbook.GetSheet("Consolidado") ?? workbook.GetSheetAt(0);

                    // Search for the header row containing "Cliente" within the first 10 rows
                    IRow? headerRow = null;
                    int headerRowIndex = -1;
                    for (int r = 0; r <= Math.Min(sheet.LastRowNum, 10); r++)
                    {
                        var row = sheet.GetRow(r);
                        if (row == null) continue;

                        for (int c = 0; c < row.LastCellNum; c++)
                        {
                            var cellVal = row.GetCell(c)?.ToString()?.Trim()?.ToLower() ?? "";
                            if (cellVal == "cliente" || cellVal.Contains("cliente"))
                            {
                                headerRow = row;
                                headerRowIndex = r;
                                break;
                            }
                        }
                        if (headerRow != null) break;
                    }

                    if (headerRow == null)
                    {
                        _snackbar.Add("No se encontró la cabecera 'Cliente' en el archivo Excel.", Severity.Error);
                        archivoExcel = null;
                        estaCargandoArchivo = false;
                        return;
                    }

                    // Map columns indices
                    var colIndices = new Dictionary<string, int>();
                    for (int i = 0; i < headerRow.LastCellNum; i++)
                    {
                        var cellValue = headerRow.GetCell(i)?.ToString()?.Trim()?.ToLower();
                        if (string.IsNullOrEmpty(cellValue)) continue;

                        if (cellValue == "cliente") colIndices["cliente"] = i;
                        else if (cellValue.Contains("área") || cellValue.Contains("area") || cellValue.Contains("cadena")) colIndices["area"] = i;
                        else if (cellValue.Contains("sku") || cellValue.Contains("código") || cellValue.Contains("codigo")) colIndices["sku"] = i;
                        else if (cellValue.Contains("descripción") || cellValue.Contains("descripcion")) colIndices["descripcion"] = i;
                        else if (cellValue.Contains("rango") || cellValue.Contains("fechas") || cellValue.Contains("período") || cellValue.Contains("periodo")) colIndices["rango_fechas"] = i;
                        else if (cellValue.Contains("tarifa")) colIndices["tarifa"] = i;
                        else if (cellValue.Contains("cantidad")) colIndices["cantidad"] = i;
                        else if (cellValue.Contains("agrupación") || cellValue.Contains("agrupacion")) colIndices["agrupacion"] = i;
                    }

                    // Validate minimal required columns
                    if (!colIndices.ContainsKey("cliente") || !colIndices.ContainsKey("sku") || !colIndices.ContainsKey("agrupacion"))
                    {
                        _snackbar.Add("El Excel debe contener al menos las columnas: 'Cliente', 'Código SKU' y 'Agrupación'.", Severity.Error);
                        archivoExcel = null;
                        estaCargandoArchivo = false;
                        return;
                    }

                    var parsedItems = new List<(string cliente, string agrupacion, ProformaItemDto item)>();

                    for (int r = headerRowIndex + 1; r <= sheet.LastRowNum; r++)
                    {
                        IRow row = sheet.GetRow(r);
                        if (row == null) continue;

                        var cliente = GetStringVal(row, colIndices, "cliente", evaluator);
                        var agrupacion = GetStringVal(row, colIndices, "agrupacion", evaluator);
                        var sku = GetStringVal(row, colIndices, "sku", evaluator);

                        // Skip rows that don't have basic data or don't have an Agrupación
                        if (string.IsNullOrEmpty(cliente) || string.IsNullOrEmpty(sku) || string.IsNullOrEmpty(agrupacion))
                            continue;

                        var descCompleta = GetStringVal(row, colIndices, "descripcion", evaluator) ?? sku;
                        var rangoFechas = GetStringVal(row, colIndices, "rango_fechas", evaluator);
                        if (!string.IsNullOrEmpty(rangoFechas) && !descCompleta.Contains("Del ") && !descCompleta.Contains("del ") && !descCompleta.Contains(rangoFechas))
                        {
                            descCompleta = $"{descCompleta} ({rangoFechas})";
                        }

                        var itemDto = new ProformaItemDto
                        {
                            Area = GetStringVal(row, colIndices, "area", evaluator) ?? "",
                            SKU = sku,
                            Descripcion = descCompleta,
                            Cantidad = GetDecimalVal(row, colIndices, "cantidad", evaluator),
                            Tarifa = GetDecimalVal(row, colIndices, "tarifa", evaluator)
                        };

                        parsedItems.Add((cliente, agrupacion, itemDto));
                    }

                    // Group by Cliente + Agrupación
                    gruposProformas = parsedItems
                        .GroupBy(x => new { x.cliente, x.agrupacion })
                        .Select(g => new ProformaGrupoDto
                        {
                            Cliente = g.Key.cliente,
                            Agrupacion = g.Key.agrupacion,
                            Items = g.Select(x => x.item).ToList()
                        })
                        .OrderBy(x => x.Cliente)
                        .ThenBy(x => x.Agrupacion)
                        .ToList();

                    if (!gruposProformas.Any())
                    {
                        _snackbar.Add("No se encontraron registros con la columna 'Agrupación' especificada.", Severity.Warning);
                    }
                    else
                    {
                        _snackbar.Add($"Se procesó el archivo Excel. Detectadas {gruposProformas.Count} proformas.", Severity.Success);
                    }
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al analizar el archivo: {ex.Message}", Severity.Error);
                archivoExcel = null;
            }
            finally
            {
                estaCargandoArchivo = false;
                StateHasChanged();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var opResponse = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
                if (opResponse.Model != null)
                {
                    operacionesList = opResponse.Model.ToList();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar operaciones: {ex.Message}", Severity.Error);
            }
        }

        protected async Task GenerarProformasOdoo()
        {
            if (gruposProformas == null || !gruposProformas.Any())
            {
                _snackbar.Add("No hay proformas para generar.", Severity.Warning);
                return;
            }

            if (!selectedOperacionId.HasValue || selectedOperacionId.Value <= 0)
            {
                _snackbar.Add("Debe seleccionar la Operación antes de generar las proformas.", Severity.Warning);
                return;
            }

            estaGenerando = true;
            StateHasChanged();

            try
            {
                var request = new GenerarProformasOdooRequest
                {
                    OperacionId = selectedOperacionId.Value,
                    Grupos = gruposProformas
                };

                var response = await OdooService.GenerarProformasOdoo(request);
                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al generar proformas en Odoo: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    _snackbar.Add("Proformas creadas y asociadas exitosamente en Odoo y el sistema local.", Severity.Success);
                    gruposProformas.Clear();
                    archivoExcel = null;
                    _navigationManager.NavigateTo("/operaciones/produccion-diaria");
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al enviar petición: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaGenerando = false;
                StateHasChanged();
            }
        }

        #region Helper Excel parsers
        private string? GetStringVal(IRow row, Dictionary<string, int> indices, string key, IFormulaEvaluator evaluator)
        {
            if (!indices.ContainsKey(key)) return null;
            var cell = row.GetCell(indices[key]);
            if (cell == null) return null;

            if (cell.CellType == CellType.Formula)
            {
                if (cell.CachedFormulaResultType == CellType.String) return cell.StringCellValue?.Trim();
                if (cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue.ToString()?.Trim();
                if (cell.CachedFormulaResultType == CellType.Boolean) return cell.BooleanCellValue.ToString()?.Trim();
                return string.Empty;
            }

            return cell.ToString()?.Trim();
        }

        private decimal GetDecimalVal(IRow row, Dictionary<string, int> indices, string key, IFormulaEvaluator evaluator)
        {
            if (!indices.ContainsKey(key)) return 0;
            var cell = row.GetCell(indices[key]);
            if (cell == null) return 0;

            if (cell.CellType == CellType.Numeric)
            {
                return (decimal)cell.NumericCellValue;
            }
            if (cell.CellType == CellType.Formula)
            {
                if (cell.CachedFormulaResultType == CellType.Numeric)
                    return (decimal)cell.NumericCellValue;
                if (cell.CachedFormulaResultType == CellType.String && decimal.TryParse(cell.StringCellValue, out decimal v))
                    return v;
                return 0;
            }

            if (decimal.TryParse(cell.ToString(), out decimal val))
            {
                return val;
            }

            return 0;
        }
        #endregion
    }
}
