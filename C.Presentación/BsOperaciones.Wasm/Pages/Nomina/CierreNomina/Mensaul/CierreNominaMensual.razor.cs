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
        protected int _activeIndex = 1; // Abrir por defecto en Conciliación & Cierre si ya hay nómina o en 0
        protected bool isloaddata = false;
        protected int _anioGlobal = DateTime.Now.Year;
        protected int _mesGlobal = DateTime.Now.Month == 1 ? 12 : DateTime.Now.Month - 1;
        private List<EmpleadosActivos?> _PayLoadListLocal = new();
        protected List<EmpleadosActivos?> _PayLoadListInss = new();
        protected List<NominaMensualReportar> _nominaFinalGlobal = new();

        protected int ConteoDiscrepancias
        {
            get
            {
                if (_PayLoadListInss == null || !_PayLoadListInss.Any() || _nominaFinalGlobal == null || !_nominaFinalGlobal.Any())
                    return 0;

                int sinNom = _PayLoadListInss.Count(i => i != null && i.ActivoInss == true && !_nominaFinalGlobal.Any(n => n.emp_nomina == i.IdNomina));
                int sinCob = _nominaFinalGlobal.Count(n => (n.emp_deduc_inss == 0 || n.emp_deduc_inss == null) &&
                                                           _PayLoadListInss.Any(i => i != null && i.IdNomina == n.emp_nomina && i.ActivoInss == true));
                return sinNom + sinCob;
            }
        }

        protected void SetActiveTab(int index)
        {
            _activeIndex = index;
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadEmpleados();
            _PayLoadListInss = new();
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
                Snackbar.Add("Archivo procesado con éxito. Revise la comparación antes de sincronizar.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al procesar archivo: " + ex.Message, Severity.Error);
            }
            finally
            {
                isloaddata = false;
            }
        }

        protected void HandleNominaGenerated(List<NominaMensualReportar> nomina)
        {
            _nominaFinalGlobal = nomina;
            Snackbar.Add($"Nómina consolidada generada con {nomina.Count} colaboradores. Pestañas de Auditoría y Archivo INSS actualizadas.", Severity.Success);
            StateHasChanged();
        }

        private async Task LoadEmpleados()
        {
            var respemp = await _mediator.Send(new GetAllEmpleadosQuery());
            if (!respemp.Respuesta.ExisteError)
                _PayLoadListLocal = respemp.Model;
        }

        protected async Task UpdateEmpleados()
        {
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
                        BotonEnabledActivosInss = true;
                        _activeIndex = 1; // Avanza a la pestaña de Nómina y Cierre
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
