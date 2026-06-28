using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Command;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;

namespace BsOperaciones.Pages.Operaciones.MarcadasxFacturarxDia
{
    public partial class MarcadasxFacturarxDia : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }

        public List<DiasTrabajadosAreas> PayLoadList { get; set; } = new();
        public List<Combos> PayLoadOper { get; set; } = new();
        public int operacion { set; get; } = 0;
        public bool isloaddata { set; get; } = false;
        public bool BotonEnabled { set; get; } = true;
        public string nombreOperacion { set; get; } = "";

        public DateTime FchaDesde { set; get; } = DateTime.Now.AddDays(-5);
        public DateTime FchaHasta { set; get; } = DateTime.Now;
        protected DateTime? _fchaDesdeWrapper { get => FchaDesde; set => FchaDesde = value ?? DateTime.Now.AddDays(-5); }
        protected DateTime? _fchaHastaWrapper { get => FchaHasta; set => FchaHasta = value ?? DateTime.Now; }

        protected override async Task OnInitializedAsync()
        {
            var regop = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
            PayLoadOper = regop.Model ?? new();
        }

        protected async Task OnChangeCliente(int value)
        {
            operacion = value;
            if (operacion > 0)
            {
                BotonEnabled = true;
                await GetAllMarcadasFacturar();
            }
        }

        protected void HandleValidationChange(bool deshabilitar)
        {
            InvokeAsync(() =>
            {
                if (BotonEnabled != deshabilitar)
                {
                    BotonEnabled = deshabilitar;
                    StateHasChanged();
                }
            });
        }

        protected async Task GetAllMarcadasFacturar()
        {
            isloaddata = true;
            try
            {
                var opSelected = PayLoadOper.FirstOrDefault(x => x.id == operacion);
                nombreOperacion = opSelected?.nombre ?? "";
                typeeinout rango = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = operacion };

                // 1. Traer pendientes
                var registrosPendientes = await _mediator.Send(new GetAllMarcadasFacturarQuery(rango));
                // 2. Traer cerrados
                var registrosCerrados = await _mediator.Send(new GetReporteCierreClienteRangoQuery(operacion, FchaDesde, FchaHasta));

                var listaFinal = registrosPendientes.Model ?? new();

                // 3. Marcar los registros cerrados antes de unirlos o compararlos
                if (registrosCerrados.Model != null)
                {
                    foreach (var cerrado in registrosCerrados.Model)
                    {
                        var itemExistente = listaFinal.FirstOrDefault(x => x.id == cerrado.id_empleado && x.tarea == cerrado.tarea_id);
                        if (itemExistente != null && itemExistente.id > 0)
                        {
                            // Marcamos con una bandera especial o comentario para la lógica de alertas
                            itemExistente.cierre = true;
                        }      
                    }
                }

                PayLoadList = listaFinal ?? new();
            }
            finally { isloaddata = false; }
        }

        protected async Task ConfirmarCierre()
        {
            bool? result = await DialogService.ShowMessageBox("Confirmar Cierre", $"¿Desea cerrar {PayLoadList.Count} registros?", yesText: "Confirmar", cancelText: "Cancelar");
            if (result == true) await EjecutarCierreMarcadas();
        }

        protected async Task EjecutarCierreMarcadas()
        {
            isloaddata = true;
            try
            {
                var response = await _mediator.Send(new AddCierreMarcadasCommand(PayLoadList, operacion));
                if (!response.Respuesta.ExisteError)
                {
                    Snackbar.Add("Cierre exitoso.", Severity.Success);
                    PayLoadList.Clear();
                    BotonEnabled = true;
                }
                else Snackbar.Add(response.Respuesta.MensajeError, Severity.Error);
            }
            finally { isloaddata = false; }
        }

        protected async Task OnDesdeDateChanged(DateTime? value)
        {
            _fchaDesdeWrapper = value;
            if (operacion > 0)
            {
                await GetAllMarcadasFacturar();
            }
        }

        protected async Task OnHastaDateChanged(DateTime? value)
        {
            _fchaHastaWrapper = value;
            if (operacion > 0)
            {
                await GetAllMarcadasFacturar();
            }
        }
    }
}
