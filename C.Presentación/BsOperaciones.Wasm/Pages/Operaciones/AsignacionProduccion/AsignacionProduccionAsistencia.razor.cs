using BsOperaciones.Application.Features.Odoo.Commands;
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

        // Propiedades de Semana Encajonada (corte bimensual)
        protected bool EsSemanaEncajonada => fchaInicioSemana.Month != fchaFinSemana.Month;
        protected DateTime FechaFinMesAnterior => new DateTime(fchaInicioSemana.Year, fchaInicioSemana.Month, DateTime.DaysInMonth(fchaInicioSemana.Year, fchaInicioSemana.Month));
        protected DateTime FechaInicioMesActual => new DateTime(fchaFinSemana.Year, fchaFinSemana.Month, 1);
        protected string NombreMesAnterior => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fchaInicioSemana.ToString("MMMM", CultureInfo.GetCultureInfo("es-ES")));
        protected string NombreMesActual => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fchaFinSemana.ToString("MMMM", CultureInfo.GetCultureInfo("es-ES")));
        protected string PeriodoMesAnteriorTexto => $"{fchaInicioSemana:dd/MM} al {FechaFinMesAnterior:dd/MM}";
        protected string PeriodoMesActualTexto => $"{FechaInicioMesActual:dd/MM} al {fchaFinSemana:dd/MM}";

        // Filtro de visualización temporal: "TODOS", "MES_ANTERIOR", "MES_ACTUAL"
        protected string filtroPeriodoCorte = "TODOS";

        protected IEnumerable<GrillaProduccionSemanalDto> GrillaFiltrada
        {
            get
            {
                if (grillaProduccion == null) return Enumerable.Empty<GrillaProduccionSemanalDto>();
                if (!EsSemanaEncajonada || filtroPeriodoCorte == "TODOS") return grillaProduccion;
                if (filtroPeriodoCorte == "MES_ANTERIOR") return grillaProduccion.Where(g => g.es_mes_anterior);
                if (filtroPeriodoCorte == "MES_ACTUAL") return grillaProduccion.Where(g => !g.es_mes_anterior);
                return grillaProduccion;
            }
        }

        // Totales calculados generales
        protected decimal TotalHorasOrdinarias => grillaProduccion?.Where(g => g.tipo_hora == "ORDINARIA").Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalHorasExtras => grillaProduccion?.Where(g => g.tipo_hora == "EXTRA").Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalHorasFacturables => grillaProduccion?.Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalMontoVenta => grillaProduccion?.Sum(g => g.subtotal_facturable) ?? 0;

        // Desglose de Totales por Mes para Semanas Encajonadas
        protected decimal TotalHorasMesAnterior => grillaProduccion?.Where(g => g.es_mes_anterior).Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalMontoMesAnterior => grillaProduccion?.Where(g => g.es_mes_anterior).Sum(g => g.subtotal_facturable) ?? 0;
        protected decimal TotalHorasMesActual => grillaProduccion?.Where(g => !g.es_mes_anterior).Sum(g => g.cantidad_horas) ?? 0;
        protected decimal TotalMontoMesActual => grillaProduccion?.Where(g => !g.es_mes_anterior).Sum(g => g.subtotal_facturable) ?? 0;

        protected IEnumerable<MarcadaAsistenciaProduccionDto> MarcadasFiltradas
        {
            get
            {
                var query = marcadasList.AsEnumerable();
                if (EsSemanaEncajonada && filtroPeriodoCorte == "MES_ANTERIOR")
                {
                    query = query.Where(m => m.fecha_asistencia.Month == fchaInicioSemana.Month);
                }
                else if (EsSemanaEncajonada && filtroPeriodoCorte == "MES_ACTUAL")
                {
                    query = query.Where(m => m.fecha_asistencia.Month == fchaFinSemana.Month);
                }

                if (!string.IsNullOrWhiteSpace(filtroDetalle))
                {
                    query = query.Where(m =>
                        (m.nombre_empleado != null && m.nombre_empleado.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)) ||
                        (m.cargo != null && m.cargo.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)) ||
                        (m.area_nombre != null && m.area_nombre.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)) ||
                        (m.numero_asignacion_produccion != null && m.numero_asignacion_produccion.Contains(filtroDetalle, StringComparison.InvariantCultureIgnoreCase)));
                }

                return query;
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

                if (EsSemanaEncajonada)
                {
                    // Semana Encajonada: Consultar período 1 (días del mes anterior) y período 2 (días del mes actual)
                    var queryGrillaP1 = new GetGrillaProduccionSemanalQuery(operacionId, fchaInicioSemana, FechaFinMesAnterior, soloPendientes, excluirSupervisores, supExcluidosParam, aplicarReglasCobro);
                    var queryGrillaP2 = new GetGrillaProduccionSemanalQuery(operacionId, FechaInicioMesActual, fchaFinSemana, soloPendientes, excluirSupervisores, supExcluidosParam, aplicarReglasCobro);
                    var queryMarcadas = new GetMarcadasSemanalesAsignacionQuery(operacionId, fchaInicioSemana, fchaFinSemana, soloPendientes, excluirSupervisores, supExcluidosParam, aplicarReglasCobro);

                    var taskGrillaP1 = _mediator.Send(queryGrillaP1);
                    var taskGrillaP2 = _mediator.Send(queryGrillaP2);
                    var taskMarcadas = _mediator.Send(queryMarcadas);

                    await Task.WhenAll(taskGrillaP1, taskGrillaP2, taskMarcadas);

                    var resGrillaP1 = await taskGrillaP1;
                    var resGrillaP2 = await taskGrillaP2;

                    var itemsP1 = resGrillaP1.Model?.ToList() ?? new();
                    foreach (var item in itemsP1)
                    {
                        item.fecha_inicio_corte = fchaInicioSemana;
                        item.fecha_fin_corte = FechaFinMesAnterior;
                        item.periodo_mes = $"{NombreMesAnterior} ({PeriodoMesAnteriorTexto})";
                        item.es_mes_anterior = true;
                    }

                    var itemsP2 = resGrillaP2.Model?.ToList() ?? new();
                    foreach (var item in itemsP2)
                    {
                        item.fecha_inicio_corte = FechaInicioMesActual;
                        item.fecha_fin_corte = fchaFinSemana;
                        item.periodo_mes = $"{NombreMesActual} ({PeriodoMesActualTexto})";
                        item.es_mes_anterior = false;
                    }

                    grillaProduccion = itemsP1.Concat(itemsP2).ToList();

                    var resMarcadas = await taskMarcadas;
                    marcadasList = resMarcadas.Model?.ToList() ?? new();
                }
                else
                {
                    var queryGrilla = new GetGrillaProduccionSemanalQuery(operacionId, fchaInicioSemana, fchaFinSemana, soloPendientes, excluirSupervisores, supExcluidosParam, aplicarReglasCobro);
                    var queryMarcadas = new GetMarcadasSemanalesAsignacionQuery(operacionId, fchaInicioSemana, fchaFinSemana, soloPendientes, excluirSupervisores, supExcluidosParam, aplicarReglasCobro);

                    var taskGrilla = _mediator.Send(queryGrilla);
                    var taskMarcadas = _mediator.Send(queryMarcadas);

                    await Task.WhenAll(taskGrilla, taskMarcadas);

                    var resGrilla = await taskGrilla;
                    grillaProduccion = resGrilla.Model?.ToList() ?? new();
                    foreach (var item in grillaProduccion)
                    {
                        item.fecha_inicio_corte = fchaInicioSemana;
                        item.fecha_fin_corte = fchaFinSemana;
                        item.periodo_mes = $"{NombreMesActual} ({fchaInicioSemana:dd/MM} al {fchaFinSemana:dd/MM})";
                        item.es_mes_anterior = false;
                    }

                    var resMarcadas = await taskMarcadas;
                    marcadasList = resMarcadas.Model?.ToList() ?? new();
                }

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
            foreach (var m in MarcadasFiltradas)
            {
                if (!m.asignado_produccion)
                    m.IsSelected = true;
            }
        }

        protected void SeleccionarMarcadasMesAnterior()
        {
            foreach (var m in marcadasList)
            {
                if (!m.asignado_produccion && m.fecha_asistencia.Month == fchaInicioSemana.Month)
                    m.IsSelected = true;
            }
        }

        protected void SeleccionarMarcadasMesActual()
        {
            foreach (var m in marcadasList)
            {
                if (!m.asignado_produccion && m.fecha_asistencia.Month == fchaFinSemana.Month)
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

            var pendientesMesAnterior = pendientesSeleccionados.Where(m => m.fecha_asistencia.Month == fchaInicioSemana.Month).ToList();
            var pendientesMesActual = pendientesSeleccionados.Where(m => m.fecha_asistencia.Month == fchaFinSemana.Month).ToList();
            bool esDual = EsSemanaEncajonada && pendientesMesAnterior.Any() && pendientesMesActual.Any();

            string defaultLote = $"{clienteCodigo}-SEM{numeroSemana:D2}-{fchaInicioSemana.Year}";
            string defaultLoteP1 = $"{clienteCodigo}-SEM{numeroSemana:D2}-{fchaInicioSemana:MMM.ToUpper()}-{fchaInicioSemana.Year}";
            string defaultLoteP2 = $"{clienteCodigo}-SEM{numeroSemana:D2}-{fchaFinSemana:MMM.ToUpper()}-{fchaFinSemana.Year}";

            var parameters = new DialogParameters
            {
                { "NumeroAsignacion", defaultLote },
                { "CantidadMarcadas", pendientesSeleccionados.Count },
                { "TotalHoras", pendientesSeleccionados.Sum(m => m.horas_totales) },
                { "EsEncajonada", esDual },
                { "NumeroAsignacionMesAnterior", defaultLoteP1 },
                { "CantidadMarcadasMesAnterior", pendientesMesAnterior.Count },
                { "TotalHorasMesAnterior", pendientesMesAnterior.Sum(m => m.horas_totales) },
                { "NombreMesAnterior", NombreMesAnterior },
                { "PeriodoMesAnterior", PeriodoMesAnteriorTexto },
                { "NumeroAsignacionMesActual", defaultLoteP2 },
                { "CantidadMarcadasMesActual", pendientesMesActual.Count },
                { "TotalHorasMesActual", pendientesMesActual.Sum(m => m.horas_totales) },
                { "NombreMesActual", NombreMesActual },
                { "PeriodoMesActual", PeriodoMesActualTexto }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = _dialogService.Show<DialogSellarLote>("Sellar Lote de Producción", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data != null)
            {
                estaCargando = true;
                StateHasChanged();
                try
                {
                    if (result.Data is ResultadoSellarLoteDto resLote)
                    {
                        if (resLote.EsEncajonada)
                        {
                            mensajeCarga = $"Sellando lotes {resLote.LoteMesAnterior} y {resLote.LoteMesActual}...";
                            StateHasChanged();

                            var reqP1 = new AsignarMarcadasProduccionRequest
                            {
                                ids = pendientesMesAnterior.Select(m => m.id_cierre).ToList(),
                                numero_asignacion = resLote.LoteMesAnterior,
                                usuario_id = 0
                            };
                            var respP1 = await _mediator.Send(new AsignarMarcadasProduccionCommand(reqP1));

                            var reqP2 = new AsignarMarcadasProduccionRequest
                            {
                                ids = pendientesMesActual.Select(m => m.id_cierre).ToList(),
                                numero_asignacion = resLote.LoteMesActual,
                                usuario_id = 0
                            };
                            var respP2 = await _mediator.Send(new AsignarMarcadasProduccionCommand(reqP2));

                            if (respP1.Respuesta.ExisteError || respP2.Respuesta.ExisteError)
                            {
                                string err = respP1.Respuesta.MensajeError ?? respP2.Respuesta.MensajeError ?? "Error desconocido";
                                _snackbar.Add($"Error al sellar lotes: {err}", Severity.Error);
                            }
                            else
                            {
                                _snackbar.Add($"¡Lotes sellados exitosamente: {resLote.LoteMesAnterior} ({pendientesMesAnterior.Count} marcadas) y {resLote.LoteMesActual} ({pendientesMesActual.Count} marcadas)!", Severity.Success);
                                await ConsultarSemana();
                                await CargarHistorico();
                            }
                        }
                        else
                        {
                            mensajeCarga = $"Sellando lote {resLote.LoteUnico}...";
                            StateHasChanged();

                            var req = new AsignarMarcadasProduccionRequest
                            {
                                ids = pendientesSeleccionados.Select(m => m.id_cierre).ToList(),
                                numero_asignacion = resLote.LoteUnico,
                                usuario_id = 0
                            };
                            var response = await _mediator.Send(new AsignarMarcadasProduccionCommand(req));
                            if (response.Respuesta.ExisteError)
                            {
                                _snackbar.Add($"Error al sellar lote: {response.Respuesta.MensajeError}", Severity.Error);
                            }
                            else
                            {
                                _snackbar.Add($"¡Lote {resLote.LoteUnico} sellado exitosamente con {req.ids.Count} marcadas!", Severity.Success);
                                await ConsultarSemana();
                                await CargarHistorico();
                            }
                        }
                    }
                    else if (result.Data is string strLote && !string.IsNullOrWhiteSpace(strLote))
                    {
                        mensajeCarga = $"Sellando lote {strLote}...";
                        StateHasChanged();

                        var req = new AsignarMarcadasProduccionRequest
                        {
                            ids = pendientesSeleccionados.Select(m => m.id_cierre).ToList(),
                            numero_asignacion = strLote,
                            usuario_id = 0
                        };
                        var response = await _mediator.Send(new AsignarMarcadasProduccionCommand(req));
                        if (response.Respuesta.ExisteError)
                        {
                            _snackbar.Add($"Error al sellar lote: {response.Respuesta.MensajeError}", Severity.Error);
                        }
                        else
                        {
                            _snackbar.Add($"¡Lote {strLote} sellado exitosamente con {req.ids.Count} marcadas!", Severity.Success);
                            await ConsultarSemana();
                            await CargarHistorico();
                        }
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

        protected async Task IngresarProduccionDiariaDirecto()
        {
            if (grillaProduccion == null || !grillaProduccion.Any())
            {
                _snackbar.Add("No hay registros en la grilla para ingresar a producción.", Severity.Warning);
                return;
            }

            string clienteNombre = operacionesList.FirstOrDefault(o => o.id == operacionId)?.nombre ?? "Cliente";

            var msgs = new List<string>();
            if (EsSemanaEncajonada)
            {
                var p1 = grillaProduccion.Where(g => g.es_mes_anterior).ToList();
                var p2 = grillaProduccion.Where(g => !g.es_mes_anterior).ToList();
                msgs.Add($"<b>Semana Encajonada:</b> Se ingresarán <b>{grillaProduccion.Count} registros</b> de producción divididos en sus dos meses correspondientes:");
                msgs.Add($"• <b>{NombreMesAnterior} ({PeriodoMesAnteriorTexto}):</b> {p1.Count} ítems, {p1.Sum(x => x.cantidad_horas):N2} horas, $ {p1.Sum(x => x.subtotal_facturable):N2}");
                msgs.Add($"• <b>{NombreMesActual} ({PeriodoMesActualTexto}):</b> {p2.Count} ítems, {p2.Sum(x => x.cantidad_horas):N2} horas, $ {p2.Sum(x => x.subtotal_facturable):N2}");
            }
            else
            {
                msgs.Add($"Se ingresarán <b>{grillaProduccion.Count} registros</b> de producción para {clienteNombre} ({fchaInicioSemana:dd/MM/yyyy} - {fchaFinSemana:dd/MM/yyyy}), totalizando {TotalHorasFacturables:N2} horas.");
            }

            bool? confirmar = await _dialogService.ShowMessageBox(
                "Confirmar Ingreso a Producción Diaria",
                (MarkupString)string.Join("<br/>", msgs),
                yesText: "Ingresar Producción", cancelText: "Cancelar");

            if (confirmar != true) return;

            estaCargando = true;
            mensajeCarga = "Ingresando producción a Odoo/Producción Diaria...";
            StateHasChanged();
            try
            {
                var items = new List<ProduccionDiariaDto>();
                foreach (var g in grillaProduccion)
                {
                    DateTime fInicio = g.fecha_inicio_corte ?? fchaInicioSemana;
                    DateTime fFin = g.fecha_fin_corte ?? fchaFinSemana;

                    string clienteCodigo = string.Concat(clienteNombre.Where(char.IsLetterOrDigit)).ToUpper();
                    if (clienteCodigo.Length > 8) clienteCodigo = clienteCodigo.Substring(0, 8);
                    string mesTag = fInicio.ToString("MMM", CultureInfo.InvariantCulture).ToUpper();
                    string loteSugerido = $"{clienteCodigo}-SEM{numeroSemana:D2}-{mesTag}-{fInicio.Year}";

                    items.Add(new ProduccionDiariaDto
                    {
                        cliente = g.cliente_nombre,
                        area_cliente = g.area_cliente,
                        fecha_inicio = fInicio,
                        fecha_fin = fFin,
                        servicio_codigo = g.servicio_codigo,
                        servicio_descripcion = g.servicio_descripcion,
                        nombre_producto = g.servicio_descripcion,
                        cantidad_producto = g.cantidad_horas,
                        costo_producto = g.tarifa_venta,
                        no_lote = loteSugerido,
                        operacion_id = operacionId,
                        actividad = $"{g.cargo} ({g.turno}) - {g.tipo_hora}"
                    });
                }

                var response = await _mediator.Send(new ImportarProduccionDiariaCommand(items));
                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al registrar producción: {response.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    _snackbar.Add($"¡Se ingresaron {items.Count} registros de producción exitosamente!", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error en el registro de producción: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
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

                // Hoja 1: Resumen Producción Segregado por Período de Corte
                var sheetProd = workbook.CreateSheet("Produccion_Semanal");
                var headers1 = new[] { 
                    "Fecha Inicio (YYYY-MM-DD)", "Fecha Fin (YYYY-MM-DD)", "Mes Corte", "Cliente", "Área de Cliente", 
                    "Cargo", "Turno", "Tipo Hora", "Código Servicio (SKU)", "Descripción Servicio", 
                    "Unidad Medida", "Tarifa Venta ($)", "Horas", "Total Venta ($)", "Cant. Empleados" 
                };

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
                    DateTime fIni = g.fecha_inicio_corte ?? fchaInicioSemana;
                    DateTime fFin = g.fecha_fin_corte ?? fchaFinSemana;
                    string periodo = g.periodo_mes ?? (g.es_mes_anterior ? NombreMesAnterior : NombreMesActual);

                    row.CreateCell(0).SetCellValue(fIni.ToString("yyyy-MM-dd"));
                    row.CreateCell(1).SetCellValue(fFin.ToString("yyyy-MM-dd"));
                    row.CreateCell(2).SetCellValue(periodo);
                    row.CreateCell(3).SetCellValue(g.cliente_nombre);
                    row.CreateCell(4).SetCellValue(g.area_cliente);
                    row.CreateCell(5).SetCellValue(g.cargo);
                    row.CreateCell(6).SetCellValue(g.turno ?? "Diurno");
                    row.CreateCell(7).SetCellValue(g.tipo_hora);
                    row.CreateCell(8).SetCellValue(g.servicio_codigo);
                    row.CreateCell(9).SetCellValue(g.servicio_descripcion);
                    row.CreateCell(10).SetCellValue(g.unidad_medida);

                    var c11 = row.CreateCell(11);
                    c11.SetCellValue((double)g.tarifa_venta);
                    c11.CellStyle = currencyStyle;

                    var c12 = row.CreateCell(12);
                    c12.SetCellValue((double)g.cantidad_horas);
                    c12.CellStyle = numberStyle;

                    var c13 = row.CreateCell(13);
                    c13.SetCellValue((double)g.subtotal_facturable);
                    c13.CellStyle = currencyStyle;

                    row.CreateCell(14).SetCellValue(g.cantidad_empleados);
                }

                for (int i = 0; i < headers1.Length; i++) sheetProd.AutoSizeColumn(i);

                // Hoja 2: Detalle Marcadas Asistencia
                var sheetMarcadas = workbook.CreateSheet("Detalle_Asistencias");
                var headers2 = new[] { 
                    "ID Cierre", "Fecha", "Mes Corte", "Empleado", "Es Supervisor", "Cargo", "Área", 
                    "Tipo Empleado", "Turno", "Entrada", "Salida", "Horas Ord", "Tarifa Ord", 
                    "Horas Ext", "Tarifa Ext", "Total Horas", "Regla Cobro", "Asignado Producción", 
                    "N° Asignación / Lote", "Tiene Regla" 
                };

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
                    string mesMarcada = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.fecha_asistencia.ToString("MMMM", CultureInfo.GetCultureInfo("es-ES")));

                    row.CreateCell(0).SetCellValue(m.id_cierre);
                    row.CreateCell(1).SetCellValue(m.fecha_asistencia.ToString("dd/MM/yyyy"));
                    row.CreateCell(2).SetCellValue(mesMarcada);
                    row.CreateCell(3).SetCellValue(m.nombre_empleado);
                    row.CreateCell(4).SetCellValue(m.es_supervisor ? "SÍ" : "NO");
                    row.CreateCell(5).SetCellValue(m.cargo);
                    row.CreateCell(6).SetCellValue(m.area_nombre);
                    row.CreateCell(7).SetCellValue(m.tipo_empleado);
                    row.CreateCell(8).SetCellValue(m.turno ?? "Diurno");
                    row.CreateCell(9).SetCellValue(m.entrada_movimiento);
                    row.CreateCell(10).SetCellValue(m.salida_movimiento);

                    var c11 = row.CreateCell(11);
                    c11.SetCellValue((double)m.horas_ordinarias);
                    c11.CellStyle = numberStyle;

                    var c12 = row.CreateCell(12);
                    c12.SetCellValue((double)m.tarifa_ord);
                    c12.CellStyle = currencyStyle;

                    var c13 = row.CreateCell(13);
                    c13.SetCellValue((double)m.horas_extras);
                    c13.CellStyle = numberStyle;

                    var c14 = row.CreateCell(14);
                    c14.SetCellValue((double)m.tarifa_ext);
                    c14.CellStyle = currencyStyle;

                    var c15 = row.CreateCell(15);
                    c15.SetCellValue((double)m.horas_totales);
                    c15.CellStyle = numberStyle;

                    row.CreateCell(16).SetCellValue(m.regla_aplicada ?? "");
                    row.CreateCell(17).SetCellValue(m.asignado_produccion ? "SÍ" : "NO");
                    row.CreateCell(18).SetCellValue(m.numero_asignacion_produccion ?? "");
                    row.CreateCell(19).SetCellValue(m.tiene_regla_catalogo ? "SÍ" : "NO");
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
