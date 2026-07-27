using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using MudBlazor;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BsOperaciones.Pages.Operaciones.ProduccionDiaria
{
    public partial class ProduccionDiaria : ComponentBase
    {
        [Inject] private IMediator _mediator { get; set; } = null!;
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IJSRuntime _jsRuntime { get; set; } = null!;
        [Inject] private IDialogService _dialogService { get; set; } = null!;
        [Inject] private IOdooService OdooService { get; set; } = null!;

        // Formato prellenado state
        protected GenerarFormatoRequest formatoRequest = new();
        protected bool estaDescargando;

        // Carga de Excel state
        protected IBrowserFile? archivoExcel;
        protected bool estaImportando;
        protected List<Combos> operacionesList = new();
        protected int operacionCargaId;

        // Historial / Filtros
        protected DateRange dateRange = new(DateTime.Today.AddDays(-30), DateTime.Today);
        protected int? filtroOperacionId;
        protected string filtroEstado = "Todos";
        protected bool estaCargando;
        protected bool estaExportando;
        protected List<ProduccionDiariaDto> produccionDiariaList = new();

        // Consolidación
        protected HashSet<ProduccionDiariaDto> selectedItems = new();
        protected string noProformaConsolidada = string.Empty;
        protected string noFacturaConsolidada = string.Empty;
        protected bool estaConsolidando;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var regop = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
                operacionesList = regop.Model ?? new();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar operaciones: {ex.Message}", Severity.Error);
            }
            await CargarProduccion();
        }

        protected async Task CargarProduccion()
        {
            estaCargando = true;
            StateHasChanged();
            try
            {
                var response = await _mediator.Send(new GetProduccionDiariaQuery(
                    dateRange.Start, 
                    dateRange.End, 
                    filtroOperacionId, 
                    filtroEstado == "Todos" ? null : filtroEstado
                ));

                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al cargar historial: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    produccionDiariaList = response.Model?.ToList() ?? new List<ProduccionDiariaDto>();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar historial: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
            }
        }

        protected void LimpiarFiltros()
        {
            dateRange = new DateRange(DateTime.Today.AddDays(-30), DateTime.Today);
            filtroOperacionId = null;
            filtroEstado = "Todos";
            _ = CargarProduccion();
        }

        protected async Task<IEnumerable<Combos>> BuscarPlantillas(string value, System.Threading.CancellationToken token)
        {
            try
            {
                var response = await _mediator.Send(new GetAllCombosQuery("Plantillas"));
                if (response.Respuesta.ExisteError || response.Model == null) return Array.Empty<Combos>();

                if (string.IsNullOrEmpty(value))
                    return response.Model.Take(15);

                return response.Model
                    .Where(x => x.nombre.Contains(value, StringComparison.InvariantCultureIgnoreCase))
                    .Take(15);
            }
            catch
            {
                return Array.Empty<Combos>();
            }
        }

        protected async Task DescargarPlantillaExcel()
        {
            if (operacionCargaId == 0)
            {
                _snackbar.Add("Por favor seleccione la operación/cliente antes de descargar.", Severity.Warning);
                return;
            }

            var selectedOp = operacionesList.FirstOrDefault(x => x.id == operacionCargaId);
            if (selectedOp == null)
            {
                _snackbar.Add("Operación/cliente no válida.", Severity.Warning);
                return;
            }

            estaDescargando = true;
            StateHasChanged();
            try
            {
                formatoRequest.templateId = operacionCargaId;
                var query = new GenerarFormatoExcelQuery(formatoRequest);
                var response = await _mediator.Send(query);

                if (response.Respuesta.ExisteError || response.Model == null || string.IsNullOrEmpty(response.Model.Base64Data))
                {
                    _snackbar.Add($"Error al generar formato: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    string cleanName = selectedOp.nombre.Replace(" ", "_").Replace("/", "_").Replace("-", "_").Replace(":", "_");
                    string fileName = $"{cleanName}.xlsx";
                    await _jsRuntime.InvokeVoidAsync(
                        "downloadFile", 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        response.Model.Base64Data, 
                        fileName
                    );
                    _snackbar.Add("Formato Excel descargado correctamente.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al descargar: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaDescargando = false;
                StateHasChanged();
            }
        }

        protected void OnExcelFileSelected(InputFileChangeEventArgs e)
        {
            archivoExcel = e.File;
        }

        protected async Task EliminarRegistro(ProduccionDiariaDto item)
        {
            if (!string.IsNullOrEmpty(item.no_proforma) || !string.IsNullOrEmpty(item.no_factura) || item.facturada)
            {
                _snackbar.Add("No se puede eliminar un registro que ya tiene proforma o factura asignada.", Severity.Warning);
                return;
            }

            bool? result = await _dialogService.ShowMessageBox(
                "Confirmar Eliminación",
                $"¿Está seguro de eliminar el registro de {item.servicio_descripcion} del cliente {item.cliente}?",
                yesText: "Eliminar", cancelText: "Cancelar");

            if (result == true)
            {
                estaCargando = true;
                StateHasChanged();
                try
                {
                    var response = await _mediator.Send(new BsOperaciones.Application.Features.Odoo.Command.DeleteProduccionDiariaCommand { Id = item.id });
                    if (response.Respuesta.ExisteError)
                    {
                        _snackbar.Add($"Error al eliminar: {response.Respuesta.MensajeError}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add("Registro eliminado exitosamente.", Severity.Success);
                        await CargarProduccion();
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Excepción al eliminar: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }

        protected async Task EliminarSeleccionados()
        {
            if (selectedItems == null || !selectedItems.Any()) return;

            var deletable = selectedItems.Where(x => string.IsNullOrEmpty(x.no_proforma) && string.IsNullOrEmpty(x.no_factura) && !x.facturada).ToList();
            if (!deletable.Any())
            {
                _snackbar.Add("No se pueden eliminar los registros seleccionados porque todos tienen proforma o factura asignada.", Severity.Warning);
                return;
            }

            bool? result = await _dialogService.ShowMessageBox(
                "Confirmar Eliminación Masiva",
                $"¿Está seguro de eliminar los {deletable.Count} registros seleccionados?",
                yesText: "Eliminar Todo", cancelText: "Cancelar");

            if (result == true)
            {
                estaCargando = true;
                StateHasChanged();
                try
                {
                    var ids = deletable.Select(x => x.id).ToList();
                    var response = await _mediator.Send(new BsOperaciones.Application.Features.Odoo.Command.DeleteBulkProduccionDiariaCommand { Ids = ids });
                    if (response.Respuesta.ExisteError)
                    {
                        _snackbar.Add($"Error al eliminar: {response.Respuesta.MensajeError}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add($"{deletable.Count} registros eliminados exitosamente.", Severity.Success);
                        selectedItems.Clear();
                        await CargarProduccion();
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Excepción al eliminar: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }

        protected async Task ImportarArchivoProduccion()
        {
            if (operacionCargaId == 0)
            {
                _snackbar.Add("Por favor seleccione la operación/cliente antes de importar.", Severity.Warning);
                return;
            }
            if (archivoExcel == null)
            {
                _snackbar.Add("Por favor seleccione un archivo Excel para importar.", Severity.Warning);
                return;
            }
            estaImportando = true;
            StateHasChanged();

            try
            {
                var items = new List<ProduccionDiariaDto>();

                using (var ms = new MemoryStream())
                {
                    await archivoExcel.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
                    ms.Position = 0;

                    IWorkbook workbook = WorkbookFactory.Create(ms);
                    IFormulaEvaluator evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
                    ISheet sheet = workbook.GetSheetAt(0);

                    IRow headerRow = sheet.GetRow(0);
                    if (headerRow == null)
                    {
                        _snackbar.Add("El archivo Excel está vacío.", Severity.Error);
                        estaImportando = false;
                        return;
                    }

                    var colIndices = new Dictionary<string, int>();
                    for (int i = 0; i < headerRow.LastCellNum; i++)
                    {
                        var cellValue = headerRow.GetCell(i)?.ToString()?.Trim()?.ToLower();
                        if (string.IsNullOrEmpty(cellValue)) continue;

                        if (cellValue.Contains("fecha inicio") || cellValue.Contains("fecha_inicio") || cellValue.Contains("fecha de inicio") || cellValue.Contains("fecha desde")) colIndices["fecha_inicio"] = i;
                        else if (cellValue.Contains("fecha fin") || cellValue.Contains("fecha_fin") || cellValue.Contains("fecha de fin") || cellValue.Contains("fecha final") || cellValue.Contains("fecha hasta")) colIndices["fecha_fin"] = i;
                        else if (cellValue.Contains("fecha")) colIndices["fecha"] = i;
                        else if (cellValue.Contains("hoja")) colIndices["hoja_servicio"] = i;
                        else if (cellValue.Contains("cliente") && !cellValue.Contains("área") && !cellValue.Contains("area")) colIndices["cliente"] = i;
                        else if (cellValue.Contains("área") || cellValue.Contains("area")) colIndices["area_cliente"] = i;
                        else if (cellValue.Contains("hora inicio") || cellValue.Contains("hora_inicio")) colIndices["hora_inicio"] = i;
                        else if (cellValue.Contains("hora fin") || cellValue.Contains("hora_fin")) colIndices["hora_fin"] = i;
                        else if (cellValue.Contains("actividad")) colIndices["actividad"] = i;
                        else if (cellValue.Contains("sku") || cellValue.Contains("código") || cellValue.Contains("codigo")) colIndices["servicio_codigo"] = i;
                        else if (cellValue.Contains("descripción") || cellValue.Contains("descripcion")) colIndices["servicio_descripcion"] = i;
                        else if (cellValue.Contains("lote")) colIndices["no_lote"] = i;
                        else if (cellValue.Contains("oc")) colIndices["oc"] = i;
                        else if (cellValue.Contains("marchamo")) colIndices["no_marchamo"] = i;
                        else if (cellValue.Contains("peso")) colIndices["peso"] = i;
                        else if (cellValue.Contains("cantidad")) colIndices["cantidad"] = i;
                        else if (cellValue.Contains("costo") || cellValue.Contains("tarifa")) colIndices["costo_producto"] = i;
                        else if (cellValue.Contains("asignado") || cellValue.Contains("operador")) colIndices["asignado_a"] = i;
                    }

                    if (!colIndices.ContainsKey("servicio_codigo"))
                    {
                        _snackbar.Add("El archivo no tiene la columna obligatoria: 'Código Servicio (SKU)'.", Severity.Error);
                        estaImportando = false;
                        return;
                    }

                    for (int r = 1; r <= sheet.LastRowNum; r++)
                    {
                        IRow row = sheet.GetRow(r);
                        if (row == null) continue;

                        var hs = GetStringVal(row, colIndices, "hoja_servicio", evaluator) ?? string.Empty;
                        var sc = GetStringVal(row, colIndices, "servicio_codigo", evaluator);
                        
                        if (string.IsNullOrEmpty(sc)) continue;

                        var fInicio = GetDateVal(row, colIndices, "fecha_inicio", evaluator) ?? GetDateVal(row, colIndices, "fecha", evaluator);
                        var fFin = GetDateVal(row, colIndices, "fecha_fin", evaluator) ?? fInicio;

                        var dto = new ProduccionDiariaDto
                        {
                            hoja_servicio = hs,
                            servicio_codigo = sc,
                            actividad = GetStringVal(row, colIndices, "actividad", evaluator),
                            cliente = GetStringVal(row, colIndices, "cliente", evaluator),
                            area_cliente = GetStringVal(row, colIndices, "area_cliente", evaluator),
                            fecha_inicio = fInicio,
                            fecha_fin = fFin,
                            hora_inicio = GetStringVal(row, colIndices, "hora_inicio", evaluator),
                            hora_fin = GetStringVal(row, colIndices, "hora_fin", evaluator),
                            nombre_producto = GetStringVal(row, colIndices, "servicio_descripcion", evaluator),
                            servicio_descripcion = GetStringVal(row, colIndices, "servicio_descripcion", evaluator),
                            no_lote = GetStringVal(row, colIndices, "no_lote", evaluator),
                            oc = GetStringVal(row, colIndices, "oc", evaluator),
                            no_marchamo = GetStringVal(row, colIndices, "no_marchamo", evaluator),
                            peso = GetDecimalVal(row, colIndices, "peso", evaluator),
                            cantidad_producto = GetDecimalVal(row, colIndices, "cantidad", evaluator),
                            costo_producto = GetDecimalVal(row, colIndices, "costo_producto", evaluator),
                            asignado_a = GetStringVal(row, colIndices, "asignado_a", evaluator)
                        };
                        dto.operacion_id = operacionCargaId;

                        items.Add(dto);
                    }
                }

                if (!items.Any())
                {
                    _snackbar.Add("El archivo no contiene registros válidos para importar.", Severity.Warning);
                    estaImportando = false;
                    return;
                }

                var response = await _mediator.Send(new ImportarProduccionDiariaCommand(items));
                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al importar: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    _snackbar.Add($"Se procesaron {items.Count} registros correctamente.", Severity.Success);
                    archivoExcel = null;
                    await CargarProduccion();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al procesar archivo: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaImportando = false;
                StateHasChanged();
            }
        }

        protected async Task ConsolidarSeleccion()
        {
            if (selectedItems == null || !selectedItems.Any() || string.IsNullOrEmpty(noProformaConsolidada))
            {
                _snackbar.Add("Seleccione ítems e ingrese el número de proforma.", Severity.Warning);
                return;
            }

            estaConsolidando = true;
            StateHasChanged();
            try
            {
                var req = new ConsolidarProformaRequest
                {
                    ids = selectedItems.Select(x => x.id).ToList(),
                    no_proforma = noProformaConsolidada,
                    no_factura = string.IsNullOrEmpty(noFacturaConsolidada) ? null : noFacturaConsolidada
                };

                var response = await _mediator.Send(new ConsolidarProformaCommand(req));
                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al consolidar: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    _snackbar.Add($"Se asociaron {req.ids.Count} registros a la proforma {noProformaConsolidada} correctamente.", Severity.Success);
                    selectedItems.Clear();
                    noProformaConsolidada = string.Empty;
                    noFacturaConsolidada = string.Empty;
                    await CargarProduccion();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al consolidar: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaConsolidando = false;
                StateHasChanged();
            }
        }

        protected async Task OnOrderChanged(ProduccionDiariaDto item, int? newOrder)
        {
            if (!newOrder.HasValue) return;
            item.proforma_orden = newOrder.Value;

            try
            {
                var response = await _mediator.Send(new ActualizarOrdenItemProformaCommand(item.id, newOrder.Value));
                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al actualizar orden: {response.Respuesta.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al actualizar orden: {ex.Message}", Severity.Error);
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

        private DateTime? GetDateVal(IRow row, Dictionary<string, int> indices, string key, IFormulaEvaluator evaluator)
        {
            if (!indices.ContainsKey(key)) return null;
            var cell = row.GetCell(indices[key]);
            if (cell == null) return null;

            if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
            {
                return cell.DateCellValue;
            }
            
            if (cell.CellType == CellType.Formula)
            {
                if (cell.CachedFormulaResultType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue;
                }
                
                string? sVal = cell.CachedFormulaResultType == CellType.String ? cell.StringCellValue : null;
                if (DateTime.TryParse(sVal, out DateTime d)) return d;
                return null;
            }

            if (DateTime.TryParse(cell.ToString(), out DateTime date))
            {
                return date;
            }

            return null;
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

        protected async Task ExportarExcelAnalisis()
        {
            if (produccionDiariaList == null || !produccionDiariaList.Any())
            {
                _snackbar.Add("No hay datos cargados para exportar.", Severity.Warning);
                return;
            }

            estaExportando = true;
            StateHasChanged();
            try
            {
                // 1. Group items for the Consolidado sheet
                var consolidatedGroups = produccionDiariaList
                    .GroupBy(x => new { 
                        Cliente = x.cliente ?? "", 
                        Area = x.area_cliente ?? "", 
                        SKU = x.servicio_codigo ?? "", 
                        Descripcion = x.servicio_descripcion ?? "", 
                        Tarifa = x.costo_producto 
                    })
                    .Select(g => new
                    {
                        g.Key.Cliente,
                        g.Key.Area,
                        g.Key.SKU,
                        g.Key.Descripcion,
                        g.Key.Tarifa,
                        Cantidad = g.Sum(x => x.cantidad_producto),
                        Peso = g.Sum(x => x.peso),
                        Total = g.Sum(x => x.cantidad_producto * x.costo_producto)
                    })
                    .OrderBy(x => x.Cliente)
                    .ThenBy(x => x.SKU)
                    .ToList();

                var consolidatedData = new List<Dictionary<string, object>>();
                foreach (var x in consolidatedGroups)
                {
                    var dict = new Dictionary<string, object>
                    {
                        { "Cliente", x.Cliente },
                        { "Área / Cadena", x.Area },
                        { "Código SKU", x.SKU },
                        { "Descripción", x.Descripcion },
                        { "Tarifa Unitario", x.Tarifa },
                        { "Cantidad Total", x.Cantidad },
                        { "Peso Total (kg)", x.Peso },
                        { "Total Facturado (C$)", x.Total },
                        { "Agrupación", "" }
                    };
                    consolidatedData.Add(dict);
                }

                // 2. Map items for the Detalle sheet
                var detailData = new List<Dictionary<string, object>>();
                var detailSorted = produccionDiariaList
                    .OrderByDescending(x => x.fecha_inicio)
                    .ThenBy(x => x.cliente)
                    .ToList();

                foreach (var x in detailSorted)
                {
                    var dict = new Dictionary<string, object>
                    {
                        { "Fecha", x.fecha_inicio?.ToString("yyyy-MM-dd") ?? "" },
                        { "Hoja Servicio", x.hoja_servicio ?? "" },
                        { "Cliente", x.cliente ?? "" },
                        { "Área / Cadena", x.area_cliente ?? "" },
                        { "Actividad", x.actividad ?? "" },
                        { "Código SKU", x.servicio_codigo ?? "" },
                        { "Descripción", x.servicio_descripcion ?? "" },
                        { "No. Lote", x.no_lote ?? "" },
                        { "OC", x.oc ?? "" },
                        { "No. Marchamo", x.no_marchamo ?? "" },
                        { "Peso (kg)", x.peso },
                        { "Cantidad", x.cantidad_producto },
                        { "Tarifa / Costo", x.costo_producto },
                        { "Total Facturado (C$)", x.cantidad_producto * x.costo_producto },
                        { "No. Proforma", x.no_proforma ?? "Sin Proforma" },
                        { "No. Factura", x.no_factura ?? "Sin Factura" },
                        { "Asignado A", x.asignado_a ?? "" }
                    };
                    detailData.Add(dict);
                }

                // 3. Construct the request for GenerateExcel
                var request = new MultiSheetExcelRequest
                {
                    Hojas = new List<ExcelRequest>
                    {
                        new ExcelRequest { Hoja = "Consolidado", Datos = consolidatedData, IncludeHeader = true },
                        new ExcelRequest { Hoja = "Detalle", Datos = detailData, IncludeHeader = true }
                    }
                };

                // 4. Generate the Excel from OdooService
                var response = await OdooService.GenerateExcel(request);

                if (response == null || response.Model == null || string.IsNullOrEmpty(response.Model.File))
                {
                    _snackbar.Add("Error al generar el archivo Excel consolidado.", Severity.Error);
                }
                else
                {
                    string dateRangeStr = "";
                    if (dateRange.Start.HasValue && dateRange.End.HasValue)
                    {
                        dateRangeStr = $"_{dateRange.Start.Value:yyyyMMdd}_al_{dateRange.End.Value:yyyyMMdd}";
                    }
                    string fileName = $"Consolidado_Produccion{dateRangeStr}.xlsx";
                    await _jsRuntime.InvokeVoidAsync(
                        "downloadFile",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        response.Model.File,
                        fileName
                    );
                    _snackbar.Add("Excel de producción exportado correctamente.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al exportar reporte Excel: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaExportando = false;
                StateHasChanged();
            }
        }

        protected async Task EditarRegistro(ProduccionDiariaDto item)
        {
            if (!string.IsNullOrEmpty(item.no_proforma) || !string.IsNullOrEmpty(item.no_factura) || item.facturada)
            {
                _snackbar.Add("No se puede editar un registro que ya tiene proforma o factura asignada.", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters
            {
                { "Item", item },
                { "OperacionesList", operacionesList }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = _dialogService.Show<EditarProduccionDialog>("Editar Registro de Producción", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is ProduccionDiariaDto updatedItem)
            {
                estaCargando = true;
                StateHasChanged();
                try
                {
                    var response = await _mediator.Send(new UpdateProduccionDiariaCommand(updatedItem));
                    if (response.Respuesta.ExisteError)
                    {
                        _snackbar.Add($"Error al actualizar: {response.Respuesta.MensajeError}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add("Registro actualizado exitosamente.", Severity.Success);
                        await CargarProduccion();
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Excepción al actualizar: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }
    }
}
