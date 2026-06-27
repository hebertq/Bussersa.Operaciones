using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Utilidades.ClasesGenericas;

namespace BsOperaciones.Pages.Operaciones.MarcadasxFacturarxDia
{
    public partial class TablaMarcadasxFacturar : ComponentBase
    {
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected HostService.Interfaces.IOdooService OdooService { get; set; }

        [Parameter] public IList<DiasTrabajadosAreas> PayLoadList { get; set; }
        [Parameter] public bool BotonEnabled { get; set; }
        [Parameter] public string OperacionName { get; set; }
        [Parameter] public int OperacionId { get; set; }
        [Parameter] public EventCallback<bool> OnValidationChanged { get; set; }
        [Parameter] public EventCallback OnRefreshData { get; set; }

        protected string _searchString;
        private bool _ultimoEstadoValidacion = true;

        protected override async Task OnParametersSetAsync()
        {
            if (PayLoadList != null)
            {
                foreach (var item in PayLoadList)
                {
                    item.estado_calculado = ObtenerEstadoRegistro(item).Etiqueta;
                }

                bool tieneProblemas = PayLoadList.Any(x => x.estado_calculado != "OK");
                bool deshabilitar = !PayLoadList.Any() || tieneProblemas;

                if (deshabilitar != _ultimoEstadoValidacion)
                {
                    _ultimoEstadoValidacion = deshabilitar;
                    await OnValidationChanged.InvokeAsync(deshabilitar);
                }
            }
        }

        protected async Task MostrarDetalle(DiasTrabajadosAreas item)
        {
            var parameters = new DialogParameters<DetalleFilaDialog>();
            parameters.Add(nameof(DetalleFilaDialog.Item), item); 
            parameters.Add(nameof(DetalleFilaDialog.operacion), OperacionId);

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, BackdropClick = true };
            var dialog = await DialogService.ShowAsync<DetalleFilaDialog>("Toma de Decisión", parameters, options); 

            var result = await dialog.Result; 

            if (!result.Canceled)
            {
                // Al regresar del diálogo con éxito, disparamos el refresco hacia el padre
                await OnRefreshData.InvokeAsync();

                // También recalculamos el estado local por si acaso
                item.estado_calculado = ObtenerEstadoRegistro(item).Etiqueta;
                StateHasChanged();
            }
        }

        protected class AlertaInfo
        {
            public string Etiqueta { get; set; }
            public string Mensaje { get; set; }
            public MudBlazor.Color Color { get; set; }
            public string Icono { get; set; }
        }

        protected AlertaInfo ObtenerEstadoRegistro(DiasTrabajadosAreas item)
        {
            if (item == null) return new AlertaInfo { Etiqueta = "N/A", Color = Color.Default };

            // 1. DUPLICADO (Usando tu función original)
            if (VerificarDuplicadoMovimiento(item))
                return new AlertaInfo { Etiqueta = "DUPLICADO", Color = Color.Error, Icono = Icons.Material.Filled.CopyAll };

            // 2. CRUCE (Usando tu función original)
            if (VerificarCruceHorarios(item) || !string.IsNullOrEmpty(item.cliente_conflicto))
                return new AlertaInfo { Etiqueta = "CRUCE", Color = Color.Error, Icono = Icons.Material.Filled.EventBusy };

            // 3. INCONSISTENCIAS (S.MOV / S.MARC)
            if (item.tarea == 0 && item.idmarca > 0)
                return new AlertaInfo { Etiqueta = "S. MOV", Color = Color.Warning, Icono = Icons.Material.Filled.RunningWithErrors };

            if (item.tarea == 0 && item.idmarca > 0 && item.es_op_mixta)
                return new AlertaInfo { Etiqueta = "S. MAOP", Color = Color.Warning, Icono = Icons.Material.Filled.RunningWithErrors };

            if (item.tarea > 0 && item.idmarca == 0)
                return new AlertaInfo { Etiqueta = "S. MARC", Color = Color.Warning, Icono = Icons.Material.Filled.Fingerprint };

            // 4. DESBORDE
            if (item.desbordamiento > 0)
                return new AlertaInfo { Etiqueta = "DESBORDE", Color = Color.Warning, Icono = Icons.Material.Filled.HistoryToggleOff };

            // 5. OK / DOBLE TURNO
            // Verificamos si hay otro registro para el mismo empleado el mismo día con marca distinta
            bool esDoble = PayLoadList.Any(x => x != item && x.id == item.id && x.fecha.Date == item.fecha.Date && x.idmarca != item.idmarca);
            if (esDoble) item.doble_turno = true;

            return new AlertaInfo
            {
                Etiqueta = "OK",
                Color = Color.Success,
                Icono = esDoble ? Icons.Material.Filled.Repeat : Icons.Material.Filled.CheckCircle
            };
        }

        protected bool VerificarDuplicadoMovimiento(DiasTrabajadosAreas item)
        {
            // Si no hay horario de movimiento, no puede ser duplicado
            if (string.IsNullOrEmpty(item.entrada_movimiento) || item.entrada_movimiento == "00:00:00")
                return false;

            return PayLoadList.Any(x =>
                   x != item && // No compararse consigo mismo
                   x.id == item.id &&
                   x.fecha.Date == item.fecha.Date &&
                   x.entrada_movimiento == item.entrada_movimiento &&
                   x.salida_movimineto == item.salida_movimineto &&
                   x.idmarca == item.idmarca &&
                   x.idmarca > 0); // Solo marcar duplicado si hay un ID de marca real
        }

        protected bool VerificarCruceHorarios(DiasTrabajadosAreas item)
        {
            // Si no hay horarios definidos, no hay cruce que evaluar
            if (string.IsNullOrEmpty(item.entrada_movimiento) || item.entrada_movimiento == "00:00:00" ||
                string.IsNullOrEmpty(item.salida_movimineto) || item.salida_movimineto == "00:00:00")
                return false;

            return PayLoadList.Any(x =>
                   x != item && // No compararse consigo mismo
                   x.id == item.id &&
                   x.fecha.Date == item.fecha.Date &&
                   !string.IsNullOrEmpty(x.entrada_movimiento) &&
                   x.entrada_movimiento != "00:00:00" &&
                   !string.IsNullOrEmpty(x.salida_movimineto) &&
                   x.salida_movimineto != "00:00:00" &&
                   // Lógica de traslape: (Entrada1 < Salida2) AND (Entrada2 < Salida1)
                   String.Compare(item.entrada_movimiento, x.salida_movimineto) < 0 &&
                   String.Compare(x.entrada_movimiento, item.salida_movimineto) < 0);
        }

        protected bool isloading { get; set; } = false;

        protected async Task DownloadFile()
        {
            if (PayLoadList == null || !PayLoadList.Any()) return;
            isloading = true;
            StateHasChanged();
            await Task.Delay(50);
            try
            {
                var datosExcel = PayLoadList.OrderBy(x => x.fecha).ThenBy(x => x.nombre).Select(x =>
                {
                    var alerta = ObtenerEstadoRegistro(x);
                    return new
                    {
                        Cerrada = x.cierre,
                        ESTADO_ANALISIS = alerta.Etiqueta,
                        DETALLE_ALERTA = alerta.Mensaje,
                        fecha = x.fecha.ToString("yyyy-MM-dd"),
                        x.areamov,
                        x.tipoempleado,
                        x.id,
                        x.nombre,
                        contrato = x.state_contra ? "Activo" : "Cerrado",
                        x.entrada,
                        x.salida,
                        x.horas,
                        x.horasextras,
                        x.tarea,
                        x.entrada_movimiento,
                        x.salida_movimineto,
                        x.comentario,
                    };
                }).ToList();

                var queryMarcas = PrepararMarcacionesManuales(OperacionId == 14).Select(d => new { d.id, d.nombre, d.fecha, d.entrada, d.salida, d.bono, d.almuerzocena }).ToList();

                var request = new MultiSheetExcelRequest
                {
                    Hojas = new List<ExcelRequest>
                    {
                        new ExcelRequest { Hoja = "Revision_Bussersa", Datos = Modelo.Validaciones.Util.ToDictionaryList(datosExcel), IncludeHeader = false },
                        new ExcelRequest { Hoja = $"Agregar marcadas de {OperacionName}", Datos = Modelo.Validaciones.Util.ToDictionaryList(queryMarcas), IncludeHeader = false }
                    }
                };

                var response = await OdooService.GenerateExcel(request);
                if (!response.Respuesta.ExisteError && response.Model != null)
                {
                    string base64File = response.Model.File;
                    await JS.InvokeVoidAsync("downloadFile", "application/xlsx", base64File, $"Analisis_Facturacion_{OperacionName.Replace(" ", "_")}.xlsx");
                    Snackbar.Add("Excel generado con éxito", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Error al generar Excel: " + response.Respuesta.MensajeError, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error en exportación: " + ex.Message, Severity.Error);
            }
            finally
            {
                isloading = false;
            }
        }

        protected Func<DiasTrabajadosAreas, bool> _quickFilter => x => string.IsNullOrWhiteSpace(_searchString) ||
                x.id.ToString().Contains(_searchString) ||
                x.nombre.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                (x.estado_calculado != null && x.estado_calculado.Contains(_searchString, StringComparison.OrdinalIgnoreCase));

        private List<HoraEntrada> PrepararMarcacionesManuales(bool op)
        {
            if (PayLoadList == null) return new();

            // Filtramos solo S.MARC que no estén cerrados
            var pendientes = PayLoadList.Where(x => (x.estado_calculado == "S. MARC" || x.estado_calculado == "S. MAOP") && !x.cierre && x.es_op_mixta == op).ToList();
            var resultado = new List<HoraEntrada>();

            // Agrupamos por ID y Fecha para decidir si es Doble Turno o Consolidado
            var grupos = pendientes.GroupBy(x => new { x.id, x.fecha.Date });

            foreach (var grupo in grupos)
            {
                if (grupo.Any(g => g.doble_turno))
                {
                    // Caso Doble Turno: Generamos registros individuales tal cual el movimiento
                    resultado.AddRange(grupo.Select(item =>
                        MapearAHoraEntrada(item, item.entrada_movimiento, item.salida_movimineto)));
                }
                else
                {
                    // Caso Sencillo: Consolidamos Entrada Min y Salida Max
                    var principal = grupo.First();
                    var minEntrada = grupo.Min(g => g.entrada_movimiento);
                    var maxSalida = grupo.Max(g => g.salida_movimineto);

                    var consolidado = MapearAHoraEntrada(principal, minEntrada, maxSalida);
                    // Sumamos valores económicos del grupo
                    consolidado.bono = (double)grupo.Sum(x => x.bono_movimiento);
                    consolidado.almuerzocena = (double)grupo.Sum(x => x.comida_movimiento);

                    resultado.Add(consolidado);
                }
            }
            return resultado;
        }

        // Método centralizado para evitar repetición de lógica de tiempo y formato
        private HoraEntrada MapearAHoraEntrada(DiasTrabajadosAreas item, string entrada, string salida)
        {
            // Aseguramos formato HH:mm:ss
            string Formatear(string h) =>
                string.IsNullOrEmpty(h) ? "00:00:00" : (h.Count(f => f == ':') == 1 ? $"{h}:00" : h);

            string hEntrada = Formatear(entrada);
            string hSalida = Formatear(salida);
            string fechaSalida = item.fecha.ToString("yyyy-MM-dd");

            // Lógica de Medianoche: Si salida < entrada, sumamos un día
            if (TimeSpan.Parse(hSalida) < TimeSpan.Parse(hEntrada))
            {
                fechaSalida = item.fecha.AddDays(1).ToString("yyyy-MM-dd");
            }

            return new HoraEntrada
            {
                id = item.id,
                nombre = item.nombre,
                idmarca = item.idmarca,
                fecha = item.fecha.ToString("yyyy-MM-dd"),
                entrada = hEntrada,
                salida = hSalida,
                bono = (double)item.bono_movimiento,
                almuerzocena = (double)item.comida_movimiento
            };
        }
    }
}
