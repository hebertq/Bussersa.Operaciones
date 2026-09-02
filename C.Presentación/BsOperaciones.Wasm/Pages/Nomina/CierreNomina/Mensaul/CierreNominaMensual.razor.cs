using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Commands;
using BsOperaciones.Application.Features.Odoo.Command;
using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Report;
using Utilidades.Interfaces;

namespace BsOperaciones.Pages.Nomina.CierreNomina.Mensaul
{
    public partial class CierreNominaMensual : ComponentBase
    {
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        private long maxFileSize = 1024 * 1024 * 15;
        public bool BotonEnabledActivosInss { get; set; } = true;
        protected int _activeIndex = 0;
        protected bool isloaddata = false;
        protected int _anioGlobal = DateTime.Now.AddMonths(-1).Year;
        protected int _mesGlobal = DateTime.Now.AddMonths(-1).Month;
        private List<EmpleadosActivos?> _PayLoadListLocal = new();
        protected List<EmpleadosActivos?> _PayLoadListInss = new();
        protected List<NominaMensualReportar> _nominaFinalGlobal = new();

        protected override async Task OnInitializedAsync()
        {
             await LoadEmpleados();
            _PayLoadListInss = new(); // Aseguramos que empiece vacío visualmente
        }

        protected async Task LoadFiles(InputFileChangeEventArgs e)
        {
            isloaddata = true;
            try
            {
                string sFileExtension = Path.GetExtension(e.File.Name).ToLower();
                DataTable dataActivos = new DataTable();

                using (MemoryStream fs = new MemoryStream())
                {
                    await e.File.OpenReadStream(maxFileSize).CopyToAsync(fs);
                    fs.Position = 0;
                    dataActivos = _Util.Excel_To_DataTable(fs, "ActivosInss", sFileExtension, 0);
                }

                var result = ExtCierreMensual.GetEmpActivosInss(_PayLoadListLocal, dataActivos);
                _PayLoadListInss = result.Cast<EmpleadosActivos?>().ToList();

                BotonEnabledActivosInss = false;
                Snackbar.Add("Archivo procesado. Verifique los registros con ID 0.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al procesar: " + ex.Message, Severity.Error);
            }
            finally
            {
                isloaddata = false;
            }
        }

        protected void HandleNominaGenerated(List<NominaMensualReportar> nomina)
        {
            _nominaFinalGlobal = nomina;
            _activeIndex = 2; // SALTO AUTOMÁTICO AL PASO 3
            StateHasChanged(); // Permite que el paso 3 reciba los datos sin saltar automáticamente
        }

        private async Task LoadEmpleados()
        {
            var respemp = await _mediator.Send(new GetAllEmpleadosQuery());
            if (!respemp.Respuesta.ExisteError)
                _PayLoadListLocal = respemp.Model;
        }

        protected async Task UpdateEmpleados()
        {
            // Filtramos vinculados y activos reales
            var vinculados = _PayLoadListInss.Where(x => x != null && x.IdNomina > 0 && x.ActivoInss == true).Cast<EmpleadosActivos>().ToList();

            if (!vinculados.Any())
            {
                Snackbar.Add("No hay empleados aptos para actualización masiva.", Severity.Warning);
                return;
            }

            bool? result = await DialogService.ShowMessageBox(
                "Sincronización con Odoo",
                $"Se procederá a actualizar {vinculados.Count} registros. Los empleados no vinculados (ID 0) se mantendrán en la lista para su revisión.",
                yesText: "Aceptar", cancelText: "Cancelar");

            if (result == true)
            {
                isloaddata = true;
                try
                {
                    var command = new ActualizarEstadosEmpleadosCommand(vinculados);
                    var response = await _mediator.Send(command);

                    if (response.Respuesta.ExisteError)
                    {
                        Snackbar.Add(response.Respuesta.MensajeError, Severity.Error);
                    }
                    else
                    {
                        Snackbar.Add("Sincronización masiva exitosa.", Severity.Success);

                        // Mantenemos _PayLoadList intacto para el Paso 2
                        BotonEnabledActivosInss = true;

                        // Avanzamos al Paso 2 automáticamente
                        _activeIndex = 1; // AVANCE AL PASO 2 TRAS SINCRONIZAR
                        StateHasChanged();
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add("Error de conexión: " + ex.Message, Severity.Error);
                }
                finally
                {
                    isloaddata = false;
                }
            }
        }
    }
}
