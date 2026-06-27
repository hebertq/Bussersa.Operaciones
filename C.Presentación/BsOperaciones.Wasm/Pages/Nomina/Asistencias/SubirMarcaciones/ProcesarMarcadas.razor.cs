using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Common.Strategy.Reloj;
using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Component.DialogModal;
using Modelo.ClasesGenericas;
using Modelo.Enum;
using Modelo.Entidades.Entradas.Odoo;
using Utilidades.Interfaces;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.Asistencias.SubirMarcaciones
{
    public partial class ProcesarMarcadas : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected RelojSelector _relojSelector { get; set; }

        public List<HoraEntrada> PayLoadList { get; set; } = new();
        public List<Combos> PayLoadOper { get; set; } = new();
        public DateTime FchaCarga { set; get; } = DateTime.Now.Date.AddDays(-1);
        protected DateTime? FchaCargaWrapper { get => FchaCarga; set => FchaCarga = value ?? DateTime.Now.Date; }

        public TipoReloj? operacion { set; get; }
        public int? empresa { set; get; }
        public bool isloaddata { set; get; } = false;
        protected string _searchString = "";
        private long maxFileSize = 1024 * 1024 * 15;

        // Máscara para obligar al formato HH:mm:ss
        public PatternMask horaMask = new PatternMask("00:00:00");
        public ModaComponentBS ErrorModal { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            var regop = await _mediator.Send(new GetAllCombosQuery("Operaciones"));
            // Solo mostramos operaciones válidas (ID mayor a 0)
            PayLoadOper = regop.Model.Where(x => x.id > 0).ToList();
        }

        protected void OnChangeReloj(TipoReloj? val)
        {
            operacion = val;
            PayLoadList.Clear();
            empresa = null; 
            StateHasChanged();
        }

        protected async Task LoadFiles(InputFileChangeEventArgs e)
        {
            if (operacion == null) return;
            isloaddata = true;
            try
            {
                using var ms = new MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(ms);
                ms.Position = 0;

                var strategy = _relojSelector.ObtenerEstrategia(operacion.Value);
                var listaCruda = strategy.Parsear(ms, FchaCarga);

                if (listaCruda != null && listaCruda.Any())
                {
                    var emps = await _mediator.Send(new GetAllCombosQuery("Empleados"));
                    PayLoadList = (from lh in listaCruda
                                   join em in emps.Model on lh.id equals em.id
                                   select new HoraEntrada {
                                       fecha = lh.fecha, id = lh.id, nombre = em.nombre,
                                       entrada = lh.entrada, salida = lh.salida,
                                       bono = lh.bono, almuerzocena = lh.almuerzocena
                                   })
                                   .GroupBy(x => new { x.id, x.fecha, x.entrada })
                                   .Select(g => g.First()).ToList();
                    
                    Snackbar.Add($"Proceso exitoso: {PayLoadList.Count} registros cargados.", Severity.Info);
                }
                else 
                {
                    Snackbar.Add("El archivo no contiene marcas para la fecha seleccionada.", Severity.Warning);
                }
            }
            catch (Exception ex) 
            { 
                Snackbar.Add($"Error al procesar archivo: {ex.Message}", Severity.Error); 
            }
            finally 
            { 
                isloaddata = false; 
                StateHasChanged(); 
            }
        }

        protected void LimpiarDuplicados() 
        {
            var inicial = PayLoadList.Count;
            PayLoadList = PayLoadList.GroupBy(x => new { x.id, x.fecha, x.entrada, x.salida })
                                     .Select(g => g.First()).ToList();
            Snackbar.Add($"Limpieza completa: {inicial - PayLoadList.Count} duplicados eliminados.", Severity.Info);
        }

        protected void EliminarFila(HoraEntrada item) 
        {
            PayLoadList.Remove(item);
            Snackbar.Add("Registro eliminado de la vista previa.", Severity.Normal);
        }
        
        protected void CommittedItemChanges(object item)
        {
            // La máscara asegura el formato, solo refrescamos estados visuales
            StateHasChanged();
        }

        protected async Task SendMarca()
        {
            if (!empresa.HasValue) 
            {
                Snackbar.Add("Debe seleccionar una operación destino.", Severity.Warning);
                return;
            }

            // Validación estricta: No permitir guardar si hay estados incompletos
            var registrosError = PayLoadList.Where(x => x.Estado != EstadoMarcacion.Completo).ToList();
            if (registrosError.Any())
            {
                Snackbar.Add($"BLOQUEO: Hay {registrosError.Count} registros incompletos (rojos). Corríjalos para continuar.", 
                             Severity.Error, config => config.VisibleStateDuration = 10000);
                return;
            }

            isloaddata = true;
            try
            {
                var res = await _mediator.Send(new AddAllMarcadasCommand(PayLoadList, empresa.Value, NombreOperacion()));
                if (!res.Respuesta.ExisteError) 
                { 
                    Snackbar.Add("Marcaciones sincronizadas con Odoo correctamente.", Severity.Success); 
                    PayLoadList.Clear(); 
                    empresa = null;
                }
                else 
                {
                    Snackbar.Add($"Odoo respondió con error: {res.Respuesta.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex) 
            { 
                Snackbar.Add($"Error de red: {ex.Message}", Severity.Error); 
            }
            finally 
            { 
                isloaddata = false; 
            }
        }

        private string NombreOperacion()
        {
            if (!empresa.HasValue) return "N/A";
            var oper = PayLoadOper.FirstOrDefault(x => x.id == empresa.Value);
            return oper != null ? $"{empresa}-{oper.nombre}" : "N/A";
        }

        protected async Task DownloadFile()
        {
            if (!PayLoadList.Any()) return;
            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);
            try 
            {
                var query = PayLoadList.Select(d => new { d.id, d.nombre, d.fecha, d.entrada, d.salida, d.bono, d.almuerzocena }).ToList();
                var exceldata = DataExcel.CreateExcel(query, "Marcaciones");
                string base64Data = Convert.ToBase64String(exceldata.Data);
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, $"Marcas_{FchaCarga:ddMMyyyy}.xlsx");
                Snackbar.Add("Descarga de Excel iniciada.", Severity.Success);
            }
            catch(Exception ex) 
            { 
                Snackbar.Add("Fallo al exportar Excel: " + ex.Message, Severity.Error); 
            }
            finally 
            { 
                isloaddata = false; 
            }
        }

        protected bool QuickFilterMethod(HoraEntrada x) 
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            return (x.nombre?.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ?? false) || 
                   x.id.ToString().Contains(_searchString);
        }
    }
}
