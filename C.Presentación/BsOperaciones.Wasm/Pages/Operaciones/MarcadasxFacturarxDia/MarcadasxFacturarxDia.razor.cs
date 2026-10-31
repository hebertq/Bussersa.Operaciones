using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Common.Strategy.Conciliacion;
using BsOperaciones.Application.Features.Odoo.Command;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Report;

namespace BsOperaciones.Pages.Operaciones.MarcadasxFacturarxDia
{
    public partial class MarcadasxFacturarxDia : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected HostService.Interfaces.IOdooService OdooService { get; set; }
        [Inject] protected ConciliacionClienteSelector ConciliacionSelector { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }

        public List<DiasTrabajadosAreas> PayLoadList { get; set; } = new();
        public List<Combos> PayLoadOper { get; set; } = new();
        public int operacion { set; get; } = 0;
        public bool isloaddata { set; get; } = false;
        public bool BotonEnabled { set; get; } = true;
        public string nombreOperacion { set; get; } = "";

        public DateTime FchaDesde { set; get; } = DateTime.Today.AddDays(-5);
        public DateTime FchaHasta { set; get; } = DateTime.Today;
        protected DateTime? _fchaDesdeWrapper { get => FchaDesde; set => FchaDesde = value ?? DateTime.Today.AddDays(-5); }
        protected DateTime? _fchaHastaWrapper { get => FchaHasta; set => FchaHasta = value ?? DateTime.Today; }

        // --- CONCILIACIÓN CON CLIENTE (SINSA) ---
        protected bool SoportaConciliacionActual => ConciliacionSelector != null && ConciliacionSelector.SoportaConciliacion(operacion, nombreOperacion);
        protected bool TieneConciliacion => resultadoConciliacion != null;
        protected ResultadoConciliacionDto? resultadoConciliacion = null;
        protected string nombreArchivoConciliacion = string.Empty;
        protected byte[]? archivoConciliacionBytes = null;
        protected int activeTabIndex = 0;
        protected string modoVistaConsolidado = "AreaPuesto"; // "AreaPuesto" o "SoloArea"
        protected string _searchConsolidado = "";
        protected string _searchFaltante = "";
        protected string _searchSoloCliente = "";
        protected string _searchDiferencia = "";
        protected string _searchSupervisorExcluido = "";

        // --- GESTIÓN Y EXCLUSIÓN DE SUPERVISORES (REGLA BUSSERSA) ---
        protected bool excluirSupervisores = true;
        protected bool mostrarModalSupervisores = false;
        protected HashSet<int> supervisoresIds = new();
        protected string _searchSupervisor = "";
        protected List<EmpleadoSupervisorItem> ListaEmpleadosSupervisores = new();

        public class EmpleadoSupervisorItem
        {
            public int IdEmpleado { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Area { get; set; } = string.Empty;
            public string TipoEmpleado { get; set; } = string.Empty;
            public bool EsSupervisor { get; set; }
        }

        protected IEnumerable<EmpleadoSupervisorItem> FilteredSupervisoresList =>
            string.IsNullOrWhiteSpace(_searchSupervisor)
                ? ListaEmpleadosSupervisores
                : ListaEmpleadosSupervisores.Where(x =>
                    (x.Nombre ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase) ||
                    x.IdEmpleado.ToString().Contains(_searchSupervisor) ||
                    (x.Area ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase) ||
                    (x.TipoEmpleado ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase));

        public bool EsSupervisor(DiasTrabajadosAreas item)
        {
            if (item == null) return false;
            return ConciliacionSinsaStrategy.EsSupervisor(item, supervisoresIds);
        }

        public List<DiasTrabajadosAreas> PayLoadListFiltrada =>
            excluirSupervisores ? PayLoadList.Where(x => !EsSupervisor(x)).ToList() : PayLoadList;

        public int CantidadSupervisoresExcluidos => PayLoadList.Count(EsSupervisor);

        public decimal TotalHorasSupervisoresExcluidas =>
            Math.Round(PayLoadList.Where(EsSupervisor).Sum(x => x.horas + x.horasextras), 2);

        protected async Task OnToggleExcluirSupervisores(bool value)
        {
            excluirSupervisores = value;
            if (archivoConciliacionBytes != null)
            {
                EjecutarConciliacion();
            }
        }

        protected async Task CargarSupervisoresConfig()
        {
            try
            {
                string? json = await JS.InvokeAsync<string>("localStorage.getItem", "catalogo_supervisores_ids");
                if (!string.IsNullOrEmpty(json))
                {
                    var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json);
                    supervisoresIds = ids != null ? new HashSet<int>(ids) : new HashSet<int>();
                }
                else
                {
                    supervisoresIds = new HashSet<int>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar catalogo_supervisores_ids: {ex.Message}");
                supervisoresIds = new HashSet<int>();
            }
        }

        protected async Task AbrirModalSupervisores()
        {
            isloaddata = true;
            StateHasChanged();
            try
            {
                await CargarSupervisoresConfig();
                var items = new List<EmpleadoSupervisorItem>();

                if (PayLoadList != null && PayLoadList.Any())
                {
                    foreach (var grp in PayLoadList.GroupBy(x => x.id))
                    {
                        var first = grp.First();
                        bool isSelected = supervisoresIds.Contains(first.id) || ConciliacionSinsaStrategy.EsSupervisor(first, supervisoresIds);

                        items.Add(new EmpleadoSupervisorItem
                        {
                            IdEmpleado = first.id,
                            Nombre = first.nombre ?? $"Empleado #{first.id}",
                            Area = !string.IsNullOrWhiteSpace(first.areamov) ? first.areamov : (!string.IsNullOrWhiteSpace(first.area) ? first.area : "N/A"),
                            TipoEmpleado = !string.IsNullOrWhiteSpace(first.cargo) ? first.cargo : (!string.IsNullOrWhiteSpace(first.tipoempleado) ? first.tipoempleado : "N/A"),
                            EsSupervisor = isSelected
                        });
                    }
                }

                ListaEmpleadosSupervisores = items.OrderByDescending(x => x.EsSupervisor).ThenBy(x => x.Nombre).ToList();
                _searchSupervisor = "";
                mostrarModalSupervisores = true;
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al abrir catálogo de supervisores: {ex.Message}", Severity.Error);
            }
            finally
            {
                isloaddata = false;
                StateHasChanged();
            }
        }

        protected void MarcarTodosSupervisores()
        {
            foreach (var item in FilteredSupervisoresList) item.EsSupervisor = true;
        }

        protected void DesmarcarTodosSupervisores()
        {
            foreach (var item in FilteredSupervisoresList) item.EsSupervisor = false;
        }

        protected async Task GuardarSupervisoresConfig()
        {
            try
            {
                supervisoresIds = new HashSet<int>(ListaEmpleadosSupervisores.Where(x => x.EsSupervisor).Select(x => x.IdEmpleado));
                string json = System.Text.Json.JsonSerializer.Serialize(supervisoresIds.ToList());
                await JS.InvokeVoidAsync("localStorage.setItem", "catalogo_supervisores_ids", json);
                Snackbar.Add("Catálogo de supervisores guardado exitosamente.", Severity.Success);
                mostrarModalSupervisores = false;
                if (archivoConciliacionBytes != null)
                {
                    EjecutarConciliacion();
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al guardar supervisores: " + ex.Message, Severity.Error);
            }
        }

        protected Func<ConsolidadoAreaPuestoDto, bool> _filterConsolidadoAreaPuesto => x =>
            string.IsNullOrWhiteSpace(_searchConsolidado) ||
            (x.Area != null && x.Area.Contains(_searchConsolidado, StringComparison.OrdinalIgnoreCase)) ||
            (x.Puesto != null && x.Puesto.Contains(_searchConsolidado, StringComparison.OrdinalIgnoreCase));

        protected Func<ConsolidadoSoloAreaDto, bool> _filterConsolidadoSoloArea => x =>
            string.IsNullOrWhiteSpace(_searchConsolidado) ||
            (x.Area != null && x.Area.Contains(_searchConsolidado, StringComparison.OrdinalIgnoreCase));

        protected Func<TurnoFaltanteClienteDto, bool> _filterFaltante => x =>
            string.IsNullOrWhiteSpace(_searchFaltante) ||
            (x.NombreEmpleado != null && x.NombreEmpleado.Contains(_searchFaltante, StringComparison.OrdinalIgnoreCase)) ||
            (x.Area != null && x.Area.Contains(_searchFaltante, StringComparison.OrdinalIgnoreCase)) ||
            (x.Cargo != null && x.Cargo.Contains(_searchFaltante, StringComparison.OrdinalIgnoreCase)) ||
            x.IdEmpleado.ToString().Contains(_searchFaltante);

        protected Func<RegistroSoloClienteDto, bool> _filterSoloCliente => x =>
            string.IsNullOrWhiteSpace(_searchSoloCliente) ||
            (x.NombreEmpleado != null && x.NombreEmpleado.Contains(_searchSoloCliente, StringComparison.OrdinalIgnoreCase)) ||
            (x.Puesto != null && x.Puesto.Contains(_searchSoloCliente, StringComparison.OrdinalIgnoreCase)) ||
            (x.Bodega != null && x.Bodega.Contains(_searchSoloCliente, StringComparison.OrdinalIgnoreCase));

        protected Func<DiferenciaDiaComunDto, bool> _filterDiferencia => x =>
            string.IsNullOrWhiteSpace(_searchDiferencia) ||
            (x.NombreEmpleado != null && x.NombreEmpleado.Contains(_searchDiferencia, StringComparison.OrdinalIgnoreCase)) ||
            (x.AreaBussersa != null && x.AreaBussersa.Contains(_searchDiferencia, StringComparison.OrdinalIgnoreCase)) ||
            (x.BodegaCliente != null && x.BodegaCliente.Contains(_searchDiferencia, StringComparison.OrdinalIgnoreCase)) ||
            (x.Cargo != null && x.Cargo.Contains(_searchDiferencia, StringComparison.OrdinalIgnoreCase));

        protected Func<RegistroSupervisorExcluidoDto, bool> _filterSupervisorExcluido => x =>
            string.IsNullOrWhiteSpace(_searchSupervisorExcluido) ||
            (x.NombreEmpleado != null && x.NombreEmpleado.Contains(_searchSupervisorExcluido, StringComparison.OrdinalIgnoreCase)) ||
            (x.Area != null && x.Area.Contains(_searchSupervisorExcluido, StringComparison.OrdinalIgnoreCase)) ||
            (x.Cargo != null && x.Cargo.Contains(_searchSupervisorExcluido, StringComparison.OrdinalIgnoreCase)) ||
            x.IdEmpleado.ToString().Contains(_searchSupervisorExcluido);

        protected override async Task OnInitializedAsync()
        {
            await CargarSupervisoresConfig();
            var regop = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
            PayLoadOper = regop.Model ?? new();
        }

        protected async Task OnChangeCliente(int value)
        {
            operacion = value;
            LimpiarConciliacionSilencioso();
            if (operacion > 0)
            {
                BotonEnabled = true;
                await GetAllMarcadasFacturar();
            }
        }

        protected void HandleValidationChange(bool deshabilitar)
        {
            InvokeAsync(() =>
            {
                if (BotonEnabled != deshabilitar)
                {
                    BotonEnabled = deshabilitar;
                    StateHasChanged();
                }
            });
        }

        protected async Task GetAllMarcadasFacturar()
        {
            isloaddata = true;
            try
            {
                var opSelected = PayLoadOper.FirstOrDefault(x => x.id == operacion);
                nombreOperacion = opSelected?.nombre ?? "";
                typeeinout rango = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = operacion };

                // 1. Traer pendientes
                var registrosPendientes = await _mediator.Send(new GetAllMarcadasFacturarQuery(rango));
                // 2. Traer cerrados
                var registrosCerrados = await _mediator.Send(new GetReporteCierreClienteRangoQuery(operacion, FchaDesde, FchaHasta));

                var listaFinal = registrosPendientes.Model ?? new();

                // 3. Marcar los registros cerrados antes de unirlos o compararlos
                if (registrosCerrados.Model != null)
                {
                    foreach (var cerrado in registrosCerrados.Model)
                    {
                        var itemExistente = listaFinal.FirstOrDefault(x => x.id == cerrado.id_empleado && x.tarea == cerrado.tarea_id);
                        if (itemExistente != null && itemExistente.id > 0)
                        {
                            itemExistente.cierre = true;
                        }      
                    }
                }

                PayLoadList = listaFinal ?? new();

                // Si hay un archivo cargado previamente, reejecutar la conciliación
                if (archivoConciliacionBytes != null)
                {
                    EjecutarConciliacion();
                }
            }
            finally { isloaddata = false; }
        }

        protected async Task CargarArchivoConciliacion(IBrowserFile file)
        {
            if (file == null) return;
            if (PayLoadList == null || !PayLoadList.Any())
            {
                Snackbar.Add("No hay marcadas cargadas en Bussersa para el periodo seleccionado. Asegúrese de seleccionar las fechas correctas.", Severity.Warning);
                return;
            }

            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);

            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 25 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                archivoConciliacionBytes = ms.ToArray();
                nombreArchivoConciliacion = file.Name;

                EjecutarConciliacion();

                if (resultadoConciliacion != null)
                {
                    resultadoConciliacion.NombreArchivo = nombreArchivoConciliacion;
                    activeTabIndex = 0; // Mostrar primero la pestaña de Consolidado
                    if (resultadoConciliacion.ConsolidadoCoincideTotalmente)
                    {
                        Snackbar.Add($"¡Consolidado Coincidente! Las horas ordinarias y extras coinciden al 100% con Sinsa ({resultadoConciliacion.TotalHorasBussersa:N2}h).", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add($"Conciliación procesada: {resultadoConciliacion.AreasConDiferencia} área(s) con diferencias. Dif. global: {resultadoConciliacion.DiferenciaTotalHorasGlobal:+0.00;-0.00;0.00}h.", Severity.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al procesar archivo de conciliación: {ex.Message}", Severity.Error);
            }
            finally
            {
                isloaddata = false;
                StateHasChanged();
            }
        }

        protected void EjecutarConciliacion()
        {
            if (archivoConciliacionBytes == null || !archivoConciliacionBytes.Any()) return;
            if (PayLoadList == null || !PayLoadList.Any()) return;

            try
            {
                var estrategia = ConciliacionSelector.ObtenerEstrategia(operacion, nombreOperacion);
                if (estrategia == null)
                {
                    Snackbar.Add("No hay una estrategia de conciliación configurada para esta operación.", Severity.Warning);
                    return;
                }

                using var ms = new MemoryStream(archivoConciliacionBytes);
                resultadoConciliacion = estrategia.ConciliarMovimientos(ms, PayLoadList, FchaDesde, FchaHasta, excluirSupervisores, supervisoresIds);
                resultadoConciliacion.NombreArchivo = nombreArchivoConciliacion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en EjecutarConciliacion: {ex.Message}");
            }
        }

        protected void LimpiarConciliacion()
        {
            LimpiarConciliacionSilencioso();
            Snackbar.Add("Conciliación removida.", Severity.Info);
            StateHasChanged();
        }

        private void LimpiarConciliacionSilencioso()
        {
            resultadoConciliacion = null;
            archivoConciliacionBytes = null;
            nombreArchivoConciliacion = string.Empty;
            activeTabIndex = 0;
        }

        protected async Task ExportarConciliacionExcel()
        {
            if (resultadoConciliacion == null) return;

            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);

            try
            {
                // Hoja 1: Consolidado por Área y Puesto
                var consolidadoExport = resultadoConciliacion.ConsolidadoAreaPuesto.Select(x => new
                {
                    No = x.No,
                    Area = x.Area,
                    Puesto = x.Puesto,
                    Turnos_Bussersa = x.TurnosBussersa,
                    Turnos_Sinsa = x.TurnosCliente,
                    Dif_Turnos = x.DiferenciaTurnos,
                    Horas_Ord_Bussersa = x.HorasOrdBussersa,
                    Horas_Ord_Sinsa = x.HorasOrdCliente,
                    Dif_Horas_Ord = x.DiferenciaHorasOrd,
                    Horas_Ext_Bussersa = x.HorasExtBussersa,
                    Horas_Ext_Sinsa = x.HorasExtCliente,
                    Dif_Horas_Ext = x.DiferenciaHorasExt,
                    Total_Horas_Bussersa = x.TotalHorasBussersa,
                    Total_Horas_Sinsa = x.TotalHorasCliente,
                    Dif_Total_Horas = x.DiferenciaTotalHoras,
                    Estado = x.Coincide ? "COINCIDE" : "DIFERENCIA"
                }).ToList();

                // Hoja 2: Consolidado solo por Área
                var soloAreaExport = resultadoConciliacion.ConsolidadoSoloArea.Select(x => new
                {
                    No = x.No,
                    Area = x.Area,
                    Turnos_Bussersa = x.TurnosBussersa,
                    Turnos_Sinsa = x.TurnosCliente,
                    Dif_Turnos = x.DiferenciaTurnos,
                    Horas_Ord_Bussersa = x.HorasOrdBussersa,
                    Horas_Ord_Sinsa = x.HorasOrdCliente,
                    Dif_Horas_Ord = x.DiferenciaHorasOrd,
                    Horas_Ext_Bussersa = x.HorasExtBussersa,
                    Horas_Ext_Sinsa = x.HorasExtCliente,
                    Dif_Horas_Ext = x.DiferenciaHorasExt,
                    Total_Horas_Bussersa = x.TotalHorasBussersa,
                    Total_Horas_Sinsa = x.TotalHorasCliente,
                    Dif_Total_Horas = x.DiferenciaTotalHoras,
                    Estado = x.Coincide ? "COINCIDE" : "DIFERENCIA"
                }).ToList();

                // Hoja 3: Turnos Faltantes en Sinsa (Solo en Bussersa)
                var faltantesExport = resultadoConciliacion.TurnosFaltantes.Select(x => new
                {
                    No = x.No,
                    ID_Empleado = x.IdEmpleado,
                    Colaborador = x.NombreEmpleado,
                    Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                    Dia_Semana = x.DiaSemana,
                    Area = x.Area,
                    Puesto = x.Cargo,
                    Turno = x.Turno,
                    Horas_Ord = x.HorasOrdCobrables,
                    Horas_Ext = x.HorasExtCobrables,
                    Total_Horas = x.TotalHorasCobrables,
                    Diagnostico = x.MotivoJustificacion
                }).ToList();

                // Hoja 4: Solo en Sinsa (No en Bussersa)
                var soloClienteExport = resultadoConciliacion.RegistrosSoloCliente.Select(x => new
                {
                    No = x.No,
                    Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                    Colaborador = x.NombreEmpleado,
                    Area_Bodega = x.Bodega,
                    Puesto = x.Puesto,
                    Horas_Ord = x.HorasOrd,
                    Horas_Ext = x.HorasExt,
                    Total_Horas = x.TotalHoras,
                    Diagnostico = x.Diagnostico
                }).ToList();

                // Hoja 5: Diferencias en Horas mismo Colaborador y Fecha
                var diferenciasExport = resultadoConciliacion.DiferenciasDiasComunes.Select(x => new
                {
                    No = x.No,
                    Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                    Colaborador = x.NombreEmpleado,
                    Area_Bussersa = x.AreaBussersa,
                    Bodega_Sinsa = x.BodegaCliente,
                    Puesto = x.Cargo,
                    Horas_Ord_Bussersa = x.HorasOrdBussersa,
                    Horas_Ord_Sinsa = x.HorasOrdCliente,
                    Dif_Ord = x.DiferenciaHorasOrd,
                    Horas_Ext_Bussersa = x.HorasExtBussersa,
                    Horas_Ext_Sinsa = x.HorasExtCliente,
                    Dif_Ext = x.DiferenciaHorasExt,
                    Dif_Total = x.DiferenciaHoras
                }).ToList();

                var request = new MultiSheetExcelRequest
                {
                    Hojas = new List<ExcelRequest>
                    {
                        new ExcelRequest { Hoja = "Consolidado Area y Puesto", Datos = Modelo.Validaciones.Util.ToDictionaryList(consolidadoExport), IncludeHeader = true },
                        new ExcelRequest { Hoja = "Consolidado por Area", Datos = Modelo.Validaciones.Util.ToDictionaryList(soloAreaExport), IncludeHeader = true },
                        new ExcelRequest { Hoja = "Faltantes en Sinsa", Datos = Modelo.Validaciones.Util.ToDictionaryList(faltantesExport), IncludeHeader = true },
                        new ExcelRequest { Hoja = "Solo en Sinsa", Datos = Modelo.Validaciones.Util.ToDictionaryList(soloClienteExport), IncludeHeader = true },
                        new ExcelRequest { Hoja = "Diferencias de Horas", Datos = Modelo.Validaciones.Util.ToDictionaryList(diferenciasExport), IncludeHeader = true }
                    }
                };

                // Hoja 6: Supervisores Excluidos (Horas Asumidas por Bussersa)
                if (resultadoConciliacion.SupervisoresExcluidos != null && resultadoConciliacion.SupervisoresExcluidos.Any())
                {
                    var supervisoresExport = resultadoConciliacion.SupervisoresExcluidos.Select(x => new
                    {
                        No = x.No,
                        ID_Empleado = x.IdEmpleado,
                        Colaborador = x.NombreEmpleado,
                        Fecha = x.Fecha.ToString("yyyy-MM-dd"),
                        Area = x.Area,
                        Cargo = x.Cargo,
                        Horas_Ord = x.HorasOrd,
                        Horas_Ext = x.HorasExt,
                        Total_Horas = x.TotalHoras,
                        Motivo = x.MotivoExclusion
                    }).ToList();

                    request.Hojas.Add(new ExcelRequest
                    {
                        Hoja = "Supervisores Excluidos",
                        Datos = Modelo.Validaciones.Util.ToDictionaryList(supervisoresExport),
                        IncludeHeader = true
                    });
                }

                var response = await OdooService.GenerateExcel(request);
                if (!response.Respuesta.ExisteError && response.Model != null)
                {
                    string base64File = response.Model.File;
                    string cliente = resultadoConciliacion.NombreCliente.Replace(" ", "_");
                    string fchaStr = DateTime.Now.ToString("ddMMyy");
                    await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64File, $"Conciliacion_Consolidado_{cliente}_{fchaStr}.xlsx");
                    Snackbar.Add("Informe de Conciliación descargado con éxito.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar Excel: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al exportar: " + ex.Message, Severity.Error);
            }
            finally
            {
                isloaddata = false;
                StateHasChanged();
            }
        }

        protected async Task ConfirmarCierre()
        {
            var listaCierre = PayLoadListFiltrada;
            int totalExcluidos = excluirSupervisores ? CantidadSupervisoresExcluidos : 0;
            decimal horasExcluidas = excluirSupervisores ? TotalHorasSupervisoresExcluidas : 0;

            string mensajeAlerta = $"¿Desea cerrar {listaCierre.Count} registros facturables?";
            if (totalExcluidos > 0)
            {
                mensajeAlerta += $"\n\n🛡️ REGLA SUPERVISORES: Se excluyen {totalExcluidos} marcaciones ({horasExcluidas:N2}h asumidas por Bussersa).";
            }
            if (TieneConciliacion)
            {
                if (resultadoConciliacion.ConsolidadoCoincideTotalmente)
                {
                    mensajeAlerta += "\n\n✅ La conciliación coincide al 100% con Sinsa por área y puesto.";
                }
                else
                {
                    mensajeAlerta += $"\n\n⚠️ ADVERTENCIA: La conciliación presenta {resultadoConciliacion.AreasConDiferencia} área(s) con diferencias (Diferencia global: {resultadoConciliacion.DiferenciaTotalHorasGlobal:+0.00;-0.00;0.00}h).";
                }
            }

            bool? result = await DialogService.ShowMessageBox("Confirmar Cierre de Marcadas", mensajeAlerta, yesText: "Confirmar Cierre", cancelText: "Cancelar");
            if (result == true) await EjecutarCierreMarcadas(listaCierre);
        }

        protected async Task EjecutarCierreMarcadas(List<DiasTrabajadosAreas>? listaACerrar = null)
        {
            isloaddata = true;
            try
            {
                var items = listaACerrar ?? PayLoadListFiltrada;
                var response = await _mediator.Send(new AddCierreMarcadasCommand(items, operacion));
                if (!response.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Cierre exitoso ({items.Count} registros cerrados).", Severity.Success);
                    PayLoadList.Clear();
                    LimpiarConciliacionSilencioso();
                    BotonEnabled = true;
                }
                else Snackbar.Add(response.Respuesta.MensajeError, Severity.Error);
            }
            finally { isloaddata = false; }
        }

        protected async Task OnDesdeDateChanged(DateTime? value)
        {
            _fchaDesdeWrapper = value;
            if (operacion > 0)
            {
                await GetAllMarcadasFacturar();
            }
        }

        protected async Task OnHastaDateChanged(DateTime? value)
        {
            _fchaHastaWrapper = value;
            if (operacion > 0)
            {
                await GetAllMarcadasFacturar();
            }
        }
    }
}
