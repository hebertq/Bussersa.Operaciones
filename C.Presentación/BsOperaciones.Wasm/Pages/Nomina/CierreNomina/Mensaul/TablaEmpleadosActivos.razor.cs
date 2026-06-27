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

        protected async Task LoadFiles(InputFileChangeEventArgs e) => await OnLoadFiles.InvokeAsync(e);

        protected async Task UpdateEmpleados() => await OnUpdateEmpleados.InvokeAsync();

        // Lógica para descargar el archivo basado en la lista actual de la tabla
        protected async Task DownloadFile()
        {
            if (_PayLoadList == null || !_PayLoadList.Any()) return;

            isloading = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                // Generar el Excel usando la utilidad DataExcel
                var exceldata = DataExcel.CreateExcel(_PayLoadList, "Revision_Cierre_Mensual");

                // Convertir a Base64 para la descarga
                string base64Data = Convert.ToBase64String(exceldata.Data);

                // Invocar función de JavaScript para descargar el archivo
                await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64Data, "Revision_Empleados_Cierre.xlsx");
            }
            finally
            {
                isloading = false;
            }
        }
    }
}
