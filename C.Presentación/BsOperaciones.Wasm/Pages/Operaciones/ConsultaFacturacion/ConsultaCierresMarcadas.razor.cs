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
        protected List<EmpleadosActivos> EmpleadosActivosMaster = new();
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

        // Reglas de Cobro & Configuración
        protected bool mostrarModalReglas = false;
        protected ReglasCobroConfig ReglasConfig = new();

        protected IEnumerable<EmpleadoSupervisorItem> FilteredSupervisoresList =>
            string.IsNullOrWhiteSpace(_searchSupervisor)
                ? ListaEmpleadosSupervisores
                : ListaEmpleadosSupervisores.Where(x =>
                    (x.Nombre ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase) ||
                    x.IdEmpleado.ToString().Contains(_searchSupervisor) ||
                    (x.Area ?? "").Contains(_searchSupervisor, StringComparison.OrdinalIgnoreCase));

        protected override async Task OnInitializedAsync()
        {
            YearsList = new List<int> { DateTime.Now.Year, DateTime.Now.Year - 1 };

            var resOper = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
            PayLoadOper = resOper.Model ?? new();

            try
            {
                var resEmp = await _mediator.Send(new GetAllEmpleadosQuery());
                EmpleadosActivosMaster = resEmp.Model?.ToList() ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar GetAllEmpleadosQuery: " + ex.Message);
            }

            await CargarSupervisoresConfig();
            await CargarReglasConfig();
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
                        cargo = x.cargo,
                        entrada_movimiento = x.entrada_movimiento,
                        salida_movimiento = x.salida_movimiento,
                        horas_totales = x.horas_totales,
                        horas_extras = x.horas_extras,
                        HorasNormalesCobrables = x.horas_normales_cobrables,
                        HorasExtrasCobrables = x.horas_extras_cobrables,
                        ReglaAplicada = x.regla_aplicada,
                        Turno = DeterminarTurnoSegunLeyNicaragua(x.entrada_movimiento, x.salida_movimiento)
                    }).ToList();

                    await CargarSupervisoresConfig();
                    await CargarReglasConfig();
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
                string? json = await JS.InvokeAsync<string>("localStorage.getItem", "catalogo_supervisores_ids");
                if (!string.IsNullOrEmpty(json))
                {
                    var ids = JsonSerializer.Deserialize<List<int>>(json);
                    supervisoresIds = ids != null ? new HashSet<int>(ids) : new HashSet<int>();
                }
                else
                {
                    supervisoresIds = new HashSet<int>();
                    if (EmpleadosActivosMaster != null)
                    {
                        foreach (var emp in EmpleadosActivosMaster)
                        {
                            string nombre = emp.NombreCompleto ?? emp.NombreCorto ?? "";
                            if (nombre.Contains("SUPERVISOR", StringComparison.OrdinalIgnoreCase) || nombre.Contains("SUPERVISORA", StringComparison.OrdinalIgnoreCase))
                            {
                                supervisoresIds.Add(emp.IdNomina);
                            }
                        }
                    }
                    if (supervisoresIds.Any())
                    {
                        await GuardarSupervisoresConfig();
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
                await JS.InvokeVoidAsync("localStorage.setItem", "catalogo_supervisores_ids", json);

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
            if (item == null) return false;
            return supervisoresIds.Contains(item.id_empleado);
        }

        protected async Task AbrirModalSupervisores()
        {
            isloading = true;
            StateHasChanged();
            try
            {
                await CargarSupervisoresConfig();

                var items = new List<EmpleadoSupervisorItem>();

                if (EmpleadosActivosMaster != null && EmpleadosActivosMaster.Any())
                {
                    foreach (var emp in EmpleadosActivosMaster)
                    {
                        int id = emp.IdNomina;
                        string nombre = !string.IsNullOrWhiteSpace(emp.NombreCompleto) ? emp.NombreCompleto : (!string.IsNullOrWhiteSpace(emp.NombreCorto) ? emp.NombreCorto : $"Empleado #{id}");
                        bool isSelected = supervisoresIds.Contains(id);

                        items.Add(new EmpleadoSupervisorItem
                        {
                            IdEmpleado = id,
                            Nombre = nombre,
                            Area = !string.IsNullOrEmpty(emp.NoCedula) ? $"Cédula: {emp.NoCedula}" : "Empleado Activo",
                            TipoEmpleado = emp.ActivoInss ? "INSS Activo" : "Nómina",
                            EsSupervisor = isSelected
                        });
                    }
                }

                if (DatosCierreRaw != null && DatosCierreRaw.Any())
                {
                    foreach (var row in DatosCierreRaw)
                    {
                        int id = row.id_empleado;
                        if (!items.Any(x => x.IdEmpleado == id))
                        {
                            bool isSelected = supervisoresIds.Contains(id);
                            items.Add(new EmpleadoSupervisorItem
                            {
                                IdEmpleado = id,
                                Nombre = row.nombre_empleado ?? $"Empleado #{id}",
                                Area = row.area_nombre ?? "N/A",
                                TipoEmpleado = row.tipo_empleado ?? "Reporte",
                                EsSupervisor = isSelected
                            });
                        }
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

        protected async Task CargarReglasConfig()
        {
            try
            {
                string? json = await JS.InvokeAsync<string>("localStorage.getItem", "config_reglas_cobro_horas");
                if (!string.IsNullOrEmpty(json))
                {
                    var config = JsonSerializer.Deserialize<ReglasCobroConfig>(json);
                    if (config != null)
                    {
                        ReglasConfig = config;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar config_reglas_cobro_horas: " + ex.Message);
            }
            ReglasConfig = new ReglasCobroConfig();
        }

        protected string NombreClienteSeleccionado =>
            PayLoadOper.FirstOrDefault(x => x.id == operacionId)?.nombre ?? (operacionId > 0 ? $"Cliente #{operacionId}" : "Sin Cliente Seleccionado");

        protected List<Modelo.Entidades.Operaciones.ReglaCobroClienteDto> ReglasCliente = new();
        protected bool cargandoReglas = false;

        protected async Task AbrirModalReglas()
        {
            if (operacionId == 0)
            {
                Snackbar.Add("Seleccione un cliente para configurar sus reglas de cobro.", Severity.Warning);
                return;
            }

            cargandoReglas = true;
            mostrarModalReglas = true;
            StateHasChanged();

            try
            {
                var res = await OdooService.GetReglasCobroCliente(operacionId);
                ReglasCliente = res.Model?.ToList() ?? new();
                if (!ReglasCliente.Any())
                {
                    CargarPlantillaDefaultReglas();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al cargar reglas del cliente: {ex.Message}", Severity.Error);
            }
            finally
            {
                cargandoReglas = false;
                StateHasChanged();
            }
        }

        protected void CargarPlantillaDefaultReglas()
        {
            ReglasCliente = new List<Modelo.Entidades.Operaciones.ReglaCobroClienteDto>
            {
                new Modelo.Entidades.Operaciones.ReglaCobroClienteDto
                {
                    OperacionId = operacionId,
                    NombreRegla = "Operador de Montacargas (12h)",
                    CargoPatron = "montacarga, montacarguista",
                    TipoRegla = "MONTACARGA_12H",
                    HorasNormales = 8.00m,
                    HorasExtrasBase = 4.00m,
                    HorasBaseTurno = 12.00m,
                    Activa = true,
                    Orden = 1
                },
                new Modelo.Entidades.Operaciones.ReglaCobroClienteDto
                {
                    OperacionId = operacionId,
                    NombreRegla = "Operador de Almacén (Nocturno 7h -> 8h)",
                    CargoPatron = "almacen, bodega, operario, ayudante",
                    TipoRegla = "NOCTURNO_EQUIVALENCIA",
                    HorasNormales = 8.00m,
                    HorasExtrasBase = 0.00m,
                    HorasBaseTurno = 7.00m,
                    Activa = true,
                    Orden = 2
                },
                new Modelo.Entidades.Operaciones.ReglaCobroClienteDto
                {
                    OperacionId = operacionId,
                    NombreRegla = "Conductor / Chofer (Fijo 8h)",
                    CargoPatron = "conductor, chofer, motorista",
                    TipoRegla = "FIJO_DIARIO",
                    HorasNormales = 8.00m,
                    HorasExtrasBase = 0.00m,
                    HorasBaseTurno = 8.00m,
                    Activa = true,
                    Orden = 3
                }
            };
        }

        protected void AgregarNuevaRegla()
        {
            ReglasCliente.Add(new Modelo.Entidades.Operaciones.ReglaCobroClienteDto
            {
                OperacionId = operacionId,
                NombreRegla = $"Nueva Regla #{ReglasCliente.Count + 1}",
                CargoPatron = "",
                TipoRegla = "TOPE_DIARIO",
                HorasNormales = 8.00m,
                HorasExtrasBase = 0.00m,
                HorasBaseTurno = 8.00m,
                Activa = true,
                Orden = ReglasCliente.Count + 1
            });
        }

        protected void EliminarRegla(Modelo.Entidades.Operaciones.ReglaCobroClienteDto regla)
        {
            ReglasCliente.Remove(regla);
        }

        protected async Task GuardarReglasClienteEnDb()
        {
            if (operacionId == 0) return;

            cargandoReglas = true;
            StateHasChanged();
            try
            {
                var res = await OdooService.SaveReglasCobroCliente(operacionId, ReglasCliente);
                if (res.Respuesta != null && res.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al guardar reglas: {res.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    Snackbar.Add("Reglas de cobro guardadas exitosamente en la base de datos.", Severity.Success);
                    mostrarModalReglas = false;
                    await ConsultarReporte();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al guardar reglas: {ex.Message}", Severity.Error);
            }
            finally
            {
                cargandoReglas = false;
                StateHasChanged();
            }
        }

        protected void CalcularHorasCobrables(ReporteCierreMarcadasDetalle item)
        {
            if (item == null) return;

            // Si las reglas de cobro están desactivadas por el toggle, mostramos marcación real
            if (!ReglasConfig.AplicarReglas)
            {
                item.HorasNormalesCobrables = item.horas_totales;
                item.HorasExtrasCobrables = item.horas_extras;
                item.ReglaAplicada = "Marcación Real";
                return;
            }

            // Si ya vino pre-calculado desde PostgreSQL vía LEFT JOIN LATERAL
            if (!string.IsNullOrWhiteSpace(item.ReglaAplicada) && item.ReglaAplicada != "Marcación Real")
            {
                return;
            }

            // Fallback estándar si viniera sin pre-cálculo
            decimal ht = item.horas_totales;
            if (ht >= 8.0m)
            {
                item.HorasNormalesCobrables = 8.0m;
                item.HorasExtrasCobrables = item.horas_extras + (ht - 8.0m);
            }
            else
            {
                item.HorasNormalesCobrables = ht;
                item.HorasExtrasCobrables = item.horas_extras;
            }
            item.ReglaAplicada = "Estándar";
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

            foreach (var item in DatosCierre)
            {
                CalcularHorasCobrables(item);
            }

            if (ReglasConfig.ModoAgrupacion == "AreaCargo")
            {
                ResumenPorArea = DatosCierre
                    .GroupBy(x => new
                    {
                        Area = string.IsNullOrWhiteSpace(x.area_nombre) ? "SIN ÁREA" : x.area_nombre.Trim(),
                        Cargo = string.IsNullOrWhiteSpace(x.cargo) ? "SIN CARGO" : x.cargo.Trim(),
                        Turno = "CONSOLIDADO"
                    })
                    .Select(g => new ConsolidadoCierre
                    {
                        Area = g.Key.Area,
                        Cargo = g.Key.Cargo,
                        Turno = g.Key.Turno,
                        FechaMin = g.Min(x => x.fecha_asistencia),
                        FechaMax = g.Max(x => x.fecha_asistencia),
                        DiasLaborados = g.Select(x => x.fecha_asistencia.Date).Distinct().Count(),
                        TotalHoras = g.Sum(x => x.HorasNormalesCobrables),
                        TotalExtras = g.Sum(x => x.HorasExtrasCobrables),
                        TotalHorasReales = g.Sum(x => x.horas_totales),
                        TotalExtrasReales = g.Sum(x => x.horas_extras),
                        TotalColaboradores = g.Select(x => x.id_empleado).Distinct().Count()
                    })
                    .OrderBy(x => x.Area)
                    .ThenBy(x => x.Cargo)
                    .ToList();
            }
            else
            {
                ResumenPorArea = DatosCierre
                    .GroupBy(x => new
                    {
                        Area = string.IsNullOrWhiteSpace(x.area_nombre) ? "SIN ÁREA" : x.area_nombre.Trim(),
                        Cargo = string.IsNullOrWhiteSpace(x.cargo) ? "SIN CARGO" : x.cargo.Trim(),
                        Turno = string.IsNullOrWhiteSpace(x.Turno) ? "SIN TURNO" : x.Turno.Trim()
                    })
                    .Select(g => new ConsolidadoCierre
                    {
                        Area = g.Key.Area,
                        Cargo = g.Key.Cargo,
                        Turno = g.Key.Turno,
                        FechaMin = g.Min(x => x.fecha_asistencia),
                        FechaMax = g.Max(x => x.fecha_asistencia),
                        DiasLaborados = g.Select(x => x.fecha_asistencia.Date).Distinct().Count(),
                        TotalHoras = g.Sum(x => x.HorasNormalesCobrables),
                        TotalExtras = g.Sum(x => x.HorasExtrasCobrables),
                        TotalHorasReales = g.Sum(x => x.horas_totales),
                        TotalExtrasReales = g.Sum(x => x.horas_extras),
                        TotalColaboradores = g.Select(x => x.id_empleado).Distinct().Count()
                    })
                    .OrderBy(x => x.Area)
                    .ThenBy(x => x.Cargo)
                    .ThenBy(x => x.Turno)
                    .ToList();
            }

            StateHasChanged();
        }

        public static bool TryParseHora(string? str, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(str)) return false;
            str = str.Trim();
            if (TimeSpan.TryParse(str, out time)) return true;
            if (DateTime.TryParse(str, out var dt))
            {
                time = dt.TimeOfDay;
                return true;
            }
            return false;
        }

        public static string DeterminarTurnoSegunLeyNicaragua(string? entradaStr, string? salidaStr)
        {
            if (!TryParseHora(entradaStr, out var entrada) || !TryParseHora(salidaStr, out var salida))
            {
                return "Sin Turno";
            }

            if (entrada == salida)
            {
                return "Sin Turno";
            }

            int startMinutes = (int)entrada.TotalMinutes;
            int endMinutes = (int)salida.TotalMinutes;

            if (endMinutes <= startMinutes)
            {
                endMinutes += 1440; // Cruza la medianoche
            }

            int totalMinutes = endMinutes - startMinutes;
            if (totalMinutes <= 0 || totalMinutes > 1440 * 2) return "Sin Turno";

            int diurnos = 0;
            int nocturnos = 0;

            for (int m = startMinutes; m < endMinutes; m++)
            {
                int minDelDia = m % 1440;
                // Código del Trabajo de Nicaragua (Ley N° 185):
                // Art. 51: Jornada diurna es la que se ejecuta entre las 06:00 y las 20:00 (360 a 1200 minutos).
                // Art. 52: Jornada nocturna es la que se ejecuta entre las 20:00 y las 06:00 del día siguiente.
                if (minDelDia >= 360 && minDelDia < 1200)
                {
                    diurnos++;
                }
                else
                {
                    nocturnos++;
                }
            }

            // Art. 51: Diurno completo
            if (nocturnos == 0)
            {
                return "Diurno";
            }

            // Art. 52: Nocturno completo
            if (diurnos == 0)
            {
                return "Nocturno";
            }

            // Art. 53: Mixto
            // "siempre que el período nocturno sea menor de tres horas y media (210 minutos).
            // Si en la jornada mixta se laboran tres horas y media o más en el período nocturno,
            // se reputará trabajo nocturno para todos los efectos legales."
            if (nocturnos >= 210)
            {
                return "Nocturno";
            }

            return "Mixto";
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
                    x.Cargo,
                    x.Turno,
                    Desde = x.FechaMin.ToString("yyyy-MM-dd"),
                    Hasta = x.FechaMax.ToString("yyyy-MM-dd"),
                    Días = x.DiasLaborados,
                    Personal = x.TotalColaboradores,
                    Horas_Cobrables = x.TotalHoras,
                    Extras_Cobrables = x.TotalExtras,
                    Total_Cobrable = x.TotalHoras + x.TotalExtras,
                    Horas_Marcadas_Reales = x.TotalHorasReales,
                    Extras_Marcadas_Reales = x.TotalExtrasReales
                }).ToList();

                var detalleExport = DatosCierre.Select(x => new
                {
                    Fecha = x.fecha_asistencia.ToString("yyyy-MM-dd"),
                    Área = x.area_nombre,
                    Cargo = x.cargo,
                    x.Turno,
                    ID = x.id_empleado,
                    Nombre = x.nombre_empleado,
                    Entrada = x.entrada_movimiento,
                    Salida = x.salida_movimiento,
                    Horas_Marcadas = x.horas_totales,
                    Extras_Marcadas = x.horas_extras,
                    Horas_Cobrables = x.HorasNormalesCobrables,
                    Extras_Cobrables = x.HorasExtrasCobrables,
                    Regla_Aplicada = x.ReglaAplicada
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

        protected Func<ConsolidadoCierre, bool> _filterMaestro => x => string.IsNullOrWhiteSpace(_searchMaestro) ||
            (x.Area != null && x.Area.Contains(_searchMaestro, StringComparison.OrdinalIgnoreCase)) ||
            (x.Cargo != null && x.Cargo.Contains(_searchMaestro, StringComparison.OrdinalIgnoreCase)) ||
            (x.Turno != null && x.Turno.Contains(_searchMaestro, StringComparison.OrdinalIgnoreCase));
        protected Func<ReporteCierreMarcadasDetalle, bool> _filterDetalle => x => string.IsNullOrWhiteSpace(_searchDetalle) ||
            (x.nombre_empleado != null && x.nombre_empleado.Contains(_searchDetalle, StringComparison.OrdinalIgnoreCase)) ||
            (x.cargo != null && x.cargo.Contains(_searchDetalle, StringComparison.OrdinalIgnoreCase)) ||
            (x.Turno != null && x.Turno.Contains(_searchDetalle, StringComparison.OrdinalIgnoreCase)) ||
            (x.area_nombre != null && x.area_nombre.Contains(_searchDetalle, StringComparison.OrdinalIgnoreCase));
    }
}
