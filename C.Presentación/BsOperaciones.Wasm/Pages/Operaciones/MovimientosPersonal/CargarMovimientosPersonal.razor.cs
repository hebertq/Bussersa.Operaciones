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
using Modelo.Interfaces;
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
        [Inject] private IUserInfo _userInfo { get; set; } = null!;

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
        protected int totalViaticosCreados = 0;

        protected ModoGeneracionTickets modoSeleccionado = ModoGeneracionTickets.Ambos;

        protected string filtroFecha = "TODAS";
        protected List<string> fechasDisponibles = new();
        protected List<MovimientoGridItem> filasExcel = new();

        protected List<MovimientoGridItem> filasFiltradas => 
            filtroFecha == "TODAS" ? filasExcel : filasExcel.Where(x => x.Fecha == filtroFecha).ToList();

        public static bool EsMontoValido(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            var v = val.Trim();
            if (v == "0" || v == "0.0" || v == "0.00") return false;
            if (decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                return d > 0;
            return true;
        }

        protected int totalMovimientosPendientes => filasFiltradas.Count(x => x.Seleccionado && x.Estado != EstadoCreacion.Exitoso);
        protected int totalViaticosPendientes => filasFiltradas.Count(x => x.Seleccionado && x.TieneViaticos && !x.RedmineViaticosIssueId.HasValue);

        protected bool PuedeCrear => 
            !estaProcesando && 
            selectedOperacionId.HasValue && 
            (
                (modoSeleccionado == ModoGeneracionTickets.Ambos && (totalMovimientosPendientes > 0 || totalViaticosPendientes > 0)) ||
                (modoSeleccionado == ModoGeneracionTickets.SoloMovimientos && totalMovimientosPendientes > 0) ||
                (modoSeleccionado == ModoGeneracionTickets.SoloViaticos && totalViaticosPendientes > 0)
            );

        protected string TextoBotonCrear
        {
            get
            {
                if (estaProcesando) return "Creando tickets en Redmine...";
                if (modoSeleccionado == ModoGeneracionTickets.SoloMovimientos)
                    return $"Crear {totalMovimientosPendientes} Movimientos de Personal";
                if (modoSeleccionado == ModoGeneracionTickets.SoloViaticos)
                    return $"Crear {totalViaticosPendientes} Solicitudes de Viáticos";
                
                if (totalViaticosPendientes > 0)
                    return $"Crear {totalMovimientosPendientes} Movimientos y {totalViaticosPendientes} Viáticos";
                return $"Crear {totalMovimientosPendientes} Movimientos en Redmine";
            }
        }

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
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar operaciones: {ex.Message}", Severity.Warning);
            }
        }

        protected string ObtenerNombreOperacion(int? id)
        {
            if (!id.HasValue) return "-- Seleccione el cliente --";
            var op = operacionesList.FirstOrDefault(x => x.id == id.Value);
            return op != null ? $"{op.id}-{op.nombre}" : $"Cliente {id.Value}";
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
            selectedOperacionId = null;
            clienteRedmineIdentificador = string.Empty;
            filasExcel.Clear();
            fechasDisponibles.Clear();
            filtroFecha = "TODAS";
            totalExitosos = 0;
            totalFallidos = 0;
            totalViaticosCreados = 0;
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
                int colVtcoTrasp = -1;
                int colVtcoAlim = -1;
                int colIds = -1;

                for (int c = 0; c < headerRow.LastCellNum; c++)
                {
                    var val = headerRow.GetCell(c)?.ToString()?.Trim()?.ToLower() ?? "";
                    if (val == "área" || val == "area") colArea = c;
                    else if (val.Contains("puesto") || val.Contains("cargo")) colPuesto = c;
                    else if (val == "fecha") colFecha = c;
                    else if (val.Contains("hora inicio") || val.Contains("hora entrada") || val == "inicio") colHoraInicio = c;
                    else if (val.Contains("hora fin") || val.Contains("hora salida") || val == "fin") colHoraFin = c;
                    else if (val.Contains("trasp") || val.Contains("transporte")) colVtcoTrasp = c;
                    else if (val.Contains("alim") || val.Contains("alimentaci")) colVtcoAlim = c;
                    else if (val.Contains("cant") || (val.Contains("empleados") && !val.Contains("ids"))) colCant = c;
                    else if (val.Contains("ids") || val.Contains("separados")) colIds = c;
                }

                // Fallbacks posicionales estándar si no se detectaron todos por nombre
                if (colArea == -1) colArea = 0;
                if (colPuesto == -1) colPuesto = 1;
                if (colFecha == -1) colFecha = 2;
                if (colHoraInicio == -1) colHoraInicio = 3;
                if (colHoraFin == -1) colHoraFin = 4;
                if (colCant == -1) colCant = 5;

                if (colIds == -1)
                {
                    if (headerRow.LastCellNum >= 9)
                    {
                        if (colVtcoTrasp == -1) colVtcoTrasp = 6;
                        if (colVtcoAlim == -1) colVtcoAlim = 7;
                        colIds = 8;
                    }
                    else
                    {
                        colIds = 6;
                    }
                }
                else if (colIds >= 8)
                {
                    if (colVtcoTrasp == -1) colVtcoTrasp = 6;
                    if (colVtcoAlim == -1) colVtcoAlim = 7;
                }

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
                    string vtcoTrasp = colVtcoTrasp >= 0 ? ExtraerMonto(row.GetCell(colVtcoTrasp), evaluator) : "0";
                    string vtcoAlim = colVtcoAlim >= 0 ? ExtraerMonto(row.GetCell(colVtcoAlim), evaluator) : "0";

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
                        VtcoTrasp = vtcoTrasp,
                        VtcoAlim = vtcoAlim,
                        IdsEmpleados = idsEmpleados,
                        Asunto = asuntoSugerido
                    };

                    filasExcel.Add(item);
                }

                fechasDisponibles = fechasSet.OrderBy(x => x).ToList();

                // Auto-inferir cliente del archivo cargado si aún no está seleccionado
                if (!selectedOperacionId.HasValue && filasExcel.Any())
                {
                    var primeraArea = filasExcel.FirstOrDefault()?.Area?.ToLower() ?? "";
                    var matchOp = operacionesList.FirstOrDefault(op => 
                        !string.IsNullOrEmpty(op.nombre) && 
                        (primeraArea.Contains(op.nombre.ToLower()) || 
                         (op.nombre.ToLower().Contains("walmart") && primeraArea.Contains("walmart")) ||
                         (op.nombre.ToLower().Contains("sinsa") && primeraArea.Contains("sinsa")) ||
                         (op.nombre.ToLower().Contains("condes") && primeraArea.Contains("condes")) ||
                         (op.nombre.ToLower().Contains("glacial") && primeraArea.Contains("glacial"))));

                    if (matchOp != null)
                    {
                        OnOperacionChanged(matchOp.id);
                    }
                }

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

            var pendientes = filasFiltradas.Where(x => 
                x.Seleccionado && 
                (
                    (modoSeleccionado == ModoGeneracionTickets.Ambos && (x.Estado != EstadoCreacion.Exitoso || (x.TieneViaticos && !x.RedmineViaticosIssueId.HasValue))) ||
                    (modoSeleccionado == ModoGeneracionTickets.SoloMovimientos && x.Estado != EstadoCreacion.Exitoso) ||
                    (modoSeleccionado == ModoGeneracionTickets.SoloViaticos && x.TieneViaticos && !x.RedmineViaticosIssueId.HasValue)
                )
            ).ToList();

            if (!pendientes.Any())
            {
                _snackbar.Add("No hay registros pendientes para el modo seleccionado.", Severity.Info);
                return;
            }

            string detalleTexto = modoSeleccionado switch
            {
                ModoGeneracionTickets.SoloMovimientos => $"Crear {pendientes.Count} tickets de Movimientos de Personal",
                ModoGeneracionTickets.SoloViaticos => $"Crear {pendientes.Count(x => x.TieneViaticos)} tickets de Solicitud de Viáticos",
                _ => $"Crear {pendientes.Count} tickets de Movimientos y hasta {pendientes.Count(x => x.TieneViaticos)} tickets de Viáticos"
            };

            bool? confirmado = await _dialogService.ShowMessageBox(
                "Confirmación de Carga a Redmine",
                $"¿Está seguro de {detalleTexto} en Redmine para el cliente '{clienteRedmineIdentificador}'?",
                yesText: "Sí, Procesar",
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
                        ModoGeneracion = modoSeleccionado.ToString(),
                        UsuarioRedmine = _userInfo.RedmineUser ?? _userInfo.UserName ?? "Supervisor",
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
                            Observaciones = string.Empty,
                            VtcoTrasp = x.VtcoTrasp,
                            VtcoAlim = x.VtcoAlim
                        }).ToList()
                    };

                    var apiResponse = await _odooService.CrearMovimientosPersonalMasivo(requestDto);
                    if (apiResponse?.Model?.Detalles != null)
                    {
                        foreach (var det in apiResponse.Model.Detalles)
                        {
                            var target = chunk.FirstOrDefault(x => x.Index == det.FilaIndex);
                            if (target != null)
                            {
                                if (det.Exitoso)
                                {
                                    target.Estado = EstadoCreacion.Exitoso;
                                    if (det.RedmineIssueId.HasValue) target.RedmineIssueId = det.RedmineIssueId;
                                    if (det.RedmineViaticosIssueId.HasValue)
                                    {
                                        target.RedmineViaticosIssueId = det.RedmineViaticosIssueId;
                                        totalViaticosCreados++;
                                    }
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
                    _snackbar.Add($"¡Excelente! Proceso completado exitosamente en Redmine.", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Proceso finalizado. Exitosos: {totalExitosos}, Errores: {totalFallidos}. Revisa la grilla para detalles.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                foreach (var it in pendientes.Where(x => x.Estado == EstadoCreacion.EnProceso))
                {
                    it.Estado = EstadoCreacion.Fallido;
                    it.MensajeError = ex.Message;
                    totalFallidos++;
                }
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

        private string ExtraerMonto(ICell? cell, IFormulaEvaluator evaluator)
        {
            if (cell == null) return "0";
            try
            {
                if (cell.CellType == CellType.Numeric)
                {
                    return cell.NumericCellValue.ToString("0.##", CultureInfo.InvariantCulture);
                }
                if (cell.CellType == CellType.Formula)
                {
                    var res = evaluator.Evaluate(cell);
                    if (res != null && res.CellType == CellType.Numeric)
                        return res.NumberValue.ToString("0.##", CultureInfo.InvariantCulture);
                    if (res != null && res.CellType == CellType.String)
                    {
                        var s = res.StringValue?.Trim() ?? "0";
                        return string.IsNullOrEmpty(s) ? "0" : s;
                    }
                }
                var str = cell.ToString()?.Trim() ?? "0";
                return string.IsNullOrEmpty(str) ? "0" : str;
            }
            catch
            {
                return "0";
            }
        }

        #endregion
    }

    public enum ModoGeneracionTickets
    {
        Ambos,
        SoloMovimientos,
        SoloViaticos
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
        public int? RedmineViaticosIssueId { get; set; }
        public string? MensajeError { get; set; }
        public string? MensajeViaticos { get; set; }

        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty; // yyyy-MM-dd
        public string FechaFormateada { get; set; } = string.Empty; // dd/MM/yyyy
        public string HoraInicio { get; set; } = string.Empty; // HH:mm
        public string HoraFin { get; set; } = string.Empty; // HH:mm
        public int CantidadEmpleados { get; set; }
        public string? VtcoTrasp { get; set; } = "0";
        public string? VtcoAlim { get; set; } = "0";
        public string IdsEmpleados { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;

        public bool TieneViaticos => CargarMovimientosPersonal.EsMontoValido(VtcoTrasp) || CargarMovimientosPersonal.EsMontoValido(VtcoAlim);
    }
}
