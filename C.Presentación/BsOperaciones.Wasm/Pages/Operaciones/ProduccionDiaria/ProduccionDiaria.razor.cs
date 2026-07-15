using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Queries;
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

        // Formato prellenado state
        protected GenerarFormatoRequest formatoRequest = new();
        protected Combos? plantillaSeleccionada;
        protected bool estaDescargando;

        // Carga de Excel state
        protected IBrowserFile? archivoExcel;
        protected bool estaImportando;
        protected List<Combos> operacionesList = new();
        protected int operacionCargaId;

        // Historial / Filtros
        protected DateRange dateRange = new(DateTime.Today.AddDays(-30), DateTime.Today);
        protected string? filtroCliente;
        protected string filtroEstado = "Todos";
        protected bool estaCargando;
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
                    filtroCliente, 
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
            filtroCliente = null;
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
            if (plantillaSeleccionada == null)
            {
                _snackbar.Add("Por favor seleccione la plantilla antes de descargar.", Severity.Warning);
                return;
            }

            estaDescargando = true;
            StateHasChanged();
            try
            {
                formatoRequest.templateId = plantillaSeleccionada.id;
                var query = new GenerarFormatoExcelQuery(formatoRequest);
                var response = await _mediator.Send(query);

                if (response.Respuesta.ExisteError || response.Model == null || string.IsNullOrEmpty(response.Model.Base64Data))
                {
                    _snackbar.Add($"Error al generar formato: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    string cleanName = plantillaSeleccionada.nombre.Replace(" ", "_").Replace("/", "_").Replace("-", "_").Replace(":", "_");
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
                    int deletedCount = 0;
                    int errorCount = 0;
                    foreach (var item in deletable)
                    {
                        var response = await _mediator.Send(new BsOperaciones.Application.Features.Odoo.Command.DeleteProduccionDiariaCommand { Id = item.id });
                        if (response.Respuesta.ExisteError)
                        {
                            errorCount++;
                        }
                        else
                        {
                            deletedCount++;
                        }
                    }

                    if (deletedCount > 0)
                    {
                        _snackbar.Add($"{deletedCount} registros eliminados exitosamente.", Severity.Success);
                    }
                    if (errorCount > 0)
                    {
                        _snackbar.Add($"Error al eliminar {errorCount} registros.", Severity.Error);
                    }

                    selectedItems.Clear();
                    await CargarProduccion();
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
            if (archivoExcel == null) return;
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

                        if (cellValue.Contains("fecha")) colIndices["fecha"] = i;
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

                        var dto = new ProduccionDiariaDto
                        {
                            hoja_servicio = hs,
                            servicio_codigo = sc,
                            actividad = GetStringVal(row, colIndices, "actividad", evaluator),
                            cliente = GetStringVal(row, colIndices, "cliente", evaluator),
                            area_cliente = GetStringVal(row, colIndices, "area_cliente", evaluator),
                            fecha_inicio = GetDateVal(row, colIndices, "fecha", evaluator),
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
                        dto.fecha_fin = dto.fecha_inicio;
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
                var cv = evaluator.Evaluate(cell);
                if (cv.CellType == CellType.String) return cv.StringValue?.Trim();
                if (cv.CellType == CellType.Numeric) return cv.NumberValue.ToString()?.Trim();
                if (cv.CellType == CellType.Boolean) return cv.BooleanValue.ToString()?.Trim();
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
                var cv = evaluator.Evaluate(cell);
                if (cv.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                {
                    return cell.DateCellValue;
                }
                
                string? sVal = cv.CellType == CellType.String ? cv.StringValue : cv.ToString();
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
                var cv = evaluator.Evaluate(cell);
                if (cv.CellType == CellType.Numeric)
                    return (decimal)cv.NumberValue;
                if (cv.CellType == CellType.String && decimal.TryParse(cv.StringValue, out decimal v))
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
