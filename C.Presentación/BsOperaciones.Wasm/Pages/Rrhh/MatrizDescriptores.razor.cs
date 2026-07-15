using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MediatR;
using BsOperaciones.Application.Features.Comercial.Queries;
using Modelo.Comercial;
using HostService.Interfaces;

namespace BsOperaciones.Pages.Rrhh
{
    public partial class MatrizDescriptores : ComponentBase
    {
        [Inject] private IMediator Mediator { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private IJSRuntime JS { get; set; } = null!;
        [Inject] private IOdooService OdooService { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; } = null!;

        public List<JobDescription> JobDescriptionsList { get; set; } = new();
        public List<string> SelectedJobTitles { get; set; } = new();
        public List<JobFunction> JobFunctionsList { get; set; } = new();
        public List<RaciAssignment> RaciAssignmentsList { get; set; } = new();
        public List<JobMatrix> JobMatricesList { get; set; } = new();
        public int SelectedMatrixId { get; set; }
        
        private Dictionary<(int funcionId, int puestoId), string> RaciState { get; set; } = new();

        public List<JobDescription> DisplayedJobDescriptions => SelectedMatrixId > 0 && JobMatricesList.Any(m => m.id == SelectedMatrixId) 
            ? JobDescriptionsList.Where(j => JobMatricesList.First(m => m.id == SelectedMatrixId).puestoIds.Contains(j.id)).ToList() 
            : JobDescriptionsList;

        public List<JobFunction> DisplayedJobFunctions => SelectedMatrixId > 0 
            ? JobFunctionsList.Where(f => f.matriz_id == SelectedMatrixId).ToList() 
            : JobFunctionsList;

        public bool isLoading = false;
        public bool isPrinting = false;
        public bool isSaving = false;
        public bool hasChanges = false;
        public bool isManageFunctionsOpen = false;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            try
            {
                await LoadJobDescriptions();
                await LoadJobMatrices();
                if (JobMatricesList.Any())
                {
                    SelectedMatrixId = JobMatricesList.First().id;
                }
                await LoadJobFunctions();
                await LoadRaciAssignments();
                ApplyMatrixFiltering();
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadJobMatrices()
        {
            var response = await OdooService.GetJobMatrices();
            if (!response.Respuesta.ExisteError && response.Model != null)
            {
                JobMatricesList = response.Model.ToList();
            }
            else
            {
                Snackbar.Add("Error al cargar matrices: " + response.Respuesta.MensajeError, Severity.Error);
            }
        }

        private void OnSelectedMatrixChanged(int matrixId)
        {
            SelectedMatrixId = matrixId;
            ApplyMatrixFiltering();
            hasChanges = false;
        }

        private async Task OpenManageMatricesDialog()
        {
            var parameters = new DialogParameters { ["AllJobs"] = JobDescriptionsList };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = DialogService.Show<DialogManageMatrices>("Gestionar Matrices", parameters, options);
            await dialog.Result;
            await LoadJobMatrices();
            if (SelectedMatrixId == 0 || !JobMatricesList.Any(m => m.id == SelectedMatrixId))
            {
                SelectedMatrixId = JobMatricesList.FirstOrDefault()?.id ?? 0;
            }
            ApplyMatrixFiltering();
        }

        private void ApplyMatrixFiltering()
        {
            var matrix = JobMatricesList.FirstOrDefault(m => m.id == SelectedMatrixId);
            if (matrix != null)
            {
                var targetPuestoIds = matrix.puestoIds ?? new List<int>();
                var targetTitles = JobDescriptionsList.Where(j => targetPuestoIds.Contains(j.id)).Select(j => j.title).ToList();
                SelectedJobTitles = targetTitles;
            }
            else
            {
                SelectedJobTitles = JobDescriptionsList.Select(j => j.title).ToList();
            }
            StateHasChanged();
        }

        private async Task LoadJobDescriptions()
        {
            var response = await OdooService.GetJobDescriptions();
            if (!response.Respuesta.ExisteError && response.Model != null)
            {
                JobDescriptionsList = response.Model.ToList();
            }
            else
            {
                Snackbar.Add("Error al cargar puestos: " + response.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task LoadJobFunctions()
        {
            var response = await OdooService.GetJobFunctions();
            if (!response.Respuesta.ExisteError && response.Model != null)
            {
                JobFunctionsList = response.Model.ToList();
            }
            else
            {
                Snackbar.Add("Error al cargar actividades: " + response.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task LoadRaciAssignments()
        {
            var response = await OdooService.GetRaciAssignments();
            if (!response.Respuesta.ExisteError && response.Model != null)
            {
                RaciAssignmentsList = response.Model.ToList();
                RaciState.Clear();
                foreach (var assign in RaciAssignmentsList)
                {
                    RaciState[(assign.funcion_id, assign.puesto_id)] = assign.rol_raci;
                }
            }
            else
            {
                Snackbar.Add("Error al cargar asignaciones RACI: " + response.Respuesta.MensajeError, Severity.Error);
            }
        }

        private void ToggleJobSelection(string title)
        {
            if (SelectedJobTitles.Contains(title))
            {
                if (SelectedJobTitles.Count > 1) // Keep at least one selected
                {
                    SelectedJobTitles.Remove(title);
                }
                else
                {
                    Snackbar.Add("Debe seleccionar al menos un puesto para comparar.", Severity.Warning);
                }
            }
            else
            {
                SelectedJobTitles.Add(title);
            }
            StateHasChanged();
        }

        private string GetRaciValue(int funcionId, int puestoId)
        {
            if (RaciState.TryGetValue((funcionId, puestoId), out var val))
            {
                return val;
            }
            return "";
        }

        private void SetRaciValue(int funcionId, int puestoId, string val)
        {
            RaciState[(funcionId, puestoId)] = val;
            hasChanges = true;
            StateHasChanged();
        }

        private string GetRaciClass(string val)
        {
            return val switch
            {
                "R" => "raci-select-r",
                "A" => "raci-select-a",
                "C" => "raci-select-c",
                "I" => "raci-select-i",
                _ => ""
            };
        }

        private async Task SaveRaciMatrix()
        {
            isSaving = true;
            StateHasChanged();
            try
            {
                var listToSave = RaciState.Select(kvp => new RaciAssignment
                {
                    funcion_id = kvp.Key.funcionId,
                    puesto_id = kvp.Key.puestoId,
                    rol_raci = kvp.Value
                }).ToList();

                var res = await OdooService.SaveRaciAssignments(listToSave);
                if (!res.Respuesta.ExisteError)
                {
                    Snackbar.Add("Matriz RACI guardada con éxito.", Severity.Success);
                    hasChanges = false;
                    await LoadRaciAssignments();
                }
                else
                {
                    Snackbar.Add("Error al guardar la matriz: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error inesperado: " + ex.Message, Severity.Error);
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        public async Task PrintMatrixPdf()
        {
            isPrinting = true;
            StateHasChanged();
            try
            {
                var matrixName = JobMatricesList.FirstOrDefault(m => m.id == SelectedMatrixId)?.nombre ?? "";
                
                var selectedJobs = JobDescriptionsList
                    .Where(j => SelectedJobTitles.Contains(j.title))
                    .Select(j => new { id = j.id, title = j.title, department = j.department })
                    .ToList();

                var displayedFunctions = DisplayedJobFunctions
                    .Select(f => new { id = f.id, area = f.area, actividad = f.actividad, orden = f.orden })
                    .ToList();

                var assignments = new List<object>();
                foreach (var func in DisplayedJobFunctions)
                {
                    foreach (var job in JobDescriptionsList.Where(j => SelectedJobTitles.Contains(j.title)))
                    {
                        var val = GetRaciValue(func.id, job.id);
                        if (!string.IsNullOrEmpty(val))
                        {
                            assignments.Add(new { funcion_id = func.id, puesto_id = job.id, rol_raci = val });
                        }
                    }
                }

                var payload = new
                {
                    MatrixName = matrixName,
                    Jobs = selectedJobs,
                    Functions = displayedFunctions,
                    Assignments = assignments
                };

                var res = await Mediator.Send(new PrintMatrizDescriptoresPdfQuery(payload));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", "Matriz_Responsabilidades_RACI.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Matriz RACI generada y descargada.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar PDF: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error inesperado: " + ex.Message, Severity.Error);
            }
            finally
            {
                isPrinting = false;
                StateHasChanged();
            }
        }

        private void OpenManageFunctionsDialog()
        {
            isManageFunctionsOpen = true;
            StateHasChanged();
        }

        private void CloseManageFunctionsDialog()
        {
            isManageFunctionsOpen = false;
            StateHasChanged();
        }

        private async Task OpenAddFunctionDialog()
        {
            var existingAreas = DisplayedJobFunctions.Select(f => f.area).Distinct().ToList();
            var parameters = new DialogParameters 
            { 
                ["JobFunc"] = new JobFunction { matriz_id = SelectedMatrixId },
                ["ExistingAreas"] = existingAreas
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = DialogService.Show<DialogAddUpdJobFunction>("Agregar Actividad", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is JobFunction payload)
            {
                var response = await OdooService.SaveJobFunction(payload);
                if (!response.Respuesta.ExisteError)
                {
                    Snackbar.Add("Actividad creada con éxito.", Severity.Success);
                    await LoadJobFunctions();
                }
                else
                {
                    Snackbar.Add("Error al crear actividad: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
        }

        private async Task OpenEditFunctionDialog(JobFunction func)
        {
            var existingAreas = DisplayedJobFunctions.Select(f => f.area).Distinct().ToList();
            var parameters = new DialogParameters 
            { 
                ["JobFunc"] = func,
                ["ExistingAreas"] = existingAreas
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = DialogService.Show<DialogAddUpdJobFunction>("Editar Actividad", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is JobFunction payload)
            {
                var response = await OdooService.SaveJobFunction(payload);
                if (!response.Respuesta.ExisteError)
                {
                    Snackbar.Add("Actividad actualizada con éxito.", Severity.Success);
                    await LoadJobFunctions();
                }
                else
                {
                    Snackbar.Add("Error al actualizar actividad: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
        }

        private async Task DeleteFunction(int id)
        {
            bool? confirm = await DialogService.ShowMessageBox(
                "Eliminar Actividad",
                "¿Está seguro de que desea eliminar esta actividad? Se eliminarán todas las asignaciones RACI vinculadas a ella.",
                yesText: "Sí, eliminar", cancelText: "Cancelar"
            );

            if (confirm == true)
            {
                var response = await OdooService.DeleteJobFunction(id);
                if (!response.Respuesta.ExisteError)
                {
                    Snackbar.Add("Actividad eliminada con éxito.", Severity.Success);
                    await LoadJobFunctions();
                    await LoadRaciAssignments();
                }
                else
                {
                    Snackbar.Add("Error al eliminar actividad: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
        }
    }
}
