using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MediatR;
using MudBlazor;
using Modelo.ClasesGenericas;
using Modelo.Report;
using Modelo.Entidades.Entradas.Odoo;
using BsOperaciones.Application.Features.Odoo.Queries;
using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;

namespace BsOperaciones.Pages.Comercial
{
    public partial class ConsultarServicios : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected IOdooService OdooService { get; set; } = default!;

        private Combos? selectedTemplate;
        private List<Combos> templatesList = new();
        private List<OdooVariantDto>? variantsList;
        private Dictionary<int, OdooVariantDto> editedVariants = new();

        private bool isSearching = false;
        private bool isSaving = false;
        private bool isExporting = false;
        private string searchString = "";

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var response = await _mediator.Send(new GetAllCombosQuery("Plantillas"));
                if (response?.Model != null)
                {
                    templatesList = response.Model.ToList();
                }
                else if (response?.Respuesta != null && response.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al cargar plantillas: {response.Respuesta.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al iniciar consulta: {ex.Message}", Severity.Error);
            }
        }

        private async Task<IEnumerable<Combos>> SearchTemplates(string value, CancellationToken token)
        {
            if (string.IsNullOrEmpty(value))
                return templatesList;

            return await Task.FromResult(
                templatesList.Where(x => x.nombre != null && x.nombre.Contains(value, StringComparison.OrdinalIgnoreCase))
            );
        }

        private async Task OnTemplateChanged(Combos? template)
        {
            selectedTemplate = template;
            variantsList = null;
            editedVariants.Clear();

            if (selectedTemplate != null)
            {
                await FetchVariants();
            }
        }

        private async Task FetchVariants()
        {
            if (selectedTemplate == null) return;

            isSearching = true;
            editedVariants.Clear();
            StateHasChanged();

            try
            {
                var response = await _mediator.Send(new GetProductVariantsQuery(selectedTemplate.id));
                if (response?.Model != null)
                {
                    variantsList = response.Model.ToList();
                }
                else if (response?.Respuesta != null && response.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al obtener variantes: {response.Respuesta.MensajeError}", Severity.Error);
                    variantsList = new();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al consultar variantes: {ex.Message}", Severity.Error);
                variantsList = new();
            }
            finally
            {
                isSearching = false;
                StateHasChanged();
            }
        }

        private void OnPriceEdited(OdooVariantDto variant, decimal newPrice)
        {
            variant.precio = newPrice;
            if (editedVariants.ContainsKey(variant.id))
            {
                editedVariants[variant.id] = variant;
            }
            else
            {
                editedVariants.Add(variant.id, variant);
            }
        }

        private void DiscardChanges()
        {
            _ = FetchVariants();
        }

        private async Task SaveChanges()
        {
            if (!editedVariants.Any()) return;

            isSaving = true;
            StateHasChanged();

            try
            {
                var response = await _mediator.Send(new ActualizarPreciosVariantesCommand(editedVariants.Values.ToList()));
                if (response != null && !response.Respuesta.ExisteError)
                {
                    Snackbar.Add("Las tarifas de las variantes seleccionadas se han sincronizado con Odoo con éxito.", Severity.Success);
                    await FetchVariants();
                }
                else if (response?.Respuesta != null)
                {
                    Snackbar.Add($"Error al sincronizar tarifas: {response.Respuesta.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al guardar cambios: {ex.Message}", Severity.Error);
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private bool FilterFunc(OdooVariantDto element) => FilterFunc1(element, searchString);

        private bool FilterFunc1(OdooVariantDto element, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            search = search.Trim();

            if (!string.IsNullOrEmpty(element.default_code) && element.default_code.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrEmpty(element.nombre) && element.nombre.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            if (element.precio.ToString("N4").Contains(search, StringComparison.OrdinalIgnoreCase) || element.precio.ToString("G").Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private async Task ExportarConsulta()
        {
            if (variantsList == null || !variantsList.Any())
            {
                Snackbar.Add("No hay datos disponibles para exportar.", Severity.Warning);
                return;
            }

            var filteredData = variantsList.Where(FilterFunc).ToList();
            if (!filteredData.Any())
            {
                Snackbar.Add("No se encontraron registros que coincidan con la búsqueda activa.", Severity.Warning);
                return;
            }

            isExporting = true;
            StateHasChanged();
            await Task.Delay(50);

            try
            {
                string nombrePlantilla = selectedTemplate?.nombre ?? "Servicios";
                string nombreLimpio = string.Concat(nombrePlantilla.Split(System.IO.Path.GetInvalidFileNameChars())).Replace(" ", "_");
                string fileName = $"Consulta_Variantes_{nombreLimpio}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Crear libro Excel limpio sin logos utilizando NPOI
                var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
                var sheet = workbook.CreateSheet("Variantes");

                // Estilo para encabezados
                var headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerFont.FontHeightInPoints = 11;
                
                var headerStyle = workbook.CreateCellStyle();
                headerStyle.SetFont(headerFont);
                headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
                headerStyle.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;

                // Estilo para precios (formato numérico)
                var priceStyle = workbook.CreateCellStyle();
                var dataFormat = workbook.CreateDataFormat();
                priceStyle.DataFormat = dataFormat.GetFormat("#,##0.0000");

                // Fila 0: Encabezados exactos solicitados
                var headerRow = sheet.CreateRow(0);

                var c0 = headerRow.CreateCell(0);
                c0.SetCellValue("Nombre");
                c0.CellStyle = headerStyle;

                var c1 = headerRow.CreateCell(1);
                c1.SetCellValue("Precio de venta");
                c1.CellStyle = headerStyle;

                var c2 = headerRow.CreateCell(2);
                c2.SetCellValue("Valores de las variantes");
                c2.CellStyle = headerStyle;

                // Filas de datos
                int rowIndex = 1;
                foreach (var item in filteredData)
                {
                    var row = sheet.CreateRow(rowIndex++);
                    string prodNombre = !string.IsNullOrWhiteSpace(item.template_name) ? item.template_name : (selectedTemplate?.nombre ?? "");
                    
                    row.CreateCell(0).SetCellValue(prodNombre);
                    
                    var priceCell = row.CreateCell(1);
                    priceCell.SetCellValue((double)item.precio);
                    priceCell.CellStyle = priceStyle;

                    row.CreateCell(2).SetCellValue(item.nombre ?? "");
                }

                // Establecer ancho de columnas (evita System.Drawing.Common PlatformNotSupportedException en Blazor WASM)
                sheet.SetColumnWidth(0, 35 * 256);
                sheet.SetColumnWidth(1, 20 * 256);
                sheet.SetColumnWidth(2, 65 * 256);

                using var ms = new System.IO.MemoryStream();
                workbook.Write(ms);
                workbook.Close();
                byte[] bytes = ms.ToArray();
                string base64 = Convert.ToBase64String(bytes);

                await JS.InvokeVoidAsync("downloadFile", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", base64, fileName);
                Snackbar.Add("Consulta exportada a Excel con éxito.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al exportar consulta: {ex.Message}", Severity.Error);
            }
            finally
            {
                isExporting = false;
                StateHasChanged();
            }
        }
    }
}
