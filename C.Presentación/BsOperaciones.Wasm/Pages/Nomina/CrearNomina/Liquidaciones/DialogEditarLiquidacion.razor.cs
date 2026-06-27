using Microsoft.AspNetCore.Components;
using MudBlazor;
using Modelo.Entidades.Nomina;

namespace BsOperaciones.Pages.Nomina.CrearNomina.Liquidaciones
{
    public partial class DialogEditarLiquidacion : ComponentBase
    {
        [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; }
        [Parameter] public SeveranceDetail Item { get; set; }

        protected void Submit() => MudDialog.Close(DialogResult.Ok(Item));
        protected void Cancel() => MudDialog.Cancel();
    }
}
