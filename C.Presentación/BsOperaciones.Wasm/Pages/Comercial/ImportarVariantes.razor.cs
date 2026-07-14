using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MediatR;
using MudBlazor;
using Modelo.Entidades.Entradas.Odoo;
using NPOI.SS.UserModel;

namespace BsOperaciones.Pages.Comercial
{
    public partial class ImportarVariantes : ComponentBase
    {
        [Inject] protected IMediator _mediator { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;

        public List<ImportarVarianteItem> PayLoadList { get; set; } = new();
        public bool isloaddata { get; set; } = false;
        protected string _searchString = "";
        private long maxFileSize = 1024 * 1024 * 10; // 10 MB

        protected async Task LoadFiles(InputFileChangeEventArgs e)
        {
            isloaddata = true;
            try
            {
                using var ms = new MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(ms);
                ms.Position = 0;

                var list = new List<ImportarVarianteItem>();
                IWorkbook workbook = WorkbookFactory.Create(ms);
                try
                {
                    ISheet sheet = workbook.GetSheetAt(0); // Primera hoja
                    IRow headerRow = sheet.GetRow(0);
                    if (headerRow == null)
                    {
                        Snackbar.Add("El archivo Excel está vacío o no tiene encabezado.", Severity.Error);
                        return;
                    }

                    int colName = -1;
                    int colCode = -1;
                    int colPrice = -1;
                    int colAttrs = -1;

                    for (int col = 0; col < headerRow.LastCellNum; col++)
                    {
                        var cellVal = headerRow.GetCell(col)?.ToString()?.Trim()?.ToLower();
                        if (string.IsNullOrEmpty(cellVal)) continue;

                        if (cellVal == "name" || cellVal == "plantilla" || cellVal == "producto" || cellVal == "nombre" || cellVal == "servicio" || cellVal == "servicios")
                            colName = col;
                        else if (cellVal == "default_code" || cellVal == "codigo" || cellVal == "código" || cellVal == "referencia" || cellVal == "referencia interna" || cellVal == "id interno" || cellVal == "id_interno" || cellVal == "id" || cellVal == "código interno" || cellVal == "codigo interno" || cellVal == "id-interno")
                            colCode = col;
                        else if (cellVal == "lst_price" || cellVal == "precio" || cellVal == "precio de venta" || cellVal == "precio_venta" || cellVal == "precio venta" || cellVal == "tarifa" || cellVal == "costo" || cellVal == "monto")
                            colPrice = col;
                        else if (cellVal == "product_template_variant_value_ids" || cellVal == "atributos" || cellVal == "atributos de variante" || cellVal == "valores" || cellVal == "detalles" || cellVal == "variante" || cellVal == "variantes")
                            colAttrs = col;
                    }

                    // Fallback a mapeo por posición si no se encuentran por nombre:
                    if (colName == -1) colName = 0;
                    if (colCode == -1) colCode = 1;
                    if (colPrice == -1) colPrice = 2;
                    if (colAttrs == -1) colAttrs = 3;

                    for (int i = 1; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue;

                        string nameVal = row.GetCell(colName)?.ToString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(nameVal)) continue;

                        string codeVal = row.GetCell(colCode)?.ToString()?.Trim() ?? string.Empty;
                        
                        decimal priceVal = 0;
                        var priceCellStr = row.GetCell(colPrice)?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(priceCellStr))
                        {
                            decimal.TryParse(priceCellStr, out priceVal);
                        }

                        string attrsVal = row.GetCell(colAttrs)?.ToString()?.Trim() ?? string.Empty;

                        list.Add(new ImportarVarianteItem
                        {
                            name = nameVal,
                            default_code = codeVal,
                            lst_price = priceVal,
                            product_template_variant_value_ids = attrsVal
                        });
                    }
                }
                finally
                {
                    workbook.Close();
                }

                if (list.Any())
                {
                    PayLoadList = list;
                    Snackbar.Add($"Proceso exitoso: {PayLoadList.Count} variantes cargadas para revisión.", Severity.Info);
                }
                else
                {
                    Snackbar.Add("El archivo no contiene filas de variantes válidas.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al procesar archivo Excel: {ex.Message}", Severity.Error);
            }
            finally
            {
                isloaddata = false;
                StateHasChanged();
            }
        }

        protected async Task ImportarVariantesOdoo()
        {
            if (!PayLoadList.Any()) return;

            isloaddata = true;
            try
            {
                var res = await _mediator.Send(new BsOperaciones.Application.Features.Odoo.Commands.ImportarVariantesCommand(PayLoadList));
                if (!res.Respuesta.ExisteError)
                {
                    Snackbar.Add("Variantes importadas y sincronizadas con Odoo correctamente.", Severity.Success);
                    PayLoadList.Clear();
                }
                else
                {
                    Snackbar.Add($"Odoo respondió con error: {res.Respuesta.MensajeError}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al conectar con el servidor: {ex.Message}", Severity.Error);
            }
            finally
            {
                isloaddata = false;
            }
        }
    }
}
