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
    public partial class CosteoPersonal : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private ISender Mediator { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private DialogOptions catalogDialogOptions = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };
        private int? selectedViaticoToAdd;
        private int? selectedEppToAdd;

        private string cliente = "";
        private bool isPrinting = false;
        private Cotizacion? editingQuote;
        private string turno = "Diurno";
        private int horasTurno = 8;
        private decimal salarioBase = 9000.00m;
        private decimal utilidadPorcentaje = 20.00m;
        private decimal supervision = 1000.00m;
        private decimal cargos = 500.00m;
        private decimal seguros = 300.00m;
        private decimal gastosOperativos = 0.00m;
        private int diasFeriados = 15;
        private bool recargoDoble = false;
        private decimal montoAdicionalHoraExtra = 15.00m;

        private List<CatalogoEpp> eppList = new List<CatalogoEpp>();
        private List<CatalogoViatico> viaticosList = new List<CatalogoViatico>();
        private List<CatalogoMaquinaria> machineryList = new List<CatalogoMaquinaria>();
        private List<Cotizacion> savedQuotes = new List<Cotizacion>();

        private CargosSocialesConfig cargosConfig = new CargosSocialesConfig
        {
            InssPatronal = 22.5m,
            Inatec = 2.0m,
            VacacionesDias = 30.0m,
            AguinaldoDias = 30.0m,
            IndemnizacionDias = 41.5m,
            DomingosDias = 52.0m,
            FeriadosDias = 15.0m,
            ColchonSubsidio = 3.0m
        };

        private decimal vacasAlMes = 2.5m;
        private decimal aguinaldoAlMes = 2.5m;
        private decimal indemnizacionAlMes = 3.4583m;

        private decimal CalculatedPrestacionesFactor => cargosConfig != null 
            ? ((cargosConfig.InssPatronal + cargosConfig.Inatec + cargosConfig.ColchonSubsidio) / 100m) 
              + ((cargosConfig.DomingosDias + diasFeriados + (vacasAlMes * 12m) + (aguinaldoAlMes * 12m) + (indemnizacionAlMes * 12m)) / 360m)
            : 0.743m;

        public class GroupedCotizacion
        {
            public string ClienteNombre { get; set; } = "";
            public string Periodo { get; set; } = "";
            public int Year { get; set; }
            public int Month { get; set; }
            public decimal CostoTotal => Quotes.Sum(q => q.CostoTotal);
            public decimal TarifaSugerida => Quotes.Sum(q => q.TarifaSugerida);
            public List<Cotizacion> Quotes { get; set; } = new List<Cotizacion>();
            public HashSet<Cotizacion> SelectedQuotesForPrint { get; set; } = new HashSet<Cotizacion>();
            public bool ShowDetails { get; set; }
        }

        private List<GroupedCotizacion> groupedQuotes = new List<GroupedCotizacion>();
        private HashSet<Cotizacion> selectedQuotesForPrint = new HashSet<Cotizacion>();
        private List<Cotizacion> temporaryShifts = new List<Cotizacion>();

        private IEnumerable<Cotizacion> CurrentClientQuotes => savedQuotes
            .Where(q => q.ClienteNombre != null && q.ClienteNombre.Trim().Equals(cliente.Trim(), StringComparison.OrdinalIgnoreCase));

        private HashSet<int> selectedEpps = new HashSet<int>();
        private HashSet<int> selectedViaticos = new HashSet<int>();

        // Diccionarios para guardar montos personalizados por cotización
        private Dictionary<int, decimal> customViaticosCosts = new Dictionary<int, decimal>();
        private Dictionary<int, decimal> customEppsCosts = new Dictionary<int, decimal>();

        // Campos de edición de catálogo
        private CatalogoEpp editingEpp = new CatalogoEpp();
        private CatalogoViatico editingViatico = new CatalogoViatico();
        private CatalogoMaquinaria editingMachinery = new CatalogoMaquinaria();

        // Fórmulas dinámicas
        private decimal CalculatedPrestaciones => salarioBase * CalculatedPrestacionesFactor;
        
        private decimal CalculatedViaticos => viaticosList
            .Where(x => selectedViaticos.Contains(x.Id))
            .Sum(x => customViaticosCosts.TryGetValue(x.Id, out var cost) ? cost : x.CostoMensual);

        private decimal CalculatedEpp => eppList
            .Where(x => selectedEpps.Contains(x.Id))
            .Sum(x => (customEppsCosts.TryGetValue(x.Id, out var cost) ? cost : x.CostoMensual) * 1.05m); // 5% de reposición

        private decimal CalculatedCmt => salarioBase 
            + CalculatedPrestaciones 
            + CalculatedViaticos 
            + CalculatedEpp 
            + supervision 
            + cargos 
            + seguros 
            + gastosOperativos;

        private decimal CalculatedUtility => (salarioBase / (1.0m - (utilidadPorcentaje / 100.0m))) - salarioBase;

        private decimal CalculatedTarifaSugerida => CalculatedCmt + CalculatedUtility;

        private decimal CalculatedFcr => salarioBase > 0 ? (CalculatedCmt / salarioBase) : 0m;

        private decimal CalculatedFfr => salarioBase > 0 ? (CalculatedTarifaSugerida / salarioBase) : 0m;

        private decimal GetDiasTrabajoMes(int hours)
        {
            if (hours <= 0) return 26m;
            if (hours <= 8) return 26m;
            if (hours == 12) return 16m;
            if (hours == 24) return 8m;
            return Math.Round((48m / (decimal)hours) * 4m, 1);
        }

        private decimal CalculatedDiasTrabajoMes => GetDiasTrabajoMes(horasTurno);

        private decimal CalculatedTarifaHora => CalculatedTarifaSugerida / (CalculatedDiasTrabajoMes * (decimal)(horasTurno > 0 ? horasTurno : 8));

        private decimal CalculatedTarifaDia => CalculatedTarifaHora * (decimal)horasTurno;

        private decimal CalculatedTarifaExtra => recargoDoble 
            ? (CalculatedTarifaHora * 2m)
            : (((salarioBase / 30m) / (decimal)(horasTurno > 0 ? horasTurno : 8)) + CalculatedTarifaHora + montoAdicionalHoraExtra);

        private decimal CalculatedTarifaFeriado => CalculatedTarifaExtra;

        private decimal CalculatedTarifaDomingo => CalculatedTarifaExtra;

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

                if (cats.CargosSociales != null)
                {
                    cargosConfig = cats.CargosSociales;
                    vacasAlMes = cargosConfig.VacacionesDias / 12m;
                    aguinaldoAlMes = cargosConfig.AguinaldoDias / 12m;
                    indemnizacionAlMes = cargosConfig.IndemnizacionDias / 12m;
                }

                // Seleccionar EPPs y viáticos por defecto basados en el excel si es primera inicialización
                if (!selectedViaticos.Any() && !selectedEpps.Any() && customViaticosCosts.Count == 0 && customEppsCosts.Count == 0)
                {
                    foreach (var item in viaticosList.Where(x => x.Nombre == "Transporte" || x.Nombre == "Alimentación"))
                    {
                        selectedViaticos.Add(item.Id);
                    }
                    foreach (var item in eppList.Where(x => x.Nombre == "Guantes" || x.Nombre == "Camisas" || x.Nombre == "Botas de seguridad" || x.Nombre == "Gafas" || x.Nombre == "Cinturón" || x.Nombre == "Protectores térmicos" || x.Nombre == "Arneses de seguridad"))
                    {
                        selectedEpps.Add(item.Id);
                    }
                }
            }
        }

        private async Task LoadQuotes()
        {
            var res = await Mediator.Send(new GetCotizacionesQuery());
            if (res.Model != null)
            {
                savedQuotes = res.Model.Where(x => x.TipoCosteo == "Personal").ToList();

                // Agrupar
                groupedQuotes = savedQuotes
                    .GroupBy(x => new { 
                        Cliente = x.ClienteNombre, 
                        x.FechaCreacion.Year,
                        x.FechaCreacion.Month
                    })
                    .Select(g => new GroupedCotizacion
                    {
                        ClienteNombre = g.Key.Cliente,
                        Periodo = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-NI")),
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Quotes = g.OrderByDescending(x => x.FechaCreacion).ToList()
                    })
                    .OrderByDescending(g => g.Year)
                    .ThenByDescending(g => g.Month)
                    .ToList();

                foreach (var g in groupedQuotes)
                {
                    g.SelectedQuotesForPrint = new HashSet<Cotizacion>(g.Quotes);
                }
            }
        }

        private decimal GetViaticoCost(int id)
        {
            if (customViaticosCosts.TryGetValue(id, out var cost))
                return cost;
            var item = viaticosList.FirstOrDefault(x => x.Id == id);
            return item?.CostoMensual ?? 0m;
        }

        private void SetViaticoCost(int id, decimal cost)
        {
            customViaticosCosts[id] = cost;
        }

        private decimal GetEppCost(int id)
        {
            if (customEppsCosts.TryGetValue(id, out var cost))
                return cost;
            var item = eppList.FirstOrDefault(x => x.Id == id);
            return item?.CostoMensual ?? 0m;
        }

        private void SetEppCost(int id, decimal cost)
        {
            customEppsCosts[id] = cost;
        }

        private void AddViaticoRow(int? id)
        {
            if (id.HasValue)
            {
                selectedViaticos.Add(id.Value);
                if (!customViaticosCosts.ContainsKey(id.Value))
                {
                    var item = viaticosList.FirstOrDefault(x => x.Id == id.Value);
                    if (item != null)
                    {
                        customViaticosCosts[id.Value] = item.CostoMensual;
                    }
                }
            }
            selectedViaticoToAdd = null;
        }

        private void RemoveViaticoRow(int id)
        {
            selectedViaticos.Remove(id);
        }

        private void AddEppRow(int? id)
        {
            if (id.HasValue)
            {
                selectedEpps.Add(id.Value);
                if (!customEppsCosts.ContainsKey(id.Value))
                {
                    var item = eppList.FirstOrDefault(x => x.Id == id.Value);
                    if (item != null)
                    {
                        customEppsCosts[id.Value] = item.CostoMensual;
                    }
                }
            }
            selectedEppToAdd = null;
        }

        private void RemoveEppRow(int id)
        {
            selectedEpps.Remove(id);
        }

        private void LoadQuoteToForm(Cotizacion quote)
        {
            editingQuote = quote; // Guardar referencia para edición
            cliente = quote.ClienteNombre;
            utilidadPorcentaje = quote.UtilidadPorcentaje;
            
            if (quote.PersonalDetalle != null)
            {
                turno = quote.PersonalDetalle.Turno;
                horasTurno = quote.PersonalDetalle.HorasTurno;
                salarioBase = quote.PersonalDetalle.SalarioBase;
                supervision = quote.PersonalDetalle.Supervision;
                cargos = quote.PersonalDetalle.Cargos;
                seguros = quote.PersonalDetalle.Seguros;
                gastosOperativos = quote.PersonalDetalle.GastosOperativos;
                diasFeriados = quote.PersonalDetalle.DiasFeriados;
                recargoDoble = quote.PersonalDetalle.RecargoDoble;
                montoAdicionalHoraExtra = quote.PersonalDetalle.MontoAdicionalHoraExtra;

                selectedEpps.Clear();
                customEppsCosts.Clear();
                if (quote.EppDetalles != null)
                {
                    foreach (var epp in quote.EppDetalles)
                    {
                        if (epp.EppId.HasValue)
                        {
                            selectedEpps.Add(epp.EppId.Value);
                            customEppsCosts[epp.EppId.Value] = epp.CostoMensual;
                        }
                    }
                }

                selectedViaticos.Clear();
                customViaticosCosts.Clear();
                if (quote.ViaticoDetalles != null)
                {
                    foreach (var viatico in quote.ViaticoDetalles)
                    {
                        if (viatico.ViaticoId.HasValue)
                        {
                            selectedViaticos.Add(viatico.ViaticoId.Value);
                            customViaticosCosts[viatico.ViaticoId.Value] = viatico.CostoMensual;
                        }
                    }
                }
            }
            Snackbar.Add($"Modo edición activado para el turno '{turno}' de '{cliente}'. Modifique los valores y presione Guardar Cambios.", Severity.Info);
        }

        private async Task UpdateExistingQuote()
        {
            if (editingQuote == null) return;

            if (string.IsNullOrWhiteSpace(cliente))
            {
                Snackbar.Add("El nombre del cliente es obligatorio.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(turno))
            {
                Snackbar.Add("El nombre del turno es obligatorio.", Severity.Warning);
                return;
            }

            // Actualizar propiedades de la cotización existente
            editingQuote.ClienteNombre = cliente;
            editingQuote.UtilidadPorcentaje = utilidadPorcentaje;
            editingQuote.CostoTotal = CalculatedCmt;
            editingQuote.TarifaSugerida = CalculatedTarifaSugerida;
            editingQuote.TarifaAcordada = CalculatedTarifaSugerida;

            if (editingQuote.PersonalDetalle != null)
            {
                editingQuote.PersonalDetalle.SalarioBase = salarioBase;
                editingQuote.PersonalDetalle.PrestacionesFactor = CalculatedPrestacionesFactor;
                editingQuote.PersonalDetalle.ViaticosTotales = CalculatedViaticos;
                editingQuote.PersonalDetalle.EppTotales = CalculatedEpp;
                editingQuote.PersonalDetalle.Supervision = supervision;
                editingQuote.PersonalDetalle.Cargos = cargos;
                editingQuote.PersonalDetalle.Seguros = seguros;
                editingQuote.PersonalDetalle.GastosOperativos = gastosOperativos;
                editingQuote.PersonalDetalle.Turno = turno;
                editingQuote.PersonalDetalle.HorasTurno = horasTurno;
                editingQuote.PersonalDetalle.TarifaExtra = CalculatedTarifaExtra;
                editingQuote.PersonalDetalle.TarifaFeriado = CalculatedTarifaFeriado;
                editingQuote.PersonalDetalle.TarifaDomingo = CalculatedTarifaDomingo;
                editingQuote.PersonalDetalle.DiasFeriados = diasFeriados;
                editingQuote.PersonalDetalle.RecargoDoble = recargoDoble;
                editingQuote.PersonalDetalle.MontoAdicionalHoraExtra = montoAdicionalHoraExtra;
            }

            // Rellenar listas de EPP
            editingQuote.EppDetalles.Clear();
            foreach (var id in selectedEpps)
            {
                var item = eppList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    var finalCost = customEppsCosts.TryGetValue(id, out var c) ? c : item.CostoMensual;
                    editingQuote.EppDetalles.Add(new CotizacionEppDetalle
                    {
                        EppId = id,
                        Nombre = item.Nombre,
                        Cantidad = item.Cantidad,
                        MesesProrrateo = item.MesesProrrateo,
                        CostoUnitario = item.CostoUnitario,
                        CostoMensual = finalCost
                    });
                }
            }

            // Rellenar lista de Viáticos
            editingQuote.ViaticoDetalles.Clear();
            foreach (var id in selectedViaticos)
            {
                var item = viaticosList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    var finalCost = customViaticosCosts.TryGetValue(id, out var c) ? c : item.CostoMensual;
                    editingQuote.ViaticoDetalles.Add(new CotizacionViaticoDetalle
                    {
                        ViaticoId = id,
                        Nombre = item.Nombre,
                        CostoMensual = finalCost
                    });
                }
            }

            var res = await Mediator.Send(new SaveCotizacionCommand(editingQuote));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add($"Turno '{turno}' de '{cliente}' actualizado con éxito.", Severity.Success);
                editingQuote = null;
                
                // Resetear campos
                turno = "";
                horasTurno = 8;
                salarioBase = 9000.00m;
                recargoDoble = false;
                selectedEpps.Clear();
                selectedViaticos.Clear();
                customEppsCosts.Clear();
                customViaticosCosts.Clear();
                
                await LoadQuotes();
            }
            else
            {
                Snackbar.Add("Error al actualizar la cotización: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private void CancelEditing()
        {
            editingQuote = null;
            turno = "";
            horasTurno = 8;
            salarioBase = 9000.00m;
            selectedEpps.Clear();
            selectedViaticos.Clear();
            customEppsCosts.Clear();
            customViaticosCosts.Clear();
            Snackbar.Add("Edición cancelada.", Severity.Info);
        }

        private void AddTurnoToSession()
        {
            if (string.IsNullOrWhiteSpace(cliente))
            {
                Snackbar.Add("El nombre del cliente es obligatorio.", Severity.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(turno))
            {
                Snackbar.Add("El nombre del turno es obligatorio.", Severity.Warning);
                return;
            }

            if (temporaryShifts.Any(s => s.PersonalDetalle?.Turno?.Trim().Equals(turno.Trim(), StringComparison.OrdinalIgnoreCase) == true))
            {
                Snackbar.Add($"El turno '{turno}' ya ha sido agregado a esta cotización.", Severity.Warning);
                return;
            }

            var detail = new CotizacionPersonalDetalle
            {
                SalarioBase = salarioBase,
                PrestacionesFactor = CalculatedPrestacionesFactor,
                ViaticosTotales = CalculatedViaticos,
                EppTotales = CalculatedEpp,
                Supervision = supervision,
                Cargos = cargos,
                Seguros = seguros,
                GastosOperativos = gastosOperativos,
                Turno = turno,
                HorasTurno = horasTurno,
                TarifaExtra = CalculatedTarifaExtra,
                TarifaFeriado = CalculatedTarifaFeriado,
                TarifaDomingo = CalculatedTarifaDomingo,
                DiasFeriados = diasFeriados,
                RecargoDoble = recargoDoble,
                MontoAdicionalHoraExtra = montoAdicionalHoraExtra
            };

            var quoteId = Guid.NewGuid();
            var numCot = temporaryShifts.Any() 
                ? (temporaryShifts.First().NumeroCotizacion ?? $"COT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Split('-')[0].ToUpper()}")
                : $"COT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Split('-')[0].ToUpper()}";

            var quote = new Cotizacion
            {
                Id = quoteId,
                ClienteNombre = cliente,
                TipoCosteo = "Personal",
                UtilidadPorcentaje = utilidadPorcentaje,
                CostoTotal = CalculatedCmt,
                TarifaSugerida = CalculatedTarifaSugerida,
                TarifaAcordada = CalculatedTarifaSugerida, // por defecto igual a sugerida
                PersonalDetalle = detail,
                Estado = "Borrador",
                NumeroCotizacion = numCot
            };

            // Rellenar listas de detalle histórico
            foreach (var id in selectedEpps)
            {
                var item = eppList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    var finalCost = customEppsCosts.TryGetValue(id, out var c) ? c : item.CostoMensual;
                    quote.EppDetalles.Add(new CotizacionEppDetalle
                    {
                        EppId = id,
                        Nombre = item.Nombre,
                        Cantidad = item.Cantidad,
                        MesesProrrateo = item.MesesProrrateo,
                        CostoUnitario = item.CostoUnitario,
                        CostoMensual = finalCost
                    });
                }
            }

            foreach (var id in selectedViaticos)
            {
                var item = viaticosList.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    var finalCost = customViaticosCosts.TryGetValue(id, out var c) ? c : item.CostoMensual;
                    quote.ViaticoDetalles.Add(new CotizacionViaticoDetalle
                    {
                        ViaticoId = id,
                        Nombre = item.Nombre,
                        CostoMensual = finalCost
                    });
                }
            }

            temporaryShifts.Add(quote);
            Snackbar.Add($"Turno '{turno}' agregado localmente a la cotización.", Severity.Info);

            // Resetear campos del turno, pero MANTENER el cliente
            turno = "";
            horasTurno = 8;
            salarioBase = 9000.00m;
            recargoDoble = false;
            selectedEpps.Clear();
            selectedViaticos.Clear();
            customEppsCosts.Clear();
            customViaticosCosts.Clear();
        }

        private void RemoveTemporaryShift(Cotizacion quote)
        {
            temporaryShifts.Remove(quote);
            Snackbar.Add("Turno removido de la sesión.", Severity.Warning);
        }

        private async Task SaveAllTemporaryShifts()
        {
            if (!temporaryShifts.Any()) return;

            bool allSaved = true;
            string errorMsg = "";

            foreach (var quote in temporaryShifts)
            {
                var res = await Mediator.Send(new SaveCotizacionCommand(quote));
                if (res.Respuesta.ExisteError)
                {
                    allSaved = false;
                    errorMsg = res.Respuesta.MensajeError;
                    break;
                }
            }

            if (allSaved)
            {
                Snackbar.Add("Cotización guardada exitosamente con todos sus turnos.", Severity.Success);
                temporaryShifts.Clear();
                cliente = ""; // Limpiar cliente ahora que ya se guardó todo
                await LoadQuotes();
            }
            else
            {
                Snackbar.Add("Error al guardar algunos turnos: " + errorMsg, Severity.Error);
            }
        }

        private async Task UpdateTarifaAcordada(Cotizacion quote, decimal val)
        {
            quote.TarifaAcordada = val;
            var res = await Mediator.Send(new SaveCotizacionCommand(quote));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add($"Tarifa acordada del turno '{quote.PersonalDetalle?.Turno}' actualizada a C$ {val:N2}.", Severity.Success);
                await LoadQuotes();
            }
            else
            {
                Snackbar.Add("Error al actualizar tarifa acordada: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task PrintConsolidatedCotizacion()
        {
            if (!selectedQuotesForPrint.Any()) return;

            isPrinting = true;
            StateHasChanged();
            try
            {
                var ids = selectedQuotesForPrint.Select(q => q.Id).ToList();
                var res = await Mediator.Send(new PrintCotizacionPdfQuery(ids));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Cotizacion_{cliente.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Cotización generada y descargada.", Severity.Success);
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

        private async Task PrintConsolidatedCotizacionDesglose()
        {
            if (!selectedQuotesForPrint.Any()) return;

            isPrinting = true;
            StateHasChanged();
            try
            {
                var ids = selectedQuotesForPrint.Select(q => q.Id).ToList();
                var res = await Mediator.Send(new PrintCotizacionDesglosePdfQuery(ids));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Cotizacion_Desglose_{cliente.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
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

        private async Task PrintGroup(GroupedCotizacion group)
        {
            if (group.SelectedQuotesForPrint == null || !group.SelectedQuotesForPrint.Any())
            {
                Snackbar.Add("Debe seleccionar al menos un turno para imprimir.", Severity.Warning);
                return;
            }
            var ids = group.SelectedQuotesForPrint.Select(q => q.Id).ToList();
            isPrinting = true;
            StateHasChanged();
            try
            {
                var res = await Mediator.Send(new PrintCotizacionPdfQuery(ids));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Cotizacion_{group.ClienteNombre.Replace(" ", "_")}_{group.Periodo.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Cotización consolidada generada y descargada.", Severity.Success);
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

        private async Task PrintGroupDesglose(GroupedCotizacion group)
        {
            if (group.SelectedQuotesForPrint == null || !group.SelectedQuotesForPrint.Any())
            {
                Snackbar.Add("Debe seleccionar al menos un turno para imprimir.", Severity.Warning);
                return;
            }
            var ids = group.SelectedQuotesForPrint.Select(q => q.Id).ToList();
            isPrinting = true;
            StateHasChanged();
            try
            {
                var res = await Mediator.Send(new PrintCotizacionDesglosePdfQuery(ids));
                if (!res.Respuesta.ExisteError && res.Model != null && !string.IsNullOrEmpty(res.Model.File))
                {
                    var fileBytes = Convert.FromBase64String(res.Model.File);
                    await JS.InvokeVoidAsync("saveAsFile", $"Cotizacion_Desglose_{group.ClienteNombre.Replace(" ", "_")}_{group.Periodo.Replace(" ", "_")}.pdf", fileBytes, "application/pdf");
                    Snackbar.Add("Desglose consolidado generado y descargado.", Severity.Success);
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

        private async Task DeleteGroup(GroupedCotizacion group)
        {
            bool confirm = await JS.InvokeAsync<bool>("confirm", $"¿Está seguro de eliminar toda la cotización de {group.ClienteNombre} para el período {group.Periodo}? Esto borrará sus {group.Quotes.Count} turnos.");
            if (!confirm) return;

            bool allDeleted = true;
            foreach (var q in group.Quotes)
            {
                var res = await Mediator.Send(new DeleteCotizacionCommand(q.Id));
                if (res.Respuesta.ExisteError)
                {
                    allDeleted = false;
                }
            }

            if (allDeleted)
            {
                Snackbar.Add("Cotización eliminada por completo.", Severity.Info);
            }
            else
            {
                Snackbar.Add("Se eliminaron algunos turnos, pero otros fallaron.", Severity.Warning);
            }
            await LoadQuotes();
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

        // Métodos de Gestión de Catálogos
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

        private void EditViatico(CatalogoViatico item)
        {
            editingViatico = new CatalogoViatico
            {
                Id = item.Id,
                Nombre = item.Nombre,
                CostoMensual = item.CostoMensual
            };
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
                Snackbar.Add("Equipo guardado exitosamente.", Severity.Success);
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
                Snackbar.Add("Equipo eliminado.", Severity.Info);
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al eliminar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }

        private async Task SaveCargosSociales()
        {
            cargosConfig.VacacionesDias = vacasAlMes * 12m;
            cargosConfig.AguinaldoDias = aguinaldoAlMes * 12m;
            cargosConfig.IndemnizacionDias = indemnizacionAlMes * 12m;

            var res = await Mediator.Send(new SaveCargosSocialesConfigCommand(cargosConfig));
            if (!res.Respuesta.ExisteError)
            {
                Snackbar.Add("Configuración de cargos sociales guardada con éxito.", Severity.Success);
                await LoadCatalogs();
            }
            else
            {
                Snackbar.Add("Error al guardar: " + res.Respuesta.MensajeError, Severity.Error);
            }
        }
    }
}
