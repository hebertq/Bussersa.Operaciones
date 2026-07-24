using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MediatR;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Entidades.Operaciones;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Application.Features.Odoo.Command;

namespace BsOperaciones.Pages.Operaciones
{
    public partial class RotacionTurnos : ComponentBase
    {
        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        private List<Combos> OperacionesList { get; set; } = new();
        private int? SelectedOperacionId { get; set; }

        private int _selectedCantidadTurnos = 2;
        private int SelectedCantidadTurnos
        {
            get => _selectedCantidadTurnos;
            set
            {
                if (_selectedCantidadTurnos != value)
                {
                    _selectedCantidadTurnos = value;
                    OnCantidadTurnosChanged();
                }
            }
        }

        private DateTime? _selectedMonday;
        private DateTime? SelectedMonday
        {
            get => _selectedMonday;
            set
            {
                if (_selectedMonday != value)
                {
                    _selectedMonday = value.HasValue ? GetMonday(value.Value) : null;
                    if (_selectedMonday.HasValue && SelectedOperacionId.HasValue)
                    {
                        _ = LoadWeekSchedule(_selectedMonday.Value);
                    }
                }
            }
        }

        private bool IsLoading { get; set; } = false;

        // Single unifed collection for all employees in the screen
        private List<ProgramacionTurnoDto> AllRotationItems { get; set; } = new();

        // Computed properties to filter items dynamically based on their zone
        private List<ProgramacionTurnoDto> UnassignedOperators => AllRotationItems.Where(x => GetItemZone(x) == "disponible").ToList();
        
        private ProgramacionTurnoDto? Shift1Supervisor => AllRotationItems.FirstOrDefault(x => GetItemZone(x) == "supervisor_1");
        private ProgramacionTurnoDto? Shift2Supervisor => AllRotationItems.FirstOrDefault(x => GetItemZone(x) == "supervisor_2");
        private ProgramacionTurnoDto? Shift3Supervisor => AllRotationItems.FirstOrDefault(x => GetItemZone(x) == "supervisor_3");

        private int? Shift1SupervisorId => Shift1Supervisor?.EmpleadoId;
        private int? Shift2SupervisorId => Shift2Supervisor?.EmpleadoId;
        private int? Shift3SupervisorId => Shift3Supervisor?.EmpleadoId;

        private List<ProgramacionTurnoDto> Shift1Operators => AllRotationItems.Where(x => GetItemZone(x) == "turno_1").ToList();
        private List<ProgramacionTurnoDto> Shift2Operators => AllRotationItems.Where(x => GetItemZone(x) == "turno_2").ToList();
        private List<ProgramacionTurnoDto> Shift3Operators => AllRotationItems.Where(x => GetItemZone(x) == "turno_3").ToList();

        protected override async Task OnInitializedAsync()
        {
            SelectedMonday = GetMonday(DateTime.Today);
            var result = await Mediator.Send(new GetAllCombosQuery("Operaciones"));
            if (!result.Respuesta.ExisteError && result.Model != null)
            {
                OperacionesList = result.Model.ToList();
            }
        }

        private async Task OnOperacionChanged(int? opId)
        {
            SelectedOperacionId = opId;
            if (SelectedOperacionId.HasValue && SelectedMonday.HasValue)
            {
                await LoadWeekSchedule(SelectedMonday.Value);
            }
            else
            {
                ClearAllLists();
                StateHasChanged();
            }
        }

        private void ClearAllLists()
        {
            AllRotationItems.Clear();
        }

        private void OnCantidadTurnosChanged()
        {
            // If the user changes quantity, we redistribute operators who are now in unsupported shifts
            foreach (var item in AllRotationItems)
            {
                string zone = GetItemZone(item);
                if (SelectedCantidadTurnos == 1 && (zone.EndsWith("_2") || zone.EndsWith("_3")))
                {
                    item.Turno = "";
                    item.Puesto = "Operador de almacen";
                    item.SupervisorId = null;
                }
                else if (SelectedCantidadTurnos == 2 && zone.EndsWith("_3"))
                {
                    item.Turno = "";
                    item.Puesto = "Operador de almacen";
                    item.SupervisorId = null;
                }
            }

            // Sync shift names on existing lists
            UpdateShiftNamesOnAssignedOperators();
            SyncSupervisorsForShifts();
            StateHasChanged();
        }

        private void UpdateShiftNamesOnAssignedOperators()
        {
            var names = GetShiftNames();
            foreach (var item in AllRotationItems)
            {
                if (string.IsNullOrEmpty(item.Turno)) continue;

                string zone = GetItemZone(item);
                if (zone.EndsWith("_1")) item.Turno = names[0];
                else if (zone.EndsWith("_2") && names.Count > 1) item.Turno = names[1];
                else if (zone.EndsWith("_3") && names.Count > 2) item.Turno = names[2];
            }
        }

        private List<string> GetShiftNames()
        {
            if (SelectedCantidadTurnos == 1) return new List<string> { "Turno Único" };
            if (SelectedCantidadTurnos == 3) return new List<string> { "Mañana", "Tarde", "Noche" };
            return new List<string> { "Diurno", "Nocturno" }; // Default 2 shifts
        }

        private DateTime GetMonday(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private async Task LoadWeekSchedule(DateTime monday)
        {
            if (!SelectedOperacionId.HasValue) return;

            IsLoading = true;
            StateHasChanged();
            ClearAllLists();
            try
            {
                // Load turnos programming from DB
                var result = await Mediator.Send(new GetProgramacionTurnosQuery(monday, SelectedOperacionId.Value));
                if (!result.Respuesta.ExisteError)
                {
                    var savedList = result.Model?.ToList() ?? new List<ProgramacionTurnoDto>();

                    if (savedList.Any())
                    {
                        // Determine shift count based on saved names
                        bool hasThree = savedList.Any(x => x.Turno == "Mañana" || x.Turno == "Tarde" || x.Turno == "Noche");
                        bool hasOne = savedList.All(x => x.Turno == "Turno Único");
                        _selectedCantidadTurnos = hasThree ? 3 : (hasOne ? 1 : 2);

                        // Populate all loaded items into the collection
                        AllRotationItems.AddRange(savedList);
                    }
                }
                else
                {
                    Snackbar.Add("Error al cargar turnos: " + result.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Excepción al cargar turnos: " + ex.Message, Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        // Maps an item dynamically to its drop zone identifier
        private string GetItemZone(ProgramacionTurnoDto item)
        {
            if (string.IsNullOrEmpty(item.Turno))
            {
                return "disponible";
            }

            var names = GetShiftNames();
            bool isSupervisor = item.Puesto == "Supervisor de personal";

            if (item.Turno == names[0])
            {
                return isSupervisor ? "supervisor_1" : "turno_1";
            }
            if (names.Count > 1 && item.Turno == names[1])
            {
                return isSupervisor ? "supervisor_2" : "turno_2";
            }
            if (names.Count > 2 && item.Turno == names[2])
            {
                return isSupervisor ? "supervisor_3" : "turno_3";
            }

            return "disponible";
        }

        // Handles Blazor Drag & Drop event
        private void ItemUpdated(MudItemDropInfo<ProgramacionTurnoDto> dropInfo)
        {
            var item = dropInfo.Item;
            var targetZone = dropInfo.DropzoneIdentifier;

            if (item == null) return;

            var names = GetShiftNames();

            // Clear previous supervisor if a new one is dropped into the slot
            if (targetZone.StartsWith("supervisor_"))
            {
                // Find if there is already a supervisor assigned in that zone
                var currentSup = AllRotationItems.FirstOrDefault(x => GetItemZone(x) == targetZone);
                if (currentSup != null && currentSup != item)
                {
                    currentSup.Turno = "";
                    currentSup.Puesto = "Operador de almacen";
                    currentSup.SupervisorId = null;
                }

                item.Puesto = "Supervisor de personal";
            }
            else if (targetZone.StartsWith("turno_"))
            {
                item.Puesto = "Operador de almacen";
            }
            else if (targetZone == "disponible")
            {
                item.Puesto = "Operador de almacen";
            }

            // Assign new Turno value
            if (targetZone == "disponible")
            {
                item.Turno = "";
                item.SupervisorId = null;
            }
            else if (targetZone == "turno_1" || targetZone == "supervisor_1")
            {
                item.Turno = names[0];
            }
            else if (targetZone == "turno_2" || targetZone == "supervisor_2")
            {
                item.Turno = names[1];
            }
            else if (targetZone == "turno_3" || targetZone == "supervisor_3")
            {
                item.Turno = names[2];
            }

            SyncSupervisorsForShifts();
            StateHasChanged();
        }

        private void SyncSupervisorsForShifts()
        {
            var names = GetShiftNames();
            var shift1SupId = Shift1Supervisor?.EmpleadoId;
            var shift2SupId = Shift2Supervisor?.EmpleadoId;
            var shift3SupId = Shift3Supervisor?.EmpleadoId;

            foreach (var item in AllRotationItems)
            {
                if (item.Puesto != "Supervisor de personal")
                {
                    if (item.Turno == names[0]) item.SupervisorId = shift1SupId;
                    else if (names.Count > 1 && item.Turno == names[1]) item.SupervisorId = shift2SupId;
                    else if (names.Count > 2 && item.Turno == names[2]) item.SupervisorId = shift3SupId;
                    else item.SupervisorId = null;
                }
            }
        }

        private void MoveOperator(ProgramacionTurnoDto op, string targetShift)
        {
            var names = GetShiftNames();
            if (targetShift == "")
            {
                op.Turno = "";
                op.Puesto = "Operador de almacen";
                op.SupervisorId = null;
            }
            else if (targetShift == names[0])
            {
                op.Turno = names[0];
                op.Puesto = "Operador de almacen";
            }
            else if (SelectedCantidadTurnos > 1 && targetShift == names[1])
            {
                op.Turno = names[1];
                op.Puesto = "Operador de almacen";
            }
            else if (SelectedCantidadTurnos > 2 && targetShift == names[2])
            {
                op.Turno = names[2];
                op.Puesto = "Operador de almacen";
            }

            SyncSupervisorsForShifts();
            StateHasChanged();
        }

        private void AssignSupervisor(ProgramacionTurnoDto op, string targetShift)
        {
            var names = GetShiftNames();
            string targetZone = targetShift == names[0] ? "supervisor_1" :
                                (names.Count > 1 && targetShift == names[1] ? "supervisor_2" : "supervisor_3");

            // Reset current supervisor in that shift
            var currentSup = AllRotationItems.FirstOrDefault(x => GetItemZone(x) == targetZone);
            if (currentSup != null && currentSup != op)
            {
                currentSup.Turno = "";
                currentSup.Puesto = "Operador de almacen";
                currentSup.SupervisorId = null;
            }

            op.Turno = targetShift;
            op.Puesto = "Supervisor de personal";
            op.SupervisorId = null;

            SyncSupervisorsForShifts();
            StateHasChanged();
        }

        private void UnassignSupervisor(int index)
        {
            ProgramacionTurnoDto? sup = index == 0 ? Shift1Supervisor :
                                        (index == 1 ? Shift2Supervisor : Shift3Supervisor);

            if (sup != null)
            {
                sup.Turno = "";
                sup.Puesto = "Operador de almacen";
                sup.SupervisorId = null;
            }

            SyncSupervisorsForShifts();
            StateHasChanged();
        }

        private void RemoveOperatorCompletely(ProgramacionTurnoDto op)
        {
            AllRotationItems.Remove(op);
            SyncSupervisorsForShifts();
            StateHasChanged();
        }

        private void SwapSupervisors()
        {
            if (SelectedCantidadTurnos == 1)
            {
                Snackbar.Add("No hay suficientes turnos asignados para realizar un intercambio.", Severity.Warning);
                return;
            }

            var names = GetShiftNames();
            var s1 = Shift1Supervisor;
            var s2 = Shift2Supervisor;
            var s3 = Shift3Supervisor;

            if (SelectedCantidadTurnos == 2)
            {
                if (s1 != null) s1.Turno = names[1];
                if (s2 != null) s2.Turno = names[0];
            }
            else if (SelectedCantidadTurnos == 3)
            {
                if (s1 != null) s1.Turno = names[1];
                if (s2 != null) s2.Turno = names[2];
                if (s3 != null) s3.Turno = names[0];
            }

            SyncSupervisorsForShifts();
            Snackbar.Add("Supervisores rotados. Recuerde guardar la programación.", Severity.Info);
            StateHasChanged();
        }

        private async Task SaveChanges()
        {
            if (!SelectedMonday.HasValue || !SelectedOperacionId.HasValue) return;

            IsLoading = true;
            StateHasChanged();
            try
            {
                var combined = new List<ProgramacionTurnoDto>();
                var names = GetShiftNames();
                DateTime fechaFin = SelectedMonday.Value.AddDays(6);

                // Collect all assigned items (both supervisors and operators)
                foreach (var item in AllRotationItems)
                {
                    if (!string.IsNullOrEmpty(item.Turno))
                    {
                        item.FechaInicio = SelectedMonday.Value;
                        item.FechaFin = fechaFin;
                        item.OperacionId = SelectedOperacionId.Value;
                        combined.Add(item);
                    }
                }

                // If combined is empty, we force a clear by sending a dummy item with EmpleadoId = 0
                if (!combined.Any())
                {
                    combined.Add(new ProgramacionTurnoDto
                    {
                        EmpleadoId = 0,
                        NombreCompleto = "CLEAR_ALL",
                        Puesto = "CLEAR_ALL",
                        Turno = "",
                        SupervisorId = null,
                        FechaInicio = SelectedMonday.Value,
                        FechaFin = fechaFin,
                        OperacionId = SelectedOperacionId.Value
                    });
                }

                var result = await Mediator.Send(new SaveProgramacionTurnosCommand(combined));
                if (!result.Respuesta.ExisteError)
                {
                    Snackbar.Add("Programación de turnos guardada exitosamente.", Severity.Success);
                    await LoadWeekSchedule(SelectedMonday.Value);
                }
                else
                {
                    Snackbar.Add("Error al guardar: " + result.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Excepción al guardar turnos: " + ex.Message, Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task AutoRotateNextWeek()
        {
            if (!SelectedMonday.HasValue || !SelectedOperacionId.HasValue) return;

            IsLoading = true;
            StateHasChanged();
            try
            {
                DateTime nextMonday = SelectedMonday.Value.AddDays(7);
                var result = await Mediator.Send(new AutoRotarTurnosCommand(SelectedMonday.Value, nextMonday, SelectedOperacionId.Value));
                if (!result.Respuesta.ExisteError)
                {
                    Snackbar.Add("Turnos rotados y copiados a la siguiente semana.", Severity.Success);
                    SelectedMonday = nextMonday;
                }
                else
                {
                    Snackbar.Add("Error al auto-rotar: " + result.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Excepción al auto-rotar turnos: " + ex.Message, Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private int GetColumnWidth()
        {
            if (SelectedCantidadTurnos == 1) return 6;
            if (SelectedCantidadTurnos == 2) return 4;
            return 3;
        }

        private string GetShiftHeaderStyle(int index)
        {
            if (index == 0) return "background: linear-gradient(135deg, #ffedd5 0%, #fef3c7 100%); padding: 12px; border-bottom: 1px solid #fed7aa;";
            if (index == 1) return "background: linear-gradient(135deg, #e0e7ff 0%, #e8f0fe 100%); padding: 12px; border-bottom: 1px solid #c7d2fe;";
            return "background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%); padding: 12px; border-bottom: 1px solid #cbd5e1;";
        }

        private string GetShiftCardStyle(int index)
        {
            if (index == 0) return "border-radius: 16px; border: 1px solid #fed7aa; background: #fffcf8; overflow: hidden;";
            if (index == 1) return "border-radius: 16px; border: 1px solid #c7d2fe; background: #fafaff; overflow: hidden;";
            return "border-radius: 16px; border: 1px solid #cbd5e1; background: #f8fafc; overflow: hidden;";
        }

        private string GetShiftIcon(int index)
        {
            if (index == 0) return Icons.Material.Filled.WbSunny;
            if (index == 1) return Icons.Material.Filled.NightsStay;
            return Icons.Material.Filled.AccessTime;
        }

        private string GetShiftIconStyle(int index)
        {
            if (index == 0) return "color: #d97706; font-size: 2rem;";
            if (index == 1) return "color: #4f46e5; font-size: 2rem;";
            return "color: #475569; font-size: 2rem;";
        }

        private string GetShiftTitle(int index)
        {
            var names = GetShiftNames();
            if (index < names.Count) return names[index].ToUpper();
            return "TURNO";
        }

        private string GetShiftSubtitle(int index)
        {
            if (SelectedCantidadTurnos == 1) return "Jornada Única";
            if (SelectedCantidadTurnos == 2)
            {
                return index == 0 ? "Horario de Día" : "Horario de Noche";
            }
            if (index == 0) return "Turno Matutino";
            if (index == 1) return "Turno Vespertino";
            return "Turno Nocturno";
        }

        private string GetSupervisorCardBorderStyle(int index)
        {
            if (index == 0) return "background: #ffffff; border-radius: 12px; border: 1px dashed #f59e0b;";
            if (index == 1) return "background: #ffffff; border-radius: 12px; border: 1px dashed #6366f1;";
            return "background: #ffffff; border-radius: 12px; border: 1px dashed #64748b;";
        }

        private ProgramacionTurnoDto? GetShiftSupervisor(int index)
        {
            if (index == 0) return Shift1Supervisor;
            if (index == 1) return Shift2Supervisor;
            return Shift3Supervisor;
        }

        private List<ProgramacionTurnoDto> GetShiftOperators(int index)
        {
            if (index == 0) return Shift1Operators;
            if (index == 1) return Shift2Operators;
            return Shift3Operators;
        }

        private async Task OpenAddEmployeesDialog()
        {
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = DialogService.Show<DialogAddEmployeesToRotation>("Buscar y Agregar Personal", options);
            var result = await dialog.Result;
            if (!result.Canceled && result.Data is List<OdooEmployeeDto> selectedEmployees)
            {
                int countAdded = 0;
                foreach (var emp in selectedEmployees)
                {
                    bool alreadyAssigned = AllRotationItems.Any(o => o.EmpleadoId == emp.Id);

                    if (!alreadyAssigned)
                    {
                        AllRotationItems.Add(new ProgramacionTurnoDto
                        {
                            EmpleadoId = emp.Id,
                            NombreCompleto = emp.Name,
                            Puesto = emp.JobTitle == "Supervisor de personal" ? "Supervisor de personal" : "Operador de almacen",
                            Turno = "",
                            SupervisorId = null
                        });
                        countAdded++;
                    }
                }

                if (countAdded > 0)
                {
                    Snackbar.Add($"Se agregaron {countAdded} empleados al listado disponible.", Severity.Success);
                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add("Los empleados seleccionados ya estaban incluidos en la programación.", Severity.Info);
                }
            }
        }
    }
}
