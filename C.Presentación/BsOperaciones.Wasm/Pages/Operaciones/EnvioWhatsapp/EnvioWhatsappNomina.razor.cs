using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using MudBlazor;
using Utilidades.Interfaces;

namespace BsOperaciones.Pages.Operaciones.EnvioWhatsapp
{
    public partial class EnvioWhatsappNomina : ComponentBase
    {
        [Inject] protected IOdooService OdooService { get; set; } = default!;
        [Inject] protected IMediator Mediator { get; set; } = default!;
        [Inject] protected IJSRuntime JS { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IUserInfo UserInfo { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;

        protected class FilaDetalleGrillaVm
        {
            public int EmployeeId { get; set; }
            public int CodigoEmpleado { get; set; }
            public string NombreEmpleado { get; set; } = string.Empty;
            public string Cargo { get; set; } = string.Empty;
            public DetalleDiaLaboradoWhatsappDto Dia { get; set; } = new();
        }

        protected List<LoteNominaWhatsappItemDto> LotesNomina { get; set; } = new();
        protected List<Combos> Operaciones { get; set; } = new();
        protected List<ColaboradorEnvioWhatsappDto> Colaboradores { get; set; } = new();

        protected HashSet<int> SeleccionadosIds { get; set; } = new();
        protected HashSet<int> FilasExpandidas { get; set; } = new();
        protected HashSet<int> FilasProcesando { get; set; } = new();

        protected int LoteSeleccionadoId { get; set; } = 0;
        protected int OperacionSeleccionadaId { get; set; } = 0;
        protected DateTime? FechaDesde { get; set; } = DateTime.Today.AddDays(-15);
        protected DateTime? FechaHasta { get; set; } = DateTime.Today;
        protected string ModoContenido { get; set; } = "AMBOS"; // AMBOS | SOLO_DIAS | SOLO_COLILLA
        protected string FormatoAdjunto { get; set; } = "IMAGEN"; // IMAGEN (sin lector PDF) | PDF
        protected string FiltroRapido { get; set; } = "TODOS"; // TODOS | CON_TELEFONO | SIN_TELEFONO | PENDIENTES | ENVIADOS
        protected string TextoBusqueda { get; set; } = string.Empty;

        protected bool Cargando { get; set; }
        protected bool EnviandoMasivo { get; set; }
        protected int ProgresoActual { get; set; }
        protected int ProgresoTotal { get; set; }
        protected string TituloSpinnerProceso { get; set; } = string.Empty;
        protected string DetalleSpinnerProceso { get; set; } = string.Empty;
        protected bool MostrarSpinnerEspera => EnviandoMasivo || FilasProcesando.Count > 0;

        protected bool TieneApiConfigurada { get; set; }
        protected string OrigenCredenciales { get; set; } = "SIN_CONFIGURAR";

        // Modal Consolidado y Detalle de la Grilla
        protected bool MostrarModalResumenGrilla { get; set; }
        protected string VistaResumenGrilla { get; set; } = "CONSOLIDADO"; // CONSOLIDADO | DETALLE
        protected int EmpleadoFiltroDetalleId { get; set; } = 0;
        protected string BusquedaDetalleModal { get; set; } = string.Empty;
        protected bool DescargandoPdfGrilla { get; set; }

        protected bool IncluirColilla => ModoContenido == "AMBOS" || ModoContenido == "SOLO_COLILLA";
        protected bool IncluirDiasLaborados => ModoContenido == "AMBOS" || ModoContenido == "SOLO_DIAS";

        protected List<ColaboradorEnvioWhatsappDto> ColaboradoresFiltrados => Colaboradores
            .Where(CumpleFiltro)
            .ToList();

        protected List<FilaDetalleGrillaVm> FilasDetalleGrilla
        {
            get
            {
                var query = ColaboradoresFiltrados.AsEnumerable();
                if (EmpleadoFiltroDetalleId > 0)
                    query = query.Where(c => c.EmployeeId == EmpleadoFiltroDetalleId);

                if (!string.IsNullOrWhiteSpace(BusquedaDetalleModal))
                {
                    string q = BusquedaDetalleModal.Trim();
                    query = query.Where(c =>
                        c.NombreEmpleado.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || c.CodigoEmpleado.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                        || c.Cargo.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || c.OperacionNombre.Contains(q, StringComparison.OrdinalIgnoreCase));
                }

                return query
                    .SelectMany(c => (c.DetalleDias ?? new List<DetalleDiaLaboradoWhatsappDto>()).Select(d => new FilaDetalleGrillaVm
                    {
                        EmployeeId = c.EmployeeId,
                        CodigoEmpleado = c.CodigoEmpleado,
                        NombreEmpleado = c.NombreEmpleado,
                        Cargo = c.Cargo,
                        Dia = d
                    }))
                    .OrderBy(x => x.NombreEmpleado)
                    .ThenBy(x => x.Dia.Fecha)
                    .ThenBy(x => x.Dia.HoraEntrada)
                    .ToList();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            if (Nav.Uri.Contains("/operaciones/envio-whatsapp-nomina", StringComparison.OrdinalIgnoreCase))
            {
                Nav.NavigateTo("/nomina/envio-whatsapp-nomina", replace: true);
                return;
            }

            Cargando = true;
            try
            {
                var taskOps = Mediator.Send(new GetAllCombosQuery("Operaciones"));
                var taskLotes = OdooService.GetLotesNominaWhatsapp();
                var taskCfg = OdooService.GetConfigCanalWhatsapp();

                await Task.WhenAll(taskOps, taskLotes, taskCfg);

                Operaciones = taskOps.Result?.Model ?? new List<Combos>();
                LotesNomina = (taskLotes.Result?.Model ?? new List<LoteNominaWhatsappItemDto>())
                    .Where(x => !string.Equals(x.Estado, "done", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(x.Estado, "close", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(x.Estado, "cancel", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (taskCfg.Result?.Model != null)
                {
                    TieneApiConfigurada = taskCfg.Result.Model.TieneCredencialesApi;
                    OrigenCredenciales = taskCfg.Result.Model.OrigenCredenciales;
                }

                if (LotesNomina.Count > 0)
                {
                    var primerLote = LotesNomina.First();
                    LoteSeleccionadoId = primerLote.Id;
                    FechaDesde = primerLote.FechaInicio;
                    FechaHasta = primerLote.FechaFin;
                    OperacionSeleccionadaId = 0;
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error al inicializar catálogos: {ex.Message}", Severity.Warning);
            }
            finally
            {
                Cargando = false;
            }

            if (LoteSeleccionadoId > 0)
            {
                await ConsultarDatosAsync();
            }
        }

        protected void OnLoteChanged(int nuevoLoteId)
        {
            LoteSeleccionadoId = nuevoLoteId;
            if (nuevoLoteId > 0)
            {
                var lote = LotesNomina.FirstOrDefault(x => x.Id == nuevoLoteId);
                if (lote != null)
                {
                    FechaDesde = lote.FechaInicio;
                    FechaHasta = lote.FechaFin;
                    OperacionSeleccionadaId = 0;
                }
            }
        }

        protected void OnOperacionChanged(int nuevaOperacionId)
        {
            OperacionSeleccionadaId = nuevaOperacionId;
            if (nuevaOperacionId > 0 && LoteSeleccionadoId > 0)
            {
                var loteActual = LotesNomina.FirstOrDefault(x => x.Id == LoteSeleccionadoId);
                if (loteActual != null && loteActual.OperacionSugeridaId.HasValue && loteActual.OperacionSugeridaId.Value != nuevaOperacionId)
                {
                    var loteOp = LotesNomina.FirstOrDefault(x => x.OperacionSugeridaId == nuevaOperacionId);
                    if (loteOp != null)
                    {
                        LoteSeleccionadoId = loteOp.Id;
                        FechaDesde = loteOp.FechaInicio;
                        FechaHasta = loteOp.FechaFin;
                    }
                    else
                    {
                        LoteSeleccionadoId = 0;
                    }
                }
            }
        }

        protected async Task ConsultarDatosAsync()
        {
            if (LoteSeleccionadoId <= 0 && OperacionSeleccionadaId <= 0)
            {
                Snackbar.Add("Seleccione un Lote de Nómina o una Operación/Cliente para consultar.", Severity.Warning);
                return;
            }

            if (!FechaDesde.HasValue || !FechaHasta.HasValue)
            {
                Snackbar.Add("Indique el rango de fechas del período.", Severity.Warning);
                return;
            }

            Cargando = true;
            SeleccionadosIds.Clear();
            FilasExpandidas.Clear();
            try
            {
                var req = new ConsultaEnvioWhatsappRequestDto
                {
                    PayslipRunId = LoteSeleccionadoId > 0 ? LoteSeleccionadoId : null,
                    OperacionId = OperacionSeleccionadaId > 0 ? OperacionSeleccionadaId : null,
                    FechaDesde = DateTime.SpecifyKind(FechaDesde.Value.Date, DateTimeKind.Unspecified),
                    FechaHasta = DateTime.SpecifyKind(FechaHasta.Value.Date, DateTimeKind.Unspecified),
                    IncluirColilla = IncluirColilla,
                    IncluirDiasLaborados = IncluirDiasLaborados
                };

                var resp = await OdooService.ConsultarEnvioWhatsappNomina(req);
                if (resp.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error en la consulta: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                var data = resp.Model ?? new ConsultaEnvioWhatsappResponseDto();
                Colaboradores = data.Colaboradores ?? new List<ColaboradorEnvioWhatsappDto>();
                TieneApiConfigurada = data.TieneApiConfigurada;
                OrigenCredenciales = data.OrigenCredenciales;

                foreach (var c in Colaboradores.Where(x => TieneTelefonoValido(x.TelefonoWhatsapp)))
                {
                    SeleccionadosIds.Add(c.EmployeeId);
                }

                if (Colaboradores.Count == 0)
                    Snackbar.Add("No se encontraron colaboradores con registros para los filtros seleccionados.", Severity.Info);
                else
                    Snackbar.Add($"Se cargaron {Colaboradores.Count} colaboradores.", Severity.Success);
            }
            finally
            {
                Cargando = false;
            }
        }

        private bool CumpleFiltro(ColaboradorEnvioWhatsappDto x)
        {
            if (FiltroRapido == "CON_TELEFONO" && !TieneTelefonoValido(x.TelefonoWhatsapp))
                return false;
            if (FiltroRapido == "SIN_TELEFONO" && TieneTelefonoValido(x.TelefonoWhatsapp))
                return false;
            if (FiltroRapido == "ENVIADOS" && !EsEnviado(x.UltimoEstadoEnvio))
                return false;
            if (FiltroRapido == "PENDIENTES" && EsEnviado(x.UltimoEstadoEnvio))
                return false;

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                string q = TextoBusqueda.Trim();
                bool match = x.NombreEmpleado.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || x.Cedula.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || x.Cargo.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || x.OperacionNombre.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || x.TelefonoWhatsapp.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || x.CodigoEmpleado.ToString().Contains(q, StringComparison.OrdinalIgnoreCase);
                if (!match) return false;
            }

            return true;
        }

        protected static bool TieneTelefonoValido(string? tel)
        {
            if (string.IsNullOrWhiteSpace(tel)) return false;
            int digits = tel.Count(char.IsDigit);
            return digits >= 8;
        }

        protected static bool EsEnviado(string? estado)
            => string.Equals(estado, "ENVIADO_API", StringComparison.OrdinalIgnoreCase)
            || string.Equals(estado, "ENLACE_WA", StringComparison.OrdinalIgnoreCase);

        protected static string ObtenerDiaCorto(DateTime fecha)
        {
            return fecha.DayOfWeek switch
            {
                DayOfWeek.Monday => "Lun",
                DayOfWeek.Tuesday => "Mar",
                DayOfWeek.Wednesday => "Mié",
                DayOfWeek.Thursday => "Jue",
                DayOfWeek.Friday => "Vie",
                DayOfWeek.Saturday => "Sáb",
                DayOfWeek.Sunday => "Dom",
                _ => string.Empty
            };
        }

        protected void ToggleSeleccionFila(int empId, bool seleccionado)
        {
            if (seleccionado) SeleccionadosIds.Add(empId);
            else SeleccionadosIds.Remove(empId);
        }

        protected void SeleccionarVisiblesConTelefono()
        {
            foreach (var c in ColaboradoresFiltrados.Where(x => TieneTelefonoValido(x.TelefonoWhatsapp)))
                SeleccionadosIds.Add(c.EmployeeId);
        }

        protected void LimpiarSeleccion() => SeleccionadosIds.Clear();

        protected void ToggleExpandirFila(int empId)
        {
            if (!FilasExpandidas.Add(empId))
                FilasExpandidas.Remove(empId);
        }

        // --- Modal Consolidado y Detalle de Datos de la Grilla ---
        protected void AbrirModalResumenGrilla()
        {
            if (ColaboradoresFiltrados.Count == 0)
            {
                Snackbar.Add("Primero consulte los colaboradores para visualizar el consolidado y detalle.", Severity.Warning);
                return;
            }

            VistaResumenGrilla = "CONSOLIDADO";
            EmpleadoFiltroDetalleId = 0;
            BusquedaDetalleModal = string.Empty;
            MostrarModalResumenGrilla = true;
        }

        protected void VerDetalleColaboradorEnModal(int empId)
        {
            EmpleadoFiltroDetalleId = empId;
            VistaResumenGrilla = "DETALLE";
        }

        protected async Task DescargarPdfConsolidadoGrillaAsync()
        {
            if (ColaboradoresFiltrados.Count == 0)
                return;

            DescargandoPdfGrilla = true;
            try
            {
                string opNom = OperacionSeleccionadaId > 0
                    ? (Operaciones.FirstOrDefault(x => x.id == OperacionSeleccionadaId)?.nombre ?? string.Empty)
                    : (ColaboradoresFiltrados.Select(x => x.OperacionNombre).Distinct().Count() == 1
                        ? ColaboradoresFiltrados.First().OperacionNombre
                        : "Operaciones Consolidado");

                string loteNom = LoteSeleccionadoId > 0
                    ? (LotesNomina.FirstOrDefault(x => x.Id == LoteSeleccionadoId)?.Nombre ?? string.Empty)
                    : string.Empty;

                var req = new EnviarConsolidadoSupervisorRequestDto
                {
                    ModoCanal = "SOLO_PDF",
                    NombreSupervisor = string.Empty,
                    TelefonoSupervisor = string.Empty,
                    OperacionId = OperacionSeleccionadaId > 0 ? OperacionSeleccionadaId : null,
                    OperacionNombre = opNom,
                    PayslipRunId = LoteSeleccionadoId > 0 ? LoteSeleccionadoId : null,
                    LoteNombre = loteNom,
                    FechaDesde = FechaDesde ?? DateTime.Today,
                    FechaHasta = FechaHasta ?? DateTime.Today,
                    IncluirResumenColilla = IncluirColilla,
                    ObservacionAdicional = string.Empty,
                    UsuarioEnvio = ObtenerUsuarioActual(),
                    Colaboradores = ColaboradoresFiltrados
                };

                var resp = await OdooService.EnviarWhatsappConsolidadoSupervisor(req);
                if (resp.Respuesta.ExisteError || resp.Model == null || string.IsNullOrWhiteSpace(resp.Model.PdfBase64))
                {
                    Snackbar.Add($"Error al generar PDF consolidado: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                await JS.InvokeVoidAsync("downloadFile", "application/pdf", resp.Model.PdfBase64, resp.Model.NombreArchivoPdf);
                Snackbar.Add("PDF de Consolidado y Detalle de la grilla descargado.", Severity.Success);
            }
            finally
            {
                DescargandoPdfGrilla = false;
            }
        }

        protected async Task GuardarTelefonoFilaAsync(ColaboradorEnvioWhatsappDto item)
        {
            FilasProcesando.Add(item.EmployeeId);
            try
            {
                var resp = await OdooService.ActualizarTelefonoColaboradorWhatsapp(new ActualizarTelefonoColaboradorRequestDto
                {
                    EmployeeId = item.EmployeeId,
                    CodigoEmpleado = item.CodigoEmpleado,
                    TelefonoWhatsapp = item.TelefonoWhatsapp?.Trim() ?? string.Empty,
                    UsuarioActualizacion = ObtenerUsuarioActual()
                });

                if (resp.Respuesta.ExisteError)
                {
                    Snackbar.Add($"No se pudo guardar el teléfono: {resp.Respuesta.MensajeError}", Severity.Error);
                }
                else
                {
                    if (TieneTelefonoValido(item.TelefonoWhatsapp))
                        SeleccionadosIds.Add(item.EmployeeId);
                    Snackbar.Add($"Teléfono actualizado para {item.NombreEmpleado}.", Severity.Success);
                }
            }
            finally
            {
                FilasProcesando.Remove(item.EmployeeId);
            }
        }

        protected async Task DescargarPdfIndividualAsync(ColaboradorEnvioWhatsappDto item)
        {
            FilasProcesando.Add(item.EmployeeId);
            TituloSpinnerProceso = "Generando documento PDF oficial...";
            DetalleSpinnerProceso = item.NombreEmpleado;
            StateHasChanged();
            await Task.Yield();
            try
            {
                var resp = await OdooService.EnviarWhatsappIndividualNomina(new EnviarWhatsappIndividualRequestDto
                {
                    ModoCanal = "SOLO_PDF",
                    IncluirColilla = IncluirColilla,
                    IncluirDiasLaborados = IncluirDiasLaborados,
                    UsuarioEnvio = ObtenerUsuarioActual(),
                    Colaboradores = new List<ColaboradorEnvioWhatsappDto> { item }
                });

                if (resp.Respuesta.ExisteError || resp.Model == null || resp.Model.Count == 0)
                {
                    Snackbar.Add($"Error al generar PDF: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                var r = resp.Model.First();
                AplicarResultadoAColaborador(item, r);
                if (!string.IsNullOrWhiteSpace(r.PdfBase64))
                {
                    await JS.InvokeVoidAsync("downloadFile", "application/pdf", r.PdfBase64, r.NombreArchivoPdf);
                }
            }
            finally
            {
                FilasProcesando.Remove(item.EmployeeId);
            }
        }

        protected async Task DescargarImagenIndividualAsync(ColaboradorEnvioWhatsappDto item)
        {
            FilasProcesando.Add(item.EmployeeId);
            TituloSpinnerProceso = "Generando imagen PNG del comprobante...";
            DetalleSpinnerProceso = item.NombreEmpleado;
            StateHasChanged();
            await Task.Yield();
            try
            {
                var resp = await OdooService.EnviarWhatsappIndividualNomina(new EnviarWhatsappIndividualRequestDto
                {
                    ModoCanal = "SOLO_IMAGEN",
                    FormatoAdjunto = "IMAGEN",
                    IncluirColilla = IncluirColilla,
                    IncluirDiasLaborados = IncluirDiasLaborados,
                    UsuarioEnvio = ObtenerUsuarioActual(),
                    Colaboradores = new List<ColaboradorEnvioWhatsappDto> { item }
                });

                if (resp.Respuesta.ExisteError || resp.Model == null || resp.Model.Count == 0)
                {
                    Snackbar.Add($"Error al generar imagen: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                var r = resp.Model.First();
                AplicarResultadoAColaborador(item, r);
                if (!string.IsNullOrWhiteSpace(r.ImagenPngBase64))
                {
                    await JS.InvokeVoidAsync("downloadFile", "image/png", r.ImagenPngBase64, r.NombreArchivoImagen);
                }
                else if (!string.IsNullOrWhiteSpace(r.DetalleRespuesta))
                {
                    Snackbar.Add(r.DetalleRespuesta, Severity.Warning);
                }
            }
            finally
            {
                FilasProcesando.Remove(item.EmployeeId);
            }
        }

        protected async Task AbrirWhatsappDirectoFilaAsync(ColaboradorEnvioWhatsappDto item)
        {
            if (!TieneTelefonoValido(item.TelefonoWhatsapp))
            {
                Snackbar.Add("Ingrese primero un número de teléfono válido (8 dígitos o con código de país) en la fila.", Severity.Warning);
                return;
            }

            FilasProcesando.Add(item.EmployeeId);
            TituloSpinnerProceso = "Preparando resumen para WhatsApp...";
            DetalleSpinnerProceso = $"{item.NombreEmpleado} ({item.TelefonoWhatsapp})";
            StateHasChanged();
            await Task.Yield();
            try
            {
                var resp = await OdooService.EnviarWhatsappIndividualNomina(new EnviarWhatsappIndividualRequestDto
                {
                    ModoCanal = "PREPARAR_WA_WEB",
                    IncluirColilla = IncluirColilla,
                    IncluirDiasLaborados = IncluirDiasLaborados,
                    UsuarioEnvio = ObtenerUsuarioActual(),
                    Colaboradores = new List<ColaboradorEnvioWhatsappDto> { item }
                });

                if (resp.Respuesta.ExisteError || resp.Model == null || resp.Model.Count == 0)
                {
                    Snackbar.Add($"No se pudo preparar el enlace: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                var r = resp.Model.First();
                AplicarResultadoAColaborador(item, r);
                if (!string.IsNullOrWhiteSpace(r.UrlWhatsappWeb))
                {
                    await JS.InvokeVoidAsync("open", r.UrlWhatsappWeb, "_blank");
                    Snackbar.Add($"WhatsApp abierto para {item.NombreEmpleado}.", Severity.Success);
                }
            }
            finally
            {
                FilasProcesando.Remove(item.EmployeeId);
            }
        }

        protected async Task EnviarApiIndividualFilaAsync(ColaboradorEnvioWhatsappDto item)
        {
            if (!TieneTelefonoValido(item.TelefonoWhatsapp))
            {
                Snackbar.Add("Ingrese primero el número de WhatsApp del colaborador.", Severity.Warning);
                return;
            }

            FilasProcesando.Add(item.EmployeeId);
            TituloSpinnerProceso = "Enviando comprobante por WhatsApp API...";
            DetalleSpinnerProceso = $"{item.NombreEmpleado} · Tel: {item.TelefonoWhatsapp}";
            StateHasChanged();
            await Task.Yield();
            try
            {
                var resp = await OdooService.EnviarWhatsappIndividualNomina(new EnviarWhatsappIndividualRequestDto
                {
                    ModoCanal = "META_API",
                    FormatoAdjunto = FormatoAdjunto,
                    IncluirColilla = IncluirColilla,
                    IncluirDiasLaborados = IncluirDiasLaborados,
                    UsuarioEnvio = ObtenerUsuarioActual(),
                    Colaboradores = new List<ColaboradorEnvioWhatsappDto> { item }
                });

                if (resp.Respuesta.ExisteError || resp.Model == null || resp.Model.Count == 0)
                {
                    Snackbar.Add($"Error de envío: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                var r = resp.Model.First();
                AplicarResultadoAColaborador(item, r);
                if (r.Exito)
                    Snackbar.Add($"Enviado por WhatsApp API a {item.NombreEmpleado}.", Severity.Success);
                else
                    Snackbar.Add($"{item.NombreEmpleado}: {r.DetalleRespuesta}", Severity.Warning);
            }
            finally
            {
                FilasProcesando.Remove(item.EmployeeId);
            }
        }

        protected async Task EnviarSeleccionadosMasivoApiAsync()
        {
            var cola = Colaboradores
                .Where(x => SeleccionadosIds.Contains(x.EmployeeId) && TieneTelefonoValido(x.TelefonoWhatsapp))
                .ToList();

            if (cola.Count == 0)
            {
                Snackbar.Add("Seleccione al menos un colaborador con número de WhatsApp válido.", Severity.Warning);
                return;
            }

            EnviandoMasivo = true;
            ProgresoActual = 0;
            ProgresoTotal = cola.Count;
            TituloSpinnerProceso = "Enviando comprobantes por WhatsApp API...";
            DetalleSpinnerProceso = $"Preparando envío de {cola.Count} comprobante(s)...";
            StateHasChanged();
            await Task.Yield();

            int exitosos = 0;
            int fallidos = 0;
            string ultimoMotivoFallo = string.Empty;

            try
            {
                for (int i = 0; i < cola.Count; i++)
                {
                    var actual = cola[i];
                    DetalleSpinnerProceso = $"Enviando {i + 1} de {cola.Count}: {actual.NombreEmpleado} ({actual.TelefonoWhatsapp})";
                    StateHasChanged();
                    await Task.Yield();

                    var resp = await OdooService.EnviarWhatsappIndividualNomina(new EnviarWhatsappIndividualRequestDto
                    {
                        ModoCanal = "META_API",
                        FormatoAdjunto = FormatoAdjunto,
                        IncluirColilla = IncluirColilla,
                        IncluirDiasLaborados = IncluirDiasLaborados,
                        UsuarioEnvio = ObtenerUsuarioActual(),
                        Colaboradores = new List<ColaboradorEnvioWhatsappDto> { actual }
                    });

                    if (!resp.Respuesta.ExisteError && resp.Model != null && resp.Model.Count > 0)
                    {
                        var r = resp.Model.First();
                        AplicarResultadoAColaborador(actual, r);
                        if (r.Exito)
                        {
                            exitosos++;
                        }
                        else
                        {
                            fallidos++;
                            if (string.IsNullOrWhiteSpace(ultimoMotivoFallo) && !string.IsNullOrWhiteSpace(r.DetalleRespuesta))
                                ultimoMotivoFallo = r.DetalleRespuesta;
                        }
                    }
                    else
                    {
                        fallidos++;
                        if (string.IsNullOrWhiteSpace(ultimoMotivoFallo) && !string.IsNullOrWhiteSpace(resp.Respuesta.MensajeError))
                            ultimoMotivoFallo = resp.Respuesta.MensajeError;
                    }

                    ProgresoActual = i + 1;
                    StateHasChanged();
                }

                if (exitosos > 0 && fallidos == 0)
                {
                    Snackbar.Add($"Envío masivo completado: {exitosos} comprobante(s) enviado(s) por WhatsApp API.", Severity.Success);
                }
                else
                {
                    string detTxt = string.IsNullOrWhiteSpace(ultimoMotivoFallo) ? "" : $" Motivo: {ultimoMotivoFallo}";
                    Snackbar.Add($"Proceso finalizado: {exitosos} enviados por API, {fallidos} con observación.{detTxt}", exitosos > 0 ? Severity.Info : Severity.Warning);
                }
            }
            finally
            {
                EnviandoMasivo = false;
            }
        }

        private static void AplicarResultadoAColaborador(ColaboradorEnvioWhatsappDto item, ResultadoItemEnvioWhatsappDto r)
        {
            item.TokenPublico = r.TokenPublico;
            item.UrlPdfPublico = r.UrlPdfPublico;
            item.UrlWhatsappWeb = r.UrlWhatsappWeb;

            // Si solo fue descarga local de vista previa (PDF_PREVIEW / GENERADO), no pisar el estado real de envío (ej. ENVIADO_API)
            bool esSoloDescargaLocal = string.Equals(r.CanalUsado, "PDF_PREVIEW", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(r.EstadoEnvio, "GENERADO", StringComparison.OrdinalIgnoreCase);
            if (esSoloDescargaLocal && !string.IsNullOrWhiteSpace(item.UltimoEstadoEnvio))
                return;

            item.UltimoCanalEnvio = r.CanalUsado;
            item.UltimoEstadoEnvio = r.EstadoEnvio;
            item.FechaUltimoEnvio = DateTime.Now;
            item.DetalleUltimoEnvio = r.DetalleRespuesta;
        }

        private string ObtenerUsuarioActual()
        {
            return UserInfo?.User?.username
                ?? UserInfo?.User?.email
                ?? "Operaciones";
        }
    }
}
