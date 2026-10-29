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
    public enum ModoVistaGrilla
    {
        Operativo, // Paso 2: Jornada & Viáticos (Horas, Turno auto, Comida, Viáticos)
        Ubicacion, // Paso 1: Ubicación & Puesto (Cliente, Área, Cargo)
        Completo,  // Todas las columnas
        Tarjetas   // Modo tarjetas touch-friendly para tablets pequeñas o móviles
    }

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
        public string TiempoComida { get; set; } = "1.00";
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
        public string TipoEmpleado { get; set; } = "Permanente"; // "Permanente" o "Prestamo"
        public bool Asistio { get; set; } = true;
        public string Estado { get; set; } = "GUARDADO";
        public int? TicketRedmineId { get; set; }
        public int? TicketViaticoId { get; set; }
        public int? TicketAusenciaId { get; set; }
        public string TipoAusencia { get; set; } = "Ausencias justificadas";
        public DateTime? FechaInicioAusencia { get; set; }
        public DateTime? FechaFinAusencia { get; set; }
        public decimal DiasAusencia { get; set; } = 1.0m;
        public string VacacionesTipo { get; set; } = "Descansadas";
        public string ObservacionesAusencia { get; set; } = string.Empty;
        public bool SincronizadoAusencia => TicketAusenciaId.HasValue && TicketAusenciaId.Value > 0;
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
        public string TiempoComida { get; set; } = "1.00";
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
        public string Observaciones { get; set; } = string.Empty;
        public string NotaPrestamos { get; set; } = string.Empty;
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
        protected ModoVistaGrilla modoVistaGrilla = ModoVistaGrilla.Operativo;

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

        // Clientes Asignados al Supervisor (desde tbl_bs_supervisor_cliente)
        protected List<SupervisorClienteDto> misClientesAsignados = new();
        protected List<string> NombresMisClientes => misClientesAsignados
            .Select(c => c.ClienteNombre?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Plantilla Personal del Supervisor (Tab 4)
        protected List<SupervisorPlantillaItemDto> miPlantillaPersonal = new();
        protected bool cargandoPlantilla = false;
        protected string textoBusquedaPlantilla = string.Empty;
        protected ModoVistaGrilla modoVistaPlantilla = ModoVistaGrilla.Operativo;
        protected ModoVistaGrilla modoVistaMovimientos = ModoVistaGrilla.Tarjetas;

        // Pestaña Ausencias (Tab 2)
        protected List<Combos> tiposAusenciaOdoo = new();
        protected ModoVistaGrilla modoVistaAusencias = ModoVistaGrilla.Tarjetas;
        protected string textoBusquedaAusencias = string.Empty;
        protected bool estaEnviandoAusencias = false;

        protected int TotalAusenciasSincronizadas => empleadosJornada.Count(x => !x.Asistio && x.TicketAusenciaId.HasValue && x.TicketAusenciaId > 0);
        protected int TotalAusenciasPendientes => empleadosJornada.Count(x => !x.Asistio && (!x.TicketAusenciaId.HasValue || x.TicketAusenciaId <= 0));
        protected int CantidadAusentesSeleccionados => empleadosJornada.Count(x => !x.Asistio && x.Seleccionado);

        protected IEnumerable<EmpleadoJornadaItem> AusentesFiltrados
        {
            get
            {
                var ausentes = empleadosJornada.Where(x => !x.Asistio);
                DateTime fRef = fechaSeleccionada ?? DateTime.Today;
                foreach (var a in ausentes)
                {
                    if (!a.FechaInicioAusencia.HasValue || a.FechaInicioAusencia.Value <= DateTime.MinValue)
                        a.FechaInicioAusencia = fRef;
                    if (!a.FechaFinAusencia.HasValue || a.FechaFinAusencia.Value <= DateTime.MinValue)
                        a.FechaFinAusencia = a.FechaInicioAusencia ?? fRef;
                }

                if (string.IsNullOrWhiteSpace(textoBusquedaAusencias))
                    return ausentes;
                var q = textoBusquedaAusencias.Trim();
                return ausentes.Where(x =>
                    (x.NombreEmpleado != null && x.NombreEmpleado.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    x.CodigoEmpleado.ToString().Contains(q) ||
                    (x.Cliente != null && x.Cliente.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (x.TipoAusencia != null && x.TipoAusencia.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Area != null && x.Area.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }
        }
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
                        (!string.IsNullOrEmpty(x.AreaNombre) && x.AreaNombre.Contains(filtroTextoArea, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(x.ClienteNombre) && x.ClienteNombre.Contains(filtroTextoArea, StringComparison.OrdinalIgnoreCase)));
                }
                return list.OrderBy(x => x.ClienteNombre)
                           .ThenBy(x => x.AreaNombre);
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

        protected int TotalConViaticos => empleadosJornada.Count(x => x.Asistio && (x.VtcoTrasp > 0 || x.VtcoAlim > 0 || x.BonoBnc > 0));
        protected int TotalSinViaticos => empleadosJornada.Count(x => x.Asistio && (x.VtcoTrasp == 0 && x.VtcoAlim == 0 && x.BonoBnc == 0));

        // Filtros para la pestaña de Viáticos
        protected string textoBusquedaViaticos = string.Empty;
        protected string filtroViaticos = "CON_VIATICOS"; // Por defecto solo mostrar quienes tienen viático o bono

        protected IEnumerable<EmpleadoJornadaItem> EmpleadosViaticosFiltrados
        {
            get
            {
                var query = empleadosJornada.Where(x => x.Asistio);

                if (filtroViaticos == "CON_VIATICOS")
                {
                    query = query.Where(x => x.VtcoTrasp > 0 || x.VtcoAlim > 0 || x.BonoBnc > 0);
                }
                else if (filtroViaticos == "SIN_VIATICOS")
                {
                    query = query.Where(x => x.VtcoTrasp == 0 && x.VtcoAlim == 0 && x.BonoBnc == 0);
                }

                if (!string.IsNullOrWhiteSpace(textoBusquedaViaticos))
                {
                    var busq = textoBusquedaViaticos.Trim();
                    query = query.Where(x =>
                        x.NombreEmpleado.Contains(busq, StringComparison.OrdinalIgnoreCase) ||
                        x.CodigoEmpleado.ToString().Contains(busq) ||
                        x.Area.Contains(busq, StringComparison.OrdinalIgnoreCase) ||
                        x.Cliente.Contains(busq, StringComparison.OrdinalIgnoreCase));
                }

                return query;
            }
        }

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
                await CargarMisClientesAsignados();
                await RecuperarOperacionPrevia();
                await CargarMiPlantilla();
                await CargarTiposAusencias();
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
                    DateTime fRef = fechaSeleccionada ?? DateTime.Today;
                    empleadosJornada = resp.Model.Select(m =>
                    {
                        var entrada = m.HoraEntrada;
                        var salida = m.HoraSalida;
                        var comida = !string.IsNullOrWhiteSpace(m.TiempoComida) ? m.TiempoComida : "1.00";
                        var turno = !string.IsNullOrWhiteSpace(m.TurnoNombre) 
                            ? m.TurnoNombre 
                            : DeterminarTurnoSegunLeyNicaragua(m.HoraEntrada, m.HoraSalida);

                        if (!m.Asistio)
                        {
                            entrada = "--:--";
                            salida = "--:--";
                            comida = "0";
                            turno = "Ausente";
                        }

                        DateTime fIni = (m.FechaInicioAusencia.HasValue && m.FechaInicioAusencia.Value > DateTime.MinValue)
                            ? m.FechaInicioAusencia.Value
                            : (m.Fecha > DateTime.MinValue ? m.Fecha : fRef);
                        DateTime fFin = (m.FechaFinAusencia.HasValue && m.FechaFinAusencia.Value > DateTime.MinValue)
                            ? m.FechaFinAusencia.Value
                            : fIni;

                        return new EmpleadoJornadaItem
                        {
                            Id = m.Id,
                            IdEmpleado = m.IdEmpleado,
                            CodigoEmpleado = m.CodigoEmpleado,
                            NombreEmpleado = m.NombreEmpleado,
                            Area = m.Area,
                            Puesto = m.Puesto,
                            TurnoNombre = turno,
                            HoraEntrada = entrada,
                            HoraSalida = salida,
                            TiempoComida = comida,
                            Cliente = !string.IsNullOrWhiteSpace(m.Cliente)
                                ? m.Cliente
                                : (operacionesList.FirstOrDefault(o => o.id == m.OperacionId)?.nombre ?? "General"),
                            Bnc = m.Bnc ?? string.Empty,
                            VtcoTrasp = m.VtcoTrasp,
                            VtcoAlim = m.VtcoAlim,
                            Observaciones = m.Observaciones ?? string.Empty,
                            TipoEmpleado = miPlantillaPersonal.FirstOrDefault(p => p.CodigoEmpleado == m.CodigoEmpleado)?.TipoEmpleado ?? "Permanente",
                            Asistio = m.Asistio,
                            Estado = m.Estado,
                            TicketRedmineId = m.TicketRedmineId,
                            TicketViaticoId = m.TicketViaticoId,
                            TicketAusenciaId = m.TicketAusenciaId,
                            TipoAusencia = !string.IsNullOrWhiteSpace(m.TipoAusencia) ? m.TipoAusencia : "Ausencias justificadas",
                            FechaInicioAusencia = fIni,
                            FechaFinAusencia = fFin,
                            DiasAusencia = m.DiasAusencia.HasValue && m.DiasAusencia.Value > 0 ? m.DiasAusencia.Value : 1.0m,
                            VacacionesTipo = !string.IsNullOrWhiteSpace(m.VacacionesTipo) ? m.VacacionesTipo : "Descansadas",
                            ObservacionesAusencia = m.Observaciones ?? string.Empty,
                            Seleccionado = false,
                            Modificado = false
                        };
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

        // --- CLIENTES ASIGNADOS DEL SUPERVISOR ---
        protected async Task CargarMisClientesAsignados()
        {
            try
            {
                // 1. Consultar por el identificador actual (email o username, ej: sinsa@bussersa.com)
                var resp = await _odooService.GetSupervisorClientes(usuario: currentUserName);
                if (resp?.Model != null && resp.Model.Any())
                {
                    misClientesAsignados = resp.Model.Where(c => c.Activo).ToList();
                    return;
                }

                // 2. Si _userInfo.UserId > 0, consultar por el ID numérico
                if (_userInfo.UserId > 0)
                {
                    var respId = await _odooService.GetSupervisorClientes(usuario: _userInfo.UserId.ToString());
                    if (respId?.Model != null && respId.Model.Any())
                    {
                        misClientesAsignados = respId.Model.Where(c => c.Activo).ToList();
                        return;
                    }
                }

                // 3. Si _userInfo.UserName es diferente a currentUserName
                if (!string.IsNullOrWhiteSpace(_userInfo.UserName) && !string.Equals(_userInfo.UserName, currentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    var respUser = await _odooService.GetSupervisorClientes(usuario: _userInfo.UserName);
                    if (respUser?.Model != null && respUser.Model.Any())
                    {
                        misClientesAsignados = respUser.Model.Where(c => c.Activo).ToList();
                        return;
                    }
                }

                // 4. Intentar sin dominio si es correo
                if (currentUserName.Contains("@"))
                {
                    string userSinDominio = currentUserName.Split('@')[0];
                    var respSinDom = await _odooService.GetSupervisorClientes(usuario: userSinDominio);
                    if (respSinDom?.Model != null && respSinDom.Model.Any())
                    {
                        misClientesAsignados = respSinDom.Model.Where(c => c.Activo).ToList();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar clientes asignados: {ex.Message}");
            }
        }

        // --- GESTIÓN DE PLANTILLA DE PERSONAL DEL SUPERVISOR ---
        protected async Task CargarMiPlantilla()
        {
            cargandoPlantilla = true;
            try
            {
                if (!misClientesAsignados.Any())
                {
                    await CargarMisClientesAsignados();
                }

                var resp = await _odooService.GetSupervisorPlantilla(currentUserName);
                if (resp?.Model == null || !resp.Model.Any())
                {
                    if (_userInfo.UserId > 0)
                    {
                        resp = await _odooService.GetSupervisorPlantilla(_userInfo.UserId.ToString());
                    }
                }
                if (resp?.Model == null || !resp.Model.Any())
                {
                    if (!string.IsNullOrWhiteSpace(_userInfo.UserName) && !string.Equals(_userInfo.UserName, currentUserName, StringComparison.OrdinalIgnoreCase))
                    {
                        resp = await _odooService.GetSupervisorPlantilla(_userInfo.UserName);
                    }
                }

                if (resp?.Model != null)
                {
                    var items = resp.Model.ToList();
                    // Filtrar estrictamente por los clientes configurados del usuario supervisor
                    if (NombresMisClientes.Any())
                    {
                        items = items.Where(x => NombresMisClientes.Contains(x.ClienteDefault, StringComparer.OrdinalIgnoreCase)).ToList();
                    }
                    miPlantillaPersonal = items;
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

        // --- CARGAR DATOS BASE (COPIAR DE LA TABLA MAESTRA DE ADMINISTRACIÓN) ---
        protected async Task CargarDatosBasePlantilla()
        {
            try
            {
                cargandoPlantilla = true;

                if (!misClientesAsignados.Any())
                {
                    await CargarMisClientesAsignados();
                }

                if (!NombresMisClientes.Any())
                {
                    _snackbar.Add("No tienes clientes asignados configurados en el sistema de administración. Consulta con el administrador para que te asigne tus clientes.", Severity.Warning);
                    return;
                }

                if (miPlantillaPersonal.Any())
                {
                    bool? confirm = await _dialogService.ShowMessageBox(
                        "Cargar Datos Base",
                        $"¿Deseas reemplazar tu plantilla actual ({miPlantillaPersonal.Count} colaboradores) con los datos base de administración de tus clientes ({string.Join(", ", NombresMisClientes)})?",
                        yesText: "Sí, Cargar Base",
                        cancelText: "Cancelar");

                    if (confirm != true) return;
                }

                var nuevosBase = new List<PersonalClienteItemDto>();
                foreach (var cl in NombresMisClientes)
                {
                    var respPersonal = await _odooService.GetPersonalCliente(cliente: cl);
                    if (respPersonal?.Model != null && respPersonal.Model.Any())
                    {
                        nuevosBase.AddRange(respPersonal.Model.Where(p => p.Activo));
                    }
                }

                if (!nuevosBase.Any())
                {
                    _snackbar.Add($"No se encontraron colaboradores en la base de administración para los clientes: {string.Join(", ", NombresMisClientes)}.", Severity.Info);
                    return;
                }

                var unicos = nuevosBase
                    .GroupBy(p => p.CodigoEmpleado)
                    .Select(g => g.First())
                    .ToList();

                miPlantillaPersonal.Clear();
                int guardados = 0;

                foreach (var b in unicos)
                {
                    var item = new SupervisorPlantillaItemDto
                    {
                        UsuarioSupervisor = currentUserName,
                        IdEmpleado = b.IdEmpleado,
                        CodigoEmpleado = b.CodigoEmpleado,
                        NombreEmpleado = b.NombreEmpleado,
                        ClienteDefault = b.ClienteNombre,
                        AreaDefault = !string.IsNullOrWhiteSpace(b.AreaDefault) ? b.AreaDefault : "Operaciones",
                        PuestoDefault = !string.IsNullOrWhiteSpace(b.PuestoDefault) ? b.PuestoDefault : "6-Operador de almacen",
                        TurnoDefault = !string.IsNullOrWhiteSpace(b.TurnoDefault) ? b.TurnoDefault : "Diurno",
                        HoraEntradaDefault = !string.IsNullOrWhiteSpace(b.HoraEntradaDefault) ? b.HoraEntradaDefault : "06:00",
                        HoraSalidaDefault = !string.IsNullOrWhiteSpace(b.HoraSalidaDefault) ? b.HoraSalidaDefault : "14:00",
                        TiempoComidaDefault = !string.IsNullOrWhiteSpace(b.TiempoComidaDefault) ? b.TiempoComidaDefault : "1.00",
                        VtcoTraspDefault = b.VtcoTraspDefault,
                        VtcoAlimDefault = b.VtcoAlimDefault,
                        BonoBncDefault = b.BonoBncDefault,
                        TipoEmpleado = "Permanente",
                        Activo = true
                    };

                    await _odooService.SaveSupervisorPlantillaItem(item);
                    miPlantillaPersonal.Add(item);
                    guardados++;
                }

                _snackbar.Add($"Se cargaron exitosamente {guardados} colaboradores base de {string.Join(", ", NombresMisClientes)} a tu plantilla.", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar datos base: {ex.Message}", Severity.Error);
            }
            finally
            {
                cargandoPlantilla = false;
                StateHasChanged();
            }
        }

        // --- ACTUALIZAR PLANTILLA (AGREGAR NUEVOS REGISTROS DE LA TABLA MAESTRA) ---
        protected async Task ActualizarPlantillaDesdeBase()
        {
            try
            {
                cargandoPlantilla = true;

                if (!misClientesAsignados.Any())
                {
                    await CargarMisClientesAsignados();
                }

                if (!NombresMisClientes.Any())
                {
                    _snackbar.Add("No tienes clientes asignados configurados en el sistema de administración.", Severity.Warning);
                    return;
                }

                var baseMaestra = new List<PersonalClienteItemDto>();
                foreach (var cl in NombresMisClientes)
                {
                    var respPersonal = await _odooService.GetPersonalCliente(cliente: cl);
                    if (respPersonal?.Model != null && respPersonal.Model.Any())
                    {
                        baseMaestra.AddRange(respPersonal.Model.Where(p => p.Activo));
                    }
                }

                var existentes = miPlantillaPersonal.Select(x => x.CodigoEmpleado).ToHashSet();
                var nuevosPorAgregar = baseMaestra
                    .Where(p => !existentes.Contains(p.CodigoEmpleado))
                    .GroupBy(p => p.CodigoEmpleado)
                    .Select(g => g.First())
                    .ToList();

                if (!nuevosPorAgregar.Any())
                {
                    _snackbar.Add($"Tu plantilla ya está al día. No hay nuevos colaboradores en la base de {string.Join(", ", NombresMisClientes)}.", Severity.Info);
                    return;
                }

                int agregados = 0;
                foreach (var b in nuevosPorAgregar)
                {
                    var item = new SupervisorPlantillaItemDto
                    {
                        UsuarioSupervisor = currentUserName,
                        IdEmpleado = b.IdEmpleado,
                        CodigoEmpleado = b.CodigoEmpleado,
                        NombreEmpleado = b.NombreEmpleado,
                        ClienteDefault = b.ClienteNombre,
                        AreaDefault = !string.IsNullOrWhiteSpace(b.AreaDefault) ? b.AreaDefault : "Operaciones",
                        PuestoDefault = !string.IsNullOrWhiteSpace(b.PuestoDefault) ? b.PuestoDefault : "6-Operador de almacen",
                        TurnoDefault = !string.IsNullOrWhiteSpace(b.TurnoDefault) ? b.TurnoDefault : "Diurno",
                        HoraEntradaDefault = !string.IsNullOrWhiteSpace(b.HoraEntradaDefault) ? b.HoraEntradaDefault : "06:00",
                        HoraSalidaDefault = !string.IsNullOrWhiteSpace(b.HoraSalidaDefault) ? b.HoraSalidaDefault : "14:00",
                        TiempoComidaDefault = !string.IsNullOrWhiteSpace(b.TiempoComidaDefault) ? b.TiempoComidaDefault : "1.00",
                        VtcoTraspDefault = b.VtcoTraspDefault,
                        VtcoAlimDefault = b.VtcoAlimDefault,
                        BonoBncDefault = b.BonoBncDefault,
                        TipoEmpleado = "Permanente",
                        Activo = true
                    };

                    await _odooService.SaveSupervisorPlantillaItem(item);
                    miPlantillaPersonal.Add(item);
                    agregados++;
                }

                _snackbar.Add($"Se actualizaron y agregaron {agregados} nuevos colaboradores desde la administración base ({string.Join(", ", nuevosPorAgregar.Take(3).Select(n => $"#{n.CodigoEmpleado} {n.NombreEmpleado}"))}{(nuevosPorAgregar.Count > 3 ? "..." : "")}).", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al actualizar plantilla: {ex.Message}", Severity.Error);
            }
            finally
            {
                cargandoPlantilla = false;
                StateHasChanged();
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
                        TiempoComida = !string.IsNullOrWhiteSpace(item.TiempoComidaDefault) ? item.TiempoComidaDefault : "1.00",
                        VtcoTrasp = item.VtcoTraspDefault,
                        VtcoAlim = item.VtcoAlimDefault,
                        BonoBnc = item.BonoBncDefault,
                        TipoEmpleado = !string.IsNullOrWhiteSpace(item.TipoEmpleado) ? item.TipoEmpleado : "Permanente",
                        Asistio = true,
                        Estado = "BORRADOR",
                        FechaInicioAusencia = fechaSeleccionada ?? DateTime.Today,
                        FechaFinAusencia = fechaSeleccionada ?? DateTime.Today,
                        DiasAusencia = 1.0m,
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
                    TiempoComidaDefault = string.IsNullOrWhiteSpace(e.TiempoComida) ? "1.00" : e.TiempoComida,
                    VtcoTraspDefault = e.VtcoTrasp,
                    VtcoAlimDefault = e.VtcoAlim,
                    BonoBncDefault = e.BonoBnc,
                    TipoEmpleado = !string.IsNullOrWhiteSpace(e.TipoEmpleado) ? e.TipoEmpleado : "Permanente",
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
                var resp = await _odooService.SaveSupervisorPlantillaItem(item);
                if (resp?.Respuesta?.ExisteError == false)
                {
                    _snackbar.Add($"✅ Guardado en Mi Plantilla: #{item.CodigoEmpleado} {item.NombreEmpleado} ({item.ClienteDefault} - {item.AreaDefault}).", Severity.Success);
                }
                else
                {
                    string msg = resp?.Respuesta?.MensajeError ?? "No se pudo actualizar en BD";
                    _snackbar.Add($"⚠️ No se pudo guardar: {msg}", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al actualizar: {ex.Message}", Severity.Warning);
            }
        }

        protected async Task AbrirDialogoEditarPlantilla(SupervisorPlantillaItemDto item)
        {
            var parameters = new DialogParameters
            {
                ["ItemOriginal"] = item,
                ["OperacionesDisponibles"] = operacionesList,
                ["AreasPorCliente"] = areasPorCliente,
                ["CargosDisponibles"] = cargosDisponibles
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarColaboradorPlantilla>("Editar Colaborador en Mi Plantilla", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is SupervisorPlantillaItemDto modificado)
            {
                item.ClienteDefault = modificado.ClienteDefault;
                item.AreaDefault = modificado.AreaDefault;
                item.PuestoDefault = modificado.PuestoDefault;
                item.TurnoDefault = modificado.TurnoDefault;
                item.HoraEntradaDefault = modificado.HoraEntradaDefault;
                item.HoraSalidaDefault = modificado.HoraSalidaDefault;
                item.TiempoComidaDefault = modificado.TiempoComidaDefault;
                item.VtcoTraspDefault = modificado.VtcoTraspDefault;
                item.VtcoAlimDefault = modificado.VtcoAlimDefault;
                item.BonoBncDefault = modificado.BonoBncDefault;
                item.TipoEmpleado = modificado.TipoEmpleado;

                await GuardarItemPlantilla(item);
                StateHasChanged();
            }
        }

        protected async Task AbrirEditorViaticoPlantilla(SupervisorPlantillaItemDto item)
        {
            var parameters = new DialogParameters
            {
                ["CodigoEmpleado"] = item.CodigoEmpleado,
                ["NombreEmpleado"] = item.NombreEmpleado,
                ["VtcoTraspInicial"] = item.VtcoTraspDefault,
                ["VtcoAlimInicial"] = item.VtcoAlimDefault,
                ["BonoBncInicial"] = item.BonoBncDefault,
                ["CantidadSeleccionados"] = 0
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarViaticos>("Viáticos & Bono de Plantilla", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEditarViaticos.ResultadoEdicionViaticos data)
            {
                item.VtcoTraspDefault = data.VtcoTrasp;
                item.VtcoAlimDefault = data.VtcoAlim;
                item.BonoBncDefault = data.BonoBnc;

                await GuardarItemPlantilla(item);
                StateHasChanged();
            }
        }

        protected async Task AplicarPresetHorarioPlantilla(SupervisorPlantillaItemDto item, string entrada, string salida, string turno)
        {
            item.TurnoDefault = turno;
            item.HoraEntradaDefault = entrada;
            item.HoraSalidaDefault = salida;
            await GuardarItemPlantilla(item);
            StateHasChanged();
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

                    var entrada = e.HoraEntrada;
                    var salida = e.HoraSalida;
                    var comida = string.IsNullOrWhiteSpace(e.TiempoComida) ? "1.00" : e.TiempoComida;
                    var turno = e.TurnoNombre;

                    if (!e.Asistio)
                    {
                        entrada = "--:--";
                        salida = "--:--";
                        comida = "0";
                        turno = "Ausente";
                    }

                    DateTime fRef = fechaSeleccionada.Value;
                    DateTime fIni = (e.FechaInicioAusencia.HasValue && e.FechaInicioAusencia.Value > DateTime.MinValue)
                        ? e.FechaInicioAusencia.Value
                        : fRef;
                    DateTime fFin = (e.FechaFinAusencia.HasValue && e.FechaFinAusencia.Value > DateTime.MinValue)
                        ? e.FechaFinAusencia.Value
                        : fIni;

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
                        HoraEntrada = entrada,
                        HoraSalida = salida,
                        TurnoNombre = turno,
                        TiempoComida = comida,
                        Bnc = e.Bnc,
                        VtcoTrasp = e.VtcoTrasp,
                        VtcoAlim = e.VtcoAlim,
                        Observaciones = e.Observaciones,
                        Asistio = e.Asistio,
                        Estado = e.Estado == "SINCRONIZADO" ? "MODIFICADO" : "GUARDADO",
                        TicketAusenciaId = e.TicketAusenciaId,
                        TipoAusencia = !string.IsNullOrWhiteSpace(e.TipoAusencia) ? e.TipoAusencia : "Ausencias justificadas",
                        FechaInicioAusencia = fIni,
                        FechaFinAusencia = fFin,
                        DiasAusencia = e.DiasAusencia > 0 ? e.DiasAusencia : 1.0m,
                        VacacionesTipo = !string.IsNullOrWhiteSpace(e.VacacionesTipo) ? e.VacacionesTipo : "Descansadas"
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
                    if (!string.IsNullOrWhiteSpace(data.TiempoComida)) emp.TiempoComida = data.TiempoComida;
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
                ["TextoBusquedaInicial"] = textoInicial,
                ["PlantillaExistente"] = miPlantillaPersonal
            };

            var dialog = await _dialogService.ShowAsync<DialogAgregarColaboradorJornada>("Agregar Colaborador a la Jornada", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is EmpleadoJornadaItem nuevo)
            {
                var existente = empleadosJornada.FirstOrDefault(e => e.CodigoEmpleado == nuevo.CodigoEmpleado);
                if (existente != null)
                {
                    _snackbar.Add($"ℹ️ Registro adicional (Turno {nuevo.TurnoNombre}) agregado para #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado}.", Severity.Info);
                }
                else
                {
                    _snackbar.Add($"✅ #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado} agregado a la jornada para '{nuevo.Cliente}'.", Severity.Success);
                }

                nuevo.Modificado = true;
                empleadosJornada.Insert(0, nuevo);
                ActualizarGruposConsolidados();

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
                        TiempoComidaDefault = nuevo.TiempoComida,
                        VtcoTraspDefault = nuevo.VtcoTrasp,
                        VtcoAlimDefault = nuevo.VtcoAlim,
                        BonoBncDefault = nuevo.BonoBnc,
                        TipoEmpleado = nuevo.TipoEmpleado,
                        Activo = true
                    };
                    await _odooService.SaveSupervisorPlantillaItem(itemP);
                    miPlantillaPersonal.Add(itemP);
                }
                else
                {
                    var enPlantilla = miPlantillaPersonal.FirstOrDefault(p => p.CodigoEmpleado == nuevo.CodigoEmpleado);
                    if (enPlantilla != null && enPlantilla.TipoEmpleado != nuevo.TipoEmpleado)
                    {
                        enPlantilla.TipoEmpleado = nuevo.TipoEmpleado;
                        await _odooService.SaveSupervisorPlantillaItem(enPlantilla);
                    }
                }

                ActualizarGruposConsolidados();
            }
        }

        // --- LIMPIAR GRILLA COMPLETA ---
        protected async Task LimpiarGrillaCompleta()
        {
            if (!empleadosJornada.Any()) return;

            bool tieneRegistrosGuardados = empleadosJornada.Any(x => x.Id > 0);
            string mensaje = tieneRegistrosGuardados
                ? $"¿Deseas limpiar la grilla y eliminar de la base de datos los {empleadosJornada.Count} registros guardados para el día {fechaSeleccionada:dd/MM/yyyy}?"
                : $"¿Deseas limpiar los {empleadosJornada.Count} colaboradores de la grilla?";

            string yesBtn = tieneRegistrosGuardados ? "Sí, Limpiar y Borrar de BD" : "Sí, Limpiar Grilla";

            bool? confirm = await _dialogService.ShowMessageBox(
                "Limpiar Grilla Diaria",
                mensaje,
                yesText: yesBtn, 
                cancelText: "Cancelar");

            if (confirm == true)
            {
                estaCargando = true;
                try
                {
                    var idsGuardados = empleadosJornada.Where(x => x.Id > 0).Select(x => x.Id).ToList();
                    if (idsGuardados.Any())
                    {
                        var tasks = idsGuardados.Select(id => _odooService.DeleteMovimientoPersonalDiario(id));
                        await Task.WhenAll(tasks);
                    }

                    empleadosJornada.Clear();
                    await LimpiarBorradorLocal();
                    ActualizarGruposConsolidados();
                    _snackbar.Add("Grilla limpiada exitosamente.", Severity.Info);
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error al limpiar grilla: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                }
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
                    "Área Operativa", "Cargo / Puesto", "Turno", "Hora Entrada", "Hora Salida", "Tiempo Comida (Hrs)",
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
                    row.CreateCell(11).SetCellValue(emp.TiempoComida);

                    var cTrasp = row.CreateCell(12);
                    cTrasp.SetCellValue((double)emp.VtcoTrasp);
                    cTrasp.CellStyle = currencyStyle;

                    var cAlim = row.CreateCell(13);
                    cAlim.SetCellValue((double)emp.VtcoAlim);
                    cAlim.CellStyle = currencyStyle;

                    var cBnc = row.CreateCell(14);
                    cBnc.SetCellValue((double)emp.BonoBnc);
                    cBnc.CellStyle = currencyStyle;

                    var cTotal = row.CreateCell(15);
                    cTotal.SetCellValue((double)(emp.VtcoTrasp + emp.VtcoAlim + emp.BonoBnc));
                    cTotal.CellStyle = currencyStyle;

                    row.CreateCell(16).SetCellValue(emp.Observaciones);
                    row.CreateCell(17).SetCellValue(emp.Estado);
                    row.CreateCell(18).SetCellValue(emp.TicketRedmineId.HasValue ? emp.TicketRedmineId.Value.ToString() : "");
                    row.CreateCell(19).SetCellValue(emp.TicketViaticoId.HasValue ? emp.TicketViaticoId.Value.ToString() : "");
                    row.CreateCell(20).SetCellValue(currentUserName);

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

                var sumTrasp = totalRow.CreateCell(12);
                sumTrasp.SetCellValue((double)TotalViaticosTrasp);
                sumTrasp.CellStyle = currencyStyle;

                var sumAlim = totalRow.CreateCell(13);
                sumAlim.SetCellValue((double)TotalViaticosAlim);
                sumAlim.CellStyle = currencyStyle;

                var sumBnc = totalRow.CreateCell(14);
                sumBnc.SetCellValue((double)TotalViaticosBnc);
                sumBnc.CellStyle = currencyStyle;

                var sumGlobal = totalRow.CreateCell(15);
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
                    14, // 11: Tiempo Comida (Hrs)
                    22, // 12: Viático Transporte (C$)
                    22, // 13: Viático Alimentación (C$)
                    18, // 14: Bono BNC (C$)
                    20, // 15: Total Viáticos (C$)
                    32, // 16: Observaciones
                    14, // 17: Estado
                    16, // 18: Ticket Redmine
                    16, // 19: Ticket Viático
                    26  // 20: Supervisor
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

        protected void ToggleAsistencia(EmpleadoJornadaItem emp)
        {
            AplicarEstadoAsistencia(emp, !emp.Asistio);
        }

        protected void AplicarEstadoAsistencia(EmpleadoJornadaItem emp, bool asistio)
        {
            emp.Asistio = asistio;
            emp.Modificado = true;

            if (!asistio)
            {
                emp.HoraEntrada = "--:--";
                emp.HoraSalida = "--:--";
                emp.TiempoComida = "0";
                emp.TurnoNombre = "Ausente";
                DateTime fRef = fechaSeleccionada ?? DateTime.Today;
                emp.FechaInicioAusencia = fRef;
                emp.FechaFinAusencia = fRef;
                emp.DiasAusencia = 1.0m;
            }
            else
            {
                var plant = miPlantillaPersonal.FirstOrDefault(p => p.CodigoEmpleado == emp.CodigoEmpleado);
                emp.HoraEntrada = !string.IsNullOrWhiteSpace(plant?.HoraEntradaDefault) ? plant.HoraEntradaDefault : "06:00";
                emp.HoraSalida = !string.IsNullOrWhiteSpace(plant?.HoraSalidaDefault) ? plant.HoraSalidaDefault : "14:00";
                emp.TiempoComida = !string.IsNullOrWhiteSpace(plant?.TiempoComidaDefault) ? plant.TiempoComidaDefault : "1.00";
                emp.TurnoNombre = DeterminarTurnoSegunLeyNicaragua(emp.HoraEntrada, emp.HoraSalida);
            }

            ActualizarGruposConsolidados();
        }

        protected void MarcarAsistenciaSeleccionados(bool asistio)
        {
            var sel = empleadosJornada.Where(x => x.Seleccionado).ToList();
            if (!sel.Any()) return;

            foreach (var e in sel)
            {
                AplicarEstadoAsistencia(e, asistio);
                e.Seleccionado = false;
            }

            _snackbar.Add($"Marcados {sel.Count} colaboradores como {(asistio ? "Presentes" : "Ausentes")}.", Severity.Info);
            ActualizarGruposConsolidados();
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
                return "Diurno";
            }

            if (entrada == salida)
            {
                return "Diurno";
            }

            int startMinutes = (int)entrada.TotalMinutes;
            int endMinutes = (int)salida.TotalMinutes;

            if (endMinutes <= startMinutes)
            {
                endMinutes += 1440; // Cruza la medianoche
            }

            int totalMinutes = endMinutes - startMinutes;
            if (totalMinutes <= 0 || totalMinutes > 1440 * 2) return "Diurno";

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

        private static string NormalizarFormatoHora(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "00:00";
            input = input.Trim();
            if (TimeSpan.TryParse(input, out var ts))
            {
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
            }
            if (DateTime.TryParse(input, out var dt))
            {
                return dt.ToString("HH:mm");
            }
            return input;
        }

        protected void OnHorarioEntradaCambiado(EmpleadoJornadaItem item, string? nuevaEntrada)
        {
            if (!item.Asistio) return;
            if (string.IsNullOrWhiteSpace(nuevaEntrada)) nuevaEntrada = "06:00";
            item.HoraEntrada = NormalizarFormatoHora(nuevaEntrada);
            item.TurnoNombre = DeterminarTurnoSegunLeyNicaragua(item.HoraEntrada, item.HoraSalida);
            item.Modificado = true;
            ActualizarGruposConsolidados();
        }

        protected void OnHorarioSalidaCambiado(EmpleadoJornadaItem item, string? nuevaSalida)
        {
            if (!item.Asistio) return;
            if (string.IsNullOrWhiteSpace(nuevaSalida)) nuevaSalida = "14:00";
            item.HoraSalida = NormalizarFormatoHora(nuevaSalida);
            item.TurnoNombre = DeterminarTurnoSegunLeyNicaragua(item.HoraEntrada, item.HoraSalida);
            item.Modificado = true;
            ActualizarGruposConsolidados();
        }

        protected void AplicarPresetHorario(EmpleadoJornadaItem item, string entrada, string salida)
        {
            item.HoraEntrada = entrada;
            item.HoraSalida = salida;
            item.TurnoNombre = DeterminarTurnoSegunLeyNicaragua(entrada, salida);
            item.Modificado = true;
            ActualizarGruposConsolidados();
        }

        protected string NormalizarDecimalComida(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return "0.00";
            valor = valor.Replace(',', '.').Trim();
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
            {
                return d.ToString("0.00", CultureInfo.InvariantCulture);
            }
            return valor;
        }

        protected void OnTiempoComidaCambiado(EmpleadoJornadaItem item, string? nuevoTiempo)
        {
            if (!item.Asistio) return;
            item.TiempoComida = NormalizarDecimalComida(nuevoTiempo);
            item.Modificado = true;
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

        protected Color GetColorTurno(string turno)
        {
            return turno switch
            {
                "Diurno" => Color.Primary,
                "DCC" => Color.Secondary,
                "Mixto" => Color.Warning,
                "Nocturno" => Color.Dark,
                _ => Color.Default
            };
        }

        protected async Task AbrirEditorHorario(EmpleadoJornadaItem item)
        {
            var seleccionados = empleadosJornada.Where(x => x.Seleccionado).ToList();
            var parameters = new DialogParameters
            {
                ["CodigoEmpleado"] = item.CodigoEmpleado,
                ["NombreEmpleado"] = item.NombreEmpleado,
                ["TurnoInicial"] = item.TurnoNombre,
                ["EntradaInicial"] = item.HoraEntrada,
                ["SalidaInicial"] = item.HoraSalida,
                ["CantidadSeleccionados"] = seleccionados.Count
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarHorario>("Actualizar Horario", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEditarHorario.ResultadoEdicionHorario data)
            {
                item.TurnoNombre = data.TurnoNombre;
                item.HoraEntrada = data.HoraEntrada;
                item.HoraSalida = data.HoraSalida;
                item.Modificado = true;

                if (data.AplicarASeleccionados && seleccionados.Any())
                {
                    foreach (var s in seleccionados)
                    {
                        s.TurnoNombre = data.TurnoNombre;
                        s.HoraEntrada = data.HoraEntrada;
                        s.HoraSalida = data.HoraSalida;
                        s.Modificado = true;
                    }
                }

                ActualizarGruposConsolidados();
                _snackbar.Add($"Horario actualizado ({data.TurnoNombre} {data.HoraEntrada} - {data.HoraSalida})", Severity.Success);
            }
        }

        protected async Task AbrirEditorViaticoIndividual(EmpleadoJornadaItem item)
        {
            var seleccionados = empleadosJornada.Where(x => x.Seleccionado).ToList();
            var parameters = new DialogParameters
            {
                ["CodigoEmpleado"] = item.CodigoEmpleado,
                ["NombreEmpleado"] = item.NombreEmpleado,
                ["VtcoTraspInicial"] = item.VtcoTrasp,
                ["VtcoAlimInicial"] = item.VtcoAlim,
                ["BonoBncInicial"] = item.BonoBnc,
                ["CantidadSeleccionados"] = seleccionados.Count
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarViaticos>("Viáticos & Bono", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEditarViaticos.ResultadoEdicionViaticos data)
            {
                item.VtcoTrasp = data.VtcoTrasp;
                item.VtcoAlim = data.VtcoAlim;
                item.BonoBnc = data.BonoBnc;
                item.Modificado = true;

                if (data.AplicarASeleccionados && seleccionados.Any())
                {
                    foreach (var s in seleccionados)
                    {
                        s.VtcoTrasp = data.VtcoTrasp;
                        s.VtcoAlim = data.VtcoAlim;
                        s.BonoBnc = data.BonoBnc;
                        s.Modificado = true;
                    }
                }

                ActualizarGruposConsolidados();
                var total = data.VtcoTrasp + data.VtcoAlim + data.BonoBnc;
                _snackbar.Add($"Viáticos actualizados (Total C$ {total:N2})", Severity.Success);
            }
        }

        protected async Task AsignarViaticosMasivosASeleccionados()
        {
            var seleccionados = empleadosJornada.Where(x => x.Seleccionado).ToList();
            if (!seleccionados.Any())
            {
                _snackbar.Add("Selecciona al menos un colaborador usando las casillas de verificación.", Severity.Warning);
                return;
            }

            var primer = seleccionados.First();
            var parameters = new DialogParameters
            {
                ["CodigoEmpleado"] = primer.CodigoEmpleado,
                ["NombreEmpleado"] = primer.NombreEmpleado,
                ["VtcoTraspInicial"] = primer.VtcoTrasp,
                ["VtcoAlimInicial"] = primer.VtcoAlim,
                ["BonoBncInicial"] = primer.BonoBnc,
                ["CantidadSeleccionados"] = seleccionados.Count
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarViaticos>("Asignación Masiva de Viáticos", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEditarViaticos.ResultadoEdicionViaticos data)
            {
                foreach (var s in seleccionados)
                {
                    s.VtcoTrasp = data.VtcoTrasp;
                    s.VtcoAlim = data.VtcoAlim;
                    s.BonoBnc = data.BonoBnc;
                    s.Modificado = true;
                }

                ActualizarGruposConsolidados();
                var total = (data.VtcoTrasp + data.VtcoAlim + data.BonoBnc) * seleccionados.Count;
                _snackbar.Add($"Viáticos asignados a {seleccionados.Count} colaboradores (Total C$ {total:N2})", Severity.Success);
            }
        }

        protected async Task LimpiarTodosLosViaticos()
        {
            bool? confirm = await _dialogService.ShowMessageBox(
                "Limpiar Viáticos",
                "¿Deseas restablecer a C$ 0 todos los viáticos y bonos de la jornada?",
                yesText: "Sí, Limpiar a C$ 0", cancelText: "Cancelar");

            if (confirm == true)
            {
                foreach (var emp in empleadosJornada)
                {
                    if (emp.VtcoTrasp > 0 || emp.VtcoAlim > 0 || emp.BonoBnc > 0)
                    {
                        emp.VtcoTrasp = 0;
                        emp.VtcoAlim = 0;
                        emp.BonoBnc = 0;
                        emp.Modificado = true;
                    }
                }
                ActualizarGruposConsolidados();
                _snackbar.Add("Se han restablecido todos los viáticos a C$ 0.", Severity.Info);
            }
        }

        protected void MarcarTodosViaticos(bool marcar)
        {
            foreach (var e in EmpleadosViaticosFiltrados)
            {
                e.Seleccionado = marcar;
            }
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

            // Preservar observaciones previas del supervisor si ya existían para no sobreescribir lo que digitó
            var dicObsPrevias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prev in gruposConsolidados)
            {
                string key = $"{prev.Cliente}|{prev.Area}|{prev.Puesto}|{prev.HoraInicio}|{prev.HoraFin}";
                if (!dicObsPrevias.ContainsKey(key))
                {
                    dicObsPrevias[key] = prev.Observaciones;
                }
            }

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
                    TiempoComida = string.IsNullOrWhiteSpace(x.TiempoComida) ? "1.00" : x.TiempoComida,
                    x.VtcoTrasp,
                    x.VtcoAlim,
                    x.BonoBnc
                })
                .Select((g, idx) =>
                {
                    var empleadosGrupo = g.ToList();

                    // Detectar colaboradores en condición de préstamo o cuyo cliente sea diferente al de la asistencia
                    var listaPrestamos = new List<string>();
                    foreach (var emp in empleadosGrupo)
                    {
                        var enPlantilla = miPlantillaPersonal.FirstOrDefault(p => p.CodigoEmpleado == emp.CodigoEmpleado);
                        string clienteNomina = !string.IsNullOrWhiteSpace(enPlantilla?.ClienteDefault) ? enPlantilla.ClienteDefault : string.Empty;

                        bool esPrestamo = string.Equals(emp.TipoEmpleado, "Prestamo", StringComparison.OrdinalIgnoreCase)
                            || (!string.IsNullOrWhiteSpace(clienteNomina) && !string.Equals(clienteNomina, g.Key.Cliente, StringComparison.OrdinalIgnoreCase));

                        if (esPrestamo)
                        {
                            string nomDestino = !string.IsNullOrWhiteSpace(clienteNomina) && !string.Equals(clienteNomina, g.Key.Cliente, StringComparison.OrdinalIgnoreCase)
                                ? clienteNomina
                                : (!string.IsNullOrWhiteSpace(clienteNomina) ? clienteNomina : "su cliente de origen");

                            listaPrestamos.Add($"ID {emp.CodigoEmpleado} pertenece a la nómina de {nomDestino}");
                        }
                    }

                    string notaAutomatica = string.Empty;
                    if (listaPrestamos.Any())
                    {
                        string clausulas = string.Join(", ", listaPrestamos);
                        notaAutomatica = $"{clausulas} para que sus horas nómina se graben en la nómina correspondiente.";
                    }

                    string keyGrupo = $"{g.Key.Cliente}|{g.Key.Area}|{g.Key.Puesto}|{g.Key.HoraInicio}|{g.Key.HoraFin}";
                    string obsFinal = notaAutomatica;

                    if (dicObsPrevias.TryGetValue(keyGrupo, out var obsPrevia) && !string.IsNullOrWhiteSpace(obsPrevia))
                    {
                        if (!string.IsNullOrWhiteSpace(notaAutomatica) && !obsPrevia.Contains("pertenece a la nómina de", StringComparison.OrdinalIgnoreCase))
                        {
                            obsFinal = $"{notaAutomatica} {obsPrevia}".Trim();
                        }
                        else
                        {
                            obsFinal = obsPrevia;
                        }
                    }

                    return new ConsolidadoHelpdeskItem
                    {
                        GrupoIndex = idx + 1,
                        Cliente = g.Key.Cliente,
                        Area = g.Key.Area,
                        Puesto = g.Key.Puesto,
                        TurnoNombre = g.Key.TurnoNombre,
                        HoraInicio = g.Key.HoraInicio,
                        HoraFin = g.Key.HoraFin,
                        TiempoComida = g.Key.TiempoComida,
                        VtcoTrasp = g.Key.VtcoTrasp,
                        VtcoAlim = g.Key.VtcoAlim,
                        VtcoBnc = g.Key.BonoBnc,
                        Empleados = empleadosGrupo,
                        Observaciones = obsFinal,
                        NotaPrestamos = notaAutomatica
                    };
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
                    TiempoComida = g.TiempoComida,
                    CantidadEmpleados = g.Cantidad,
                    IdsEmpleados = g.IdsEmpleadosTexto,
                    Asunto = !string.IsNullOrWhiteSpace(g.Cliente) ? $"{g.Cliente} - {g.Area} - {g.Puesto}".Trim() : $"{g.Area} - {g.Puesto}".Trim(),
                    Observaciones = g.Observaciones,
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
                        var cName = item.ClienteNombre?.Trim();
                        var aName = item.AreaNombre?.Trim();
                        if (!string.IsNullOrWhiteSpace(cName) && !string.IsNullOrWhiteSpace(aName))
                        {
                            if (!areasPorCliente.ContainsKey(cName))
                                areasPorCliente[cName] = new List<string>();

                            if (!areasPorCliente[cName].Contains(aName, StringComparer.OrdinalIgnoreCase))
                                areasPorCliente[cName].Add(aName);
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
                StateHasChanged();
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

        protected async Task CargarTiposAusencias()
        {
            try
            {
                var resp = await _odooService.GetTiposAusencias();
                if (resp?.Model != null && resp.Model.Any())
                {
                    tiposAusenciaOdoo = resp.Model.ToList();
                }
            }
            catch
            {
                // Fallback local
            }

            if (!tiposAusenciaOdoo.Any())
            {
                tiposAusenciaOdoo = new List<Combos>
                {
                    new() { id = 9, nombre = "Ausencias justificadas" },
                    new() { id = 8, nombre = "Falta injustificada" },
                    new() { id = 6, nombre = "Vacaciones" },
                    new() { id = 7, nombre = "Subsidio" },
                    new() { id = 10, nombre = "Cuarentena" },
                    new() { id = 11, nombre = "Suspensión laboral" }
                };
            }
        }

        protected void MarcarTodosAusentes(bool marcar)
        {
            foreach (var a in AusentesFiltrados)
            {
                a.Seleccionado = marcar;
            }
        }

        protected void OnFechaInicioAusenciaChanged(EmpleadoJornadaItem emp, DateTime? fecha)
        {
            DateTime fVal = fecha ?? fechaSeleccionada ?? DateTime.Today;
            emp.FechaInicioAusencia = fVal;
            if (!emp.FechaFinAusencia.HasValue || emp.FechaFinAusencia.Value < fVal)
            {
                emp.FechaFinAusencia = fVal;
            }
            CalcularDiasAusencia(emp);
            emp.Modificado = true;
        }

        protected void OnFechaFinAusenciaChanged(EmpleadoJornadaItem emp, DateTime? fecha)
        {
            DateTime fVal = fecha ?? emp.FechaInicioAusencia ?? fechaSeleccionada ?? DateTime.Today;
            emp.FechaFinAusencia = fVal;
            if (emp.FechaInicioAusencia.HasValue && fVal < emp.FechaInicioAusencia.Value)
            {
                emp.FechaInicioAusencia = fVal;
            }
            CalcularDiasAusencia(emp);
            emp.Modificado = true;
        }

        protected void CalcularDiasAusencia(EmpleadoJornadaItem emp)
        {
            if (emp.FechaInicioAusencia.HasValue && emp.FechaFinAusencia.HasValue)
            {
                int diff = (emp.FechaFinAusencia.Value.Date - emp.FechaInicioAusencia.Value.Date).Days + 1;
                emp.DiasAusencia = Math.Max(1, diff);
            }
            else
            {
                emp.DiasAusencia = 1.0m;
            }
        }

        protected void MarcarComoPresente(EmpleadoJornadaItem emp)
        {
            AplicarEstadoAsistencia(emp, true);
            _snackbar.Add($"Colaborador {emp.NombreEmpleado} marcado como PRESENTE.", Severity.Info);
        }

        // --- ENVIAR AUSENCIAS A HELPDESK (REDMINE TRACKER 8) ---
        protected async Task EnviarAusenciasAHelpdeskMasivo(bool soloSeleccionados = false)
        {
            var ausentes = soloSeleccionados 
                ? empleadosJornada.Where(x => !x.Asistio && x.Seleccionado).ToList()
                : empleadosJornada.Where(x => !x.Asistio).ToList();

            if (!ausentes.Any())
            {
                _snackbar.Add("No hay colaboradores ausentes para enviar a Helpdesk.", Severity.Warning);
                return;
            }

            if (HayCambiosSinGuardar)
            {
                await GuardarEnBaseDatos();
            }

            int total = ausentes.Count;
            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Solicitud de Ausencias",
                $"¿Estás seguro de generar la solicitud de ausencia en Helpdesk para {total} colaborador(es) ausente(s)?",
                yesText: "Sí, Solicitar en Helpdesk", cancelText: "Cancelar");

            if (confirm != true) return;

            estaEnviandoAusencias = true;
            try
            {
                var dtos = ausentes.Select((a, idx) => new AusenciaPersonalItemDto
                {
                    FilaIndex = idx + 1,
                    MovimientoId = a.Id,
                    CodigoEmpleado = a.CodigoEmpleado,
                    NombreEmpleado = a.NombreEmpleado,
                    Cliente = !string.IsNullOrWhiteSpace(a.Cliente) ? a.Cliente : (clienteRedmineIdentificador ?? string.Empty),
                    Area = a.Area,
                    Puesto = a.Puesto,
                    TipoAusencia = !string.IsNullOrWhiteSpace(a.TipoAusencia) ? a.TipoAusencia : "Ausencias justificadas",
                    FechaInicio = (a.FechaInicioAusencia ?? fechaSeleccionada ?? DateTime.Today).ToString("yyyy-MM-dd"),
                    FechaFin = (a.FechaFinAusencia ?? a.FechaInicioAusencia ?? fechaSeleccionada ?? DateTime.Today).ToString("yyyy-MM-dd"),
                    DiasSolicitados = a.DiasAusencia > 0 ? a.DiasAusencia : 1.0m,
                    VacacionesTipo = !string.IsNullOrWhiteSpace(a.VacacionesTipo) ? a.VacacionesTipo : "Descansadas",
                    Observaciones = !string.IsNullOrWhiteSpace(a.ObservacionesAusencia) ? a.ObservacionesAusencia : a.Observaciones
                }).ToList();

                var request = new CrearAusenciasPersonalRequestDto
                {
                    OperacionId = selectedOperacionId ?? 0,
                    UsuarioRedmine = _userInfo.RedmineUser ?? currentUserName,
                    AutorizadoPor = "9",
                    Ausencias = dtos
                };

                var response = await _odooService.CrearAusenciasPersonalMasivo(request);
                if (response?.Model != null)
                {
                    var res = response.Model;
                    if (res.Detalles != null)
                    {
                        foreach (var det in res.Detalles)
                        {
                            var emp = ausentes.FirstOrDefault(x => x.CodigoEmpleado == det.CodigoEmpleado);
                            if (emp != null && det.RedmineIssueId.HasValue)
                            {
                                emp.TicketAusenciaId = det.RedmineIssueId;
                                emp.Modificado = true;
                                if (emp.Id > 0)
                                {
                                    await _odooService.UpdateSyncHelpdeskMovimientos(new ActualizarSyncHelpdeskRequest
                                    {
                                        Ids = new List<int> { emp.Id },
                                        TicketAusenciaId = det.RedmineIssueId,
                                        Estado = "SINCRONIZADO"
                                    });
                                }
                            }
                        }
                    }

                    if (res.Exitosos > 0)
                    {
                        _snackbar.Add($"✅ Se generaron {res.Exitosos} solicitud(es) de ausencia en Helpdesk con éxito.", Severity.Success);
                    }
                    if (res.Fallidos > 0)
                    {
                        _snackbar.Add($"⚠️ Hubo {res.Fallidos} solicitud(es) que no se pudieron procesar.", Severity.Warning);
                    }
                }
                else
                {
                    _snackbar.Add("Error al comunicarse con el servicio de Helpdesk.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al enviar ausencias a Helpdesk: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaEnviandoAusencias = false;
                StateHasChanged();
            }
        }

        protected async Task EnviarAusenciaIndividual(EmpleadoJornadaItem emp)
        {
            if (emp == null) return;
            if (HayCambiosSinGuardar)
            {
                await GuardarEnBaseDatos();
            }

            estaEnviandoAusencias = true;
            try
            {
                var dto = new AusenciaPersonalItemDto
                {
                    FilaIndex = 1,
                    MovimientoId = emp.Id,
                    CodigoEmpleado = emp.CodigoEmpleado,
                    NombreEmpleado = emp.NombreEmpleado,
                    Cliente = !string.IsNullOrWhiteSpace(emp.Cliente) ? emp.Cliente : (clienteRedmineIdentificador ?? string.Empty),
                    Area = emp.Area,
                    Puesto = emp.Puesto,
                    TipoAusencia = !string.IsNullOrWhiteSpace(emp.TipoAusencia) ? emp.TipoAusencia : "Ausencias justificadas",
                    FechaInicio = (emp.FechaInicioAusencia ?? fechaSeleccionada ?? DateTime.Today).ToString("yyyy-MM-dd"),
                    FechaFin = (emp.FechaFinAusencia ?? emp.FechaInicioAusencia ?? fechaSeleccionada ?? DateTime.Today).ToString("yyyy-MM-dd"),
                    DiasSolicitados = emp.DiasAusencia > 0 ? emp.DiasAusencia : 1.0m,
                    VacacionesTipo = !string.IsNullOrWhiteSpace(emp.VacacionesTipo) ? emp.VacacionesTipo : "Descansadas",
                    Observaciones = !string.IsNullOrWhiteSpace(emp.ObservacionesAusencia) ? emp.ObservacionesAusencia : emp.Observaciones
                };

                var request = new CrearAusenciasPersonalRequestDto
                {
                    OperacionId = selectedOperacionId ?? 0,
                    UsuarioRedmine = _userInfo.RedmineUser ?? currentUserName,
                    AutorizadoPor = "9",
                    Ausencias = new List<AusenciaPersonalItemDto> { dto }
                };

                var response = await _odooService.CrearAusenciasPersonalMasivo(request);
                var det = response?.Model?.Detalles?.FirstOrDefault();
                if (det != null && det.Exitoso && det.RedmineIssueId.HasValue)
                {
                    emp.TicketAusenciaId = det.RedmineIssueId;
                    emp.Modificado = true;
                    if (emp.Id > 0)
                    {
                        await _odooService.UpdateSyncHelpdeskMovimientos(new ActualizarSyncHelpdeskRequest
                        {
                            Ids = new List<int> { emp.Id },
                            TicketAusenciaId = det.RedmineIssueId,
                            Estado = "SINCRONIZADO"
                        });
                    }
                    _snackbar.Add($"✅ Solicitud de ausencia #{det.RedmineIssueId} generada exitosamente.", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Error al generar ausencia: {det?.Mensaje ?? "Respuesta no exitosa"}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaEnviandoAusencias = false;
                StateHasChanged();
            }
        }

}
    }
