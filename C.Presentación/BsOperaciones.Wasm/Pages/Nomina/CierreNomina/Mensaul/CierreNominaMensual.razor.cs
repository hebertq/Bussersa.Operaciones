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
using Utilidades.ClasesGenericas;
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
        protected int _activeIndex = 0; // Pestaña activa inicial: 1. Validar Activos
        protected bool isloaddata = false;
        protected int _anioGlobal = DateTime.Now.Year;
        protected int _mesGlobal = DateTime.Now.Month == 1 ? 12 : DateTime.Now.Month - 1;
        private List<EmpleadosActivos?> _PayLoadListLocal = new();
        protected List<EmpleadosActivos?> _PayLoadListInss = new();
        protected List<NominaMensualReportar> _nominaFinalGlobal = new();
        protected List<NominaMensualReportar> _nominaMesAnterior = new();

        public List<EmpleadosActivos> ListaAseguradosParaAuditoria
        {
            get
            {
                if (_PayLoadListInss != null && _PayLoadListInss.Any(x => x != null && x.ActivoInss == true))
                {
                    return _PayLoadListInss.Where(x => x != null).Cast<EmpleadosActivos>().ToList();
                }

                // Fallback automático a los asegurados activos de la base de datos local
                return _PayLoadListLocal.Where(x => x != null && x.ActivoInss == true).Cast<EmpleadosActivos>().ToList();
            }
        }

        public List<EmpleadosActivos> ListaAseguradosParaInss
        {
            get
            {
                if (_PayLoadListInss != null && _PayLoadListInss.Any(x => x != null))
                {
                    return _PayLoadListInss.Where(x => x != null).Cast<EmpleadosActivos>().ToList();
                }

                return _PayLoadListLocal.Where(x => x != null).Cast<EmpleadosActivos>().ToList();
            }
        }

        protected int ConteoDiscrepancias
        {
            get
            {
                var listaActivos = ListaAseguradosParaAuditoria;
                if (listaActivos == null || !listaActivos.Any() || _nominaFinalGlobal == null || !_nominaFinalGlobal.Any())
                    return 0;

                int sinNom = listaActivos.Count(i => i != null && i.ActivoInss == true && !_nominaFinalGlobal.Any(n => 
                    (i.IdNomina > 0 && n.emp_nomina == i.IdNomina) ||
                    (i.NoInss > 0 && InssReporteHelper.CleanInss(n.emp_noinss) == InssReporteHelper.CleanInss(i.NoInss.ToString()))));

                int sinCob = _nominaFinalGlobal.Count(n => (n.emp_deduc_inss == 0 || n.emp_deduc_inss == null) &&
                                                           listaActivos.Any(i => i != null && i.ActivoInss == true &&
                                                            ((i.IdNomina > 0 && i.IdNomina == n.emp_nomina) ||
                                                             (i.NoInss > 0 && InssReporteHelper.CleanInss(n.emp_noinss) == InssReporteHelper.CleanInss(i.NoInss.ToString())))));
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
            await CargarNominaMesAnterior();
            _PayLoadListInss = new();
        }

        protected async Task CargarNominaMesAnterior()
        {
            int anioAnt = _mesGlobal == 1 ? _anioGlobal - 1 : _anioGlobal;
            int mesAnt = _mesGlobal == 1 ? 12 : _mesGlobal - 1;
            try
            {
                var resp = await _mediator.Send(new GetNominaMensualInssQuery(anioAnt, mesAnt));
                if (!resp.Respuesta.ExisteError && resp.Model != null)
                {
                    _nominaMesAnterior = resp.Model;
                }
                else
                {
                    _nominaMesAnterior = new();
                }
            }
            catch
            {
                _nominaMesAnterior = new();
            }
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

        protected async void HandleNominaGenerated(List<NominaMensualReportar> nomina)
        {
            _nominaFinalGlobal = nomina;
            await CargarNominaMesAnterior();
            Snackbar.Add($"Nómina consolidada generada con {nomina.Count} colaboradores. Pestañas de Auditoría y Archivo INSS actualizadas.", Severity.Success);
            StateHasChanged();
        }

        private async Task LoadEmpleados()
        {
            var respemp = await _mediator.Send(new GetAllEmpleadosQuery());
            if (!respemp.Respuesta.ExisteError)
            {
                _PayLoadListLocal = respemp.Model;
                if (_PayLoadListLocal != null)
                {
                    foreach (var emp in _PayLoadListLocal)
                    {
                        if (emp != null && string.IsNullOrWhiteSpace(emp.PrimerNombre) && !string.IsNullOrWhiteSpace(emp.NombreCompleto))
                        {
                            var (pn, pa) = InssReporteHelper.SplitNombreCompleto(emp.NombreCompleto);
                            emp.PrimerNombre = pn;
                            emp.PrimerApellido = pa;
                        }
                    }
                }
            }
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
