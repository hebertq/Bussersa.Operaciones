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
    public partial class CosteoProduccion : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private ISender Mediator { get; set; } = default!;

        private string cliente = "";
        private string skuNombre = "";
        private int produccionDiaria = 1000;
        private int cantidadTrabajadores = 1;
        private decimal manoObraUnitaria = 1.5000m;
        private decimal mermaPorcentaje = 2.00m;
        private decimal tarifaAcordada = 0.0000m;

        private Guid? selectedPersonnelQuoteId;
        private Cotizacion selectedPersonnelQuote;
        private string selectedFilterEmpresa = "";
        private List<string> importedProducts = new List<string>();
        private int currentProductIndex = -1;

        private List<CatalogoEpp> eppList = new List<CatalogoEpp>();
        private List<CatalogoViatico> viaticosList = new List<CatalogoViatico>();
        private List<CatalogoMaquinaria> machineryList = new List<CatalogoMaquinaria>();
        private List<CatalogoMaterial> materialesList = new List<CatalogoMaterial>();
        
        private List<Cotizacion> savedQuotes = new List<Cotizacion>();
        private List<Cotizacion> personnelQuotes = new List<Cotizacion>();

        private HashSet<int> selectedMachinery = new HashSet<int>();
        private HashSet<int> selectedMateriales = new HashSet<int>();
        private Dictionary<int, decimal> customMaterialesCosts = new Dictionary<int, decimal>();

        private int? selectedMaterialToAdd;
        private int? selectedMachineryToAdd;

        // Modelos para gestión de catálogos
        private CatalogoMaterial editingMaterial = new CatalogoMaterial();
        private CatalogoMaquinaria editingMachinery = new CatalogoMaquinaria();
        private CatalogoEpp editingEpp = new CatalogoEpp();
        private CatalogoViatico editingViatico = new CatalogoViatico();

        // Cálculos dinámicos
        private decimal CalculatedCostoDiarioPersona => selectedPersonnelQuote != null ? selectedPersonnelQuote.CostoTotal / 30m : 0m;

        private decimal CalculatedManoObraUnitaria => selectedPersonnelQuoteId.HasValue 
            ? ((produccionDiaria > 0) ? (CalculatedCostoDiarioPersona / (decimal)produccionDiaria) : 0m)
            : manoObraUnitaria;

        private decimal CalculatedMateriales => materialesList
            .Where(x => selectedMateriales.Contains(x.Id))
            .Sum(x => customMaterialesCosts.TryGetValue(x.Id, out var cost) ? cost : x.CostoUnitario);

        private decimal CalculatedAmortizacionMensual => machineryList
            .Where(x => selectedMachinery.Contains(x.Id))
            .Sum(x => x.ProyeccionMensual);

        private decimal CalculatedAmortizacionUnitaria => (produccionDiaria > 0 && cantidadTrabajadores > 0)
            ? ((CalculatedAmortizacionMensual / 30m) / (decimal)cantidadTrabajadores) / (decimal)produccionDiaria
            : 0m;

        private decimal CalculatedCostoUnitarioBase => CalculatedManoObraUnitaria + CalculatedMateriales + CalculatedAmortizacionUnitaria;

        private decimal CalculatedMermaCosto => CalculatedCostoUnitarioBase * (mermaPorcentaje / 100m);

        private decimal CalculatedTarifaUnitaria => CalculatedCostoUnitarioBase + CalculatedMermaCosto;

        private decimal CalculatedFacturacionDiaria => tarifaAcordada * (decimal)produccionDiaria;

        private decimal CalculatedFacturacionMensual => CalculatedFacturacionDiaria * 30m;

        // Punto de equilibrio
        private decimal CalculatedUtilidadUnitariaAntesDeLabor => tarifaAcordada - CalculatedMateriales - CalculatedAmortizacionUnitaria - CalculatedMermaCosto;

        private decimal CalculatedUnidadesBreakeven => (CalculatedUtilidadUnitariaAntesDeLabor > 0 && CalculatedCostoDiarioPersona > 0)
            ? CalculatedCostoDiarioPersona / CalculatedUtilidadUnitariaAntesDeLabor
            : 0m;

        protected override async Task OnInitializedAsync()
        {
            await LoadCatalogs();
            await LoadQuotes();
        }

        private async Task LoadCatalogs()
        {
            var res = await Mediator.Send(new GetCatalogosComercialQuery());
            if (res.Model != null && res.Model.Any())
            {
                var cats = res.Model.First();
                eppList = cats.Epp ?? new List<CatalogoEpp>();
                viaticosList = cats.Viaticos ?? new List<CatalogoViatico>();
                machineryList = cats.Maquinaria ?? new List<CatalogoMaquinaria>();
                materialesList = cats.Materiales ?? new List<CatalogoMaterial>();

                // Seleccionar por defecto algunos materiales si están disponibles
                if (!selectedMateriales.Any())
                {
                    foreach (var mat in materialesList.Where(x => x.Nombre.Contains("Manga") || x.Nombre.Contains("Cinchos") || x.Nombre.Contains("Stickers") || x.Nombre.Contains("Estibado")))
                    {
                        selectedMateriales.Add(mat.Id);
                    }
                }

                // Seleccionar por defecto maquinaria
                if (!selectedMachinery.Any())
                {
                    foreach (var item in machineryList)
                    {
                        selectedMachinery.Add(item.Id);
                    }
                }
            }
        }

        private async Task LoadQuotes()
        {
            var res = await Mediator.Send(new GetCotizacionesQuery());
            if (res.Model != null)
            {
                savedQuotes = res.Model.Where(x => x.TipoCosteo == "Produccion").ToList();
                personnelQuotes = res.Model.Where(x => x.TipoCosteo == "Personal").ToList();
            }
            RecalculateTarifaAcordada();
        }

        private void RecalculateTarifaAcordada()
        {
            if (tarifaAcordada == 0m)
            {
                tarifaAcordada = CalculatedTarifaUnitaria;
            }
        }

        private void OnEmpresaFilterChanged(string val)
        {
            selectedFilterEmpresa = val;
            selectedPersonnelQuoteId = null;
            selectedPersonnelQuote = null;
            manoObraUnitaria = 0m;
            tarifaAcordada = CalculatedTarifaUnitaria;
        }

        private void OnPersonnelQuoteChanged(Guid? id)
        {
            selectedPersonnelQuoteId = id;
            if (id.HasValue)
            {
                selectedPersonnelQuote = personnelQuotes.FirstOrDefault(x => x.Id == id.Value);
            }
            else
            {
                selectedPersonnelQuote = null;
            }
            tarifaAcordada = CalculatedTarifaUnitaria;
        }

        private void OnProduccionDiariaChanged(int val)
        {
            produccionDiaria = val;
            tarifaAcordada = CalculatedTarifaUnitaria;
        }

        private void OnCantidadTrabajadoresChanged(int val)
        {
            cantidadTrabajadores = val;
            tarifaAcordada = CalculatedTarifaUnitaria;
        }

        private decimal GetMaterialCost(int id)
        {
            if (customMaterialesCosts.TryGetValue(id, out var cost))
                return cost;
            var item = materialesList.FirstOrDefault(x => x.Id == id);
            return item?.CostoUnitario ?? 0m;
        }

        private void SetMaterialCost(int id, decimal cost)
        {
            customMaterialesCosts[id] = cost;
            tarifaAcordada = CalculatedTarifaUnitaria;
        }

        private void AddMaterialRow(int? id)
        {
            if (id.HasValue)
            {
                selectedMateriales.Add(id.Value);
                if (!customMaterialesCosts.ContainsKey(id.Value))
                {
                    var item = materialesList.FirstOrDefault(x => x.Id == id.Value);
                    if (item != null)
                    {
                        customMaterialesCosts[id.Value] = item.CostoUnitario;
                    }
                }
                tarifaAcordada = CalculatedTarifaUnitaria;
            }
            selectedMaterialToAdd = null;
        }

        private void RemoveMaterialRow(int id)
        {
            selectedMateriales.Remove(id);
            tarifaAcordada = CalculatedTarifaUnitaria;
        }

        private void AddMachineryRow(int? id)
        {
            if (id.HasValue)
            {
                selectedMachinery.Add(id.Value);
                tarifaAcordada = CalculatedTarifaUnitaria;
            }
            selectedMachineryToAdd = null;
        }

        private void RemoveMachineryRow(int id)
        {
            selectedMachinery.Remove(id);
            tarifaAcordada = CalculatedTarifaUnitaria;
        }

        private void LoadQuoteToForm(Cotizacion quote)
        {
            cliente = quote.ClienteNombre;
            selectedPersonnelQuoteId = null;
            selectedPersonnelQuote = null;

            if (quote.ProduccionDetalle != null)
            {
                skuNombre = quote.ProduccionDetalle.SkuNombre;
                produccionDiaria = quote.ProduccionDetalle.ProduccionDiaria;
                cantidadTrabajadores = quote.ProduccionDetalle.CantidadTrabajadores;
                manoObraUnitaria = quote.ProduccionDetalle.ManoObraUnitaria;
                mermaPorcentaje = quote.ProduccionDetalle.MermaPorcentaje;
                tarifaAcordada = quote.TarifaAcordada;

                if (quote.ProduccionDetalle.PersonalCotizacionId.HasValue)
                {
                    selectedPersonnelQuoteId = quote.ProduccionDetalle.PersonalCotizacionId;
                    selectedPersonnelQuote = personnelQuotes.FirstOrDefault(x => x.Id == selectedPersonnelQuoteId.Value);
                    if (selectedPersonnelQuote != null)
                    {
                        selectedFilterEmpresa = selectedPersonnelQuote.ClienteNombre;
                    }
                }

                // Cargar materiales seleccionados
                selectedMateriales.Clear();
                customMaterialesCosts.Clear();
                if (quote.MaterialDetalles != null)
                {
                    foreach (var mat in quote.MaterialDetalles)
                    {
                        if (mat.MaterialId.HasValue)
                        {
                            selectedMateriales.Add(mat.MaterialId.Value);
                            customMaterialesCosts[mat.MaterialId.Value] = mat.CostoUnitario;
                        }
                    }
                }

                // Cargar maquinaria seleccionada
                selectedMachinery.Clear();
                if (quote.MaquinariaDetalles != null)
                {
                    foreach (var maq in quote.MaquinariaDetalles)
                    {
                        if (maq.MaquinariaId.HasValue)
                        {
                            selectedMachinery.Add(maq.MaquinariaId.Value);
                        }
                    }
                }
            }
            Snackbar.Add($"Cotización de '{cliente}' cargada con éxito.", Severity.Info);
        }

        private async Task SaveQuote()
        {
            if (string.IsNullOrWhiteSpace(cliente))
            {
                Snackbar.Add("El nombre del cliente es obligatorio.", Severity.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(skuNombre))
            {
                Snackbar.Add("El nombre del SKU es obligatorio.", Severity.Warning);
                return;
            }

            var detail = new CotizacionProduccionDetalle
            {
                SkuNombre = skuNombre,
                ProduccionDiaria = produccionDiaria,
                CantidadTrabajadores = cantidadTrabajadores,
                ManoObraUnitaria = CalculatedManoObraUnitaria,
                MaterialesTotales = CalculatedMateriales,
                MermaPorcentaje = mermaPorcentaje,
                AmortizacionUnitaria = CalculatedAmortizacionUnitaria,
                PersonalCotizacionId = selectedPersonnelQuoteId
            };

            var quote = new Cotizacion
            {
                ClienteNombre = cliente,
                TipoCosteo = "Produccion",
                UtilidadPorcentaje = mermaPorcentaje,
                CostoTotal = CalculatedCostoUnitarioBase * (decimal)produccionDiaria * 30m,
                TarifaSugerida = CalculatedTarifaUnitaria,
                TarifaAcordada = tarifaAcordada,
                ProduccionDetalle = detail,
                Estado = "Borrador"
            };

            // Rellenar materiales seleccionados históricos
            foreach (var id in selectedMateriales)
            {
                var item = materialesList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    quote.MaterialDetalles.Add(new CotizacionMaterialDetalle
                    {
                        MaterialId = id,
                        Nombre = item.Nombre,
                        CostoUnitario = customMaterialesCosts.TryGetValue(id, out var cost) ? cost : item.CostoUnitario
                    });
                }
            }

            // Rellenar maquinaria seleccionada histórica
            foreach (var id in selectedMachinery)
            {
                var item = machineryList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    quote.MaquinariaDetalles.Add(new CotizacionMaquinariaDetalle
                    {
                        MaquinariaId = id,
                        Nombre = item.Nombre,
                        Precio = item.Precio,
                        Cantidad = item.Cantidad,
                        MesesProyeccion = item.MesesProyeccion,
                        ProyeccionMensual = item.ProyeccionMensual
                    });
                }
            }

            var res = await Mediator.Send(new SaveCotizacionCommand(quote));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Cotización de producción guardada exitosamente.", Severity.Success);
                cliente = "";
                skuNombre = "";
                selectedPersonnelQuoteId = null;
                selectedPersonnelQuote = null;
                selectedMateriales.Clear();
                customMaterialesCosts.Clear();
                selectedMachinery.Clear();
                await LoadQuotes();
            }
            else
            {
                Snackbar.Add("Error al guardar: " + res.Respuesta.MensajeError, Severity.Error);
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

        private Color GetStatusColor(string status)
        {
            return status switch
            {
                "Aprobado" => Color.Success,
                "Rechazado" => Color.Error,
                _ => Color.Default
            };
        }

        // GESTIÓN DE CATÁLOGOS LOCALES
        private void EditMaterial(CatalogoMaterial item)
        {
            editingMaterial = new CatalogoMaterial { Id = item.Id, Nombre = item.Nombre, CostoUnitario = item.CostoUnitario };
        }

        private void CancelEditMaterial()
        {
            editingMaterial = new CatalogoMaterial();
        }

        private async Task SaveMaterial()
        {
            if (string.IsNullOrWhiteSpace(editingMaterial.Nombre))
            {
                Snackbar.Add("El nombre del material es obligatorio.", Severity.Warning);
                return;
            }

            var res = await Mediator.Send(new SaveCatalogoMaterialCommand(editingMaterial));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Material guardado exitosamente.", Severity.Success);
                editingMaterial = new CatalogoMaterial();
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al guardar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task DeleteMaterial(int id)
        {
            var res = await Mediator.Send(new DeleteCatalogoMaterialCommand(id));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Material eliminado.", Severity.Info);
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al eliminar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        // MAQUINARIA
        private void EditMachinery(CatalogoMaquinaria item)
        {
            editingMachinery = new CatalogoMaquinaria
            {
                Id = item.Id,
                Nombre = item.Nombre,
                Precio = item.Precio,
                Cantidad = item.Cantidad,
                MesesProyeccion = item.MesesProyeccion
            };
        }

        private void CancelEditMachinery()
        {
            editingMachinery = new CatalogoMaquinaria();
        }

        private async Task SaveMachinery()
        {
            if (string.IsNullOrWhiteSpace(editingMachinery.Nombre))
            {
                Snackbar.Add("El nombre es obligatorio.", Severity.Warning);
                return;
            }

            var res = await Mediator.Send(new SaveCatalogoMaquinariaCommand(editingMachinery));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Maquinaria guardada exitosamente.", Severity.Success);
                editingMachinery = new CatalogoMaquinaria();
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al guardar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task DeleteMachinery(int id)
        {
            var res = await Mediator.Send(new DeleteCatalogoMaquinariaCommand(id));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Maquinaria eliminada.", Severity.Info);
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al eliminar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        // EPP
        private void EditEpp(CatalogoEpp item)
        {
            editingEpp = new CatalogoEpp
            {
                Id = item.Id,
                Nombre = item.Nombre,
                Cantidad = item.Cantidad,
                MesesProrrateo = item.MesesProrrateo,
                CostoUnitario = item.CostoUnitario
            };
        }

        private void CancelEditEpp()
        {
            editingEpp = new CatalogoEpp();
        }

        private async Task SaveEpp()
        {
            if (string.IsNullOrWhiteSpace(editingEpp.Nombre))
            {
                Snackbar.Add("El nombre es obligatorio.", Severity.Warning);
                return;
            }

            var res = await Mediator.Send(new SaveCatalogoEppCommand(editingEpp));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("EPP guardado exitosamente.", Severity.Success);
                editingEpp = new CatalogoEpp();
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al guardar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task DeleteEpp(int id)
        {
            var res = await Mediator.Send(new DeleteCatalogoEppCommand(id));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("EPP eliminado.", Severity.Info);
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al eliminar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        // VIATICOS
        private void EditViatico(CatalogoViatico item)
        {
            editingViatico = new CatalogoViatico { Id = item.Id, Nombre = item.Nombre, CostoMensual = item.CostoMensual };
        }

        private void CancelEditViatico()
        {
            editingViatico = new CatalogoViatico();
        }

        private async Task SaveViatico()
        {
            if (string.IsNullOrWhiteSpace(editingViatico.Nombre))
            {
                Snackbar.Add("El nombre es obligatorio.", Severity.Warning);
                return;
            }

            var res = await Mediator.Send(new SaveCatalogoViaticoCommand(editingViatico));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Viático guardado exitosamente.", Severity.Success);
                editingViatico = new CatalogoViatico();
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al guardar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task DeleteViatico(int id)
        {
            var res = await Mediator.Send(new DeleteCatalogoViaticoCommand(id));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Viático eliminado.", Severity.Info);
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al eliminar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task UploadExcelFile(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file == null) return;

            try
            {
                using (var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024))
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        var fileBytes = ms.ToArray();
                        var res = await Mediator.Send(new ParseProductsExcelCommand(fileBytes, file.Name));
                        if (!res.Respuesta.ExisteError && res.Model != null)
                        {
                            importedProducts = res.Model;
                            if (importedProducts.Any())
                            {
                                currentProductIndex = 0;
                                skuNombre = importedProducts[0];
                                Snackbar.Add($"Se importaron {importedProducts.Count} productos. Iniciando cotización.", Severity.Success);
                            }
                            else
                            {
                                Snackbar.Add("El Excel no contiene productos en la primera columna.", Severity.Warning);
                            }
                        }
                        else
                        {
                            Snackbar.Add("Error al leer Excel: " + res.Respuesta.MensajeError, Severity.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error de carga: " + ex.Message, Severity.Error);
            }
        }

        private void ClearImportedProducts()
        {
            importedProducts.Clear();
            currentProductIndex = -1;
            skuNombre = "";
            Snackbar.Add("Lista de productos importados limpiada.", Severity.Info);
        }

        private void GoToPreviousProduct()
        {
            if (currentProductIndex > 0)
            {
                currentProductIndex--;
                skuNombre = importedProducts[currentProductIndex];
            }
        }

        private void GoToNextProduct()
        {
            if (currentProductIndex < importedProducts.Count - 1)
            {
                currentProductIndex++;
                skuNombre = importedProducts[currentProductIndex];
            }
        }
    }
}
