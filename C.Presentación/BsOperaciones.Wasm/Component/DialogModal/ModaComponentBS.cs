using System.Threading.Tasks;
using MudBlazor;

namespace BsOperaciones.Component.DialogModal
{
    public class ModaComponentBS
    {
        private async Task ShowModal(string titulo, string mensaje, Severity color, IDialogService dialogService)
        {
            var parameters = new DialogParameters
            {
                { "Titulo", titulo },
                { "Mensaje", mensaje },
                { "AlertSeverity", color }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            await dialogService.ShowAsync<MessageModal>(titulo, parameters, options);
        }

        public Task setErrorInfo(string mensaje, IDialogService dialogService)
            => ShowModal("Mensaje informativo", mensaje, Severity.Info, dialogService);

        public Task setError(string mensaje, IDialogService dialogService)
            => ShowModal("Mensaje de error", mensaje, Severity.Error, dialogService);

        public Task setErrorWarn(string mensaje, IDialogService dialogService)
            => ShowModal("Mensaje de advertencia", mensaje, Severity.Warning, dialogService);
        public Task setSuccess(string mensaje, IDialogService dialogService)
            => ShowModal("Mensaje de exito", mensaje, Severity.Success, dialogService);       
    }
}

