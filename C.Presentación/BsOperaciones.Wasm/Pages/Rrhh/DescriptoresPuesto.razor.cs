using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Modelo.Comercial;
using HostService.Interfaces;
using MediatR;
using Microsoft.JSInterop;
using Modelo.Interfaces;

namespace BsOperaciones.Pages.Rrhh
{
    public partial class DescriptoresPuesto : ComponentBase
    {
        [Inject] public IOdooService OdooService { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] public IMediator Mediator { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IUserInfo UserInfo { get; set; }

        public List<JobDescription> JobDescriptionsList { get; set; } = new();
        public string SearchQuery { get; set; } = "";
        public JobDescription SelectedJob { get; set; }
        public bool isloading { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadJobDescriptions();
        }

        private void SelectJob(JobDescription job)
        {
            SelectedJob = job;
        }

        private async Task LoadJobDescriptions(int? selectId = null)
        {
            isloading = true;
            var response = await OdooService.GetJobDescriptions();
            isloading = false;
            if (!response.Respuesta.ExisteError && response.Model != null)
            {
                JobDescriptionsList = response.Model.ToList();
                if (selectId.HasValue)
                {
                    SelectedJob = JobDescriptionsList.FirstOrDefault(x => x.id == selectId.Value);
                }
                if (SelectedJob == null)
                {
                    SelectedJob = JobDescriptionsList.FirstOrDefault();
                }
            }
            else
            {
                Snackbar.Add("Error al cargar descriptores: " + response.Respuesta.MensajeError, Severity.Error);
            }
        }

        public List<JobDescription> FilteredJobs()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                return JobDescriptionsList;

            var q = SearchQuery.ToLower();
            return JobDescriptionsList.Where(j =>
                (j.title?.ToLower().Contains(q) ?? false) ||
                (j.objective?.ToLower().Contains(q) ?? false) ||
                (j.essential_functions?.Any(f => f.ToLower().Contains(q)) ?? false) ||
                (j.competencies?.Any(c => c.ToLower().Contains(q)) ?? false)
            ).ToList();
        }

        private async Task AddJob()
        {
            var parameters = new DialogParameters
            {
                ["Title"] = "Agregar Descriptor de Puesto",
                ["payload"] = new JobDescription(),
                ["IsEdit"] = false
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = DialogService.Show<DialogAddUpdJobDescription>("Agregar Descriptor de Puesto", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await LoadJobDescriptions();
            }
        }

        private async Task EditJob()
        {
            if (SelectedJob == null) return;

            var clone = new JobDescription
            {
                id = SelectedJob.id,
                title = SelectedJob.title,
                tab_icon = SelectedJob.tab_icon,
                department = SelectedJob.department,
                reports_to = SelectedJob.reports_to,
                supervises = SelectedJob.supervises,
                shift = SelectedJob.shift,
                employment_type = SelectedJob.employment_type,
                objective = SelectedJob.objective,
                education = SelectedJob.education,
                experience = SelectedJob.experience,
                technical_knowledge = SelectedJob.technical_knowledge,
                tools_languages = SelectedJob.tools_languages,
                horary = SelectedJob.horary,
                epp_requirements = SelectedJob.epp_requirements,
                risks = SelectedJob.risks,
                essential_functions = SelectedJob.essential_functions != null ? SelectedJob.essential_functions.ToList() : new(),
                occasional_functions = SelectedJob.occasional_functions != null ? SelectedJob.occasional_functions.ToList() : new(),
                competencies = SelectedJob.competencies != null ? SelectedJob.competencies.ToList() : new(),
                kpis = SelectedJob.kpis != null ? SelectedJob.kpis.ToList() : new()
            };

            var parameters = new DialogParameters
            {
                ["Title"] = "Editar Descriptor de Puesto",
                ["payload"] = clone,
                ["IsEdit"] = true
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = DialogService.Show<DialogAddUpdJobDescription>("Editar Descriptor de Puesto", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await LoadJobDescriptions(clone.id);
            }
        }

        private async Task DeleteJob()
        {
            if (SelectedJob == null) return;

            var confirm = await DialogService.ShowMessageBox(
                "Eliminar Descriptor",
                $"¿Está seguro de eliminar el descriptor de '{SelectedJob.title}'?",
                yesText: "Eliminar", cancelText: "Cancelar");

            if (confirm == true)
            {
                isloading = true;
                var response = await OdooService.DeleteJobDescription(SelectedJob.id);
                isloading = false;
                if (!response.Respuesta.ExisteError)
                {
                    Snackbar.Add("Descriptor eliminado con éxito.", Severity.Success);
                    SelectedJob = null;
                    await LoadJobDescriptions();
                }
                else
                {
                    Snackbar.Add("Error al eliminar: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
        }

        private async Task PrintDescriptorPdf()
        {
            if (SelectedJob == null) return;

            try
            {
                isloading = true;
                var res = await Mediator.Send(new BsOperaciones.Application.Features.Comercial.Queries.PrintDescriptorPdfQuery(SelectedJob.id));
                isloading = false;
                if (!res.Respuesta.ExisteError && res.Model != null)
                {
                    var fileBytes = System.Convert.FromBase64String(res.Model.File);
                    await JSRuntime.InvokeVoidAsync("saveAsFile", $"Descriptor_{SelectedJob.title.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                }
                else
                {
                    Snackbar.Add("Error al generar PDF: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (System.Exception ex)
            {
                isloading = false;
                Snackbar.Add("Error inesperado al imprimir: " + ex.Message, Severity.Error);
            }
        }
    }
}
