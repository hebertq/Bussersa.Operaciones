using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Modelo.ClasesGenericas;
using System.Threading.Tasks;

namespace BsOperaciones.Component.DialogModal
{
    public partial class ModalDelete
    {
        [Inject]
        public IJSRuntime _js { get; set; }

        [Parameter]
        public EventCallback OnDeleted { get; set; }

        [Parameter]
        public ModalError ErrorModal { get; set; }

        [Parameter]
        public string ModalId { get; set; }

        private async Task CloseModal()
        {
            await _js.InvokeVoidAsync("global.closeModal", ModalId);
        }

        private async Task OnDeleteClick()
        {
            await OnDeleted.InvokeAsync();
            await _js.InvokeVoidAsync("global.closeModalNoty", new object[] { ModalId, ErrorModal });
        }
    }
}
