using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Modelo.Comercial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using BsOperaciones.Application.Features.Comercial.Queries;
using BsOperaciones.Application.Features.Comercial.Commands;

namespace BsOperaciones.Pages.Comercial
{
    public partial class CosteoMateriales : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private ISender Mediator { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private DialogOptions sendDialogOptions = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };

        private bool isPrinting = false;
        private bool isSendDialogOpen = false;
        private Cotizacion? sendTargetQuote;

        private string cliente = "";
        private decimal utilidadPorcentaje = 20.00m;
        private bool aplicaIva = true;
        private string observaciones = "";
        private decimal tarifaAcordada = 0.00m;
        private string searchFilter = "";

        // Formulario nuevo material
        private int? selectedMaterialId;
        private string newMaterialNombre = "";
        private decimal newMaterialCantidad = 1.00m;
        private string newMaterialUnidad = "Ud";
        private decimal newMaterialCostoUnitario = 0.00m;

        // Modal Envío
        private string sendEmailAddress = "";
        private string sendEmailSubject = "";
        private string sendEmailBody = "";
        private string sendPhoneWhatsapp = "";

        // Listas
        private List<CatalogoMaterial> materialesList = new List<CatalogoMaterial>();
        private List<CotizacionMaterialDetalle> currentMateriales = new List<CotizacionMaterialDetalle>();
        private List<Cotizacion> savedQuotes = new List<Cotizacion>();

        private CatalogoMaterial editingMaterial = new CatalogoMaterial();

        // Cálculos dinámicos
        private decimal CalculatedCostoBase => currentMateriales.Sum(x => x.Cantidad * x.CostoUnitario);
        private decimal CalculatedUtilidad => CalculatedCostoBase * (utilidadPorcentaje / 100.0m);
        private decimal CalculatedSubtotalConUtilidad => CalculatedCostoBase + CalculatedUtilidad;
        private decimal CalculatedIva => aplicaIva ? (CalculatedSubtotalConUtilidad * 0.15m) : 0.00m;
        private decimal CalculatedTarifaSugerida => CalculatedSubtotalConUtilidad + CalculatedIva;

        private IEnumerable<Cotizacion> FilteredQuotes => savedQuotes
            .Where(q => q.TipoCosteo == "MATERIALES" || q.MaterialDetalles != null && q.MaterialDetalles.Any())
            .Where(q => string.IsNullOrWhiteSpace(searchFilter) || q.ClienteNombre.Contains(searchFilter, StringComparison.OrdinalIgnoreCase));

        protected override async Task OnInitializedAsync()
        {
            await LoadCatalogs();
            await LoadQuotes();
        }

        private async Task LoadCatalogs()
        {
            try
            {
                var res = await Mediator.Send(new GetCatalogosComercialQuery());
                if (!res.Respuesta.ExisteError && res.Model != null && res.Model.Any())
                {
                    var cat = res.Model.First();
                    materialesList = cat.Materiales ?? new List<CatalogoMaterial>();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al cargar catálogo de materiales: " + ex.Message, Severity.Error);
            }
        }

        private async Task LoadQuotes()
        {
            try
            {
                var res = await Mediator.Send(new GetCotizacionesQuery());
                if (!res.Respuesta.ExisteError && res.Model != null)
                {
                    savedQuotes = res.Model.Where(q => q.TipoCosteo == "MATERIALES" || (q.MaterialDetalles != null && q.MaterialDetalles.Any())).ToList();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al cargar cotizaciones: " + ex.Message, Severity.Error);
            }
        }

        private void OnMaterialSelected(int? id)
        {
            selectedMaterialId = id;
            if (id.HasValue)
            {
                var mat = materialesList.FirstOrDefault(x => x.Id == id.Value);
                if (mat != null)
                {
                    newMaterialNombre = mat.Nombre;
                    newMaterialCostoUnitario = mat.CostoUnitario;
                }
            }
        }

        private void AddMaterialToQuote()
        {
            if (string.IsNullOrWhiteSpace(newMaterialNombre))
            {
                Snackbar.Add("El nombre del material es obligatorio.", Severity.Warning);
                return;
            }

            if (newMaterialCantidad <= 0)
            {
                Snackbar.Add("La cantidad debe ser mayor a 0.", Severity.Warning);
                return;
            }

            var item = new CotizacionMaterialDetalle
            {
                MaterialId = selectedMaterialId,
                Nombre = newMaterialNombre,
                Cantidad = newMaterialCantidad,
                CostoUnitario = newMaterialCostoUnitario,
                SkuNombre = string.IsNullOrWhiteSpace(newMaterialUnidad) ? "Ud" : newMaterialUnidad
            };

            currentMateriales.Add(item);
            
            // Actualizar tarifa acordada automáticamente con la recomendada si no se ha escrito manualmente
            if (tarifaAcordada == 0m)
            {
                tarifaAcordada = CalculatedTarifaSugerida;
            }

            // Limpiar campos
            selectedMaterialId = null;
            newMaterialNombre = "";
            newMaterialCantidad = 1.00m;
            newMaterialUnidad = "Ud";
            newMaterialCostoUnitario = 0.00m;

            Snackbar.Add("Material agregado a la cotización.", Severity.Success);
        }

        private void RemoveMaterialFromQuote(CotizacionMaterialDetalle item)
        {
            currentMateriales.Remove(item);
            if (!currentMateriales.Any())
            {
                tarifaAcordada = 0m;
            }
        }

        private async Task SaveQuote()
        {
            if (string.IsNullOrWhiteSpace(cliente))
            {
                Snackbar.Add("Debe ingresar el Nombre del Cliente / Proyecto.", Severity.Warning);
                return;
            }

            if (!currentMateriales.Any())
            {
                Snackbar.Add("Debe agregar al menos un material a la cotización.", Severity.Warning);
                return;
            }

            decimal finalTarifaAcordada = tarifaAcordada > 0m ? tarifaAcordada : CalculatedTarifaSugerida;

            var quote = new Cotizacion
            {
                Id = Guid.NewGuid(),
                NumeroCotizacion = $"COT-MAT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Split('-')[0].ToUpper()}",
                ClienteNombre = cliente,
                FechaCreacion = DateTime.Now,
                TipoCosteo = "MATERIALES",
                UtilidadPorcentaje = utilidadPorcentaje,
                CostoTotal = CalculatedCostoBase,
                TarifaSugerida = CalculatedTarifaSugerida,
                TarifaAcordada = finalTarifaAcordada,
                MaterialDetalles = currentMateriales.ToList(),
                Estado = "Borrador"
            };

            var res = await Mediator.Send(new SaveCotizacionCommand(quote));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add($"Cotización de materiales {quote.NumeroCotizacion} guardada exitosamente.", Severity.Success);
                cliente = "";
                observaciones = "";
                currentMateriales.Clear();
                tarifaAcordada = 0m;
                await LoadQuotes();
            }
            else
            {
                Snackbar.Add("Error al guardar cotización: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task DeleteQuote(Guid id)
        {
            var res = await Mediator.Send(new DeleteCotizacionCommand(id));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Cotización eliminada.", Severity.Info);
                await LoadQuotes();
            }
            else
            {
                Snackbar.Add("Error al eliminar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task PrintPdf(Cotizacion quote)
        {
            isPrinting = true;
            StateHasChanged();
            try
            {
                var ids = new List<Guid> { quote.Id };
                var res = await Mediator.Send(new PrintCotizacionPdfQuery(ids));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Cotizacion_Materiales_{quote.ClienteNombre.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("PDF de Cotización de Materiales generado y descargado.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar PDF: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error al procesar PDF: " + ex.Message, Severity.Error);
            }
            finally
            {
                isPrinting = false;
                StateHasChanged();
            }
        }

        // ENVIAR COTIZACIÓN
        private void OpenSendDialog()
        {
            sendTargetQuote = null;
            sendEmailSubject = $"Cotización de Materiales e Insumos - {cliente} - BUSSERSA";
            sendEmailBody = BuildEmailSummaryText(cliente, currentMateriales, CalculatedTarifaSugerida, tarifaAcordada > 0 ? tarifaAcordada : CalculatedTarifaSugerida);
            isSendDialogOpen = true;
        }

        private void OpenSendDialogForSavedQuote(Cotizacion quote)
        {
            sendTargetQuote = quote;
            sendEmailSubject = $"Cotización de Materiales N° {quote.NumeroCotizacion} - {quote.ClienteNombre}";
            var items = quote.MaterialDetalles ?? new List<CotizacionMaterialDetalle>();
            sendEmailBody = BuildEmailSummaryText(quote.ClienteNombre, items, quote.TarifaSugerida, quote.TarifaAcordada);
            isSendDialogOpen = true;
        }

        private void CloseSendDialog()
        {
            isSendDialogOpen = false;
        }

        private string BuildEmailSummaryText(string clientName, List<CotizacionMaterialDetalle> items, decimal sugerida, decimal acordada)
        {
            var summary = $"Estimado cliente {clientName},\n\n" +
                          $"Adjunto enviamos el detalle de la cotización de materiales e insumos solicitada:\n\n";

            foreach (var item in items)
            {
                summary += $"• {item.Nombre} - Cant: {item.Cantidad:N2} {item.SkuNombre} @ C$ {item.CostoUnitario:N2} = C$ {(item.Cantidad * item.CostoUnitario):N2}\n";
            }

            summary += $"\n----------------------------------------\n" +
                       $"Precio Sugerido Total: C$ {sugerida:N2}\n" +
                       $"Precio Acordado Total: C$ {acordada:N2}\n" +
                       $"----------------------------------------\n\n" +
                       $"Quedamos a su disposición para cualquier duda o consulta.\n\n" +
                       $"Atentamente,\n" +
                       $"Departamento Comercial - BUSSERSA";

            return summary;
        }

        private async Task SendEmailAction()
        {
            if (string.IsNullOrWhiteSpace(sendEmailAddress))
            {
                Snackbar.Add("Ingrese una dirección de correo electrónico válida.", Severity.Warning);
                return;
            }

            await Task.Delay(300);
            Snackbar.Add($"Cotización enviada exitosamente a {sendEmailAddress}.", Severity.Success);
            isSendDialogOpen = false;
        }

        private async Task SendWhatsappAction()
        {
            string message = Uri.EscapeDataString(sendEmailBody);
            string url = string.IsNullOrWhiteSpace(sendPhoneWhatsapp) 
                ? $"https://api.whatsapp.com/send?text={message}" 
                : $"https://api.whatsapp.com/send?phone={sendPhoneWhatsapp.Replace("+", "").Replace(" ", "")}&text={message}";

            await JS.InvokeVoidAsync("open", url, "_blank");
            Snackbar.Add("Enlace a WhatsApp generado exitosamente.", Severity.Info);
            isSendDialogOpen = false;
        }

        // GESTIÓN DEL CATÁLOGO
        private void EditCatalogMaterial(CatalogoMaterial item)
        {
            editingMaterial = new CatalogoMaterial { Id = item.Id, Nombre = item.Nombre, CostoUnitario = item.CostoUnitario };
        }

        private void CancelEditCatalogMaterial()
        {
            editingMaterial = new CatalogoMaterial();
        }

        private async Task SaveCatalogMaterial()
        {
            if (string.IsNullOrWhiteSpace(editingMaterial.Nombre))
            {
                Snackbar.Add("El nombre del material es obligatorio.", Severity.Warning);
                return;
            }

            var res = await Mediator.Send(new SaveCatalogoMaterialCommand(editingMaterial));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Material guardado en el catálogo exitosamente.", Severity.Success);
                editingMaterial = new CatalogoMaterial();
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al guardar catálogo: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task DeleteCatalogMaterial(int id)
        {
            var res = await Mediator.Send(new DeleteCatalogoMaterialCommand(id));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Material eliminado del catálogo.", Severity.Info);
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al eliminar del catálogo: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }
    }
}
