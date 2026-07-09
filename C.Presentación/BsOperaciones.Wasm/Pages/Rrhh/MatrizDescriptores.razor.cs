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
        [Inject] private IMediator Mediator { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private IOdooService OdooService { get; set; }

        public List<JobDescription> JobDescriptionsList { get; set; } = new();
        public List<string> SelectedJobTitles { get; set; } = new();
        public bool isPrinting = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadJobDescriptions();
            // Select all by default
            SelectedJobTitles = JobDescriptionsList.Select(j => j.title).ToList();
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

        public async Task PrintMatrixPdf()
        {
            isPrinting = true;
            StateHasChanged();
            try
            {
                var res = await Mediator.Send(new PrintMatrizDescriptoresPdfQuery());
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", "Matriz_Descriptores_Puestos.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Matriz de descriptores generada y descargada.", Severity.Success);
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

        public async Task PrintJobPdf(string jobTitle)
        {
            isPrinting = true;
            StateHasChanged();
            try
            {
                var res = await Mediator.Send(new PrintDescriptorPdfQuery(jobTitle));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Descriptor_{jobTitle.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add($"Descriptor de {jobTitle} generado y descargado.", Severity.Success);
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
    }
}
