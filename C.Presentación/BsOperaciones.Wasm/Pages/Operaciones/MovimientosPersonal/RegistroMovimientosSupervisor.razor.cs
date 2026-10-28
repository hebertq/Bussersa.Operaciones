using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HostService.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Modelo.Admin;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using MudBlazor;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace BsOperaciones.Pages.Operaciones.MovimientosPersonal
{
    public class EmpleadoJornadaItem
    {
        public int Id { get; set; }
        public int IdEmpleado { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Puesto { get; set; } = "6-Operador de almacen";
        public string TurnoNombre { get; set; } = "Diurno";
        public string HoraEntrada { get; set; } = "06:00";
        public string HoraSalida { get; set; } = "14:00";
        public string Cliente { get; set; } = string.Empty;
        public decimal VtcoTrasp { get; set; }
        public decimal VtcoAlim { get; set; }
        public decimal BonoBnc { get; set; }
        public string Bnc
        {
            get => BonoBnc > 0 ? BonoBnc.ToString("N2", CultureInfo.InvariantCulture) : string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    BonoBnc = 0;
                else if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                    BonoBnc = val;
                else if (decimal.TryParse(value, NumberStyles.Any, new CultureInfo("es-NI"), out decimal valEs))
                    BonoBnc = valEs;
                else
                    BonoBnc = 0;
            }
        }
        public string Observaciones { get; set; } = string.Empty;
        public bool Asistio { get; set; } = true;
        public string Estado { get; set; } = "GUARDADO";
        public int? TicketRedmineId { get; set; }
        public int? TicketViaticoId { get; set; }
        public bool Seleccionado { get; set; }
        public bool Modificado { get; set; }
    }

    public class ConsolidadoHelpdeskItem
    {
        public int GrupoIndex { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string TurnoNombre { get; set; } = string.Empty;
        public string HoraInicio { get; set; } = string.Empty;
        public string HoraFin { get; set; } = string.Empty;
        public decimal VtcoTrasp { get; set; }
        public decimal VtcoAlim { get; set; }
        public decimal VtcoBnc { get; set; }
        public List<EmpleadoJornadaItem> Empleados { get; set; } = new();
        public int Cantidad => Empleados.Count;
        public string IdsEmpleadosTexto => string.Join(", ", Empleados.Select(e => e.CodigoEmpleado));
        public string NombresResumen => string.Join(", ", Empleados.Take(3).Select(e => e.NombreEmpleado)) + (Empleados.Count > 3 ? $" y {Empleados.Count - 3} más..." : "");
        public int? TicketRedmineId => Empleados.FirstOrDefault(e => e.TicketRedmineId.HasValue && e.TicketRedmineId > 0)?.TicketRedmineId;
        public int? TicketViaticoId => Empleados.FirstOrDefault(e => e.TicketViaticoId.HasValue && e.TicketViaticoId > 0)?.TicketViaticoId;
        public bool Sincronizado => TicketRedmineId.HasValue && TicketRedmineId.Value > 0;
    }

    public partial class RegistroMovimientosSupervisor : ComponentBase
    {
        [Inject] private IOdooService _odooService { get; set; } = null!;
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IDialogService _dialogService { get; set; } = null!;
        [Inject] private IJSRuntime _js { get; set; } = null!;
        [Inject] private NavigationManager _navigationManager { get; set; } = null!;
        [Inject] private AuthenticationStateProvider _authStateProvider { get; set; } = null!;
        [Inject] private IUserInfo _userInfo { get; set; } = null!;

        // Estado general
        protected bool estaCargando = true;
        protected bool estaGuardando = false;
        protected bool estaEnviandoHelpdesk = false;
        protected int activeTabIndex = 0;

        protected string currentUserName = string.Empty;
        protected List<Combos> operacionesList = new();
        protected int? selectedOperacionId;
        protected string clienteRedmineIdentificador = string.Empty;
        protected DateTime? fechaSeleccionada = DateTime.Today;

        // Catálogos
        protected List<OdooEmployeeDto> catalogoEmpleadosOdoo = new();
        protected List<string> cargosDisponibles = new()
        {
            "6-Operador de almacen",
            "12-Holler",
            "10-Operador de montacarga",
            "5-Supervisor de personal",
            "11-Conductor",
            "3-Asistentes de gerencia"
        };
        protected List<string> areasSugeridas = new();

        // Lista de empleados de la jornada activa (Grilla Tab 1)
        protected List<EmpleadoJornadaItem> empleadosJornada = new();

        // Filtro rápido de búsqueda en la grilla
        protected string textoBusqueda = string.Empty;
        protected string filtroAsistencia = "TODOS"; // "TODOS", "PRESENTES", "AUSENTES"

        // Lista de grupos consolidados para Helpdesk (Tab 2)
        protected List<ConsolidadoHelpdeskItem> gruposConsolidados = new();

        // Historial (Tab 3)
        protected DateTime? fechaHistorial = DateTime.Today.AddDays(-1);
        protected List<MovimientoPersonalDiarioDto> listaHistorial = new();
        protected bool cargandoHistorial = false;

        // Plantilla Personal del Supervisor (Tab 4)
        protected List<SupervisorPlantillaItemDto> miPlantillaPersonal = new();
        protected bool cargandoPlantilla = false;
        protected string textoBusquedaPlantilla = string.Empty;
        protected IEnumerable<SupervisorPlantillaItemDto> PlantillaFiltrada =>
            string.IsNullOrWhiteSpace(textoBusquedaPlantilla)
                ? miPlantillaPersonal
                : miPlantillaPersonal.Where(x =>
                    x.NombreEmpleado.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase) ||
                    x.CodigoEmpleado.ToString().Contains(textoBusquedaPlantilla) ||
                    x.ClienteDefault.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase) ||
                    x.AreaDefault.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase) ||
                    x.PuestoDefault.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase));

        // Catálogo de Áreas por Cliente (Tab 5)
        protected List<ClienteAreaDto> catalogoClienteAreas = new();
        protected Dictionary<string, List<string>> areasPorCliente = new(StringComparer.OrdinalIgnoreCase);
        protected int? filtroClienteAreaId = null;
        protected string filtroTextoArea = string.Empty;
        protected bool cargandoAreas = false;
        protected List<Combos> listaAreasAtributoOdoo = new();
        protected string nuevaAreaNombre = string.Empty;
        protected int? nuevoClienteAreaOpId = null;

        protected IEnumerable<ClienteAreaDto> CatalogoAreasFiltrado
        {
            get
            {
                var list = catalogoClienteAreas.AsEnumerable();
                if (filtroClienteAreaId.HasValue && filtroClienteAreaId.Value > 0)
                {
                    list = list.Where(x => x.OperacionId == filtroClienteAreaId.Value);
                }
                if (!string.IsNullOrWhiteSpace(filtroTextoArea))
                {
                    list = list.Where(x =>
                        (x.AreaNombre != null && x.AreaNombre.Contains(filtroTextoArea, StringComparison.OrdinalIgnoreCase)) ||
                        (x.ClienteNombre != null && x.ClienteNombre.Contains(filtroTextoArea, StringComparison.OrdinalIgnoreCase)));
                }
                return list.OrderBy(x => x.ClienteNombre).ThenBy(x => x.AreaNombre);
            }
        }

        // Totales calculados
        protected int TotalColaboradores => empleadosJornada.Count;
        protected int TotalPresentes => empleadosJornada.Count(x => x.Asistio);
        protected int TotalAusentes => empleadosJornada.Count(x => !x.Asistio);
        protected decimal TotalViaticosTrasp => empleadosJornada.Where(x => x.Asistio).Sum(x => x.VtcoTrasp);
        protected decimal TotalViaticosAlim => empleadosJornada.Where(x => x.Asistio).Sum(x => x.VtcoAlim);
        protected decimal TotalViaticosBnc => empleadosJornada.Where(x => x.Asistio).Sum(x => x.BonoBnc);
        protected decimal TotalViaticosGlobal => TotalViaticosTrasp + TotalViaticosAlim + TotalViaticosBnc;
        protected int CantidadSeleccionados => empleadosJornada.Count(x => x.Seleccionado);
        protected bool HayCambiosSinGuardar => empleadosJornada.Any(x => x.Modificado || x.Id == 0);

        // Clave de LocalStorage para borrador de seguridad por usuario y fecha
        private string DraftStorageKey => $"supervisor_jornada_draft_{currentUserName}_{fechaSeleccionada:yyyyMMdd}";

        protected override async Task OnInitializedAsync()
        {
            estaCargando = true;
            try
            {
                try
                {
                    var authState = await _authStateProvider.GetAuthenticationStateAsync();
                    currentUserName = authState.User.Identity?.Name ?? _userInfo.UserName ?? "";
                }
                catch
                {
                    currentUserName = _userInfo.UserName ?? "";
                }

                if (string.IsNullOrWhiteSpace(currentUserName))
                {
                    currentUserName = "Supervisor";
                }

                await PurgarBorradoresLocales();
                await CargarOperaciones();
                await CargarCatalogoClienteAreas();
                await RecuperarOperacionPrevia();
                await CargarMiPlantilla();
                await CargarJornadaActual();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al inicializar: {ex.Message}", Severity.Warning);
            }
            finally
            {
                estaCargando = false;
            }
        }

        private async Task CargarOperaciones()
        {
            try
            {
                var resp = await _odooService.GetAllCombosHost("Operaciones");
                if (resp?.Model != null)
                {
                    operacionesList = resp.Model.OrderBy(x => x.id).ToList();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar operaciones: {ex.Message}", Severity.Warning);
            }
        }

        private async Task RecuperarOperacionPrevia()
        {
            try
            {
                // No precargar con ningún ID por defecto; mostrar placeholder limpio "Seleccione de la lista..."
                selectedOperacionId = null;
                clienteRedmineIdentificador = string.Empty;
                areasSugeridas = new List<string>
                {
                    "Operaciones",
                    "WalMart Importado DCC",
                    "WalMart Importado descarga",
                    "WalMart Nestle",
                    "WalMart Despacho",
                    "WalMart Devoluciones",
                    "WalMart Farmacia",
                    "WalMart Operaciones",
                    "Comodin",
                    "Holler alisto",
                    "Holler movimientos",
                    "Tarima importado"
                };
            }
            catch
            {
                selectedOperacionId = null;
            }
        }

        protected async Task OnOperacionChanged(int? newOpId)
        {
            selectedOperacionId = newOpId;
            ActualizarIdentificadorRedmine();
            if (newOpId.HasValue && newOpId.Value > 0)
            {
                await CargarAreasOperacion();
            }
        }

        private void ActualizarIdentificadorRedmine()
        {
            if (!selectedOperacionId.HasValue)
            {
                clienteRedmineIdentificador = string.Empty;
                return;
            }

            var op = operacionesList.FirstOrDefault(x => x.id == selectedOperacionId.Value);
            if (op == null)
            {
                clienteRedmineIdentificador = $"{selectedOperacionId}-General";
                return;
            }

            string nombreLimpio = op.nombre.Trim();
            if (nombreLimpio.StartsWith($"{op.id}-", StringComparison.OrdinalIgnoreCase))
                clienteRedmineIdentificador = nombreLimpio;
            else
                clienteRedmineIdentificador = $"{op.id}-{nombreLimpio}";
        }

        private async Task CargarAreasOperacion()
        {
            if (!selectedOperacionId.HasValue) return;

            try
            {
                var resp = await _odooService.GetAreasOperacion(selectedOperacionId.Value);
                if (resp?.Model != null && resp.Model.Any())
                {
                    areasSugeridas = resp.Model.Distinct().ToList();
                }
                else
                {
                    areasSugeridas = new List<string>
                    {
                        "WalMart Importado DCC",
                        "WalMart Importado descarga",
                        "WalMart Nestle",
                        "WalMart Despacho",
                        "WalMart Devoluciones",
                        "WalMart Farmacia",
                        "WalMart Operaciones",
                        "Comodin",
                        "Holler alisto",
                        "Holler movimientos",
                        "Tarima importado"
                    };
                }
            }
            catch
            {
                areasSugeridas = new List<string>
                {
                    "WalMart Importado DCC",
                    "WalMart Nestle",
                    "WalMart Despacho",
                    "Comodin"
                };
            }
        }

        protected async Task CambiarFecha(int dias)
        {
            if (!fechaSeleccionada.HasValue) fechaSeleccionada = DateTime.Today;
            fechaSeleccionada = fechaSeleccionada.Value.AddDays(dias);
            await CargarJornadaActual();
        }

        protected async Task OnFechaSeleccionadaChanged(DateTime? nuevaFecha)
        {
            fechaSeleccionada = nuevaFecha;
            await CargarJornadaActual();
        }

        // --- CARGA DE LA JORNADA (Desde BD o Borrador) ---
        protected async Task CargarJornadaActual()
        {
            if (!fechaSeleccionada.HasValue) return;

            estaCargando = true;
            try
            {
                // 1. Consultar en PostgreSQL todos los registros del usuario actual para esta fecha (todos sus clientes)
                var resp = await _odooService.GetMovimientosPersonalDiario(fechaSeleccionada.Value, 0, currentUserName);
                if (resp?.Model != null && resp.Model.Any())
                {
                    empleadosJornada = resp.Model.Select(m => new EmpleadoJornadaItem
                    {
                        Id = m.Id,
                        IdEmpleado = m.IdEmpleado,
                        CodigoEmpleado = m.CodigoEmpleado,
                        NombreEmpleado = m.NombreEmpleado,
                        Area = m.Area,
                        Puesto = m.Puesto,
                        TurnoNombre = m.TurnoNombre,
                        HoraEntrada = m.HoraEntrada,
                        HoraSalida = m.HoraSalida,
                        Cliente = !string.IsNullOrWhiteSpace(m.Cliente)
                            ? m.Cliente
                            : (operacionesList.FirstOrDefault(o => o.id == m.OperacionId)?.nombre ?? "General"),
                        Bnc = m.Bnc ?? string.Empty,
                        VtcoTrasp = m.VtcoTrasp,
                        VtcoAlim = m.VtcoAlim,
                        Observaciones = m.Observaciones ?? string.Empty,
                        Asistio = m.Asistio,
                        Estado = m.Estado,
                        TicketRedmineId = m.TicketRedmineId,
                        TicketViaticoId = m.TicketViaticoId,
                        Seleccionado = false,
                        Modificado = false
                    }).ToList();

                    _snackbar.Add($"Jornada del {fechaSeleccionada:dd/MM/yyyy} cargada ({empleadosJornada.Count} registros de {currentUserName}).", Severity.Info);
                }
                else
                {
                    // Si no hay registros guardados en BD para esta fecha, la grilla queda limpia
                    empleadosJornada.Clear();
                }

                ActualizarGruposConsolidados();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar jornada: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
            }
        }

        // --- GESTIÓN DE PLANTILLA DE PERSONAL DEL SUPERVISOR ---
        protected async Task CargarMiPlantilla()
        {
            cargandoPlantilla = true;
            try
            {
                var resp = await _odooService.GetSupervisorPlantilla(currentUserName);
                if (resp?.Model != null)
                {
                    miPlantillaPersonal = resp.Model.ToList();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al consultar plantilla: {ex.Message}", Severity.Warning);
            }
            finally
            {
                cargandoPlantilla = false;
            }
        }

        protected async Task PrecargarDesdeMiPlantilla(bool forzarCarga = false)
        {
            if (!miPlantillaPersonal.Any())
            {
                await CargarMiPlantilla();
            }

            if (!miPlantillaPersonal.Any())
            {
                _snackbar.Add("No tienes colaboradores en tu plantilla. Puedes configurarlos en la pestaña 'Mi Personal' o guardar la jornada actual como tu plantilla.", Severity.Info);
                activeTabIndex = 3; // Cambiar a pestaña Mi Personal
                return;
            }

            if (!forzarCarga && empleadosJornada.Any())
            {
                bool? confirm = await _dialogService.ShowMessageBox(
                    "Cargar Mi Personal",
                    $"La grilla actual ya contiene {empleadosJornada.Count} colaboradores. ¿Deseas reemplazarla con tu plantilla de {miPlantillaPersonal.Count} colaboradores o añadir los faltantes?",
                    yesText: "Reemplazar Grilla",
                    noText: "Añadir Faltantes",
                    cancelText: "Cancelar");

                if (confirm == null) return;

                if (confirm == true)
                {
                    empleadosJornada.Clear();
                }
            }

            var existentes = empleadosJornada.Select(x => x.CodigoEmpleado).ToHashSet();
            int agregados = 0;

            foreach (var item in miPlantillaPersonal)
            {
                if (!existentes.Contains(item.CodigoEmpleado))
                {
                    string clienteDefault = !string.IsNullOrWhiteSpace(item.ClienteDefault) 
                        ? item.ClienteDefault 
                        : (operacionesList.FirstOrDefault()?.nombre ?? "General");

                    empleadosJornada.Add(new EmpleadoJornadaItem
                    {
                        Id = 0,
                        IdEmpleado = item.IdEmpleado,
                        CodigoEmpleado = item.CodigoEmpleado,
                        NombreEmpleado = item.NombreEmpleado,
                        Cliente = clienteDefault,
                        Area = !string.IsNullOrWhiteSpace(item.AreaDefault) ? item.AreaDefault : "Operaciones",
                        Puesto = !string.IsNullOrWhiteSpace(item.PuestoDefault) ? item.PuestoDefault : "6-Operador de almacen",
                        TurnoNombre = !string.IsNullOrWhiteSpace(item.TurnoDefault) ? item.TurnoDefault : "Diurno",
                        HoraEntrada = !string.IsNullOrWhiteSpace(item.HoraEntradaDefault) ? item.HoraEntradaDefault : "06:00",
                        HoraSalida = !string.IsNullOrWhiteSpace(item.HoraSalidaDefault) ? item.HoraSalidaDefault : "14:00",
                        VtcoTrasp = item.VtcoTraspDefault,
                        VtcoAlim = item.VtcoAlimDefault,
                        BonoBnc = item.BonoBncDefault,
                        Asistio = true,
                        Estado = "BORRADOR",
                        Modificado = true
                    });
                    agregados++;
                }
            }

            ActualizarGruposConsolidados();
            _snackbar.Add($"Se cargaron {agregados} colaboradores de tu plantilla con sus clientes iniciales.", Severity.Success);
        }

        protected async Task GuardarJornadaComoMiPlantilla()
        {
            if (!empleadosJornada.Any())
            {
                _snackbar.Add("No hay colaboradores en la grilla para guardar en la plantilla.", Severity.Warning);
                return;
            }

            bool? confirm = await _dialogService.ShowMessageBox(
                "Guardar como Mi Plantilla Permanente",
                $"¿Deseas guardar los {empleadosJornada.Count} colaboradores de la grilla como tu plantilla habitual de trabajo con sus clientes iniciales asignados?",
                yesText: "Sí, Guardar en Mi Plantilla",
                cancelText: "Cancelar");

            if (confirm != true) return;

            estaGuardando = true;
            try
            {
                var items = empleadosJornada.Select(e => new SupervisorPlantillaItemDto
                {
                    UsuarioSupervisor = currentUserName,
                    IdEmpleado = e.IdEmpleado,
                    CodigoEmpleado = e.CodigoEmpleado,
                    NombreEmpleado = e.NombreEmpleado,
                    ClienteDefault = e.Cliente,
                    AreaDefault = e.Area,
                    PuestoDefault = e.Puesto,
                    TurnoDefault = e.TurnoNombre,
                    HoraEntradaDefault = e.HoraEntrada,
                    HoraSalidaDefault = e.HoraSalida,
                    VtcoTraspDefault = e.VtcoTrasp,
                    VtcoAlimDefault = e.VtcoAlim,
                    BonoBncDefault = e.BonoBnc,
                    Activo = true
                }).ToList();

                var req = new GuardarSupervisorPlantillaRequest
                {
                    Usuario = currentUserName,
                    Items = items
                };

                await _odooService.SaveSupervisorPlantillaMasivo(req);
                await CargarMiPlantilla();
                _snackbar.Add($"✅ {items.Count} colaboradores guardados exitosamente en tu plantilla.", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al guardar plantilla: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaGuardando = false;
            }
        }

        protected async Task GuardarItemPlantilla(SupervisorPlantillaItemDto item)
        {
            try
            {
                item.UsuarioSupervisor = currentUserName;
                await _odooService.SaveSupervisorPlantillaItem(item);
                _snackbar.Add($"Actualizado #{item.CodigoEmpleado} {item.NombreEmpleado} ({item.ClienteDefault}).", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al actualizar: {ex.Message}", Severity.Warning);
            }
        }

        protected async Task EliminarItemPlantilla(SupervisorPlantillaItemDto item)
        {
            bool? confirm = await _dialogService.ShowMessageBox(
                "Quitar Colaborador de Mi Plantilla",
                $"¿Deseas quitar a {item.NombreEmpleado} (#{item.CodigoEmpleado}) de tu plantilla permanente?",
                yesText: "Sí, Quitar",
                cancelText: "Cancelar");

            if (confirm != true) return;

            try
            {
                await _odooService.DeleteSupervisorPlantillaItem(item.Id, currentUserName);
                miPlantillaPersonal.Remove(item);
                _snackbar.Add($"#{item.CodigoEmpleado} {item.NombreEmpleado} removido de tu plantilla.", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al remover: {ex.Message}", Severity.Error);
            }
        }

        // --- CLONAR JORNADA DE AYER ---
        protected async Task CopiarDeAyer()
        {
            if (!fechaSeleccionada.HasValue) return;

            DateTime fechaAyer = fechaSeleccionada.Value.AddDays(-1);
            bool? confirm = await _dialogService.ShowMessageBox(
                "Copiar Jornada Anterior",
                $"¿Deseas precargar la jornada del día {fechaAyer:dd/MM/yyyy} para la fecha activa {fechaSeleccionada:dd/MM/yyyy}?",
                yesText: "Sí, Copiar", cancelText: "Cancelar");

            if (confirm != true) return;

            estaCargando = true;
            try
            {
                var req = new ClonarMovimientosDiaAnteriorRequest
                {
                    OperacionId = selectedOperacionId ?? 0,
                    FechaOrigen = fechaAyer,
                    FechaDestino = fechaSeleccionada.Value,
                    Usuario = currentUserName
                };

                var res = await _odooService.ClonarMovimientosDiaAnterior(req);
                if (res?.Respuesta != null && !res.Respuesta.ExisteError)
                {
                    _snackbar.Add($"Jornada copiada exitosamente de {fechaAyer:dd/MM}.", Severity.Success);
                    await CargarJornadaActual();
                }
                else
                {
                    _snackbar.Add($"No se encontraron registros del {fechaAyer:dd/MM} para copiar.", Severity.Info);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al copiar jornada: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaCargando = false;
            }
        }

        // --- GUARDAR EN BASE DE DATOS ---
        protected async Task GuardarEnBaseDatos()
        {
            if (!fechaSeleccionada.HasValue) return;

            if (!empleadosJornada.Any())
            {
                _snackbar.Add("No hay movimientos para guardar.", Severity.Warning);
                return;
            }

            estaGuardando = true;
            try
            {
                var dtos = empleadosJornada.Select(e =>
                {
                    int opId = operacionesList.FirstOrDefault(x => x.nombre.Equals(e.Cliente, StringComparison.OrdinalIgnoreCase))?.id 
                               ?? (selectedOperacionId ?? 0);

                    return new MovimientoPersonalDiarioDto
                    {
                        Id = e.Id,
                        Fecha = fechaSeleccionada.Value,
                        OperacionId = opId,
                        Cliente = !string.IsNullOrWhiteSpace(e.Cliente) ? e.Cliente : "General",
                        IdEmpleado = e.IdEmpleado,
                        CodigoEmpleado = e.CodigoEmpleado,
                        NombreEmpleado = e.NombreEmpleado,
                        Puesto = e.Puesto,
                        Area = e.Area,
                        HoraEntrada = e.HoraEntrada,
                        HoraSalida = e.HoraSalida,
                        TurnoNombre = e.TurnoNombre,
                        Bnc = e.Bnc,
                        VtcoTrasp = e.VtcoTrasp,
                        VtcoAlim = e.VtcoAlim,
                        Observaciones = e.Observaciones,
                        Asistio = e.Asistio,
                        Estado = e.Estado == "SINCRONIZADO" ? "MODIFICADO" : "GUARDADO"
                    };
                }).ToList();

                var request = new GuardarMovimientosPersonalDiarioRequest
                {
                    OperacionId = selectedOperacionId ?? 0,
                    Fecha = fechaSeleccionada.Value,
                    Usuario = currentUserName,
                    Movimientos = dtos
                };

                var res = await _odooService.SaveMovimientosPersonalDiario(request);
                if (res?.Respuesta != null && !res.Respuesta.ExisteError)
                {
                    _snackbar.Add($"✅ {dtos.Count} movimientos guardados exitosamente en la base de datos.", Severity.Success);
                    await LimpiarBorradorLocal();
                    await CargarJornadaActual();
                }
                else
                {
                    _snackbar.Add($"Error al guardar: {res?.Respuesta?.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al guardar movimientos: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaGuardando = false;
            }
        }

        // --- ASIGNACIÓN MASIVA RÁPIDA ---
        protected async Task AbrirAsignacionMasiva()
        {
            var seleccionados = empleadosJornada.Where(x => x.Seleccionado).ToList();
            if (!seleccionados.Any())
            {
                _snackbar.Add("Selecciona al menos un empleado con la casilla de verificación.", Severity.Info);
                return;
            }

            var firstClient = seleccionados.FirstOrDefault()?.Cliente;
            var clientAreas = GetAreasParaCliente(firstClient);

            var parameters = new DialogParameters
            {
                ["CantidadSeleccionada"] = seleccionados.Count,
                ["OperacionesDisponibles"] = operacionesList,
                ["AreasPorCliente"] = areasPorCliente,
                ["AreasDisponibles"] = clientAreas,
                ["CargosDisponibles"] = cargosDisponibles,
                ["AreaInicial"] = seleccionados.First().Area,
                ["PuestoInicial"] = seleccionados.First().Puesto,
                ["ClienteInicial"] = firstClient ?? string.Empty
            };

            var dialog = await _dialogService.ShowAsync<DialogAsignacionMasivaTurno>("Asignación Masiva", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogAsignacionMasivaTurno.ResultadoAsignacionMasiva data)
            {
                foreach (var emp in seleccionados)
                {
                    emp.TurnoNombre = data.TurnoNombre;
                    emp.HoraEntrada = data.HoraEntrada;
                    emp.HoraSalida = data.HoraSalida;
                    if (!string.IsNullOrWhiteSpace(data.Area)) emp.Area = data.Area;
                    if (!string.IsNullOrWhiteSpace(data.Puesto)) emp.Puesto = data.Puesto;
                    if (!string.IsNullOrWhiteSpace(data.Cliente)) emp.Cliente = data.Cliente;
                    emp.VtcoTrasp = data.VtcoTrasp;
                    emp.VtcoAlim = data.VtcoAlim;
                    emp.BonoBnc = data.BonoBnc;
                    emp.Asistio = true;
                    emp.Modificado = true;
                    emp.Seleccionado = false;
                }

                _snackbar.Add($"Asignado turno, área y viáticos a {seleccionados.Count} colaboradores.", Severity.Success);
                ActualizarGruposConsolidados();
            }
        }

        // --- AGREGAR COLABORADOR MANUAL O DESDE ODOO ---
        protected async Task AbrirDialogoAgregarColaborador(string textoInicial = "")
        {
            string defaultOp = selectedOperacionId.HasValue 
                ? (operacionesList.FirstOrDefault(o => o.id == selectedOperacionId.Value)?.nombre ?? "") 
                : (operacionesList.FirstOrDefault()?.nombre ?? "WalMart");
            var defaultAreas = GetAreasParaCliente(defaultOp);

            var parameters = new DialogParameters
            {
                ["OperacionesDisponibles"] = operacionesList,
                ["AreasDisponibles"] = defaultAreas,
                ["AreasPorCliente"] = areasPorCliente,
                ["CargosDisponibles"] = cargosDisponibles,
                ["OperacionDefecto"] = defaultOp,
                ["AreaDefecto"] = defaultAreas.FirstOrDefault() ?? "Operaciones",
                ["TextoBusquedaInicial"] = textoInicial
            };

            var dialog = await _dialogService.ShowAsync<DialogAgregarColaboradorJornada>("Agregar Colaborador a la Jornada", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is EmpleadoJornadaItem nuevo)
            {
                var existente = empleadosJornada.FirstOrDefault(e => e.CodigoEmpleado == nuevo.CodigoEmpleado);
                if (existente != null)
                {
                    _snackbar.Add($"El colaborador #{nuevo.CodigoEmpleado} ({nuevo.NombreEmpleado}) ya se encuentra en la jornada.", Severity.Warning);
                    textoBusqueda = nuevo.CodigoEmpleado.ToString();
                    return;
                }

                empleadosJornada.Insert(0, nuevo);
                _snackbar.Add($"✅ #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado} agregado a la jornada para '{nuevo.Cliente}'.", Severity.Success);

                // Guardar también en la plantilla del supervisor si aún no estaba
                if (!miPlantillaPersonal.Any(p => p.CodigoEmpleado == nuevo.CodigoEmpleado))
                {
                    var itemP = new SupervisorPlantillaItemDto
                    {
                        UsuarioSupervisor = currentUserName,
                        IdEmpleado = nuevo.IdEmpleado,
                        CodigoEmpleado = nuevo.CodigoEmpleado,
                        NombreEmpleado = nuevo.NombreEmpleado,
                        ClienteDefault = nuevo.Cliente,
                        AreaDefault = nuevo.Area,
                        PuestoDefault = nuevo.Puesto,
                        TurnoDefault = nuevo.TurnoNombre,
                        HoraEntradaDefault = nuevo.HoraEntrada,
                        HoraSalidaDefault = nuevo.HoraSalida,
                        VtcoTraspDefault = nuevo.VtcoTrasp,
                        VtcoAlimDefault = nuevo.VtcoAlim,
                        BonoBncDefault = nuevo.BonoBnc,
                        Activo = true
                    };
                    await _odooService.SaveSupervisorPlantillaItem(itemP);
                    miPlantillaPersonal.Add(itemP);
                }

                ActualizarGruposConsolidados();
            }
        }

        // --- LIMPIAR GRILLA COMPLETA ---
        protected async Task LimpiarGrillaCompleta()
        {
            if (!empleadosJornada.Any()) return;

            bool? confirm = await _dialogService.ShowMessageBox(
                "Limpiar Grilla Diaria",
                $"¿Estás seguro de que deseas limpiar los {empleadosJornada.Count} colaboradores de la pantalla para esta fecha? Esta acción no borra la base de datos hasta que presiones Guardar.",
                yesText: "Sí, Limpiar Grilla", cancelText: "Cancelar");

            if (confirm == true)
            {
                empleadosJornada.Clear();
                await LimpiarBorradorLocal();
                ActualizarGruposConsolidados();
                _snackbar.Add("Grilla limpiada exitosamente.", Severity.Info);
            }
        }

        // --- EXPORTAR A EXCEL (.XLSX) ---
        protected async Task ExportarExcel()
        {
            if (!empleadosJornada.Any())
            {
                _snackbar.Add("No hay registros en la grilla para exportar.", Severity.Warning);
                return;
            }

            try
            {
                var workbook = new XSSFWorkbook();

                // Estilos
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
                currencyStyle.DataFormat = dataFormat.GetFormat("C$ #,##0.00;($#,##0.00);\"-\"");

                var sheet = workbook.CreateSheet($"Jornada_{fechaSeleccionada:yyyyMMdd}");

                string[] headers = new[]
                {
                    "#", "Fecha", "Carnet / Código", "Colaborador", "Cliente / Operación", "Asistencia",
                    "Área Operativa", "Cargo / Puesto", "Turno", "Hora Entrada", "Hora Salida",
                    "Viático Transporte (C$)", "Viático Alimentación (C$)", "Bono BNC (C$)", "Total Viáticos (C$)",
                    "Observaciones", "Estado", "Ticket Redmine", "Ticket Viático", "Supervisor"
                };

                var headerRow = sheet.CreateRow(0);
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                int rowIdx = 1;
                foreach (var emp in empleadosJornada)
                {
                    var row = sheet.CreateRow(rowIdx);
                    row.CreateCell(0).SetCellValue(rowIdx);
                    row.CreateCell(1).SetCellValue(fechaSeleccionada?.ToString("yyyy-MM-dd") ?? "");
                    row.CreateCell(2).SetCellValue(emp.CodigoEmpleado);
                    row.CreateCell(3).SetCellValue(emp.NombreEmpleado);
                    row.CreateCell(4).SetCellValue(emp.Cliente);
                    row.CreateCell(5).SetCellValue(emp.Asistio ? "PRESENTE" : "AUSENTE");
                    row.CreateCell(6).SetCellValue(emp.Area);
                    row.CreateCell(7).SetCellValue(emp.Puesto);
                    row.CreateCell(8).SetCellValue(emp.TurnoNombre);
                    row.CreateCell(9).SetCellValue(emp.HoraEntrada);
                    row.CreateCell(10).SetCellValue(emp.HoraSalida);

                    var cTrasp = row.CreateCell(11);
                    cTrasp.SetCellValue((double)emp.VtcoTrasp);
                    cTrasp.CellStyle = currencyStyle;

                    var cAlim = row.CreateCell(12);
                    cAlim.SetCellValue((double)emp.VtcoAlim);
                    cAlim.CellStyle = currencyStyle;

                    var cBnc = row.CreateCell(13);
                    cBnc.SetCellValue((double)emp.BonoBnc);
                    cBnc.CellStyle = currencyStyle;

                    var cTotal = row.CreateCell(14);
                    cTotal.SetCellValue((double)(emp.VtcoTrasp + emp.VtcoAlim + emp.BonoBnc));
                    cTotal.CellStyle = currencyStyle;

                    row.CreateCell(15).SetCellValue(emp.Observaciones);
                    row.CreateCell(16).SetCellValue(emp.Estado);
                    row.CreateCell(17).SetCellValue(emp.TicketRedmineId.HasValue ? emp.TicketRedmineId.Value.ToString() : "");
                    row.CreateCell(18).SetCellValue(emp.TicketViaticoId.HasValue ? emp.TicketViaticoId.Value.ToString() : "");
                    row.CreateCell(19).SetCellValue(currentUserName);

                    rowIdx++;
                }

                // Fila de Totales
                var totalRow = sheet.CreateRow(rowIdx);
                var totalLabelCell = totalRow.CreateCell(3);
                totalLabelCell.SetCellValue("TOTALES:");
                var boldFont = workbook.CreateFont();
                boldFont.IsBold = true;
                var totalStyle = workbook.CreateCellStyle();
                totalStyle.SetFont(boldFont);
                totalLabelCell.CellStyle = totalStyle;

                var sumTrasp = totalRow.CreateCell(11);
                sumTrasp.SetCellValue((double)TotalViaticosTrasp);
                sumTrasp.CellStyle = currencyStyle;

                var sumAlim = totalRow.CreateCell(12);
                sumAlim.SetCellValue((double)TotalViaticosAlim);
                sumAlim.CellStyle = currencyStyle;

                var sumBnc = totalRow.CreateCell(13);
                sumBnc.SetCellValue((double)TotalViaticosBnc);
                sumBnc.CellStyle = currencyStyle;

                var sumGlobal = totalRow.CreateCell(14);
                sumGlobal.SetCellValue((double)TotalViaticosGlobal);
                sumGlobal.CellStyle = currencyStyle;

                // Definir anchos de columna fijos compatibles con WebAssembly (evita System.Drawing.Common PlatformNotSupportedException)
                int[] colWidths = new[]
                {
                    6,  // 0: #
                    14, // 1: Fecha
                    18, // 2: Carnet / Código
                    34, // 3: Colaborador
                    24, // 4: Cliente / Operación
                    14, // 5: Asistencia
                    26, // 6: Área Operativa
                    28, // 7: Cargo / Puesto
                    14, // 8: Turno
                    14, // 9: Hora Entrada
                    14, // 10: Hora Salida
                    22, // 11: Viático Transporte (C$)
                    22, // 12: Viático Alimentación (C$)
                    18, // 13: Bono BNC (C$)
                    20, // 14: Total Viáticos (C$)
                    32, // 15: Observaciones
                    14, // 16: Estado
                    16, // 17: Ticket Redmine
                    16, // 18: Ticket Viático
                    26  // 19: Supervisor
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    int width = (i < colWidths.Length) ? colWidths[i] : 20;
                    sheet.SetColumnWidth(i, width * 256);
                }

                using var ms = new MemoryStream();
                workbook.Write(ms);
                byte[] bytes = ms.ToArray();
                string base64 = Convert.ToBase64String(bytes);
                string fileName = $"Jornada_Supervisor_{fechaSeleccionada:yyyyMMdd}_{currentUserName}.xlsx";

                await _js.InvokeVoidAsync("downloadFile", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64, fileName);
                _snackbar.Add($"✅ Archivo '{fileName}' descargado con éxito.", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al exportar Excel: {ex.Message}", Severity.Error);
            }
        }

        // --- SELECCIÓN Y EDICIÓN RÁPIDA EN GRILLA ---
        protected void MarcarTodos(bool marcar)
        {
            foreach (var e in EmpleadosFiltrados)
            {
                e.Seleccionado = marcar;
            }
        }

        protected void MarcarAsistenciaSeleccionados(bool asistio)
        {
            var sel = empleadosJornada.Where(x => x.Seleccionado).ToList();
            if (!sel.Any()) return;

            foreach (var e in sel)
            {
                e.Asistio = asistio;
                e.Modificado = true;
                e.Seleccionado = false;
            }

            _snackbar.Add($"Marcados {sel.Count} colaboradores como {(asistio ? "Presentes" : "Ausentes")}.", Severity.Info);
            ActualizarGruposConsolidados();
        }

        protected void AlternarTurnoRapido(EmpleadoJornadaItem item)
        {
            if (item.TurnoNombre == "Diurno")
            {
                item.TurnoNombre = "DCC";
                item.HoraEntrada = "06:00";
                item.HoraSalida = "15:30";
            }
            else if (item.TurnoNombre == "DCC")
            {
                item.TurnoNombre = "Mixto";
                item.HoraEntrada = "14:00";
                item.HoraSalida = "22:00";
            }
            else if (item.TurnoNombre == "Mixto")
            {
                item.TurnoNombre = "Nocturno";
                item.HoraEntrada = "22:00";
                item.HoraSalida = "06:00";
            }
            else
            {
                item.TurnoNombre = "Diurno";
                item.HoraEntrada = "06:00";
                item.HoraSalida = "14:00";
            }
            item.Modificado = true;
            ActualizarGruposConsolidados();
        }

        protected void AlternarViaticoRapido(EmpleadoJornadaItem item, string tipo)
        {
            if (tipo == "Trasp")
            {
                if (item.VtcoTrasp == 0) item.VtcoTrasp = 50;
                else if (item.VtcoTrasp == 50) item.VtcoTrasp = 100;
                else if (item.VtcoTrasp == 100) item.VtcoTrasp = 150;
                else item.VtcoTrasp = 0;
            }
            else if (tipo == "Alim")
            {
                if (item.VtcoAlim == 0) item.VtcoAlim = 50;
                else if (item.VtcoAlim == 50) item.VtcoAlim = 70;
                else if (item.VtcoAlim == 70) item.VtcoAlim = 100;
                else item.VtcoAlim = 0;
            }
            item.Modificado = true;
            ActualizarGruposConsolidados();
        }

        protected async Task EliminarEmpleadoDeJornada(EmpleadoJornadaItem item)
        {
            if (item.Id > 0)
            {
                bool? confirm = await _dialogService.ShowMessageBox(
                    "Quitar Colaborador",
                    $"¿Deseas quitar a {item.NombreEmpleado} (#{item.CodigoEmpleado}) de la jornada del {fechaSeleccionada:dd/MM/yyyy}?",
                    yesText: "Sí, Quitar",
                    cancelText: "Cancelar");

                if (confirm != true) return;

                try
                {
                    var resp = await _odooService.DeleteMovimientoPersonalDiario(item.Id);
                    if (resp?.Respuesta != null && !resp.Respuesta.ExisteError)
                    {
                        empleadosJornada.Remove(item);
                        ActualizarGruposConsolidados();
                        _snackbar.Add($"#{item.CodigoEmpleado} {item.NombreEmpleado} removido de la base de datos.", Severity.Success);
                    }
                    else
                    {
                        _snackbar.Add($"Error al eliminar de la base de datos: {resp?.Respuesta?.MensajeError}", Severity.Error);
                    }
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error al eliminar: {ex.Message}", Severity.Error);
                }
            }
            else
            {
                empleadosJornada.Remove(item);
                ActualizarGruposConsolidados();
                _snackbar.Add($"#{item.CodigoEmpleado} {item.NombreEmpleado} removido de la jornada.", Severity.Info);
            }
        }

        // --- FILTRADO DE LA GRILLA ---
        protected IEnumerable<EmpleadoJornadaItem> EmpleadosFiltrados
        {
            get
            {
                var query = empleadosJornada.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(textoBusqueda))
                {
                    query = query.Where(x =>
                        x.NombreEmpleado.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                        x.CodigoEmpleado.ToString().Contains(textoBusqueda) ||
                        x.IdEmpleado.ToString().Contains(textoBusqueda) ||
                        x.Area.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                        x.Puesto.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase) ||
                        x.Cliente.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase));
                }

                if (filtroAsistencia == "PRESENTES")
                    query = query.Where(x => x.Asistio);
                else if (filtroAsistencia == "AUSENTES")
                    query = query.Where(x => !x.Asistio);

                return query.OrderBy(x => x.Cliente).ThenBy(x => x.Area).ThenBy(x => x.NombreEmpleado);
            }
        }

        // --- CONSOLIDADO HELPDESK (Tab 2) ---
        private void ActualizarGruposConsolidados()
        {
            // Agrupación automática igual que Sheet 'Consolidado Agrupado' de OPM_Produccion_Diaria_Hrs.xlsx
            var presentes = empleadosJornada.Where(x => x.Asistio).ToList();

            var grupos = presentes
                .GroupBy(x => new
                {
                    Cliente = string.IsNullOrWhiteSpace(x.Cliente) 
                        ? (operacionesList.FirstOrDefault(o => o.id == selectedOperacionId)?.nombre ?? "General") 
                        : x.Cliente,
                    Area = string.IsNullOrWhiteSpace(x.Area) ? "Operaciones" : x.Area,
                    Puesto = string.IsNullOrWhiteSpace(x.Puesto) ? "6-Operador de almacen" : x.Puesto,
                    HoraInicio = x.HoraEntrada,
                    HoraFin = x.HoraSalida,
                    x.TurnoNombre,
                    x.VtcoTrasp,
                    x.VtcoAlim,
                    x.BonoBnc
                })
                .Select((g, idx) => new ConsolidadoHelpdeskItem
                {
                    GrupoIndex = idx + 1,
                    Cliente = g.Key.Cliente,
                    Area = g.Key.Area,
                    Puesto = g.Key.Puesto,
                    TurnoNombre = g.Key.TurnoNombre,
                    HoraInicio = g.Key.HoraInicio,
                    HoraFin = g.Key.HoraFin,
                    VtcoTrasp = g.Key.VtcoTrasp,
                    VtcoAlim = g.Key.VtcoAlim,
                    VtcoBnc = g.Key.BonoBnc,
                    Empleados = g.ToList()
                })
                .ToList();

            gruposConsolidados = grupos;
        }

        // --- ENVIAR A HELPDESK (REDMINE) ---
        protected async Task EnviarAHelpdeskMasivo()
        {
            if (!gruposConsolidados.Any())
            {
                _snackbar.Add("No hay grupos activos para enviar a Helpdesk.", Severity.Warning);
                return;
            }

            if (HayCambiosSinGuardar)
            {
                await GuardarEnBaseDatos();
            }

            int totalGrupos = gruposConsolidados.Count;
            int totalEmpleados = TotalPresentes;

            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Envío a Helpdesk",
                $"¿Estás seguro de enviar {totalGrupos} grupos de movimiento ({totalEmpleados} colaboradores) a Redmine Helpdesk para el cliente '{clienteRedmineIdentificador}'?",
                yesText: "Sí, Enviar a Helpdesk", cancelText: "Cancelar");

            if (confirm != true) return;

            estaEnviandoHelpdesk = true;
            try
            {
                var movimientosRedmine = gruposConsolidados.Select(g => new MovimientoPersonalItemDto
                {
                    FilaIndex = g.GrupoIndex,
                    Cliente = g.Cliente,
                    Area = g.Area,
                    Cargo = g.Puesto,
                    Fecha = fechaSeleccionada?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                    HoraInicio = g.HoraInicio,
                    HoraFin = g.HoraFin,
                    CantidadEmpleados = g.Cantidad,
                    IdsEmpleados = g.IdsEmpleadosTexto,
                    Asunto = !string.IsNullOrWhiteSpace(g.Cliente) ? $"{g.Cliente} - {g.Area} - {g.Puesto}".Trim() : $"{g.Area} - {g.Puesto}".Trim(),
                    Bonos = g.VtcoBnc > 0 ? g.VtcoBnc.ToString("N2", CultureInfo.InvariantCulture) : "0.00",
                    VtcoTrasp = g.VtcoTrasp > 0 ? g.VtcoTrasp.ToString("N2", CultureInfo.InvariantCulture) : "0",
                    VtcoAlim = g.VtcoAlim > 0 ? g.VtcoAlim.ToString("N2", CultureInfo.InvariantCulture) : "0"
                }).ToList();

                var request = new CrearMovimientosPersonalRequestDto
                {
                    OperacionId = selectedOperacionId ?? 0,
                    ClienteRedmine = !string.IsNullOrWhiteSpace(clienteRedmineIdentificador) ? clienteRedmineIdentificador : "Operaciones",
                    ModoGeneracion = "Ambos",
                    UsuarioRedmine = _userInfo.RedmineUser ?? currentUserName,
                    Movimientos = movimientosRedmine
                };

                var response = await _odooService.CrearMovimientosPersonalMasivo(request);

                if (response?.Model != null)
                {
                    var res = response.Model;
                    int creados = res.Exitosos;

                    // Estampar los IDs de Redmine en los empleados correspondientes
                    if (res.Detalles != null)
                    {
                        foreach (var det in res.Detalles)
                        {
                            var grupo = gruposConsolidados.FirstOrDefault(g => g.GrupoIndex == det.FilaIndex);
                            if (grupo != null && (det.RedmineIssueId.HasValue || det.RedmineViaticosIssueId.HasValue))
                            {
                                var idsDb = grupo.Empleados.Where(e => e.Id > 0).Select(e => e.Id).ToList();
                                if (idsDb.Any())
                                {
                                    await _odooService.UpdateSyncHelpdeskMovimientos(new ActualizarSyncHelpdeskRequest
                                    {
                                        Ids = idsDb,
                                        TicketRedmineId = det.RedmineIssueId,
                                        TicketViaticoId = det.RedmineViaticosIssueId,
                                        Estado = "SINCRONIZADO"
                                    });
                                }

                                foreach (var emp in grupo.Empleados)
                                {
                                    emp.TicketRedmineId = det.RedmineIssueId;
                                    emp.TicketViaticoId = det.RedmineViaticosIssueId;
                                    emp.Estado = "SINCRONIZADO";
                                }
                            }
                        }
                    }

                    _snackbar.Add($"🚀 {creados} tickets creados con éxito en Helpdesk Redmine.", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Error al enviar a Helpdesk: {response?.Respuesta?.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error en comunicación con Helpdesk: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaEnviandoHelpdesk = false;
            }
        }

        // --- HISTORIAL (Tab 3) ---
        protected async Task ConsultarHistorial()
        {
            if (!fechaHistorial.HasValue) return;

            cargandoHistorial = true;
            try
            {
                int opId = selectedOperacionId ?? 0;
                var resp = await _odooService.GetMovimientosPersonalDiario(fechaHistorial.Value, opId, currentUserName);
                if (resp?.Model != null)
                {
                    listaHistorial = resp.Model.ToList();
                    _snackbar.Add($"Se encontraron {listaHistorial.Count} movimientos en el historial de {currentUserName}.", Severity.Info);
                }
                else
                {
                    listaHistorial.Clear();
                    _snackbar.Add("No hay registros en esa fecha.", Severity.Info);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al consultar historial: {ex.Message}", Severity.Error);
            }
            finally
            {
                cargandoHistorial = false;
            }
        }

        protected async Task CargarJornadaHistorialParaModificar(DateTime fecha)
        {
            fechaSeleccionada = fecha;
            activeTabIndex = 0;
            await CargarJornadaActual();
            _snackbar.Add($"Jornada del {fecha:dd/MM/yyyy} cargada en la grilla para modificación.", Severity.Info);
        }

        // --- PURGA DE BORRADORES OBSOLETOS DE LOCALSTORAGE ---
        private async Task PurgarBorradoresLocales()
        {
            try
            {
                await _js.InvokeVoidAsync("eval", @"
                    try {
                        for (let i = localStorage.length - 1; i >= 0; i--) {
                            const key = localStorage.key(i);
                            if (key && (key.startsWith('supervisor_jornada_draft_') || key.includes('jornada_draft'))) {
                                localStorage.removeItem(key);
                            }
                        }
                    } catch(e) {}
                ");
            }
            catch { }
        }

        // --- CATÁLOGO DE ÁREAS POR CLIENTE ---
        public List<string> GetAreasParaCliente(string? cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente))
                return areasSugeridas.Any() ? areasSugeridas : new List<string> { "Operaciones" };

            string limpio = cliente.Trim();
            if (areasPorCliente.TryGetValue(limpio, out var exactList) && exactList.Any())
                return exactList;

            var match = areasPorCliente.FirstOrDefault(kvp =>
                kvp.Key.Contains(limpio, StringComparison.OrdinalIgnoreCase) ||
                limpio.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));

            if (match.Value != null && match.Value.Any())
                return match.Value;

            return new List<string> { "Operaciones" };
        }

        protected async Task CargarCatalogoClienteAreas()
        {
            cargandoAreas = true;
            try
            {
                if (!listaAreasAtributoOdoo.Any())
                {
                    try
                    {
                        var respAtrib = await _odooService.GetOdooAreasAtributo();
                        if (respAtrib?.Model != null && respAtrib.Model.Any())
                        {
                            listaAreasAtributoOdoo = respAtrib.Model.ToList();
                        }
                    }
                    catch (Exception exAtrib)
                    {
                        Console.WriteLine($"Error cargando areas atributo odoo: {exAtrib.Message}");
                    }
                }

                var resp = await _odooService.GetAllClienteAreas();
                if (resp?.Model != null && resp.Model.Any())
                {
                    catalogoClienteAreas = resp.Model.ToList();
                    areasPorCliente.Clear();

                    foreach (var item in catalogoClienteAreas.Where(x => x.Activo))
                    {
                        if (!string.IsNullOrWhiteSpace(item.ClienteNombre))
                        {
                            var cName = item.ClienteNombre.Trim();
                            if (!areasPorCliente.ContainsKey(cName))
                                areasPorCliente[cName] = new List<string>();

                            if (!areasPorCliente[cName].Contains(item.AreaNombre.Trim(), StringComparer.OrdinalIgnoreCase))
                                areasPorCliente[cName].Add(item.AreaNombre.Trim());
                        }
                    }

                    if (selectedOperacionId.HasValue)
                    {
                        var op = operacionesList.FirstOrDefault(o => o.id == selectedOperacionId.Value);
                        if (op != null)
                        {
                            areasSugeridas = GetAreasParaCliente(op.nombre);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando catalogo cliente areas: {ex.Message}");
            }
            finally
            {
                cargandoAreas = false;
            }
        }

        protected async Task GuardarNuevaAreaCliente()
        {
            if (!nuevoClienteAreaOpId.HasValue || nuevoClienteAreaOpId.Value <= 0)
            {
                _snackbar.Add("Seleccione el cliente para la nueva área.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(nuevaAreaNombre))
            {
                _snackbar.Add("Ingrese el nombre del área operativa.", Severity.Warning);
                return;
            }

            var op = operacionesList.FirstOrDefault(x => x.id == nuevoClienteAreaOpId.Value);
            string cName = op?.nombre ?? $"Cliente #{nuevoClienteAreaOpId.Value}";

            var req = new GuardarClienteAreaRequest
            {
                OperacionId = nuevoClienteAreaOpId.Value,
                ClienteNombre = cName,
                AreaNombre = nuevaAreaNombre.Trim(),
                Usuario = currentUserName
            };

            var res = await _odooService.GuardarClienteArea(req);
            if (res?.Respuesta?.ExisteError == false)
            {
                _snackbar.Add($"Área '{nuevaAreaNombre}' guardada exitosamente para '{cName}'.", Severity.Success);
                nuevaAreaNombre = string.Empty;
                await CargarCatalogoClienteAreas();
            }
            else
            {
                _snackbar.Add(res?.Respuesta?.MensajeError ?? "Error al guardar el área.", Severity.Error);
            }
        }

        protected async Task EliminarAreaCliente(ClienteAreaDto item)
        {
            bool? confirm = await _dialogService.ShowMessageBox(
                "Eliminar Área",
                $"¿Está seguro de eliminar el área '{item.AreaNombre}' del cliente '{item.ClienteNombre}'?",
                yesText: "Sí, Eliminar", cancelText: "Cancelar");

            if (confirm == true)
            {
                var res = await _odooService.EliminarClienteArea(item.Id);
                if (res?.Respuesta?.ExisteError == false)
                {
                    _snackbar.Add($"Área '{item.AreaNombre}' eliminada.", Severity.Info);
                    await CargarCatalogoClienteAreas();
                }
                else
                {
                    _snackbar.Add(res?.Respuesta?.MensajeError ?? "Error al eliminar área.", Severity.Error);
                }
            }
        }

        protected async Task<IEnumerable<string>> BuscarAreaAtributo(string value, CancellationToken token)
        {
            await Task.Yield();
            if (string.IsNullOrWhiteSpace(value))
            {
                return listaAreasAtributoOdoo.Select(a => a.nombre).Take(30);
            }
            return listaAreasAtributoOdoo
                .Where(a => a.nombre.Contains(value, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.nombre)
                .Take(30);
        }

        private async Task GuardarBorradorLocal() => await Task.CompletedTask;
        private async Task<bool> RecuperarBorradorLocal() => await Task.FromResult(false);
        private async Task LimpiarBorradorLocal() => await PurgarBorradoresLocales();
    }
}
