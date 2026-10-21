using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HostService.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Modelo.Admin;
using Modelo.ClasesGenericas;
using MudBlazor;
using NPOI.SS.UserModel;

namespace BsOperaciones.Pages.Operaciones.MovimientosPersonal
{
    public partial class CargarMovimientosPersonal : ComponentBase
    {
        [Inject] private IOdooService _odooService { get; set; } = null!;
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IDialogService _dialogService { get; set; } = null!;
        [Inject] private IJSRuntime _js { get; set; } = null!;

        protected List<Combos> operacionesList = new();
        protected int? selectedOperacionId;
        protected string clienteRedmineIdentificador = string.Empty;

        protected IBrowserFile? archivoExcel;
        protected bool estaCargandoArchivo = false;
        protected bool estaProcesando = false;

        protected int progresoActual = 0;
        protected int progresoTotal = 0;
        protected int progresoEnvio => progresoTotal > 0 ? (int)((progresoActual / (double)progresoTotal) * 100) : 0;

        protected int totalExitosos = 0;
        protected int totalFallidos = 0;

        protected string filtroFecha = "TODAS";
        protected List<string> fechasDisponibles = new();
        protected List<MovimientoGridItem> filasExcel = new();

        protected List<MovimientoGridItem> filasFiltradas => 
            filtroFecha == "TODAS" ? filasExcel : filasExcel.Where(x => x.Fecha == filtroFecha).ToList();

        protected bool PuedeCrear => 
            !estaProcesando && 
            selectedOperacionId.HasValue && 
            filasFiltradas.Any(x => x.Seleccionado && x.Estado != EstadoCreacion.Exitoso);

        protected override async Task OnInitializedAsync()
        {
            await CargarOperaciones();
        }

        private async Task CargarOperaciones()
        {
            try
            {
                var response = await _odooService.GetAllCombosHost("Operaciones");
                if (response?.Model != null)
                {
                    operacionesList = response.Model.OrderBy(x => x.id).ToList();

                    // Pre-seleccionar WalMart por defecto si existe (id 4)
                    var wm = operacionesList.FirstOrDefault(x => x.id == 4 || x.nombre.ToLower().Contains("walmart"));
                    if (wm != null)
                    {
                        selectedOperacionId = wm.id;
                        clienteRedmineIdentificador = $"{wm.id}-{wm.nombre}";
                    }
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar operaciones: {ex.Message}", Severity.Warning);
            }
        }

        protected void OnOperacionChanged(int? operacionId)
        {
            selectedOperacionId = operacionId;
            if (operacionId.HasValue)
            {
                var op = operacionesList.FirstOrDefault(x => x.id == operacionId.Value);
                clienteRedmineIdentificador = op != null ? $"{op.id}-{op.nombre}" : $"Operacion_{operacionId.Value}";
            }
            else
            {
                clienteRedmineIdentificador = string.Empty;
            }

            ActualizarSugerenciasAsunto();
        }

        protected void OnFiltroFechaChanged(string fecha)
        {
            filtroFecha = fecha;
        }

        protected void SeleccionarTodo()
        {
            foreach (var item in filasFiltradas.Where(x => x.Estado != EstadoCreacion.Exitoso))
            {
                item.Seleccionado = true;
            }
        }

        protected void DeseleccionarTodo()
        {
            foreach (var item in filasFiltradas)
            {
                item.Seleccionado = false;
            }
        }

        protected void LimpiarTodo()
        {
            archivoExcel = null;
            filasExcel.Clear();
            fechasDisponibles.Clear();
            filtroFecha = "TODAS";
            totalExitosos = 0;
            totalFallidos = 0;
            progresoActual = 0;
            progresoTotal = 0;
            StateHasChanged();
        }

        protected async Task OnExcelFileSelected(IBrowserFile file)
        {
            if (file == null) return;
            archivoExcel = file;
            estaCargandoArchivo = true;
            filasExcel.Clear();
            fechasDisponibles.Clear();
            totalExitosos = 0;
            totalFallidos = 0;
            StateHasChanged();

            try
            {
                using var ms = new MemoryStream();
                await file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024).CopyToAsync(ms);
                ms.Position = 0;

                IWorkbook workbook = WorkbookFactory.Create(ms);
                IFormulaEvaluator evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

                // Buscar específicamente la pestaña "Consolidado Agrupado", o "Consolidado", o la segunda pestaña
                ISheet? sheet = null;
                for (int s = 0; s < workbook.NumberOfSheets; s++)
                {
                    var sheetName = workbook.GetSheetName(s);
                    if (sheetName.Trim().ToLower().Contains("consolidado agrupado"))
                    {
                        sheet = workbook.GetSheetAt(s);
                        break;
                    }
                }

                if (sheet == null)
                {
                    sheet = workbook.GetSheet("Consolidado") ?? workbook.GetSheetAt(workbook.NumberOfSheets > 1 ? 1 : 0);
                }

                // Buscar fila de encabezados
                IRow? headerRow = null;
                int headerRowIndex = -1;
                for (int r = 0; r <= Math.Min(sheet.LastRowNum, 15); r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    bool tieneArea = false;
                    bool tieneFecha = false;
                    for (int c = 0; c < row.LastCellNum; c++)
                    {
                        var val = row.GetCell(c)?.ToString()?.Trim()?.ToLower() ?? "";
                        if (val.Contains("área") || val.Contains("area")) tieneArea = true;
                        if (val.Contains("fecha")) tieneFecha = true;
                    }

                    if (tieneArea && tieneFecha)
                    {
                        headerRow = row;
                        headerRowIndex = r;
                        break;
                    }
                }

                if (headerRow == null)
                {
                    _snackbar.Add("No se encontró la fila de encabezados con 'Área' y 'Fecha' en la pestaña analizada.", Severity.Error);
                    estaCargandoArchivo = false;
                    return;
                }

                // Mapear columnas
                int colArea = -1;
                int colPuesto = -1;
                int colFecha = -1;
                int colHoraInicio = -1;
                int colHoraFin = -1;
                int colCant = -1;
                int colIds = -1;

                for (int c = 0; c < headerRow.LastCellNum; c++)
                {
                    var val = headerRow.GetCell(c)?.ToString()?.Trim()?.ToLower() ?? "";
                    if (val == "área" || val == "area") colArea = c;
                    else if (val.Contains("puesto") || val.Contains("cargo")) colPuesto = c;
                    else if (val == "fecha") colFecha = c;
                    else if (val.Contains("hora inicio") || val.Contains("hora entrada") || val == "inicio") colHoraInicio = c;
                    else if (val.Contains("hora fin") || val.Contains("hora salida") || val == "fin") colHoraFin = c;
                    else if (val.Contains("cant") || val.Contains("empleados") && !val.Contains("ids")) colCant = c;
                    else if (val.Contains("ids") || val.Contains("separados")) colIds = c;
                }

                // Fallbacks posicionales estándar de la plantilla si no se detectaron todos por nombre
                if (colArea == -1) colArea = 0;
                if (colPuesto == -1) colPuesto = 1;
                if (colFecha == -1) colFecha = 2;
                if (colHoraInicio == -1) colHoraInicio = 3;
                if (colHoraFin == -1) colHoraFin = 4;
                if (colCant == -1) colCant = 5;
                if (colIds == -1) colIds = 6;

                int indexCounter = 1;
                var fechasSet = new HashSet<string>();

                for (int r = headerRowIndex + 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    string area = ExtraerString(row.GetCell(colArea), evaluator);
                    string puesto = ExtraerString(row.GetCell(colPuesto), evaluator);
                    string idsEmpleados = ExtraerString(row.GetCell(colIds), evaluator);
                    string fechaStr = ExtraerFecha(row.GetCell(colFecha), evaluator);
                    string horaInicio = ExtraerHora(row.GetCell(colHoraInicio), evaluator);
                    string horaFin = ExtraerHora(row.GetCell(colHoraFin), evaluator);
                    int cantidad = ExtraerEntero(row.GetCell(colCant), evaluator);

                    // Si no hay área ni IDs, es una fila vacía o de pie
                    if (string.IsNullOrWhiteSpace(area) && string.IsNullOrWhiteSpace(idsEmpleados))
                        continue;

                    // Formatear fecha para Redmine (yyyy-MM-dd) y visualización (dd/MM/yyyy)
                    string fechaRedmine = fechaStr;
                    string fechaVisual = fechaStr;
                    if (DateTime.TryParse(fechaStr, out DateTime dtParsed))
                    {
                        fechaRedmine = dtParsed.ToString("yyyy-MM-dd");
                        fechaVisual = dtParsed.ToString("dd/MM/yyyy");
                    }

                    if (!string.IsNullOrEmpty(fechaRedmine))
                    {
                        fechasSet.Add(fechaRedmine);
                    }

                    // Asunto sugerido
                    string asuntoSugerido = GenerarAsuntoSugerido(area, puesto);

                    var item = new MovimientoGridItem
                    {
                        Index = indexCounter++,
                        Seleccionado = true,
                        Estado = EstadoCreacion.Pendiente,
                        Area = area,
                        Cargo = puesto,
                        Fecha = fechaRedmine,
                        FechaFormateada = fechaVisual,
                        HoraInicio = horaInicio,
                        HoraFin = horaFin,
                        CantidadEmpleados = cantidad > 0 ? cantidad : Math.Max(1, idsEmpleados.Split(',', StringSplitOptions.RemoveEmptyEntries).Length),
                        IdsEmpleados = idsEmpleados,
                        Asunto = asuntoSugerido
                    };

                    filasExcel.Add(item);
                }

                fechasDisponibles = fechasSet.OrderBy(x => x).ToList();

                if (!filasExcel.Any())
                {
                    _snackbar.Add("No se encontraron registros de jornadas en el archivo.", Severity.Warning);
                }
                else
                {
                    _snackbar.Add($"Se cargaron exitosamente {filasExcel.Count} jornadas del consolidado.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al procesar el archivo Excel: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargandoArchivo = false;
                StateHasChanged();
            }
        }

        private string GenerarAsuntoSugerido(string area, string cargo)
        {
            string prefijo = "";
            if (!string.IsNullOrEmpty(clienteRedmineIdentificador))
            {
                var lower = clienteRedmineIdentificador.ToLower();
                if (lower.Contains("walmart")) prefijo = "WM ";
                else if (lower.Contains("sinsa")) prefijo = "SINSA ";
                else if (lower.Contains("condes")) prefijo = "LC ";
                else if (lower.Contains("glacial")) prefijo = "GLACIAL ";
            }

            string limpio = area.Replace("WalMart ", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("Walmart ", "", StringComparison.OrdinalIgnoreCase)
                                .Trim();

            return $"{prefijo}{limpio}".ToUpper();
        }

        private void ActualizarSugerenciasAsunto()
        {
            foreach (var item in filasExcel.Where(x => x.Estado != EstadoCreacion.Exitoso))
            {
                item.Asunto = GenerarAsuntoSugerido(item.Area, item.Cargo);
            }
        }

        protected async Task ConfirmarYCrearMovimientos()
        {
            if (!selectedOperacionId.HasValue || string.IsNullOrEmpty(clienteRedmineIdentificador))
            {
                _snackbar.Add("Debe seleccionar un cliente antes de crear los movimientos en Redmine.", Severity.Warning);
                return;
            }

            var pendientes = filasFiltradas.Where(x => x.Seleccionado && x.Estado != EstadoCreacion.Exitoso).ToList();
            if (!pendientes.Any())
            {
                _snackbar.Add("No hay movimientos seleccionados pendientes de procesar.", Severity.Info);
                return;
            }

            bool? confirmado = await _dialogService.ShowMessageBox(
                "Confirmación de Carga a Redmine",
                $"¿Está seguro de crear {pendientes.Count} tickets de Movimientos de Personal en Redmine para el cliente '{clienteRedmineIdentificador}'?",
                yesText: "Sí, Crear Tickets",
                cancelText: "Cancelar"
            );

            if (confirmado != true) return;

            estaProcesando = true;
            progresoActual = 0;
            progresoTotal = pendientes.Count;
            StateHasChanged();

            try
            {
                // Procesar en lotes de 15 para actualización reactiva fluida
                int batchSize = 15;
                for (int i = 0; i < pendientes.Count; i += batchSize)
                {
                    var chunk = pendientes.Skip(i).Take(batchSize).ToList();
                    foreach (var it in chunk) it.Estado = EstadoCreacion.EnProceso;
                    StateHasChanged();

                    var requestDto = new CrearMovimientosPersonalRequestDto
                    {
                        OperacionId = selectedOperacionId.Value,
                        ClienteRedmine = clienteRedmineIdentificador,
                        Movimientos = chunk.Select(x => new MovimientoPersonalItemDto
                        {
                            FilaIndex = x.Index,
                            Area = x.Area,
                            Cargo = x.Cargo,
                            Fecha = x.Fecha,
                            HoraInicio = x.HoraInicio,
                            HoraFin = x.HoraFin,
                            CantidadEmpleados = x.CantidadEmpleados,
                            IdsEmpleados = x.IdsEmpleados,
                            Asunto = x.Asunto,
                            Observaciones = string.Empty
                        }).ToList()
                    };

                    var apiResponse = await _odooService.CrearMovimientosPersonalMasivo(requestDto);
                    if (apiResponse != null && apiResponse.Model != null && apiResponse.Model.Detalles != null)
                    {
                        foreach (var det in apiResponse.Model.Detalles)
                        {
                            var target = chunk.FirstOrDefault(x => x.Index == det.FilaIndex);
                            if (target != null)
                            {
                                if (det.Exitoso)
                                {
                                    target.Estado = EstadoCreacion.Exitoso;
                                    target.RedmineIssueId = det.RedmineIssueId;
                                    target.MensajeError = null;
                                    target.Seleccionado = false;
                                    totalExitosos++;
                                }
                                else
                                {
                                    target.Estado = EstadoCreacion.Fallido;
                                    target.MensajeError = det.Mensaje;
                                    totalFallidos++;
                                }
                            }
                        }
                    }
                    else
                    {
                        string err = apiResponse?.Respuesta?.MensajeError ?? "Error desconocido en el API al crear lote.";
                        foreach (var target in chunk)
                        {
                            target.Estado = EstadoCreacion.Fallido;
                            target.MensajeError = err;
                            totalFallidos++;
                        }
                    }

                    progresoActual += chunk.Count;
                    StateHasChanged();
                }

                if (totalFallidos == 0)
                {
                    _snackbar.Add($"¡Excelente! Se crearon exitosamente {totalExitosos} tickets en Redmine.", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Proceso finalizado. Creados: {totalExitosos}, Errores: {totalFallidos}. Revisa los estados en la grilla.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error crítico durante la creación en Redmine: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaProcesando = false;
                StateHasChanged();
            }
        }

        #region Helpers de extracción NPOI

        private string ExtraerString(ICell? cell, IFormulaEvaluator evaluator)
        {
            if (cell == null) return string.Empty;

            try
            {
                if (cell.CellType == CellType.Formula)
                {
                    var evalResult = evaluator.Evaluate(cell);
                    if (evalResult != null)
                    {
                        if (evalResult.CellType == CellType.String) return evalResult.StringValue?.Trim() ?? string.Empty;
                        if (evalResult.CellType == CellType.Numeric) return evalResult.NumberValue.ToString(CultureInfo.InvariantCulture);
                    }
                    if (cell.CachedFormulaResultType == CellType.String) return cell.StringCellValue?.Trim() ?? string.Empty;
                    if (cell.CachedFormulaResultType == CellType.Numeric) return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                    return string.Empty;
                }

                if (cell.CellType == CellType.String) return cell.StringCellValue?.Trim() ?? string.Empty;
                if (cell.CellType == CellType.Numeric) return cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
                return cell.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return cell.ToString()?.Trim() ?? string.Empty;
            }
        }

        private string ExtraerFecha(ICell? cell, IFormulaEvaluator evaluator)
        {
            if (cell == null) return string.Empty;

            try
            {
                if (cell.CellType == CellType.Formula)
                {
                    var evalResult = evaluator.Evaluate(cell);
                    if (evalResult != null && evalResult.CellType == CellType.Numeric)
                    {
                        if (DateUtil.IsCellDateFormatted(cell))
                            return cell.DateCellValue.ToString("yyyy-MM-dd");
                        return DateTime.FromOADate(evalResult.NumberValue).ToString("yyyy-MM-dd");
                    }
                    if (cell.CachedFormulaResultType == CellType.Numeric)
                    {
                        if (DateUtil.IsCellDateFormatted(cell))
                            return cell.DateCellValue.ToString("yyyy-MM-dd");
                        return DateTime.FromOADate(cell.NumericCellValue).ToString("yyyy-MM-dd");
                    }
                    return cell.StringCellValue?.Trim() ?? string.Empty;
                }

                if (cell.CellType == CellType.Numeric)
                {
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString("yyyy-MM-dd");
                    return DateTime.FromOADate(cell.NumericCellValue).ToString("yyyy-MM-dd");
                }

                var text = cell.ToString()?.Trim() ?? string.Empty;
                if (DateTime.TryParse(text, out DateTime dt))
                    return dt.ToString("yyyy-MM-dd");
                return text;
            }
            catch
            {
                return cell.ToString()?.Trim() ?? string.Empty;
            }
        }

        private string ExtraerHora(ICell? cell, IFormulaEvaluator evaluator)
        {
            if (cell == null) return string.Empty;

            try
            {
                if (cell.CellType == CellType.Formula)
                {
                    var evalResult = evaluator.Evaluate(cell);
                    if (evalResult != null && evalResult.CellType == CellType.Numeric)
                    {
                        double num = evalResult.NumberValue;
                        if (num < 1.0)
                        {
                            var ts = TimeSpan.FromDays(num);
                            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
                        }
                        if (DateUtil.IsCellDateFormatted(cell))
                            return cell.DateCellValue.ToString("HH:mm");
                    }
                    if (cell.CachedFormulaResultType == CellType.Numeric)
                    {
                        double num = cell.NumericCellValue;
                        if (num < 1.0)
                        {
                            var ts = TimeSpan.FromDays(num);
                            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
                        }
                        if (DateUtil.IsCellDateFormatted(cell))
                            return cell.DateCellValue.ToString("HH:mm");
                    }
                    var sVal = cell.StringCellValue?.Trim() ?? string.Empty;
                    return FormatearHoraTexto(sVal);
                }

                if (cell.CellType == CellType.Numeric)
                {
                    double num = cell.NumericCellValue;
                    if (num < 1.0)
                    {
                        var ts = TimeSpan.FromDays(num);
                        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
                    }
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString("HH:mm");
                }

                return FormatearHoraTexto(cell.ToString()?.Trim() ?? string.Empty);
            }
            catch
            {
                return cell.ToString()?.Trim() ?? string.Empty;
            }
        }

        private string FormatearHoraTexto(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (TimeSpan.TryParse(raw, out TimeSpan ts))
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
            if (DateTime.TryParse(raw, out DateTime dt))
                return dt.ToString("HH:mm");
            return raw;
        }

        private int ExtraerEntero(ICell? cell, IFormulaEvaluator evaluator)
        {
            if (cell == null) return 0;
            try
            {
                if (cell.CellType == CellType.Numeric) return (int)Math.Round(cell.NumericCellValue);
                if (cell.CellType == CellType.Formula)
                {
                    var evalResult = evaluator.Evaluate(cell);
                    if (evalResult != null && evalResult.CellType == CellType.Numeric)
                        return (int)Math.Round(evalResult.NumberValue);
                    if (cell.CachedFormulaResultType == CellType.Numeric)
                        return (int)Math.Round(cell.NumericCellValue);
                    if (int.TryParse(cell.StringCellValue, out int v)) return v;
                }
                if (int.TryParse(cell.ToString(), out int val)) return val;
            }
            catch { }
            return 0;
        }

        #endregion
    }

    public enum EstadoCreacion
    {
        Pendiente,
        EnProceso,
        Exitoso,
        Fallido
    }

    public class MovimientoGridItem
    {
        public int Index { get; set; }
        public bool Seleccionado { get; set; } = true;
        public EstadoCreacion Estado { get; set; } = EstadoCreacion.Pendiente;
        public int? RedmineIssueId { get; set; }
        public string? MensajeError { get; set; }

        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty; // yyyy-MM-dd
        public string FechaFormateada { get; set; } = string.Empty; // dd/MM/yyyy
        public string HoraInicio { get; set; } = string.Empty; // HH:mm
        public string HoraFin { get; set; } = string.Empty; // HH:mm
        public int CantidadEmpleados { get; set; }
        public string IdsEmpleados { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
    }
}
