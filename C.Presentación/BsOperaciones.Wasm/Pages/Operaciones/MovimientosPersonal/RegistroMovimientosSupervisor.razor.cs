using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HostService.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Modelo.Admin;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using MudBlazor;

namespace BsOperaciones.Pages.Operaciones.MovimientosPersonal
{
    public class MovimientoSupervisorItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int FilaIndex { get; set; }
        public string Fecha { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string TurnoPreset { get; set; } = "Diurno";
        public string HoraInicio { get; set; } = "06:00";
        public string HoraFin { get; set; } = "14:00";
        public List<OdooEmployeeDto> Empleados { get; set; } = new();
        public string IdsManuales { get; set; } = string.Empty;
        public string IdsEmpleadosTexto { get; set; } = string.Empty;
        public int CantidadEmpleados => Empleados.Count > 0 ? Empleados.Count : (int.TryParse(CantidadManual, out int c) ? c : 0);
        public string CantidadManual { get; set; } = "1";
        public string? VtcoTrasp { get; set; } = "0";
        public string? VtcoAlim { get; set; } = "0";
        public string Observaciones { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Bonos { get; set; } = "0.00";
        public string DobleTurno { get; set; } = "0";
        public string TiempoComida { get; set; } = "0.00";
        public string EsOperacionMixta { get; set; } = "0";

        public EstadoMovimientoSupervisor Estado { get; set; } = EstadoMovimientoSupervisor.Borrador;
        public int? RedmineIssueId { get; set; }
        public int? RedmineViaticosIssueId { get; set; }
        public string? MensajeError { get; set; }
        public DateTime FechaRegistroLocal { get; set; } = DateTime.Now;
    }

    public enum EstadoMovimientoSupervisor
    {
        Borrador,
        Enviando,
        Sincronizado,
        Error
    }

    public partial class RegistroMovimientosSupervisor : ComponentBase
    {
        [Inject] private IOdooService _odooService { get; set; } = null!;
        [Inject] private ISnackbar _snackbar { get; set; } = null!;
        [Inject] private IDialogService _dialogService { get; set; } = null!;
        [Inject] private IJSRuntime _js { get; set; } = null!;
        [Inject] private NavigationManager _navigationManager { get; set; } = null!;

        // Estado general
        protected bool estaCargando = true;
        protected bool estaEnviando = false;
        protected List<Combos> operacionesList = new();
        protected int? selectedOperacionId;
        protected string clienteRedmineIdentificador = string.Empty;
        protected DateTime? fechaSeleccionada = DateTime.Today;

        // Catálogos
        protected List<OdooEmployeeDto> todosLosEmpleados = new();
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

        // Formulario de Entrada Rápida
        protected string turnoSeleccionado = "Diurno";
        protected TimeSpan? horaInicio = new TimeSpan(6, 0, 0);
        protected TimeSpan? horaFin = new TimeSpan(14, 0, 0);
        protected string areaInput = string.Empty;
        protected string cargoInput = "6-Operador de almacen";
        protected List<OdooEmployeeDto> empleadosSeleccionados = new();
        protected string idsManualesInput = string.Empty;
        protected decimal viaticoTransporte = 0m;
        protected decimal viaticoAlimentacion = 0m;
        protected string observacionesInput = string.Empty;
        protected string asuntoPersonalizado = string.Empty;
        protected string bonosInput = "0.00";
        protected bool esDobleTurno = false;
        protected string tiempoComidaInput = "0.00";
        protected bool esOperacionMixta = false;
        protected bool mostrarOpcionesAvanzadas = false;

        // Búsqueda de empleado en el autocomplete
        protected OdooEmployeeDto? empleadoAutocompletado;

        // Edición de un movimiento existente en la lista
        protected Guid? editandoItemId;

        // Lista de movimientos de la jornada actual
        protected List<MovimientoSupervisorItem> movimientosJornada = new();

        // Estadísticas de la jornada
        protected int TotalMovimientos => movimientosJornada.Count;
        protected int TotalPersonalRegistrado => movimientosJornada.Sum(x => x.CantidadEmpleados);
        protected decimal TotalViaticosTrasp => movimientosJornada.Sum(x => decimal.TryParse(x.VtcoTrasp, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m);
        protected decimal TotalViaticosAlim => movimientosJornada.Sum(x => decimal.TryParse(x.VtcoAlim, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m);
        protected int TotalPendientesEnvio => movimientosJornada.Count(x => x.Estado != EstadoMovimientoSupervisor.Sincronizado);
        protected int TotalSincronizados => movimientosJornada.Count(x => x.Estado == EstadoMovimientoSupervisor.Sincronizado);

        protected override async Task OnInitializedAsync()
        {
            estaCargando = true;
            try
            {
                await CargarOperaciones();
                await CargarCatalogoEmpleados();
                await RecuperarOperacionPrevia();
                await CargarBorradorLocal();
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
                _snackbar.Add($"No se pudieron cargar operaciones: {ex.Message}", Severity.Warning);
            }
        }

        private async Task CargarCatalogoEmpleados()
        {
            try
            {
                var resp = await _odooService.GetAllActiveEmployeesForRotation();
                if (resp?.Model != null)
                {
                    todosLosEmpleados = resp.Model.ToList();
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Catálogo de empleados no disponible offline: {ex.Message}", Severity.Info);
            }
        }

        private async Task RecuperarOperacionPrevia()
        {
            try
            {
                string? guardado = await _js.InvokeAsync<string>("localStorage.getItem", "supervisor_last_operacion_id");
                if (int.TryParse(guardado, out int opId) && operacionesList.Any(x => x.id == opId))
                {
                    selectedOperacionId = opId;
                    ActualizarIdentificadorCliente(opId);
                    ActualizarAreasSugeridas();
                }
                else if (operacionesList.Any())
                {
                    var defecto = operacionesList.FirstOrDefault(x => x.nombre.Contains("WalMart", StringComparison.OrdinalIgnoreCase)) ?? operacionesList.First();
                    selectedOperacionId = defecto.id;
                    ActualizarIdentificadorCliente(defecto.id);
                    ActualizarAreasSugeridas();
                }
            }
            catch { }
        }

        protected void OnOperacionChanged(int? operacionId)
        {
            selectedOperacionId = operacionId;
            if (operacionId.HasValue)
            {
                ActualizarIdentificadorCliente(operacionId.Value);
                _ = _js.InvokeVoidAsync("localStorage.setItem", "supervisor_last_operacion_id", operacionId.Value.ToString());
                ActualizarAreasSugeridas();
            }
            else
            {
                clienteRedmineIdentificador = string.Empty;
                areasSugeridas.Clear();
            }
            StateHasChanged();
        }

        private void ActualizarIdentificadorCliente(int operacionId)
        {
            var op = operacionesList.FirstOrDefault(x => x.id == operacionId);
            clienteRedmineIdentificador = op != null ? $"{op.id}-{op.nombre}" : $"Operacion_{operacionId}";
        }

        private void ActualizarAreasSugeridas()
        {
            areasSugeridas.Clear();
            if (!selectedOperacionId.HasValue) return;

            var opNombre = operacionesList.FirstOrDefault(x => x.id == selectedOperacionId.Value)?.nombre ?? "";

            // Áreas estándar según el cliente
            if (opNombre.Contains("WalMart", StringComparison.OrdinalIgnoreCase) || opNombre.Contains("Walmart", StringComparison.OrdinalIgnoreCase))
            {
                areasSugeridas = new List<string>
                {
                    "WalMart Importado DCC",
                    "WalMart Nestle",
                    "WalMart Importado descarga",
                    "WalMart Despacho",
                    "WalMart Holler despacho",
                    "WalMart Mantenimiento",
                    "WalMart Operaciones",
                    "WalMart Alisto",
                    "WalMart Bodega",
                    "WalMart Cedis",
                    "WalMart Canastas",
                    "WalMart Crosdosk",
                    "WalMart Farmacia",
                    "WalMart Tarima",
                    "WalMart Grano",
                    "WalMart Importado",
                    "WalMart BH",
                    "WalMart Devoluciones",
                    "WalMart Estantería",
                    "WalMart Movimientos",
                    "WalMart Transporte",
                    "WalMart BH DCC",
                    "WalMart Momotombo",
                    "WalMart Auditoria",
                    "WalMart Congesa",
                    "WalMart Industrias cárnicas",
                    "WalMart Retornable",
                    "WalMart Torre de control",
                    "WalMart Trafico",
                    "WalMart Cargue cestas",
                    "Comodin"
                };
            }
            else if (opNombre.Contains("Food", StringComparison.OrdinalIgnoreCase))
            {
                areasSugeridas = new List<string> { "FoServ CD", "CS Food Services", "OPM Food Services", "CD", "Comodin" };
            }
            else if (opNombre.Contains("Cafe", StringComparison.OrdinalIgnoreCase) || opNombre.Contains("Café", StringComparison.OrdinalIgnoreCase))
            {
                areasSugeridas = new List<string> { "CS CD", "CS Suministro", "CS Transporte", "CC CD", "CS Food Services", "OPM Cafe Soluble", "Comodin" };
            }
            else if (opNombre.Contains("Condes", StringComparison.OrdinalIgnoreCase))
            {
                areasSugeridas = new List<string> { "Las Condes Importaciones", "Las Condes Operaciones", "Las Condes Transporte", "Las Condes Congesa", "Las Condes Cargill", "Las Condes Descargue de cestas", "OPM Las Condes", "Comodin" };
            }
            else if (opNombre.Contains("Sinsa", StringComparison.OrdinalIgnoreCase))
            {
                areasSugeridas = new List<string> { "Ceramica", "Construcción", "Rampa", "Despacho Operaciones", "Despacho Ceramica", "PVC", "Logistica", "Alta y Baja tención", "Control de calidad", "Dequipo", "Axis", "Matagalpa", "Comodin" };
            }
            else if (opNombre.Contains("Horti", StringComparison.OrdinalIgnoreCase))
            {
                areasSugeridas = new List<string> { "HTF CD", "HTF Ayudantes de entrega", "HTF Ayudantes de recolección", "OPM Hortifruti", "Comodin" };
            }
            else
            {
                areasSugeridas = new List<string> { "CD", "Bodega", "Operaciones", "Despacho", "Alisto", "Transporte", "Comodin" };
            }

            if (string.IsNullOrEmpty(areaInput) && areasSugeridas.Any())
            {
                areaInput = areasSugeridas.First();
            }
        }

        protected string ObtenerNombreOperacion(int? id)
        {
            if (!id.HasValue) return "-- Seleccione la operación --";
            var op = operacionesList.FirstOrDefault(x => x.id == id.Value);
            return op != null ? $"{op.id}-{op.nombre}" : $"Operación #{id.Value}";
        }

        #region Manejo de Turnos y Presets

        protected void SeleccionarTurno(string turno)
        {
            turnoSeleccionado = turno;
            switch (turno)
            {
                case "Diurno":
                    horaInicio = new TimeSpan(6, 0, 0);
                    horaFin = new TimeSpan(14, 0, 0);
                    break;
                case "DCC":
                    horaInicio = new TimeSpan(7, 0, 0);
                    horaFin = new TimeSpan(17, 0, 0);
                    break;
                case "Mixto":
                    horaInicio = new TimeSpan(14, 0, 0);
                    horaFin = new TimeSpan(22, 0, 0);
                    break;
                case "Nocturno":
                    horaInicio = new TimeSpan(22, 0, 0);
                    horaFin = new TimeSpan(6, 0, 0);
                    break;
                case "Personalizado":
                    break;
            }
        }

        #endregion

        #region Búsqueda y Selección de Empleados

        protected Task<IEnumerable<OdooEmployeeDto>> BuscarEmpleado(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Sugerir primeros 10 empleados de la operación actual
                var depto = todosLosEmpleados
                    .Where(x => selectedOperacionId.HasValue && x.DepartmentId == selectedOperacionId.Value)
                    .Take(10);
                return Task.FromResult<IEnumerable<OdooEmployeeDto>>(depto);
            }

            string s = value.Trim();
            var resultados = todosLosEmpleados
                .Where(x => (x.Name != null && x.Name.Contains(s, StringComparison.OrdinalIgnoreCase)) ||
                            (x.CodigoEmpleado.HasValue && x.CodigoEmpleado.Value.ToString().Contains(s)) ||
                            (x.Id.ToString().Contains(s)))
                .Take(15);

            return Task.FromResult<IEnumerable<OdooEmployeeDto>>(resultados);
        }

        protected void OnEmpleadoSeleccionadoDesdeAutocomplete(OdooEmployeeDto? emp)
        {
            if (emp == null) return;
            AgregarEmpleadoALista(emp);
            empleadoAutocompletado = null;
        }

        private void AgregarEmpleadoALista(OdooEmployeeDto emp)
        {
            int code = emp.CodigoEmpleado ?? emp.Id;
            if (empleadosSeleccionados.Any(x => (x.CodigoEmpleado ?? x.Id) == code))
            {
                _snackbar.Add($"{emp.Name} ya está en la lista.", Severity.Info);
                return;
            }
            empleadosSeleccionados.Add(emp);
        }

        protected void RemoverEmpleadoDeLista(OdooEmployeeDto emp)
        {
            int code = emp.CodigoEmpleado ?? emp.Id;
            var item = empleadosSeleccionados.FirstOrDefault(x => (x.CodigoEmpleado ?? x.Id) == code);
            if (item != null)
            {
                empleadosSeleccionados.Remove(item);
            }
        }

        protected void LimpiarEmpleadosSeleccionados()
        {
            empleadosSeleccionados.Clear();
            idsManualesInput = string.Empty;
        }

        protected async Task AbrirDialogoCatalogo()
        {
            var opNombre = operacionesList.FirstOrDefault(x => x.id == selectedOperacionId)?.nombre ?? "";
            var parameters = new DialogParameters
            {
                ["TodosLosEmpleados"] = todosLosEmpleados,
                ["EmpleadosPreseleccionados"] = empleadosSeleccionados,
                ["OperacionId"] = selectedOperacionId,
                ["NombreOperacion"] = opNombre
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<DialogCatalogoEmpleadosTablet>("Catálogo de Personal", parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled && result.Data is List<OdooEmployeeDto> seleccionados)
            {
                empleadosSeleccionados = seleccionados;
                StateHasChanged();
            }
        }

        protected Task<IEnumerable<string>> BuscarArea(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult<IEnumerable<string>>(areasSugeridas);

            var result = areasSugeridas
                .Where(x => x.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!result.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                result.Insert(0, value); // Permitir libre entrada
            }

            return Task.FromResult<IEnumerable<string>>(result);
        }

        #endregion

        #region Viáticos Rápido

        protected void AsignarViaticoTransporte(decimal monto)
        {
            viaticoTransporte = monto;
        }

        protected void AsignarViaticoAlimentacion(decimal monto)
        {
            viaticoAlimentacion = monto;
        }

        #endregion

        #region Gestión de Movimientos (Agregar, Editar, Eliminar)

        protected async Task AgregarOActualizarMovimiento()
        {
            if (!selectedOperacionId.HasValue || selectedOperacionId.Value <= 0)
            {
                _snackbar.Add("Seleccione la Operación / Cliente.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(areaInput))
            {
                _snackbar.Add("Indique el Área operativa del movimiento.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(cargoInput))
            {
                _snackbar.Add("Seleccione el Puesto / Cargo.", Severity.Warning);
                return;
            }

            // Ensamblar IDs de empleados
            var listaIds = new List<string>();
            foreach (var emp in empleadosSeleccionados)
            {
                listaIds.Add((emp.CodigoEmpleado ?? emp.Id).ToString());
            }

            if (!string.IsNullOrWhiteSpace(idsManualesInput))
            {
                var manuales = idsManualesInput.Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var m in manuales)
                {
                    var limpio = m.Trim();
                    if (!string.IsNullOrEmpty(limpio) && !listaIds.Contains(limpio))
                    {
                        listaIds.Add(limpio);
                    }
                }
            }

            if (!listaIds.Any())
            {
                _snackbar.Add("Debe agregar al menos un empleado (por catálogo o escribiendo códigos).", Severity.Warning);
                return;
            }

            string hIni = horaInicio.HasValue ? $"{(int)horaInicio.Value.TotalHours:D2}:{horaInicio.Value.Minutes:D2}" : "06:00";
            string hFin = horaFin.HasValue ? $"{(int)horaFin.Value.TotalHours:D2}:{horaFin.Value.Minutes:D2}" : "14:00";
            string fechaStr = (fechaSeleccionada ?? DateTime.Today).ToString("yyyy-MM-dd");
            string asunto = !string.IsNullOrWhiteSpace(asuntoPersonalizado) ? asuntoPersonalizado.Trim() : $"{areaInput.Trim()} - {cargoInput.Trim()}";

            if (editandoItemId.HasValue)
            {
                var existente = movimientosJornada.FirstOrDefault(x => x.Id == editandoItemId.Value);
                if (existente != null)
                {
                    existente.Fecha = fechaStr;
                    existente.TurnoPreset = turnoSeleccionado;
                    existente.HoraInicio = hIni;
                    existente.HoraFin = hFin;
                    existente.Area = areaInput.Trim();
                    existente.Cargo = cargoInput.Trim();
                    existente.Empleados = empleadosSeleccionados.ToList();
                    existente.IdsManuales = idsManualesInput.Trim();
                    existente.IdsEmpleadosTexto = string.Join(", ", listaIds);
                    existente.CantidadManual = listaIds.Count.ToString();
                    existente.VtcoTrasp = viaticoTransporte > 0 ? viaticoTransporte.ToString("0.##", CultureInfo.InvariantCulture) : "0";
                    existente.VtcoAlim = viaticoAlimentacion > 0 ? viaticoAlimentacion.ToString("0.##", CultureInfo.InvariantCulture) : "0";
                    existente.Observaciones = observacionesInput.Trim();
                    existente.Asunto = asunto;
                    existente.Bonos = bonosInput;
                    existente.DobleTurno = esDobleTurno ? "1" : "0";
                    existente.TiempoComida = tiempoComidaInput;
                    existente.EsOperacionMixta = esOperacionMixta ? "1" : "0";
                    existente.Estado = EstadoMovimientoSupervisor.Borrador;
                    existente.MensajeError = null;

                    _snackbar.Add("Movimiento actualizado exitosamente.", Severity.Success);
                }
                editandoItemId = null;
            }
            else
            {
                var nuevo = new MovimientoSupervisorItem
                {
                    Id = Guid.NewGuid(),
                    FilaIndex = movimientosJornada.Count + 1,
                    Fecha = fechaStr,
                    TurnoPreset = turnoSeleccionado,
                    HoraInicio = hIni,
                    HoraFin = hFin,
                    Area = areaInput.Trim(),
                    Cargo = cargoInput.Trim(),
                    Empleados = empleadosSeleccionados.ToList(),
                    IdsManuales = idsManualesInput.Trim(),
                    IdsEmpleadosTexto = string.Join(", ", listaIds),
                    CantidadManual = listaIds.Count.ToString(),
                    VtcoTrasp = viaticoTransporte > 0 ? viaticoTransporte.ToString("0.##", CultureInfo.InvariantCulture) : "0",
                    VtcoAlim = viaticoAlimentacion > 0 ? viaticoAlimentacion.ToString("0.##", CultureInfo.InvariantCulture) : "0",
                    Observaciones = observacionesInput.Trim(),
                    Asunto = asunto,
                    Bonos = bonosInput,
                    DobleTurno = esDobleTurno ? "1" : "0",
                    TiempoComida = tiempoComidaInput,
                    EsOperacionMixta = esOperacionMixta ? "1" : "0",
                    Estado = EstadoMovimientoSupervisor.Borrador
                };

                movimientosJornada.Add(nuevo);
                _snackbar.Add($"¡Agregado! {listaIds.Count} personal en {areaInput}.", Severity.Success);
            }

            // Limpiar selección de personal para el siguiente grupo (mantener operación, turno y área para agilidad)
            empleadosSeleccionados.Clear();
            idsManualesInput = string.Empty;
            observacionesInput = string.Empty;
            viaticoTransporte = 0m;
            viaticoAlimentacion = 0m;
            asuntoPersonalizado = string.Empty;

            await GuardarBorradorLocal();
            StateHasChanged();
        }

        protected void CargarParaEditar(MovimientoSupervisorItem item)
        {
            editandoItemId = item.Id;
            turnoSeleccionado = item.TurnoPreset;
            if (TimeSpan.TryParse(item.HoraInicio, out var ti)) horaInicio = ti;
            if (TimeSpan.TryParse(item.HoraFin, out var tf)) horaFin = tf;
            areaInput = item.Area;
            cargoInput = item.Cargo;
            empleadosSeleccionados = item.Empleados.ToList();
            idsManualesInput = item.IdsManuales;
            viaticoTransporte = decimal.TryParse(item.VtcoTrasp, NumberStyles.Any, CultureInfo.InvariantCulture, out var vt) ? vt : 0m;
            viaticoAlimentacion = decimal.TryParse(item.VtcoAlim, NumberStyles.Any, CultureInfo.InvariantCulture, out var va) ? va : 0m;
            observacionesInput = item.Observaciones;
            asuntoPersonalizado = item.Asunto;
            bonosInput = item.Bonos;
            esDobleTurno = item.DobleTurno == "1";
            tiempoComidaInput = item.TiempoComida;
            esOperacionMixta = item.EsOperacionMixta == "1";

            _snackbar.Add("Cargado en el formulario para editar.", Severity.Info);
        }

        protected void CancelarEdicion()
        {
            editandoItemId = null;
            empleadosSeleccionados.Clear();
            idsManualesInput = string.Empty;
            observacionesInput = string.Empty;
            viaticoTransporte = 0m;
            viaticoAlimentacion = 0m;
            asuntoPersonalizado = string.Empty;
            StateHasChanged();
        }

        protected async Task EliminarMovimiento(MovimientoSupervisorItem item)
        {
            movimientosJornada.Remove(item);
            for (int i = 0; i < movimientosJornada.Count; i++)
            {
                movimientosJornada[i].FilaIndex = i + 1;
            }
            await GuardarBorradorLocal();
            _snackbar.Add("Movimiento eliminado de la jornada.", Severity.Normal);
            StateHasChanged();
        }

        protected async Task LimpiarTodaLaJornada()
        {
            bool? confirmado = await _dialogService.ShowMessageBox(
                "Reiniciar Jornada",
                "¿Está seguro de que desea limpiar todos los registros de la pantalla? Los que no se hayan enviado a Redmine se perderán.",
                yesText: "Sí, Limpiar",
                cancelText: "Cancelar"
            );

            if (confirmado == true)
            {
                movimientosJornada.Clear();
                editandoItemId = null;
                empleadosSeleccionados.Clear();
                idsManualesInput = string.Empty;
                await LimpiarBorradorLocal();
                _snackbar.Add("Jornada reiniciada.", Severity.Info);
                StateHasChanged();
            }
        }

        #endregion

        #region Persistencia Local (Resistencia a Desconexiones)

        private async Task GuardarBorradorLocal()
        {
            try
            {
                string json = JsonSerializer.Serialize(movimientosJornada);
                await _js.InvokeVoidAsync("localStorage.setItem", "supervisor_movimientos_jornada_draft", json);
            }
            catch { }
        }

        private async Task CargarBorradorLocal()
        {
            try
            {
                string? json = await _js.InvokeAsync<string>("localStorage.getItem", "supervisor_movimientos_jornada_draft");
                if (!string.IsNullOrEmpty(json))
                {
                    var items = JsonSerializer.Deserialize<List<MovimientoSupervisorItem>>(json);
                    if (items != null && items.Any())
                    {
                        movimientosJornada = items;
                    }
                }
            }
            catch { }
        }

        private async Task LimpiarBorradorLocal()
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", "supervisor_movimientos_jornada_draft");
            }
            catch { }
        }

        #endregion

        #region Envío a Redmine

        protected async Task EnviarJornadaARedmine()
        {
            if (!selectedOperacionId.HasValue || selectedOperacionId.Value <= 0)
            {
                _snackbar.Add("Seleccione la Operación / Cliente antes de enviar.", Severity.Warning);
                return;
            }

            var pendientes = movimientosJornada.Where(x => x.Estado != EstadoMovimientoSupervisor.Sincronizado).ToList();
            if (!pendientes.Any())
            {
                _snackbar.Add("Todos los movimientos de esta jornada ya fueron enviados a Redmine.", Severity.Info);
                return;
            }

            bool? confirmado = await _dialogService.ShowMessageBox(
                "Confirmar Envío a Redmine",
                $"Se crearán los tickets de Redmine para {pendientes.Count} grupo(s) de trabajo con un total de {pendientes.Sum(x => x.CantidadEmpleados)} personas.\n\n¿Desea continuar?",
                yesText: "Sí, Enviar a Redmine",
                cancelText: "Cancelar"
            );

            if (confirmado != true) return;

            estaEnviando = true;
            foreach (var p in pendientes) p.Estado = EstadoMovimientoSupervisor.Enviando;
            StateHasChanged();

            try
            {
                var requestDto = new CrearMovimientosPersonalRequestDto
                {
                    OperacionId = selectedOperacionId.Value,
                    ClienteRedmine = clienteRedmineIdentificador,
                    ModoGeneracion = "Ambos",
                    Movimientos = pendientes.Select(x => new MovimientoPersonalItemDto
                    {
                        FilaIndex = x.FilaIndex,
                        Area = x.Area,
                        Cargo = x.Cargo,
                        Fecha = x.Fecha,
                        HoraInicio = x.HoraInicio,
                        HoraFin = x.HoraFin,
                        CantidadEmpleados = x.CantidadEmpleados,
                        IdsEmpleados = x.IdsEmpleadosTexto,
                        Asunto = x.Asunto,
                        Observaciones = x.Observaciones,
                        Bonos = x.Bonos,
                        DobleTurno = x.DobleTurno,
                        TiempoComida = x.TiempoComida,
                        EsOperacionMixta = x.EsOperacionMixta,
                        VtcoTrasp = x.VtcoTrasp,
                        VtcoAlim = x.VtcoAlim
                    }).ToList()
                };

                var apiResponse = await _odooService.CrearMovimientosPersonalMasivo(requestDto);
                if (apiResponse?.Model?.Detalles != null)
                {
                    int exitoCount = 0;
                    int errorCount = 0;

                    foreach (var det in apiResponse.Model.Detalles)
                    {
                        var target = pendientes.FirstOrDefault(x => x.FilaIndex == det.FilaIndex);
                        if (target != null)
                        {
                            if (det.Exitoso)
                            {
                                target.Estado = EstadoMovimientoSupervisor.Sincronizado;
                                target.RedmineIssueId = det.RedmineIssueId;
                                target.RedmineViaticosIssueId = det.RedmineViaticosIssueId;
                                target.MensajeError = null;
                                exitoCount++;
                            }
                            else
                            {
                                target.Estado = EstadoMovimientoSupervisor.Error;
                                target.MensajeError = det.Mensaje;
                                errorCount++;
                            }
                        }
                    }

                    await GuardarBorradorLocal();

                    if (errorCount == 0)
                    {
                        _snackbar.Add($"¡Excelente! {exitoCount} movimientos creados exitosamente en Redmine.", Severity.Success);
                    }
                    else
                    {
                        _snackbar.Add($"Proceso finalizado. Exitosos: {exitoCount}, Errores: {errorCount}.", Severity.Warning);
                    }
                }
                else
                {
                    string err = apiResponse?.Respuesta?.MensajeError ?? "No se recibió respuesta válida del servidor.";
                    foreach (var p in pendientes)
                    {
                        p.Estado = EstadoMovimientoSupervisor.Error;
                        p.MensajeError = err;
                    }
                    _snackbar.Add($"Error al enviar a Redmine: {err}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                foreach (var p in pendientes)
                {
                    p.Estado = EstadoMovimientoSupervisor.Error;
                    p.MensajeError = ex.Message;
                }
                _snackbar.Add($"Excepción al conectar con Redmine: {ex.Message}", Severity.Error);
            }
            finally
            {
                estaEnviando = false;
                await GuardarBorradorLocal();
                StateHasChanged();
            }
        }

        #endregion
    }
}
