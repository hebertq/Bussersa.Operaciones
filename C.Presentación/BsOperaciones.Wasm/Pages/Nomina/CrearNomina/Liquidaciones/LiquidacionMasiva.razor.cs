using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Command;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.Entidades.Nomina;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.CrearNomina.Liquidaciones
{
    public partial class LiquidacionMasiva : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }

        protected List<SeveranceDetail> ListaPendientes = new();
        protected DateTime? pidt_inicio = DateTime.Now.Date.AddDays(-15), pidt_fin = DateTime.Now.Date;
        protected string piv_nombre = "";
        protected bool isProcessing = false;

        protected override async Task OnInitializedAsync() => await CargarPendientes();

        private async Task CargarPendientes()
        {
            isProcessing = true;
            try
            {
                var result = await _mediator.Send(new GetPendingSeveranceQuery());
                ListaPendientes = result.Model?.ToList() ?? new();
            }
            finally { isProcessing = false; }
        }

        protected async Task AbrirDialogoEdicion(SeveranceDetail item)
        {
            // Clonamos el objeto para evitar que cambios no guardados se vean en la grilla si cancela
            var itemClonado = JsonSerializer.Deserialize<SeveranceDetail>(JsonSerializer.Serialize(item));

            var parameters = new DialogParameters { ["Item"] = itemClonado };
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackdropClick = false,
                CloseOnEscapeKey = false
            };

            var dialog = await DialogService.ShowAsync<DialogEditarLiquidacion>("Editar Datos de Liquidación", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data != null)
            {
                var index = ListaPendientes.FindIndex(x => x.id_severance == item.id_severance);
                if (index != -1)
                {
                    ListaPendientes[index] = (SeveranceDetail)result.Data;
                    StateHasChanged();
                    Snackbar.Add("Cambios aplicados localmente", Severity.Info);
                }
            }
        }

        protected async Task ExportarExcel()
        {
            if (!ListaPendientes.Any()) return;
            isProcessing = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                var base64 = DataExcel.CreateExcel(ListaPendientes, "Previa_Liquidaciones");
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64, $"Revision_Liquidacion_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex) { Snackbar.Add("Error al exportar: " + ex.Message, Severity.Error); }
            finally
            {
                isProcessing = false;
            }
        }

        protected void QuitarDeLista(int id) => ListaPendientes.RemoveAll(x => x.id_severance == id);

        protected async Task ProcesarLote()
        {
            if (string.IsNullOrWhiteSpace(piv_nombre)) { Snackbar.Add("Debe ingresar un nombre para el lote.", Severity.Warning); return; }

            isProcessing = true;
            try
            {
                var command = new CreateSettlementPayrollCommand
                {
                    Inicio = pidt_inicio.Value,
                    Fin = pidt_fin.Value,
                    Nombre = piv_nombre,
                    model = ListaPendientes
                };

                var response = await _mediator.Send(command);

                Snackbar.Add(response.Respuesta.MensajeError, Severity.Success);
                piv_nombre = "";
                await CargarPendientes();
            }
            finally { isProcessing = false; }
        }
    }
}
