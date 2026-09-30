using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.ClasesGenericas;
using Modelo.Report;
using Modelo.Entidades.Entradas.Odoo;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Operaciones.ConsultaFacturacion
{
    public partial class ConsultaCierresMarcadas : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected HostService.Interfaces.IOdooService OdooService { get; set; }

        protected List<ReporteCierreMarcadasDetalle> DatosCierreRaw = new();
        protected List<ReporteCierreMarcadasDetalle> DatosCierre = new();
        protected List<ConsolidadoCierre> ResumenPorArea = new();
        protected List<Combos> PayLoadOper = new();
        protected List<int> YearsList = new();

        protected int operacionId = 0, anio = DateTime.Now.Year, mes = DateTime.Now.Month;
        protected bool esRango, isloading;
        protected DateTime? fchaInicio = DateTime.Now.Date.AddDays(-30), fchaFin = DateTime.Now.Date;
        protected string _searchMaestro = "", _searchDetalle = "";

        // Supervisores exclusion & catalog
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

        public class ConsolidadoCierre
        {
            public string Area { get; set; }
            public DateTime FechaMin { get; set; }
            public DateTime FechaMax { get; set; }
            public int DiasLaborados { get; set; }
            public decimal TotalHoras { get; set; }
            public decimal TotalExtras { get; set; }
            public int TotalColaboradores { get; set; }
        }

        protected IEnumerable<EmpleadoSupervisorItem> FilteredSupervisoresList =>
            string.IsNullOrWhiteSpace(_searchSupervisor)
                ? ListaEmpleadosSupervisores
                : ListaEmpleadosSupervisores.Where(x =>
                    (x.Nombre ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase) ||
                    x.IdEmpleado.ToString().Contains(_searchSupervisor) ||
                    (x.Area ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase) ||
                    (x.TipoEmpleado ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase));

        protected override async Task OnInitializedAsync()
        {
            YearsList = new List<int> { DateTime.Now.Year, DateTime.Now.Year - 1 };

            var res = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
            PayLoadOper = res.Model ?? new();

            await CargarSupervisoresConfig();
        }

        protected async Task ConsultarReporte()
        {
            if (operacionId == 0)
            {
                Snackbar.Add("Seleccione un cliente para consultar.", Severity.Warning);
                return;
            }

            isloading = true;
            try
            {
                var response = esRango
                    ? await _mediator.Send(new GetReporteCierreClienteRangoQuery(operacionId, fchaInicio ?? DateTime.Now, fchaFin ?? DateTime.Now))
                    : await _mediator.Send(new GetReporteCierreClienteMesQuery(operacionId, anio, mes));

                if (response.Model != null)
                {
                    DatosCierreRaw = response.Model.Select(x => new ReporteCierreMarcadasDetalle
                    {
                        fecha_asistencia = x.fecha_asistencia,
                        id_empleado = x.id_empleado,
                        nombre_empleado = x.nombre_empleado,
                        tipo_empleado = x.tipo_empleado,
                        area_nombre = x.area_nombre,
                        entrada_movimiento = x.entrada_movimiento,
                        salida_movimiento = x.salida_movimiento,
                        horas_totales = x.horas_totales,
                        horas_extras = x.horas_extras
                    }).ToList();

                    await CargarSupervisoresConfig();
                    AplicarFiltrosYTotales();

                    if (!DatosCierreRaw.Any()) Snackbar.Add("No se encontraron registros.", Severity.Info);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al consultar: " + ex.Message, Severity.Error);
            }
            finally { isloading = false; }
        }

        protected async Task CargarSupervisoresConfig()
        {
            try
            {
                string? jsonGlobal = await JS.InvokeAsync<string>("localStorage.getItem", "supervisores_catalogo_global");
                var ids = !string.IsNullOrEmpty(jsonGlobal) ? JsonSerializer.Deserialize<List<int>>(jsonGlobal) : null;
                supervisoresIds = ids != null ? new HashSet<int>(ids) : new HashSet<int>();

                if (operacionId > 0)
                {
                    string keyOper = $"supervisores_cliente_{operacionId}";
                    string? jsonOper = await JS.InvokeAsync<string>("localStorage.getItem", keyOper);
                    if (!string.IsNullOrEmpty(jsonOper))
                    {
                        var idsOper = JsonSerializer.Deserialize<List<int>>(jsonOper);
                        if (idsOper != null)
                        {
                            foreach (var id in idsOper) supervisoresIds.Add(id);
                        }
                    }
                }
            }
            catch
            {
                supervisoresIds = new HashSet<int>();
            }
        }

        protected async Task GuardarSupervisoresConfig()
        {
            try
            {
                supervisoresIds = new HashSet<int>(ListaEmpleadosSupervisores.Where(x => x.EsSupervisor).Select(x => x.IdEmpleado));
                string json = JsonSerializer.Serialize(supervisoresIds.ToList());
                await JS.InvokeVoidAsync("localStorage.setItem", "supervisores_catalogo_global", json);
                if (operacionId > 0)
                {
                    string keyOper = $"supervisores_cliente_{operacionId}";
                    await JS.InvokeVoidAsync("localStorage.setItem", keyOper, json);
                }

                Snackbar.Add("Catálogo de supervisores guardado exitosamente.", Severity.Success);
                mostrarModalSupervisores = false;
                AplicarFiltrosYTotales();
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al guardar supervisores: " + ex.Message, Severity.Error);
            }
        }

        protected bool EsSupervisor(ReporteCierreMarcadasDetalle item)
        {
            if (supervisoresIds.Contains(item.id_empleado)) return true;

            string tipo = item.tipo_empleado ?? "";
            string nombre = item.nombre_empleado ?? "";

            return tipo.Contains("SUPERVISOR", StringComparison.OrdinalIgnoreCase) ||
                   tipo.Contains("SUPERVISORA", StringComparison.OrdinalIgnoreCase) ||
                   nombre.Contains("SUPERVISOR", StringComparison.OrdinalIgnoreCase) ||
                   nombre.Contains("SUPERVISORA", StringComparison.OrdinalIgnoreCase);
        }

        protected async Task AbrirModalSupervisores()
        {
            isloading = true;
            StateHasChanged();
            try
            {
                await CargarSupervisoresConfig();

                List<EmpleadoSupervisorItem> items = new();

                // 1. Empleados de la consulta actual si existen
                if (DatosCierreRaw != null && DatosCierreRaw.Any())
                {
                    var queryItems = DatosCierreRaw
                        .GroupBy(x => x.id_empleado)
                        .Select(g => {
                            var first = g.First();
                            bool esSup = supervisoresIds.Contains(first.id_empleado) ||
                                         (!string.IsNullOrEmpty(first.tipo_empleado) && (first.tipo_empleado.Contains("SUPERVISOR", StringComparison.OrdinalIgnoreCase) || first.tipo_empleado.Contains("SUPERVISORA", StringComparison.OrdinalIgnoreCase))) ||
                                         (!string.IsNullOrEmpty(first.nombre_empleado) && (first.nombre_empleado.Contains("SUPERVISOR", StringComparison.OrdinalIgnoreCase) || first.nombre_empleado.Contains("SUPERVISORA", StringComparison.OrdinalIgnoreCase)));
                            return new EmpleadoSupervisorItem
                            {
                                IdEmpleado = first.id_empleado,
                                Nombre = first.nombre_empleado ?? $"Empleado #{first.id_empleado}",
                                Area = first.area_nombre ?? "N/A",
                                TipoEmpleado = first.tipo_empleado ?? "",
                                EsSupervisor = esSup
                            };
                        });
                    items.AddRange(queryItems);
                }

                // 2. Traer todos los empleados activos del sistema para configurar cualquier supervisor
                try
                {
                    var resEmp = await OdooService.GetAllActiveEmployeesForRotation();
                    if (resEmp?.Model != null)
                    {
                        var masterEmp = resEmp.Model.Select(e => new EmpleadoSupervisorItem
                        {
                            IdEmpleado = e.Id,
                            Nombre = e.Name,
                            Area = e.DepartmentName ?? "General",
                            TipoEmpleado = e.JobTitle ?? "Colaborador",
                            EsSupervisor = supervisoresIds.Contains(e.Id) ||
                                           (!string.IsNullOrEmpty(e.JobTitle) && (e.JobTitle.Contains("SUPERVISOR", StringComparison.OrdinalIgnoreCase) || e.JobTitle.Contains("SUPERVISORA", StringComparison.OrdinalIgnoreCase))) ||
                                           (!string.IsNullOrEmpty(e.Name) && (e.Name.Contains("SUPERVISOR", StringComparison.OrdinalIgnoreCase) || e.Name.Contains("SUPERVISORA", StringComparison.OrdinalIgnoreCase)))
                        });

                        foreach (var emp in masterEmp)
                        {
                            if (!items.Any(x => x.IdEmpleado == emp.IdEmpleado))
                            {
                                items.Add(emp);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback
                }

                ListaEmpleadosSupervisores = items.OrderBy(x => x.Nombre).ToList();
                _searchSupervisor = "";
                mostrarModalSupervisores = true;
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al abrir catálogo: {ex.Message}", Severity.Error);
            }
            finally
            {
                isloading = false;
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

        protected void AplicarFiltrosYTotales()
        {
            if (excluirSupervisores)
            {
                DatosCierre = DatosCierreRaw.Where(x => !EsSupervisor(x)).ToList();
            }
            else
            {
                DatosCierre = DatosCierreRaw.ToList();
            }

            ResumenPorArea = DatosCierre
                .GroupBy(x => x.area_nombre)
                .Select(g => new ConsolidadoCierre
                {
                    Area = g.Key ?? "SIN ÁREA",
                    FechaMin = g.Min(x => x.fecha_asistencia),
                    FechaMax = g.Max(x => x.fecha_asistencia),
                    DiasLaborados = g.Select(x => x.fecha_asistencia.Date).Distinct().Count(),
                    TotalHoras = g.Sum(x => x.horas_totales),
                    TotalExtras = g.Sum(x => x.horas_extras),
                    TotalColaboradores = g.Select(x => x.id_empleado).Distinct().Count()
                })
                .OrderBy(x => x.Area)
                .ToList();

            StateHasChanged();
        }

        protected int CantidadSupervisoresConfigurados => supervisoresIds.Count;
        protected int CantidadMarcacionesExcluidas => DatosCierreRaw.Count(EsSupervisor);

        protected async Task ExportarExcelCompleto()
        {
            if (DatosCierre == null || !DatosCierre.Any()) return;

            isloading = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                var consolidadoExport = ResumenPorArea.Select(x => new
                {
                    x.Area,
                    Desde = x.FechaMin.ToString("yyyy-MM-dd"),
                    Hasta = x.FechaMax.ToString("yyyy-MM-dd"),
                    Días = x.DiasLaborados,
                    Personal = x.TotalColaboradores,
                    Total_Horas = x.TotalHoras,
                    Total_Extras = x.TotalExtras
                }).ToList();

                var detalleExport = DatosCierre.Select(x => new
                {
                    Fecha = x.fecha_asistencia.ToString("yyyy-MM-dd"),
                    Área = x.area_nombre,
                    ID = x.id_empleado,
                    Nombre = x.nombre_empleado,
                    Entrada = x.entrada_movimiento,
                    Salida = x.salida_movimiento,
                    Horas = x.horas_totales,
                    Extras = x.horas_extras
                }).ToList();

                var request = new MultiSheetExcelRequest
                {
                    Hojas = new List<ExcelRequest>
                    {
                        new ExcelRequest { Hoja = "Consolidado por Area", Datos = Modelo.Validaciones.Util.ToDictionaryList(consolidadoExport), IncludeHeader = true },
                        new ExcelRequest { Hoja = "Detalle de Marcaciones", Datos = Modelo.Validaciones.Util.ToDictionaryList(detalleExport), IncludeHeader = true }
                    }
                };

                var response = await OdooService.GenerateExcel(request);
                if (!response.Respuesta.ExisteError && response.Model != null)
                {
                    string base64File = response.Model.File;
                    string cliente = PayLoadOper.FirstOrDefault(x => x.id == operacionId)?.nombre ?? "Reporte";
                    string fchaStr = DateTime.Now.ToString("ddMMyy");
                    await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64File, $"Cierre_{cliente.Replace(" ", "_")}_{fchaStr}.xlsx");
                    Snackbar.Add("Excel generado con éxito", Severity.Success);
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
                isloading = false;
            }
        }

        protected Func<ConsolidadoCierre, bool> _filterMaestro => x => string.IsNullOrWhiteSpace(_searchMaestro) || x.Area.Contains(_searchMaestro, StringComparison.OrdinalIgnoreCase);
        protected Func<ReporteCierreMarcadasDetalle, bool> _filterDetalle => x => string.IsNullOrWhiteSpace(_searchDetalle) || x.nombre_empleado.Contains(_searchDetalle, StringComparison.OrdinalIgnoreCase);
    }
}
