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
                else if (response?.Respuesta != null && response.Respuesta.ExisteError)
                {
                    Snackbar.Add(response.Respuesta.MensajeError ?? "Error al consultar los registros del cliente.", Severity.Error);
                }
                else
                {
                    Snackbar.Add("No se encontraron registros para la consulta realizada.", Severity.Info);
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

        protected string ReglasResumenTexto
        {
            get
            {
                if (!ReglasConfig.AplicarReglas) return "⚙️ Marcación Real (Sin reglas de cobro)";
                if (DatosCierre == null || !DatosCierre.Any()) return "⚖️ Reglas de Cobro Activas";

                var reglasDistintas = DatosCierre
                    .Select(x => x.ReglaAplicada)
                    .Where(r => !string.IsNullOrWhiteSpace(r) && r != "Estándar" && r != "Feriado" && r != "Marcación Real")
                    .Distinct()
                    .ToList();

                if (reglasDistintas.Any())
                {
                    return $"⚖️ Reglas Especiales: {string.Join(" | ", reglasDistintas)}";
                }

                return "⚖️ Reglas Estándar (Jornada base 8h)";
            }
        }

        protected int modalOperacionId = 0;
        protected string NombreOperacionModal =>
            PayLoadOper.FirstOrDefault(x => x.id == modalOperacionId)?.nombre ?? (modalOperacionId > 0 ? $"Operación #{modalOperacionId}" : "Seleccione Operación");

        protected List<Modelo.Entidades.Operaciones.ReglaCobroClienteDto> ReglasCliente = new();
        protected Modelo.Entidades.Operaciones.ReglaCobroClienteDto editingRegla = new();
        protected bool cargandoReglas = false;
        protected bool guardandoRegla = false;
        protected List<Combos> ListaCargosHr = new();
        protected bool permitirCargoManual = false;

        protected async Task CargarCargosHr()
        {
            if (ListaCargosHr != null && ListaCargosHr.Any()) return;
            try
            {
                var res = await OdooService.GetHrJobs();
                if (res != null && res.Model != null)
                {
                    ListaCargosHr = res.Model.OrderBy(x => x.nombre).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar cargos hr_job: {ex.Message}");
            }
        }

        protected async Task AbrirModalReglas()
        {
            if (operacionId > 0)
            {
                modalOperacionId = operacionId;
            }
            else if (PayLoadOper != null && PayLoadOper.Any())
            {
                var glacial = PayLoadOper.FirstOrDefault(x => x.id == 28 || (x.nombre != null && x.nombre.Contains("Glacial", StringComparison.OrdinalIgnoreCase)));
                modalOperacionId = glacial?.id ?? PayLoadOper.First().id;
            }
            else
            {
                modalOperacionId = 0;
            }

            LimpiarFormularioEdicion();
            mostrarModalReglas = true;
            StateHasChanged();

            await CargarCargosHr();

            if (modalOperacionId > 0)
            {
                await CargarReglasDeOperacion(modalOperacionId);
            }
        }

        protected async Task OnModalOperacionChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int opId))
            {
                modalOperacionId = opId;
                LimpiarFormularioEdicion();
                if (modalOperacionId > 0)
                {
                    await CargarReglasDeOperacion(modalOperacionId);
                }
                else
                {
                    ReglasCliente.Clear();
                    StateHasChanged();
                }
            }
        }

        protected async Task CargarReglasDeOperacion(int opId)
        {
            cargandoReglas = true;
            StateHasChanged();
            try
            {
                var res = await OdooService.GetReglasCobroCliente(opId);
                ReglasCliente = res.Model?.OrderBy(x => x.Orden).ThenBy(x => x.Id).ToList() ?? new();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al cargar reglas: {ex.Message}", Severity.Error);
                ReglasCliente = new();
            }
            finally
            {
                cargandoReglas = false;
                StateHasChanged();
            }
        }

        protected void LimpiarFormularioEdicion()
        {
            editingRegla = new Modelo.Entidades.Operaciones.ReglaCobroClienteDto
            {
                Id = 0,
                OperacionId = modalOperacionId,
                NombreRegla = "",
                CargoPatron = "",
                TipoRegla = "MONTACARGA_12H",
                HorasNormales = 12.00m,
                HorasExtrasBase = 0.00m,
                HorasBaseTurno = 12.00m,
                Activa = true,
                Orden = ReglasCliente.Count + 1
            };
            permitirCargoManual = false;
            StateHasChanged();
        }

        protected void EditarRegla(Modelo.Entidades.Operaciones.ReglaCobroClienteDto regla)
        {
            editingRegla = new Modelo.Entidades.Operaciones.ReglaCobroClienteDto
            {
                Id = regla.Id,
                OperacionId = regla.OperacionId,
                NombreRegla = regla.NombreRegla,
                CargoPatron = regla.CargoPatron,
                TipoRegla = regla.TipoRegla,
                HorasNormales = regla.HorasNormales,
                HorasExtrasBase = regla.HorasExtrasBase,
                HorasBaseTurno = regla.HorasBaseTurno,
                Activa = regla.Activa,
                Orden = regla.Orden,
                FechaRegistro = regla.FechaRegistro,
                FechaActualizacion = regla.FechaActualizacion
            };
            permitirCargoManual = false;
            StateHasChanged();
        }

        protected void OnCargoChanged(ChangeEventArgs e)
        {
            string nuevoCargo = e.Value?.ToString() ?? "";
            editingRegla.CargoPatron = nuevoCargo;

            // Si el nombre de la regla está vacío o es un nombre por defecto de regla, sugerir nombre
            if (string.IsNullOrWhiteSpace(editingRegla.NombreRegla) || editingRegla.NombreRegla.StartsWith("Regla "))
            {
                if (!string.IsNullOrWhiteSpace(nuevoCargo))
                {
                    editingRegla.NombreRegla = $"Regla {nuevoCargo}";
                }
            }

            // Auto-sugerir TipoRegla y horas según el cargo oficial de hr_job
            if (!string.IsNullOrWhiteSpace(nuevoCargo))
            {
                string lower = nuevoCargo.ToLower();
                if (lower.Contains("montacarga"))
                {
                    editingRegla.TipoRegla = "MONTACARGA_12H";
                    editingRegla.HorasNormales = 12.00m;
                    editingRegla.HorasExtrasBase = 0.00m;
                    editingRegla.HorasBaseTurno = 12.00m;
                }
                else if (lower.Contains("almacen") || lower.Contains("bodega"))
                {
                    editingRegla.TipoRegla = "NOCTURNO_EQUIVALENCIA";
                    editingRegla.HorasNormales = 8.00m;
                    editingRegla.HorasExtrasBase = 0.00m;
                    editingRegla.HorasBaseTurno = 7.00m;
                }
                else if (lower.Contains("conductor") || lower.Contains("chofer"))
                {
                    editingRegla.TipoRegla = "FIJO_DIARIO";
                    editingRegla.HorasNormales = 8.00m;
                    editingRegla.HorasExtrasBase = 0.00m;
                    editingRegla.HorasBaseTurno = 8.00m;
                }
            }
        }

        protected void OnTipoReglaChanged(ChangeEventArgs e)
        {
            string nuevoTipo = e.Value?.ToString() ?? "PERSONALIZADA";
            editingRegla.TipoRegla = nuevoTipo;
            switch (nuevoTipo)
            {
                case "MONTACARGA_12H":
                    editingRegla.HorasNormales = 12.00m;
                    editingRegla.HorasExtrasBase = 0.00m;
                    editingRegla.HorasBaseTurno = 12.00m;
                    if (string.IsNullOrWhiteSpace(editingRegla.NombreRegla)) editingRegla.NombreRegla = "Operador de montacarga (12h)";
                    if (string.IsNullOrWhiteSpace(editingRegla.CargoPatron)) editingRegla.CargoPatron = "Operador de montacarga";
                    break;
                case "NOCTURNO_EQUIVALENCIA":
                    editingRegla.HorasNormales = 8.00m;
                    editingRegla.HorasExtrasBase = 0.00m;
                    editingRegla.HorasBaseTurno = 7.00m;
                    if (string.IsNullOrWhiteSpace(editingRegla.NombreRegla)) editingRegla.NombreRegla = "Operador de Almacén (Nocturno 7h -> 8h)";
                    if (string.IsNullOrWhiteSpace(editingRegla.CargoPatron)) editingRegla.CargoPatron = "Operador de almacen";
                    break;
                case "FIJO_DIARIO":
                    editingRegla.HorasNormales = 8.00m;
                    editingRegla.HorasExtrasBase = 0.00m;
                    editingRegla.HorasBaseTurno = 8.00m;
                    if (string.IsNullOrWhiteSpace(editingRegla.NombreRegla)) editingRegla.NombreRegla = "Conductor / Chofer (Fijo 8h)";
                    if (string.IsNullOrWhiteSpace(editingRegla.CargoPatron)) editingRegla.CargoPatron = "Conductor";
                    break;
                case "TOPE_DIARIO":
                    editingRegla.HorasNormales = 8.00m;
                    editingRegla.HorasExtrasBase = 0.00m;
                    editingRegla.HorasBaseTurno = 8.00m;
                    break;
            }
        }

        protected async Task GuardarReglaFormulario()
        {
            if (modalOperacionId == 0)
            {
                Snackbar.Add("Debe seleccionar una operación primero.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(editingRegla.NombreRegla))
            {
                Snackbar.Add("El nombre de la regla es requerido.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(editingRegla.CargoPatron))
            {
                Snackbar.Add("Debe indicar al menos una palabra clave de cargo.", Severity.Warning);
                return;
            }

            editingRegla.OperacionId = modalOperacionId;
            guardandoRegla = true;
            StateHasChanged();

            try
            {
                var listaActualizada = ReglasCliente.ToList();
                if (editingRegla.Id > 0)
                {
                    var idx = listaActualizada.FindIndex(x => x.Id == editingRegla.Id);
                    if (idx >= 0)
                    {
                        listaActualizada[idx] = editingRegla;
                    }
                    else
                    {
                        listaActualizada.Add(editingRegla);
                    }
                }
                else
                {
                    editingRegla.Orden = listaActualizada.Count + 1;
                    listaActualizada.Add(editingRegla);
                }

                var res = await OdooService.SaveReglasCobroCliente(modalOperacionId, listaActualizada);
                if (res.Respuesta != null && res.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al guardar en base de datos: {res.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    Snackbar.Add(editingRegla.Id > 0 ? "Regla actualizada en base de datos con éxito." : "Regla creada y guardada en base de datos con éxito.", Severity.Success);
                    await CargarReglasDeOperacion(modalOperacionId);
                    LimpiarFormularioEdicion();

                    if (modalOperacionId == operacionId && DatosCierreRaw != null && DatosCierreRaw.Any())
                    {
                        await ConsultarReporte();
                    }
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
            finally
            {
                guardandoRegla = false;
                StateHasChanged();
            }
        }

        protected async Task EliminarReglaDeGrilla(Modelo.Entidades.Operaciones.ReglaCobroClienteDto regla)
        {
            if (modalOperacionId == 0) return;

            guardandoRegla = true;
            StateHasChanged();
            try
            {
                var lista = ReglasCliente.Where(x => x.Id != regla.Id && x != regla).ToList();
                var res = await OdooService.SaveReglasCobroCliente(modalOperacionId, lista);
                if (res.Respuesta != null && res.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al eliminar regla: {res.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    Snackbar.Add("Regla eliminada de la base de datos.", Severity.Success);
                    if (editingRegla.Id == regla.Id)
                    {
                        LimpiarFormularioEdicion();
                    }
                    await CargarReglasDeOperacion(modalOperacionId);

                    if (modalOperacionId == operacionId && DatosCierreRaw != null && DatosCierreRaw.Any())
                    {
                        await ConsultarReporte();
                    }
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al eliminar: {ex.Message}", Severity.Error);
            }
            finally
            {
                guardandoRegla = false;
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
            // Si todas las horas están en extras (horas_totales = 0 y horas_extras > 0), es feriado en día diferente de domingo: 100% extras
            if (item.horas_totales == 0 && item.horas_extras > 0 && item.fecha_asistencia.DayOfWeek != DayOfWeek.Sunday)
            {
                item.HorasNormalesCobrables = 0.0m;
                item.HorasExtrasCobrables = item.horas_extras;
                item.ReglaAplicada = "Feriado";
                return;
            }

            // Si es domingo, trabajan por turnos y las horas registradas en extras representan su jornada de turno
            decimal ht = (item.fecha_asistencia.DayOfWeek == DayOfWeek.Sunday && item.horas_totales == 0)
                ? item.horas_extras
                : item.horas_totales;
            decimal he = (item.fecha_asistencia.DayOfWeek == DayOfWeek.Sunday && item.horas_totales == 0)
                ? 0.0m
                : item.horas_extras;

            if (ht >= 8.0m)
            {
                item.HorasNormalesCobrables = 8.0m;
                item.HorasExtrasCobrables = he + (ht - 8.0m);
            }
            else
            {
                item.HorasNormalesCobrables = ht;
                item.HorasExtrasCobrables = he;
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
