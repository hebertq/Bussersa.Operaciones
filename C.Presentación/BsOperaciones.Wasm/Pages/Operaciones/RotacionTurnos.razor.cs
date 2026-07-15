using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MediatR;
using Modelo.ClasesGenericas;
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

        private ProgramacionTurnoDto? DaySupervisor { get; set; }
        private ProgramacionTurnoDto? NightSupervisor { get; set; }
        private List<ProgramacionTurnoDto> DayOperators { get; set; } = new();
        private List<ProgramacionTurnoDto> NightOperators { get; set; } = new();

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
                DaySupervisor = null;
                NightSupervisor = null;
                DayOperators.Clear();
                NightOperators.Clear();
                StateHasChanged();
            }
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
            try
            {
                var result = await Mediator.Send(new GetProgramacionTurnosQuery(monday, SelectedOperacionId.Value));
                if (!result.Respuesta.ExisteError && result.Model != null)
                {
                    var allItems = result.Model.ToList();

                    // Split supervisors and operators
                    var supervisors = allItems.Where(x => x.Puesto == "Supervisor de personal").ToList();
                    var operators = allItems.Where(x => x.Puesto != "Supervisor de personal").ToList();

                    DaySupervisor = supervisors.FirstOrDefault(x => x.Turno == "Diurno");
                    NightSupervisor = supervisors.FirstOrDefault(x => x.Turno == "Nocturno");

                    // If database didn't have turnos ordered, assign default
                    if (DaySupervisor == null && supervisors.Any())
                    {
                        DaySupervisor = supervisors.First();
                        DaySupervisor.Turno = "Diurno";
                    }
                    if (NightSupervisor == null && supervisors.Count > 1)
                    {
                        NightSupervisor = supervisors.Skip(1).First();
                        NightSupervisor.Turno = "Nocturno";
                    }

                    DayOperators = operators.Where(x => x.Turno == "Diurno").ToList();
                    NightOperators = operators.Where(x => x.Turno == "Nocturno").ToList();
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

        private void ToggleShift(ProgramacionTurnoDto op)
        {
            if (op.Turno == "Diurno")
            {
                op.Turno = "Nocturno";
                op.SupervisorId = NightSupervisor?.EmpleadoId;
                DayOperators.Remove(op);
                NightOperators.Add(op);
            }
            else
            {
                op.Turno = "Diurno";
                op.SupervisorId = DaySupervisor?.EmpleadoId;
                NightOperators.Remove(op);
                DayOperators.Add(op);
            }
            StateHasChanged();
        }

        private void SwapSupervisors()
        {
            if (DaySupervisor == null || NightSupervisor == null)
            {
                Snackbar.Add("Debe haber dos supervisores asignados para realizar el intercambio.", Severity.Warning);
                return;
            }

            // Swap supervisors shifts
            DaySupervisor.Turno = "Nocturno";
            NightSupervisor.Turno = "Diurno";

            // Swap properties
            var temp = DaySupervisor;
            DaySupervisor = NightSupervisor;
            NightSupervisor = temp;

            // Re-assign supervisor IDs to operators based on the new shift supervisors
            foreach (var op in DayOperators)
            {
                op.SupervisorId = DaySupervisor.EmpleadoId;
            }
            foreach (var op in NightOperators)
            {
                op.SupervisorId = NightSupervisor.EmpleadoId;
            }

            Snackbar.Add("Supervisores intercambiados. Recuerde guardar la programación.", Severity.Info);
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
                if (DaySupervisor != null)
                {
                    DaySupervisor.OperacionId = SelectedOperacionId.Value;
                    combined.Add(DaySupervisor);
                }
                if (NightSupervisor != null)
                {
                    NightSupervisor.OperacionId = SelectedOperacionId.Value;
                    combined.Add(NightSupervisor);
                }

                foreach (var op in DayOperators)
                {
                    op.OperacionId = SelectedOperacionId.Value;
                }
                foreach (var op in NightOperators)
                {
                    op.OperacionId = SelectedOperacionId.Value;
                }

                combined.AddRange(DayOperators);
                combined.AddRange(NightOperators);

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
    }
}
