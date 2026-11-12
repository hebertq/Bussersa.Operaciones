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
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;

        private Guid? editingQuoteId;
        private string? editingNumeroCotizacion;
        private bool isPrinting = false;
        private bool showExcelPanel = false;
        private HashSet<Cotizacion> expandedQuotes = new HashSet<Cotizacion>();
        private Dictionary<Guid, HashSet<CotizacionProduccionDetalle>> selectedSkusForPrint = new Dictionary<Guid, HashSet<CotizacionProduccionDetalle>>();

        private string cliente = "";
        private string skuNombre = "";
        private int produccionDiaria = 1000;
        private int cantidadTrabajadores = 8;
        private decimal diasOperativosMes = 26.00m;
        private decimal turnosAsignadosMes = 26.00m;
        private string manualTurno = "Diurno";
        private bool isTarifaManualOverride = false;

        protected int activeTabIndex = 0;
        protected void SetActiveTab(int index)
        {
            activeTabIndex = index;
            StateHasChanged();
        }

        private decimal manoObraUnitaria = 1.5000m;
        private decimal mermaPorcentaje = 2.00m;
        private decimal utilidadPorcentaje = 35.00m;
        private decimal tarifaAcordada = 0.0000m;

        private Guid? selectedPersonnelQuoteId;
        private Cotizacion? selectedPersonnelQuote;
        private string selectedFilterEmpresa = "";
        private List<string> importedProducts = new List<string>();
        private int currentProductIndex = -1;

        private List<CatalogoEpp> eppList = new List<CatalogoEpp>();
        private List<CatalogoViatico> viaticosList = new List<CatalogoViatico>();
        private List<CatalogoMaquinaria> machineryList = new List<CatalogoMaquinaria>();
        private List<CatalogoMaterial> materialesList = new List<CatalogoMaterial>();

        private List<Cotizacion> savedQuotes = new List<Cotizacion>();
        private List<Cotizacion> personnelQuotes = new List<Cotizacion>();

        private List<CotizacionProduccionDetalle> currentItems = new List<CotizacionProduccionDetalle>();
        private List<CotizacionMaterialDetalle> tempMaterialDetalles = new List<CotizacionMaterialDetalle>();
        private List<CotizacionMaquinariaDetalle> tempMaquinariaDetalles = new List<CotizacionMaquinariaDetalle>();

        private HashSet<int> selectedMachinery = new HashSet<int>();
        private Dictionary<int, decimal> customMachineryPrices = new Dictionary<int, decimal>();
        private Dictionary<int, int> customMachineryQuantities = new Dictionary<int, int>();
        private Dictionary<int, int> customMachineryMonths = new Dictionary<int, int>();

        private HashSet<int> selectedMateriales = new HashSet<int>();
        private Dictionary<int, decimal> customMaterialesCosts = new Dictionary<int, decimal>();
        private Dictionary<int, decimal> customMaterialesQuantities = new Dictionary<int, decimal>();

        private int? selectedMaterialToAdd;
        private int? selectedMachineryToAdd;

        // Modelos para gestión de catálogos
        private CatalogoMaterial editingMaterial = new CatalogoMaterial();
        private CatalogoMaquinaria editingMachinery = new CatalogoMaquinaria();
        private CatalogoEpp editingEpp = new CatalogoEpp();
        private CatalogoViatico editingViatico = new CatalogoViatico();

        // Turno activo y horas del turno
        private string CurrentTurnoNombre => selectedPersonnelQuote?.PersonalDetalle?.Turno
            ?? (string.IsNullOrWhiteSpace(manualTurno) ? "Diurno" : manualTurno);

        private int CurrentHorasTurno => (selectedPersonnelQuote?.PersonalDetalle?.HorasTurno ?? 0) > 0
            ? selectedPersonnelQuote!.PersonalDetalle.HorasTurno
            : 8;

        private bool IsNightShift(string? turnoName)
        {
            if (string.IsNullOrWhiteSpace(turnoName)) return false;
            var t = turnoName.ToLowerInvariant();
            return t.Contains("noct") || t.Contains("noche");
        }

        private decimal CalculatedUnidadesPorHora => CurrentHorasTurno > 0
            ? Math.Round((decimal)produccionDiaria / CurrentHorasTurno, 1)
            : produccionDiaria;

        // Costos de Personal y Cuadrilla por Turno
        private decimal EffectiveDiasMes => diasOperativosMes > 0m ? diasOperativosMes : 26m;

        private decimal CalculatedCostoDiarioPersona => selectedPersonnelQuote != null
            ? (selectedPersonnelQuote.CostoTotal / EffectiveDiasMes)
            : 0m;

        private decimal CalculatedCostoTurnoCuadrilla => CalculatedCostoDiarioPersona * Math.Max(1, cantidadTrabajadores);

        private decimal CalculatedCostoMensualCuadrilla => selectedPersonnelQuote != null
            ? (selectedPersonnelQuote.CostoTotal * Math.Max(1, cantidadTrabajadores))
            : 0m;

        private decimal CalculatedManoObraUnitaria => selectedPersonnelQuoteId.HasValue
            ? ((produccionDiaria > 0) ? (CalculatedCostoTurnoCuadrilla / (decimal)produccionDiaria) : 0m)
            : manoObraUnitaria;

        // Costos de Materiales por Unidad
        private decimal CalculatedMateriales => materialesList
            .Where(x => selectedMateriales.Contains(x.Id))
            .Sum(x => GetMaterialCost(x.Id) * GetMaterialQuantity(x.Id));

        // Costos de Maquinaria y Equipos
        private decimal GetMachineryPrice(int id)
        {
            if (customMachineryPrices.TryGetValue(id, out var p)) return p;
            return machineryList.FirstOrDefault(x => x.Id == id)?.Precio ?? 0m;
        }

        private int GetMachineryQuantity(int id)
        {
            if (customMachineryQuantities.TryGetValue(id, out var q)) return q;
            return machineryList.FirstOrDefault(x => x.Id == id)?.Cantidad ?? 1;
        }

        private int GetMachineryMonths(int id)
        {
            if (customMachineryMonths.TryGetValue(id, out var m) && m > 0) return m;
            var catM = machineryList.FirstOrDefault(x => x.Id == id)?.MesesProyeccion ?? 12;
            return catM > 0 ? catM : 12;
        }

        private decimal GetMachineryMonthly(int id)
        {
            int months = GetMachineryMonths(id);
            decimal totalInv = GetMachineryPrice(id) * GetMachineryQuantity(id);
            return months > 0 ? (totalInv / months) : totalInv;
        }

        private decimal CalculatedCapexTotal => machineryList
            .Where(x => selectedMachinery.Contains(x.Id))
            .Sum(x => GetMachineryPrice(x.Id) * GetMachineryQuantity(x.Id));

        private decimal CalculatedAmortizacionMensual => machineryList
            .Where(x => selectedMachinery.Contains(x.Id))
            .Sum(x => GetMachineryMonthly(x.Id));

        private decimal CalculatedCostoTurnoMaquinaria => CalculatedAmortizacionMensual / EffectiveDiasMes;

        private decimal CalculatedAmortizacionUnitaria => (produccionDiaria > 0)
            ? (CalculatedCostoTurnoMaquinaria / (decimal)produccionDiaria)
            : 0m;

        // Cascada de Costo Unitario y Precio Comercial
        private decimal CalculatedCostoUnitarioBase => CalculatedManoObraUnitaria + CalculatedMateriales + CalculatedAmortizacionUnitaria;

        private decimal CalculatedMermaCosto => CalculatedCostoUnitarioBase * (mermaPorcentaje / 100m);

        private decimal CalculatedCostoOperativoUnitario => CalculatedCostoUnitarioBase + CalculatedMermaCosto;

        private decimal CalculatedUtilidadUnitaria => (utilidadPorcentaje > 0m && utilidadPorcentaje < 100m)
            ? (CalculatedCostoOperativoUnitario / (1m - (utilidadPorcentaje / 100m))) - CalculatedCostoOperativoUnitario
            : 0m;

        private decimal CalculatedTarifaUnitaria => Math.Round(CalculatedCostoOperativoUnitario + CalculatedUtilidadUnitaria, 4);

        // Proyecciones por Turno y Mensual
        private decimal EffectiveTurnosAsignadosMes => turnosAsignadosMes > 0m ? turnosAsignadosMes : EffectiveDiasMes;

        private decimal CalculatedFacturacionDiaria => tarifaAcordada * (decimal)produccionDiaria;
        private decimal CalculatedCostoPorTurno => CalculatedCostoOperativoUnitario * (decimal)produccionDiaria;
        private decimal CalculatedUtilidadPorTurno => CalculatedFacturacionDiaria - CalculatedCostoPorTurno;

        private decimal CalculatedFacturacionMensual => CalculatedFacturacionDiaria * EffectiveTurnosAsignadosMes;
        private decimal CalculatedCostoMensualSku => CalculatedCostoPorTurno * EffectiveTurnosAsignadosMes;
        private decimal CalculatedUtilidadMensualSku => CalculatedFacturacionMensual - CalculatedCostoMensualSku;

        private decimal CalculatedMargenRealPorcentaje => tarifaAcordada > 0m
            ? ((tarifaAcordada - CalculatedCostoOperativoUnitario) / tarifaAcordada) * 100m
            : 0m;

        // Punto de equilibrio del turno completo (Cuadrilla N personas + Equipos)
        private decimal CalculatedCostoFijoTurnoConMerma => (CalculatedCostoTurnoCuadrilla + CalculatedCostoTurnoMaquinaria) * (1m + (mermaPorcentaje / 100m));
        private decimal CalculatedMaterialUnitarioConMerma => CalculatedMateriales * (1m + (mermaPorcentaje / 100m));
        private decimal CalculatedMargenContribucionUnitario => tarifaAcordada - CalculatedMaterialUnitarioConMerma;

        private decimal CalculatedUnidadesBreakeven => (CalculatedMargenContribucionUnitario > 0m && CalculatedCostoFijoTurnoConMerma > 0m)
            ? Math.Ceiling(CalculatedCostoFijoTurnoConMerma / CalculatedMargenContribucionUnitario)
            : 0m;

        // Cálculos dinámicos consolidados de la cotización actual
        private decimal GetItemTurnosMes(CotizacionProduccionDetalle x)
            => x.TurnosAsignadosMes > 0m ? x.TurnosAsignadosMes : (x.DiasOperativosMes > 0m ? x.DiasOperativosMes : 26m);

        private decimal GetItemCostoOperativoUnit(CotizacionProduccionDetalle x)
            => (x.ManoObraUnitaria + x.MaterialesTotales + x.AmortizacionUnitaria) * (1m + (x.MermaPorcentaje / 100m));

        private decimal ConsolidatedTurnosTotalesMes => currentItems.Sum(x => GetItemTurnosMes(x));
        private decimal ConsolidatedUnidadesMensuales => currentItems.Sum(x => x.ProduccionDiaria * GetItemTurnosMes(x));
        private decimal ConsolidatedCostoMensual => currentItems.Sum(x => GetItemCostoOperativoUnit(x) * x.ProduccionDiaria * GetItemTurnosMes(x));
        private decimal ConsolidatedFacturacionMensual => currentItems.Sum(x => x.TarifaAcordada * x.ProduccionDiaria * GetItemTurnosMes(x));
        private decimal ConsolidatedUtilidadMensual => ConsolidatedFacturacionMensual - ConsolidatedCostoMensual;
        private decimal ConsolidatedMargenPorcentaje => ConsolidatedFacturacionMensual > 0m
            ? (ConsolidatedUtilidadMensual / ConsolidatedFacturacionMensual) * 100m
            : 0m;
        private decimal ConsolidatedTarifaSugeridaPromedio => currentItems.Any() ? currentItems.Average(x => x.TarifaSugerida) : 0m;
        private decimal ConsolidatedTarifaAcordadaPromedio => currentItems.Any() ? currentItems.Average(x => x.TarifaAcordada) : 0m;

        protected override async Task OnInitializedAsync()
        {
            await LoadCatalogs();
            await LoadQuotes();
        }

        private void GoToCosteoPersonal()
        {
            NavManager.NavigateTo("/comercial/costeo-personal");
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
            SyncTarifaIfAuto();
        }

        private void SyncTarifaIfAuto()
        {
            if (!isTarifaManualOverride || tarifaAcordada <= 0m)
            {
                tarifaAcordada = Math.Round(CalculatedTarifaUnitaria, 2);
            }
        }

        private void ResetTarifaToSuggested()
        {
            isTarifaManualOverride = false;
            tarifaAcordada = Math.Round(CalculatedTarifaUnitaria, 2);
        }

        private void OnTarifaAcordadaManualChanged(decimal val)
        {
            tarifaAcordada = val;
            isTarifaManualOverride = Math.Abs(val - Math.Round(CalculatedTarifaUnitaria, 2)) > 0.004m;
        }

        private void OnEmpresaFilterChanged(string val)
        {
            selectedFilterEmpresa = val;
            if (string.IsNullOrWhiteSpace(cliente) && !string.IsNullOrWhiteSpace(val))
            {
                cliente = val;
            }

            var companyShifts = personnelQuotes.Where(x => x.ClienteNombre == selectedFilterEmpresa).ToList();
            if (companyShifts.Any())
            {
                var preferred = companyShifts.FirstOrDefault(x => !IsNightShift(x.PersonalDetalle?.Turno)) ?? companyShifts.First();
                OnPersonnelQuoteChanged(preferred.Id);
            }
            else
            {
                selectedPersonnelQuoteId = null;
                selectedPersonnelQuote = null;
                SyncTarifaIfAuto();
            }
        }

        private void OnPersonnelQuoteChanged(Guid? id)
        {
            selectedPersonnelQuoteId = id;
            if (id.HasValue)
            {
                selectedPersonnelQuote = personnelQuotes.FirstOrDefault(x => x.Id == id.Value);
                if (selectedPersonnelQuote != null)
                {
                    int hrs = selectedPersonnelQuote.PersonalDetalle?.HorasTurno ?? 8;
                    if (hrs == 12 && diasOperativosMes == 26m)
                    {
                        diasOperativosMes = 16m;
                        turnosAsignadosMes = 16m;
                    }
                    else if (hrs <= 8 && diasOperativosMes == 16m)
                    {
                        diasOperativosMes = 26m;
                        turnosAsignadosMes = 26m;
                    }
                }
            }
            else
            {
                selectedPersonnelQuote = null;
            }
            SyncTarifaIfAuto();
        }

        private void OnCantidadTrabajadoresChanged(int val)
        {
            cantidadTrabajadores = Math.Max(1, val);
            SyncTarifaIfAuto();
        }

        private void OnDiasOperativosMesChanged(decimal val)
        {
            decimal oldDias = diasOperativosMes;
            diasOperativosMes = val > 0m ? val : 26m;
            if (turnosAsignadosMes == oldDias)
            {
                turnosAsignadosMes = diasOperativosMes;
            }
            SyncTarifaIfAuto();
        }

        private void OnProduccionDiariaChanged(int val)
        {
            produccionDiaria = Math.Max(1, val);
            SyncTarifaIfAuto();
        }

        private void OnUnidadesPorHoraChanged(decimal packsHora)
        {
            if (packsHora > 0m)
            {
                produccionDiaria = Math.Max(1, (int)Math.Round(packsHora * CurrentHorasTurno, 0));
                SyncTarifaIfAuto();
            }
        }

        private void OnMermaChanged(decimal val)
        {
            mermaPorcentaje = Math.Max(0m, val);
            SyncTarifaIfAuto();
        }

        private void OnUtilidadChanged(decimal val)
        {
            utilidadPorcentaje = Math.Clamp(val, 0m, 95m);
            SyncTarifaIfAuto();
        }

        private void OnManoObraManualChanged(decimal val)
        {
            manoObraUnitaria = Math.Max(0m, val);
            SyncTarifaIfAuto();
        }

        // Materiales
        private decimal GetMaterialCost(int id)
        {
            if (customMaterialesCosts.TryGetValue(id, out var cost))
                return cost;
            var item = materialesList.FirstOrDefault(x => x.Id == id);
            return item?.CostoUnitario ?? 0m;
        }

        private decimal GetMaterialQuantity(int id)
        {
            if (customMaterialesQuantities.TryGetValue(id, out var q))
                return q;
            return 1m;
        }

        private void SetMaterialCost(int id, decimal cost)
        {
            customMaterialesCosts[id] = cost;
            SyncTarifaIfAuto();
        }

        private void SetMaterialQuantity(int id, decimal qty)
        {
            customMaterialesQuantities[id] = qty;
            SyncTarifaIfAuto();
        }

        private void AddMaterialRow(int? id)
        {
            if (id.HasValue)
            {
                selectedMateriales.Add(id.Value);
                if (!customMaterialesQuantities.ContainsKey(id.Value))
                {
                    customMaterialesQuantities[id.Value] = 1m;
                }
                if (!customMaterialesCosts.ContainsKey(id.Value))
                {
                    var item = materialesList.FirstOrDefault(x => x.Id == id.Value);
                    if (item != null)
                    {
                        customMaterialesCosts[id.Value] = item.CostoUnitario;
                    }
                }
                SyncTarifaIfAuto();
            }
            selectedMaterialToAdd = null;
        }

        private void RemoveMaterialRow(int id)
        {
            selectedMateriales.Remove(id);
            SyncTarifaIfAuto();
        }

        private void ClearSelectedMateriales()
        {
            selectedMateriales.Clear();
            customMaterialesCosts.Clear();
            customMaterialesQuantities.Clear();
            SyncTarifaIfAuto();
        }

        // Maquinaria
        private void SetMachineryPrice(int id, decimal price)
        {
            customMachineryPrices[id] = Math.Max(0m, price);
            SyncTarifaIfAuto();
        }

        private void SetMachineryQuantity(int id, int qty)
        {
            customMachineryQuantities[id] = Math.Max(1, qty);
            SyncTarifaIfAuto();
        }

        private void SetMachineryMonths(int id, int months)
        {
            customMachineryMonths[id] = Math.Max(1, months);
            SyncTarifaIfAuto();
        }

        private void AddMachineryRow(int? id)
        {
            if (id.HasValue)
            {
                selectedMachinery.Add(id.Value);
                var maq = machineryList.FirstOrDefault(x => x.Id == id.Value);
                if (maq != null)
                {
                    if (!customMachineryPrices.ContainsKey(id.Value)) customMachineryPrices[id.Value] = maq.Precio;
                    if (!customMachineryQuantities.ContainsKey(id.Value)) customMachineryQuantities[id.Value] = Math.Max(1, maq.Cantidad);
                    if (!customMachineryMonths.ContainsKey(id.Value)) customMachineryMonths[id.Value] = maq.MesesProyeccion > 0 ? maq.MesesProyeccion : 12;
                }
                SyncTarifaIfAuto();
            }
            selectedMachineryToAdd = null;
        }

        private void RemoveMachineryRow(int id)
        {
            selectedMachinery.Remove(id);
            SyncTarifaIfAuto();
        }

        private void ClearSelectedMachinery()
        {
            selectedMachinery.Clear();
            customMachineryPrices.Clear();
            customMachineryQuantities.Clear();
            customMachineryMonths.Clear();
            SyncTarifaIfAuto();
        }

        private void AddProductToQuote()
        {
            if (string.IsNullOrWhiteSpace(skuNombre))
            {
                Snackbar.Add("Debe ingresar la descripción del SKU / Producto.", Severity.Warning);
                return;
            }

            string turnoActual = CurrentTurnoNombre;

            if (currentItems.Any(x => x.SkuNombre.Equals(skuNombre.Trim(), StringComparison.OrdinalIgnoreCase)
                                   && string.Equals(x.Turno ?? "Diurno", turnoActual, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add($"El SKU '{skuNombre}' ya fue agregado en el turno '{turnoActual}'.", Severity.Warning);
                return;
            }

            if (tarifaAcordada <= 0m)
            {
                Snackbar.Add("La tarifa acordada por unidad debe ser mayor a cero.", Severity.Warning);
                return;
            }

            string cleanSku = skuNombre.Trim();

            var item = new CotizacionProduccionDetalle
            {
                SkuNombre = cleanSku,
                Turno = turnoActual,
                ProduccionDiaria = produccionDiaria,
                CantidadTrabajadores = Math.Max(1, cantidadTrabajadores),
                DiasOperativosMes = EffectiveDiasMes,
                TurnosAsignadosMes = EffectiveTurnosAsignadosMes,
                ManoObraUnitaria = CalculatedManoObraUnitaria,
                MaterialesTotales = CalculatedMateriales,
                MermaPorcentaje = mermaPorcentaje,
                UtilidadPorcentaje = utilidadPorcentaje,
                AmortizacionUnitaria = CalculatedAmortizacionUnitaria,
                PersonalCotizacionId = selectedPersonnelQuoteId,
                TarifaSugerida = CalculatedTarifaUnitaria,
                TarifaAcordada = tarifaAcordada
            };

            currentItems.Add(item);

            // Reemplazar detalles previos de este SKU si no había otro turno usándolos
            tempMaterialDetalles.RemoveAll(x => x.SkuNombre == cleanSku);
            foreach (var matId in selectedMateriales)
            {
                var matInfo = materialesList.FirstOrDefault(x => x.Id == matId);
                if (matInfo != null)
                {
                    tempMaterialDetalles.Add(new CotizacionMaterialDetalle
                    {
                        MaterialId = matId,
                        Nombre = matInfo.Nombre,
                        Cantidad = GetMaterialQuantity(matId),
                        CostoUnitario = GetMaterialCost(matId),
                        SkuNombre = cleanSku
                    });
                }
            }

            tempMaquinariaDetalles.RemoveAll(x => x.SkuNombre == cleanSku);
            foreach (var maqId in selectedMachinery)
            {
                var maqInfo = machineryList.FirstOrDefault(x => x.Id == maqId);
                if (maqInfo != null)
                {
                    tempMaquinariaDetalles.Add(new CotizacionMaquinariaDetalle
                    {
                        MaquinariaId = maqId,
                        Nombre = maqInfo.Nombre,
                        Precio = GetMachineryPrice(maqId),
                        Cantidad = GetMachineryQuantity(maqId),
                        MesesProyeccion = GetMachineryMonths(maqId),
                        Personas = Math.Max(1, cantidadTrabajadores),
                        ProyeccionMensual = GetMachineryMonthly(maqId),
                        SkuNombre = cleanSku
                    });
                }
            }

            Snackbar.Add($"SKU '{cleanSku}' ({turnoActual}) agregado a la cotización.", Severity.Success);

            // Avanzar al siguiente producto si viene de un lote Excel, preservando el Turno, Cuadrilla y Maquinaria para cotizar en serie rápido
            if (importedProducts.Any() && currentProductIndex >= 0 && currentProductIndex < importedProducts.Count - 1)
            {
                currentProductIndex++;
                skuNombre = importedProducts[currentProductIndex];
            }
            else
            {
                skuNombre = "";
            }

            isTarifaManualOverride = false;
            SyncTarifaIfAuto();
        }

        private void DuplicateSkuForAlternateShift(CotizacionProduccionDetalle sourceItem)
        {
            if (string.IsNullOrEmpty(selectedFilterEmpresa) && sourceItem.PersonalCotizacionId.HasValue)
            {
                var p = personnelQuotes.FirstOrDefault(x => x.Id == sourceItem.PersonalCotizacionId.Value);
                if (p != null) selectedFilterEmpresa = p.ClienteNombre;
            }

            var altQuote = personnelQuotes
                .Where(x => x.ClienteNombre == selectedFilterEmpresa && x.Id != sourceItem.PersonalCotizacionId)
                .FirstOrDefault();

            if (altQuote == null)
            {
                Snackbar.Add("No se encontró otro turno en Costeo de Personal para este cliente. Puede crearlo en Costeo de Personal.", Severity.Info);
                return;
            }

            string altTurno = altQuote.PersonalDetalle?.Turno ?? "Nocturno";
            if (currentItems.Any(x => x.SkuNombre.Equals(sourceItem.SkuNombre, StringComparison.OrdinalIgnoreCase)
                                   && string.Equals(x.Turno, altTurno, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add($"El SKU '{sourceItem.SkuNombre}' ya existe en el turno '{altTurno}'.", Severity.Warning);
                return;
            }

            decimal diasBase = sourceItem.DiasOperativosMes > 0m ? sourceItem.DiasOperativosMes : 26m;
            int workers = Math.Max(1, sourceItem.CantidadTrabajadores);
            decimal costoTurnoCuadrillaAlt = (altQuote.CostoTotal / diasBase) * workers;
            decimal moUnitAlt = sourceItem.ProduccionDiaria > 0 ? (costoTurnoCuadrillaAlt / sourceItem.ProduccionDiaria) : sourceItem.ManoObraUnitaria;

            decimal costoBaseSinMerma = moUnitAlt + sourceItem.MaterialesTotales + sourceItem.AmortizacionUnitaria;
            decimal costoConMerma = costoBaseSinMerma * (1m + (sourceItem.MermaPorcentaje / 100m));
            decimal utilPct = sourceItem.UtilidadPorcentaje > 0m ? sourceItem.UtilidadPorcentaje : utilidadPorcentaje;
            decimal sugeridaAlt = (utilPct > 0m && utilPct < 100m)
                ? Math.Round(costoConMerma / (1m - (utilPct / 100m)), 4)
                : Math.Round(costoConMerma, 4);

            currentItems.Add(new CotizacionProduccionDetalle
            {
                SkuNombre = sourceItem.SkuNombre,
                Turno = altTurno,
                ProduccionDiaria = sourceItem.ProduccionDiaria,
                CantidadTrabajadores = workers,
                DiasOperativosMes = diasBase,
                TurnosAsignadosMes = sourceItem.TurnosAsignadosMes,
                ManoObraUnitaria = moUnitAlt,
                MaterialesTotales = sourceItem.MaterialesTotales,
                MermaPorcentaje = sourceItem.MermaPorcentaje,
                UtilidadPorcentaje = utilPct,
                AmortizacionUnitaria = sourceItem.AmortizacionUnitaria,
                PersonalCotizacionId = altQuote.Id,
                TarifaSugerida = sugeridaAlt,
                TarifaAcordada = Math.Round(sugeridaAlt, 2)
            });

            Snackbar.Add($"SKU '{sourceItem.SkuNombre}' calculado y agregado para turno '{altTurno}'.", Severity.Success);
        }

        private void RemoveProductFromQuote(CotizacionProduccionDetalle item)
        {
            currentItems.Remove(item);
            if (!currentItems.Any(x => x.SkuNombre == item.SkuNombre))
            {
                tempMaterialDetalles.RemoveAll(x => x.SkuNombre == item.SkuNombre);
                tempMaquinariaDetalles.RemoveAll(x => x.SkuNombre == item.SkuNombre);
            }
            Snackbar.Add($"Producto '{item.SkuNombre}' removido de la cotización.", Severity.Info);
        }

        private void EditProductInQuote(CotizacionProduccionDetalle item)
        {
            skuNombre = item.SkuNombre;
            produccionDiaria = item.ProduccionDiaria;
            cantidadTrabajadores = item.CantidadTrabajadores > 0 ? item.CantidadTrabajadores : 8;
            diasOperativosMes = item.DiasOperativosMes > 0m ? item.DiasOperativosMes : 26m;
            turnosAsignadosMes = item.TurnosAsignadosMes > 0m ? item.TurnosAsignadosMes : diasOperativosMes;
            mermaPorcentaje = item.MermaPorcentaje;
            utilidadPorcentaje = item.UtilidadPorcentaje > 0m ? item.UtilidadPorcentaje : 35m;
            manualTurno = string.IsNullOrWhiteSpace(item.Turno) ? "Diurno" : item.Turno;

            selectedPersonnelQuoteId = item.PersonalCotizacionId;
            if (selectedPersonnelQuoteId.HasValue)
            {
                selectedPersonnelQuote = personnelQuotes.FirstOrDefault(x => x.Id == selectedPersonnelQuoteId.Value);
                if (selectedPersonnelQuote != null)
                {
                    selectedFilterEmpresa = selectedPersonnelQuote.ClienteNombre;
                }
            }
            else
            {
                selectedPersonnelQuote = null;
                manoObraUnitaria = item.ManoObraUnitaria;
            }

            // Cargar materiales de este SKU
            selectedMateriales.Clear();
            customMaterialesCosts.Clear();
            customMaterialesQuantities.Clear();
            var mats = tempMaterialDetalles.Where(x => x.SkuNombre == item.SkuNombre).ToList();
            foreach (var mat in mats)
            {
                if (mat.MaterialId.HasValue)
                {
                    selectedMateriales.Add(mat.MaterialId.Value);
                    customMaterialesCosts[mat.MaterialId.Value] = mat.CostoUnitario;
                    customMaterialesQuantities[mat.MaterialId.Value] = mat.Cantidad;
                }
            }

            // Cargar maquinaria de este SKU con sus cantidades y meses personalizados
            selectedMachinery.Clear();
            customMachineryPrices.Clear();
            customMachineryQuantities.Clear();
            customMachineryMonths.Clear();
            var maqs = tempMaquinariaDetalles.Where(x => x.SkuNombre == item.SkuNombre).ToList();
            foreach (var maq in maqs)
            {
                if (maq.MaquinariaId.HasValue)
                {
                    int id = maq.MaquinariaId.Value;
                    selectedMachinery.Add(id);
                    customMachineryPrices[id] = maq.Precio;
                    customMachineryQuantities[id] = Math.Max(1, maq.Cantidad);
                    customMachineryMonths[id] = maq.MesesProyeccion > 0 ? maq.MesesProyeccion : 12;
                }
            }

            tarifaAcordada = item.TarifaAcordada;
            isTarifaManualOverride = Math.Abs(item.TarifaAcordada - Math.Round(CalculatedTarifaUnitaria, 2)) > 0.004m;

            // Quitar de la lista temporal para re-agregar al guardar la edición del SKU
            currentItems.Remove(item);
            if (!currentItems.Any(x => x.SkuNombre == item.SkuNombre))
            {
                tempMaterialDetalles.RemoveAll(x => x.SkuNombre == item.SkuNombre);
                tempMaquinariaDetalles.RemoveAll(x => x.SkuNombre == item.SkuNombre);
            }

            Snackbar.Add($"SKU '{item.SkuNombre}' ({item.Turno}) cargado en el editor.", Severity.Info);
        }

        private void LoadQuoteToForm(Cotizacion quote)
        {
            editingQuoteId = quote.Id;
            editingNumeroCotizacion = quote.NumeroCotizacion;
            cliente = quote.ClienteNombre;
            currentItems.Clear();
            tempMaterialDetalles.Clear();
            tempMaquinariaDetalles.Clear();

            if (quote.ProduccionDetalles != null && quote.ProduccionDetalles.Any())
            {
                currentItems = quote.ProduccionDetalles.ToList();
            }
            else if (quote.ProduccionDetalle != null)
            {
                currentItems.Add(quote.ProduccionDetalle);
            }

            if (quote.MaterialDetalles != null)
            {
                tempMaterialDetalles = quote.MaterialDetalles.ToList();
                if (currentItems.Any())
                {
                    var firstSku = currentItems.First().SkuNombre;
                    foreach (var mat in tempMaterialDetalles)
                    {
                        if (string.IsNullOrEmpty(mat.SkuNombre))
                        {
                            mat.SkuNombre = firstSku;
                        }
                    }
                }
            }

            if (quote.MaquinariaDetalles != null)
            {
                tempMaquinariaDetalles = quote.MaquinariaDetalles.ToList();
                if (currentItems.Any())
                {
                    var firstSku = currentItems.First().SkuNombre;
                    foreach (var maq in tempMaquinariaDetalles)
                    {
                        if (string.IsNullOrEmpty(maq.SkuNombre))
                        {
                            maq.SkuNombre = firstSku;
                        }
                    }
                }
            }

            // Pre-configurar el formulario con el turno, cuadrilla y maquinaria del primer ítem de la cotización
            var refItem = currentItems.FirstOrDefault();
            if (refItem != null)
            {
                cantidadTrabajadores = refItem.CantidadTrabajadores > 0 ? refItem.CantidadTrabajadores : 8;
                diasOperativosMes = refItem.DiasOperativosMes > 0m ? refItem.DiasOperativosMes : 26m;
                turnosAsignadosMes = refItem.TurnosAsignadosMes > 0m ? refItem.TurnosAsignadosMes : diasOperativosMes;
                mermaPorcentaje = refItem.MermaPorcentaje;
                utilidadPorcentaje = refItem.UtilidadPorcentaje > 0m ? refItem.UtilidadPorcentaje : 35m;
                manualTurno = string.IsNullOrWhiteSpace(refItem.Turno) ? "Diurno" : refItem.Turno;

                if (refItem.PersonalCotizacionId.HasValue)
                {
                    selectedPersonnelQuoteId = refItem.PersonalCotizacionId.Value;
                    selectedPersonnelQuote = personnelQuotes.FirstOrDefault(x => x.Id == refItem.PersonalCotizacionId.Value);
                    if (selectedPersonnelQuote != null)
                    {
                        selectedFilterEmpresa = selectedPersonnelQuote.ClienteNombre;
                    }
                }

                selectedMachinery.Clear();
                customMachineryPrices.Clear();
                customMachineryQuantities.Clear();
                customMachineryMonths.Clear();
                foreach (var maq in tempMaquinariaDetalles.Where(x => x.SkuNombre == refItem.SkuNombre))
                {
                    if (maq.MaquinariaId.HasValue)
                    {
                        int id = maq.MaquinariaId.Value;
                        selectedMachinery.Add(id);
                        customMachineryPrices[id] = maq.Precio;
                        customMachineryQuantities[id] = Math.Max(1, maq.Cantidad);
                        customMachineryMonths[id] = maq.MesesProyeccion > 0 ? maq.MesesProyeccion : 12;
                    }
                }
            }

            skuNombre = "";
            selectedMateriales.Clear();
            customMaterialesCosts.Clear();
            customMaterialesQuantities.Clear();
            isTarifaManualOverride = false;
            SyncTarifaIfAuto();

            activeTabIndex = 0;
            Snackbar.Add($"Cotización '{quote.NumeroCotizacion ?? cliente}' cargada en el cotizador ({currentItems.Count} SKUs).", Severity.Info);
        }

        private void ClearCurrentQuoteForm()
        {
            editingQuoteId = null;
            editingNumeroCotizacion = null;
            cliente = "";
            skuNombre = "";
            produccionDiaria = 1000;
            cantidadTrabajadores = 8;
            diasOperativosMes = 26m;
            turnosAsignadosMes = 26m;
            mermaPorcentaje = 2.00m;
            utilidadPorcentaje = 35.00m;
            selectedFilterEmpresa = "";
            selectedPersonnelQuoteId = null;
            selectedPersonnelQuote = null;
            currentItems.Clear();
            tempMaterialDetalles.Clear();
            tempMaquinariaDetalles.Clear();
            ClearSelectedMateriales();
            ClearSelectedMachinery();
            isTarifaManualOverride = false;
            SyncTarifaIfAuto();
        }

        private async Task SaveQuote()
        {
            if (string.IsNullOrWhiteSpace(cliente))
            {
                Snackbar.Add("El nombre del cliente / proyecto es obligatorio.", Severity.Warning);
                return;
            }

            if (!currentItems.Any())
            {
                Snackbar.Add("Debe agregar al menos un producto (SKU) a la cotización.", Severity.Warning);
                return;
            }

            var quote = new Cotizacion
            {
                Id = editingQuoteId ?? Guid.NewGuid(),
                NumeroCotizacion = editingNumeroCotizacion ?? $"COT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Split('-')[0].ToUpper()}",
                ClienteNombre = cliente.Trim(),
                TipoCosteo = "Produccion",
                UtilidadPorcentaje = currentItems.Average(x => x.UtilidadPorcentaje > 0m ? x.UtilidadPorcentaje : utilidadPorcentaje),
                CostoTotal = ConsolidatedCostoMensual,
                TarifaSugerida = ConsolidatedFacturacionMensual,
                TarifaAcordada = ConsolidatedFacturacionMensual,
                ProduccionDetalles = currentItems,
                MaterialDetalles = tempMaterialDetalles,
                MaquinariaDetalles = tempMaquinariaDetalles,
                Estado = "Borrador"
            };

            var res = await Mediator.Send(new SaveCotizacionCommand(quote));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Cotización de producción guardada exitosamente.", Severity.Success);
                ClearCurrentQuoteForm();
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
                MesesProyeccion = item.MesesProyeccion,
                Personas = item.Personas
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
                                showExcelPanel = true;
                                Snackbar.Add($"Se importaron {importedProducts.Count} productos. Iniciando cotización en serie.", Severity.Success);
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

        private void ToggleRow(Cotizacion quote)
        {
            if (expandedQuotes.Contains(quote))
                expandedQuotes.Remove(quote);
            else
                expandedQuotes.Add(quote);
        }

        private async Task UpdateTarifaAcordada(Cotizacion quote, CotizacionProduccionDetalle det, decimal val)
        {
            det.TarifaAcordada = val;

            // Recalcular costos y facturación mensual total de la cotización maestra
            quote.CostoTotal = quote.ProduccionDetalles.Sum(x => GetItemCostoOperativoUnit(x) * x.ProduccionDiaria * GetItemTurnosMes(x));
            quote.TarifaSugerida = quote.ProduccionDetalles.Sum(x => x.TarifaSugerida * x.ProduccionDiaria * GetItemTurnosMes(x));
            quote.TarifaAcordada = quote.ProduccionDetalles.Sum(x => x.TarifaAcordada * x.ProduccionDiaria * GetItemTurnosMes(x));

            var res = await Mediator.Send(new SaveCotizacionCommand(quote));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add($"Tarifa acordada del SKU '{det.SkuNombre}' actualizada a C$ {val:N2}.", Severity.Success);
                await LoadQuotes();
            }
            else
            {
                Snackbar.Add("Error al actualizar tarifa acordada: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private List<CotizacionProduccionDetalle> GetQuoteDetailsList(Cotizacion quote)
        {
            if (quote.ProduccionDetalles != null && quote.ProduccionDetalles.Any())
            {
                return quote.ProduccionDetalles;
            }
            if (quote.ProduccionDetalle != null)
            {
                return new List<CotizacionProduccionDetalle> { quote.ProduccionDetalle };
            }
            return new List<CotizacionProduccionDetalle>();
        }

        private HashSet<CotizacionProduccionDetalle> GetSelectedSkusForPrint(Cotizacion quote)
        {
            if (!selectedSkusForPrint.TryGetValue(quote.Id, out var skus))
            {
                skus = GetQuoteDetailsList(quote).ToHashSet();
                selectedSkusForPrint[quote.Id] = skus;
            }
            return skus;
        }

        private void OnSelectedSkusChanged(Cotizacion quote, IEnumerable<CotizacionProduccionDetalle> selected)
        {
            selectedSkusForPrint[quote.Id] = selected.ToHashSet();
            StateHasChanged();
        }

        private async Task PrintConsolidatedCotizacion(Cotizacion quote)
        {
            var selectedDetails = GetSelectedSkusForPrint(quote);
            var skus = selectedDetails.Select(d => d.SkuNombre).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            if (!skus.Any())
            {
                Snackbar.Add("Debe seleccionar al menos un SKU para imprimir.", Severity.Warning);
                return;
            }
            isPrinting = true;
            StateHasChanged();
            try
            {
                var ids = new List<Guid> { quote.Id };
                var res = await Mediator.Send(new PrintCotizacionPdfQuery(ids, skus));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Cotizacion_{quote.ClienteNombre.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Cotización de producción generada y descargada.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar PDF: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            finally
            {
                isPrinting = false;
                StateHasChanged();
            }
        }

        private async Task PrintConsolidatedCotizacionDesglose(Cotizacion quote)
        {
            var selectedDetails = GetSelectedSkusForPrint(quote);
            var skus = selectedDetails.Select(d => d.SkuNombre).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            if (!skus.Any())
            {
                Snackbar.Add("Debe seleccionar al menos un SKU para imprimir.", Severity.Warning);
                return;
            }
            isPrinting = true;
            StateHasChanged();
            try
            {
                var ids = new List<Guid> { quote.Id };
                var res = await Mediator.Send(new PrintCotizacionDesglosePdfQuery(ids, skus));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Cotizacion_Desglose_{quote.ClienteNombre.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Desglose de cotización generado y descargado.", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar PDF de desglose: " + res.Respuesta.MensajeError, Severity.Error);
                }
            }
            finally
            {
                isPrinting = false;
                StateHasChanged();
            }
        }
    }
}
