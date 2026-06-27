using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Component.DialogModal;
using Modelo.ClasesGenericas;
using Utilidades.Interfaces;
using Modelo.Interfaces;
using Modelo.Entidades.Entradas.Odoo;
using OdooDiasTrabajados = Modelo.Entidades.Entradas.Odoo.DiasTrabajados;

namespace BsOperaciones.Pages.Nomina.Asistencias.DiasTrabajados
{
    public partial class Diastrabajados : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected IUserInfo _Iuser { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        public List<Combos> PayLoadOper { get; set; } = new List<Combos>(); 
        public OdooDiasTrabajados payload { get; set; } = new OdooDiasTrabajados();
        public ModaComponentBS ErrorModal { get; set; } = new ModaComponentBS();
        
        public DateTime FchaDesde { set; get; } = DateTime.Now.AddDays(-5);
        public DateTime FchaHasta { set; get; } = DateTime.Now;
        
        // Wrappers para MudDatePicker (convierten DateTime a DateTime?)
        protected DateTime? _fchaDesdeWrapper { get => FchaDesde; set => FchaDesde = value ?? DateTime.Now.AddDays(-5); }
        protected DateTime? _fchaHastaWrapper { get => FchaHasta; set => FchaHasta = value ?? DateTime.Now; }

        public DateTime HoraHasta { get; set; } 
        public DateTime HoraDesde { get; set; } 
        public int operacion { set; get; } = 0;
        public int operacioncrud { set; get; } = 0;
        public bool isloading { set; get; } = false;
        public string formTitle { set; get; }
        public bool BotonEnabled { set; get; } = true;
        protected string NombreOperacion { set; get; } = "";

        protected IList<DiasxempleadosOpera?> PayLoadList { get; set; } = new List<DiasxempleadosOpera>();

        protected override async Task OnInitializedAsync()
        {
            var regop = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
            PayLoadOper = regop.Model; 
        }

        protected async Task OnUpdate(OdooDiasTrabajados reg)
        {
            // 1. Identificar si es Insert (1) o Update (2)
            int tipoOperacion = reg.idmarca == 0 ? 1 : 2;

            // 2. Configurar Parámetros para tu componente AddUpdDiasTrabajados
            var parameters = new DialogParameters
            {
                { "payload", reg },
                { "ReportName", tipoOperacion == 1 ? "Agregar Día Laborado" : "Editar Día Laborado" },
                { "Operacion", operacion } // Esto es lo que recibe tu componente para el Mediator
            };

            // 3. Opciones del Modal
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                BackdropClick = false
            };

            // 4. Invocar tu componente
            var dialog = await DialogService.ShowAsync<AddUpdDiasTrabajados>("Gestión de Asistencia", parameters, options);
            var result = await dialog.Result;

            // 5. Procesar el resultado cuando cierras el modal con MudDialog.Close(DialogResult.Ok(response))
            if (!result.Canceled && result.Data != null)
            {
                var response = (SingleResponse<OdooDiasTrabajados>)result.Data;

                if (!response.Respuesta.ExisteError)
                {
                    // Refrescar la lista local para que el usuario vea el cambio sin recargar la página
                    await GetAllMarcadas(); // Recargamos de la base de datos para asegurar consistencia
                    Snackbar.Add("Cambios sincronizados con éxito", Severity.Success);
                }
            }
        }

        protected async Task OnDelete(OdooDiasTrabajados reg)
        {
            if (reg == null || reg.idmarca == 0)
            {
                Snackbar.Add("Seleccione un registro válido para eliminar", Severity.Warning);
                return;
            }

            // El diálogo de confirmación se muestra aquí en el Padre
            bool? confirm = await DialogService.ShowMessageBox(
                "Confirmar Eliminación",
                $"¿Desea eliminar la marcación del empleado {reg.nombre} el día {reg.fecha:dd/MM/yyyy}?",
                yesText: "Eliminar",
                cancelText: "Cancelar"
            );

            if (confirm == true)
            {
                isloading = true; // Mostramos el loader corporativo
                try
                {
                    // Ejecutamos el comando original (Operación 3 = Delete)
                    var response = await _mediator.Send(new AddMarcadasIdCommand(reg, 3));

                    if (!response.Respuesta.ExisteError)
                    {
                        Snackbar.Add("Registro eliminado correctamente", Severity.Success);
                        await GetAllMarcadas(); // Refrescamos la lista de la DB
                    }
                    else
                    {
                        await ErrorModal.setError(response.Respuesta.MensajeError, DialogService);
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add("Error al eliminar: " + ex.Message, Severity.Error);
                }
                finally
                {
                    isloading = false;
                    StateHasChanged();
                }
            }
        }

        protected async Task OnChangeCliente(int value)
        {
            operacion = value;
            var selected = PayLoadOper.FirstOrDefault(x => x.id == operacion);
            NombreOperacion = selected?.nombre ?? "";      
            await GetAllMarcadas(); 
            BotonEnabled = PayLoadList.Count > 0 ? false : true;
        }

        protected async Task GetAllMarcadas()
        {
            if (operacion == 0) return;

            isloading = true;
            try 
            {
                typeeinout rango = new typeeinout { entrada = FchaDesde, salida = FchaHasta, id = operacion };
                PayLoadList = new List<DiasxempleadosOpera>();

                var dataload = await _mediator.Send(new GetAllDiasTrabajadosOperacionQuery(rango));
                if (dataload.Respuesta.ExisteError)
                {
                    Snackbar.Add(dataload.Respuesta.MensajeError, Severity.Error);
                }      
                else
                {
                    PayLoadList = dataload.Model ?? new List<DiasxempleadosOpera>();
                    if (!PayLoadList.Any()) Snackbar.Add("No hay datos para el rango seleccionado", Severity.Info);
                } 
            }
            finally 
            {
                isloading = false;
            }
        }
    }
}
