using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Modelo.ClasesGenericas;
using System.Threading.Tasks;

namespace BsOperaciones.Component.DialogModal
{
    public partial class ModalDialog
    {
        [Parameter]
        public EventCallback OnUpdate { get; set; }

        [Parameter]
        public string ModalId { get; set; }

        [Parameter]
        public string FormTitle { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public ModalError ErrorModal { get; set; }

        [Inject]
        public IJSRuntime _js { get; set; }

        private async Task CloseModal()
        {
            await _js.InvokeVoidAsync("global.closeModal", ModalId);
        }

        private async Task OnUpdateClick()
        {
            await OnUpdate.InvokeAsync();
            await _js.InvokeVoidAsync("global.closeModalNoty", new object[] { ModalId, ErrorModal });
        }
    }
}
