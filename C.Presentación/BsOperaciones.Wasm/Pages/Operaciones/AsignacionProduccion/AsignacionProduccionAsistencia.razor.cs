using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Application.Features.Operaciones.Commands;
using BsOperaciones.Application.Features.Operaciones.Queries;
using HostService.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Entidades.Operaciones;
using MudBlazor;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BsOperaciones.Pages.Operaciones.AsignacionProduccion
{
    public partial class AsignacionProduccionAsistencia : ComponentBase
    {
        [Inject] private IMediator _mediator { get; set; } = null!;
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IJSRuntime _jsRuntime { get; set; } = null!;
        [Inject] private IDialogService _dialogService { get; set; } = null!;
        [Inject] private IOdooService _odooService { get; set; } = null!;

        // Estado general y catálogos de apoyo
        protected List<Combos> operacionesList = new();
        protected List<Combos> hrJobsList = new();
        protected List<OdooVariantDto> variantsList = new();
        protected List<Combos> templatesList = new();

        protected bool estaCargando;
        protected string mensajeCarga = "Cargando...";

        // Pestaña 1: Generación Semanal
        protected int operacionId;
        protected DateTime fchaInicioSemana;
        protected DateTime fchaFinSemana;
        protected int numeroSemana;
        protected bool soloPendientes = true;
        protected string filtroDetalle = string.Empty;
        protected bool excluirSupervisores = true;
        protected bool aplicarReglasCobro = true;
        protected HashSet<int> supervisoresIds = new();

        protected List<GrillaProduccionSemanalDto> grillaProduccion = new();
        protected List<MarcadaAsistenciaProduccionDto> marcadasList = new();

        // Totales calculados
        protected decimal TotalHorasOrdinarias => grillaProduccion?.Where(g => g.tipo_hora == "ORDINARIA").Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalHorasExtras => grillaProduccion?.Where(g => g.tipo_hora == "EXTRA").Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalHorasFacturables => grillaProduccion?.Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalMontoVenta => grillaProduccion?.Sum(g => g.subtotal_facturable) ?? 0;

        protected IEnumerable<MarcadaAsistenciaProduccionDto> MarcadasFiltradas
        {
            get
            {
                if (string.IsNullOrWhiteSpace(filtroDetalle)) return marcadasList;
                return marcadasList.Where(m =>
                    (m.nombre_empleado != null && m.nombre_empleado.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)) ||
                    (m.cargo != null && m.cargo.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)) ||
                    (m.area_nombre != null && m.area_nombre.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)) ||
                    (m.numero_asignacion_produccion != null && m.numero_asignacion_produccion.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)));
            }
        }

        // Pestaña 2: Catálogo de Variantes
        protected int catalogoClienteId;
        protected List<CatalogoServicioPuestoAreaDto> catalogoList = new();
        protected List<CatalogoServicioPuestoAreaDto> masterCatalogoList = new();

        protected List<Combos> OperacionesConfiguradas =>
            operacionesList
                .Where(op => masterCatalogoList.Any(c => c.operacion_id == op.id && c.activo))
                .OrderBy(op => op.nombre)
                .ToList();

        // Pestaña 3: Histórico y Fugas
        protected int historicoClienteId;
        protected int historicoAnio = DateTime.Today.Year;
        protected List<HistoricoAsignacionProduccionDto> historicoList = new();

        protected override async Task OnInitializedAsync()
        {
            SetSemana(DateTime.Today);
            await CargarCombosIniciales();
            await CargarSupervisoresConfig();
            await CargarCatalogo();
            await CargarHistorico();

            if (OperacionesConfiguradas.Any())
            {
                operacionId = OperacionesConfiguradas.First().id;
                catalogoClienteId = 0; // Inicia mostrando 'Todas las operaciones configuradas...'
                FiltrarCatalogo();
                historicoClienteId = operacionId;
                await ConsultarSemana();
            }
        }

        private async Task CargarSupervisoresConfig()
        {
            try
            {
                string? json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "catalogo_supervisores_ids");
                if (!string.IsNullOrEmpty(json))
                {
                    var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json);
                    supervisoresIds = ids != null ? new HashSet<int>(ids) : new HashSet<int>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar catalogo_supervisores_ids: " + ex.Message);
            }
        }

        private async Task CargarCombosIniciales()
        {
            estaCargando = true;
            mensajeCarga = "Cargando catálogos operativos y variantes...";
            StateHasChanged();
            try
            {
                var opTask = _mediator.Send(new GetAllCombosQuery("Operaciones"));
                var jobTask = _odooService.GetHrJobs();
                var varTask = _odooService.GetAllProductVariants();
                var tmplTask = _mediator.Send(new GetAllCombosQuery("Plantillas"));

                await Task.WhenAll(opTask, jobTask, varTask, tmplTask);

                var opRes = await opTask;
                operacionesList = opRes.Model?.OrderBy(x => x.nombre).ToList() ?? new();

                var jobRes = await jobTask;
                hrJobsList = jobRes.Model?.OrderBy(x => x.nombre).ToList() ?? new();

                var varRes = await varTask;
                variantsList = varRes.Model?.OrderBy(x => x.default_code).ToList() ?? new();

                // Fallback preventivo si GetAllProductVariants no trajo datos: traer la plantilla de horas laborales (id: 9)
                if (!variantsList.Any())
                {
                    try
                    {
                        var fallbackVarRes = await _odooService.GetProductVariants(9);
                        if (fallbackVarRes.Model != null && fallbackVarRes.Model.Any())
                        {
                            variantsList = fallbackVarRes.Model.OrderBy(x => x.default_code).ToList();
                        }
                    }
                    catch (Exception exVar)
                    {
                        Console.WriteLine("Advertencia fallback variantes: " + exVar.Message);
                    }
                }

                var tmplRes = await tmplTask;
                templatesList = tmplRes.Model?.OrderBy(x => x.nombre).ToList() ?? new();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar catálogos iniciales: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
            }
        }

        #region Lógica de Semanas (Lunes a Domingo)

        protected void SetSemana(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            fchaInicioSemana = date.AddDays(-1 * diff).Date;
            fchaFinSemana = fchaInicioSemana.AddDays(6).Date;
            numeroSemana = ISOWeek.GetWeekOfYear(fchaInicioSemana);
        }

        protected async Task SemanaAnterior()
        {
            SetSemana(fchaInicioSemana.AddDays(-7));
            if (operacionId > 0)
                await ConsultarSemana();
        }

        protected async Task SemanaSiguiente()
        {
            SetSemana(fchaInicioSemana.AddDays(7));
            if (operacionId > 0)
                await ConsultarSemana();
        }

        protected async Task SemanaActual()
        {
            SetSemana(DateTime.Today);
            if (operacionId > 0)
                await ConsultarSemana();
        }

        #endregion

        #region Pestaña 1: Consultas, Sellado y Exportación

        protected async Task ConsultarSemana()
        {
            if (operacionId <= 0)
            {
                _snackbar.Add("Seleccione una operación / cliente primero.", Severity.Warning);
                return;
            }

            estaCargando = true;
            mensajeCarga = "Calculando horas y tarifas semanales...";
            StateHasChanged();
            try
            {
                string? supExcluidosParam = supervisoresIds.Any() ? string.Join(",", supervisoresIds) : null;
                var queryGrilla = new GetGrillaProduccionSemanalQuery(operacionId, fchaInicioSemana, fchaFinSemana, soloPendientes, excluirSupervisores, supExcluidosParam, aplicarReglasCobro);
                var queryMarcadas = new GetMarcadasSemanalesAsignacionQuery(operacionId, fchaInicioSemana, fchaFinSemana, soloPendientes, excluirSupervisores, supExcluidosParam, aplicarReglasCobro);

                var taskGrilla = _mediator.Send(queryGrilla);
                var taskMarcadas = _mediator.Send(queryMarcadas);

                await Task.WhenAll(taskGrilla, taskMarcadas);

                var resGrilla = await taskGrilla;
                grillaProduccion = resGrilla.Model?.ToList() ?? new();

                var resMarcadas = await taskMarcadas;
                marcadasList = resMarcadas.Model?.ToList() ?? new();

                // Marcar seleccionados por defecto los pendientes
                foreach (var m in marcadasList)
                {
                    m.IsSelected = !m.asignado_produccion;
                }

                if (!grillaProduccion.Any())
                {
                    _snackbar.Add("No se encontraron registros de producción para la semana seleccionada.", Severity.Info);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al consultar producción semanal: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
            }
        }

        protected void SeleccionarTodasMarcadas()
        {
            foreach (var m in marcadasList)
            {
                if (!m.asignado_produccion)
                    m.IsSelected = true;
            }
        }

        protected void DeseleccionarTodasMarcadas()
        {
            foreach (var m in marcadasList)
            {
                m.IsSelected = false;
            }
        }

        protected async Task AbrirModalSellarLote()
        {
            var pendientesSeleccionados = marcadasList.Where(m => m.IsSelected && !m.asignado_produccion).ToList();
            if (!pendientesSeleccionados.Any())
            {
                _snackbar.Add("Seleccione al menos una marcada pendiente para sellar el lote.", Severity.Warning);
                return;
            }

            string clienteNombre = operacionesList.FirstOrDefault(o => o.id == operacionId)?.nombre ?? "CLI";
            string clienteCodigo = string.Concat(clienteNombre.Where(char.IsLetterOrDigit)).ToUpper();
            if (clienteCodigo.Length > 8) clienteCodigo = clienteCodigo.Substring(0, 8);

            string defaultLote = $"{clienteCodigo}-SEM{numeroSemana:D2}-{fchaInicioSemana.Year}";

            var parameters = new DialogParameters
            {
                { "NumeroAsignacion", defaultLote },
                { "CantidadMarcadas", pendientesSeleccionados.Count },
                { "TotalHoras", pendientesSeleccionados.Sum(m => m.horas_totales) }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = _dialogService.Show<DialogSellarLote>("Sellar Lote de Producción", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string numeroLote && !string.IsNullOrWhiteSpace(numeroLote))
            {
                estaCargando = true;
                mensajeCarga = $"Sellando lote {numeroLote}...";
                StateHasChanged();
                try
                {
                    var req = new AsignarMarcadasProduccionRequest
                    {
                        ids = pendientesSeleccionados.Select(m => m.id_cierre).ToList(),
                        numero_asignacion = numeroLote,
                        usuario_id = 0
                    };

                    var response = await _mediator.Send(new AsignarMarcadasProduccionCommand(req));
                    if (response.Respuesta.ExisteError)
                    {
                        _snackbar.Add($"Error al sellar lote: {response.Respuesta.MensajeError}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add($"¡Lote {numeroLote} sellado exitosamente con {req.ids.Count} marcadas!", Severity.Success);
                        await ConsultarSemana();
                        await CargarHistorico();
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error en el proceso de sellado: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }

        protected async Task ExportarExcel()
        {
            if (grillaProduccion == null || !grillaProduccion.Any())
            {
                _snackbar.Add("No hay registros en la grilla para exportar.", Severity.Warning);
                return;
            }

            try
            {
                var workbook = new XSSFWorkbook();

                // Formatos
                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerFont.Color = NPOI.HSSF.Util.HSSFColor.White.Index;

                var headerStyle = workbook.CreateCellStyle();
                headerStyle.SetFont(headerFont);
                headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.RoyalBlue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;

                var currencyStyle = workbook.CreateCellStyle();
                var dataFormat = workbook.CreateDataFormat();
                currencyStyle.DataFormat = dataFormat.GetFormat("$#,##0.00;($#,##0.00);\"-\"");

                var numberStyle = workbook.CreateCellStyle();
                numberStyle.DataFormat = dataFormat.GetFormat("#,##0.00");

                // Hoja 1: Resumen Producción
                var sheetProd = workbook.CreateSheet("Produccion_Semanal");
                var headers1 = new[] { "Cliente", "Área", "Cargo", "Turno", "Tipo Hora", "SKU Servicio", "Descripción Servicio", "Unidad Medida", "Tarifa Venta ($)", "Horas", "Total Venta ($)", "Cant. Empleados" };
                var rowH1 = sheetProd.CreateRow(0);
                for (int i = 0; i < headers1.Length; i++)
                {
                    var cell = rowH1.CreateCell(i);
                    cell.SetCellValue(headers1[i]);
                    cell.CellStyle = headerStyle;
                }

                int r1 = 1;
                foreach (var g in grillaProduccion)
                {
                    var row = sheetProd.CreateRow(r1++);
                    row.CreateCell(0).SetCellValue(g.cliente_nombre);
                    row.CreateCell(1).SetCellValue(g.area_cliente);
                    row.CreateCell(2).SetCellValue(g.cargo);
                    row.CreateCell(3).SetCellValue(g.turno ?? "Diurno");
                    row.CreateCell(4).SetCellValue(g.tipo_hora);
                    row.CreateCell(5).SetCellValue(g.servicio_codigo);
                    row.CreateCell(6).SetCellValue(g.servicio_descripcion);
                    row.CreateCell(7).SetCellValue(g.unidad_medida);

                    var c8 = row.CreateCell(8);
                    c8.SetCellValue((double)g.tarifa_venta);
                    c8.CellStyle = currencyStyle;

                    var c9 = row.CreateCell(9);
                    c9.SetCellValue((double)g.cantidad_horas);
                    c9.CellStyle = numberStyle;

                    var c10 = row.CreateCell(10);
                    c10.SetCellValue((double)g.subtotal_facturable);
                    c10.CellStyle = currencyStyle;

                    row.CreateCell(11).SetCellValue(g.cantidad_empleados);
                }

                for (int i = 0; i < headers1.Length; i++) sheetProd.AutoSizeColumn(i);

                // Hoja 2: Detalle Marcadas Asistencia
                var sheetMarcadas = workbook.CreateSheet("Detalle_Asistencias");
                var headers2 = new[] { "ID Cierre", "Fecha", "Empleado", "Es Supervisor", "Cargo", "Área", "Tipo Empleado", "Turno", "Entrada", "Salida", "Horas Ord", "Tarifa Ord", "Horas Ext", "Tarifa Ext", "Total Horas", "Regla Cobro", "Asignado Producción", "N° Asignación / Lote", "Tiene Regla" };
                var rowH2 = sheetMarcadas.CreateRow(0);
                for (int i = 0; i < headers2.Length; i++)
                {
                    var cell = rowH2.CreateCell(i);
                    cell.SetCellValue(headers2[i]);
                    cell.CellStyle = headerStyle;
                }

                int r2 = 1;
                foreach (var m in marcadasList)
                {
                    var row = sheetMarcadas.CreateRow(r2++);
                    row.CreateCell(0).SetCellValue(m.id_cierre);
                    row.CreateCell(1).SetCellValue(m.fecha_asistencia.ToString("dd/MM/yyyy"));
                    row.CreateCell(2).SetCellValue(m.nombre_empleado);
                    row.CreateCell(3).SetCellValue(m.es_supervisor ? "SÍ" : "NO");
                    row.CreateCell(4).SetCellValue(m.cargo);
                    row.CreateCell(5).SetCellValue(m.area_nombre);
                    row.CreateCell(6).SetCellValue(m.tipo_empleado);
                    row.CreateCell(7).SetCellValue(m.turno ?? "Diurno");
                    row.CreateCell(8).SetCellValue(m.entrada_movimiento);
                    row.CreateCell(9).SetCellValue(m.salida_movimiento);

                    var c10 = row.CreateCell(10);
                    c10.SetCellValue((double)m.horas_ordinarias);
                    c10.CellStyle = numberStyle;

                    var c11 = row.CreateCell(11);
                    c11.SetCellValue((double)m.tarifa_ord);
                    c11.CellStyle = currencyStyle;

                    var c12 = row.CreateCell(12);
                    c12.SetCellValue((double)m.horas_extras);
                    c12.CellStyle = numberStyle;

                    var c13 = row.CreateCell(13);
                    c13.SetCellValue((double)m.tarifa_ext);
                    c13.CellStyle = currencyStyle;

                    var c14 = row.CreateCell(14);
                    c14.SetCellValue((double)m.horas_totales);
                    c14.CellStyle = numberStyle;

                    row.CreateCell(15).SetCellValue(m.regla_aplicada ?? "");
                    row.CreateCell(16).SetCellValue(m.asignado_produccion ? "SÍ" : "NO");
                    row.CreateCell(17).SetCellValue(m.numero_asignacion_produccion ?? "");
                    row.CreateCell(18).SetCellValue(m.tiene_regla_catalogo ? "SÍ" : "NO");
                }

                for (int i = 0; i < headers2.Length; i++) sheetMarcadas.AutoSizeColumn(i);

                using var ms = new MemoryStream();
                workbook.Write(ms);
                byte[] bytes = ms.ToArray();
                string base64 = Convert.ToBase64String(bytes);

                string clienteNombre = operacionesList.FirstOrDefault(o => o.id == operacionId)?.nombre ?? "Cliente";
                string fileName = $"Produccion_Semanal_{clienteNombre}_Sem{numeroSemana}_{fchaInicioSemana:yyyyMMdd}.xlsx";

                await _jsRuntime.InvokeVoidAsync("downloadFile", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64, fileName);
                _snackbar.Add("Archivo Excel exportado exitosamente.", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al exportar Excel: {ex.Message}", Severity.Error);
            }
        }

        #endregion

        #region Pestaña 2: Catálogo de Variantes

        protected void OnCatalogoClienteChanged(int newClientId)
        {
            catalogoClienteId = newClientId;
            FiltrarCatalogo();
            StateHasChanged();
        }

        private void FiltrarCatalogo()
        {
            if (catalogoClienteId > 0)
            {
                catalogoList = masterCatalogoList
                    .Where(c => c.operacion_id == catalogoClienteId)
                    .OrderBy(c => c.cliente_nombre).ThenBy(c => c.cargo).ThenBy(c => c.tipo_hora).ToList();
            }
            else
            {
                catalogoList = masterCatalogoList
                    .OrderBy(c => c.cliente_nombre).ThenBy(c => c.cargo).ThenBy(c => c.tipo_hora).ToList();
            }
        }

        protected async Task CargarCatalogo()
        {
            estaCargando = true;
            mensajeCarga = "Cargando reglas del catálogo de variantes...";
            StateHasChanged();
            try
            {
                var res = await _mediator.Send(new GetCatalogoServicioPuestoAreaQuery(0));
                masterCatalogoList = res.Model?.Where(c => c.activo).ToList() ?? new();

                FiltrarCatalogo();

                if (OperacionesConfiguradas.Any() && !OperacionesConfiguradas.Any(o => o.id == operacionId))
                {
                    operacionId = OperacionesConfiguradas.First().id;
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar catálogo de variantes: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
            }
        }

        protected async Task AbrirModalNuevaRegla()
        {
            var nuevaRegla = new CatalogoServicioPuestoAreaDto
            {
                operacion_id = catalogoClienteId > 0 ? catalogoClienteId : (operacionesList.FirstOrDefault()?.id ?? 0),
                area_nombre = "*",
                turno = "*",
                tipo_hora = "ORDINARIA",
                unidad_medida = "HORAS",
                factor_conversion_dias = 8.00m,
                activo = true
            };

            var parameters = new DialogParameters
            {
                { "Item", nuevaRegla },
                { "OperacionesList", operacionesList },
                { "HrJobsList", hrJobsList },
                { "VariantsList", variantsList },
                { "TemplatesList", templatesList }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = _dialogService.Show<DialogReglaCatalogo>("Nueva Regla de Tarifa y Variante", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is CatalogoServicioPuestoAreaDto itemParaGuardar)
            {
                await GuardarRegla(itemParaGuardar);
            }
        }

        protected async Task AbrirModalEditarRegla(CatalogoServicioPuestoAreaDto item)
        {
            var parameters = new DialogParameters
            {
                { "Item", item },
                { "OperacionesList", operacionesList },
                { "HrJobsList", hrJobsList },
                { "VariantsList", variantsList },
                { "TemplatesList", templatesList }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = _dialogService.Show<DialogReglaCatalogo>("Editar Regla de Tarifa y Variante", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is CatalogoServicioPuestoAreaDto itemParaGuardar)
            {
                await GuardarRegla(itemParaGuardar);
            }
        }

        private async Task GuardarRegla(CatalogoServicioPuestoAreaDto regla)
        {
            estaCargando = true;
            mensajeCarga = "Guardando regla de tarifa...";
            StateHasChanged();
            try
            {
                var response = await _mediator.Send(new SaveCatalogoServicioPuestoAreaCommand(regla));
                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al guardar regla: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    _snackbar.Add("Regla de servicio guardada exitosamente.", Severity.Success);
                    await CargarCatalogo();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Excepción al guardar regla: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
            }
        }

        protected async Task EliminarRegla(int id)
        {
            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Eliminación",
                "¿Está seguro de eliminar esta regla de tarifa y variante de servicio?",
                yesText: "Eliminar", cancelText: "Cancelar");

            if (confirm == true)
            {
                estaCargando = true;
                mensajeCarga = "Eliminando regla...";
                StateHasChanged();
                try
                {
                    var response = await _mediator.Send(new DeleteCatalogoServicioPuestoAreaCommand(id));
                    if (response.Respuesta.ExisteError)
                    {
                        _snackbar.Add($"Error al eliminar regla: {response.Respuesta.MensajeError}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add("Regla eliminada exitosamente.", Severity.Success);
                        await CargarCatalogo();
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error al eliminar regla: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }

        #endregion

        #region Pestaña 3: Trazabilidad, Lotes e Incidencias (Fugas)

        protected async Task CargarHistorico()
        {
            estaCargando = true;
            mensajeCarga = "Cargando auditoría histórica de lotes y fugas...";
            StateHasChanged();
            try
            {
                var res = await _mediator.Send(new GetHistoricoAsignacionesProduccionQuery(historicoClienteId, historicoAnio));
                historicoList = res.Model?.OrderByDescending(h => h.fecha_min).ToList() ?? new();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar histórico de asignaciones: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
            }
        }

        protected async Task DesasignarLote(string numeroAsignacion)
        {
            if (string.IsNullOrWhiteSpace(numeroAsignacion)) return;

            bool? confirm = await _dialogService.ShowMessageBox(
                "Revertir Asignación de Lote",
                $"¿Está seguro de desasignar y liberar las horas del lote '{numeroAsignacion}'? Las marcadas volverán a quedar pendientes para ser recalculadas o reasignadas.",
                yesText: "Sí, Desasignar", cancelText: "Cancelar");

            if (confirm == true)
            {
                estaCargando = true;
                mensajeCarga = $"Desasignando lote {numeroAsignacion}...";
                StateHasChanged();
                try
                {
                    var response = await _mediator.Send(new DesasignarMarcadasProduccionCommand(numeroAsignacion));
                    if (response.Respuesta.ExisteError)
                    {
                        _snackbar.Add($"Error al desasignar lote: {response.Respuesta.MensajeError}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add($"Lote '{numeroAsignacion}' revertido correctamente. Las horas están disponibles nuevamente.", Severity.Success);
                        await CargarHistorico();
                        if (operacionId > 0)
                            await ConsultarSemana();
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error al desasignar: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }

        protected async Task IrAFacturarFugas(HistoricoAsignacionProduccionDto lote)
        {
            operacionId = lote.operacion_id;
            SetSemana(lote.fecha_min);
            soloPendientes = true;
            await ConsultarSemana();
            _snackbar.Add($"Se cargaron las horas pendientes huérfanas de la semana del {lote.fecha_min:dd/MM/yyyy} al {lote.fecha_max:dd/MM/yyyy}.", Severity.Info);
        }

        #endregion
    }
}
