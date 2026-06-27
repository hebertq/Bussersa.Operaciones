using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Component.DialogModal;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Pages.Nomina.CierreNomina.Activas
{
    public partial class CerrarNomina : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUserInfo _Iuser { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        public List<nominatype> PayLoadList { get; set; } = new();
        public List<Combos> PayLoadNomina { get; set; } = new();
        public nominatype payload { get; set; } = new();
        public ModaComponentBS ErrorModal { get; set; } = new();
        public int operacion { set; get; } = 0;
        public bool isloaddata { set; get; } = false;
        public bool BotonEnabled { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await CargarNominas();
        }

        private async Task CargarNominas()
        {
            var regop = await _mediator.Send(new GetAllCombosQuery("Nominas"));
            PayLoadNomina = regop.Model;
        }

        protected async Task OnChangeNomina(int value)
        {
            PayLoadList = new List<nominatype>();
            operacion = value;

            if (operacion > 0)
            {
                isloaddata = true;
                try
                {
                    await GetAllMarcadasFacturar();
                    BotonEnabled = PayLoadList.Count == 0;
                }
                finally
                {
                    isloaddata = false;
                }
            }
            else
            {
                BotonEnabled = true;
            }
        }

        private async Task GetAllMarcadasFacturar()
        {
            var registros = await _mediator.Send(new GetAllPagoNominaQuery(operacion));
            if (registros.Model.Count > 0)
            {
                PayLoadList = registros.Model;
                StateHasChanged();
            }
        }

        protected async Task RefrescarCierre()
        {
            bool? result = await DialogService.ShowMessageBox(
                "Confirmación de Cierre",
                "¿Está seguro que desea cerrar esta nómina? Esta acción no se puede deshacer.",
                yesText: "Sí, Cerrar", cancelText: "Cancelar");

            if (result == true)
            {
                isloaddata = true;
                try
                {
                    await CerrarNominas();
                }
                finally
                {
                    isloaddata = false;
                }
            }
        }

        private async Task CerrarNominas()
        {
            if (operacion == 0)
            {
                Snackbar.Add("Debe seleccionar una nómina válida", Severity.Warning);
                return;
            }

            var respuesta = await _mediator.Send(new CerrarNominaActivasCommand(operacion));
            if (respuesta.Respuesta.ExisteError)
            {
                await ErrorModal.setError(respuesta.Respuesta.MensajeError, DialogService);
            }
            else
            {
                Snackbar.Add("Nómina cerrada exitosamente", Severity.Success);
                operacion = 0;
                PayLoadList.Clear();
                await CargarNominas();
                BotonEnabled = true;
                StateHasChanged();
            }
        }
    }
}
