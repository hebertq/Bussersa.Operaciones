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

        protected async Task GuardarReglasConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(ReglasConfig);
                await JS.InvokeVoidAsync("localStorage.setItem", "config_reglas_cobro_horas", json);
                Snackbar.Add("Reglas de cobro guardadas exitosamente.", Severity.Success);
                mostrarModalReglas = false;
                AplicarFiltrosYTotales();
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al guardar reglas de cobro: " + ex.Message, Severity.Error);
            }
        }

        protected async Task RestablecerReglasDefault()
        {
            ReglasConfig = new ReglasCobroConfig();
            await GuardarReglasConfig();
            Snackbar.Add("Reglas restablecidas a los valores oficiales por defecto.", Severity.Info);
        }

        public static bool ContienePalabraClave(string? texto, string keywordsCsv)
        {
            if (string.IsNullOrWhiteSpace(texto) || string.IsNullOrWhiteSpace(keywordsCsv)) return false;
            var keywords = keywordsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var kw in keywords)
            {
                if (texto.Contains(kw, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        protected void CalcularHorasCobrables(ReporteCierreMarcadasDetalle item)
        {
            if (item == null) return;

            // Si las reglas de cobro están desactivadas, conservamos marcación real sin ajuste
            if (!ReglasConfig.AplicarReglas)
            {
                item.HorasNormalesCobrables = item.horas_totales;
                item.HorasExtrasCobrables = item.horas_extras;
                item.ReglaAplicada = "Marcación Real";
                return;
            }

            string cargo = item.cargo ?? "";
            string turno = item.Turno ?? "";

            // Cálculo de tiempo transcurrido físico
            decimal horasTranscurridas = 0;
            if (TryParseHora(item.entrada_movimiento, out var tEntrada) && TryParseHora(item.salida_movimiento, out var tSalida) && tEntrada != tSalida)
            {
                int minEntrada = (int)tEntrada.TotalMinutes;
                int minSalida = (int)tSalida.TotalMinutes;
                if (minSalida <= minEntrada) minSalida += 1440;
                horasTranscurridas = Math.Round((decimal)(minSalida - minEntrada) / 60m, 2);
            }

            decimal totalHorasMarcadas = item.horas_totales + item.horas_extras;
            decimal horasEfectivas = horasTranscurridas > 0 ? horasTranscurridas : totalHorasMarcadas;

            // 1. REGLA: CONDUCTORES / CHOFERES
            // Pago y cobro fijo de 8 horas normales, 0 extras siempre
            if (ReglasConfig.ConductoresActivo && ContienePalabraClave(cargo, ReglasConfig.ConductoresKeywords))
            {
                item.HorasNormalesCobrables = ReglasConfig.ConductoresHorasFijas;
                item.HorasExtrasCobrables = 0.0m;
                item.ReglaAplicada = $"Conductor (Fijo {ReglasConfig.ConductoresHorasFijas:N0}N)";
                return;
            }

            // 2. REGLA: OPERADORES DE MONTACARGA
            // Turno de 12 horas diurno o nocturno se cobra como 8 normales y 4 extras
            if (ReglasConfig.MontacargasActivo && ContienePalabraClave(cargo, ReglasConfig.MontacargasKeywords))
            {
                if (horasEfectivas >= 11.0m || totalHorasMarcadas >= 11.0m)
                {
                    item.HorasNormalesCobrables = ReglasConfig.MontacargasHorasNormales;
                    decimal excedenteSobre12h = Math.Max(0.0m, horasEfectivas - 12.0m);
                    item.HorasExtrasCobrables = ReglasConfig.MontacargasHorasExtras12h + excedenteSobre12h;
                    item.ReglaAplicada = $"Montacarga 12h ({item.HorasNormalesCobrables:N0}N + {item.HorasExtrasCobrables:N1}E)";
                }
                else if (horasEfectivas >= ReglasConfig.MontacargasHorasNormales)
                {
                    item.HorasNormalesCobrables = ReglasConfig.MontacargasHorasNormales;
                    item.HorasExtrasCobrables = horasEfectivas - ReglasConfig.MontacargasHorasNormales;
                    item.ReglaAplicada = $"Montacarga ({item.HorasNormalesCobrables:N0}N + {item.HorasExtrasCobrables:N1}E)";
                }
                else
                {
                    item.HorasNormalesCobrables = horasEfectivas > 0 ? horasEfectivas : item.horas_totales;
                    item.HorasExtrasCobrables = 0.0m;
                    item.ReglaAplicada = "Montacarga (Parcial)";
                }
                return;
            }

            // 3. REGLA: OPERADORES DE ALMACÉN / OPERARIOS / BODEGA
            // Diurno: 8h normales, excedente a extras.
            // Nocturno: Jornada de 7h se factura como 8h normales; excedente sobre 7h pasa a horas extras.
            if (ReglasConfig.AlmacenActivo && ContienePalabraClave(cargo, ReglasConfig.AlmacenKeywords))
            {
                bool esNocturnoOMixto = turno.Equals("Nocturno", StringComparison.OrdinalIgnoreCase) ||
                                       turno.Equals("Mixto", StringComparison.OrdinalIgnoreCase);

                if (esNocturnoOMixto)
                {
                    decimal horasReales = item.horas_totales > 0 ? item.horas_totales : horasEfectivas;
                    if (horasReales >= 6.5m)
                    {
                        item.HorasNormalesCobrables = ReglasConfig.AlmacenHorasNormalesNocturnoCobro;
                        decimal excesoSobre7h = Math.Max(0.0m, horasReales - ReglasConfig.AlmacenHorasBaseNocturno);
                        item.HorasExtrasCobrables = item.horas_extras + excesoSobre7h;
                        item.ReglaAplicada = $"Almacén {turno} ({ReglasConfig.AlmacenHorasBaseNocturno:N0}h → {item.HorasNormalesCobrables:N0}N)";
                    }
                    else
                    {
                        item.HorasNormalesCobrables = horasReales;
                        item.HorasExtrasCobrables = item.horas_extras;
                        item.ReglaAplicada = $"Almacén {turno} (Parcial)";
                    }
                }
                else
                {
                    decimal hTotales = item.horas_totales > 0 ? item.horas_totales : horasEfectivas;
                    if (hTotales >= ReglasConfig.AlmacenHorasNormalesDiurno)
                    {
                        item.HorasNormalesCobrables = ReglasConfig.AlmacenHorasNormalesDiurno;
                        item.HorasExtrasCobrables = item.horas_extras + (hTotales - ReglasConfig.AlmacenHorasNormalesDiurno);
                    }
                    else
                    {
                        item.HorasNormalesCobrables = hTotales;
                        item.HorasExtrasCobrables = item.horas_extras;
                    }
                    item.ReglaAplicada = $"Almacén {turno} ({item.HorasNormalesCobrables:N0}N + {item.HorasExtrasCobrables:N1}E)";
                }
                return;
            }

            // 4. REGLA POR DEFECTO: OTROS PUESTOS (Tope 8N, exceso a extras)
            decimal ht = item.horas_totales > 0 ? item.horas_totales : horasEfectivas;
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
