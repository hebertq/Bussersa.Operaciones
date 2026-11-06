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
using Modelo.Interfaces;
using MudBlazor;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BsOperaciones.Pages.Operaciones.ActividadesProduccion
{
    public partial class SellarActividadesProduccion : ComponentBase
    {
        [Inject] private IMediator _mediator { get; set; } = null!;
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IJSRuntime _jsRuntime { get; set; } = null!;
        [Inject] private IDialogService _dialogService { get; set; } = null!;
        [Inject] private IOdooService _odooService { get; set; } = null!;

        public class ActividadSeleccionableItem
        {
            public int id { get; set; }
            public string hoja_servicio { get; set; } = string.Empty;
            public string? actividad { get; set; }
            public string? cliente { get; set; }
            public string? area_cliente { get; set; }
            public DateTime? fecha_inicio { get; set; }
            public DateTime? fecha_fin { get; set; }
            public string? hora_inicio { get; set; }
            public string? hora_fin { get; set; }
            public string? nombre_producto { get; set; }
            public string? no_lote { get; set; }
            public string? oc { get; set; }
            public string? no_marchamo { get; set; }
            public decimal peso { get; set; }
            public decimal cantidad_producto { get; set; }
            public decimal costo_producto { get; set; }
            public string? servicio_codigo { get; set; }
            public string? servicio_descripcion { get; set; }
            public string? asignado_a { get; set; }
            public bool facturada { get; set; }
            public string? no_proforma { get; set; }
            public string? no_factura { get; set; }
            public DateTime fecha_registro { get; set; }
            public int? operacion_id { get; set; }
            public bool IsSelected { get; set; }
            public bool EsMesAnterior { get; set; }
            public string PeriodoMes { get; set; } = string.Empty;
        }

        public class GrillaConsolidadaActividadesDto
        {
            public string PeriodoMes { get; set; } = string.Empty;
            public bool EsMesAnterior { get; set; }
            public string Actividad { get; set; } = string.Empty;
            public string NombreProducto { get; set; } = string.Empty;
            public string ServicioCodigo { get; set; } = string.Empty;
            public string AreaCliente { get; set; } = string.Empty;
            public decimal TarifaVenta { get; set; }
            public decimal CantidadTotal { get; set; }
            public decimal SubtotalFacturable { get; set; }
            public int CantidadRegistros { get; set; }
        }

        // Estado General
        protected int activeTabIndex = 0;
        protected bool estaCargando = false;
        protected string mensajeCarga = "Cargando información...";

        // Filtros y Parámetros Semanales
        protected int operacionId = 0;
        protected List<Combos> OperacionesConfiguradas = new();
        protected DateTime fchaInicioSemana;
        protected DateTime fchaFinSemana;
        protected int numeroSemana;
        protected bool soloPendientes = true;
        protected string filtroPeriodoCorte = "TODOS";
        protected string filtroDetalle = string.Empty;

        // Datos de Producción
        protected List<ProduccionDiariaDto> actividadesList = new();
        protected List<ActividadSeleccionableItem> actividadesSeleccionables = new();
        protected List<GrillaConsolidadaActividadesDto> grillaConsolidada = new();

        // Histórico de Lotes Sellados
        protected int historicoClienteId = 0;
        protected int historicoAnio = 2026;
        protected List<HistoricoLoteActividadesDto> historicoLotesList = new();

        #region Propiedades Calculadas y Encajonadas

        protected bool EsSemanaEncajonada => fchaInicioSemana.Month != fchaFinSemana.Month;

        protected string NombreMesAnterior =>
            new DateTime(fchaInicioSemana.Year, fchaInicioSemana.Month, 1).ToString("MMMM", new CultureInfo("es-ES")).ToUpper();

        protected string NombreMesActual =>
            new DateTime(fchaFinSemana.Year, fchaFinSemana.Month, 1).ToString("MMMM", new CultureInfo("es-ES")).ToUpper();

        protected DateTime UltimoDiaMesAnterior =>
            new DateTime(fchaInicioSemana.Year, fchaInicioSemana.Month, DateTime.DaysInMonth(fchaInicioSemana.Year, fchaInicioSemana.Month));

        protected DateTime PrimerDiaMesActual =>
            new DateTime(fchaFinSemana.Year, fchaFinSemana.Month, 1);

        protected string PeriodoMesAnteriorTexto =>
            $"{fchaInicioSemana:dd/MM} al {UltimoDiaMesAnterior:dd/MM}";

        protected string PeriodoMesActualTexto =>
            $"{PrimerDiaMesActual:dd/MM} al {fchaFinSemana:dd/MM}";

        // Totales Encajonados
        protected decimal TotalCantidadMesAnterior =>
            actividadesSeleccionables.Where(a => a.EsMesAnterior).Sum(a => a.cantidad_producto);

        protected decimal TotalMontoMesAnterior =>
            actividadesSeleccionables.Where(a => a.EsMesAnterior).Sum(a => a.cantidad_producto * a.costo_producto);

        protected decimal TotalCantidadMesActual =>
            actividadesSeleccionables.Where(a => !a.EsMesAnterior).Sum(a => a.cantidad_producto);

        protected decimal TotalMontoMesActual =>
            actividadesSeleccionables.Where(a => !a.EsMesAnterior).Sum(a => a.cantidad_producto * a.costo_producto);

        // Totales Generales KPI
        protected int TotalRegistros => actividadesSeleccionables.Count;
        protected decimal TotalCantidadProducida => actividadesSeleccionables.Sum(a => a.cantidad_producto);
        protected decimal TotalSubtotalFacturable => actividadesSeleccionables.Sum(a => a.cantidad_producto * a.costo_producto);
        protected int TotalPendientes => actividadesSeleccionables.Count(a => string.IsNullOrWhiteSpace(a.no_lote));
        protected int TotalSelladas => actividadesSeleccionables.Count(a => !string.IsNullOrWhiteSpace(a.no_lote));

        // Filtrado por Corte y Búsqueda
        protected IEnumerable<GrillaConsolidadaActividadesDto> GrillaFiltrada
        {
            get
            {
                if (grillaConsolidada == null) return Enumerable.Empty<GrillaConsolidadaActividadesDto>();
                if (!EsSemanaEncajonada || filtroPeriodoCorte == "TODOS") return grillaConsolidada;
                if (filtroPeriodoCorte == "MES_ANTERIOR") return grillaConsolidada.Where(g => g.EsMesAnterior);
                if (filtroPeriodoCorte == "MES_ACTUAL") return grillaConsolidada.Where(g => !g.EsMesAnterior);
                return grillaConsolidada;
            }
        }

        protected IEnumerable<ActividadSeleccionableItem> DetalleFiltrado
        {
            get
            {
                var query = actividadesSeleccionables.AsEnumerable();

                if (EsSemanaEncajonada)
                {
                    if (filtroPeriodoCorte == "MES_ANTERIOR")
                        query = query.Where(a => a.EsMesAnterior);
                    else if (filtroPeriodoCorte == "MES_ACTUAL")
                        query = query.Where(a => !a.EsMesAnterior);
                }

                if (!string.IsNullOrWhiteSpace(filtroDetalle))
                {
                    string f = filtroDetalle.Trim().ToLowerInvariant();
                    query = query.Where(a =>
                        (a.actividad?.ToLowerInvariant().Contains(f) ?? false) ||
                        (a.nombre_producto?.ToLowerInvariant().Contains(f) ?? false) ||
                        (a.servicio_codigo?.ToLowerInvariant().Contains(f) ?? false) ||
                        (a.area_cliente?.ToLowerInvariant().Contains(f) ?? false) ||
                        (a.no_lote?.ToLowerInvariant().Contains(f) ?? false)
                    );
                }

                return query;
            }
        }

        #endregion

        #region Ciclo de Vida e Inicialización

        protected override async Task OnInitializedAsync()
        {
            SetSemana(DateTime.Today);
            await CargarCombosOperaciones();
            await CargarHistorico();
        }

        protected void SetActiveTab(int index)
        {
            activeTabIndex = index;
            StateHasChanged();
        }

        protected async Task CargarCombosOperaciones()
        {
            try
            {
                var response = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
                if (!response.Respuesta.ExisteError && response.Model != null)
                {
                    OperacionesConfiguradas = response.Model.OrderBy(c => c.nombre).ToList();

                    // Preseleccionar la primera operación si hay guardada en localStorage
                    try
                    {
                        var guardado = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "actividades_sellar_operacion_id");
                        if (int.TryParse(guardado, out int opGuardada) && OperacionesConfiguradas.Any(o => o.id == opGuardada))
                        {
                            operacionId = opGuardada;
                            historicoClienteId = opGuardada;
                            await ConsultarSemana();
                        }
                        else if (OperacionesConfiguradas.Any(o => o.id == 27)) // CBC Pepsi
                        {
                            operacionId = 27;
                            historicoClienteId = 27;
                            await ConsultarSemana();
                        }
                    }
                    catch
                    {
                        if (OperacionesConfiguradas.Any())
                        {
                            operacionId = OperacionesConfiguradas.First().id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar operaciones: {ex.Message}", Severity.Error);
            }
        }

        #endregion

        #region Navegación Semanal

        protected void SetSemana(DateTime fechaRef)
        {
            // Ajustar al lunes de la semana
            int diff = (7 + (fechaRef.DayOfWeek - DayOfWeek.Monday)) % 7;
            fchaInicioSemana = fechaRef.AddDays(-1 * diff).Date;
            fchaFinSemana = fchaInicioSemana.AddDays(6).Date;

            // Calcular número de semana ISO 8601
            Calendar cal = CultureInfo.InvariantCulture.Calendar;
            DayOfWeek day = cal.GetDayOfWeek(fchaInicioSemana);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                fechaRef = fechaRef.AddDays(3);
            }
            numeroSemana = cal.GetWeekOfYear(fchaInicioSemana, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        protected async Task SemanaAnterior()
        {
            SetSemana(fchaInicioSemana.AddDays(-7));
            filtroPeriodoCorte = "TODOS";
            if (operacionId > 0) await ConsultarSemana();
        }

        protected async Task SemanaActual()
        {
            SetSemana(DateTime.Today);
            filtroPeriodoCorte = "TODOS";
            if (operacionId > 0) await ConsultarSemana();
        }

        protected async Task SemanaSiguiente()
        {
            SetSemana(fchaInicioSemana.AddDays(7));
            filtroPeriodoCorte = "TODOS";
            if (operacionId > 0) await ConsultarSemana();
        }

        #endregion

        #region Consulta y Consolidación Semanal

        protected async Task ConsultarSemana()
        {
            if (operacionId <= 0)
            {
                _snackbar.Add("Seleccione una operación o cliente.", Severity.Warning);
                return;
            }

            estaCargando = true;
            mensajeCarga = $"Consultando actividades de la semana {numeroSemana}...";
            StateHasChanged();

            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "actividades_sellar_operacion_id", operacionId.ToString());

                // Consultar actividades en el rango semanal para este cliente
                var response = await _mediator.Send(new GetProduccionDiariaQuery(
                    fchaInicioSemana,
                    fchaFinSemana,
                    operacionId,
                    null
                ));

                if (response.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Error al consultar actividades: {response.Respuesta.MensajeError}", Severity.Error);
                    actividadesList = new();
                    actividadesSeleccionables = new();
                    grillaConsolidada = new();
                }
                else
                {
                    var raw = response.Model?.ToList() ?? new List<ProduccionDiariaDto>();

                    if (soloPendientes)
                    {
                        actividadesList = raw.Where(a => string.IsNullOrWhiteSpace(a.no_lote)).ToList();
                    }
                    else
                    {
                        actividadesList = raw;
                    }

                    // Mapear a seleccionables con detección de mes
                    actividadesSeleccionables = actividadesList.Select(a =>
                    {
                        bool esAnt = EsSemanaEncajonada && (a.fecha_inicio ?? fchaInicioSemana).Month == fchaInicioSemana.Month;
                        string periodoMes = esAnt ? NombreMesAnterior : (EsSemanaEncajonada ? NombreMesActual : "Semana Normal");

                        return new ActividadSeleccionableItem
                        {
                            id = a.id,
                            hoja_servicio = a.hoja_servicio,
                            actividad = a.actividad,
                            cliente = a.cliente,
                            area_cliente = a.area_cliente,
                            fecha_inicio = a.fecha_inicio,
                            fecha_fin = a.fecha_fin,
                            hora_inicio = a.hora_inicio,
                            hora_fin = a.hora_fin,
                            nombre_producto = a.nombre_producto,
                            no_lote = a.no_lote,
                            oc = a.oc,
                            no_marchamo = a.no_marchamo,
                            peso = a.peso,
                            cantidad_producto = a.cantidad_producto,
                            costo_producto = a.costo_producto,
                            servicio_codigo = a.servicio_codigo,
                            servicio_descripcion = a.servicio_descripcion,
                            asignado_a = a.asignado_a,
                            facturada = a.facturada,
                            no_proforma = a.no_proforma,
                            no_factura = a.no_factura,
                            fecha_registro = a.fecha_registro,
                            operacion_id = a.operacion_id,
                            IsSelected = string.IsNullOrWhiteSpace(a.no_lote),
                            EsMesAnterior = esAnt,
                            PeriodoMes = periodoMes
                        };
                    }).ToList();

                    // Construir grilla consolidada agrupada
                    ConstruirGrillaConsolidada();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al consultar actividades: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
                StateHasChanged();
            }
        }

        protected void ConstruirGrillaConsolidada()
        {
            grillaConsolidada = actividadesSeleccionables
                .GroupBy(a => new
                {
                    a.PeriodoMes,
                    a.EsMesAnterior,
                    Actividad = a.actividad ?? "Sin Actividad",
                    NombreProducto = a.nombre_producto ?? "",
                    ServicioCodigo = a.servicio_codigo ?? "",
                    AreaCliente = a.area_cliente ?? "CD",
                    a.costo_producto
                })
                .Select(g => new GrillaConsolidadaActividadesDto
                {
                    PeriodoMes = g.Key.PeriodoMes,
                    EsMesAnterior = g.Key.EsMesAnterior,
                    Actividad = g.Key.Actividad,
                    NombreProducto = g.Key.NombreProducto,
                    ServicioCodigo = g.Key.ServicioCodigo,
                    AreaCliente = g.Key.AreaCliente,
                    TarifaVenta = g.Key.costo_producto,
                    CantidadTotal = g.Sum(x => x.cantidad_producto),
                    SubtotalFacturable = g.Sum(x => x.cantidad_producto * x.costo_producto),
                    CantidadRegistros = g.Count()
                })
                .OrderBy(g => g.EsMesAnterior ? 0 : 1)
                .ThenBy(g => g.Actividad)
                .ThenBy(g => g.ServicioCodigo)
                .ToList();
        }

        #endregion

        #region Selecciones de Detalle

        protected void SeleccionarTodasPendientes()
        {
            foreach (var act in actividadesSeleccionables.Where(a => string.IsNullOrWhiteSpace(a.no_lote)))
            {
                act.IsSelected = true;
            }
            StateHasChanged();
        }

        protected void SeleccionarMesAnterior()
        {
            foreach (var act in actividadesSeleccionables)
            {
                act.IsSelected = act.EsMesAnterior && string.IsNullOrWhiteSpace(act.no_lote);
            }
            StateHasChanged();
        }

        protected void SeleccionarMesActual()
        {
            foreach (var act in actividadesSeleccionables)
            {
                act.IsSelected = !act.EsMesAnterior && string.IsNullOrWhiteSpace(act.no_lote);
            }
            StateHasChanged();
        }

        protected void DeseleccionarTodas()
        {
            foreach (var act in actividadesSeleccionables)
            {
                act.IsSelected = false;
            }
            StateHasChanged();
        }

        #endregion

        #region Sellado de Lotes

        protected async Task AbrirModalSellarLote()
        {
            var pendientesSeleccionados = actividadesSeleccionables
                .Where(a => a.IsSelected && string.IsNullOrWhiteSpace(a.no_lote))
                .ToList();

            if (!pendientesSeleccionados.Any())
            {
                _snackbar.Add("Debe seleccionar al menos una actividad pendiente para sellar.", Severity.Warning);
                return;
            }

            var opObj = OperacionesConfiguradas.FirstOrDefault(o => o.id == operacionId);
            string codCliente = (opObj?.nombre ?? "CLIENTE").Replace(" ", "").ToUpper();
            if (codCliente.Length > 10) codCliente = codCliente.Substring(0, 10);

            var parameters = new DialogParameters();

            if (EsSemanaEncajonada)
            {
                var pendientesMesAnterior = pendientesSeleccionados.Where(a => a.EsMesAnterior).ToList();
                var pendientesMesActual = pendientesSeleccionados.Where(a => !a.EsMesAnterior).ToList();

                string mesAntCorto = NombreMesAnterior.Substring(0, Math.Min(3, NombreMesAnterior.Length));
                string mesActCorto = NombreMesActual.Substring(0, Math.Min(3, NombreMesActual.Length));

                parameters.Add("EsEncajonada", true);
                parameters.Add("NombreMesAnterior", NombreMesAnterior);
                parameters.Add("PeriodoMesAnterior", PeriodoMesAnteriorTexto);
                parameters.Add("CantidadMarcadasMesAnterior", pendientesMesAnterior.Count);
                parameters.Add("TotalHorasMesAnterior", pendientesMesAnterior.Sum(a => a.cantidad_producto));
                parameters.Add("NumeroAsignacionMesAnterior", $"{codCliente}-ACT-SEM{numeroSemana}-{mesAntCorto}-{fchaInicioSemana.Year}");

                parameters.Add("NombreMesActual", NombreMesActual);
                parameters.Add("PeriodoMesActual", PeriodoMesActualTexto);
                parameters.Add("CantidadMarcadasMesActual", pendientesMesActual.Count);
                parameters.Add("TotalHorasMesActual", pendientesMesActual.Sum(a => a.cantidad_producto));
                parameters.Add("NumeroAsignacionMesActual", $"{codCliente}-ACT-SEM{numeroSemana}-{mesActCorto}-{fchaFinSemana.Year}");
            }
            else
            {
                parameters.Add("EsEncajonada", false);
                parameters.Add("CantidadMarcadas", pendientesSeleccionados.Count);
                parameters.Add("TotalHoras", pendientesSeleccionados.Sum(a => a.cantidad_producto));
                parameters.Add("NumeroAsignacion", $"{codCliente}-ACT-SEM{numeroSemana}-{fchaInicioSemana.Year}");
            }

            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<AsignacionProduccion.DialogSellarLote>("Sellar Lote de Actividades", parameters, options);
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
                            var pendientesMesAnterior = pendientesSeleccionados.Where(a => a.EsMesAnterior).ToList();
                            var pendientesMesActual = pendientesSeleccionados.Where(a => !a.EsMesAnterior).ToList();

                            mensajeCarga = $"Sellando lotes {resLote.LoteMesAnterior} y {resLote.LoteMesActual}...";
                            StateHasChanged();

                            IResponse? respP1 = null;
                            if (pendientesMesAnterior.Any())
                            {
                                var reqP1 = new SellarLoteActividadesRequest
                                {
                                    ids = pendientesMesAnterior.Select(a => a.id).ToList(),
                                    numero_lote = resLote.LoteMesAnterior,
                                    usuario_id = 0
                                };
                                respP1 = await _mediator.Send(new SellarLoteActividadesCommand(reqP1));
                            }

                            IResponse? respP2 = null;
                            if (pendientesMesActual.Any())
                            {
                                var reqP2 = new SellarLoteActividadesRequest
                                {
                                    ids = pendientesMesActual.Select(a => a.id).ToList(),
                                    numero_lote = resLote.LoteMesActual,
                                    usuario_id = 0
                                };
                                respP2 = await _mediator.Send(new SellarLoteActividadesCommand(reqP2));
                            }

                            bool errorP1 = respP1 != null && respP1.Respuesta.ExisteError;
                            bool errorP2 = respP2 != null && respP2.Respuesta.ExisteError;

                            if (errorP1 || errorP2)
                            {
                                string err = (errorP1 ? respP1?.Respuesta.MensajeError : null) ?? respP2?.Respuesta.MensajeError ?? "Error al sellar lotes";
                                _snackbar.Add($"Error: {err}", Severity.Error);
                            }
                            else
                            {
                                _snackbar.Add($"¡Lotes de actividades sellados con éxito: {resLote.LoteMesAnterior} y {resLote.LoteMesActual}!", Severity.Success);
                                await ConsultarSemana();
                                await CargarHistorico();
                            }
                        }
                        else
                        {
                            mensajeCarga = $"Sellando lote {resLote.LoteUnico}...";
                            StateHasChanged();

                            var req = new SellarLoteActividadesRequest
                            {
                                ids = pendientesSeleccionados.Select(a => a.id).ToList(),
                                numero_lote = resLote.LoteUnico,
                                usuario_id = 0
                            };
                            var resp = await _mediator.Send(new SellarLoteActividadesCommand(req));

                            if (resp.Respuesta.ExisteError)
                            {
                                _snackbar.Add($"Error al sellar lote: {resp.Respuesta.MensajeError}", Severity.Error);
                            }
                            else
                            {
                                _snackbar.Add($"¡Lote {resLote.LoteUnico} sellado exitosamente con {pendientesSeleccionados.Count} actividades!", Severity.Success);
                                await ConsultarSemana();
                                await CargarHistorico();
                            }
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

        #endregion

        #region Trazabilidad y Lotes Sellados

        protected async Task CargarHistorico()
        {
            try
            {
                var response = await _mediator.Send(new GetHistoricoLotesActividadesQuery(historicoClienteId, historicoAnio));
                if (!response.Respuesta.ExisteError && response.Model != null)
                {
                    historicoLotesList = response.Model.ToList();
                }
                else
                {
                    historicoLotesList = new();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar histórico: {ex.Message}", Severity.Error);
            }
        }

        protected async Task DesellarLote(string numeroLote)
        {
            bool? confirm = await _dialogService.ShowMessageBox(
                "Revertir Sellado de Lote",
                $"¿Está seguro de revertir y liberar las actividades del lote '{numeroLote}'? Quedarán nuevamente como 'Pendientes' para modificación o nuevo sellado.",
                yesText: "Sí, revertir lote",
                cancelText: "Cancelar"
            );

            if (confirm == true)
            {
                estaCargando = true;
                mensajeCarga = $"Revirtiendo lote {numeroLote}...";
                StateHasChanged();

                try
                {
                    var response = await _mediator.Send(new DesellarLoteActividadesCommand(numeroLote));
                    if (response.Respuesta.ExisteError)
                    {
                        _snackbar.Add($"Error al revertir lote: {response.Respuesta.MensajeError}", Severity.Error);
                    }
                    else
                    {
                        _snackbar.Add($"Lote '{numeroLote}' revertido correctamente. Las actividades están disponibles nuevamente.", Severity.Success);
                        await CargarHistorico();
                        if (operacionId > 0) await ConsultarSemana();
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }

        #endregion

        #region Exportación a Excel

        protected async Task ExportarExcel()
        {
            if (actividadesList == null || !actividadesList.Any()) return;

            try
            {
                var workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet("Actividades Producción");

                // Estilo Cabecera
                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerFont.Color = IndexedColors.White.Index;

                var headerStyle = workbook.CreateCellStyle();
                headerStyle.SetFont(headerFont);
                headerStyle.FillForegroundColor = IndexedColors.RoyalBlue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;

                // Fila de Encabezados
                var headerRow = sheet.CreateRow(0);
                string[] headers = new[]
                {
                    "ID", "Fecha", "Cliente", "Área", "Actividad", "Variante / Producto",
                    "SKU Odoo", "Cantidad", "Tarifa Venta ($)", "Subtotal ($)", "N° Lote", "Facturada", "N° Factura"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Filas de Datos
                int rowIndex = 1;
                foreach (var item in DetalleFiltrado)
                {
                    var row = sheet.CreateRow(rowIndex++);
                    row.CreateCell(0).SetCellValue(item.id);
                    row.CreateCell(1).SetCellValue(item.fecha_inicio?.ToString("dd/MM/yyyy") ?? "");
                    row.CreateCell(2).SetCellValue(item.cliente ?? "");
                    row.CreateCell(3).SetCellValue(item.area_cliente ?? "");
                    row.CreateCell(4).SetCellValue(item.actividad ?? "");
                    row.CreateCell(5).SetCellValue(item.nombre_producto ?? "");
                    row.CreateCell(6).SetCellValue(item.servicio_codigo ?? "");
                    row.CreateCell(7).SetCellValue((double)item.cantidad_producto);
                    row.CreateCell(8).SetCellValue((double)item.costo_producto);
                    row.CreateCell(9).SetCellValue((double)(item.cantidad_producto * item.costo_producto));
                    row.CreateCell(10).SetCellValue(item.no_lote ?? "");
                    row.CreateCell(11).SetCellValue(item.facturada ? "SÍ" : "NO");
                    row.CreateCell(12).SetCellValue(item.no_factura ?? "");
                }

                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                }

                using var stream = new MemoryStream();
                workbook.Write(stream);
                var bytes = stream.ToArray();

                var fileName = $"Actividades_Produccion_{operacionId}_Sem{numeroSemana}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                await _jsRuntime.InvokeVoidAsync("saveAsFile", fileName, Convert.ToBase64String(bytes));
                _snackbar.Add("Archivo Excel exportado exitosamente.", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al exportar Excel: {ex.Message}", Severity.Error);
            }
        }

        #endregion
    }
}
