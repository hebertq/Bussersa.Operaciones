using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Component.DialogModal;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;
using Utilidades.Interfaces;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.Reportes.Nomina_Cierre
{
    public partial class NominaxCierre : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IUserInfo _Iuser { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        public List<PayrollMonthRecord> PayLoadList { get; set; } = new();
        public ModaComponentBS ErrorModal { get; set; } = new();
        public bool isloaddata { set; get; } = false;
        public bool BotonEnabled { get; set; } = true;
        protected string _searchString = "";
        protected int anio = DateTime.Now.Year, mes = DateTime.Now.Month;
        protected List<int> YearsList = new();

        protected override void OnInitialized()
        {
            YearsList = new List<int> { DateTime.Now.Year, DateTime.Now.Year - 1 };
        }

        protected async Task OnChangeNomina()
        {
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
            PayLoadList = new List<PayrollMonthRecord>();
            var registros = await _mediator.Send(new GetAllNominaMensualCierreQuery(AnioMes()));
            if (registros.Model.Count > 0) PayLoadList = registros.Model;
        }

        private int AnioMes()
        {      
            return int.Parse($"{anio}{mes:00}");
        }

        protected async Task DownloadFile()
        {
            if (!PayLoadList.Any()) return;
            isloaddata = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                string nombre = $"Nomina-{AnioMes()}";
                var exceldata = DataExcel.CreateExcel(PayLoadList, "Nomina");
                string base64Data = Convert.ToBase64String(exceldata.Data);
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, $"{nombre}.xlsx");
                Snackbar.Add("Descarga de Excel iniciada.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add("Fallo al exportar Excel: " + ex.Message, Severity.Error);
            }
            finally
            {
                isloaddata = false;
            }
        }

        protected Func<PayrollMonthRecord, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            return x.nombre.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                   x.id.ToString().Contains(_searchString);
        };
    }
}
