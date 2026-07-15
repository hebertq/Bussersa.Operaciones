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

        // Active Odoo Employees Pools
        private List<OdooEmployeeDto> AllSupervisors { get; set; } = new();
        private List<OdooEmployeeDto> AllOperators { get; set; } = new();

        // Supervisor ID for each shift
        private int? Shift1SupervisorId { get; set; }
        private int? Shift2SupervisorId { get; set; }
        private int? Shift3SupervisorId { get; set; }

        // Operators lists
        private List<ProgramacionTurnoDto> Shift1Operators { get; set; } = new();
        private List<ProgramacionTurnoDto> Shift2Operators { get; set; } = new();
        private List<ProgramacionTurnoDto> Shift3Operators { get; set; } = new();
        private List<ProgramacionTurnoDto> UnassignedOperators { get; set; } = new();

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
            AllSupervisors.Clear();
            AllOperators.Clear();
            Shift1SupervisorId = null;
            Shift2SupervisorId = null;
            Shift3SupervisorId = null;
            Shift1Operators.Clear();
            Shift2Operators.Clear();
            Shift3Operators.Clear();
            UnassignedOperators.Clear();
        }

        private void OnCantidadTurnosChanged()
        {
            // If the user changes quantity, we redistribute operators who are now in unsupported shifts
            if (SelectedCantidadTurnos < 3 && Shift3Operators.Any())
            {
                foreach (var op in Shift3Operators)
                {
                    op.Turno = "";
                    op.SupervisorId = null;
                    UnassignedOperators.Add(op);
                }
                Shift3Operators.Clear();
                Shift3SupervisorId = null;
            }

            if (SelectedCantidadTurnos < 2 && Shift2Operators.Any())
            {
                foreach (var op in Shift2Operators)
                {
                    op.Turno = "";
                    op.SupervisorId = null;
                    UnassignedOperators.Add(op);
                }
                Shift2Operators.Clear();
                Shift2SupervisorId = null;
            }

            // Sync shift names on existing lists
            UpdateShiftNamesOnAssignedOperators();
            StateHasChanged();
        }

        private void UpdateShiftNamesOnAssignedOperators()
        {
            var names = GetShiftNames();
            foreach (var op in Shift1Operators) op.Turno = names[0];
            if (SelectedCantidadTurnos > 1)
            {
                foreach (var op in Shift2Operators) op.Turno = names[1];
            }
            if (SelectedCantidadTurnos > 2)
            {
                foreach (var op in Shift3Operators) op.Turno = names[2];
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
                // 1. Load active employees from Odoo
                var odooEmpsResult = await Mediator.Send(new GetEmployeesForRotationQuery(SelectedOperacionId.Value));
                if (odooEmpsResult.Respuesta.ExisteError || odooEmpsResult.Model == null)
                {
                    Snackbar.Add("Error al cargar empleados de Odoo: " + odooEmpsResult.Respuesta.MensajeError, Severity.Error);
                    return;
                }

                var activeEmps = odooEmpsResult.Model.ToList();
                AllSupervisors = activeEmps.Where(e => e.JobTitle == "Supervisor de personal").ToList();
                AllOperators = activeEmps.Where(e => e.JobTitle == "Operador de almacen").ToList();

                // 2. Load turnos programming from DB
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

                        var names = GetShiftNames();

                        // Supervisors
                        var supervisors = savedList.Where(x => x.Puesto == "Supervisor de personal").ToList();
                        var shift1Sup = supervisors.FirstOrDefault(x => x.Turno == names[0]);
                        Shift1SupervisorId = shift1Sup?.EmpleadoId;

                        if (SelectedCantidadTurnos > 1)
                        {
                            var shift2Sup = supervisors.FirstOrDefault(x => x.Turno == names[1]);
                            Shift2SupervisorId = shift2Sup?.EmpleadoId;
                        }
                        if (SelectedCantidadTurnos > 2)
                        {
                            var shift3Sup = supervisors.FirstOrDefault(x => x.Turno == names[2]);
                            Shift3SupervisorId = shift3Sup?.EmpleadoId;
                        }

                        // Operators
                        var dbOperators = savedList.Where(x => x.Puesto != "Supervisor de personal").ToList();
                        Shift1Operators = dbOperators.Where(x => x.Turno == names[0]).ToList();
                        if (SelectedCantidadTurnos > 1)
                        {
                            Shift2Operators = dbOperators.Where(x => x.Turno == names[1]).ToList();
                        }
                        if (SelectedCantidadTurnos > 2)
                        {
                            Shift3Operators = dbOperators.Where(x => x.Turno == names[2]).ToList();
                        }

                        // Add new/missing Odoo operators to Unassigned
                        var savedOperatorIds = dbOperators.Select(o => o.EmpleadoId).ToHashSet();
                        var missingOperators = AllOperators.Where(o => !savedOperatorIds.Contains(o.Id)).ToList();
                        foreach (var op in missingOperators)
                        {
                            UnassignedOperators.Add(new ProgramacionTurnoDto
                            {
                                EmpleadoId = op.Id,
                                NombreCompleto = op.Name,
                                Puesto = op.JobTitle,
                                Turno = "",
                                SupervisorId = null
                            });
                        }
                    }
                    else
                    {
                        // New week: put all operators in Unassigned pool
                        foreach (var op in AllOperators)
                        {
                            UnassignedOperators.Add(new ProgramacionTurnoDto
                            {
                                EmpleadoId = op.Id,
                                NombreCompleto = op.Name,
                                Puesto = op.JobTitle,
                                Turno = "",
                                SupervisorId = null
                            });
                        }
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

        private void MoveOperator(ProgramacionTurnoDto op, string targetShift)
        {
            // Remove from current list
            Shift1Operators.Remove(op);
            Shift2Operators.Remove(op);
            Shift3Operators.Remove(op);
            UnassignedOperators.Remove(op);

            var names = GetShiftNames();
            if (targetShift == "")
            {
                op.Turno = "";
                op.SupervisorId = null;
                UnassignedOperators.Add(op);
            }
            else if (targetShift == names[0])
            {
                op.Turno = names[0];
                op.SupervisorId = Shift1SupervisorId;
                Shift1Operators.Add(op);
            }
            else if (SelectedCantidadTurnos > 1 && targetShift == names[1])
            {
                op.Turno = names[1];
                op.SupervisorId = Shift2SupervisorId;
                Shift2Operators.Add(op);
            }
            else if (SelectedCantidadTurnos > 2 && targetShift == names[2])
            {
                op.Turno = names[2];
                op.SupervisorId = Shift3SupervisorId;
                Shift3Operators.Add(op);
            }

            StateHasChanged();
        }

        private void SwapSupervisors()
        {
            if (SelectedCantidadTurnos == 1)
            {
                Snackbar.Add("No hay suficientes turnos asignados para realizar un intercambio.", Severity.Warning);
                return;
            }

            if (SelectedCantidadTurnos == 2)
            {
                var temp = Shift1SupervisorId;
                Shift1SupervisorId = Shift2SupervisorId;
                Shift2SupervisorId = temp;
            }
            else if (SelectedCantidadTurnos == 3)
            {
                // Shift 1 -> Shift 2 -> Shift 3 -> Shift 1
                var temp = Shift1SupervisorId;
                Shift1SupervisorId = Shift3SupervisorId;
                Shift3SupervisorId = Shift2SupervisorId;
                Shift2SupervisorId = temp;
            }

            // Sync supervisors IDs in operators lists
            foreach (var op in Shift1Operators) op.SupervisorId = Shift1SupervisorId;
            foreach (var op in Shift2Operators) op.SupervisorId = Shift2SupervisorId;
            foreach (var op in Shift3Operators) op.SupervisorId = Shift3SupervisorId;

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

                // Add supervisor DTOs
                void AddSupervisorDto(int? supervisorId, string shiftName)
                {
                    if (supervisorId.HasValue)
                    {
                        var sup = AllSupervisors.FirstOrDefault(s => s.Id == supervisorId.Value);
                        if (sup != null)
                        {
                            combined.Add(new ProgramacionTurnoDto
                            {
                                EmpleadoId = sup.Id,
                                NombreCompleto = sup.Name,
                                Puesto = sup.JobTitle,
                                Turno = shiftName,
                                SupervisorId = null,
                                FechaInicio = SelectedMonday.Value,
                                FechaFin = fechaFin,
                                OperacionId = SelectedOperacionId.Value
                            });
                        }
                    }
                }

                AddSupervisorDto(Shift1SupervisorId, names[0]);
                if (SelectedCantidadTurnos > 1) AddSupervisorDto(Shift2SupervisorId, names[1]);
                if (SelectedCantidadTurnos > 2) AddSupervisorDto(Shift3SupervisorId, names[2]);

                // Add operators DTOs
                void PopulateOperatorsDto(List<ProgramacionTurnoDto> list, string shiftName, int? supervisorId)
                {
                    foreach (var op in list)
                    {
                        op.Turno = shiftName;
                        op.SupervisorId = supervisorId;
                        op.FechaInicio = SelectedMonday.Value;
                        op.FechaFin = fechaFin;
                        op.OperacionId = SelectedOperacionId.Value;
                        combined.Add(op);
                    }
                }

                PopulateOperatorsDto(Shift1Operators, names[0], Shift1SupervisorId);
                if (SelectedCantidadTurnos > 1) PopulateOperatorsDto(Shift2Operators, names[1], Shift2SupervisorId);
                if (SelectedCantidadTurnos > 2) PopulateOperatorsDto(Shift3Operators, names[2], Shift3SupervisorId);

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

        private int? GetShiftSupervisorId(int index)
        {
            if (index == 0) return Shift1SupervisorId;
            if (index == 1) return Shift2SupervisorId;
            return Shift3SupervisorId;
        }

        private void SetShiftSupervisorId(int index, int? value)
        {
            if (index == 0)
            {
                Shift1SupervisorId = value;
                foreach (var op in Shift1Operators) op.SupervisorId = value;
            }
            else if (index == 1)
            {
                Shift2SupervisorId = value;
                foreach (var op in Shift2Operators) op.SupervisorId = value;
            }
            else if (index == 2)
            {
                Shift3SupervisorId = value;
                foreach (var op in Shift3Operators) op.SupervisorId = value;
            }
            StateHasChanged();
        }

        private List<ProgramacionTurnoDto> GetShiftOperators(int index)
        {
            if (index == 0) return Shift1Operators;
            if (index == 1) return Shift2Operators;
            return Shift3Operators;
        }
    }
}
