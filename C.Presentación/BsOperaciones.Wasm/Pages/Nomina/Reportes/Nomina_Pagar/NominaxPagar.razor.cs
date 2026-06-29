using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Component.DialogModal;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using Utilidades.Interfaces;
using HostService.Interfaces;

namespace BsOperaciones.Pages.Nomina.Reportes.Nomina_Pagar
{
    public partial class NominaxPagar : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IUserInfo _Iuser { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IOdooService OdooService { get; set; }

        public List<nominatype> PayLoadList { get; set; } = new();
        public List<Combos> PayLoadNomina { get; set; } = new();
        public ModaComponentBS ErrorModal { get; set; } = new();
        public int operacion { set; get; } = 0;
        public bool isloaddata { set; get; } = false;
        public bool BotonEnabled { get; set; } = true;
        protected string _searchString = "";

        protected override async Task OnInitializedAsync()
        {
            var regop = await _mediator.Send(new GetAllCombosQuery("Nominas"));
            PayLoadNomina = regop.Model;
        }

        protected async Task OnChangeNomina(int value)
        {
            operacion = value;
            if (operacion == 0) return;

            isloaddata = true;
            try
            {
                await GetAllNominaxPagar();
                BotonEnabled = !PayLoadList.Any();
                if (BotonEnabled) Snackbar.Add("No se encontraron registros para esta nómina", Severity.Info);
            }
            finally
            {
                isloaddata = false;
            }
        }

        private async Task GetAllNominaxPagar()
        {
            PayLoadList = new List<nominatype>();
            var registros = await _mediator.Send(new GetAllPagoNominaQuery(operacion));
            if (registros.Model.Count > 0) PayLoadList = registros.Model;
        }

        protected async Task ImprimirPdf()
        {
            isloaddata = true;
            try { await GeneraImpresion(); }
            finally { isloaddata = false; }
        }

        protected async Task ExcelPrint()
        {
            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);
            try { await CrearLibroExcel(); }
            finally { isloaddata = false; }
        }

        private async Task CrearLibroExcel()
        {
            var pagarnomina = GenerarAchivo();
            string nombre = PayLoadNomina.FirstOrDefault(x => x.id == operacion)?.nombre ?? "Nomina";
            
            var request = new MultiSheetExcelRequest
            {
                Hojas = new List<ExcelRequest>
                {
                    new ExcelRequest { Hoja = "Detalle Nómina", Datos = Modelo.Validaciones.Util.ToDictionaryList(pagarnomina.nominatotal), IncludeHeader = true },
                    new ExcelRequest { Hoja = "Desglose Tarjeta", Datos = Modelo.Validaciones.Util.ToDictionaryList(pagarnomina.desglocetarjeta), IncludeHeader = true },
                    new ExcelRequest { Hoja = "Desglose Efectivo", Datos = Modelo.Validaciones.Util.ToDictionaryList(pagarnomina.desgloceefectivo), IncludeHeader = true }
                }
            };

            var response = await OdooService.GenerateExcel(request);
            if (!response.Respuesta.ExisteError && response.Model != null)
            {
                string base64Data = response.Model.File;
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, $"{nombre.Replace(" ", "")}.xlsx");
                Snackbar.Add("Excel generado con éxito", Severity.Success);
            }
            else
            {
                Snackbar.Add("Error al generar Excel: " + response.Respuesta.MensajeError, Severity.Error);
            }
        }

        public repnominapago GenerarAchivo()
        {
            string nombre = PayLoadNomina.FirstOrDefault(x => x.id == operacion)?.nombre ?? "";
            var nomina = PayLoadList.Select(n => new nominaall
            {
                cedula = n.cedula,
                empleado = n.empleado,
                tipoempleado = n.tipoempleado,
                tarjeta = n.bitotal > 0 ? false : true,
                salbasico = n.salbasico,
                dias = n.dias,
                basico = n.basico,
                transporte = n.transporte,
                alimento = n.alimento,
                bonocump = n.bonocump,
                he = n.he,
                horaextra = n.horaextra,
                vacaciones = n.vacaciones,
                otrosing = n.otrosing,
                totaldev = n.totaldev,
                inss = n.inss,
                ir = n.ir,
                otdeduc = n.otdeduc,
                totalded = n.totalded,
                neto = n.neto
            }).ToList();

            var desglocetarj = PayLoadList.Where(x => x.bitotal == 0)
                .Select(n => new nominaalltarjeta { cedula = n.cedula, empleado = n.empleado, neto = n.neto }).ToList();

            var desgolceefect = PayLoadList.Where(x => x.bitotal > 0)
                .Select(n => new nominadesgefectivo
                {
                    cedula = n.cedula,
                    neto = n.neto,
                    empleado = n.empleado,
                    bi1 = n.bi1,
                    bi5 = n.bi5,
                    bi10 = n.bi10,
                    bi20 = n.bi20,
                    bi50 = n.bi50,
                    bi100 = n.bi100,
                    bi200 = n.bi200,
                    bi500 = n.bi500,
                    bi1000 = n.bi1000,
                    bitotal = n.bitotal
                }).ToList();

            return new repnominapago { nominatotal = nomina, desgloceefectivo = desgolceefect, desglocetarjeta = desglocetarj, titulo = nombre };
        }



        private async Task GeneraImpresion()
        {
            var pagarnomina = GenerarAchivo();
            string nombre = PayLoadNomina.FirstOrDefault(x => x.id == operacion)?.nombre ?? "";
            var response = await OdooService.PrintPayrollPdf(nombre, pagarnomina);

            if (response.Respuesta.ExisteError)
                await ErrorModal.setErrorInfo(response.Respuesta.MensajeError, DialogService);
            else
                await JS.InvokeVoidAsync("downloadFile", "application/pdf", response.Model.File, $"{nombre.Replace(" ", "")}.pdf");
        }

        protected Func<nominatype, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            return x.cedula.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                   x.empleado.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
        };
    }
}
