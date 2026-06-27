using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MediatR;
using MudBlazor;
using Modelo.Entidades.Entradas.Odoo;
using BsOperaciones.Application.Features.Odoo.Commands;
using OdooDiasTrabajados = Modelo.Entidades.Entradas.Odoo.DiasTrabajados;

namespace BsOperaciones.Pages.Nomina.Asistencias.DiasTrabajados
{
    public partial class AddUpdDiasTrabajados : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        [CascadingParameter]
        protected IMudDialogInstance MudDialog { get; set; } // Referencia para cerrar el diálogo

        [Parameter] public string ReportName { get; set; } = "Registro de Datos";
        [Parameter] public OdooDiasTrabajados payload { get; set; } = new OdooDiasTrabajados();
        [Parameter] public int Operacion { get; set; }

        public bool isloading { get; set; }

        // Wrappers para sincronizar los tipos de datos de MudBlazor con tu modelo
        protected DateTime? fechaWrapper
        {
            get => payload.fecha;
            set => payload.fecha = value ?? DateTime.Now;
        }

        protected TimeSpan? horaDesdeWrapper
        {
            get => TimeSpan.TryParse(payload.entrada, out var t) ? t : null;
            set => payload.entrada = value?.ToString(@"hh\:mm") + ":00";
        }

        protected TimeSpan? horaHastaWrapper
        {
            get => TimeSpan.TryParse(payload.salida, out var t) ? t : null;
            set => payload.salida = value?.ToString(@"hh\:mm") + ":00";
        }

        protected async Task OnUpdate()
        {
            try
            {
                isloading = true;
                // Enviamos el comando a través de Mediator
                var response = await _mediator.Send(new AddMarcadasIdCommand(payload, Operacion));
                isloading = false;

                Snackbar.Add("Datos registrados correctamente", Severity.Success);

                // CERRAMOS EL DIÁLOGO con éxito
                MudDialog.Close(DialogResult.Ok(response));
            }
            catch (Exception ex)
            {
                isloading = false;
                Snackbar.Add("Error al registrar: " + ex.Message, Severity.Error);
            }
        }

        // MÉTODO PARA SOLO CERRAR EL DIÁLOGO
        protected void Cancel() => MudDialog.Cancel();
    }
}
