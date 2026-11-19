using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HostService.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
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
        public bool TraspAplicaNomina { get; set; }
        public bool AlimAplicaNomina { get; set; }
        public decimal TotalViaticos => VtcoTrasp + VtcoAlim;
        public decimal TotalViaticosNomina => (TraspAplicaNomina ? VtcoTrasp : 0) + (AlimAplicaNomina ? VtcoAlim : 0);
        public decimal TotalViaticosEfectivo => (TraspAplicaNomina ? 0 : VtcoTrasp) + (AlimAplicaNomina ? 0 : VtcoAlim);
        public decimal BonoExtra => Math.Max(0, BonoBnc - TotalViaticosNomina);
        public decimal TotalBeneficios => TotalViaticos + BonoExtra;
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
        public string ClienteOrigenNomina { get; set; } = string.Empty;
        public bool Asistio { get; set; } = true;
        public string Estado { get; set; } = "GUARDADO";
        public int? TicketRedmineId { get; set; }
        public int? TicketViaticoId { get; set; }
        public int? TicketBonoId { get; set; }
        public int? TicketAusenciaId { get; set; }
        public bool SincronizadoBono => TicketBonoId.HasValue && TicketBonoId.Value > 0;
        public bool SincronizadoViatico => TicketViaticoId.HasValue && TicketViaticoId.Value > 0;
        public string TipoAusencia { get; set; } = "Ausencias justificadas";
        public DateTime? FechaInicioAusencia { get; set; }
        public DateTime? FechaFinAusencia { get; set; }
        public decimal DiasAusencia { get; set; } = 1.0m;
        public string VacacionesTipo { get; set; } = "Descansadas";
        public string ObservacionesAusencia { get; set; } = string.Empty;
        public bool SincronizadoAusencia => TicketAusenciaId.HasValue && TicketAusenciaId.Value > 0;
        public bool Seleccionado { get; set; }
        public bool Modificado { get; set; }
        public bool GuardadoEnBd => Id > 0 && !Modificado;
        public bool TieneConflictoHorario { get; set; }
        public string DetalleConflictoHorario { get; set; } = string.Empty;
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
        public bool TraspAplicaNomina { get; set; }
        public bool AlimAplicaNomina { get; set; }
        public decimal TotalNominaPorPersona => (TraspAplicaNomina ? VtcoTrasp : 0) + (AlimAplicaNomina ? VtcoAlim : 0);
        public decimal TotalEfectivoPorPersona => (TraspAplicaNomina ? 0 : VtcoTrasp) + (AlimAplicaNomina ? 0 : VtcoAlim);
        public List<EmpleadoJornadaItem> Empleados { get; set; } = new();
        public int Cantidad => Empleados.Count(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.HoraEntrada != "--:--");
        public string IdsEmpleadosTexto => string.Join(", ", Empleados.Where(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.HoraEntrada != "--:--").Select(e => e.CodigoEmpleado));
        public string NombresResumen => string.Join(", ", Empleados.Where(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.HoraEntrada != "--:--").Take(3).Select(e => e.NombreEmpleado)) + (Cantidad > 3 ? $" y {Cantidad - 3} más..." : "");
        public int? TicketRedmineId => Empleados.FirstOrDefault(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.TicketRedmineId.HasValue && e.TicketRedmineId > 0)?.TicketRedmineId;
        public int? TicketViaticoId => Empleados.FirstOrDefault(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.TicketViaticoId.HasValue && e.TicketViaticoId > 0)?.TicketViaticoId;
        public int? TicketBonoId => Empleados.FirstOrDefault(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.TicketBonoId.HasValue && e.TicketBonoId > 0)?.TicketBonoId;
        public bool Sincronizado => TicketRedmineId.HasValue && TicketRedmineId.Value > 0;
        public string Observaciones { get; set; } = string.Empty;
        public string NotaPrestamos { get; set; } = string.Empty;
    }

    public class GrupoViaticoItem
    {
        public int Index { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string HoraInicio { get; set; } = string.Empty;
        public string HoraFin { get; set; } = string.Empty;
        public string TurnoNombre { get; set; } = string.Empty;
        public int Recursos => Empleados.Count(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.HoraEntrada != "--:--"); // Número de personas del viático (asistentes)
        public decimal VtcoTrasp { get; set; }
        public decimal VtcoAlim { get; set; }
        public bool TraspAplicaNomina { get; set; }
        public bool AlimAplicaNomina { get; set; }
        public decimal TotalPorPersona => VtcoTrasp + VtcoAlim;
        public decimal TotalGrupo => Recursos * TotalPorPersona;
        public decimal TotalNominaPorPersona => (TraspAplicaNomina ? VtcoTrasp : 0) + (AlimAplicaNomina ? VtcoAlim : 0);
        public decimal TotalEfectivoPorPersona => (TraspAplicaNomina ? 0 : VtcoTrasp) + (AlimAplicaNomina ? 0 : VtcoAlim);
        public decimal TotalNominaGrupo => Recursos * TotalNominaPorPersona;
        public decimal TotalEfectivoGrupo => Recursos * TotalEfectivoPorPersona;
        public List<EmpleadoJornadaItem> Empleados { get; set; } = new();
        public string IdsEmpleadosTexto => string.Join(", ", Empleados.Where(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.HoraEntrada != "--:--").Select(e => e.CodigoEmpleado));
        public int? TicketViaticoId => Empleados.FirstOrDefault(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && e.TicketViaticoId.HasValue && e.TicketViaticoId > 0)?.TicketViaticoId;
        public bool Sincronizado => TicketViaticoId.HasValue && TicketViaticoId.Value > 0;
    }

    public partial class RegistroMovimientosSupervisor : ComponentBase, IDisposable
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
        protected bool estaLimpiando = false;
        protected bool estaEliminando = false;
        protected string textoOperacionBd = "Procesando operación en base de datos... Por favor, espere.";
        protected bool estaOperandoEnBd => estaGuardando || estaCargando || cargandoPlantilla || estaEnviandoViaticos || estaEnviandoBonos || estaEnviandoHelpdesk || estaEnviandoAusencias || estaLimpiando || estaEliminando;
        protected int activeTabIndex = 0;
        protected void SetActiveTab(int index)
        {
            if (index == 0)
            {
                mostrarTodosClientesHelpdesk = false;
            }
            activeTabIndex = index;
            StateHasChanged();
        }
        protected ModoVistaGrilla modoVistaGrilla = ModoVistaGrilla.Operativo;

        protected string currentUserName = string.Empty;
        protected string UsuarioRedmineEfectivo
        {
            get
            {
                var u = !string.IsNullOrWhiteSpace(_userInfo?.RedmineUser) 
                    ? _userInfo.RedmineUser 
                    : (!string.IsNullOrWhiteSpace(_userInfo?.UserName) ? _userInfo.UserName : currentUserName);
                if (!string.IsNullOrWhiteSpace(u) && u.Contains('@'))
                    u = u.Split('@')[0];
                return u?.Trim() ?? string.Empty;
            }
        }
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
        // Filtros rápidos de Cliente y Área para la grilla
        protected string? filtroClienteGrilla { get; set; } = null;
        protected string? filtroAreaGrilla { get; set; } = null;

        protected IEnumerable<string> ClientesDisponiblesGrilla
        {
            get
            {
                // Solo mostrar los clientes presentes en la grilla de asistencias de la jornada actual
                return empleadosJornada
                    .Select(x => x.Cliente?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c);
            }
        }

        protected IEnumerable<string> AreasDisponiblesGrilla
        {
            get
            {
                string cli = !string.IsNullOrWhiteSpace(filtroClienteGrilla) ? filtroClienteGrilla : ClienteActivoAsistenciasSeguro;
                if (!string.IsNullOrWhiteSpace(cli))
                {
                    var areasJornada = empleadosJornada
                        .Where(x => string.Equals(x.Cliente, cli, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Area?.Trim())
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .Select(a => a!);

                    var areasCatalogo = GetAreasParaCliente(cli)
                        .Where(a => !string.IsNullOrWhiteSpace(a));

                    return areasJornada.Union(areasCatalogo, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(a => a);
                }
                else
                {
                    return empleadosJornada
                        .Select(x => x.Area?.Trim())
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .Select(a => a!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(a => a);
                }
            }
        }

        protected void OnFiltroClienteGrillaChanged(string? nuevoCliente)
        {
            filtroClienteGrilla = nuevoCliente;
            // Al seleccionar o cambiar cliente, resetear el filtro de área para seleccionar entre las áreas del nuevo cliente
            filtroAreaGrilla = null;
            StateHasChanged();
        }

        protected void OnFiltroAreaGrillaChanged(string? nuevaArea)
        {
            filtroAreaGrilla = nuevaArea;
            StateHasChanged();
        }

        protected void LimpiarFiltrosUbicacionGrilla()
        {
            filtroClienteGrilla = null;
            filtroAreaGrilla = null;
            StateHasChanged();
        }

        // Lista de grupos consolidados para Helpdesk (Tab 2)
        protected List<ConsolidadoHelpdeskItem> gruposConsolidados = new();

        // Clientes Asignados al Supervisor (desde tbl_bs_supervisor_cliente con AplicaHoras activado)
        protected List<SupervisorClienteDto> misClientesAsignados = new();
        protected List<string> NombresMisClientes => misClientesAsignados
            .Where(c => c.AplicaHoras)
            .Select(c => c.ClienteNombre?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Sub-tabs de Clientes para Asistencias y Mi Personal
        protected string clienteActivoAsistencias = string.Empty;
        protected string clienteActivoPlantilla = string.Empty;

                protected List<string> ListaClientesAsistencia
        {
            get
            {
                var lista = new List<string>();
                if (NombresMisClientes.Any())
                {
                    lista.AddRange(NombresMisClientes);
                }
                // Si hay colaboradores en la jornada asignados a otro cliente (ej. un préstamo o movimiento),
                // incluir ese cliente en las pestañas para que el supervisor pueda verlo y gestionarlo
                foreach (var c in empleadosJornada.Select(e => e.Cliente?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    if (!lista.Contains(c, StringComparer.OrdinalIgnoreCase))
                        lista.Add(c!);
                }
                if (!lista.Any())
                {
                    foreach (var c in miPlantillaPersonal.Select(p => p.ClienteDefault?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        if (!lista.Contains(c, StringComparer.OrdinalIgnoreCase))
                            lista.Add(c!);
                    }
                }
                return lista.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        protected string ClienteActivoAsistenciasSeguro
        {
            get
            {
                var clientes = ListaClientesAsistencia;
                if (string.IsNullOrWhiteSpace(clienteActivoAsistencias) || !clientes.Contains(clienteActivoAsistencias, StringComparer.OrdinalIgnoreCase))
                {
                    clienteActivoAsistencias = clientes.FirstOrDefault() ?? "";
                }
                return clienteActivoAsistencias;
            }
        }

        protected bool mostrarTodosClientesHelpdesk = false;

        protected bool PerteneceAClienteSeleccionado(string? clienteItem)
        {
            if (mostrarTodosClientesHelpdesk) return true;
            var cliActivo = ClienteActivoAsistenciasSeguro;
            if (string.IsNullOrWhiteSpace(cliActivo)) return true;
            if (string.IsNullOrWhiteSpace(clienteItem)) return ListaClientesAsistencia.Count <= 1;
            return string.Equals(clienteItem.Trim(), cliActivo.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        protected void CambiarClienteAsistencias(string cliente)
        {
            if (cliente == "__TODOS__")
            {
                mostrarTodosClientesHelpdesk = true;
            }
            else
            {
                mostrarTodosClientesHelpdesk = false;
                clienteActivoAsistencias = cliente;
            }
            filtroAreaGrilla = null;
            foreach (var emp in empleadosJornada)
            {
                emp.Seleccionado = false;
            }
            if (activeTabIndex == 6 && !ClienteActualPermiteHorasExtras)
            {
                activeTabIndex = 0;
            }
            StateHasChanged();
        }

        protected List<string> ListaClientesPlantilla
        {
            get
            {
                if (NombresMisClientes.Any())
                {
                    return NombresMisClientes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
                var lista = new List<string>();
                foreach (var c in miPlantillaPersonal.Select(p => p.ClienteDefault?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    if (!lista.Contains(c, StringComparer.OrdinalIgnoreCase))
                        lista.Add(c!);
                }
                return lista.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        protected string ClienteActivoPlantillaSeguro
        {
            get
            {
                var clientes = ListaClientesPlantilla;
                if (string.IsNullOrWhiteSpace(clienteActivoPlantilla) || !clientes.Contains(clienteActivoPlantilla, StringComparer.OrdinalIgnoreCase))
                {
                    clienteActivoPlantilla = clientes.FirstOrDefault() ?? "";
                }
                return clienteActivoPlantilla;
            }
        }

        protected void CambiarClientePlantilla(string cliente)
        {
            clienteActivoPlantilla = cliente;
            filtroAreaPlantilla = null;
            StateHasChanged();
        }

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
        // Pestaña Bonos (Tab 4 - RRHH Tracker 11)
        protected string textoBusquedaBonos = string.Empty;
        protected string filtroBonos = "CON_BONO"; // "CON_BONO", "PENDIENTES", "TODOS"
        protected bool estaEnviandoBonos = false;
        protected bool estaEnviandoViaticos = false;
        protected ModoVistaGrilla modoVistaBonos = ModoVistaGrilla.Tarjetas;

        protected int TotalPresentesCliente => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente));
        protected decimal TotalViaticosTraspCliente => empleadosJornada.Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente)).Sum(x => x.VtcoTrasp);
        protected decimal TotalViaticosAlimCliente => empleadosJornada.Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente)).Sum(x => x.VtcoAlim);
        protected decimal TotalViaticosBncCliente => empleadosJornada.Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente)).Sum(x => x.BonoBnc);

        protected int TotalConBono => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && x.BonoBnc > 0);
        protected int TotalBonosSincronizados => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && x.BonoBnc > 0 && x.SincronizadoBono);
        protected int TotalBonosPendientes => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && x.BonoBnc > 0 && !x.SincronizadoBono);
        protected int CantidadBonosSeleccionados => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && x.BonoBnc > 0 && !x.SincronizadoBono && x.Seleccionado);

        protected int TotalConViaticosTransporteAlim => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && (x.VtcoTrasp > 0 || x.VtcoAlim > 0));
        protected int TotalViaticosSincronizados => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && (x.VtcoTrasp > 0 || x.VtcoAlim > 0) && x.SincronizadoViatico);
        protected int TotalViaticosPendientes => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && (x.VtcoTrasp > 0 || x.VtcoAlim > 0) && !x.SincronizadoViatico);
        protected List<ConsolidadoHelpdeskItem> GruposMovimientosFiltrados => gruposConsolidados.Where(g => PerteneceAClienteSeleccionado(g.Cliente)).ToList();
        protected int TotalGruposViaticosPendientes => GruposViaticos.Count(g => !g.Sincronizado && g.Recursos > 0);
        protected int TotalGruposMovimientosPendientes => GruposMovimientosFiltrados.Count(g => !g.Sincronizado && g.Cantidad > 0);

        protected IEnumerable<EmpleadoJornadaItem> EmpleadosBonosFiltrados
        {
            get
            {
                var query = empleadosJornada.Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente));
                if (filtroBonos == "CON_BONO")
                {
                    query = query.Where(x => x.BonoBnc > 0);
                }
                else if (filtroBonos == "PENDIENTES")
                {
                    query = query.Where(x => x.BonoBnc > 0 && !x.SincronizadoBono);
                }

                if (!string.IsNullOrWhiteSpace(textoBusquedaBonos))
                {
                    var busq = textoBusquedaBonos.Trim();
                    query = query.Where(x =>
                        x.NombreEmpleado.Contains(busq, StringComparison.OrdinalIgnoreCase) ||
                        x.CodigoEmpleado.ToString().Contains(busq) ||
                        x.Area.Contains(busq, StringComparison.OrdinalIgnoreCase) ||
                        x.Cliente.Contains(busq, StringComparison.OrdinalIgnoreCase));
                }

                return query;
            }
        }

        protected int TotalAusentesCliente => empleadosJornada.Count(x => !x.Asistio && PerteneceAClienteSeleccionado(x.Cliente));
        protected int TotalAusenciasSincronizadas => empleadosJornada.Count(x => !x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && x.SincronizadoAusencia);
        protected int TotalAusenciasPendientes => empleadosJornada.Count(x => !x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && !x.SincronizadoAusencia);
        protected int TotalAusenciasPendientesFiltradas => AusentesFiltrados.Count(x => !x.SincronizadoAusencia);
        protected int CantidadAusentesSeleccionados => AusentesFiltrados.Count(x => x.Seleccionado && !x.SincronizadoAusencia);

        protected IEnumerable<EmpleadoJornadaItem> AusentesFiltrados
        {
            get
            {
                var ausentes = empleadosJornada.Where(x => !x.Asistio && PerteneceAClienteSeleccionado(x.Cliente));
                DateTime fRef = fechaSeleccionada ?? DateTime.Today;
                foreach (var a in ausentes)
                {
                    if (a.SincronizadoAusencia && a.Seleccionado)
                        a.Seleccionado = false;
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
                // Filtros rápidos de Cliente y Área para Mi Personal (Plantilla)
        protected string? filtroClientePlantilla { get; set; } = null;
        protected string? filtroAreaPlantilla { get; set; } = null;

        protected IEnumerable<string> ClientesDisponiblesPlantilla
        {
            get
            {
                // Clientes presentes en la plantilla del supervisor
                return miPlantillaPersonal
                    .Select(x => x.ClienteDefault?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c);
            }
        }

        protected IEnumerable<string> AreasDisponiblesPlantilla
        {
            get
            {
                string cli = !string.IsNullOrWhiteSpace(filtroClientePlantilla) ? filtroClientePlantilla : ClienteActivoPlantillaSeguro;
                if (!string.IsNullOrWhiteSpace(cli))
                {
                    var areasPlantilla = miPlantillaPersonal
                        .Where(x => string.Equals(x.ClienteDefault, cli, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.AreaDefault?.Trim())
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .Select(a => a!);

                    var areasCatalogo = GetAreasParaCliente(cli)
                        .Where(a => !string.IsNullOrWhiteSpace(a));

                    return areasPlantilla.Union(areasCatalogo, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(a => a);
                }
                else
                {
                    return miPlantillaPersonal
                        .Select(x => x.AreaDefault?.Trim())
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .Select(a => a!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(a => a);
                }
            }
        }

        protected void OnFiltroClientePlantillaChanged(string? nuevoCliente)
        {
            filtroClientePlantilla = nuevoCliente;
            filtroAreaPlantilla = null;
            StateHasChanged();
        }

        protected void OnFiltroAreaPlantillaChanged(string? nuevaArea)
        {
            filtroAreaPlantilla = nuevaArea;
            StateHasChanged();
        }

        protected void LimpiarFiltrosPlantilla()
        {
            filtroClientePlantilla = null;
            filtroAreaPlantilla = null;
            textoBusquedaPlantilla = string.Empty;
            StateHasChanged();
        }

        protected IEnumerable<SupervisorPlantillaItemDto> PlantillaFiltrada
        {
            get
            {
                var query = miPlantillaPersonal.AsEnumerable();

                string cli = !string.IsNullOrWhiteSpace(filtroClientePlantilla) ? filtroClientePlantilla : ClienteActivoPlantillaSeguro;
                if (!string.IsNullOrWhiteSpace(cli))
                {
                    query = query.Where(x => string.Equals(x.ClienteDefault, cli, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filtroAreaPlantilla))
                {
                    query = query.Where(x => string.Equals(x.AreaDefault, filtroAreaPlantilla, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(textoBusquedaPlantilla))
                {
                    query = query.Where(x =>
                        x.NombreEmpleado.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase) ||
                        x.CodigoEmpleado.ToString().Contains(textoBusquedaPlantilla) ||
                        x.ClienteDefault.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase) ||
                        x.AreaDefault.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase) ||
                        x.PuestoDefault.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(x.TipoEmpleado) && x.TipoEmpleado.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.ClienteOrigenNomina) && x.ClienteOrigenNomina.Contains(textoBusquedaPlantilla, StringComparison.OrdinalIgnoreCase)));
                }

                return query;
            }
        }

        // Catálogo de Áreas por Cliente (Tab 5)
        protected List<ClienteAreaDto> catalogoClienteAreas = new();
        protected Dictionary<string, List<string>> areasPorCliente = new(StringComparer.OrdinalIgnoreCase);
        protected int? filtroClienteAreaId = null;
        protected string filtroTextoArea = string.Empty;
        protected bool cargandoAreas = false;
        protected List<Combos> listaAreasAtributoOdoo = new();
        protected string nuevaAreaNombre = string.Empty;
        protected int? nuevoClienteAreaOpId = null;

        // Caché de Reglas de Viáticos por Horario (Cargado en memoria para evaluación instantánea)
        protected List<ReglaViaticoDto> cacheReglasViaticos = new();
        protected bool cargandoReglasViaticos = false;

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
        protected decimal TotalViaticosCliente => TotalViaticosTrasp + TotalViaticosAlim;
        protected decimal TotalViaticosBnc => empleadosJornada.Where(x => x.Asistio).Sum(x => x.BonoBnc);
        protected decimal TotalViaticosEfectivo => empleadosJornada.Where(x => x.Asistio).Sum(x => (x.TraspAplicaNomina ? 0 : x.VtcoTrasp) + (x.AlimAplicaNomina ? 0 : x.VtcoAlim));
        protected decimal TotalViaticosGlobal => TotalViaticosCliente + empleadosJornada.Where(x => x.Asistio).Sum(x => x.BonoExtra);
        protected int CantidadSeleccionados => empleadosJornada.Count(x => x.Seleccionado);
        protected bool HayCambiosSinGuardar => empleadosJornada.Any(x => x.Modificado || x.Id == 0);
        protected int TotalGuardadosEnBd => empleadosJornada.Count(x => x.Id > 0 && !x.Modificado);
        protected int TotalModificadosSinGuardar => empleadosJornada.Count(x => x.Id > 0 && x.Modificado);
        protected int TotalNuevosSinGuardar => empleadosJornada.Count(x => x.Id <= 0);
        protected int TotalConflictosHorarios => empleadosJornada.Count(x => x.TieneConflictoHorario);

        protected int TotalConViaticos => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && (x.VtcoTrasp > 0 || x.VtcoAlim > 0));
        protected int TotalSinViaticos => empleadosJornada.Count(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && x.VtcoTrasp == 0 && x.VtcoAlim == 0);

        // Filtros para la pestaña de Viáticos
        protected string textoBusquedaViaticos = string.Empty;
        protected string filtroViaticos = "CON_VIATICOS"; // Por defecto solo mostrar quienes tienen viático
        protected string modoVistaViaticos = "GRUPOS"; // "GRUPOS" (Solicitudes con Recursos) o "COLABORADORES"
        protected ModoVistaGrilla modoVistaViaticosFormato = ModoVistaGrilla.Tarjetas; // Tarjetas o Tabla (Operativo)

        protected List<GrupoViaticoItem> GruposViaticos
        {
            get
            {
                var conViaticos = empleadosJornada
                    .Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && (x.VtcoTrasp > 0 || x.VtcoAlim > 0))
                    .ToList();
                return conViaticos
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
                        x.TraspAplicaNomina,
                        x.AlimAplicaNomina,
                        TicketViaticoKey = (x.TicketViaticoId.HasValue && x.TicketViaticoId > 0) ? x.TicketViaticoId.Value : 0
                    })
                    .Select((g, idx) => new GrupoViaticoItem
                    {
                        Index = idx + 1,
                        Cliente = g.Key.Cliente,
                        Area = g.Key.Area,
                        Puesto = g.Key.Puesto,
                        HoraInicio = g.Key.HoraInicio,
                        HoraFin = g.Key.HoraFin,
                        TurnoNombre = g.Key.TurnoNombre,
                        VtcoTrasp = g.Key.VtcoTrasp,
                        VtcoAlim = g.Key.VtcoAlim,
                        TraspAplicaNomina = g.Key.TraspAplicaNomina,
                        AlimAplicaNomina = g.Key.AlimAplicaNomina,
                        Empleados = g.ToList()
                    })
                    .ToList();
            }
        }

        protected IEnumerable<EmpleadoJornadaItem> EmpleadosViaticosFiltrados
        {
            get
            {
                var query = empleadosJornada.Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente));

                if (filtroViaticos == "CON_VIATICOS")
                {
                    query = query.Where(x => x.VtcoTrasp > 0 || x.VtcoAlim > 0);
                }
                else if (filtroViaticos == "SIN_VIATICOS")
                {
                    query = query.Where(x => x.VtcoTrasp == 0 && x.VtcoAlim == 0);
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

        // Claves de LocalStorage por usuario para trabajo Offline y Caché local
        protected bool esModoOnline { get; set; } = true;
        protected DateTime? ultimaHoraGuardadoLocal { get; set; } = null;
        private DotNetObjectReference<RegistroMovimientosSupervisor>? dotNetHelper;

        private string KeyBorradorJornada => $"supervisor_offline_jornada_{currentUserName}_{fechaSeleccionada:yyyyMMdd}";
        private string KeyCachePlantilla => $"supervisor_cache_plantilla_{currentUserName}";
        private string KeyCacheAreas => $"supervisor_cache_areas_{currentUserName}";
        private string KeyCacheMisClientes => $"supervisor_cache_mis_clientes_{currentUserName}";
        private string KeyCacheOperaciones => $"supervisor_cache_operaciones_{currentUserName}";
        private string KeyCacheReglasViaticos => $"supervisor_cache_reglas_viaticos_{currentUserName}";
        private string KeyCacheTiposAusencias => "supervisor_cache_tipos_ausencias";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                try
                {
                    dotNetHelper = DotNetObjectReference.Create(this);
                    esModoOnline = await _js.InvokeAsync<bool>("connectivityInterop.isOnline");
                    await _js.InvokeVoidAsync("connectivityInterop.registerListener", dotNetHelper);
                    StateHasChanged();
                }
                catch { }
            }
        }

        [JSInvokable]
        public void OnConexionCambiada(bool online)
        {
            esModoOnline = online;
            if (online)
            {
                _snackbar.Add("🟢 Conexión a internet restablecida. Puedes sincronizar tus cambios con la Base de Datos.", Severity.Success, config => config.VisibleStateDuration = 6000);
            }
            else
            {
                _snackbar.Add("🟡 Modo Offline (Bodega): Sin conexión a internet. Los cambios se están guardando localmente en este dispositivo.", Severity.Warning, config => config.VisibleStateDuration = 6000);
            }
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            try
            {
                dotNetHelper?.Dispose();
            }
            catch { }
        }

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

                activeTabIndex = 0;
                await LimpiarTodosLosBorradoresLocales();
                await CargarMisClientesAsignados();
                await Task.WhenAll(
                    CargarOperaciones(),
                    CargarCatalogoClienteAreas(),
                    CargarCacheReglasViaticos(),
                    CargarMiPlantilla(),
                    CargarTiposAusencias(),
                    CargarConfigHorasExtrasClientes(),
                    CargarAsignacionesJornadasCliente()
                );
                await RecuperarOperacionPrevia();
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
                if (resp?.Model != null && resp.Model.Any())
                {
                    operacionesList = resp.Model.OrderBy(x => x.id).ToList();
                    await _js.InvokeVoidAsync("localStorage.setItem", KeyCacheOperaciones, JsonSerializer.Serialize(operacionesList));
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar operaciones online: {ex.Message}");
            }

            // Fallback offline desde localStorage
            try
            {
                var cachedJson = await _js.InvokeAsync<string>("localStorage.getItem", KeyCacheOperaciones);
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cached = JsonSerializer.Deserialize<List<Combos>>(cachedJson);
                    if (cached != null && cached.Any())
                    {
                        operacionesList = cached;
                    }
                }
            }
            catch { }
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
            activeTabIndex = 0;
            await CargarJornadaActual();
        }

        protected async Task OnFechaSeleccionadaChanged(DateTime? nuevaFecha)
        {
            fechaSeleccionada = nuevaFecha;
            activeTabIndex = 0;
            await CargarJornadaActual();
        }

        // --- CARGA DE LA JORNADA (Desde BD o Borrador Offline) ---
        protected async Task CargarJornadaActual()
        {
            if (!fechaSeleccionada.HasValue) return;

            estaCargando = true;
            try
            {
                // 1. Comprobar si hay un borrador guardado en este dispositivo para esta fecha
                bool tieneBorradorLocal = false;
                List<EmpleadoJornadaItem>? borradorLocal = null;
                try
                {
                    var jsonLocal = await _js.InvokeAsync<string>("localStorage.getItem", KeyBorradorJornada);
                    if (!string.IsNullOrWhiteSpace(jsonLocal))
                    {
                        borradorLocal = JsonSerializer.Deserialize<List<EmpleadoJornadaItem>>(jsonLocal);
                        tieneBorradorLocal = borradorLocal != null && borradorLocal.Any();
                        if (tieneBorradorLocal)
                        {
                            ultimaHoraGuardadoLocal = DateTime.Now;
                        }
                    }
                }
                catch { }

                // 2. Intentar consultar en PostgreSQL registros en base de datos
                bool exitoBd = false;
                IListResponse<MovimientoPersonalDiarioDto>? resp = null;
                try
                {
                    resp = await _odooService.GetMovimientosPersonalDiario(fechaSeleccionada.Value, 0, currentUserName);
                    exitoBd = (resp?.Respuesta != null && !resp.Respuesta.ExisteError);
                }
                catch (Exception exBd)
                {
                    Console.WriteLine($"Sin conexión a BD al cargar jornada: {exBd.Message}");
                    exitoBd = false;
                }

                if (exitoBd && resp?.Model != null && resp.Model.Any())
                {
                    DateTime fRef = fechaSeleccionada ?? DateTime.Today;
                    var itemsBd = resp.Model.Select(m =>
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
                            Bnc = !m.Asistio ? "0" : (m.Bnc ?? string.Empty),
                            VtcoTrasp = !m.Asistio ? 0m : m.VtcoTrasp,
                            VtcoAlim = !m.Asistio ? 0m : m.VtcoAlim,
                            TraspAplicaNomina = m.Asistio && (m.TraspAplicaNomina || (m.VtcoTrasp > 0 && decimal.TryParse(m.Bnc, NumberStyles.Any, CultureInfo.InvariantCulture, out var bVal1) && bVal1 >= m.VtcoTrasp)),
                            AlimAplicaNomina = m.Asistio && (m.AlimAplicaNomina || (m.VtcoAlim > 0 && decimal.TryParse(m.Bnc, NumberStyles.Any, CultureInfo.InvariantCulture, out var bVal2) && bVal2 >= m.VtcoAlim)),
                            Observaciones = m.Observaciones ?? string.Empty,
                            TipoEmpleado = miPlantillaPersonal.FirstOrDefault(p => p.CodigoEmpleado == m.CodigoEmpleado)?.TipoEmpleado ?? "Permanente",
                            Asistio = m.Asistio,
                            Estado = m.Estado,
                            TicketRedmineId = !m.Asistio ? null : m.TicketRedmineId,
                            TicketViaticoId = !m.Asistio ? null : m.TicketViaticoId,
                            TicketBonoId = !m.Asistio ? null : m.TicketBonoId,
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

                    // Fusión inteligente: Si hay un borrador local que contiene registros modificados offline en bodega,
                    // preservar las ediciones locales para que el supervisor no pierda su recorrido
                    if (tieneBorradorLocal && borradorLocal != null && borradorLocal.Any(x => x.Modificado || x.Id == 0))
                    {
                        foreach (var modLocal in borradorLocal.Where(x => x.Modificado || x.Id == 0))
                        {
                            var coincidente = itemsBd.FirstOrDefault(b =>
                                (modLocal.Id > 0 && b.Id == modLocal.Id) ||
                                (modLocal.Id <= 0
                                    && b.CodigoEmpleado == modLocal.CodigoEmpleado
                                    && string.Equals(b.Cliente?.Trim(), modLocal.Cliente?.Trim(), StringComparison.OrdinalIgnoreCase)
                                    && ((!b.Asistio && !modLocal.Asistio) ||
                                        string.Equals(NormalizarFormatoHora(b.HoraEntrada), NormalizarFormatoHora(modLocal.HoraEntrada), StringComparison.OrdinalIgnoreCase))));

                            if (coincidente != null)
                            {
                                coincidente.Asistio = modLocal.Asistio;
                                coincidente.HoraEntrada = modLocal.HoraEntrada;
                                coincidente.HoraSalida = modLocal.HoraSalida;
                                coincidente.TiempoComida = modLocal.TiempoComida;
                                coincidente.TurnoNombre = modLocal.TurnoNombre;
                                coincidente.Area = modLocal.Area;
                                coincidente.Puesto = modLocal.Puesto;
                                coincidente.VtcoTrasp = modLocal.VtcoTrasp;
                                coincidente.VtcoAlim = modLocal.VtcoAlim;
                                coincidente.BonoBnc = modLocal.BonoBnc;
                                coincidente.TraspAplicaNomina = modLocal.TraspAplicaNomina;
                                coincidente.AlimAplicaNomina = modLocal.AlimAplicaNomina;
                                coincidente.TipoAusencia = modLocal.TipoAusencia;
                                coincidente.Observaciones = modLocal.Observaciones;
                                coincidente.Modificado = true;
                            }
                            else if (modLocal.Id == 0 && !itemsBd.Any(b =>
                                b.CodigoEmpleado == modLocal.CodigoEmpleado
                                && string.Equals(b.Cliente?.Trim(), modLocal.Cliente?.Trim(), StringComparison.OrdinalIgnoreCase)
                                && ((!b.Asistio && !modLocal.Asistio) ||
                                    (string.Equals(NormalizarFormatoHora(b.HoraEntrada), NormalizarFormatoHora(modLocal.HoraEntrada), StringComparison.OrdinalIgnoreCase)
                                     && string.Equals(NormalizarFormatoHora(b.HoraSalida), NormalizarFormatoHora(modLocal.HoraSalida), StringComparison.OrdinalIgnoreCase)))))
                            {
                                itemsBd.Add(modLocal);
                            }
                        }
                        itemsBd = DeduplicarEmpleadosJornada(itemsBd);
                        _snackbar.Add($"Jornada sincronizada con BD preservando tus ediciones locales de bodega ({itemsBd.Count} registros).", Severity.Info);
                    }
                    else
                    {
                        itemsBd = DeduplicarEmpleadosJornada(itemsBd);
                        _snackbar.Add($"Jornada del {fechaSeleccionada:dd/MM/yyyy} cargada de BD ({itemsBd.Count} registros de {currentUserName}).", Severity.Info);
                    }

                    empleadosJornada = itemsBd;
                    await AutoGuardarBorradorLocal();
                }
                else if (exitoBd)
                {
                    // La base de datos respondió exitosamente y confirmó que no hay registros para esta fecha.
                    // El día aún no se ha trabajado: mantener limpio en 0 y remover cualquier borrador previo.
                    empleadosJornada.Clear();
                    await LimpiarBorradorLocal();
                }
                else if (!exitoBd && tieneBorradorLocal && borradorLocal != null && borradorLocal.Any())
                {
                    // Modo Offline real (sin conexión al servidor): cargar borrador local de bodega
                    empleadosJornada = DeduplicarEmpleadosJornada(borradorLocal);
                    _snackbar.Add($"Modo Offline: jornada cargada desde memoria local del dispositivo ({empleadosJornada.Count} colaboradores).", Severity.Info);
                }
                else
                {
                    empleadosJornada.Clear();
                }

                ValidarConflictosHorarios(out _);
                ActualizarGruposConsolidadosInterno(false);
                fechaDesdeFiltroHe = fechaSeleccionada ?? DateTime.Today;
                fechaHastaFiltroHe = fechaSeleccionada ?? DateTime.Today;
                await CargarAutorizacionesHorasExtras();
            }
            catch (Exception ex)
            {
                bool recuperado = await RecuperarBorradorLocal();
                if (recuperado)
                {
                    _snackbar.Add($"Modo Offline: jornada recuperada desde memoria del dispositivo ({empleadosJornada.Count} colaboradores).", Severity.Info);
                }
                else
                {
                    _snackbar.Add($"Error al cargar jornada: {ex.Message}", Severity.Error);
                }
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
                var cachedJson = await _js.InvokeAsync<string>("localStorage.getItem", KeyCacheMisClientes);
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cached = JsonSerializer.Deserialize<List<SupervisorClienteDto>>(cachedJson);
                    if (cached != null && cached.Any())
                    {
                        misClientesAsignados = cached;
                    }
                }
            }
            catch { }

            try
            {
                // 1. Consultar por el identificador actual (email o username, ej: sinsa@bussersa.com)
                var resp = await _odooService.GetSupervisorClientes(usuario: currentUserName);
                if (resp?.Model != null && resp.Model.Any())
                {
                    misClientesAsignados = resp.Model.Where(c => c.Activo && c.AplicaHoras).ToList();
                    return;
                }

                // 2. Si _userInfo.UserId > 0, consultar por el ID numérico
                if (_userInfo.UserId > 0)
                {
                    var respId = await _odooService.GetSupervisorClientes(usuario: _userInfo.UserId.ToString());
                    if (respId?.Model != null && respId.Model.Any())
                    {
                        misClientesAsignados = respId.Model.Where(c => c.Activo && c.AplicaHoras).ToList();
                        return;
                    }
                }

                // 3. Si _userInfo.UserName es diferente a currentUserName
                if (!string.IsNullOrWhiteSpace(_userInfo.UserName) && !string.Equals(_userInfo.UserName, currentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    var respUser = await _odooService.GetSupervisorClientes(usuario: _userInfo.UserName);
                    if (respUser?.Model != null && respUser.Model.Any())
                    {
                        misClientesAsignados = respUser.Model.Where(c => c.Activo && c.AplicaHoras).ToList();
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
                        misClientesAsignados = respSinDom.Model.Where(c => c.Activo && c.AplicaHoras).ToList();
                    }
                }

                if (misClientesAsignados.Any())
                {
                    await _js.InvokeVoidAsync("localStorage.setItem", KeyCacheMisClientes, JsonSerializer.Serialize(misClientesAsignados));
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar clientes asignados online: {ex.Message}");
            }

            // Fallback offline desde localStorage para Clientes
            if (!misClientesAsignados.Any())
            {
                try
                {
                    var cachedJson = await _js.InvokeAsync<string>("localStorage.getItem", KeyCacheMisClientes);
                    if (!string.IsNullOrWhiteSpace(cachedJson))
                    {
                        var cached = JsonSerializer.Deserialize<List<SupervisorClienteDto>>(cachedJson);
                        if (cached != null && cached.Any())
                        {
                            misClientesAsignados = cached;
                        }
                    }
                }
                catch { }
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

                    // Si el supervisor tiene clientes asignados (ej. Sinsa)
                    if (NombresMisClientes.Any())
                    {
                        // Auto-corregir si algún colaborador del supervisor quedó con cliente "Administración", "General" o vacío
                        if (NombresMisClientes.Count == 1)
                        {
                            var clienteUnico = NombresMisClientes[0];
                            foreach (var it in items)
                            {
                                if (string.IsNullOrWhiteSpace(it.ClienteDefault) || 
                                    string.Equals(it.ClienteDefault, "Administración", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(it.ClienteDefault, "General", StringComparison.OrdinalIgnoreCase))
                                {
                                    it.ClienteDefault = clienteUnico;
                                    _ = _odooService.SaveSupervisorPlantillaItem(it);
                                }
                            }
                        }

                    }
                    // Garantizar unicidad absoluta: el backend ya filtró por supervisor y clientes asignados
                    if (NombresMisClientes.Any())
                    {
                        items = items.Where(x => NombresMisClientes.Contains(x.ClienteDefault, StringComparer.OrdinalIgnoreCase) ||
                                                 string.Equals(x.TipoEmpleado, "Prestamo", StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    miPlantillaPersonal = items
                        .GroupBy(x => x.CodigoEmpleado > 0 
                            ? $"cod_{x.CodigoEmpleado}" 
                            : (x.IdEmpleado > 0 ? $"id_{x.IdEmpleado}" : x.NombreEmpleado.Trim().ToLowerInvariant()))
                        .Select(g => { var p = g.First(); p.ClienteOrigenNomina = LimpiarTextoOdoo(p.ClienteOrigenNomina); return p; })
                        .ToList();

                    if (miPlantillaPersonal.Any())
                    {
                        await _js.InvokeVoidAsync("localStorage.setItem", KeyCachePlantilla, JsonSerializer.Serialize(miPlantillaPersonal));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar plantilla online: {ex.Message}");
            }
            finally
            {
                cargandoPlantilla = false;
            }

            // Fallback offline para Mi Personal
            if (!miPlantillaPersonal.Any())
            {
                try
                {
                    var cachedJson = await _js.InvokeAsync<string>("localStorage.getItem", KeyCachePlantilla);
                    if (!string.IsNullOrWhiteSpace(cachedJson))
                    {
                        var cached = JsonSerializer.Deserialize<List<SupervisorPlantillaItemDto>>(cachedJson);
                        if (cached != null && cached.Any())
                        {
                            miPlantillaPersonal = cached;
                            _snackbar.Add($"Plantilla de {miPlantillaPersonal.Count} colaboradores cargada desde memoria local (Modo Offline).", Severity.Info);
                        }
                    }
                }
                catch { }
            }
        }

        // --- CARGAR DATOS BASE (COPIAR DE LA TABLA MAESTRA DE ADMINISTRACIÓN) ---
        protected Task CargarDatosBasePlantilla() => CargarDatosBasePlantilla(ClienteActivoPlantillaSeguro);
        protected async Task CargarDatosBasePlantilla(string? clienteFiltro)
        {
            try
            {
                if (!misClientesAsignados.Any())
                {
                    await CargarMisClientesAsignados();
                }

                string clienteTarget = !string.IsNullOrWhiteSpace(clienteFiltro) ? clienteFiltro : ClienteActivoPlantillaSeguro;
                var clientesAProcesar = !string.IsNullOrWhiteSpace(clienteTarget) 
                    ? new List<string> { clienteTarget } 
                    : NombresMisClientes;

                if (!clientesAProcesar.Any())
                {
                    _snackbar.Add("No tienes clientes asignados configurados en el sistema de administración.", Severity.Warning);
                    return;
                }

                var plantillaActualCliente = miPlantillaPersonal
                    .Where(x => clientesAProcesar.Contains(x.ClienteDefault, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (plantillaActualCliente.Any())
                {
                    bool? confirm = await _dialogService.ShowMessageBox(
                        "Cargar Datos Base",
                        $"¿Deseas reemplazar tu plantilla de {string.Join(", ", clientesAProcesar)} ({plantillaActualCliente.Count} colaboradores) con los datos base de administración?",
                        yesText: "Sí, Cargar Base",
                        cancelText: "Cancelar");

                    if (confirm != true) return;
                }

                cargandoPlantilla = true;
                textoOperacionBd = $"Copiando colaboradores base de {string.Join(", ", clientesAProcesar)} a tu plantilla...";
                StateHasChanged();

                var nuevosBase = new List<PersonalClienteItemDto>();
                foreach (var cl in clientesAProcesar)
                {
                    var respPersonal = await _odooService.GetPersonalCliente(cliente: cl);
                    if (respPersonal?.Model != null && respPersonal.Model.Any())
                    {
                        nuevosBase.AddRange(respPersonal.Model.Where(p => p.Activo));
                    }
                }

                if (!nuevosBase.Any())
                {
                    _snackbar.Add("No hay registros cargados para este cliente.", Severity.Info);
                    return;
                }

                var unicos = nuevosBase
                    .GroupBy(p => p.CodigoEmpleado)
                    .Select(g => g.First())
                    .ToList();

                // Mantener los de otros clientes y los préstamos/temporales
                var aConservar = miPlantillaPersonal
                    .Where(x => !clientesAProcesar.Contains(x.ClienteDefault, StringComparer.OrdinalIgnoreCase) ||
                                string.Equals(x.TipoEmpleado, "Temporal", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(x.TipoEmpleado, "Prestamo", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                miPlantillaPersonal.Clear();
                miPlantillaPersonal.AddRange(aConservar);

                var itemsParaGuardar = unicos.Select(b => new SupervisorPlantillaItemDto
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
                }).ToList();

                var reqMasivo = new GuardarSupervisorPlantillaRequest
                {
                    Usuario = currentUserName,
                    Items = itemsParaGuardar
                };
                await _odooService.SaveSupervisorPlantillaMasivo(reqMasivo);
                miPlantillaPersonal.AddRange(itemsParaGuardar);
                await _js.InvokeVoidAsync("localStorage.setItem", KeyCachePlantilla, JsonSerializer.Serialize(miPlantillaPersonal));
                _snackbar.Add($"Se cargaron exitosamente {itemsParaGuardar.Count} colaboradores base de {string.Join(", ", clientesAProcesar)} a tu plantilla.", Severity.Success);
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
        protected Task ActualizarPlantillaDesdeBase() => ActualizarPlantillaDesdeBase(ClienteActivoPlantillaSeguro);
        protected async Task ActualizarPlantillaDesdeBase(string? clienteFiltro)
        {
            try
            {
                string clienteTarget = !string.IsNullOrWhiteSpace(clienteFiltro) ? clienteFiltro : ClienteActivoPlantillaSeguro;
                var clientesAProcesar = !string.IsNullOrWhiteSpace(clienteTarget) 
                    ? new List<string> { clienteTarget } 
                    : NombresMisClientes;

                if (!clientesAProcesar.Any())
                {
                    _snackbar.Add("No tienes clientes asignados configurados en el sistema de administración.", Severity.Warning);
                    return;
                }

                cargandoPlantilla = true;
                textoOperacionBd = $"Actualizando plantilla de {string.Join(", ", clientesAProcesar)} desde administración...";
                StateHasChanged();

                if (!misClientesAsignados.Any())
                {
                    await CargarMisClientesAsignados();
                }

                var baseMaestra = new List<PersonalClienteItemDto>();
                foreach (var cl in clientesAProcesar)
                {
                    var respPersonal = await _odooService.GetPersonalCliente(cliente: cl);
                    if (respPersonal?.Model != null && respPersonal.Model.Any())
                    {
                        baseMaestra.AddRange(respPersonal.Model.Where(p => p.Activo));
                    }
                }

                if (!baseMaestra.Any())
                {
                    _snackbar.Add("No hay registros cargados para este cliente.", Severity.Info);
                    return;
                }

                var existentesCodigos = miPlantillaPersonal.Select(x => x.CodigoEmpleado).Where(c => c > 0).ToHashSet();
                var existentesIds = miPlantillaPersonal.Select(x => x.IdEmpleado).Where(i => i > 0).ToHashSet();
                var existentesNombres = miPlantillaPersonal.Select(x => x.NombreEmpleado.Trim().ToLowerInvariant()).Where(n => !string.IsNullOrWhiteSpace(n)).ToHashSet();

                var nuevosPorAgregar = baseMaestra
                    .Where(p => !existentesCodigos.Contains(p.CodigoEmpleado) 
                             && !existentesIds.Contains(p.IdEmpleado)
                             && !existentesNombres.Contains(p.NombreEmpleado.Trim().ToLowerInvariant()))
                    .GroupBy(p => p.CodigoEmpleado > 0 ? $"cod_{p.CodigoEmpleado}" : (p.IdEmpleado > 0 ? $"id_{p.IdEmpleado}" : p.NombreEmpleado.Trim().ToLowerInvariant()))
                    .Select(g => g.First())
                    .ToList();

                if (!nuevosPorAgregar.Any())
                {
                    _snackbar.Add($"Tu plantilla de {string.Join(", ", clientesAProcesar)} ya está al día. No hay nuevos colaboradores en la base maestra.", Severity.Info);
                    return;
                }

                var itemsParaGuardarNuevos = nuevosPorAgregar.Select(b => new SupervisorPlantillaItemDto
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
                }).ToList();

                var reqMasivoNuevos = new GuardarSupervisorPlantillaRequest
                {
                    Usuario = currentUserName,
                    Items = itemsParaGuardarNuevos
                };
                await _odooService.SaveSupervisorPlantillaMasivo(reqMasivoNuevos);
                miPlantillaPersonal.AddRange(itemsParaGuardarNuevos);
                await _js.InvokeVoidAsync("localStorage.setItem", KeyCachePlantilla, JsonSerializer.Serialize(miPlantillaPersonal));
                _snackbar.Add($"Se actualizaron y agregaron {itemsParaGuardarNuevos.Count} nuevos colaboradores de {string.Join(", ", clientesAProcesar)} a tu plantilla.", Severity.Success);
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

        // --- PRECARGAR PERSONAL POR CLIENTE CON REGLA DE PRÉSTAMO / MULTICLIENTE ---
        protected async Task PrecargarPersonalCliente(string cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente)) return;

            if (!miPlantillaPersonal.Any())
            {
                await CargarMiPlantilla();
            }

            var plantillaCliente = miPlantillaPersonal
                .Where(x => string.Equals(x.ClienteDefault, cliente, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!plantillaCliente.Any())
            {
                _snackbar.Add($"No tienes colaboradores configurados en Mi Personal para {cliente}. Puedes agregarlos desde la pestaña 'Mi Personal'.", Severity.Info);
                return;
            }

            cargandoPlantilla = true;
            textoOperacionBd = $"Cargando colaboradores de {cliente} a la jornada...";
            StateHasChanged();

            try
            {
                // 1. Consultar colaboradores que ya tienen turno/horario registrado o Vacaciones/Subsidios activos para esta fecha
                DateTime fechaRefCarga = (fechaSeleccionada ?? DateTime.Today).Date;
                Dictionary<int, Modelo.ClasesGenericas.MovimientoPersonalDiarioDto> turnosOcupados = new();
                List<Modelo.ClasesGenericas.MovimientoPersonalDiarioDto> listaTurnosYAusenciasBd = new();
                try
                {
                    var respTurnos = await _odooService.GetTurnosOcupadosFecha(fechaRefCarga);
                    if (respTurnos?.Model != null && respTurnos.Model.Any())
                    {
                        listaTurnosYAusenciasBd = respTurnos.Model.ToList();
                        turnosOcupados = listaTurnosYAusenciasBd
                            .Where(t => t.Asistio && t.CodigoEmpleado > 0 && !string.IsNullOrWhiteSpace(t.HoraEntrada) && t.HoraEntrada != "--:--")
                            .GroupBy(t => t.CodigoEmpleado)
                            .ToDictionary(g => g.Key, g => g.First());
                    }
                }
                catch { }

                var bloqueosVacSub = ConstruirBloqueosVacacionesSubsidios(listaTurnosYAusenciasBd, fechaRefCarga);
                var colaboradoresOmitidosPrestamo = new List<string>();
                var colaboradoresOmitidosVacSubsidio = new List<string>();
                int agregados = 0;

                foreach (var item in plantillaCliente)
                {
                    // a) Si ya está en la jornada para este mismo cliente, omitir para no duplicar
                    if (empleadosJornada.Any(x => x.CodigoEmpleado == item.CodigoEmpleado && string.Equals(x.Cliente, cliente, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    // a.2) Excluir si el colaborador está de Vacaciones (Descansadas) o Subsidio en el rango de la fecha activa
                    if (EstaEnVacacionesOSubsidioActivo(item.CodigoEmpleado, item.IdEmpleado, bloqueosVacSub, out string motivoVacSub))
                    {
                        colaboradoresOmitidosVacSubsidio.Add($"#{item.CodigoEmpleado} {item.NombreEmpleado} ({motivoVacSub})");
                        continue;
                    }

                    // b) REGLA DE PRÉSTAMO / MULTICLIENTE:
                    // Si ya está registrado en la jornada con OTRO cliente (ej. como préstamo), NO cargarlo automáticamente
                    var yaRegistradoEnOtro = empleadosJornada.FirstOrDefault(x => x.CodigoEmpleado == item.CodigoEmpleado && !string.Equals(x.Cliente, cliente, StringComparison.OrdinalIgnoreCase));
                    if (yaRegistradoEnOtro != null)
                    {
                        colaboradoresOmitidosPrestamo.Add($"{item.NombreEmpleado} (#{item.CodigoEmpleado}) en {yaRegistradoEnOtro.Cliente} ({yaRegistradoEnOtro.TurnoNombre} {yaRegistradoEnOtro.HoraEntrada}-{yaRegistradoEnOtro.HoraSalida})");
                        continue;
                    }

                    // c) Verificación preventiva en base de datos (otro supervisor u operación)
                    if (turnosOcupados.TryGetValue(item.CodigoEmpleado, out var turnoExistente))
                    {
                        bool esOtroCliente = !string.IsNullOrWhiteSpace(turnoExistente.Cliente) && 
                            !string.Equals(turnoExistente.Cliente.Trim(), cliente.Trim(), StringComparison.OrdinalIgnoreCase);

                        if (esOtroCliente)
                        {
                            colaboradoresOmitidosPrestamo.Add($"{item.NombreEmpleado} (#{item.CodigoEmpleado}) en BD: {turnoExistente.Cliente} ({turnoExistente.HoraEntrada}-{turnoExistente.HoraSalida})");
                            continue;
                        }
                    }

                    string obsPlantilla = string.Empty;
                    if (string.Equals(item.TipoEmpleado, "Prestamo", StringComparison.OrdinalIgnoreCase) || string.Equals(item.TipoEmpleado, "Temporal", StringComparison.OrdinalIgnoreCase))
                    {
                        string orig = !string.IsNullOrWhiteSpace(item.ClienteOrigenNomina) ? item.ClienteOrigenNomina : cliente;
                        obsPlantilla = $"{item.TipoEmpleado} (Nómina: {orig})";
                    }

                    empleadosJornada.Add(new EmpleadoJornadaItem
                    {
                        Id = 0,
                        IdEmpleado = item.IdEmpleado,
                        CodigoEmpleado = item.CodigoEmpleado,
                        NombreEmpleado = item.NombreEmpleado,
                        Cliente = cliente,
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
                        ClienteOrigenNomina = LimpiarTextoOdoo(item.ClienteOrigenNomina),
                        Observaciones = obsPlantilla,
                        Asistio = true,
                        Estado = "BORRADOR",
                        FechaInicioAusencia = fechaSeleccionada ?? DateTime.Today,
                        FechaFinAusencia = fechaSeleccionada ?? DateTime.Today,
                        DiasAusencia = 1.0m,
                        Modificado = true
                    });
                    var nuevoEmp = empleadosJornada.Last();
                    AplicarReglasViaticosEnMemoria(nuevoEmp);
                    agregados++;
                }

                if (colaboradoresOmitidosVacSubsidio.Any())
                {
                    _snackbar.Add($"🏖️ Se excluyeron {colaboradoresOmitidosVacSubsidio.Count} colaborador(es) por estar en Vacaciones o Subsidio en la fecha {fechaRefCarga:dd/MM/yyyy}:\n{string.Join("; ", colaboradoresOmitidosVacSubsidio)}", Severity.Info, config => config.VisibleStateDuration = 12000);
                }

                if (colaboradoresOmitidosPrestamo.Any())
                {
                    _snackbar.Add($"ℹ️ Se omitieron {colaboradoresOmitidosPrestamo.Count} colaborador(es) porque ya están en otro cliente como préstamo o turno previo:\n{string.Join("; ", colaboradoresOmitidosPrestamo)}.\nSi requiere agregarlo en {cliente}, hágalo manualmente con '+ Colaborador'.", Severity.Warning, config => config.VisibleStateDuration = 12000);
                }

                ActualizarGruposConsolidados();
                if (agregados > 0)
                {
                    _snackbar.Add($"Se cargaron {agregados} colaboradores de {cliente} a la jornada.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al cargar colaboradores de {cliente}: {ex.Message}", Severity.Error);
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
                activeTabIndex = 5; // Cambiar a pestaña Mi Personal (Índice 5)
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

            cargandoPlantilla = true;
            textoOperacionBd = "Cargando colaboradores de mi plantilla a la jornada...";
            StateHasChanged();

            var existentes = empleadosJornada.Select(x => x.CodigoEmpleado).ToHashSet();
            int agregados = 0;

            // 1. Consultar colaboradores que ya tienen turno/horario registrado o Vacaciones/Subsidios activos para esta fecha
            DateTime fechaRefPlantilla = (fechaSeleccionada ?? DateTime.Today).Date;
            Dictionary<int, Modelo.ClasesGenericas.MovimientoPersonalDiarioDto> turnosOcupados = new();
            List<Modelo.ClasesGenericas.MovimientoPersonalDiarioDto> listaTurnosYAusencias = new();
            try
            {
                var respTurnos = await _odooService.GetTurnosOcupadosFecha(fechaRefPlantilla);
                if (respTurnos?.Model != null && respTurnos.Model.Any())
                {
                    listaTurnosYAusencias = respTurnos.Model.ToList();
                    turnosOcupados = listaTurnosYAusencias
                        .Where(t => t.Asistio && t.CodigoEmpleado > 0 && !string.IsNullOrWhiteSpace(t.HoraEntrada) && t.HoraEntrada != "--:--")
                        .GroupBy(t => t.CodigoEmpleado)
                        .ToDictionary(g => g.Key, g => g.First());
                }
            }
            catch
            {
                // No interrumpir si la verificación preventiva no responde
            }

            var bloqueosVacSubAll = ConstruirBloqueosVacacionesSubsidios(listaTurnosYAusencias, fechaRefPlantilla);
            var colaboradoresOmitidos = new List<string>();
            var colaboradoresOmitidosVacSub = new List<string>();

            foreach (var item in miPlantillaPersonal)
            {
                if (!existentes.Contains(item.CodigoEmpleado))
                {
                    if (EstaEnVacacionesOSubsidioActivo(item.CodigoEmpleado, item.IdEmpleado, bloqueosVacSubAll, out string motivoVacSub))
                    {
                        colaboradoresOmitidosVacSub.Add($"#{item.CodigoEmpleado} {item.NombreEmpleado} ({motivoVacSub})");
                        continue;
                    }

                    // REGLA DE NEGOCIO: Si el colaborador YA tiene un turno registrado en esta fecha (en otro cliente o supervisor), NO cargarlo
                    if (turnosOcupados.TryGetValue(item.CodigoEmpleado, out var turnoExistente))
                    {
                        string clienteItem = !string.IsNullOrWhiteSpace(item.ClienteDefault) && !string.Equals(item.ClienteDefault, "Administración", StringComparison.OrdinalIgnoreCase)
                            ? item.ClienteDefault 
                            : (NombresMisClientes.FirstOrDefault() ?? "General");

                        bool esOtroCliente = !string.IsNullOrWhiteSpace(turnoExistente.Cliente) && 
                            !string.Equals(turnoExistente.Cliente.Trim(), clienteItem.Trim(), StringComparison.OrdinalIgnoreCase);

                        if (esOtroCliente || forzarCarga)
                        {
                            colaboradoresOmitidos.Add($"{item.NombreEmpleado} (#{item.CodigoEmpleado} - {turnoExistente.Cliente} {turnoExistente.HoraEntrada} a {turnoExistente.HoraSalida})");
                            continue;
                        }
                    }
                    string clienteDefault = !string.IsNullOrWhiteSpace(item.ClienteDefault) && !string.Equals(item.ClienteDefault, "Administración", StringComparison.OrdinalIgnoreCase)
                        ? item.ClienteDefault 
                        : (NombresMisClientes.FirstOrDefault() ?? operacionesList.FirstOrDefault(o => !string.Equals(o.nombre, "Administración", StringComparison.OrdinalIgnoreCase))?.nombre ?? "General");

                    string obsPlantilla = string.Empty;
                    if (string.Equals(item.TipoEmpleado, "Prestamo", StringComparison.OrdinalIgnoreCase) || string.Equals(item.TipoEmpleado, "Temporal", StringComparison.OrdinalIgnoreCase))
                    {
                        string orig = !string.IsNullOrWhiteSpace(item.ClienteOrigenNomina) ? item.ClienteOrigenNomina : item.ClienteDefault;
                        obsPlantilla = $"{item.TipoEmpleado} (Nómina: {orig})";
                    }

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
                        ClienteOrigenNomina = LimpiarTextoOdoo(item.ClienteOrigenNomina),
                        Observaciones = obsPlantilla,
                        Asistio = true,
                        Estado = "BORRADOR",
                        FechaInicioAusencia = fechaSeleccionada ?? DateTime.Today,
                        FechaFinAusencia = fechaSeleccionada ?? DateTime.Today,
                        DiasAusencia = 1.0m,
                        Modificado = true
                    });
                    var nuevoEmp = empleadosJornada.Last();
                    AplicarReglasViaticosEnMemoria(nuevoEmp);
                    agregados++;
                }
            }

            if (colaboradoresOmitidosVacSub.Any())
            {
                _snackbar.Add($"🏖️ Se excluyeron {colaboradoresOmitidosVacSub.Count} colaborador(es) en Vacaciones o Subsidio ({fechaRefPlantilla:dd/MM/yyyy}): {string.Join("; ", colaboradoresOmitidosVacSub)}", Severity.Info, config => config.VisibleStateDuration = 12000);
            }

            if (colaboradoresOmitidos.Any())
            {
                _snackbar.Add($"ℹ️ Se omitieron {colaboradoresOmitidos.Count} colaborador(es) porque ya tienen turno registrado en otro cliente para esta fecha: {string.Join(", ", colaboradoresOmitidos)}", Severity.Warning, config => config.VisibleStateDuration = 10000);
            }

            ActualizarGruposConsolidados();
            cargandoPlantilla = false;
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
            textoOperacionBd = "Guardando colaboradores en tu plantilla personal...";
            try
            {
                var items = empleadosJornada
                    .GroupBy(e => e.CodigoEmpleado > 0 ? $"cod_{e.CodigoEmpleado}" : (e.IdEmpleado > 0 ? $"id_{e.IdEmpleado}" : e.NombreEmpleado.Trim().ToLowerInvariant()))
                    .Select(g => { var p = g.First(); p.ClienteOrigenNomina = LimpiarTextoOdoo(p.ClienteOrigenNomina); return p; })
                    .Select(e => new SupervisorPlantillaItemDto
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
            estaGuardando = true;
            textoOperacionBd = $"Guardando configuración de #{item.CodigoEmpleado} {item.NombreEmpleado}...";
            try
            {
                item.UsuarioSupervisor = currentUserName;
                var resp = await _odooService.SaveSupervisorPlantillaItem(item);

                if (resp?.Respuesta?.ExisteError == false)
                {
                    _snackbar.Add($"✅ Guardado en Mi Personal: #{item.CodigoEmpleado} {item.NombreEmpleado} ({item.ClienteDefault} - {item.AreaDefault}).", Severity.Success);
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
            finally
            {
                estaGuardando = false;
                StateHasChanged();
            }
        }

        protected async Task AbrirDialogoEditarAsistencia(EmpleadoJornadaItem item)
        {
            var clientesDisponibles = ListaClientesAsistencia
                .Union(NombresMisClientes)
                .Union(operacionesList.Select(o => o.nombre))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            var (regSinsa, nombreCliConciliacion) = ObtenerRegistroConciliacionEmpleado(item);

            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            decimal? heActual = null;
            string? iniExtraActual = null;
            string? finExtraActual = null;
            var manItem = itemsHorasExtrasManuales.FirstOrDefault(m => m.Fecha.Date == fRef && m.Dto.CodigoEmpleado == item.CodigoEmpleado);
            if (manItem.Dto != null)
            {
                heActual = manItem.Dto.HorasExtrasAutorizadas;
                iniExtraActual = manItem.Dto.HoraInicioExtra;
                finExtraActual = manItem.Dto.HoraFinExtra;
            }
            else if (empleadosSinHorasExtrasConciliadosSinsa.ContainsKey(item.CodigoEmpleado))
            {
                heActual = 0m;
            }
            else if (item.Asistio)
            {
                var calcPrev = CalcularHorasExtrasSegunRegla(item, fRef);
                if (calcPrev.HorasExtras > 0m)
                {
                    heActual = calcPrev.HorasExtras;
                    iniExtraActual = calcPrev.HoraInicioExtra;
                    finExtraActual = calcPrev.HoraFinExtra;
                }
            }

            var parameters = new DialogParameters
            {
                ["ItemOriginal"] = item,
                ["OperacionesDisponibles"] = operacionesList,
                ["ClientesSugeridos"] = clientesDisponibles,
                ["AreasPorCliente"] = areasPorCliente,
                ["CargosDisponibles"] = cargosDisponibles,
                ["ClientesAsignadosSupervisor"] = NombresMisClientes,
                ["NombreClienteConciliacion"] = nombreCliConciliacion,
                ["RegistroSinsa"] = regSinsa,
                ["HorasExtrasActuales"] = heActual,
                ["HoraInicioExtraActual"] = iniExtraActual,
                ["HoraFinExtraActual"] = finExtraActual
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
            var dialog = await _dialogService.ShowAsync<DialogEditarColaboradorAsistencia>("Editar Asistencia de Colaborador", parameters, options);
            var result = await dialog.Result;

            EmpleadoJornadaItem? modificado = null;
            DialogEditarColaboradorAsistencia.ResultadoEdicionAsistencia? resCompleta = null;
            if (result != null && !result.Canceled)
            {
                if (result.Data is DialogEditarColaboradorAsistencia.ResultadoEdicionAsistencia rEd)
                {
                    resCompleta = rEd;
                    modificado = rEd.EmpleadoModificado;
                }
                else if (result.Data is EmpleadoJornadaItem eDirecto)
                {
                    modificado = eDirecto;
                }
            }

            if (modificado != null)
            {
                string clienteAnterior = item.Cliente;
                string nuevoCliente = modificado.Cliente;

                item.Cliente = nuevoCliente;
                item.Area = modificado.Area;
                item.Puesto = modificado.Puesto;
                item.TipoEmpleado = modificado.TipoEmpleado;
                item.ClienteOrigenNomina = modificado.ClienteOrigenNomina;
                item.Asistio = modificado.Asistio;
                item.HoraEntrada = modificado.HoraEntrada;
                item.HoraSalida = modificado.HoraSalida;
                item.TiempoComida = modificado.TiempoComida;
                item.VtcoTrasp = modificado.VtcoTrasp;
                item.VtcoAlim = modificado.VtcoAlim;
                item.BonoBnc = modificado.BonoBnc;
                item.Bnc = modificado.Bnc;
                item.Observaciones = modificado.Observaciones;
                item.TurnoNombre = modificado.TurnoNombre;
                item.Modificado = true;

                AsegurarAreaEnCatalogoMemoria(nuevoCliente, item.Area);

                if (!string.Equals(clienteAnterior, nuevoCliente, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(item.ClienteOrigenNomina))
                    {
                        item.ClienteOrigenNomina = clienteAnterior;
                    }
                    if (string.Equals(item.TipoEmpleado, "Permanente", StringComparison.OrdinalIgnoreCase))
                    {
                        item.TipoEmpleado = "Prestamo";
                    }
                }

                if (resCompleta != null && resCompleta.AplicarCuadreHorasExtras)
                {
                    AplicarCuadreHorasExtrasColaborador(
                        item,
                        resCompleta.HorasExtrasAutorizadas,
                        resCompleta.HoraInicioExtra,
                        resCompleta.HoraFinExtra);
                }
                else if (!item.Asistio)
                {
                    itemsHorasExtrasManuales.RemoveAll(m => m.Fecha.Date == fRef && m.Dto.CodigoEmpleado == item.CodigoEmpleado);
                    edicionesHorarioHePorEmpleado.Remove(item.CodigoEmpleado);
                    empleadosSinHorasExtrasConciliadosSinsa.Remove(item.CodigoEmpleado);
                }

                AplicarReglasViaticosEnMemoria(item);
                ValidarConflictosHorarios(out _);
                ActualizarGruposConsolidadosInterno(false);
                ActualizarGruposHorasExtrasInterno();
                await AutoGuardarBorradorLocal();

                if (!string.Equals(clienteAnterior, nuevoCliente, StringComparison.OrdinalIgnoreCase))
                {
                    _snackbar.Add($"Colaborador {item.NombreEmpleado} asignado a {nuevoCliente} (Préstamo).", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Asistencia de {item.NombreEmpleado} actualizada.", Severity.Success);
                }

                StateHasChanged();
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
                item.ClienteOrigenNomina = LimpiarTextoOdoo(modificado.ClienteOrigenNomina);

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

            estaEliminando = true;
            textoOperacionBd = $"Eliminando a {item.NombreEmpleado} de la plantilla en base de datos...";
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
            finally
            {
                estaEliminando = false;
                StateHasChanged();
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
            textoOperacionBd = "Copiando jornada del día anterior...";
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
                    await CargarJornadaActual();
                    if (empleadosJornada.Any())
                    {
                        _snackbar.Add($"Jornada copiada exitosamente de {fechaAyer:dd/MM} ({empleadosJornada.Count} colaboradores).", Severity.Success);
                        foreach (var emp in empleadosJornada)
                        {
                            AplicarReglasViaticosEnMemoria(emp);
                        }
                        ActualizarGruposConsolidados();
                    }
                    else
                    {
                        _snackbar.Add($"No se encontraron registros activos del {fechaAyer:dd/MM} para copiar.", Severity.Info);
                    }
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

            empleadosJornada = DeduplicarEmpleadosJornada(empleadosJornada);

            if (!ValidarConflictosHorarios(out var errorCruce))
            {
                _snackbar.Add($"❌ No se puede guardar: se detectaron cruces de horarios entre turnos del mismo colaborador:\n{errorCruce}", Severity.Error, config => config.VisibleStateDuration = 10000);
                return;
            }

            estaGuardando = true;
            textoOperacionBd = "Guardando jornada y asistencias en base de datos...";
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
                        e.TicketRedmineId = null;
                        e.TicketViaticoId = null;
                        e.TicketBonoId = null;
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
                        Bnc = !e.Asistio ? "0" : e.Bnc,
                        VtcoTrasp = !e.Asistio ? 0m : e.VtcoTrasp,
                        VtcoAlim = !e.Asistio ? 0m : e.VtcoAlim,
                        TraspAplicaNomina = e.Asistio && e.TraspAplicaNomina,
                        AlimAplicaNomina = e.Asistio && e.AlimAplicaNomina,
                        Observaciones = e.Observaciones,
                        Asistio = e.Asistio,
                        Estado = e.Estado == "SINCRONIZADO" ? "MODIFICADO" : "GUARDADO",
                        TicketRedmineId = !e.Asistio ? null : e.TicketRedmineId,
                        TicketViaticoId = !e.Asistio ? null : e.TicketViaticoId,
                        TicketBonoId = !e.Asistio ? null : e.TicketBonoId,
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
                    // 1. Conservar los datos en pantalla y marcar como guardados sin refrescar destructivamente
                    foreach (var emp in empleadosJornada)
                    {
                        emp.Modificado = false;
                        if (emp.Estado == "BORRADOR" || emp.Estado == "MODIFICADO" || string.IsNullOrWhiteSpace(emp.Estado))
                        {
                            emp.Estado = "GUARDADO";
                        }
                    }
                    _snackbar.Add($"✅ {dtos.Count} movimientos guardados exitosamente en la base de datos.", Severity.Success);

                    // 2. En segundo plano (sin vaciar la grilla ni mostrar spinner destructivo), sincronizar IDs asignados por BD
                    try
                    {
                        var respBd = await _odooService.GetMovimientosPersonalDiario(fechaSeleccionada.Value, 0, currentUserName);
                        if (respBd?.Model != null && respBd.Model.Any())
                        {
                            foreach (var itemBd in respBd.Model)
                            {
                                var local = empleadosJornada.FirstOrDefault(e => 
                                    (e.Id > 0 && e.Id == itemBd.Id) || 
                                    (e.Id <= 0 && e.CodigoEmpleado == itemBd.CodigoEmpleado 
                                               && string.Equals(NormalizarFormatoHora(e.HoraEntrada), NormalizarFormatoHora(itemBd.HoraEntrada), StringComparison.OrdinalIgnoreCase)
                                               && string.Equals(NormalizarFormatoHora(e.HoraSalida), NormalizarFormatoHora(itemBd.HoraSalida), StringComparison.OrdinalIgnoreCase)) ||
                                    (e.Id <= 0 && e.CodigoEmpleado == itemBd.CodigoEmpleado 
                                               && string.Equals(e.Area?.Trim(), itemBd.Area?.Trim(), StringComparison.OrdinalIgnoreCase)
                                               && string.Equals(NormalizarFormatoHora(e.HoraEntrada), NormalizarFormatoHora(itemBd.HoraEntrada), StringComparison.OrdinalIgnoreCase)) ||
                                    (e.Id <= 0 && e.CodigoEmpleado == itemBd.CodigoEmpleado && !empleadosJornada.Any(other => other != e && other.Id == itemBd.Id)));
                                if (local != null)
                                {
                                    local.Id = itemBd.Id;
                                    local.Modificado = false;
                                    local.Estado = "GUARDADO";
                                    if (itemBd.TicketRedmineId.HasValue && itemBd.TicketRedmineId > 0) local.TicketRedmineId = itemBd.TicketRedmineId;
                                    if (itemBd.TicketViaticoId.HasValue && itemBd.TicketViaticoId > 0) local.TicketViaticoId = itemBd.TicketViaticoId;
                                    if (itemBd.TicketBonoId.HasValue && itemBd.TicketBonoId > 0) local.TicketBonoId = itemBd.TicketBonoId;
                                    if (itemBd.TicketAusenciaId.HasValue && itemBd.TicketAusenciaId > 0) local.TicketAusenciaId = itemBd.TicketAusenciaId;
                                }
                            }
                            empleadosJornada = DeduplicarEmpleadosJornada(empleadosJornada);
                        }
                    }
                    catch
                    {
                        // No interrumpir si la sincronización de IDs tarda
                    }

                    await AutoGuardarBorradorLocal();
                    ValidarConflictosHorarios(out _);
                    ActualizarGruposConsolidados();
                    StateHasChanged();
                }
                else
                {
                    string errorMsg = res?.Respuesta?.MensajeError ?? "Error al guardar en base de datos.";
                    if (errorMsg.Contains("Cruce de horario para"))
                    {
                        int idx = errorMsg.IndexOf("Cruce de horario para");
                        errorMsg = errorMsg.Substring(idx);
                        int nl = errorMsg.IndexOf('\n');
                        if (nl > 0) errorMsg = errorMsg.Substring(0, nl);
                    }
                    _snackbar.Add($"❌ {errorMsg}", Severity.Error, config => config.VisibleStateDuration = 12000);
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
                    if (data.VtcoTrasp == 0 && data.VtcoAlim == 0 && data.BonoBnc == 0)
                    {
                        AplicarReglasViaticosEnMemoria(emp);
                    }
                    emp.Asistio = true;
                    emp.Modificado = true;
                    emp.Seleccionado = false;
                }

                _snackbar.Add($"Asignado turno, área y viáticos a {seleccionados.Count} colaboradores.", Severity.Success);
                ActualizarGruposConsolidados();
            }
        }

        // --- AGREGAR COLABORADOR MANUAL O DESDE ODOO ---
        protected async Task AbrirDialogoAgregarColaborador(string textoInicial = "", bool esDesdePlantilla = false)
        {
            // Determinar la operación por defecto dando prioridad a la selección actual o a los clientes asignados del supervisor
            string defaultOp = "";
            if (selectedOperacionId.HasValue && selectedOperacionId.Value > 0)
            {
                defaultOp = operacionesList.FirstOrDefault(o => o.id == selectedOperacionId.Value)?.nombre ?? "";
            }

            if (esDesdePlantilla && !string.IsNullOrWhiteSpace(ClienteActivoPlantillaSeguro))
            {
                defaultOp = ClienteActivoPlantillaSeguro;
            }
            else if (!esDesdePlantilla && !string.IsNullOrWhiteSpace(ClienteActivoAsistenciasSeguro))
            {
                defaultOp = ClienteActivoAsistenciasSeguro;
            }
            else if (!string.IsNullOrWhiteSpace(filtroClienteGrilla)) 
            { 
                defaultOp = filtroClienteGrilla; 
            } 
            else if (string.IsNullOrWhiteSpace(defaultOp) || string.Equals(defaultOp, "Administración", StringComparison.OrdinalIgnoreCase))
            {
                if (NombresMisClientes.Any())
                {
                    defaultOp = NombresMisClientes.FirstOrDefault() ?? "";
                }
                else
                {
                    defaultOp = operacionesList.FirstOrDefault(o => !string.Equals(o.nombre, "Administración", StringComparison.OrdinalIgnoreCase))?.nombre ?? "Sinsa";
                }
            }

            var defaultAreas = GetAreasParaCliente(defaultOp);

            // Operaciones disponibles: excluir Administración y ordenar poniendo al inicio los clientes asignados del supervisor
            var opsDisponibles = operacionesList
                .Where(o => !string.Equals(o.nombre, "Administración", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => NombresMisClientes.Contains(o.nombre, StringComparer.OrdinalIgnoreCase))
                .ThenBy(o => o.nombre)
                .ToList();

            if (!opsDisponibles.Any())
            {
                opsDisponibles = operacionesList;
            }

            var parameters = new DialogParameters
            {
                ["OperacionesDisponibles"] = opsDisponibles,
                ["TodasOperaciones"] = operacionesList,
                ["AreasDisponibles"] = defaultAreas,
                ["AreasPorCliente"] = areasPorCliente,
                ["CargosDisponibles"] = cargosDisponibles,
                ["OperacionDefecto"] = defaultOp,
                ["AreaDefecto"] = defaultAreas.FirstOrDefault() ?? "Operaciones",
                ["TextoBusquedaInicial"] = textoInicial,
                ["PlantillaExistente"] = miPlantillaPersonal,
                ["JornadaExistente"] = empleadosJornada,
                ["EsModoPlantilla"] = esDesdePlantilla
            };

            string tituloDialogo = esDesdePlantilla ? "Agregar a Mi Personal" : "Agregar Colaborador a la Jornada";
            var dialog = await _dialogService.ShowAsync<DialogAgregarColaboradorJornada>(tituloDialogo, parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is EmpleadoJornadaItem nuevo)
            {
                nuevo.ClienteOrigenNomina = LimpiarTextoOdoo(nuevo.ClienteOrigenNomina);
                // Asegurar que si el cliente sigue como "Administración" y el supervisor tiene cliente asignado, se asigne a su cliente
                if (string.Equals(nuevo.Cliente, "Administración", StringComparison.OrdinalIgnoreCase) && NombresMisClientes.Any())
                {
                    nuevo.Cliente = NombresMisClientes.FirstOrDefault() ?? nuevo.Cliente;
                }

                // SI SE ABRIÓ DESDE "MI PERSONAL" (TAB 4):
                if (esDesdePlantilla)
                {
                    bool yaExisteEnPlantilla = miPlantillaPersonal.Any(p => 
                        (nuevo.CodigoEmpleado > 0 && p.CodigoEmpleado == nuevo.CodigoEmpleado) ||
                        (nuevo.IdEmpleado > 0 && p.IdEmpleado == nuevo.IdEmpleado) ||
                        (!string.IsNullOrWhiteSpace(nuevo.NombreEmpleado) && string.Equals(p.NombreEmpleado.Trim(), nuevo.NombreEmpleado.Trim(), StringComparison.OrdinalIgnoreCase)));

                    if (yaExisteEnPlantilla)
                    {
                        _snackbar.Add($"⚠️ El colaborador #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado} ya existe en tu personal.", Severity.Warning);
                        return;
                    }

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
                        ClienteOrigenNomina = nuevo.ClienteOrigenNomina,
                        Activo = true
                    };
                    await _odooService.SaveSupervisorPlantillaItem(itemP);
                    miPlantillaPersonal.Add(itemP);

                    if (string.Equals(nuevo.TipoEmpleado, "Permanente", StringComparison.OrdinalIgnoreCase))
                    {
                        int opId = operacionesList.FirstOrDefault(o => o.nombre.Equals(nuevo.Cliente, StringComparison.OrdinalIgnoreCase))?.id ?? (selectedOperacionId ?? 0);
                        var itemBase = new PersonalClienteItemDto
                        {
                            OperacionId = opId,
                            ClienteNombre = nuevo.Cliente,
                            UsuarioSupervisor = currentUserName,
                            IdEmpleado = nuevo.IdEmpleado,
                            CodigoEmpleado = nuevo.CodigoEmpleado,
                            NombreEmpleado = nuevo.NombreEmpleado,
                            AreaDefault = nuevo.Area,
                            PuestoDefault = nuevo.Puesto,
                            TurnoDefault = nuevo.TurnoNombre,
                            HoraEntradaDefault = nuevo.HoraEntrada,
                            HoraSalidaDefault = nuevo.HoraSalida,
                            TiempoComidaDefault = nuevo.TiempoComida,
                            VtcoTraspDefault = nuevo.VtcoTrasp,
                            VtcoAlimDefault = nuevo.VtcoAlim,
                            BonoBncDefault = nuevo.BonoBnc,
                            TipoEmpleado = "Permanente",
                            Activo = true,
                            UsuarioCreacion = currentUserName
                        };
                        await _odooService.SavePersonalClienteItem(itemBase);
                    }

                    _snackbar.Add($"✅ #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado} guardado exitosamente en Mi Personal.", Severity.Success);
                    StateHasChanged();
                    return;
                }

                bool duplicadoMismoHorario = empleadosJornada.Any(e =>
                    ((nuevo.CodigoEmpleado > 0 && e.CodigoEmpleado == nuevo.CodigoEmpleado) ||
                     (nuevo.CodigoEmpleado <= 0 && string.Equals(e.NombreEmpleado?.Trim(), nuevo.NombreEmpleado?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    && ((!e.Asistio && !nuevo.Asistio) ||
                        (e.Asistio && nuevo.Asistio
                         && string.Equals(NormalizarFormatoHora(e.HoraEntrada), NormalizarFormatoHora(nuevo.HoraEntrada), StringComparison.OrdinalIgnoreCase)
                         && string.Equals(NormalizarFormatoHora(e.HoraSalida), NormalizarFormatoHora(nuevo.HoraSalida), StringComparison.OrdinalIgnoreCase))));

                if (duplicadoMismoHorario)
                {
                    _snackbar.Add($"⚠️ El colaborador #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado} ya está registrado en la jornada con el mismo horario ({nuevo.HoraEntrada} - {nuevo.HoraSalida}).", Severity.Warning);
                    return;
                }

                var existente = empleadosJornada.FirstOrDefault(e => e.CodigoEmpleado == nuevo.CodigoEmpleado);
                if (existente != null)
                {
                    _snackbar.Add($"ℹ️ Registro adicional (Turno {nuevo.TurnoNombre}) agregado para #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado}.", Severity.Info);
                }
                else
                {
                    _snackbar.Add($"✅ #{nuevo.CodigoEmpleado} {nuevo.NombreEmpleado} agregado a la jornada para '{nuevo.Cliente}'.", Severity.Success);
                }

                if (nuevo.VtcoTrasp == 0 && nuevo.VtcoAlim == 0 && nuevo.BonoBnc == 0)
                {
                    AplicarReglasViaticosEnMemoria(nuevo);
                }
                nuevo.Modificado = true;
                empleadosJornada.Insert(0, nuevo);
                ValidarConflictosHorarios(out var msgConflicto);
                if (!string.IsNullOrWhiteSpace(msgConflicto))
                {
                    _snackbar.Add($"⚠️ Advertencia de cruce de horario:\n{msgConflicto}", Severity.Warning, config => config.VisibleStateDuration = 8000);
                }
                ActualizarGruposConsolidados();

                // Guardar también en la plantilla del supervisor si aún no estaba o actualizar datos (garantizando unicidad)
                var itemPlantilla = miPlantillaPersonal.FirstOrDefault(p => 
                    (nuevo.CodigoEmpleado > 0 && p.CodigoEmpleado == nuevo.CodigoEmpleado) ||
                    (nuevo.IdEmpleado > 0 && p.IdEmpleado == nuevo.IdEmpleado) ||
                    (!string.IsNullOrWhiteSpace(nuevo.NombreEmpleado) && string.Equals(p.NombreEmpleado.Trim(), nuevo.NombreEmpleado.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (itemPlantilla == null)
                {
                    itemPlantilla = new SupervisorPlantillaItemDto
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
                        ClienteOrigenNomina = nuevo.ClienteOrigenNomina,
                        Activo = true
                    };
                    await _odooService.SaveSupervisorPlantillaItem(itemPlantilla);
                    miPlantillaPersonal.Add(itemPlantilla);
                }
                else
                {
                    // No sobreescribir el horario ordinario base de Mi Personal con las horas de un día puntual
                    itemPlantilla.ClienteDefault = nuevo.Cliente;
                    itemPlantilla.AreaDefault = nuevo.Area;
                    itemPlantilla.PuestoDefault = nuevo.Puesto;
                    itemPlantilla.TipoEmpleado = nuevo.TipoEmpleado;
                    itemPlantilla.ClienteOrigenNomina = nuevo.ClienteOrigenNomina;
                    await _odooService.SaveSupervisorPlantillaItem(itemPlantilla);
                }

                ActualizarGruposConsolidados();
                StateHasChanged();
            }
        }

        // --- LIMPIAR GRILLA COMPLETA ---
        protected async Task LimpiarGrillaCompleta()
        {
            if (!empleadosJornada.Any()) return;

            // Verificar si hay registros guardados localmente o en la base de datos para esta fecha
            bool tieneRegistrosGuardados = empleadosJornada.Any(x => x.Id > 0);
            List<int> idsEnBd = new();

            if (fechaSeleccionada.HasValue)
            {
                try
                {
                    var respBd = await _odooService.GetMovimientosPersonalDiario(fechaSeleccionada.Value, 0, currentUserName);
                    if (respBd?.Model != null && respBd.Model.Any())
                    {
                        idsEnBd = respBd.Model.Select(m => m.Id).Where(id => id > 0).Distinct().ToList();
                        if (idsEnBd.Any()) tieneRegistrosGuardados = true;
                    }
                }
                catch { }
            }

            if (tieneRegistrosGuardados)
            {
                int totalRegistrosBd = Math.Max(idsEnBd.Count, empleadosJornada.Count(x => x.Id > 0));
                bool? confirm = await _dialogService.ShowMessageBox(
                    "Limpiar Asistencia Diaria",
                    $"La jornada del día {fechaSeleccionada:dd/MM/yyyy} tiene {totalRegistrosBd} asistencia(s) registrada(s) en la base de datos.\n\n" +
                    "• 'Limpiar Pantalla': Quita los registros de la vista temporal en tu pantalla sin borrarlos de la BD ni de 'Mi Personal'.\n" +
                    "• 'Borrar de BD': Elimina y anula permanentemente las asistencias guardadas en la base de datos para esta fecha.",
                    yesText: "Limpiar Pantalla (Conservar en BD)",
                    noText: "Borrar de BD Permanentemente",
                    cancelText: "Cancelar");

                if (confirm == null) return;

                estaLimpiando = true;
                textoOperacionBd = confirm == false ? "Borrando asistencias de la base de datos..." : "Limpiando grilla de pantalla...";
                try
                {
                    if (confirm == false) // El usuario eligió expresamente borrar de BD
                    {
                        var idsParaBorrar = empleadosJornada.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
                        foreach (var id in idsEnBd) idsParaBorrar.Add(id);

                        if (idsParaBorrar.Any())
                        {
                            var tasks = idsParaBorrar.Select(id => _odooService.DeleteMovimientoPersonalDiario(id));
                            await Task.WhenAll(tasks);
                        }
                        _snackbar.Add($"Se anularon y borraron {idsParaBorrar.Count} asistencias de la base de datos para el día {fechaSeleccionada:dd/MM/yyyy}.", Severity.Warning);
                    }
                    else
                    {
                        _snackbar.Add("Grilla limpiada de la pantalla. Las asistencias siguen guardadas en la base de datos y en 'Mi Personal'.", Severity.Info);
                    }

                    empleadosJornada.Clear();
                    await LimpiarBorradorLocal();
                    ActualizarGruposConsolidados();
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error al procesar limpieza: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaLimpiando = false;
                    estaCargando = false;
                }
            }
            else
            {
                bool? confirm = await _dialogService.ShowMessageBox(
                    "Limpiar Grilla Diaria",
                    $"¿Deseas limpiar los {empleadosJornada.Count} colaboradores de la grilla? (Tu plantilla 'Mi Personal' permanece intacta).",
                    yesText: "Sí, Limpiar Pantalla",
                    cancelText: "Cancelar");

                if (confirm == true)
                {
                    empleadosJornada.Clear();
                    await LimpiarBorradorLocal();
                    ActualizarGruposConsolidados();
                    _snackbar.Add("Grilla limpiada exitosamente.", Severity.Info);
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
                    "#", "ID BD", "Fecha", "Carnet / Código", "Colaborador", "Cliente / Operación", "Asistencia",
                    "Área Operativa", "Cargo / Puesto", "Turno", "Hora Entrada", "Hora Salida", "Tiempo Comida (Hrs)",
                    "Viático Transporte (C$)", "Viático Alimentación (C$)", "Bono BNC (C$)", "Total Viáticos (C$)",
                    "Tipo Ausencia", "Días Ausencia", "Observaciones", "Estado",
                    "Ticket Movimiento", "Ticket Viático", "Ticket Bono", "Ticket Ausencia", "Supervisor"
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
                    row.CreateCell(1).SetCellValue(emp.Id > 0 ? emp.Id.ToString() : "Sin Guardar");
                    row.CreateCell(2).SetCellValue(fechaSeleccionada?.ToString("yyyy-MM-dd") ?? "");
                    row.CreateCell(3).SetCellValue(emp.CodigoEmpleado);
                    row.CreateCell(4).SetCellValue(emp.NombreEmpleado);
                    row.CreateCell(5).SetCellValue(emp.Cliente);
                    row.CreateCell(6).SetCellValue(emp.Asistio ? "PRESENTE" : "AUSENTE");
                    row.CreateCell(7).SetCellValue(emp.Area);
                    row.CreateCell(8).SetCellValue(emp.Puesto);
                    row.CreateCell(9).SetCellValue(emp.TurnoNombre);
                    row.CreateCell(10).SetCellValue(emp.HoraEntrada);
                    row.CreateCell(11).SetCellValue(emp.HoraSalida);
                    row.CreateCell(12).SetCellValue(emp.TiempoComida);

                    var cTrasp = row.CreateCell(13);
                    cTrasp.SetCellValue((double)emp.VtcoTrasp);
                    cTrasp.CellStyle = currencyStyle;

                    var cAlim = row.CreateCell(14);
                    cAlim.SetCellValue((double)emp.VtcoAlim);
                    cAlim.CellStyle = currencyStyle;

                    var cBnc = row.CreateCell(15);
                    cBnc.SetCellValue((double)emp.BonoBnc);
                    cBnc.CellStyle = currencyStyle;

                    var cTotal = row.CreateCell(16);
                    cTotal.SetCellValue((double)emp.TotalBeneficios);
                    cTotal.CellStyle = currencyStyle;

                    row.CreateCell(17).SetCellValue(!emp.Asistio ? emp.TipoAusencia : "");
                    row.CreateCell(18).SetCellValue(!emp.Asistio ? (double)emp.DiasAusencia : 0);
                    row.CreateCell(19).SetCellValue(emp.Observaciones);
                    row.CreateCell(20).SetCellValue(emp.Estado);
                    row.CreateCell(21).SetCellValue(emp.TicketRedmineId.HasValue && emp.TicketRedmineId > 0 ? emp.TicketRedmineId.Value.ToString() : "");
                    row.CreateCell(22).SetCellValue(emp.TicketViaticoId.HasValue && emp.TicketViaticoId > 0 ? emp.TicketViaticoId.Value.ToString() : "");
                    row.CreateCell(23).SetCellValue(emp.TicketBonoId.HasValue && emp.TicketBonoId > 0 ? emp.TicketBonoId.Value.ToString() : "");
                    row.CreateCell(24).SetCellValue(emp.TicketAusenciaId.HasValue && emp.TicketAusenciaId > 0 ? emp.TicketAusenciaId.Value.ToString() : "");
                    row.CreateCell(25).SetCellValue(currentUserName);

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
                    24, // 16: Tipo Ausencia
                    14, // 17: Días Ausencia
                    32, // 18: Observaciones
                    14, // 19: Estado
                    18, // 20: Ticket Movimiento
                    18, // 21: Ticket Viático
                    18, // 22: Ticket Bono
                    18, // 23: Ticket Ausencia
                    26  // 24: Supervisor
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
                emp.TicketRedmineId = null;
                emp.TicketViaticoId = null;
                emp.TicketBonoId = null;
                DateTime fRef = fechaSeleccionada ?? DateTime.Today;
                emp.FechaInicioAusencia = fRef;
                emp.FechaFinAusencia = fRef;
                emp.DiasAusencia = 1.0m;
                emp.VtcoTrasp = 0m;
                emp.VtcoAlim = 0m;
                emp.TraspAplicaNomina = false;
                emp.AlimAplicaNomina = false;
                emp.BonoBnc = 0m;
                emp.Bnc = "0";
            }
            else
            {
                var plant = miPlantillaPersonal.FirstOrDefault(p => p.CodigoEmpleado == emp.CodigoEmpleado);
                emp.HoraEntrada = !string.IsNullOrWhiteSpace(plant?.HoraEntradaDefault) ? plant.HoraEntradaDefault : "06:00";
                emp.HoraSalida = !string.IsNullOrWhiteSpace(plant?.HoraSalidaDefault) ? plant.HoraSalidaDefault : "14:00";
                emp.TiempoComida = !string.IsNullOrWhiteSpace(plant?.TiempoComidaDefault) ? plant.TiempoComidaDefault : "1.00";
                emp.TurnoNombre = DeterminarTurnoSegunLeyNicaragua(emp.HoraEntrada, emp.HoraSalida);
                AplicarReglasViaticosEnMemoria(emp, notificar: false);
            }

            ValidarConflictosHorarios(out _);
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

        private static List<EmpleadoJornadaItem> DeduplicarEmpleadosJornada(IEnumerable<EmpleadoJornadaItem> origen)
        {
            if (origen == null) return new List<EmpleadoJornadaItem>();

            var resultado = new List<EmpleadoJornadaItem>();
            var grupos = origen.GroupBy(e =>
            {
                string empKey = e.CodigoEmpleado > 0
                    ? $"C:{e.CodigoEmpleado}"
                    : $"N:{(e.NombreEmpleado ?? string.Empty).Trim().ToLowerInvariant()}|{(e.Cliente ?? string.Empty).Trim().ToLowerInvariant()}";
                string hIn = !e.Asistio || string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) || e.HoraEntrada == "--:--"
                    ? "--:--"
                    : NormalizarFormatoHora(e.HoraEntrada);
                string hOut = !e.Asistio || string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) || e.HoraSalida == "--:--"
                    ? "--:--"
                    : NormalizarFormatoHora(e.HoraSalida);
                return $"{empKey}|{hIn}|{hOut}";
            });

            foreach (var g in grupos)
            {
                var principal = g
                    .OrderByDescending(x =>
                        (x.TicketRedmineId.HasValue && x.TicketRedmineId > 0 ? 1 : 0) +
                        (x.TicketViaticoId.HasValue && x.TicketViaticoId > 0 ? 1 : 0) +
                        (x.TicketAusenciaId.HasValue && x.TicketAusenciaId > 0 ? 1 : 0) +
                        (x.TicketBonoId.HasValue && x.TicketBonoId > 0 ? 1 : 0))
                    .ThenBy(x => x.Id > 0 ? 0 : 1)
                    .ThenBy(x => x.Id)
                    .First();

                foreach (var dup in g.Where(x => !ReferenceEquals(x, principal)))
                {
                    if ((!principal.TicketRedmineId.HasValue || principal.TicketRedmineId <= 0) && dup.TicketRedmineId.HasValue && dup.TicketRedmineId > 0)
                        principal.TicketRedmineId = dup.TicketRedmineId;
                    if ((!principal.TicketViaticoId.HasValue || principal.TicketViaticoId <= 0) && dup.TicketViaticoId.HasValue && dup.TicketViaticoId > 0)
                        principal.TicketViaticoId = dup.TicketViaticoId;
                    if ((!principal.TicketAusenciaId.HasValue || principal.TicketAusenciaId <= 0) && dup.TicketAusenciaId.HasValue && dup.TicketAusenciaId > 0)
                        principal.TicketAusenciaId = dup.TicketAusenciaId;
                    if ((!principal.TicketBonoId.HasValue || principal.TicketBonoId <= 0) && dup.TicketBonoId.HasValue && dup.TicketBonoId > 0)
                        principal.TicketBonoId = dup.TicketBonoId;
                }

                resultado.Add(principal);
            }

            return resultado;
        }

        protected bool ValidarConflictosHorarios(out string mensajeConflicto)
        {
            mensajeConflicto = string.Empty;

            foreach (var item in empleadosJornada)
            {
                item.TieneConflictoHorario = false;
                item.DetalleConflictoHorario = string.Empty;
            }

            if (!fechaSeleccionada.HasValue) return true;
            var f = fechaSeleccionada.Value.Date;

            var gruposPorEmpleado = empleadosJornada
                .Where(e => e.Asistio && !string.IsNullOrWhiteSpace(e.HoraEntrada) && !string.IsNullOrWhiteSpace(e.HoraSalida)
                            && e.HoraEntrada != "--:--" && e.HoraSalida != "--:--")
                .GroupBy(e => e.CodigoEmpleado);

            bool hayConflicto = false;
            var listaConflictos = new List<string>();

            foreach (var grupo in gruposPorEmpleado)
            {
                var items = grupo.ToList();
                if (items.Count <= 1) continue;

                var intervalos = new List<(EmpleadoJornadaItem item, DateTime inicio, DateTime fin)>();
                foreach (var it in items)
                {
                    if (TryParseHora(it.HoraEntrada, out var tIn) && TryParseHora(it.HoraSalida, out var tOut))
                    {
                        var dtStart = f + tIn;
                        var dtEnd = tOut <= tIn ? f.AddDays(1) + tOut : f + tOut;
                        intervalos.Add((it, dtStart, dtEnd));
                    }
                }

                for (int i = 0; i < intervalos.Count; i++)
                {
                    for (int j = i + 1; j < intervalos.Count; j++)
                    {
                        var (itemA, sA, eA) = intervalos[i];
                        var (itemB, sB, eB) = intervalos[j];

                        // Traslape de horarios
                        if (sA < eB && sB < eA)
                        {
                            hayConflicto = true;
                            itemA.TieneConflictoHorario = true;
                            itemA.DetalleConflictoHorario = $"Cruce de horario con turno de {itemB.HoraEntrada} a {itemB.HoraSalida} en {itemB.Cliente}";

                            itemB.TieneConflictoHorario = true;
                            itemB.DetalleConflictoHorario = $"Cruce de horario con turno de {itemA.HoraEntrada} a {itemA.HoraSalida} en {itemA.Cliente}";

                            string msg = $"{itemA.NombreEmpleado} (#{itemA.CodigoEmpleado}): Horario {itemA.HoraEntrada}-{itemA.HoraSalida} ({itemA.Cliente}) se cruza con {itemB.HoraEntrada}-{itemB.HoraSalida} ({itemB.Cliente})";
                            if (!listaConflictos.Contains(msg))
                            {
                                listaConflictos.Add(msg);
                            }
                        }
                    }
                }
            }

            if (hayConflicto)
            {
                mensajeConflicto = string.Join("; ", listaConflictos);
                return false;
            }

            return true;
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
            AplicarReglasViaticosEnMemoria(item);
            item.Modificado = true;
            ValidarConflictosHorarios(out _);
            ActualizarGruposConsolidados();
        }

        protected void OnHorarioSalidaCambiado(EmpleadoJornadaItem item, string? nuevaSalida)
        {
            if (!item.Asistio) return;
            if (string.IsNullOrWhiteSpace(nuevaSalida)) nuevaSalida = "14:00";
            item.HoraSalida = NormalizarFormatoHora(nuevaSalida);
            item.TurnoNombre = DeterminarTurnoSegunLeyNicaragua(item.HoraEntrada, item.HoraSalida);
            AplicarReglasViaticosEnMemoria(item);
            item.Modificado = true;
            ValidarConflictosHorarios(out _);
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
            AplicarReglasViaticosEnMemoria(item);
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

                        protected async Task AbrirEdicionMasivaHorarioComidaPlantilla()
        {
            if (!miPlantillaPersonal.Any())
            {
                _snackbar.Add("No hay colaboradores en tu plantilla personal.", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters
            {
                ["ListaPlantilla"] = miPlantillaPersonal,
                ["EsModoPlantilla"] = true
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await _dialogService.ShowAsync<DialogEdicionMasivaHorarioComida>("Edición Masiva de Horario y Comida (Mi Personal)", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEdicionMasivaHorarioComida.ResultadoEdicionMasivaHorario data)
            {
                var coinciden = miPlantillaPersonal.Where(x =>
                    (string.IsNullOrWhiteSpace(data.Cliente) || data.Cliente == "(Todos)" || x.ClienteDefault.Equals(data.Cliente, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(data.Area) || data.Area == "(Todas)" || x.AreaDefault.Equals(data.Area, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(data.Puesto) || data.Puesto == "(Todos)" || x.PuestoDefault.Equals(data.Puesto, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (!coinciden.Any())
                {
                    _snackbar.Add("No se encontraron colaboradores en tu personal que coincidan con la selección de Cliente/Área/Puesto.", Severity.Warning);
                    return;
                }

                string normEntrada = NormalizarFormatoHora(data.HoraEntrada);
                string normSalida = NormalizarFormatoHora(data.HoraSalida);
                string normComida = NormalizarDecimalComida(data.TiempoComida);
                string nuevoTurno = DeterminarTurnoSegunLeyNicaragua(normEntrada, normSalida);

                estaCargando = true;
                StateHasChanged();

                try
                {
                    int guardados = 0;
                    foreach (var item in coinciden)
                    {
                        item.HoraEntradaDefault = normEntrada;
                        item.HoraSalidaDefault = normSalida;
                        item.TiempoComidaDefault = normComida;
                        item.TurnoDefault = nuevoTurno;

                        await GuardarItemPlantilla(item);
                        guardados++;
                    }

                    _snackbar.Add($"✅ Horario ({normEntrada} - {normSalida}) y comida ({normComida}h) actualizados y guardados en base de datos para {guardados} colaborador(es) de Mi Personal.", Severity.Success);
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Error al actualizar en BD: {ex.Message}", Severity.Error);
                }
                finally
                {
                    estaCargando = false;
                    StateHasChanged();
                }
            }
        }

        protected async Task AbrirEdicionMasivaHorarioComida()
        {
            if (!empleadosJornada.Any())
            {
                _snackbar.Add("No hay colaboradores en la jornada actual.", Severity.Warning);
                return;
            }

            var parameters = new DialogParameters
            {
                ["ListaEmpleados"] = empleadosJornada
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await _dialogService.ShowAsync<DialogEdicionMasivaHorarioComida>("Edición Masiva de Horario y Comida", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEdicionMasivaHorarioComida.ResultadoEdicionMasivaHorario data)
            {
                var coinciden = empleadosJornada.Where(x =>
                    (!data.SoloPresentes || x.Asistio) &&
                    (string.IsNullOrWhiteSpace(data.Cliente) || data.Cliente == "(Todos)" || x.Cliente.Equals(data.Cliente, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(data.Area) || data.Area == "(Todas)" || x.Area.Equals(data.Area, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(data.Puesto) || data.Puesto == "(Todos)" || x.Puesto.Equals(data.Puesto, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (!coinciden.Any())
                {
                    _snackbar.Add("No se encontraron colaboradores que coincidan con la selección de Cliente/Área/Puesto.", Severity.Warning);
                    return;
                }

                string normEntrada = NormalizarFormatoHora(data.HoraEntrada);
                string normSalida = NormalizarFormatoHora(data.HoraSalida);
                string normComida = NormalizarDecimalComida(data.TiempoComida);

                foreach (var emp in coinciden)
                {
                    emp.HoraEntrada = normEntrada;
                    emp.HoraSalida = normSalida;
                    emp.TiempoComida = normComida;
                    emp.TurnoNombre = DeterminarTurnoSegunLeyNicaragua(emp.HoraEntrada, emp.HoraSalida);
                    AplicarReglasViaticosEnMemoria(emp);
                    emp.Modificado = true;
                }

                ValidarConflictosHorarios(out _);
                ActualizarGruposConsolidados();

                _snackbar.Add($"✅ Horario ({normEntrada} - {normSalida}) y comida ({normComida}h) aplicados exitosamente a {coinciden.Count} colaborador(es).", Severity.Success);
                StateHasChanged();
            }
        }

        public static string LimpiarTextoOdoo(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
                    var root = doc.RootElement;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        string[] preferredKeys = { "es_419", "es_NI", "es_ES", "es_MX", "en_US" };
                        foreach (var k in preferredKeys)
                        {
                            if (root.TryGetProperty(k, out var elem) && !string.IsNullOrWhiteSpace(elem.GetString()))
                                return elem.GetString()!.Trim();
                        }
                        foreach (var prop in root.EnumerateObject())
                        {
                            var val = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
                        }
                    }
                }
                catch { }
            }
            return trimmed;
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
                AplicarReglasViaticosEnMemoria(item);
                item.Modificado = true;

                if (data.AplicarASeleccionados && seleccionados.Any())
                {
                    foreach (var s in seleccionados)
                    {
                        s.TurnoNombre = data.TurnoNombre;
                        s.HoraEntrada = data.HoraEntrada;
                        s.HoraSalida = data.HoraSalida;
                        AplicarReglasViaticosEnMemoria(s);
                        s.Modificado = true;
                    }
                }

                ActualizarGruposConsolidados();
                _snackbar.Add($"Horario actualizado ({data.TurnoNombre} {data.HoraEntrada} - {data.HoraSalida})", Severity.Success);
            }
        }

        protected async Task AbrirEditorViaticoIndividual(EmpleadoJornadaItem item, bool ocultarBono = false)
        {
            var seleccionados = empleadosJornada.Where(x => x.Seleccionado).ToList();
            var parameters = new DialogParameters
            {
                ["CodigoEmpleado"] = item.CodigoEmpleado,
                ["NombreEmpleado"] = item.NombreEmpleado,
                ["VtcoTraspInicial"] = item.VtcoTrasp,
                ["VtcoAlimInicial"] = item.VtcoAlim,
                ["BonoBncInicial"] = item.BonoBnc,
                ["TraspAplicaNominaInicial"] = item.TraspAplicaNomina,
                ["AlimAplicaNominaInicial"] = item.AlimAplicaNomina,
                ["CantidadSeleccionados"] = seleccionados.Count,
                ["OcultarBono"] = ocultarBono
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarViaticos>(ocultarBono ? "Viáticos (Operaciones)" : "Viáticos & Bono", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEditarViaticos.ResultadoEdicionViaticos data)
            {
                item.VtcoTrasp = data.VtcoTrasp;
                item.VtcoAlim = data.VtcoAlim;
                item.TraspAplicaNomina = data.TraspAplicaNomina;
                item.AlimAplicaNomina = data.AlimAplicaNomina;
                if (!ocultarBono)
                {
                    item.BonoBnc = data.BonoBnc;
                }
                item.Modificado = true;

                if (data.AplicarASeleccionados && seleccionados.Any())
                {
                    foreach (var s in seleccionados)
                    {
                        s.VtcoTrasp = data.VtcoTrasp;
                        s.VtcoAlim = data.VtcoAlim;
                        s.TraspAplicaNomina = data.TraspAplicaNomina;
                        s.AlimAplicaNomina = data.AlimAplicaNomina;
                        if (!ocultarBono)
                        {
                            s.BonoBnc = data.BonoBnc;
                        }
                        s.Modificado = true;
                    }
                }

                ActualizarGruposConsolidados();
                var total = data.VtcoTrasp + data.VtcoAlim;
                _snackbar.Add($"Viáticos actualizados (Total Cliente C$ {total:N2} | Nómina C$ {data.BonoBnc:N2})", Severity.Success);
            }
        }

        protected async Task AbrirEditorViaticoGrupo(GrupoViaticoItem grupo)
        {
            var primer = grupo.Empleados.First();
            var parameters = new DialogParameters
            {
                ["CodigoEmpleado"] = primer.CodigoEmpleado,
                ["NombreEmpleado"] = $"{grupo.Recursos} recursos en {grupo.Area} ({grupo.HoraInicio}-{grupo.HoraFin})",
                ["VtcoTraspInicial"] = grupo.VtcoTrasp,
                ["VtcoAlimInicial"] = grupo.VtcoAlim,
                ["BonoBncInicial"] = primer.BonoBnc,
                ["TraspAplicaNominaInicial"] = primer.TraspAplicaNomina,
                ["AlimAplicaNominaInicial"] = primer.AlimAplicaNomina,
                ["CantidadSeleccionados"] = grupo.Recursos,
                ["OcultarBono"] = false
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarViaticos>("Editar Viáticos del Grupo", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEditarViaticos.ResultadoEdicionViaticos data)
            {
                foreach (var emp in grupo.Empleados)
                {
                    emp.VtcoTrasp = data.VtcoTrasp;
                    emp.VtcoAlim = data.VtcoAlim;
                    emp.TraspAplicaNomina = data.TraspAplicaNomina;
                    emp.AlimAplicaNomina = data.AlimAplicaNomina;
                    emp.BonoBnc = data.BonoBnc;
                    emp.Modificado = true;
                }

                ActualizarGruposConsolidados();
                var total = (data.VtcoTrasp + data.VtcoAlim) * grupo.Recursos;
                _snackbar.Add($"Viáticos actualizados para {grupo.Recursos} recursos (Total C$ {total:N2})", Severity.Success);
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
                ["TraspAplicaNominaInicial"] = primer.TraspAplicaNomina,
                ["AlimAplicaNominaInicial"] = primer.AlimAplicaNomina,
                ["CantidadSeleccionados"] = seleccionados.Count,
                ["OcultarBono"] = false
            };

            var dialog = await _dialogService.ShowAsync<DialogEditarViaticos>("Asignación Masiva de Viáticos", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is DialogEditarViaticos.ResultadoEdicionViaticos data)
            {
                foreach (var s in seleccionados)
                {
                    s.VtcoTrasp = data.VtcoTrasp;
                    s.VtcoAlim = data.VtcoAlim;
                    s.TraspAplicaNomina = data.TraspAplicaNomina;
                    s.AlimAplicaNomina = data.AlimAplicaNomina;
                    s.BonoBnc = data.BonoBnc;
                    s.Modificado = true;
                }

                ActualizarGruposConsolidados();
                var total = (data.VtcoTrasp + data.VtcoAlim) * seleccionados.Count;
                _snackbar.Add($"Viáticos asignados a {seleccionados.Count} colaboradores (Total C$ {total:N2})", Severity.Success);
            }
        }

        protected async Task LimpiarTodosLosViaticos()
        {
            bool? confirm = await _dialogService.ShowMessageBox(
                "Limpiar Viáticos",
                "¿Deseas restablecer a C$ 0 todos los viáticos de transporte y alimentación de la jornada?",
                yesText: "Sí, Limpiar a C$ 0", cancelText: "Cancelar");

            if (confirm == true)
            {
                foreach (var emp in empleadosJornada)
                {
                    if (emp.VtcoTrasp > 0 || emp.VtcoAlim > 0 || emp.BonoBnc > 0)
                    {
                        emp.VtcoTrasp = 0;
                        emp.VtcoAlim = 0;
                        emp.TraspAplicaNomina = false;
                        emp.AlimAplicaNomina = false;
                        emp.BonoBnc = 0;
                        emp.Modificado = true;
                    }
                }
                ActualizarGruposConsolidados();
                _snackbar.Add("Se han restablecido los viáticos (transporte y alimentación) a C$ 0.", Severity.Info);
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

                estaEliminando = true;
                textoOperacionBd = $"Eliminando a {item.NombreEmpleado} de la base de datos...";
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
                finally
                {
                    estaEliminando = false;
                    StateHasChanged();
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

                string cliAsist = !string.IsNullOrWhiteSpace(filtroClienteGrilla) ? filtroClienteGrilla : ClienteActivoAsistenciasSeguro;
                if (!string.IsNullOrWhiteSpace(cliAsist))
                {
                    query = query.Where(x => string.Equals(x.Cliente, cliAsist, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filtroAreaGrilla))
                {
                    query = query.Where(x => string.Equals(x.Area, filtroAreaGrilla, StringComparison.OrdinalIgnoreCase));
                }
                if (filtroAsistencia == "PRESENTES")
                    query = query.Where(x => x.Asistio);
                else if (filtroAsistencia == "AUSENTES")
                    query = query.Where(x => !x.Asistio);

                if (TieneArchivoSinsaActivo && filtroConciliacionSinsa != "TODOS")
                {
                    query = query.Where(x =>
                    {
                        var st = ObtenerEstadoConciliacionSinsa(x).Estado;
                        return filtroConciliacionSinsa switch
                        {
                            "OK" => st == "OK",
                            "DIF" => st == "DIF",
                            "NO_EN_SINSA" => st == "NO_EN_SINSA",
                            _ => true
                        };
                    });
                }

                return query.OrderBy(x => x.Cliente).ThenBy(x => x.Area).ThenBy(x => x.NombreEmpleado);
            }
        }

        // --- CONSOLIDADO HELPDESK (Tab 2) ---
        private void ActualizarGruposConsolidados() => ActualizarGruposConsolidadosInterno(true);

        private void ActualizarGruposConsolidadosInterno(bool autoGuardar = true)
        {
            // Agrupación automática igual que Sheet 'Consolidado Agrupado' de OPM_Produccion_Diaria_Hrs.xlsx
            // Garantizar que ningún colaborador ausente conserve tickets de movimientos ni viáticos
            foreach (var aus in empleadosJornada.Where(x => !x.Asistio || string.Equals(x.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase)))
            {
                aus.TicketRedmineId = null;
                aus.TicketViaticoId = null;
                aus.TicketBonoId = null;
            }

            // Agrupación automática igual que Sheet 'Consolidado Agrupado' de OPM_Produccion_Diaria_Hrs.xlsx (EXCLUSIVAMENTE ASISTENTES)
            var presentes = empleadosJornada
                .Where(x => x.Asistio 
                         && !string.Equals(x.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase)
                         && x.HoraEntrada != "--:--"
                         && x.HoraSalida != "--:--")
                .ToList();

            // Preservar observaciones previas del supervisor si ya existían para no sobreescribir lo que digitó
            var dicObsPrevias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prev in gruposConsolidados)
            {
                string key = $"{prev.Cliente}|{prev.Area}|{prev.Puesto}|{prev.HoraInicio}|{prev.HoraFin}|{prev.TicketRedmineId ?? 0}";
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
                    x.BonoBnc,
                    x.TraspAplicaNomina,
                    x.AlimAplicaNomina,
                    TicketRedmineKey = (x.TicketRedmineId.HasValue && x.TicketRedmineId > 0) ? x.TicketRedmineId.Value : 0
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

                        if (!string.IsNullOrWhiteSpace(emp.Observaciones) && emp.Observaciones.Contains("Nómina:", StringComparison.OrdinalIgnoreCase))
                        {
                            int idxNom = emp.Observaciones.IndexOf("Nómina:", StringComparison.OrdinalIgnoreCase);
                            string resto = emp.Observaciones.Substring(idxNom + 7).Trim().TrimEnd(')').Trim();
                            if (!string.IsNullOrWhiteSpace(resto))
                            {
                                clienteNomina = resto;
                            }
                        }

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

                    string keyGrupo = $"{g.Key.Cliente}|{g.Key.Area}|{g.Key.Puesto}|{g.Key.HoraInicio}|{g.Key.HoraFin}|{g.Key.TicketRedmineKey}";
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
                        TraspAplicaNomina = g.Key.TraspAplicaNomina,
                        AlimAplicaNomina = g.Key.AlimAplicaNomina,
                        Empleados = empleadosGrupo,
                        Observaciones = obsFinal,
                        NotaPrestamos = notaAutomatica
                    };
                })
                .ToList();

            gruposConsolidados = grupos;
            ActualizarGruposHorasExtrasInterno();
            if (autoGuardar)
            {
                _ = AutoGuardarBorradorLocal();
            }
        }

        // --- ENVIAR A HELPDESK (REDMINE) ---
        protected async Task EnviarAHelpdeskMasivo()
        {
            ActualizarGruposConsolidados();

            var gruposVisibles = GruposMovimientosFiltrados;
            if (!gruposVisibles.Any())
            {
                _snackbar.Add("No hay grupos activos para enviar a Helpdesk en el cliente seleccionado.", Severity.Warning);
                return;
            }

            if (HayCambiosSinGuardar)
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque hay información pendiente de guardar en Base de Datos.\n\nPor favor, haz clic en el botón amarillo 'GUARDAR EN BD' (arriba a la derecha) antes de enviar la jornada.",
                    yesText: "Entendido");
                return;
            }

            // Filtrar únicamente los grupos del cliente seleccionado que NO cuentan con ticket en Helpdesk
            var gruposPendientes = gruposVisibles.Where(g => !g.Sincronizado && g.Cantidad > 0).ToList();
            if (!gruposPendientes.Any())
            {
                _snackbar.Add("Todos los grupos de movimiento del cliente seleccionado ya cuentan con ticket en Helpdesk.", Severity.Info);
                return;
            }

            int totalGrupos = gruposPendientes.Count;
            int totalEmpleados = gruposPendientes.Sum(g => g.Cantidad);
            string clientesTexto = string.Join(", ", gruposPendientes.Select(g => g.Cliente).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(clientesTexto))
                clientesTexto = !string.IsNullOrWhiteSpace(clienteRedmineIdentificador) ? clienteRedmineIdentificador : "Operaciones";

            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Envío a Helpdesk",
                $"¿Estás seguro de enviar {totalGrupos} grupo(s) pendiente(s) de movimiento ({totalEmpleados} colaborador(es) sin ticket) a Redmine Helpdesk para '{clientesTexto}'?",
                yesText: "Sí, Enviar a Helpdesk", cancelText: "Cancelar");

            if (confirm != true) return;

            estaEnviandoHelpdesk = true;
            textoOperacionBd = "Sincronizando movimientos e incidencias con Helpdesk...";
            try
            {
                var movimientosRedmine = gruposPendientes.Select(g => new MovimientoPersonalItemDto
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
                    Bonos = "0.00",
                    VtcoTrasp = "0",
                    VtcoAlim = "0"
                }).ToList();

                string clientePrincipal = gruposPendientes.Select(g => g.Cliente).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))
                    ?? (!string.IsNullOrWhiteSpace(clienteRedmineIdentificador) ? clienteRedmineIdentificador : "Operaciones");

                var request = new CrearMovimientosPersonalRequestDto
                {
                    OperacionId = selectedOperacionId ?? 0,
                    ClienteRedmine = clientePrincipal,
                    ModoGeneracion = "SoloMovimientos",
                    UsuarioRedmine = UsuarioRedmineEfectivo,
                    Movimientos = movimientosRedmine
                };

                var response = await _odooService.CrearMovimientosPersonalMasivo(request);

                if (response?.Model != null)
                {
                    var res = response.Model;
                    int creados = res.Exitosos;

                    // Estampar los IDs de Redmine en los empleados correspondientes (EXCLUSIVAMENTE PRESENTES)
                    if (res.Detalles != null)
                    {
                        foreach (var det in res.Detalles)
                        {
                            var grupo = gruposConsolidados.FirstOrDefault(g => g.GrupoIndex == det.FilaIndex);
                            if (grupo != null && (det.RedmineIssueId.HasValue || det.RedmineViaticosIssueId.HasValue))
                            {
                                var idsDb = grupo.Empleados
                                    .Where(e => e.Id > 0 && e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase))
                                    .Select(e => e.Id)
                                    .ToList();

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

                                foreach (var emp in grupo.Empleados.Where(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (det.RedmineIssueId.HasValue) emp.TicketRedmineId = det.RedmineIssueId;
                                    if (det.RedmineViaticosIssueId.HasValue) emp.TicketViaticoId = det.RedmineViaticosIssueId;
                                    emp.Estado = "SINCRONIZADO";
                                    emp.Modificado = false;
                                }
                            }
                        }
                    }

                    ActualizarGruposConsolidados();
                    _snackbar.Add($"🚀 {creados} ticket(s) creados con éxito en Helpdesk Redmine.", Severity.Success);
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

        // --- PURGA DE BORRADORES OBSOLETOS DE LOCALSTORAGE (Conservando días recientes) ---
        private async Task LimpiarTodosLosBorradoresLocales()
        {
            try
            {
                await _js.InvokeVoidAsync("eval", @"
                    try {
                        for (let i = localStorage.length - 1; i >= 0; i--) {
                            const key = localStorage.key(i);
                            if (key && (key.startsWith('supervisor_offline_jornada_') || key.startsWith('supervisor_jornada_draft_'))) {
                                localStorage.removeItem(key);
                            }
                        }
                    } catch(e) {}
                ");
            }
            catch { }
        }

        private async Task PurgarBorradoresLocalesAntiguos() => await LimpiarTodosLosBorradoresLocales();

        private async Task PurgarBorradoresLocales() => await PurgarBorradoresLocalesAntiguos();

        protected void OnClienteFilaCambiado(EmpleadoJornadaItem item)
        {
            item.Modificado = true;
            var areasNuevas = GetAreasParaCliente(item.Cliente);
            if (!areasNuevas.Contains(item.Area, StringComparer.OrdinalIgnoreCase))
            {
                item.Area = areasNuevas.FirstOrDefault() ?? "Operaciones";
            }
            AplicarReglasViaticosEnMemoria(item);
            ActualizarGruposConsolidados();
        }

        protected void OnAreaFilaCambiado(EmpleadoJornadaItem item)
        {
            item.Modificado = true;
            AplicarReglasViaticosEnMemoria(item);
            ActualizarGruposConsolidados();
        }

        // --- CATÁLOGO DE ÁREAS POR CLIENTE ---
        public List<string> GetAreasParaCliente(string? cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente))
                return areasSugeridas.Any() ? areasSugeridas : new List<string> { "Operaciones" };

            string limpio = cliente.Trim();

            // 1. Por catálogo configurado de supervisión (exacto)
            if (areasPorCliente.TryGetValue(limpio, out var exactList) && exactList.Any())
                return exactList;

            // 2. Por coincidencia parcial en el catálogo
            var match = areasPorCliente.FirstOrDefault(kvp =>
                kvp.Key.Contains(limpio, StringComparison.OrdinalIgnoreCase) ||
                limpio.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));

            if (match.Value != null && match.Value.Any())
                return match.Value;

            // 3. Buscar en catálogo tbl_bs_cliente_areas por OperacionId si coincide con operacionesList
            var opMatch = operacionesList.FirstOrDefault(o =>
                string.Equals(o.nombre?.Trim(), limpio, StringComparison.OrdinalIgnoreCase) ||
                (o.nombre?.IndexOf(limpio, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                limpio.IndexOf(o.nombre ?? "____", StringComparison.OrdinalIgnoreCase) >= 0);

            if (opMatch != null && opMatch.id > 0)
            {
                var areasOp = catalogoClienteAreas
                    .Where(x => x.Activo && x.OperacionId == opMatch.id)
                    .Select(x => x.AreaNombre?.Trim())
                    .Where(a => !string.IsNullOrWhiteSpace(a))
                    .Select(a => a!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (areasOp.Any())
                    return areasOp;
            }

            // 4. Áreas ya existentes en la jornada activa para este cliente
            var areasDeJornada = empleadosJornada
                .Where(x => string.Equals(x.Cliente?.Trim(), limpio, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Area?.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (areasDeJornada.Any())
                return areasDeJornada;

            return new List<string> { "Operaciones" };
        }

        #region Reglas de Viáticos en Memoria (Evaluación Automática por Horario)
        protected async Task CargarCacheReglasViaticos()
        {
            try
            {
                cargandoReglasViaticos = true;
                var resp = await _odooService.GetAllReglasViaticos();
                if (resp?.Model != null && resp.Model.Any())
                {
                    cacheReglasViaticos = resp.Model.Where(r => r.Activo).ToList();
                    await _js.InvokeVoidAsync("localStorage.setItem", KeyCacheReglasViaticos, JsonSerializer.Serialize(cacheReglasViaticos));
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando cache de reglas de viaticos online: {ex.Message}");
            }
            finally
            {
                cargandoReglasViaticos = false;
            }

            // Fallback offline para Reglas de Viáticos
            if (!cacheReglasViaticos.Any())
            {
                try
                {
                    var cachedJson = await _js.InvokeAsync<string>("localStorage.getItem", KeyCacheReglasViaticos);
                    if (!string.IsNullOrWhiteSpace(cachedJson))
                    {
                        var cached = JsonSerializer.Deserialize<List<ReglaViaticoDto>>(cachedJson);
                        if (cached != null && cached.Any())
                        {
                            cacheReglasViaticos = cached;
                        }
                    }
                }
                catch { }
            }
        }

        protected void AplicarReglasViaticosEnMemoria(EmpleadoJornadaItem item, bool notificar = false)
        {
            if (!item.Asistio)
            {
                item.VtcoTrasp = 0m;
                item.VtcoAlim = 0m;
                item.TraspAplicaNomina = false;
                item.AlimAplicaNomina = false;
                item.BonoBnc = 0m;
                item.Bnc = "0";
                return;
            }

            if (cacheReglasViaticos == null || !cacheReglasViaticos.Any()) return;

            string cliente = !string.IsNullOrWhiteSpace(item.Cliente)
                ? item.Cliente
                : (selectedOperacionId.HasValue ? operacionesList.FirstOrDefault(o => o.id == selectedOperacionId.Value)?.nombre ?? "" : "");

            var evaluacion = BsOperaciones.Application.Common.ReglaViaticoEvaluator.Evaluar(
                cliente,
                item.Area,
                item.HoraEntrada,
                item.HoraSalida,
                cacheReglasViaticos,
                item.Asistio);

            if (evaluacion.AplicaRegla)
            {
                item.VtcoTrasp = evaluacion.MontoTransporte;
                item.VtcoAlim = evaluacion.MontoAlimentacion;
                item.TraspAplicaNomina = evaluacion.TraspAplicaNomina;
                item.AlimAplicaNomina = evaluacion.AlimAplicaNomina;
                item.BonoBnc = evaluacion.BonoBnc;
                item.Modificado = true;

                if (notificar)
                {
                    _snackbar.Add($"Regla aplicada a {item.NombreEmpleado}: {evaluacion.Resumen}", Severity.Info);
                }
            }
            else if (evaluacion.TieneReglasCliente)
            {
                item.VtcoTrasp = 0;
                item.VtcoAlim = 0;
                item.TraspAplicaNomina = false;
                item.AlimAplicaNomina = false;
                item.BonoBnc = 0;
                item.Modificado = true;
            }
        }

        protected void ReevaluarReglasViaticosTodos(bool soloSeleccionados = false)
        {
            if (cacheReglasViaticos == null || !cacheReglasViaticos.Any())
            {
                _snackbar.Add("No hay reglas de viáticos configuradas o activas en el sistema.", Severity.Warning);
                return;
            }

            var targets = soloSeleccionados
                ? empleadosJornada.Where(x => x.Seleccionado && x.Asistio).ToList()
                : empleadosJornada.Where(x => x.Asistio).ToList();

            if (!targets.Any())
            {
                _snackbar.Add("No hay colaboradores asistentes para evaluar.", Severity.Info);
                return;
            }

            int procesados = 0;
            foreach (var emp in targets)
            {
                AplicarReglasViaticosEnMemoria(emp);
                procesados++;
            }

            ActualizarGruposConsolidados();
            _snackbar.Add($"Reglas de viáticos evaluadas para {procesados} colaboradores.", Severity.Success);
        }
        #endregion

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

                    if (catalogoClienteAreas.Any())
                    {
                        await _js.InvokeVoidAsync("localStorage.setItem", KeyCacheAreas, JsonSerializer.Serialize(catalogoClienteAreas));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando catalogo cliente areas online: {ex.Message}");
            }
            finally
            {
                cargandoAreas = false;
                StateHasChanged();
            }

            // Fallback offline desde localStorage para Áreas
            if (!catalogoClienteAreas.Any())
            {
                try
                {
                    var cachedJson = await _js.InvokeAsync<string>("localStorage.getItem", KeyCacheAreas);
                    if (!string.IsNullOrWhiteSpace(cachedJson))
                    {
                        var cached = JsonSerializer.Deserialize<List<ClienteAreaDto>>(cachedJson);
                        if (cached != null && cached.Any())
                        {
                            catalogoClienteAreas = cached;
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
                        }
                    }
                }
                catch { }
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

        protected async Task AutoGuardarBorradorLocal()
        {
            try
            {
                if (fechaSeleccionada.HasValue && !string.IsNullOrWhiteSpace(currentUserName))
                {
                    if (empleadosJornada.Any())
                    {
                        var json = JsonSerializer.Serialize(empleadosJornada);
                        await _js.InvokeVoidAsync("localStorage.setItem", KeyBorradorJornada, json);
                        ultimaHoraGuardadoLocal = DateTime.Now;
                    }
                    else
                    {
                        await _js.InvokeVoidAsync("localStorage.removeItem", KeyBorradorJornada);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al autoguardar borrador local: {ex.Message}");
            }
        }

        protected async Task<bool> RecuperarBorradorLocal()
        {
            try
            {
                if (fechaSeleccionada.HasValue && !string.IsNullOrWhiteSpace(currentUserName))
                {
                    var json = await _js.InvokeAsync<string>("localStorage.getItem", KeyBorradorJornada);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var items = JsonSerializer.Deserialize<List<EmpleadoJornadaItem>>(json);
                        if (items != null && items.Any())
                        {
                            empleadosJornada = items;
                            ultimaHoraGuardadoLocal = DateTime.Now;
                            ValidarConflictosHorarios(out _);
                            ActualizarGruposConsolidadosInterno(false);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al recuperar borrador local: {ex.Message}");
            }
            return false;
        }

        protected async Task LimpiarBorradorLocal()
        {
            try
            {
                if (fechaSeleccionada.HasValue && !string.IsNullOrWhiteSpace(currentUserName))
                {
                    await _js.InvokeVoidAsync("localStorage.removeItem", KeyBorradorJornada);
                }
            }
            catch { }
        }

        private async Task GuardarBorradorLocal() => await AutoGuardarBorradorLocal();

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
                a.Seleccionado = marcar && !a.SincronizadoAusencia;
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
            var ausentes = (soloSeleccionados
                ? AusentesFiltrados.Where(x => !x.SincronizadoAusencia && x.Seleccionado)
                : AusentesFiltrados.Where(x => !x.SincronizadoAusencia)).ToList();

            if (!ausentes.Any())
            {
                _snackbar.Add("No hay ausencias pendientes sin número de ticket en Helpdesk para el cliente seleccionado.", Severity.Info);
                return;
            }

            if (HayCambiosSinGuardar)
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque hay información de ausencias o jornada pendiente de guardar en Base de Datos.\n\nPor favor, haz clic en el botón amarillo 'GUARDAR EN BD' (arriba a la derecha) antes de reportar las ausencias.",
                    yesText: "Entendido");
                return;
            }

            int total = ausentes.Count;
            string clientesTexto = string.Join(", ", ausentes.Select(a => a.Cliente).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase));
            string sufijoCliente = !string.IsNullOrWhiteSpace(clientesTexto) ? $" del cliente '{clientesTexto}'" : string.Empty;

            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Solicitud de Ausencias",
                $"¿Estás seguro de generar la solicitud de ausencia en Helpdesk para {total} colaborador(es) pendiente(s) sin ticket{sufijoCliente}?",
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
                    UsuarioRedmine = UsuarioRedmineEfectivo,
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
                                emp.Seleccionado = false;
                                emp.Modificado = false;
                                emp.Estado = "SINCRONIZADO";
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
                        var motivos = res.Detalles?
                            .Where(d => !d.Exitoso && !string.IsNullOrWhiteSpace(d.Mensaje))
                            .Select(d => $"#{d.CodigoEmpleado}: {d.Mensaje}")
                            .Take(3)
                            .ToList() ?? new List<string>();
                        string detalleMotivos = motivos.Any() ? $" Detalle: {string.Join(" | ", motivos)}" : string.Empty;
                        _snackbar.Add($"⚠️ Hubo {res.Fallidos} solicitud(es) de ausencia que no se pudieron procesar.{detalleMotivos}", Severity.Warning);
                    }
                }
                else
                {
                    _snackbar.Add($"Error al comunicarse con el servicio de Helpdesk: {response?.Respuesta?.MensajeError}", Severity.Error);
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
            if (emp == null || emp.SincronizadoAusencia) return;
            if (HayCambiosSinGuardar)
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque hay información pendiente de guardar en Base de Datos.\n\nPor favor, haz clic en el botón amarillo 'GUARDAR EN BD' (arriba a la derecha) antes de enviar la ausencia.",
                    yesText: "Entendido");
                return;
            }

            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Solicitud de Ausencia",
                $"¿Deseas generar la solicitud de ausencia en Helpdesk para {emp.NombreEmpleado} (#{emp.CodigoEmpleado})?",
                yesText: "Sí, Solicitar", cancelText: "Cancelar");

            if (confirm != true) return;

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
                    UsuarioRedmine = UsuarioRedmineEfectivo,
                    AutorizadoPor = "9",
                    Ausencias = new List<AusenciaPersonalItemDto> { dto }
                };

                var response = await _odooService.CrearAusenciasPersonalMasivo(request);
                var det = response?.Model?.Detalles?.FirstOrDefault();
                if (det != null && det.Exitoso && det.RedmineIssueId.HasValue)
                {
                    emp.TicketAusenciaId = det.RedmineIssueId;
                    emp.Modificado = false;
                    emp.Estado = "SINCRONIZADO";
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

        // --- ENVIAR SOLICITUD DE VIÁTICOS A HELPDESK (OPERACIONES - TRACKER 27) ---
        protected async Task EnviarViaticosMasivo()
        {
            var conViaticos = empleadosJornada
                .Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && (x.VtcoTrasp > 0 || x.VtcoAlim > 0))
                .ToList();
            if (!conViaticos.Any())
            {
                _snackbar.Add("No hay colaboradores con viáticos (transporte o alimentación) asignados en el cliente seleccionado.", Severity.Warning);
                return;
            }

            if (HayCambiosSinGuardar)
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque hay información de viáticos o jornada pendiente de guardar en Base de Datos.\n\nPor favor, haz clic en el botón amarillo 'GUARDAR EN BD' (arriba a la derecha) antes de enviar las solicitudes de viáticos.",
                    yesText: "Entendido");
                return;
            }

            ActualizarGruposConsolidados();
            var gruposVisiblesViaticos = GruposViaticos;
            var gruposConViaticosPendientes = gruposVisiblesViaticos
                .Where(g => !g.Sincronizado && g.Recursos > 0)
                .ToList();

            if (!gruposConViaticosPendientes.Any())
            {
                _snackbar.Add("Todas las solicitudes de viáticos del cliente seleccionado ya cuentan con un ticket generado en Helpdesk.", Severity.Info);
                return;
            }

            int cantPendiente = gruposConViaticosPendientes.Sum(g => g.Recursos);
            decimal totalViatPendiente = gruposConViaticosPendientes.Sum(g => g.TotalGrupo);
            decimal traspPendiente = gruposConViaticosPendientes.Sum(g => g.VtcoTrasp * g.Recursos);
            decimal alimPendiente = gruposConViaticosPendientes.Sum(g => g.VtcoAlim * g.Recursos);

            bool? confirm = await _dialogService.ShowMessageBox(
                "Solicitud de Viáticos (Operaciones - Tracker 27)",
                $"¿Deseas enviar {gruposConViaticosPendientes.Count} solicitud(es) pendiente(s) de viáticos para {cantPendiente} colaborador(es) sin ticket por un total de C$ {totalViatPendiente:N2} (Transporte: C$ {traspPendiente:N2}, Alimentación: C$ {alimPendiente:N2})?",
                yesText: "Sí, Enviar a Operaciones", cancelText: "Cancelar");

            if (confirm != true) return;

            estaEnviandoViaticos = true;
            textoOperacionBd = "Enviando solicitudes de viáticos a Helpdesk...";
            try
            {
                var movimientosRedmine = gruposConViaticosPendientes.Select(g => 
                {
                    var empConViaticos = g.Empleados
                        .Where(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase) && !e.SincronizadoViatico && (e.VtcoTrasp > 0 || e.VtcoAlim > 0))
                        .ToList();
                    decimal totalViatNominaGrupo = empConViaticos.Sum(e => e.TotalViaticosNomina);
                    decimal totalViatEfectivoGrupo = empConViaticos.Sum(e => e.TotalViaticosEfectivo);

                    var sbDetalle = new StringBuilder();
                    sbDetalle.AppendLine("FORMA DE LIQUIDACIÓN DE VIÁTICOS:");
                    sbDetalle.AppendLine($"• Pago en Nómina: C$ {totalViatNominaGrupo:N2}");
                    sbDetalle.AppendLine($"• Pago en Efectivo: C$ {totalViatEfectivoGrupo:N2}");
                    sbDetalle.AppendLine();
                    sbDetalle.AppendLine("Detalle por colaborador:");

                    foreach (var emp in empConViaticos)
                    {
                        var partes = new List<string>();
                        if (emp.VtcoTrasp > 0)
                        {
                            string via = emp.TraspAplicaNomina ? "NÓMINA" : "EFECTIVO";
                            partes.Add($"Transporte: C$ {emp.VtcoTrasp:N2} -> {via}");
                        }
                        if (emp.VtcoAlim > 0)
                        {
                            string via = emp.AlimAplicaNomina ? "NÓMINA" : "EFECTIVO";
                            partes.Add($"Alimentación: C$ {emp.VtcoAlim:N2} -> {via}");
                        }

                        string resumenEmp = $"Total: C$ {emp.TotalViaticos:N2} (Nómina: C$ {emp.TotalViaticosNomina:N2} / Efectivo: C$ {emp.TotalViaticosEfectivo:N2})";
                        sbDetalle.AppendLine($"• #{emp.CodigoEmpleado} {emp.NombreEmpleado}: {string.Join(" | ", partes)} [{resumenEmp}]");
                    }

                    string obsGrupoPrevio = gruposConsolidados
                        .FirstOrDefault(gc => string.Equals(gc.Cliente, g.Cliente, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(gc.Area, g.Area, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(gc.Puesto, g.Puesto, StringComparison.OrdinalIgnoreCase)
                            && gc.HoraInicio == g.HoraInicio && gc.HoraFin == g.HoraFin)?.Observaciones ?? string.Empty;

                    string obsConLiquidacion = string.Join("\n\n", new[] { obsGrupoPrevio, sbDetalle.ToString().Trim() }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));

                    string tiempoComidaGrupo = empConViaticos.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.TiempoComida))?.TiempoComida ?? "1.00";

                    return new MovimientoPersonalItemDto
                    {
                        FilaIndex = g.Index,
                        Cliente = g.Cliente,
                        Area = g.Area,
                        Cargo = g.Puesto,
                        Fecha = fechaSeleccionada?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                        HoraInicio = g.HoraInicio,
                        HoraFin = g.HoraFin,
                        TiempoComida = tiempoComidaGrupo,
                        CantidadEmpleados = g.Recursos,
                        IdsEmpleados = g.IdsEmpleadosTexto,
                        Asunto = !string.IsNullOrWhiteSpace(g.Cliente) ? $"{g.Cliente} - {g.Area} - {g.Puesto}".Trim() : $"{g.Area} - {g.Puesto}".Trim(),
                        Observaciones = obsConLiquidacion,
                        Bonos = "0.00",
                        VtcoTrasp = g.VtcoTrasp > 0 ? g.VtcoTrasp.ToString("N2", CultureInfo.InvariantCulture) : "0",
                        VtcoAlim = g.VtcoAlim > 0 ? g.VtcoAlim.ToString("N2", CultureInfo.InvariantCulture) : "0"
                    };
                }).ToList();

                string clientePrincipal = gruposConViaticosPendientes.Select(g => g.Cliente).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))
                    ?? (!string.IsNullOrWhiteSpace(clienteRedmineIdentificador) ? clienteRedmineIdentificador : "Operaciones");

                var request = new CrearMovimientosPersonalRequestDto
                {
                    OperacionId = selectedOperacionId ?? 0,
                    ClienteRedmine = clientePrincipal,
                    ModoGeneracion = "SoloViaticos",
                    UsuarioRedmine = UsuarioRedmineEfectivo,
                    Movimientos = movimientosRedmine
                };

                var response = await _odooService.CrearMovimientosPersonalMasivo(request);
                if (response?.Model != null)
                {
                    var res = response.Model;
                    if (res.Detalles != null)
                    {
                        foreach (var det in res.Detalles)
                        {
                            var grupo = gruposConViaticosPendientes.FirstOrDefault(g => g.Index == det.FilaIndex);
                            if (grupo != null && det.RedmineViaticosIssueId.HasValue)
                            {
                                var idsDb = grupo.Empleados
                                    .Where(e => e.Id > 0 && e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase))
                                    .Select(e => e.Id)
                                    .ToList();

                                if (idsDb.Any())
                                {
                                    await _odooService.UpdateSyncHelpdeskMovimientos(new ActualizarSyncHelpdeskRequest
                                    {
                                        Ids = idsDb,
                                        TicketViaticoId = det.RedmineViaticosIssueId,
                                        Estado = "SINCRONIZADO"
                                    });
                                }

                                foreach (var emp in grupo.Empleados.Where(e => e.Asistio && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase)))
                                {
                                    emp.TicketViaticoId = det.RedmineViaticosIssueId;
                                    emp.Modificado = false;
                                    emp.Estado = "SINCRONIZADO";
                                }
                            }
                        }
                    }

                    ActualizarGruposConsolidados();
                    _snackbar.Add($"✅ {res.Exitosos} solicitud(es) de viáticos generadas en Operaciones (Tracker 27).", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Error al enviar viáticos: {response?.Respuesta?.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error en comunicación con Helpdesk: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaEnviandoViaticos = false;
                StateHasChanged();
            }
        }

        // --- ENVIAR SOLICITUDES DE BONO DIARIO A HELPDESK (RRHH - TRACKER 11: APLICAR BONO DEL DÍA) ---
        protected async Task EnviarBonosMasivo()
        {
            var conBono = empleadosJornada
                .Where(x => x.Asistio && PerteneceAClienteSeleccionado(x.Cliente) && x.BonoBnc > 0)
                .ToList();
            if (!conBono.Any())
            {
                _snackbar.Add("No hay colaboradores con bono asignado para enviar en el cliente seleccionado.", Severity.Warning);
                return;
            }

            if (HayCambiosSinGuardar)
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque hay información de bonos o jornada pendiente de guardar en Base de Datos.\n\nPor favor, haz clic en el botón amarillo 'GUARDAR EN BD' (arriba a la derecha) antes de enviar las solicitudes de bono.",
                    yesText: "Entendido");
                return;
            }

            // Filtrar únicamente los colaboradores con bono del cliente seleccionado que NO han sido sincronizados
            var conBonoPendiente = conBono.Where(x => !x.SincronizadoBono).ToList();
            if (!conBonoPendiente.Any())
            {
                _snackbar.Add("Todas las solicitudes de Bono del Día del cliente seleccionado ya cuentan con un ticket generado en Helpdesk.", Severity.Info);
                return;
            }

            decimal totalBncPendiente = conBonoPendiente.Sum(x => x.BonoBnc);
            bool? confirm = await _dialogService.ShowMessageBox(
                "Solicitud de Bono Diario (RRHH - Tracker 11)",
                $"¿Deseas enviar las solicitudes individuales de Bono del Día a RRHH para {conBonoPendiente.Count} colaborador(es) pendiente(s) sin ticket por un total de C$ {totalBncPendiente:N2}?",
                yesText: "Sí, Enviar a RRHH", cancelText: "Cancelar");

            if (confirm != true) return;

            estaEnviandoBonos = true;
            textoOperacionBd = "Enviando bonos diarios a Helpdesk RRHH...";
            try
            {
                var dtos = conBonoPendiente.Select((b, idx) => 
                {
                                        decimal viatTraspNomina = b.TraspAplicaNomina ? b.VtcoTrasp : 0;
                    decimal viatAlimNomina = b.AlimAplicaNomina ? b.VtcoAlim : 0;
                    decimal bonoExtra = b.BonoExtra;

                    var lineasDetalle = new List<string>();
                    if (viatTraspNomina > 0)
                        lineasDetalle.Add($"• Viático Transporte en Nómina: C$ {viatTraspNomina:N2}");
                    if (viatAlimNomina > 0)
                        lineasDetalle.Add($"• Viático Alimentación en Nómina: C$ {viatAlimNomina:N2}");
                    if (bonoExtra > 0)
                        lineasDetalle.Add($"• Bono Adicional de Cumplimiento: C$ {bonoExtra:N2}");
                    if (!lineasDetalle.Any())
                        lineasDetalle.Add($"• Bono de Cumplimiento / Desempeño: C$ {b.BonoBnc:N2}");

                    string detalleDesglose = "DESGLOSE DE LO DEPOSITADO (TOTAL C$ " + b.BonoBnc.ToString("N2") + "):\n" + string.Join("\n", lineasDetalle);

                    string relacionTicketViatico = (b.TicketViaticoId.HasValue && b.TicketViaticoId.Value > 0)
                        ? $"Ticket de Viáticos Relacionado: #{b.TicketViaticoId.Value}"
                        : "";

                    string obsFinalBono = string.Join("\n\n", new[] { detalleDesglose, relacionTicketViatico, !string.IsNullOrWhiteSpace(b.Observaciones) ? $"Observaciones:\n{b.Observaciones.Trim()}" : "" }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));

                    return new BonoPersonalItemDto
                    {
                        FilaIndex = idx + 1,
                        MovimientoId = b.Id,
                        CodigoEmpleado = b.CodigoEmpleado,
                        NombreEmpleado = b.NombreEmpleado,
                        Cliente = !string.IsNullOrWhiteSpace(b.Cliente) ? b.Cliente : (clienteRedmineIdentificador ?? string.Empty),
                        Area = b.Area,
                        Puesto = b.Puesto,
                        Fecha = fechaSeleccionada?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                        MontoBono = b.BonoBnc,
                        Observaciones = obsFinalBono,
                        ViaticoTraspNomina = viatTraspNomina,
                        ViaticoAlimNomina = viatAlimNomina,
                        BonoAdicional = bonoExtra,
                        TicketViaticoRelacionadoId = b.TicketViaticoId
                    };
                }).ToList();

                var request = new CrearBonosPersonalRequestDto
                {
                    OperacionId = selectedOperacionId ?? 0,
                    UsuarioRedmine = UsuarioRedmineEfectivo,
                    AutorizadoPor = "9",
                    Bonos = dtos
                };

                var response = await _odooService.CrearBonosPersonalMasivo(request);
                if (response?.Model != null)
                {
                    var res = response.Model;
                    if (res.Detalles != null)
                    {
                        foreach (var det in res.Detalles)
                        {
                            var emp = conBono.FirstOrDefault(x => x.CodigoEmpleado == det.CodigoEmpleado);
                            if (emp != null && det.RedmineIssueId.HasValue)
                            {
                                emp.TicketBonoId = det.RedmineIssueId;
                                emp.Modificado = false;
                                emp.Estado = "SINCRONIZADO";
                                if (emp.Id > 0)
                                {
                                    await _odooService.UpdateSyncHelpdeskMovimientos(new ActualizarSyncHelpdeskRequest
                                    {
                                        Ids = new List<int> { emp.Id },
                                        TicketBonoId = det.RedmineIssueId,
                                        Estado = "SINCRONIZADO"
                                    });
                                }
                            }
                        }
                    }

                    if (res.Exitosos > 0)
                    {
                        _snackbar.Add($"✅ Se generaron {res.Exitosos} solicitud(es) de Bono Diario en RRHH (Tracker 11) con éxito.", Severity.Success);
                    }
                    if (res.Fallidos > 0)
                    {
                        var motivos = res.Detalles?
                            .Where(d => !d.Exitoso && !string.IsNullOrWhiteSpace(d.Mensaje))
                            .Select(d => $"#{d.CodigoEmpleado}: {d.Mensaje}")
                            .Take(3)
                            .ToList() ?? new List<string>();
                        string detalleMotivos = motivos.Any() ? $" Detalle: {string.Join(" | ", motivos)}" : string.Empty;
                        _snackbar.Add($"⚠️ Hubo {res.Fallidos} solicitud(es) de bono que no se pudieron procesar.{detalleMotivos}", Severity.Warning);
                    }
                }
                else
                {
                    _snackbar.Add($"Error al comunicarse con Helpdesk: {response?.Respuesta?.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al enviar bonos a Helpdesk: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaEnviandoBonos = false;
                StateHasChanged();
            }
        }

        protected async Task EnviarBonoIndividual(EmpleadoJornadaItem emp)
        {
            if (emp == null || emp.BonoBnc <= 0 || emp.SincronizadoBono) return;
            if (HayCambiosSinGuardar)
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque hay información pendiente de guardar en Base de Datos.\n\nPor favor, haz clic en el botón amarillo 'GUARDAR EN BD' (arriba a la derecha) antes de enviar el bono.",
                    yesText: "Entendido");
                return;
            }

            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Solicitud de Bono",
                $"¿Deseas generar la solicitud individual de Bono del Día (Tracker 11) en RRHH para {emp.NombreEmpleado} (#{emp.CodigoEmpleado}) por C$ {emp.BonoBnc:N2}?",
                yesText: "Sí, Enviar", cancelText: "Cancelar");

            if (confirm != true) return;

            estaEnviandoBonos = true;
            try
            {
                decimal viatTraspNomina = emp.TraspAplicaNomina ? emp.VtcoTrasp : 0;
                decimal viatAlimNomina = emp.AlimAplicaNomina ? emp.VtcoAlim : 0;
                decimal bonoExtra = emp.BonoExtra;

                var lineasDetalle = new List<string>();
                if (viatTraspNomina > 0)
                    lineasDetalle.Add($"• Viático Transporte en Nómina: C$ {viatTraspNomina:N2}");
                if (viatAlimNomina > 0)
                    lineasDetalle.Add($"• Viático Alimentación en Nómina: C$ {viatAlimNomina:N2}");
                if (bonoExtra > 0)
                    lineasDetalle.Add($"• Bono Adicional de Cumplimiento: C$ {bonoExtra:N2}");
                if (!lineasDetalle.Any())
                    lineasDetalle.Add($"• Bono de Cumplimiento / Desempeño: C$ {emp.BonoBnc:N2}");

                string detalleDesglose = "DESGLOSE DE LO DEPOSITADO (TOTAL C$ " + emp.BonoBnc.ToString("N2") + "):\n" + string.Join("\n", lineasDetalle);

                string relacionTicketViatico = (emp.TicketViaticoId.HasValue && emp.TicketViaticoId.Value > 0)
                    ? $"Ticket de Viáticos Relacionado: #{emp.TicketViaticoId.Value}"
                    : "";

                string obsFinalBono = string.Join("\n\n", new[] { detalleDesglose, relacionTicketViatico, !string.IsNullOrWhiteSpace(emp.Observaciones) ? $"Observaciones:\n{emp.Observaciones.Trim()}" : "" }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                var dto = new BonoPersonalItemDto
                {
                    FilaIndex = 1,
                    MovimientoId = emp.Id,
                    CodigoEmpleado = emp.CodigoEmpleado,
                    NombreEmpleado = emp.NombreEmpleado,
                    Cliente = !string.IsNullOrWhiteSpace(emp.Cliente) ? emp.Cliente : (clienteRedmineIdentificador ?? string.Empty),
                    Area = emp.Area,
                    Puesto = emp.Puesto,
                    Fecha = fechaSeleccionada?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                    MontoBono = emp.BonoBnc,
                    Observaciones = obsFinalBono,
                    ViaticoTraspNomina = viatTraspNomina,
                    ViaticoAlimNomina = viatAlimNomina,
                    BonoAdicional = bonoExtra,
                    TicketViaticoRelacionadoId = emp.TicketViaticoId
                };

                var request = new CrearBonosPersonalRequestDto
                {
                    OperacionId = selectedOperacionId ?? 0,
                    UsuarioRedmine = UsuarioRedmineEfectivo,
                    AutorizadoPor = "9",
                    Bonos = new List<BonoPersonalItemDto> { dto }
                };

                var response = await _odooService.CrearBonosPersonalMasivo(request);
                var det = response?.Model?.Detalles?.FirstOrDefault();
                if (det != null && det.Exitoso && det.RedmineIssueId.HasValue)
                {
                    emp.TicketBonoId = det.RedmineIssueId;
                    emp.Modificado = false;
                    emp.Estado = "SINCRONIZADO";
                    if (emp.Id > 0)
                    {
                        await _odooService.UpdateSyncHelpdeskMovimientos(new ActualizarSyncHelpdeskRequest
                        {
                            Ids = new List<int> { emp.Id },
                            TicketBonoId = det.RedmineIssueId,
                            Estado = "SINCRONIZADO"
                        });
                    }
                    _snackbar.Add($"✅ Solicitud de Bono #{det.RedmineIssueId} generada exitosamente en RRHH.", Severity.Success);
                }
                else
                {
                    _snackbar.Add($"Error al generar bono: {det?.Mensaje ?? "Respuesta no exitosa"}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaEnviandoBonos = false;
                StateHasChanged();
            }
        }

        // =========================================================================
        // TARJETAS DE HORAS EXTRAS POR ÁREA, PUESTO Y HORARIO EXTRA -> HELPDESK
        // =========================================================================
        public class FirmaLocalTarjetaHe
        {
            public string SupervisorNombre { get; set; } = string.Empty;
            public string SupervisorCargo { get; set; } = "Supervisor del Cliente";
            public string? FirmaSupervisorBase64 { get; set; }
            public string GerenteNombre { get; set; } = string.Empty;
            public string GerenteCargo { get; set; } = "Gerente del Cliente";
            public string? FirmaGerenteBase64 { get; set; }
        }

        public class GrupoHorasExtrasAreaItem
        {
            public int Index { get; set; }
            public int OperacionId { get; set; }
            public string Cliente { get; set; } = string.Empty;
            public string Area { get; set; } = string.Empty;
            public string Puesto { get; set; } = string.Empty;
            public string HoraInicioExtra { get; set; } = string.Empty;
            public string HoraFinExtra { get; set; } = string.Empty;
            public decimal HorasExtrasPorPersona { get; set; }
            public string ReglaResumen { get; set; } = string.Empty;
            public bool EsManual { get; set; }
            public List<ColaboradorAutorizacionItemDto> Colaboradores { get; set; } = new();
            public decimal TotalHorasExtras => Math.Round(Colaboradores.Sum(c => c.HorasExtrasAutorizadas), 2);
            public string HoraInicioMin => !string.IsNullOrWhiteSpace(HoraInicioExtra)
                ? HoraInicioExtra
                : (Colaboradores.Select(c => c.HoraInicioExtra).Where(h => !string.IsNullOrWhiteSpace(h)).OrderBy(h => h).FirstOrDefault() ?? "17:00");
            public string HoraFinMax => !string.IsNullOrWhiteSpace(HoraFinExtra)
                ? HoraFinExtra
                : (Colaboradores.Select(c => c.HoraFinExtra).Where(h => !string.IsNullOrWhiteSpace(h)).OrderByDescending(h => h).FirstOrDefault() ?? "20:00");
            public string KeyTarjeta => $"{Cliente}|{Area}|{Puesto}|{HoraInicioExtra}|{HoraFinExtra}|{HorasExtrasPorPersona.ToString("F2", CultureInfo.InvariantCulture)}";
            public string Observaciones { get; set; } = string.Empty;
            public int? AutorizacionId { get; set; }
            public string TokenRecibo { get; set; } = string.Empty;
            public int? TicketRedmineId { get; set; }
            public string EstadoTicket { get; set; } = string.Empty;
            public bool Sincronizado => TicketRedmineId.HasValue && TicketRedmineId.Value > 0;
            public string SupervisorNombre { get; set; } = string.Empty;
            public string SupervisorCargo { get; set; } = "Supervisor del Cliente";
            public string? FirmaSupervisorBase64 { get; set; }
            public bool TieneFirmaSupervisorBd { get; set; }
            public string GerenteNombre { get; set; } = string.Empty;
            public string GerenteCargo { get; set; } = "Gerente del Cliente";
            public string? FirmaGerenteBase64 { get; set; }
            public bool TieneFirmaGerenteBd { get; set; }
            public bool TieneFirmaSupervisor => !string.IsNullOrWhiteSpace(FirmaSupervisorBase64) || TieneFirmaSupervisorBd;
            public bool TieneFirmaGerente => !string.IsNullOrWhiteSpace(FirmaGerenteBase64) || TieneFirmaGerenteBd;
            public bool TieneFirmaCliente => TieneFirmaSupervisor || TieneFirmaGerente;
            public string ResumenFirmanteCliente
            {
                get
                {
                    var p = new List<string>();
                    if (TieneFirmaSupervisor)
                    {
                        string nom = !string.IsNullOrWhiteSpace(SupervisorNombre) ? SupervisorNombre.Trim() : "Supervisor Cliente";
                        string crg = !string.IsNullOrWhiteSpace(SupervisorCargo) ? SupervisorCargo.Trim() : "";
                        p.Add(!string.IsNullOrWhiteSpace(crg) && !string.Equals(nom, crg, StringComparison.OrdinalIgnoreCase) ? $"{nom} ({crg})" : nom);
                    }
                    if (TieneFirmaGerente)
                    {
                        string nom = !string.IsNullOrWhiteSpace(GerenteNombre) ? GerenteNombre.Trim() : "Gerente Cliente";
                        string crg = !string.IsNullOrWhiteSpace(GerenteCargo) ? GerenteCargo.Trim() : "";
                        p.Add(!string.IsNullOrWhiteSpace(crg) && !string.Equals(nom, crg, StringComparison.OrdinalIgnoreCase) ? $"{nom} ({crg})" : nom);
                    }
                    return p.Any() ? string.Join(" / ", p) : string.Empty;
                }
            }
        }

        protected List<GrupoHorasExtrasAreaItem> gruposHorasExtras = new();
        protected ModoVistaGrilla modoVistaHorasExtras = ModoVistaGrilla.Tarjetas;
        protected List<ClienteConfigHorasExtrasDto> configClientesHe = new();
        protected List<Modelo.Entidades.Nomina.AsignacionJornadaCliente> asignacionesJornadasCliente = new();
        protected List<AutorizacionHorasExtrasDto> autorizacionesHeLista = new();
        protected Dictionary<string, string> observacionesPorAreaHe = new(StringComparer.OrdinalIgnoreCase);
        protected Dictionary<string, FirmaLocalTarjetaHe> firmasPorTarjetaHe = new(StringComparer.OrdinalIgnoreCase);
        protected Dictionary<int, (string HoraBaseJornada, string HoraIni, string HoraFin, decimal Horas)> edicionesHorarioHePorEmpleado = new();
        protected List<(string Cliente, DateTime Fecha, ColaboradorAutorizacionItemDto Dto)> itemsHorasExtrasManuales = new();
        protected bool cargandoAutorizacionesHe = false;

        protected bool GrupoTieneAsistenciasSinGuardar(GrupoHorasExtrasAreaItem? grupo)
        {
            if (grupo == null || !grupo.Colaboradores.Any()) return false;
            var cods = grupo.Colaboradores.Select(c => c.CodigoEmpleado).ToHashSet();
            return empleadosJornada.Any(e => cods.Contains(e.CodigoEmpleado) && (e.Modificado || e.Id == 0));
        }

        protected bool PendientesHeTienenAsistenciasSinGuardar =>
            GruposHorasExtrasFiltrados.Where(g => !g.Sincronizado && g.Colaboradores.Count > 0).Any(GrupoTieneAsistenciasSinGuardar);
        protected bool enviandoTicketsHe = false;
        protected DateTime? fechaDesdeFiltroHe = DateTime.Today;
        protected DateTime? fechaHastaFiltroHe = DateTime.Today;
        protected string textoBusquedaHe = string.Empty;

        protected async Task CargarAsignacionesJornadasCliente()
        {
            try
            {
                var resp = await _odooService.GetAllAsignacionesJornada();
                if (resp?.Model != null)
                {
                    asignacionesJornadasCliente = resp.Model.Where(a => a.Activo).ToList();
                    ActualizarGruposHorasExtrasInterno();
                }
            }
            catch { }
        }

        private static string QuitarAcentosTextoHe(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var norm = s.Trim().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(norm.Length);
            foreach (var ch in norm)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        protected (decimal HorasExtras, decimal TopeOrdinario, decimal HorasNetas, string HoraInicioExtra, string HoraFinExtra, string ReglaResumen) CalcularHorasExtrasSegunRegla(EmpleadoJornadaItem? emp, DateTime fecha)
        {
            if (emp == null || !emp.Asistio || string.Equals(emp.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase))
                return (0m, 8m, 0m, "17:00", "20:00", "");

            string hEntStr = NormalizarFormatoHora(emp.HoraEntrada);
            string hSalStr = NormalizarFormatoHora(emp.HoraSalida);
            if (string.IsNullOrWhiteSpace(hEntStr) || string.IsNullOrWhiteSpace(hSalStr) || hEntStr == "--:--" || hSalStr == "--:--")
                return (0m, 8m, 0m, "17:00", "20:00", "");

            if (!TimeSpan.TryParse(hEntStr, out var tEnt) || !TimeSpan.TryParse(hSalStr, out var tSal))
                return (0m, 8m, 0m, "17:00", "20:00", "");

            // Descartar registros sin marcación válida (ej. 00:00 - 00:00 o entrada == salida)
            if (tSal == tEnt || (hEntStr == "00:00" && hSalStr == "00:00"))
                return (0m, 8m, 0m, "17:00", "20:00", "");

            double diffHrs = (tSal - tEnt).TotalHours;
            if (diffHrs < 0)
            {
                // Solo cruzar medianoche en turnos vespertinos/nocturnos reales (nunca salida 00:00 con entrada de mañana)
                if (tEnt.Hours >= 14 || tSal.Hours <= 10)
                    diffHrs += 24.0;
                else
                    return (0m, 8m, 0m, "17:00", "20:00", "");
            }

            if (diffHrs <= 0 || diffHrs > 18.0)
                return (0m, 8m, 0m, "17:00", "20:00", "");

            decimal horasBrutas = (decimal)diffHrs;

            decimal comida = decimal.TryParse(emp.TiempoComida?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tc)
                ? Math.Max(0m, tc)
                : 1.00m;
            decimal comidaEfectiva = horasBrutas > comida ? comida : 0m;
            decimal horasNetas = Math.Round(Math.Max(0m, horasBrutas - comidaEfectiva), 2);

            int opId = ResolverOperacionIdCliente(emp.Cliente);
            string areaClean = QuitarAcentosTextoHe(emp.Area).ToLowerInvariant();
            string puestoClean = QuitarAcentosTextoHe(emp.Puesto).ToLowerInvariant();
            string turnoClean = QuitarAcentosTextoHe(emp.TurnoNombre).ToLowerInvariant();

            bool esNocturno = turnoClean.Contains("noct") || turnoClean.Contains("noche") || (tEnt.Hours >= 18 && tSal < tEnt);
            bool esMixto = !esNocturno && (turnoClean.Contains("mixt") || (tEnt.Hours >= 13 && tEnt.Hours < 18 && horasBrutas <= 8.5m));

            // Buscar regla activa en admon.tbl_bs_asignacion_jornadas_cliente (priorizando coincidencia de Área específica)
            var reglaDb = asignacionesJornadasCliente
                .Where(a => a.Activo && a.OperacionId == opId
                    && (string.IsNullOrWhiteSpace(a.Area)
                        || QuitarAcentosTextoHe(a.Area).Equals(areaClean, StringComparison.OrdinalIgnoreCase)
                        || areaClean.Contains(QuitarAcentosTextoHe(a.Area), StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrWhiteSpace(a.Puesto)
                        || QuitarAcentosTextoHe(a.Puesto).Equals(puestoClean, StringComparison.OrdinalIgnoreCase)
                        || puestoClean.Contains(QuitarAcentosTextoHe(a.Puesto), StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(a => (!string.IsNullOrWhiteSpace(a.Area) ? 4 : 0) + (!string.IsNullOrWhiteSpace(a.Puesto) ? 2 : 0))
                .ThenByDescending(a => a.Id)
                .FirstOrDefault();

            bool domingo100 = reglaDb != null
                ? ((reglaDb.ReglasActivas != null && reglaDb.ReglasActivas.Contains("DOMINGO_DESCANSO_RECARGO_100"))
                   || (!string.IsNullOrWhiteSpace(reglaDb.ReglasActivasJson) && reglaDb.ReglasActivasJson.Contains("DOMINGO_DESCANSO_RECARGO_100", StringComparison.OrdinalIgnoreCase)))
                : (opId == 23 || (emp.Cliente?.Contains("sinsa", StringComparison.OrdinalIgnoreCase) == true));

            decimal topeOrdinario;
            string reglaResumen;

            if (fecha.DayOfWeek == DayOfWeek.Sunday && domingo100)
            {
                topeOrdinario = 0.00m;
                reglaResumen = "Domingo (100% Extra)";
            }
            else if (esNocturno)
            {
                topeOrdinario = 7.00m;
                reglaResumen = "Nocturno (7.00h)";
            }
            else if (esMixto)
            {
                topeOrdinario = 7.50m;
                reglaResumen = "Mixto (7.50h)";
            }
            else if (reglaDb != null)
            {
                topeOrdinario = fecha.DayOfWeek == DayOfWeek.Saturday ? reglaDb.HorasSabado : reglaDb.HorasLunesViernes;
                if (topeOrdinario <= 0m) topeOrdinario = 8.00m;
                reglaResumen = fecha.DayOfWeek == DayOfWeek.Saturday
                    ? $"Diurno Sáb ({topeOrdinario:0.##}h)"
                    : $"Diurno L-V ({topeOrdinario:0.##}h)";
            }
            else
            {
                // Respaldo exacto SINSA por Área si no hay conexión
                bool esArea85_55 = areaClean.Contains("calidad de datos") || areaClean.Contains("recepcion mercaderia");
                if (esArea85_55)
                {
                    topeOrdinario = fecha.DayOfWeek == DayOfWeek.Saturday ? 5.50m : 8.50m;
                    reglaResumen = fecha.DayOfWeek == DayOfWeek.Saturday ? "Diurno Sáb (5.50h)" : "Diurno L-V (8.50h)";
                }
                else
                {
                    topeOrdinario = 8.00m;
                    reglaResumen = "Diurno (8.00h)";
                }
            }

            decimal horasExtras = Math.Round(Math.Max(0m, horasNetas - topeOrdinario), 2);
            // Ignorar variaciones menores a 15 minutos (< 0.25h) propias de fila en reloj biométrico
            if (horasExtras < 0.25m)
                return (0m, topeOrdinario, horasNetas, hSalStr, hSalStr, reglaResumen);

            double inicioExtraMinutos = (tEnt.TotalHours + (double)(topeOrdinario + comidaEfectiva)) * 60.0;
            int minsMod = ((int)Math.Round(inicioExtraMinutos) % 1440 + 1440) % 1440;
            string hInicioExtra = $"{minsMod / 60:D2}:{minsMod % 60:D2}";

            string claveJornadaActual = $"{hEntStr}-{hSalStr}";
            if (emp.CodigoEmpleado > 0
                && empleadosSinHorasExtrasConciliadosSinsa.TryGetValue(emp.CodigoEmpleado, out var claveSinHeSinsa)
                && string.Equals(claveSinHeSinsa, claveJornadaActual, StringComparison.OrdinalIgnoreCase))
            {
                return (0m, topeOrdinario, horasNetas, hSalStr, hSalStr, "Conciliado SINSA (0 H.E.)");
            }

            if (emp.CodigoEmpleado > 0 && edicionesHorarioHePorEmpleado.TryGetValue(emp.CodigoEmpleado, out var edManual)
                && string.Equals(edManual.HoraBaseJornada, claveJornadaActual, StringComparison.OrdinalIgnoreCase))
            {
                return (edManual.Horas, topeOrdinario, horasNetas, edManual.HoraIni, edManual.HoraFin, reglaResumen);
            }

            return (horasExtras, topeOrdinario, horasNetas, hInicioExtra, hSalStr, reglaResumen);
        }

        protected bool TieneHorasExtrasSegunRegla(EmpleadoJornadaItem? emp)
        {
            if (emp == null || !emp.Asistio) return false;
            return CalcularHorasExtrasSegunRegla(emp, (fechaSeleccionada ?? DateTime.Today).Date).HorasExtras > 0m;
        }

        protected string ObtenerObservacionAreaHe(string keyTarjeta)
        {
            string key = string.IsNullOrWhiteSpace(keyTarjeta) ? "Operaciones" : keyTarjeta.Trim();
            return observacionesPorAreaHe.TryGetValue(key, out var val) ? val : string.Empty;
        }

        protected void SetObservacionAreaHe(string keyTarjeta, string? valor)
        {
            string key = string.IsNullOrWhiteSpace(keyTarjeta) ? "Operaciones" : keyTarjeta.Trim();
            observacionesPorAreaHe[key] = valor ?? string.Empty;
            var grp = gruposHorasExtras.FirstOrDefault(g => string.Equals(g.KeyTarjeta, key, StringComparison.OrdinalIgnoreCase));
            if (grp != null)
            {
                grp.Observaciones = valor ?? string.Empty;
            }
        }

        private void ActualizarGruposHorasExtrasInterno()
        {
            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;

            foreach (var prev in gruposHorasExtras)
            {
                if (!string.IsNullOrWhiteSpace(prev.Observaciones))
                {
                    observacionesPorAreaHe[prev.KeyTarjeta] = prev.Observaciones;
                }
            }

            var manualesHoy = itemsHorasExtrasManuales
                .Where(m => m.Fecha.Date == fRef && m.Dto != null && m.Dto.CodigoEmpleado > 0)
                .ToList();
            var codsManualesHoy = manualesHoy.Select(m => m.Dto.CodigoEmpleado).ToHashSet();

            var presentesConHe = empleadosJornada
                .Where(e => e.Asistio
                    && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase)
                    && e.HoraEntrada != "--:--"
                    && e.HoraSalida != "--:--"
                    && ClientePermiteHorasExtras(e.Cliente))
                .ToList();

            var itemsConExtras = new List<(string Cliente, ColaboradorAutorizacionItemDto Dto, string ReglaTxt, bool EsManual)>();
            foreach (var e in presentesConHe)
            {
                if (codsManualesHoy.Contains(e.CodigoEmpleado))
                    continue;

                var calc = CalcularHorasExtrasSegunRegla(e, fRef);
                if (calc.HorasExtras > 0m)
                {
                    string cliEmp = string.IsNullOrWhiteSpace(e.Cliente) ? "SINSA" : e.Cliente.Trim();
                    itemsConExtras.Add((cliEmp, new ColaboradorAutorizacionItemDto
                    {
                        MovimientoDiarioId = e.Id > 0 ? e.Id : null,
                        EmployeeId = e.IdEmpleado > 0 ? e.IdEmpleado : null,
                        CodigoEmpleado = e.CodigoEmpleado,
                        NombreEmpleado = e.NombreEmpleado,
                        Area = string.IsNullOrWhiteSpace(e.Area) ? "Operaciones" : e.Area.Trim(),
                        Cargo = string.IsNullOrWhiteSpace(e.Puesto) ? "6-Operador de almacen" : e.Puesto.Trim(),
                        HoraEntradaJornada = NormalizarFormatoHora(e.HoraEntrada),
                        HoraSalidaJornada = NormalizarFormatoHora(e.HoraSalida),
                        HoraInicioExtra = calc.HoraInicioExtra,
                        HoraFinExtra = calc.HoraFinExtra,
                        HorasExtrasAutorizadas = Math.Round(calc.HorasExtras, 2)
                    }, calc.ReglaResumen, false));
                }
            }

            foreach (var m in manualesHoy)
            {
                var empEnJornada = empleadosJornada.FirstOrDefault(e => e.CodigoEmpleado == m.Dto.CodigoEmpleado);
                if (empEnJornada != null)
                {
                    if (empEnJornada.Id > 0)
                        m.Dto.MovimientoDiarioId = empEnJornada.Id;
                    if (empEnJornada.IdEmpleado > 0 && !m.Dto.EmployeeId.HasValue)
                        m.Dto.EmployeeId = empEnJornada.IdEmpleado;
                    if (!string.IsNullOrWhiteSpace(empEnJornada.Area))
                        m.Dto.Area = empEnJornada.Area.Trim();
                    if (!string.IsNullOrWhiteSpace(empEnJornada.Puesto))
                        m.Dto.Cargo = empEnJornada.Puesto.Trim();
                    if (!string.IsNullOrWhiteSpace(empEnJornada.HoraEntrada) && empEnJornada.HoraEntrada != "--:--")
                        m.Dto.HoraEntradaJornada = NormalizarFormatoHora(empEnJornada.HoraEntrada);
                    if (!string.IsNullOrWhiteSpace(empEnJornada.HoraSalida) && empEnJornada.HoraSalida != "--:--")
                        m.Dto.HoraSalidaJornada = NormalizarFormatoHora(empEnJornada.HoraSalida);
                }

                string cliMan = string.IsNullOrWhiteSpace(m.Cliente) ? "SINSA" : m.Cliente.Trim();
                itemsConExtras.Add((cliMan, m.Dto, string.IsNullOrWhiteSpace(m.Dto.MotivoIndividual) ? "Manual" : m.Dto.MotivoIndividual!, true));
            }

            var codsYaIncluidos = itemsConExtras.Select(x => x.Dto.CodigoEmpleado).ToHashSet();
            foreach (var autDb in autorizacionesHeLista.Where(a => a.Fecha.Date == fRef && a.Estado != "ANULADA" && a.CodigoEmpleado > 0))
            {
                if (codsYaIncluidos.Contains(autDb.CodigoEmpleado))
                    continue;

                codsYaIncluidos.Add(autDb.CodigoEmpleado);
                string cliBd = string.IsNullOrWhiteSpace(autDb.ClienteNombre) ? "SINSA" : autDb.ClienteNombre.Trim();
                itemsConExtras.Add((cliBd, new ColaboradorAutorizacionItemDto
                {
                    MovimientoDiarioId = autDb.MovimientoDiarioId,
                    EmployeeId = autDb.EmployeeId,
                    CodigoEmpleado = autDb.CodigoEmpleado,
                    NombreEmpleado = autDb.NombreEmpleado,
                    Area = string.IsNullOrWhiteSpace(autDb.Area) ? "Operaciones" : autDb.Area.Trim(),
                    Cargo = string.IsNullOrWhiteSpace(autDb.Cargo) ? "6-Operador de almacen" : autDb.Cargo.Trim(),
                    HoraEntradaJornada = autDb.HoraEntradaJornada ?? string.Empty,
                    HoraSalidaJornada = autDb.HoraSalidaJornada ?? string.Empty,
                    HoraInicioExtra = string.IsNullOrWhiteSpace(autDb.HoraInicioExtra) ? "17:00" : autDb.HoraInicioExtra.Trim(),
                    HoraFinExtra = string.IsNullOrWhiteSpace(autDb.HoraFinExtra) ? "19:00" : autDb.HoraFinExtra.Trim(),
                    HorasExtrasAutorizadas = Math.Round(autDb.HorasExtrasAutorizadas, 2)
                }, "Manual", true));
            }

            int idx = 1;
            var nuevasTarjetas = new List<GrupoHorasExtrasAreaItem>();
            foreach (var g in itemsConExtras
                .GroupBy(x => new
                {
                    Cliente = (string.IsNullOrWhiteSpace(x.Cliente) ? "SINSA" : x.Cliente.Trim()).ToUpperInvariant(),
                    Area = x.Dto.Area.ToUpperInvariant(),
                    Puesto = x.Dto.Cargo.ToUpperInvariant(),
                    HoraIni = x.Dto.HoraInicioExtra,
                    HoraFin = x.Dto.HoraFinExtra,
                    Horas = x.Dto.HorasExtrasAutorizadas
                })
                .OrderBy(g => g.Key.Cliente)
                .ThenBy(g => g.Key.Area)
                .ThenBy(g => g.Key.Puesto)
                .ThenBy(g => g.Key.HoraIni)
                .ThenBy(g => g.Key.HoraFin))
            {
                var first = g.First();
                string cliNombre = string.IsNullOrWhiteSpace(first.Cliente) ? "SINSA" : first.Cliente.Trim();
                int opId = ResolverOperacionIdCliente(cliNombre);
                var codsTarjeta = g.Select(x => x.Dto.CodigoEmpleado).ToHashSet();

                var autExistente = autorizacionesHeLista.FirstOrDefault(a =>
                    a.Fecha.Date == fRef
                    && a.Estado != "ANULADA"
                    && codsTarjeta.Contains(a.CodigoEmpleado)
                    && (string.IsNullOrWhiteSpace(a.HoraInicioExtra) || string.Equals(a.HoraInicioExtra.Trim(), g.Key.HoraIni, StringComparison.OrdinalIgnoreCase)));

                if (autExistente == null)
                {
                    autExistente = autorizacionesHeLista.FirstOrDefault(a =>
                        a.Fecha.Date == fRef
                        && a.Estado != "ANULADA"
                        && codsTarjeta.Contains(a.CodigoEmpleado));
                }

                var itemTarjeta = new GrupoHorasExtrasAreaItem
                {
                    Index = idx++,
                    OperacionId = opId,
                    Cliente = cliNombre,
                    Area = first.Dto.Area,
                    Puesto = first.Dto.Cargo,
                    HoraInicioExtra = g.Key.HoraIni,
                    HoraFinExtra = g.Key.HoraFin,
                    HorasExtrasPorPersona = g.Key.Horas,
                    ReglaResumen = string.Join(" / ", g.Select(x => x.ReglaTxt).Distinct()),
                    EsManual = g.Any(x => x.EsManual),
                    Colaboradores = g.Select(x => x.Dto).OrderBy(c => c.CodigoEmpleado).ToList(),
                    AutorizacionId = autExistente?.Id,
                    TokenRecibo = autExistente?.TokenRecibo ?? string.Empty,
                    TicketRedmineId = autExistente?.TicketRedmineId,
                    EstadoTicket = autExistente != null
                        ? (autExistente.Estado == "RESUELTA" || autExistente.SincronizadoOdoo ? "EN ODOO" : autExistente.Estado)
                        : string.Empty,
                    SupervisorNombre = autExistente?.SupervisorNombre ?? string.Empty,
                    SupervisorCargo = string.IsNullOrWhiteSpace(autExistente?.SupervisorCargo) ? "Supervisor del Cliente" : autExistente!.SupervisorCargo,
                    TieneFirmaSupervisorBd = autExistente?.TieneFirmaSupervisor == true,
                    GerenteNombre = autExistente?.GerenteNombre ?? string.Empty,
                    GerenteCargo = string.IsNullOrWhiteSpace(autExistente?.GerenteCargo) ? "Gerente del Cliente" : autExistente!.GerenteCargo,
                    TieneFirmaGerenteBd = autExistente?.TieneFirmaGerente == true
                };

                if (firmasPorTarjetaHe.TryGetValue(itemTarjeta.KeyTarjeta, out var firmaLocal))
                {
                    if (!string.IsNullOrWhiteSpace(firmaLocal.SupervisorNombre))
                        itemTarjeta.SupervisorNombre = firmaLocal.SupervisorNombre;
                    if (!string.IsNullOrWhiteSpace(firmaLocal.SupervisorCargo))
                        itemTarjeta.SupervisorCargo = firmaLocal.SupervisorCargo;
                    if (!string.IsNullOrWhiteSpace(firmaLocal.FirmaSupervisorBase64))
                        itemTarjeta.FirmaSupervisorBase64 = firmaLocal.FirmaSupervisorBase64;
                    if (!string.IsNullOrWhiteSpace(firmaLocal.GerenteNombre))
                        itemTarjeta.GerenteNombre = firmaLocal.GerenteNombre;
                    if (!string.IsNullOrWhiteSpace(firmaLocal.GerenteCargo))
                        itemTarjeta.GerenteCargo = firmaLocal.GerenteCargo;
                    if (!string.IsNullOrWhiteSpace(firmaLocal.FirmaGerenteBase64))
                        itemTarjeta.FirmaGerenteBase64 = firmaLocal.FirmaGerenteBase64;
                }

                if (observacionesPorAreaHe.TryGetValue(itemTarjeta.KeyTarjeta, out var obsGuardada) && !string.IsNullOrWhiteSpace(obsGuardada))
                {
                    itemTarjeta.Observaciones = obsGuardada;
                }
                else if (!string.IsNullOrWhiteSpace(autExistente?.Motivo))
                {
                    itemTarjeta.Observaciones = autExistente.Motivo;
                }

                nuevasTarjetas.Add(itemTarjeta);
            }

            gruposHorasExtras = nuevasTarjetas;
        }

        protected List<GrupoHorasExtrasAreaItem> GruposHorasExtrasFiltrados
        {
            get
            {
                if (mostrarTodosClientesHelpdesk)
                    return gruposHorasExtras;

                string cli = ClienteActivoAsistenciasSeguro;
                if (string.IsNullOrWhiteSpace(cli))
                    return gruposHorasExtras;

                return gruposHorasExtras
                    .Where(g => string.Equals(g.Cliente?.Trim(), cli.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        protected List<GrupoHorasExtrasAreaItem> GruposHorasExtrasJornadaPorArea => GruposHorasExtrasFiltrados;

        protected int TotalGruposHePendientes => GruposHorasExtrasFiltrados.Count(g => !g.Sincronizado && g.Colaboradores.Count > 0);

        protected int TotalColaboradoresConHeCliente => GruposHorasExtrasFiltrados.Sum(g => g.Colaboradores.Count);

        protected int TotalBoletasHeCliente => gruposHorasExtras.Count > 0 ? gruposHorasExtras.Count : BoletasHorasExtrasAgrupadas.Count;

        protected async Task AbrirDialogoEditarFirmaTarjeta(GrupoHorasExtrasAreaItem grp, string tipoInicial = "SUPERVISOR")
        {
            if (grp == null) return;

            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            var autDto = new AutorizacionHorasExtrasDto
            {
                Id = grp.AutorizacionId ?? 0,
                ClienteNombre = grp.Cliente,
                Area = grp.Area,
                Cargo = grp.Puesto,
                Fecha = fRef,
                HoraInicioExtra = grp.HoraInicioMin,
                HoraFinExtra = grp.HoraFinMax,
                HorasExtrasAutorizadas = grp.HorasExtrasPorPersona,
                SupervisorNombre = grp.SupervisorNombre,
                SupervisorCargo = grp.SupervisorCargo,
                FirmaSupervisorBase64 = grp.TieneFirmaSupervisor ? (grp.FirmaSupervisorBase64 ?? "HAS_SIG") : null,
                GerenteNombre = grp.GerenteNombre,
                GerenteCargo = grp.GerenteCargo,
                FirmaGerenteBase64 = grp.TieneFirmaGerente ? (grp.FirmaGerenteBase64 ?? "HAS_SIG") : null,
                TicketRedmineId = grp.TicketRedmineId
            };

            // Si ya existe en BD pero aún no tenemos el PNG en memoria para previsualización, cargarlo opcionalmente
            string? prevSup = grp.FirmaSupervisorBase64;
            string? prevGer = grp.FirmaGerenteBase64;
            if ((grp.TieneFirmaSupervisorBd || grp.TieneFirmaGerenteBd) && !string.IsNullOrWhiteSpace(grp.TokenRecibo)
                && (string.IsNullOrWhiteSpace(prevSup) || string.IsNullOrWhiteSpace(prevGer)))
            {
                try
                {
                    var recResp = await _odooService.GetReciboAutorizacionPorToken(grp.TokenRecibo);
                    if (recResp?.Model != null)
                    {
                        if (string.IsNullOrWhiteSpace(prevSup) && !string.IsNullOrWhiteSpace(recResp.Model.FirmaSupervisorBase64))
                            prevSup = recResp.Model.FirmaSupervisorBase64;
                        if (string.IsNullOrWhiteSpace(prevGer) && !string.IsNullOrWhiteSpace(recResp.Model.FirmaGerenteBase64))
                            prevGer = recResp.Model.FirmaGerenteBase64;
                    }
                }
                catch { }
            }

            var parameters = new DialogParameters
            {
                ["Autorizacion"] = autDto,
                ["TipoFirma"] = tipoInicial,
                ["ResumenTarjeta"] = $"{grp.Cliente} • {grp.Area} • {grp.Puesto}",
                ["DetalleHorarioTarjeta"] = $"{grp.HoraInicioMin} - {grp.HoraFinMax} • {grp.HorasExtrasPorPersona:N2} hrs c/u ({grp.Colaboradores.Count} pers.)",
                ["ObservacionesIniciales"] = grp.Observaciones,
                ["TicketRedmineId"] = grp.TicketRedmineId,
                ["FirmaSupervisorPrevia"] = prevSup,
                ["FirmaGerentePrevia"] = prevGer
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<DialogFirmarReciboHe>("Editar Tarjeta y Firma", parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is DialogFirmarReciboHe.ResultadoFirmaTarjetaHe resFirma)
            {
                string oldKey = grp.KeyTarjeta;

                // Si la tarjeta aún no se ha enviado a Helpdesk, aplicar el horario extra y observaciones editadas
                if (!grp.Sincronizado && !string.IsNullOrWhiteSpace(resFirma.HoraInicioExtra) && !string.IsNullOrWhiteSpace(resFirma.HoraFinExtra) && resFirma.HorasExtrasAutorizadas > 0m)
                {
                    foreach (var col in grp.Colaboradores)
                    {
                        string baseJ = $"{col.HoraEntradaJornada}-{col.HoraSalidaJornada}";
                        edicionesHorarioHePorEmpleado[col.CodigoEmpleado] = (baseJ, resFirma.HoraInicioExtra, resFirma.HoraFinExtra, resFirma.HorasExtrasAutorizadas);

                        foreach (var itemMan in itemsHorasExtrasManuales.Where(m => m.Fecha.Date == fRef && m.Dto.CodigoEmpleado == col.CodigoEmpleado))
                        {
                            itemMan.Dto.HoraInicioExtra = resFirma.HoraInicioExtra;
                            itemMan.Dto.HoraFinExtra = resFirma.HoraFinExtra;
                            itemMan.Dto.HorasExtrasAutorizadas = resFirma.HorasExtrasAutorizadas;
                        }
                    }
                    grp.HoraInicioExtra = resFirma.HoraInicioExtra;
                    grp.HoraFinExtra = resFirma.HoraFinExtra;
                    grp.HorasExtrasPorPersona = resFirma.HorasExtrasAutorizadas;
                }

                string newKey = grp.KeyTarjeta;
                if (!string.Equals(oldKey, newKey, StringComparison.OrdinalIgnoreCase) && firmasPorTarjetaHe.TryGetValue(oldKey, out var oldSlot))
                {
                    firmasPorTarjetaHe[newKey] = oldSlot;
                }

                observacionesPorAreaHe[newKey] = resFirma.Observaciones ?? string.Empty;
                grp.Observaciones = resFirma.Observaciones ?? string.Empty;

                if (resFirma.ActualizoFirma)
                {
                    if (!firmasPorTarjetaHe.TryGetValue(newKey, out var slot))
                    {
                        slot = new FirmaLocalTarjetaHe
                        {
                            SupervisorNombre = grp.SupervisorNombre,
                            SupervisorCargo = grp.SupervisorCargo,
                            FirmaSupervisorBase64 = grp.FirmaSupervisorBase64,
                            GerenteNombre = grp.GerenteNombre,
                            GerenteCargo = grp.GerenteCargo,
                            FirmaGerenteBase64 = grp.FirmaGerenteBase64
                        };
                        firmasPorTarjetaHe[newKey] = slot;
                    }

                    if (string.Equals(resFirma.TipoFirma, "GERENTE", StringComparison.OrdinalIgnoreCase))
                    {
                        slot.GerenteNombre = resFirma.NombreFirmante;
                        slot.GerenteCargo = resFirma.CargoFirmante;
                        slot.FirmaGerenteBase64 = resFirma.FirmaBase64;
                        grp.GerenteNombre = resFirma.NombreFirmante;
                        grp.GerenteCargo = resFirma.CargoFirmante;
                        grp.FirmaGerenteBase64 = resFirma.FirmaBase64;
                    }
                    else
                    {
                        slot.SupervisorNombre = resFirma.NombreFirmante;
                        slot.SupervisorCargo = resFirma.CargoFirmante;
                        slot.FirmaSupervisorBase64 = resFirma.FirmaBase64;
                        grp.SupervisorNombre = resFirma.NombreFirmante;
                        grp.SupervisorCargo = resFirma.CargoFirmante;
                        grp.FirmaSupervisorBase64 = resFirma.FirmaBase64;
                    }
                }

                if (grp.AutorizacionId.HasValue && grp.AutorizacionId.Value > 0)
                {
                    await CargarAutorizacionesHorasExtras();
                }
                else
                {
                    ActualizarGruposHorasExtrasInterno();
                }

                StateHasChanged();
            }
        }

        protected async Task EnviarGrupoAreaHorasExtras(GrupoHorasExtrasAreaItem grupo)
        {
            if (grupo == null || !grupo.Colaboradores.Any()) return;

            if (GrupoTieneAsistenciasSinGuardar(grupo))
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque los colaboradores de esta tarjeta tienen asistencias pendientes de guardar en Base de Datos.\n\nPor favor, haz clic en 'GUARDAR EN BD' antes de enviar la tarjeta.",
                    yesText: "Entendido");
                return;
            }

            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            string obs = !string.IsNullOrWhiteSpace(grupo.Observaciones)
                ? grupo.Observaciones.Trim()
                : ObtenerObservacionAreaHe(grupo.KeyTarjeta);
            if (string.IsNullOrWhiteSpace(obs))
                obs = $"Horas extras según regla ({grupo.ReglaResumen})";

            var req = new CrearAutorizacionHorasExtrasRequestDto
            {
                OperacionId = grupo.OperacionId,
                ClienteNombre = grupo.Cliente,
                Fecha = fRef.ToString("yyyy-MM-dd"),
                HoraInicioExtra = grupo.HoraInicioMin,
                HoraFinExtra = grupo.HoraFinMax,
                HorasExtrasAutorizadas = grupo.HorasExtrasPorPersona > 0 ? grupo.HorasExtrasPorPersona : Math.Round(grupo.Colaboradores.Average(c => c.HorasExtrasAutorizadas), 2),
                Motivo = obs.Trim(),
                SupervisorNombre = grupo.SupervisorNombre,
                SupervisorCargo = grupo.SupervisorCargo,
                FirmaSupervisorBase64 = grupo.FirmaSupervisorBase64,
                GerenteNombre = grupo.GerenteNombre,
                GerenteCargo = grupo.GerenteCargo,
                FirmaGerenteBase64 = grupo.FirmaGerenteBase64,
                UsuarioRegistro = UsuarioRedmineEfectivo,
                BaseUrlPortal = _navigationManager.BaseUri.TrimEnd('/'),
                Colaboradores = grupo.Colaboradores
            };

            enviandoTicketsHe = true;
            StateHasChanged();
            try
            {
                var resp = await _odooService.CrearAutorizacionHorasExtras(req);
                if (resp != null && !resp.Respuesta.ExisteError && resp.Model != null && resp.Model.Exitosos > 0)
                {
                    int? tck = resp.Model.Detalles.FirstOrDefault()?.TicketRedmineId;
                    _snackbar.Add($"Ticket #{tck} generado en Helpdesk: {grupo.Area} • {grupo.Puesto} ({grupo.HoraInicioMin}-{grupo.HoraFinMax} | {grupo.HorasExtrasPorPersona:N2}h c/u).", Severity.Success);
                    await CargarAutorizacionesHorasExtras();
                }
                else
                {
                    string err = resp?.Model?.Detalles.FirstOrDefault()?.Mensaje ?? resp?.Respuesta?.MensajeError ?? "No se pudo crear el ticket.";
                    _snackbar.Add(err, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
            finally
            {
                enviandoTicketsHe = false;
                StateHasChanged();
            }
        }

        protected async Task EnviarTodosGruposHorasExtras()
        {
            ActualizarGruposHorasExtrasInterno();
            var pendientes = GruposHorasExtrasFiltrados.Where(g => !g.Sincronizado && g.Colaboradores.Count > 0).ToList();
            if (!pendientes.Any())
            {
                _snackbar.Add("Todas las tarjetas de Horas Extras del cliente seleccionado ya tienen ticket en Helpdesk.", Severity.Info);
                return;
            }

            if (pendientes.Any(GrupoTieneAsistenciasSinGuardar))
            {
                await _dialogService.ShowMessageBox(
                    "Guardar en Base de Datos Requerido",
                    "No se puede enviar a Helpdesk porque hay asistencias pendientes de guardar en Base de Datos.\n\nPor favor, haz clic en 'GUARDAR EN BD' antes de enviar las tarjetas de Horas Extras.",
                    yesText: "Entendido");
                return;
            }

            int totalTarjetas = pendientes.Count;
            int totalEmpleados = pendientes.Sum(g => g.Colaboradores.Count);
            decimal totalHoras = Math.Round(pendientes.Sum(g => g.TotalHorasExtras), 2);
            bool? confirm = await _dialogService.ShowMessageBox(
                "Confirmar Envío de Horas Extras a Helpdesk",
                $"¿Deseas enviar {totalTarjetas} tarjeta(s) pendiente(s) de Horas Extras ({totalEmpleados} colaborador(es), {totalHoras:N2} hrs extra totales) a Helpdesk?",
                yesText: "Sí, Enviar a Helpdesk",
                cancelText: "Cancelar");

            if (confirm != true) return;

            enviandoTicketsHe = true;
            StateHasChanged();
            int creados = 0;
            try
            {
                DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
                foreach (var grupo in pendientes)
                {
                    string obs = !string.IsNullOrWhiteSpace(grupo.Observaciones)
                        ? grupo.Observaciones.Trim()
                        : ObtenerObservacionAreaHe(grupo.KeyTarjeta);
                    if (string.IsNullOrWhiteSpace(obs))
                        obs = $"Horas extras según regla ({grupo.ReglaResumen})";

                    var req = new CrearAutorizacionHorasExtrasRequestDto
                    {
                        OperacionId = grupo.OperacionId,
                        ClienteNombre = grupo.Cliente,
                        Fecha = fRef.ToString("yyyy-MM-dd"),
                        HoraInicioExtra = grupo.HoraInicioMin,
                        HoraFinExtra = grupo.HoraFinMax,
                        HorasExtrasAutorizadas = grupo.HorasExtrasPorPersona > 0 ? grupo.HorasExtrasPorPersona : Math.Round(grupo.Colaboradores.Average(c => c.HorasExtrasAutorizadas), 2),
                        Motivo = obs.Trim(),
                        SupervisorNombre = grupo.SupervisorNombre,
                        SupervisorCargo = grupo.SupervisorCargo,
                        FirmaSupervisorBase64 = grupo.FirmaSupervisorBase64,
                        GerenteNombre = grupo.GerenteNombre,
                        GerenteCargo = grupo.GerenteCargo,
                        FirmaGerenteBase64 = grupo.FirmaGerenteBase64,
                        UsuarioRegistro = UsuarioRedmineEfectivo,
                        BaseUrlPortal = _navigationManager.BaseUri.TrimEnd('/'),
                        Colaboradores = grupo.Colaboradores
                    };

                    var resp = await _odooService.CrearAutorizacionHorasExtras(req);
                    if (resp != null && !resp.Respuesta.ExisteError && resp.Model != null && resp.Model.Exitosos > 0)
                    {
                        creados++;
                    }
                }

                await CargarAutorizacionesHorasExtras();
                if (creados > 0)
                {
                    _snackbar.Add($"🚀 {creados} ticket(s) de Horas Extras creados con éxito en Helpdesk.", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al enviar tickets: {ex.Message}", Severity.Error);
            }
            finally
            {
                enviandoTicketsHe = false;
                StateHasChanged();
            }
        }

        protected bool ClientePermiteHorasExtras(string? clienteNombre)
        {
            if (string.IsNullOrWhiteSpace(clienteNombre)) return false;
            string cli = clienteNombre.Trim();

            var asig = misClientesAsignados.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(a.ClienteNombre) &&
                string.Equals(a.ClienteNombre.Trim(), cli, StringComparison.OrdinalIgnoreCase));
            if (asig != null && asig.AplicaHorasExtras)
                return true;

            var cfg = configClientesHe.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.ClienteNombre) &&
                (string.Equals(c.ClienteNombre.Trim(), cli, StringComparison.OrdinalIgnoreCase) ||
                 cli.Contains(c.ClienteNombre.Trim(), StringComparison.OrdinalIgnoreCase) ||
                 c.ClienteNombre.Trim().Contains(cli, StringComparison.OrdinalIgnoreCase)));
            if (cfg != null)
                return cfg.PermiteAutorizacionHorasExtras;

            return cli.Contains("sinsa", StringComparison.OrdinalIgnoreCase);
        }

        protected bool ClienteActualPermiteHorasExtras
        {
            get
            {
                string cli = ClienteActivoAsistenciasSeguro;
                if (!string.IsNullOrWhiteSpace(cli))
                    return ClientePermiteHorasExtras(cli);

                return misClientesAsignados.Any(a => a.AplicaHorasExtras)
                    || empleadosJornada.Any(e => ClientePermiteHorasExtras(e.Cliente));
            }
        }

        protected int ResolverOperacionIdCliente(string? clienteNombre)
        {
            if (!string.IsNullOrWhiteSpace(clienteNombre))
            {
                string cli = clienteNombre.Trim();
                var opExacta = operacionesList.FirstOrDefault(o =>
                    !string.IsNullOrWhiteSpace(o.nombre) &&
                    (string.Equals(o.nombre.Trim(), cli, StringComparison.OrdinalIgnoreCase) ||
                     cli.Contains(o.nombre.Trim(), StringComparison.OrdinalIgnoreCase) ||
                     o.nombre.Trim().Contains(cli, StringComparison.OrdinalIgnoreCase)));
                if (opExacta != null && opExacta.id > 0)
                    return opExacta.id;

                var cfg = configClientesHe.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.ClienteNombre) &&
                    string.Equals(c.ClienteNombre.Trim(), cli, StringComparison.OrdinalIgnoreCase));
                if (cfg != null && cfg.OperacionId > 0)
                    return cfg.OperacionId;
            }
            return selectedOperacionId ?? 23;
        }

        protected async Task CargarConfigHorasExtrasClientes()
        {
            try
            {
                var resp = await _odooService.GetConfigClientesHorasExtras();
                if (resp?.Model != null)
                {
                    configClientesHe = resp.Model.ToList();
                    ActualizarGruposHorasExtrasInterno();
                }
            }
            catch { }
        }

        protected async Task CargarAutorizacionesHorasExtras()
        {
            try
            {
                cargandoAutorizacionesHe = true;
                DateTime ini = (fechaDesdeFiltroHe ?? fechaSeleccionada ?? DateTime.Today).Date;
                DateTime fin = (fechaHastaFiltroHe ?? fechaSeleccionada ?? DateTime.Today).Date;
                if (fin < ini) fin = ini;

                var resp = await _odooService.GetAutorizacionesHorasExtras(ini, fin, incluirFirmas: false);
                autorizacionesHeLista = resp?.Model?.ToList() ?? new List<AutorizacionHorasExtrasDto>();
                ActualizarGruposHorasExtrasInterno();
            }
            catch
            {
                autorizacionesHeLista = new List<AutorizacionHorasExtrasDto>();
                ActualizarGruposHorasExtrasInterno();
            }
            finally
            {
                cargandoAutorizacionesHe = false;
            }
        }

        protected AutorizacionHorasExtrasDto? ObtenerAutorizacionHeEmpleado(int codigoEmpleado)
        {
            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            return autorizacionesHeLista.FirstOrDefault(a =>
                a.CodigoEmpleado == codigoEmpleado &&
                a.Fecha.Date == fRef);
        }

        protected List<AutorizacionHorasExtrasDto> BoletasHorasExtrasAgrupadas
        {
            get
            {
                var filtradas = autorizacionesHeLista
                    .Where(a => PerteneceAClienteSeleccionado(a.ClienteNombre))
                    .ToList();

                var agrupadas = BoletaAutorizacionHeHelper.AgruparPorBoletaArea(filtradas);
                if (string.IsNullOrWhiteSpace(textoBusquedaHe))
                    return agrupadas;

                string q = textoBusquedaHe.Trim();
                return agrupadas.Where(b =>
                    (!string.IsNullOrWhiteSpace(b.Area) && b.Area.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(b.Motivo) && b.Motivo.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (b.TicketRedmineId.HasValue && b.TicketRedmineId.Value.ToString().Contains(q)) ||
                    b.ColaboradoresGrupo.Any(c =>
                        c.CodigoEmpleado.ToString().Contains(q) ||
                        (!string.IsNullOrWhiteSpace(c.NombreEmpleado) && c.NombreEmpleado.Contains(q, StringComparison.OrdinalIgnoreCase)))
                ).ToList();
            }
        }

        protected void EliminarTarjetaManualHe(GrupoHorasExtrasAreaItem grp)
        {
            if (grp == null || grp.Sincronizado) return;
            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            var cods = grp.Colaboradores.Select(c => c.CodigoEmpleado).ToHashSet();

            itemsHorasExtrasManuales.RemoveAll(m => m.Fecha.Date == fRef && cods.Contains(m.Dto.CodigoEmpleado));
            foreach (var cod in cods)
            {
                edicionesHorarioHePorEmpleado.Remove(cod);
            }
            firmasPorTarjetaHe.Remove(grp.KeyTarjeta);
            observacionesPorAreaHe.Remove(grp.KeyTarjeta);

            ActualizarGruposHorasExtrasInterno();
            _snackbar.Add("Tarjeta manual de horas extras descartada.", Severity.Info);
            StateHasChanged();
        }

        protected async Task AbrirDialogoAutorizarHorasExtras(EmpleadoJornadaItem? empInicial = null)
        {
            string cliNombre = empInicial != null && !string.IsNullOrWhiteSpace(empInicial.Cliente)
                ? empInicial.Cliente
                : ClienteActivoAsistenciasSeguro;
            if (string.IsNullOrWhiteSpace(cliNombre))
                cliNombre = "SINSA";

            int opId = ResolverOperacionIdCliente(cliNombre);
            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;

            // Incluir TODOS los colaboradores del cliente (tanto cargados en la jornada como en Mi Plantilla)
            // para poder agregar Horas Extras de forma manual a cualquier colaborador
            var mapaCandidatos = new Dictionary<int, ColaboradorAutorizacionItemDto>();

            foreach (var e in empleadosJornada.Where(e =>
                e.CodigoEmpleado > 0
                && !string.Equals(e.TurnoNombre, "Ausente", StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.Cliente?.Trim(), cliNombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                var calc = CalcularHorasExtrasSegunRegla(e, fRef);
                mapaCandidatos[e.CodigoEmpleado] = new ColaboradorAutorizacionItemDto
                {
                    MovimientoDiarioId = e.Id > 0 ? e.Id : null,
                    EmployeeId = e.IdEmpleado > 0 ? e.IdEmpleado : null,
                    CodigoEmpleado = e.CodigoEmpleado,
                    NombreEmpleado = e.NombreEmpleado,
                    Area = string.IsNullOrWhiteSpace(e.Area) ? "Operaciones" : e.Area.Trim(),
                    Cargo = string.IsNullOrWhiteSpace(e.Puesto) ? "6-Operador de almacen" : e.Puesto.Trim(),
                    HoraEntradaJornada = NormalizarFormatoHora(e.HoraEntrada),
                    HoraSalidaJornada = NormalizarFormatoHora(e.HoraSalida),
                    HoraInicioExtra = calc.HorasExtras > 0m ? calc.HoraInicioExtra : NormalizarFormatoHora(e.HoraSalida),
                    HoraFinExtra = calc.HorasExtras > 0m ? calc.HoraFinExtra : "19:00",
                    HorasExtrasAutorizadas = calc.HorasExtras > 0m ? calc.HorasExtras : 2.00m
                };
            }

            foreach (var p in miPlantillaPersonal.Where(p =>
                p.CodigoEmpleado > 0
                && (string.IsNullOrWhiteSpace(p.ClienteDefault)
                    || string.Equals(p.ClienteDefault.Trim(), cliNombre.Trim(), StringComparison.OrdinalIgnoreCase))))
            {
                if (!mapaCandidatos.ContainsKey(p.CodigoEmpleado))
                {
                    mapaCandidatos[p.CodigoEmpleado] = new ColaboradorAutorizacionItemDto
                    {
                        MovimientoDiarioId = null,
                        EmployeeId = p.IdEmpleado > 0 ? p.IdEmpleado : null,
                        CodigoEmpleado = p.CodigoEmpleado,
                        NombreEmpleado = p.NombreEmpleado,
                        Area = string.IsNullOrWhiteSpace(p.AreaDefault) ? "Operaciones" : p.AreaDefault.Trim(),
                        Cargo = string.IsNullOrWhiteSpace(p.PuestoDefault) ? "6-Operador de almacen" : p.PuestoDefault.Trim(),
                        HoraEntradaJornada = NormalizarFormatoHora(p.HoraEntradaDefault),
                        HoraSalidaJornada = NormalizarFormatoHora(p.HoraSalidaDefault),
                        HoraInicioExtra = !string.IsNullOrWhiteSpace(p.HoraSalidaDefault) ? NormalizarFormatoHora(p.HoraSalidaDefault) : "17:00",
                        HoraFinExtra = "19:00",
                        HorasExtrasAutorizadas = 2.00m
                    };
                }
            }

            var candidatos = mapaCandidatos.Values
                .OrderBy(c => c.Area)
                .ThenBy(c => c.NombreEmpleado)
                .ToList();

            var iniciales = new List<ColaboradorAutorizacionItemDto>();
            if (empInicial != null && mapaCandidatos.TryGetValue(empInicial.CodigoEmpleado, out var initCol))
            {
                iniciales.Add(initCol);
            }

            var parameters = new DialogParameters
            {
                ["OperacionId"] = opId,
                ["ClienteNombre"] = cliNombre,
                ["Fecha"] = fRef,
                ["SupervisorInicial"] = string.Empty,
                ["UsuarioRegistro"] = UsuarioRedmineEfectivo,
                ["ColaboradoresIniciales"] = iniciales,
                ["CandidatosDisponibles"] = candidatos
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<DialogAutorizarHorasExtras>("Agregar Horas Extras Manualmente", parameters, options);
            var result = await dialog.Result;
            if (result != null && !result.Canceled && result.Data is DialogAutorizarHorasExtras.ResultadoAgregarHorasExtrasManual manualRes)
            {
                var codigosAgregados = new HashSet<int>();
                foreach (var col in manualRes.Colaboradores)
                {
                    codigosAgregados.Add(col.CodigoEmpleado);
                    itemsHorasExtrasManuales.RemoveAll(m => m.Fecha.Date == fRef && m.Dto.CodigoEmpleado == col.CodigoEmpleado);
                    itemsHorasExtrasManuales.Add((cliNombre, fRef, col));

                    if (!string.IsNullOrWhiteSpace(col.HoraEntradaJornada) && !string.IsNullOrWhiteSpace(col.HoraSalidaJornada))
                    {
                        edicionesHorarioHePorEmpleado[col.CodigoEmpleado] = (
                            $"{col.HoraEntradaJornada}-{col.HoraSalidaJornada}",
                            manualRes.HoraInicioExtra,
                            manualRes.HoraFinExtra,
                            manualRes.HorasExtras);
                    }
                }

                ActualizarGruposHorasExtrasInterno();

                var tarjetasCreadas = gruposHorasExtras
                    .Where(g => !g.Sincronizado
                        && g.Colaboradores.Any(c => codigosAgregados.Contains(c.CodigoEmpleado))
                        && string.Equals(g.HoraInicioExtra, manualRes.HoraInicioExtra, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(g.HoraFinExtra, manualRes.HoraFinExtra, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var card in tarjetasCreadas)
                {
                    if (!string.IsNullOrWhiteSpace(manualRes.Observaciones))
                    {
                        observacionesPorAreaHe[card.KeyTarjeta] = manualRes.Observaciones;
                    }

                    if (!string.IsNullOrWhiteSpace(manualRes.FirmaBase64) || !string.IsNullOrWhiteSpace(manualRes.NombreAutorizador))
                    {
                        firmasPorTarjetaHe[card.KeyTarjeta] = new FirmaLocalTarjetaHe
                        {
                            SupervisorNombre = manualRes.NombreAutorizador,
                            SupervisorCargo = string.IsNullOrWhiteSpace(manualRes.CargoAutorizador) ? "Supervisor del Cliente" : manualRes.CargoAutorizador,
                            FirmaSupervisorBase64 = manualRes.FirmaBase64
                        };
                    }
                }

                ActualizarGruposHorasExtrasInterno();
                activeTabIndex = 6;
                _snackbar.Add($"Se agregó {Math.Max(1, tarjetasCreadas.Count)} tarjeta(s) manual(es) de Horas Extras ({manualRes.Colaboradores.Count} pers.).", Severity.Success);
                StateHasChanged();
            }
        }

        protected async Task AbrirDialogoFirmarAdicionalHe(AutorizacionHorasExtrasDto aut, string tipoFirma = "GERENTE")
        {
            var parameters = new DialogParameters
            {
                ["Autorizacion"] = aut,
                ["TipoFirma"] = tipoFirma
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<DialogFirmarReciboHe>("Agregar Firma en Autorización", parameters, options);
            var result = await dialog.Result;
            if (result != null && !result.Canceled)
            {
                await CargarAutorizacionesHorasExtras();
                StateHasChanged();
            }
        }

        protected async Task ImprimirBoletaIndividualHe(AutorizacionHorasExtrasDto aut)
        {
            try
            {
                AutorizacionHorasExtrasDto boletaFull = aut;
                if (!string.IsNullOrWhiteSpace(aut.TokenRecibo))
                {
                    var resp = await _odooService.GetReciboAutorizacionPorToken(aut.TokenRecibo);
                    if (resp?.Model != null)
                        boletaFull = resp.Model;
                }
                string html = BoletaAutorizacionHeHelper.ConstruirHtmlBoletaIndividual(boletaFull, false);
                await _js.InvokeVoidAsync("signaturePadInterop.printHtmlWindow", $"Recibo H.E. #{boletaFull.TicketRedmineId ?? boletaFull.Id}", html);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al preparar impresión: {ex.Message}", Severity.Error);
            }
        }

        protected async Task ImprimirLoteBoletasHe()
        {
            try
            {
                cargandoAutorizacionesHe = true;
                StateHasChanged();

                DateTime ini = (fechaDesdeFiltroHe ?? fechaSeleccionada ?? DateTime.Today).Date;
                DateTime fin = (fechaHastaFiltroHe ?? fechaSeleccionada ?? DateTime.Today).Date;
                if (fin < ini) fin = ini;

                int opId = ResolverOperacionIdCliente(ClienteActivoAsistenciasSeguro);
                var resp = await _odooService.GetAutorizacionesHorasExtras(ini, fin, opId, incluirFirmas: true);
                var lista = resp?.Model?.ToList() ?? new List<AutorizacionHorasExtrasDto>();
                if (!lista.Any())
                {
                    _snackbar.Add("No hay boletas de horas extras para imprimir en el rango seleccionado.", Severity.Info);
                    return;
                }

                string html = BoletaAutorizacionHeHelper.ConstruirHtmlLoteBoletas(lista);
                await _js.InvokeVoidAsync("signaturePadInterop.printHtmlWindow", $"Boletas Horas Extras ({ini:dd/MM/yyyy} - {fin:dd/MM/yyyy})", html);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al imprimir lote: {ex.Message}", Severity.Error);
            }
            finally
            {
                cargandoAutorizacionesHe = false;
                StateHasChanged();
            }
        }

        protected async Task SincronizarEstadosHeRedmine()
        {
            try
            {
                cargandoAutorizacionesHe = true;
                StateHasChanged();

                DateTime ini = (fechaDesdeFiltroHe ?? fechaSeleccionada ?? DateTime.Today).Date;
                DateTime fin = (fechaHastaFiltroHe ?? fechaSeleccionada ?? DateTime.Today).Date;
                int opId = ResolverOperacionIdCliente(ClienteActivoAsistenciasSeguro);

                var syncResp = await _odooService.SincronizarAutorizacionesRedmine(ini, fin, opId);
                await CargarAutorizacionesHorasExtras();

                if (syncResp?.Model != null && !string.IsNullOrWhiteSpace(syncResp.Model.Mensaje))
                {
                    _snackbar.Add(syncResp.Model.Mensaje, Severity.Success);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al sincronizar con Helpdesk: {ex.Message}", Severity.Error);
            }
            finally
            {
                cargandoAutorizacionesHe = false;
                StateHasChanged();
            }
        }

        protected async Task AnularSolicitudHe(AutorizacionHorasExtrasDto aut)
        {
            bool? confirmar = await _dialogService.ShowMessageBox(
                "Anular Autorización de Horas Extras",
                $"¿Desea anular la boleta de Horas Extras del área '{aut.Area}' ({aut.Fecha:dd/MM/yyyy})?",
                yesText: "Sí, Anular",
                cancelText: "Cancelar");

            if (confirmar == true)
            {
                var res = await _odooService.AnularAutorizacionHorasExtras(aut.Id);
                if (res?.Respuesta?.ExisteError == false)
                {
                    _snackbar.Add("Autorización de horas extras anulada.", Severity.Info);
                    await CargarAutorizacionesHorasExtras();
                    StateHasChanged();
                }
                else
                {
                    _snackbar.Add(res?.Respuesta?.MensajeError ?? "No se pudo anular.", Severity.Error);
                }
            }
        }

        // =========================================================================
        // 1. FILTRO DE VACACIONES (DESCANSADAS) Y SUBSIDIOS ACTIVOS AL CARGAR PERSONAL
        // =========================================================================
        private Dictionary<string, string> ConstruirBloqueosVacacionesSubsidios(
            IEnumerable<Modelo.ClasesGenericas.MovimientoPersonalDiarioDto>? registrosBd,
            DateTime fechaRef)
        {
            var bloqueos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            static bool EsVacOSubDescansada(string? tipoAus, string? vacTipo)
            {
                if (string.IsNullOrWhiteSpace(tipoAus)) return false;
                if (tipoAus.Contains("subsid", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (tipoAus.Contains("vacac", StringComparison.OrdinalIgnoreCase))
                    return string.IsNullOrWhiteSpace(vacTipo) || !vacTipo.Contains("pagad", StringComparison.OrdinalIgnoreCase);
                return false;
            }

            static string FormatearMotivo(string? tipoAus, DateTime? ini, DateTime? fin, DateTime fallback)
            {
                string etiqueta = !string.IsNullOrWhiteSpace(tipoAus) && tipoAus.Contains("subsid", StringComparison.OrdinalIgnoreCase)
                    ? "Subsidio"
                    : "Vacaciones";
                DateTime d1 = (ini ?? fallback).Date;
                DateTime d2 = (fin ?? d1).Date;
                return d1 == d2
                    ? $"{etiqueta} {d1:dd/MM/yyyy}"
                    : $"{etiqueta} {d1:dd/MM/yyyy} al {d2:dd/MM/yyyy}";
            }

            if (registrosBd != null)
            {
                foreach (var r in registrosBd.Where(x => !x.Asistio && EsVacOSubDescansada(x.TipoAusencia, x.VacacionesTipo)))
                {
                    DateTime dIni = (r.FechaInicioAusencia ?? fechaRef).Date;
                    DateTime dFin = (r.FechaFinAusencia ?? dIni).Date;
                    if (fechaRef.Date < dIni || fechaRef.Date > dFin)
                        continue;

                    string motivo = FormatearMotivo(r.TipoAusencia, dIni, dFin, fechaRef);
                    if (r.CodigoEmpleado > 0)
                        bloqueos[$"C:{r.CodigoEmpleado}"] = motivo;
                    if (r.IdEmpleado > 0)
                        bloqueos[$"I:{r.IdEmpleado}"] = motivo;
                }
            }

            // También considerar registros de ausencia ya presentes en memoria en la grilla actual
            foreach (var mem in empleadosJornada.Where(x => !x.Asistio && EsVacOSubDescansada(x.TipoAusencia, x.VacacionesTipo)))
            {
                DateTime dIni = (mem.FechaInicioAusencia ?? fechaRef).Date;
                DateTime dFin = (mem.FechaFinAusencia ?? dIni).Date;
                if (fechaRef.Date < dIni || fechaRef.Date > dFin)
                    continue;

                string motivo = FormatearMotivo(mem.TipoAusencia, dIni, dFin, fechaRef);
                if (mem.CodigoEmpleado > 0 && !bloqueos.ContainsKey($"C:{mem.CodigoEmpleado}"))
                    bloqueos[$"C:{mem.CodigoEmpleado}"] = motivo;
                if (mem.IdEmpleado > 0 && !bloqueos.ContainsKey($"I:{mem.IdEmpleado}"))
                    bloqueos[$"I:{mem.IdEmpleado}"] = motivo;
            }

            return bloqueos;
        }

        private static bool EstaEnVacacionesOSubsidioActivo(
            int codigoEmpleado,
            int idEmpleado,
            Dictionary<string, string> bloqueos,
            out string motivo)
        {
            if (codigoEmpleado > 0 && bloqueos.TryGetValue($"C:{codigoEmpleado}", out var mCod))
            {
                motivo = mCod;
                return true;
            }
            if (idEmpleado > 0 && bloqueos.TryGetValue($"I:{idEmpleado}", out var mId))
            {
                motivo = mId;
                return true;
            }
            motivo = string.Empty;
            return false;
        }

        // =========================================================================
        // 2. CONCILIACIÓN DIRECTA POR PATRÓN DE CLIENTE (IConciliacionClienteStrategy)
        // =========================================================================
        protected Dictionary<string, EstadoConciliacionClienteJornada> conciliacionesPorClienteFecha = new(StringComparer.OrdinalIgnoreCase);
        protected Dictionary<int, string> empleadosSinHorasExtrasConciliadosSinsa = new();
        protected string filtroConciliacionSinsa = "TODOS";

        private static string ClaveConciliacionClienteFecha(string? cliente, DateTime fecha) =>
            $"{(cliente ?? string.Empty).Trim().ToUpperInvariant()}|{fecha:yyyy-MM-dd}";

        protected (string ClienteNombre, IConciliacionClienteStrategy? Estrategia) ObtenerContextoConciliacionActual()
        {
            string cliActivo = ClienteActivoAsistenciasSeguro;
            if (!string.IsNullOrWhiteSpace(cliActivo))
            {
                int opId = ResolverOperacionIdCliente(cliActivo);
                var est = ConciliacionClienteFactory.ObtenerEstrategia(cliActivo, opId);
                return (cliActivo, est);
            }

            // Respaldo cuando aún no hay clientes cargados en la grilla
            foreach (var cAsig in NombresMisClientes)
            {
                var est = ConciliacionClienteFactory.ObtenerEstrategia(cAsig, ResolverOperacionIdCliente(cAsig));
                if (est != null)
                    return (cAsig!, est);
            }

            foreach (var op in operacionesList)
            {
                var est = ConciliacionClienteFactory.ObtenerEstrategia(op.nombre, op.id);
                if (est != null)
                    return (op.nombre, est);
            }

            return (string.Empty, null);
        }

        protected IConciliacionClienteStrategy? EstrategiaConciliacionActiva => ObtenerContextoConciliacionActual().Estrategia;

        protected string NombreClienteConciliacionActiva =>
            EstrategiaConciliacionActiva?.NombreClienteMostrar
            ?? (string.IsNullOrWhiteSpace(ClienteActivoAsistenciasSeguro) ? "Cliente" : ClienteActivoAsistenciasSeguro);

        protected bool MostrarBotonConciliarSinsa => EstrategiaConciliacionActiva != null;

        protected EstadoConciliacionClienteJornada? EstadoConciliacionClienteActual
        {
            get
            {
                var ctx = ObtenerContextoConciliacionActual();
                if (string.IsNullOrWhiteSpace(ctx.ClienteNombre))
                    return null;
                DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
                return conciliacionesPorClienteFecha.TryGetValue(ClaveConciliacionClienteFecha(ctx.ClienteNombre, fRef), out var st)
                    ? st
                    : null;
            }
        }

        protected bool TieneArchivoSinsaActivo =>
            EstadoConciliacionClienteActual != null && EstadoConciliacionClienteActual.Registros.Any();

        protected string nombreArchivoSinsaCargado => EstadoConciliacionClienteActual?.NombreArchivo ?? string.Empty;

        protected int TotalRegistrosArchivoConciliacionActivo => EstadoConciliacionClienteActual?.Registros.Count ?? 0;

        protected int TotalSinsaCuadrados
        {
            get
            {
                var st = EstadoConciliacionClienteActual;
                if (st == null) return 0;
                return empleadosJornada.Count(e =>
                    string.Equals(e.Cliente?.Trim(), st.ClienteNombre.Trim(), StringComparison.OrdinalIgnoreCase)
                    && ObtenerEstadoConciliacionSinsa(e).Estado == "OK");
            }
        }

        protected int TotalSinsaConDiferencia
        {
            get
            {
                var st = EstadoConciliacionClienteActual;
                if (st == null) return 0;
                return empleadosJornada.Count(e =>
                    string.Equals(e.Cliente?.Trim(), st.ClienteNombre.Trim(), StringComparison.OrdinalIgnoreCase)
                    && ObtenerEstadoConciliacionSinsa(e).Estado == "DIF");
            }
        }

        protected int TotalSinsaNoEnArchivo
        {
            get
            {
                var st = EstadoConciliacionClienteActual;
                if (st == null) return 0;
                return empleadosJornada.Count(e =>
                    string.Equals(e.Cliente?.Trim(), st.ClienteNombre.Trim(), StringComparison.OrdinalIgnoreCase)
                    && ObtenerEstadoConciliacionSinsa(e).Estado == "NO_EN_SINSA");
            }
        }

        private (RegistroArchivoSinsaItem? Registro, string NombreClienteMostrar) ObtenerRegistroConciliacionEmpleado(EmpleadoJornadaItem? emp)
        {
            if (emp == null || emp.CodigoEmpleado <= 0 || string.IsNullOrWhiteSpace(emp.Cliente))
                return (null, "SINSA");

            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            if (conciliacionesPorClienteFecha.TryGetValue(ClaveConciliacionClienteFecha(emp.Cliente, fRef), out var estadoCli)
                && estadoCli.RegistrosPorCodigo.TryGetValue(emp.CodigoEmpleado, out var reg))
            {
                return (reg, estadoCli.NombreClienteMostrar);
            }

            string etiqueta = ConciliacionClienteFactory.ObtenerEstrategia(emp.Cliente, ResolverOperacionIdCliente(emp.Cliente))?.NombreClienteMostrar ?? emp.Cliente;
            return (null, etiqueta);
        }

        protected (string Estado, string Detalle) ObtenerEstadoConciliacionSinsa(EmpleadoJornadaItem? emp)
        {
            if (emp == null || string.IsNullOrWhiteSpace(emp.Cliente))
                return ("", "");

            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            if (!conciliacionesPorClienteFecha.TryGetValue(ClaveConciliacionClienteFecha(emp.Cliente, fRef), out var estadoCli)
                || !estadoCli.Registros.Any())
            {
                return ("", "");
            }

            if (estadoCli.RegistrosPorCodigo.TryGetValue(emp.CodigoEmpleado, out var reg))
            {
                bool areaOk = ConciliacionSinsaHelper.NormalizarTextoBusqueda(emp.Area) == ConciliacionSinsaHelper.NormalizarTextoBusqueda(reg.Zona);
                string hEnt = NormalizarFormatoHora(emp.HoraEntrada);
                string hSal = NormalizarFormatoHora(emp.HoraSalida);
                string salTurnoConHe = reg.HoraSalidaTurnoConExtras;

                bool horarioOk = emp.Asistio && (
                    (string.Equals(hEnt, reg.TurnoInicio, StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(hSal, salTurnoConHe, StringComparison.OrdinalIgnoreCase) || string.Equals(hSal, reg.TurnoFin, StringComparison.OrdinalIgnoreCase)))
                    || (string.Equals(hEnt, reg.HoraEntradaRelojHm, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(hSal, reg.HoraSalidaRelojHm, StringComparison.OrdinalIgnoreCase)));

                string detalle = $"{estadoCli.NombreClienteMostrar}: Zona '{reg.Zona}' | Turno {reg.TurnoTexto} ({reg.HorasOrdinarias:0.##}h ord) | Reloj {reg.HoraEntradaRelojHm}-{reg.HoraSalidaRelojHm} | H.E.: {reg.HorasExtra:0.##}h";
                return (emp.Asistio && areaOk && horarioOk) ? ("OK", detalle) : ("DIF", detalle);
            }

            if (emp.Asistio)
            {
                return ("NO_EN_SINSA", $"Este colaborador figura PRESENTE en Asistencias, pero NO aparece en el archivo cargado de {estadoCli.NombreClienteMostrar}.");
            }

            return ("", "");
        }

        private void AsegurarAreaEnCatalogoMemoria(string? cliente, string? area)
        {
            if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(area)) return;
            string cliKey = areasPorCliente.Keys.FirstOrDefault(k => string.Equals(k, cliente.Trim(), StringComparison.OrdinalIgnoreCase)) ?? cliente.Trim();
            if (!areasPorCliente.TryGetValue(cliKey, out var list))
            {
                list = new List<string>();
                areasPorCliente[cliKey] = list;
            }
            if (!list.Contains(area.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                list.Add(area.Trim());
            }
        }

        protected void AplicarCuadreHorasExtrasColaborador(EmpleadoJornadaItem emp, decimal horasExtras, string? horaInicioExtra, string? horaFinExtra)
        {
            if (emp == null || emp.CodigoEmpleado <= 0) return;
            DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
            string hEntNorm = NormalizarFormatoHora(emp.HoraEntrada);
            string hSalNorm = NormalizarFormatoHora(emp.HoraSalida);
            string claveBase = $"{hEntNorm}-{hSalNorm}";
            string etiquetaEst = ConciliacionClienteFactory.ObtenerEstrategia(emp.Cliente, ResolverOperacionIdCliente(emp.Cliente))?.NombreClienteMostrar ?? emp.Cliente;

            itemsHorasExtrasManuales.RemoveAll(m => m.Fecha.Date == fRef && m.Dto.CodigoEmpleado == emp.CodigoEmpleado);

            if (horasExtras > 0m && emp.Asistio)
            {
                empleadosSinHorasExtrasConciliadosSinsa.Remove(emp.CodigoEmpleado);
                string iniEx = string.IsNullOrWhiteSpace(horaInicioExtra) ? "17:00" : NormalizarFormatoHora(horaInicioExtra);
                string finEx = string.IsNullOrWhiteSpace(horaFinExtra)
                    ? ConciliacionSinsaHelper.SumarHorasHm(iniEx, horasExtras)
                    : NormalizarFormatoHora(horaFinExtra);

                edicionesHorarioHePorEmpleado[emp.CodigoEmpleado] = (claveBase, iniEx, finEx, Math.Round(horasExtras, 2));

                itemsHorasExtrasManuales.Add((
                    string.IsNullOrWhiteSpace(emp.Cliente) ? "Sinsa" : emp.Cliente.Trim(),
                    fRef,
                    new ColaboradorAutorizacionItemDto
                    {
                        MovimientoDiarioId = emp.Id > 0 ? emp.Id : null,
                        EmployeeId = emp.IdEmpleado > 0 ? emp.IdEmpleado : null,
                        CodigoEmpleado = emp.CodigoEmpleado,
                        NombreEmpleado = emp.NombreEmpleado,
                        Area = string.IsNullOrWhiteSpace(emp.Area) ? "Operaciones" : emp.Area.Trim(),
                        Cargo = string.IsNullOrWhiteSpace(emp.Puesto) ? "6-Operador de almacen" : emp.Puesto.Trim(),
                        HoraEntradaJornada = hEntNorm,
                        HoraSalidaJornada = hSalNorm,
                        HoraInicioExtra = iniEx,
                        HoraFinExtra = finEx,
                        HorasExtrasAutorizadas = Math.Round(horasExtras, 2),
                        MotivoIndividual = $"Conciliación {etiquetaEst}"
                    }));
            }
            else
            {
                edicionesHorarioHePorEmpleado.Remove(emp.CodigoEmpleado);
                empleadosSinHorasExtrasConciliadosSinsa[emp.CodigoEmpleado] = claveBase;
            }
        }

        protected void LimpiarArchivoConciliacionSinsa()
        {
            var ctx = ObtenerContextoConciliacionActual();
            if (!string.IsNullOrWhiteSpace(ctx.ClienteNombre))
            {
                DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
                conciliacionesPorClienteFecha.Remove(ClaveConciliacionClienteFecha(ctx.ClienteNombre, fRef));
            }
            filtroConciliacionSinsa = "TODOS";
            StateHasChanged();
        }

        protected Task OnArchivoConciliacionSinsaSeleccionado(InputFileChangeEventArgs e) =>
            OnArchivoSinsaBrowserFileSelected(e.File);

        protected async Task OnArchivoSinsaBrowserFileSelected(IBrowserFile? file)
        {
            if (file == null) return;

            var ctx = ObtenerContextoConciliacionActual();
            var estrategia = ctx.Estrategia;
            if (estrategia == null)
            {
                _snackbar.Add("El cliente seleccionado aún no tiene un formato de conciliación configurado.", Severity.Info);
                return;
            }

            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var registros = estrategia.LeerArchivo(bytes, file.Name);
                if (!registros.Any())
                {
                    _snackbar.Add($"No se encontraron registros válidos en el archivo de {estrategia.NombreClienteMostrar} seleccionado.", Severity.Warning);
                    return;
                }

                var fechaDetectada = registros.Select(r => r.Fecha).FirstOrDefault(f => f.HasValue);
                DateTime fechaActiva = (fechaSeleccionada ?? DateTime.Today).Date;

                if (fechaDetectada.HasValue && fechaDetectada.Value.Date != fechaActiva)
                {
                    bool? cambiarFecha = await _dialogService.ShowMessageBox(
                        $"Fecha del Archivo {estrategia.NombreClienteMostrar} Distinta",
                        $"El archivo '{file.Name}' contiene marcaciones de la fecha {fechaDetectada.Value:dd/MM/yyyy}, mientras que la pantalla está en {fechaActiva:dd/MM/yyyy}.\n\n¿Deseas cambiar a la fecha {fechaDetectada.Value:dd/MM/yyyy} y cargar sus asistencias para conciliar?",
                        yesText: $"Sí, ir al {fechaDetectada.Value:dd/MM/yyyy}",
                        cancelText: "Mantener fecha actual");

                    if (cambiarFecha == true)
                    {
                        await OnFechaSeleccionadaChanged(fechaDetectada.Value.Date);
                    }
                }

                string cliDestino = ListaClientesAsistencia.FirstOrDefault(c => estrategia.AplicaACliente(c, ResolverOperacionIdCliente(c)))
                    ?? NombresMisClientes.FirstOrDefault(c => estrategia.AplicaACliente(c, ResolverOperacionIdCliente(c)))
                    ?? (!string.IsNullOrWhiteSpace(ctx.ClienteNombre) ? ctx.ClienteNombre : estrategia.NombreClienteMostrar);

                if (!string.Equals(ClienteActivoAsistenciasSeguro, cliDestino, StringComparison.OrdinalIgnoreCase))
                {
                    CambiarClienteAsistencias(cliDestino);
                }

                if (!catalogoEmpleadosOdoo.Any())
                {
                    try
                    {
                        var respEmp = await _odooService.GetAllActiveEmployeesForRotation();
                        if (respEmp?.Model != null)
                            catalogoEmpleadosOdoo = respEmp.Model.ToList();
                    }
                    catch { }
                }

                DateTime fechaFinal = (fechaSeleccionada ?? DateTime.Today).Date;
                string clave = ClaveConciliacionClienteFecha(cliDestino, fechaFinal);
                conciliacionesPorClienteFecha[clave] = new EstadoConciliacionClienteJornada
                {
                    ClienteNombre = cliDestino,
                    CodigoEstrategia = estrategia.CodigoEstrategia,
                    NombreClienteMostrar = estrategia.NombreClienteMostrar,
                    NombreArchivo = file.Name,
                    FechaJornada = fechaFinal,
                    Registros = registros
                };
                filtroConciliacionSinsa = "TODOS";

                await AbrirModalConciliacionSinsa();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error al leer el archivo de {estrategia.NombreClienteMostrar}: {ex.Message}", Severity.Error);
            }
        }

        protected async Task AbrirModalConciliacionSinsa()
        {
            var estadoCli = EstadoConciliacionClienteActual;
            var estrategia = EstrategiaConciliacionActiva;
            if (estadoCli == null || !estadoCli.Registros.Any()) return;

            string cliNombre = estadoCli.ClienteNombre;
            var areasCli = GetAreasParaCliente(cliNombre).ToList();
            var jornadaCli = empleadosJornada
                .Where(e => string.Equals(e.Cliente?.Trim(), cliNombre.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            var filas = ConciliacionSinsaHelper.ConstruirFilasConciliacion(
                estadoCli.Registros,
                jornadaCli,
                miPlantillaPersonal,
                catalogoEmpleadosOdoo,
                areasCli,
                estrategia,
                cliNombre);

            estadoCli.RegistrosPorCodigo.Clear();
            foreach (var f in filas.Where(x => x.Vinculado))
            {
                estadoCli.RegistrosPorCodigo[f.CodigoEmpleado!.Value] = f.Sinsa;
            }

            var codsEnArchivo = filas.Where(f => f.Vinculado).Select(f => f.CodigoEmpleado!.Value).ToHashSet();
            var noEnArchivo = jornadaCli
                .Where(e => e.Asistio && e.CodigoEmpleado > 0 && !codsEnArchivo.Contains(e.CodigoEmpleado))
                .OrderBy(e => e.NombreEmpleado)
                .ToList();

            var opcionesManuales = miPlantillaPersonal
                .Where(p => p.CodigoEmpleado > 0)
                .Select(p => (Codigo: p.CodigoEmpleado, IdEmpleado: p.IdEmpleado, Nombre: p.NombreEmpleado, Puesto: p.PuestoDefault))
                .Concat(catalogoEmpleadosOdoo
                    .Where(o => o.CodigoEmpleado.HasValue && o.CodigoEmpleado.Value > 0)
                    .Select(o => (Codigo: o.CodigoEmpleado!.Value, IdEmpleado: o.Id, Nombre: o.Name, Puesto: "6-Operador de almacen")))
                .GroupBy(x => x.Codigo)
                .Select(g => g.First())
                .OrderBy(x => x.Nombre)
                .ToList();

            var parameters = new DialogParameters
            {
                ["NombreClienteConciliacion"] = estadoCli.NombreClienteMostrar,
                ["NombreArchivo"] = estadoCli.NombreArchivo,
                ["FechaJornada"] = estadoCli.FechaJornada,
                ["Filas"] = filas,
                ["NoEnArchivoSinsa"] = noEnArchivo,
                ["AreasCatalogoSinsa"] = areasCli,
                ["OpcionesVinculacionManual"] = opcionesManuales
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraLarge, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<DialogConciliarSinsa>($"Conciliación con Archivo {estadoCli.NombreClienteMostrar}", parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is ResultadoDialogConciliarSinsa res)
            {
                estadoCli.RegistrosPorCodigo.Clear();
                foreach (var f in res.Filas.Where(x => x.Vinculado))
                {
                    estadoCli.RegistrosPorCodigo[f.CodigoEmpleado!.Value] = f.Sinsa;
                }

                if (res.AplicarCambiosMasivos)
                {
                    await AplicarResultadoConciliacionCliente(res, cliNombre, estadoCli.NombreClienteMostrar, estrategia);
                }
                else
                {
                    _snackbar.Add($"Indicadores de conciliación {estadoCli.NombreClienteMostrar} activos en la lista de Asistencia.", Severity.Info);
                    StateHasChanged();
                }
            }
        }

        private async Task AplicarResultadoConciliacionCliente(
            ResultadoDialogConciliarSinsa res,
            string cliNombre,
            string etiquetaCliente,
            IConciliacionClienteStrategy? estrategia)
        {
            int actualizados = 0;
            int nuevosAgregados = 0;
            int conHorasExtras = 0;

            foreach (var fila in res.Filas.Where(f => f.Vinculado && f.SeleccionadoAplicar))
            {
                int cod = fila.CodigoEmpleado!.Value;
                var emp = empleadosJornada.FirstOrDefault(e => e.CodigoEmpleado == cod);
                var (hEnt, hSal, comidaStr) = fila.ObtenerHorarioDestino();
                string areaFinal = string.IsNullOrWhiteSpace(fila.AreaElegida) ? fila.Sinsa.Zona : fila.AreaElegida.Trim();
                AsegurarAreaEnCatalogoMemoria(cliNombre, areaFinal);

                string turnoFinal = "Diurno";
                if (TimeSpan.TryParse(hEnt, out var tEnt))
                {
                    if (tEnt.Hours >= 18) turnoFinal = "Nocturno";
                    else if (tEnt.Hours >= 13) turnoFinal = "Mixto";
                }

                if (emp == null)
                {
                    var pl = miPlantillaPersonal.FirstOrDefault(p => p.CodigoEmpleado == cod);
                    string puestoSug = estrategia != null
                        ? estrategia.SugerirPuestoBussersa(fila.Sinsa.PuestoSinsa)
                        : ConciliacionSinsaHelper.SugerirPuestoBussersa(fila.Sinsa.PuestoSinsa);

                    emp = new EmpleadoJornadaItem
                    {
                        Id = 0,
                        IdEmpleado = fila.IdEmpleado ?? pl?.IdEmpleado ?? 0,
                        CodigoEmpleado = cod,
                        NombreEmpleado = fila.NombreEmpleadoBussersa,
                        Cliente = cliNombre,
                        Area = areaFinal,
                        Puesto = !string.IsNullOrWhiteSpace(fila.PuestoBussersa)
                            ? fila.PuestoBussersa
                            : (!string.IsNullOrWhiteSpace(pl?.PuestoDefault) ? pl!.PuestoDefault : puestoSug),
                        TurnoNombre = turnoFinal,
                        HoraEntrada = hEnt,
                        HoraSalida = hSal,
                        TiempoComida = comidaStr,
                        VtcoTrasp = pl?.VtcoTraspDefault ?? 0m,
                        VtcoAlim = pl?.VtcoAlimDefault ?? 0m,
                        BonoBnc = pl?.BonoBncDefault ?? 0m,
                        TipoEmpleado = !string.IsNullOrWhiteSpace(pl?.TipoEmpleado) ? pl!.TipoEmpleado : "Permanente",
                        ClienteOrigenNomina = pl?.ClienteOrigenNomina ?? string.Empty,
                        Asistio = true,
                        Modificado = true
                    };
                    empleadosJornada.Add(emp);
                    nuevosAgregados++;
                }
                else
                {
                    emp.Cliente = cliNombre;
                    emp.Area = areaFinal;
                    emp.Asistio = true;
                    emp.HoraEntrada = hEnt;
                    emp.HoraSalida = hSal;
                    emp.TiempoComida = comidaStr;
                    emp.TurnoNombre = turnoFinal;
                    if (string.IsNullOrWhiteSpace(emp.Puesto))
                        emp.Puesto = fila.PuestoBussersa;
                    emp.Modificado = true;
                    actualizados++;
                }

                AplicarCuadreHorasExtrasColaborador(
                    emp,
                    fila.HorasExtrasElegidas,
                    fila.HoraInicioExtraElegida,
                    fila.HoraFinExtraElegida);

                if (fila.HorasExtrasElegidas > 0m)
                    conHorasExtras++;

                AplicarReglasViaticosEnMemoria(emp);
            }

            if (res.CodigosNoEnSinsaMarcarAusentes != null && res.CodigosNoEnSinsaMarcarAusentes.Any())
            {
                var setAus = res.CodigosNoEnSinsaMarcarAusentes.ToHashSet();
                DateTime fRef = (fechaSeleccionada ?? DateTime.Today).Date;
                foreach (var empAus in empleadosJornada.Where(e => setAus.Contains(e.CodigoEmpleado)))
                {
                    empAus.Asistio = false;
                    empAus.HoraEntrada = "--:--";
                    empAus.HoraSalida = "--:--";
                    empAus.TiempoComida = "0";
                    empAus.VtcoTrasp = 0m;
                    empAus.VtcoAlim = 0m;
                    empAus.BonoBnc = 0m;
                    empAus.TurnoNombre = "Ausente";
                    empAus.Modificado = true;
                    itemsHorasExtrasManuales.RemoveAll(m => m.Fecha.Date == fRef && m.Dto.CodigoEmpleado == empAus.CodigoEmpleado);
                    edicionesHorarioHePorEmpleado.Remove(empAus.CodigoEmpleado);
                }
            }

            ValidarConflictosHorarios(out _);
            ActualizarGruposConsolidadosInterno(false);
            ActualizarGruposHorasExtrasInterno();
            await AutoGuardarBorradorLocal();

            _snackbar.Add(
                $"Conciliación {etiquetaCliente} completada: {actualizados} actualizados, {nuevosAgregados} agregados y {conHorasExtras} con Horas Extras cuadradas. Haz clic en 'GUARDAR EN BD'.",
                Severity.Success);
            StateHasChanged();
        }
}
    }

