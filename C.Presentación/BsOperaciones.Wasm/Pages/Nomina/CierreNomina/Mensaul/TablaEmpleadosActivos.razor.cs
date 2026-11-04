using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Modelo.Entidades.Entradas.Odoo;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Nomina.CierreNomina.Mensaul
{
    public partial class TablaEmpleadosActivos : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }

        [Parameter] public List<EmpleadosActivos> _PayLoadList { get; set; } = new();
        [Parameter] public EventCallback<InputFileChangeEventArgs> OnLoadFiles { get; set; }
        [Parameter] public EventCallback OnUpdateEmpleados { get; set; }
        [Parameter] public bool BotonEnabled { get; set; } = true;

        protected bool isloading { get; set; } = false;
        protected string _searchString = "";

        protected Func<EmpleadosActivos, bool> _quickFilter => x =>
        {
            if (string.IsNullOrWhiteSpace(_searchString)) return true;
            if (x.NombreCompleto != null && x.NombreCompleto.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
            if (x.NoCedula != null && x.NoCedula.Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
            if (x.NoInss != null && x.NoInss.ToString().Contains(_searchString, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        };

        protected async Task LoadFiles(InputFileChangeEventArgs e) => await OnLoadFiles.InvokeAsync(e);

        protected async Task UpdateEmpleados() => await OnUpdateEmpleados.InvokeAsync();

        protected async Task DownloadFile()
        {
            if (_PayLoadList == null || !_PayLoadList.Any()) return;

            isloading = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                var exceldata = DataExcel.CreateExcel(_PayLoadList, "Revision_Cierre_Mensual");
                string base64Data = Convert.ToBase64String(exceldata.Data);
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, "Revision_Empleados_Cierre.xlsx");
            }
            finally
            {
                isloading = false;
            }
        }
    }
}
