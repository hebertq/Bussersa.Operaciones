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

        protected List<LoteNominaWhatsappItemDto> LotesNomina { get; set; } = new();
        protected List<Combos> Operaciones { get; set; } = new();
        protected List<ColaboradorEnvioWhatsappDto> Colaboradores { get; set; } = new();
        protected List<SupervisorOperacionWhatsappDto> SupervisoresSugeridos { get; set; } = new();

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

        protected bool TieneApiConfigurada { get; set; }
        protected string OrigenCredenciales { get; set; } = "SIN_CONFIGURAR";

        // Modal Supervisor
        protected bool MostrarModalSupervisor { get; set; }
        protected bool ProcesandoSupervisor { get; set; }
        protected string SupervisorNombre { get; set; } = string.Empty;
        protected string SupervisorTelefono { get; set; } = string.Empty;
        protected bool SupervisorIncluirNeto { get; set; } = false;
        protected string SupervisorObservacion { get; set; } = string.Empty;
        protected ResultadoItemEnvioWhatsappDto? UltimoResultadoSupervisor { get; set; }

        // Modal Configuración Canal WhatsApp
        protected bool MostrarModalConfig { get; set; }
        protected bool GuardandoConfig { get; set; }
        protected ConfigCanalWhatsappDto ConfigActual { get; set; } = new();

        protected bool IncluirColilla => ModoContenido == "AMBOS" || ModoContenido == "SOLO_COLILLA";
        protected bool IncluirDiasLaborados => ModoContenido == "AMBOS" || ModoContenido == "SOLO_DIAS";

        protected List<ColaboradorEnvioWhatsappDto> ColaboradoresFiltrados => Colaboradores
            .Where(CumpleFiltro)
            .ToList();

        protected override async Task OnInitializedAsync()
        {
            Cargando = true;
            try
            {
                var taskOps = Mediator.Send(new GetAllCombosQuery("Operaciones"));
                var taskLotes = OdooService.GetLotesNominaWhatsapp();
                var taskCfg = OdooService.GetConfigCanalWhatsapp();

                await Task.WhenAll(taskOps, taskLotes, taskCfg);

                Operaciones = taskOps.Result?.Model ?? new List<Combos>();
                LotesNomina = taskLotes.Result?.Model ?? new List<LoteNominaWhatsappItemDto>();

                if (taskCfg.Result?.Model != null)
                {
                    ConfigActual = taskCfg.Result.Model;
                    TieneApiConfigurada = ConfigActual.TieneCredencialesApi;
                    OrigenCredenciales = ConfigActual.OrigenCredenciales;
                }

                if (LotesNomina.Count > 0)
                {
                    var primerLote = LotesNomina.First();
                    LoteSeleccionadoId = primerLote.Id;
                    FechaDesde = primerLote.FechaInicio;
                    FechaHasta = primerLote.FechaFin;
                    OperacionSeleccionadaId = primerLote.OperacionSugeridaId ?? 0;
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
                    if (lote.OperacionSugeridaId.HasValue && lote.OperacionSugeridaId.Value > 0)
                        OperacionSeleccionadaId = lote.OperacionSugeridaId.Value;
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
                    FechaDesde = FechaDesde.Value.Date,
                    FechaHasta = FechaHasta.Value.Date,
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
                SupervisoresSugeridos = data.SupervisoresSugeridos ?? new List<SupervisorOperacionWhatsappDto>();
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
                    Snackbar.Add($"WhatsApp abierto para {item.NombreEmpleado} con enlace PDF oficial.", Severity.Success);
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
            int exitosos = 0;
            int fallidos = 0;

            try
            {
                const int tamanoBloque = 3;
                for (int i = 0; i < cola.Count; i += tamanoBloque)
                {
                    var bloque = cola.Skip(i).Take(tamanoBloque).ToList();
                    var resp = await OdooService.EnviarWhatsappIndividualNomina(new EnviarWhatsappIndividualRequestDto
                    {
                        ModoCanal = "META_API",
                        FormatoAdjunto = FormatoAdjunto,
                        IncluirColilla = IncluirColilla,
                        IncluirDiasLaborados = IncluirDiasLaborados,
                        UsuarioEnvio = ObtenerUsuarioActual(),
                        Colaboradores = bloque
                    });

                    if (!resp.Respuesta.ExisteError && resp.Model != null)
                    {
                        foreach (var r in resp.Model)
                        {
                            var target = bloque.FirstOrDefault(b => b.EmployeeId == r.EmployeeId);
                            if (target != null)
                                AplicarResultadoAColaborador(target, r);
                            if (r.Exito) exitosos++;
                            else fallidos++;
                        }
                    }
                    else
                    {
                        fallidos += bloque.Count;
                    }

                    ProgresoActual = Math.Min(cola.Count, i + bloque.Count);
                    StateHasChanged();
                }

                if (exitosos > 0 && fallidos == 0)
                    Snackbar.Add($"Envío masivo completado: {exitosos} comprobantes enviados por WhatsApp API.", Severity.Success);
                else
                    Snackbar.Add($"Proceso finalizado: {exitosos} enviados por API, {fallidos} pendientes/con observación (enlaces 1-Clic listos).", exitosos > 0 ? Severity.Info : Severity.Warning);
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
            item.UltimoCanalEnvio = r.CanalUsado;
            item.UltimoEstadoEnvio = r.EstadoEnvio;
            item.FechaUltimoEnvio = DateTime.Now;
            item.DetalleUltimoEnvio = r.DetalleRespuesta;
        }

        // --- Modal Consolidado a Supervisor ---
        protected void AbrirModalSupervisor()
        {
            if (ColaboradoresFiltrados.Count == 0)
            {
                Snackbar.Add("Primero consulte los colaboradores del período.", Severity.Warning);
                return;
            }

            UltimoResultadoSupervisor = null;
            if (string.IsNullOrWhiteSpace(SupervisorNombre) && SupervisoresSugeridos.Count > 0)
            {
                var sup = SupervisoresSugeridos.First();
                SupervisorNombre = sup.Nombre;
                SupervisorTelefono = sup.Telefono;
            }
            MostrarModalSupervisor = true;
        }

        protected void SeleccionarSupervisorSugerido(SupervisorOperacionWhatsappDto sup)
        {
            SupervisorNombre = sup.Nombre;
            if (!string.IsNullOrWhiteSpace(sup.Telefono))
                SupervisorTelefono = sup.Telefono;
        }

        protected async Task EjecutarAccionSupervisorAsync(string modoCanal)
        {
            if (modoCanal != "SOLO_PDF" && !TieneTelefonoValido(SupervisorTelefono))
            {
                Snackbar.Add("Ingrese el número de WhatsApp del supervisor.", Severity.Warning);
                return;
            }

            ProcesandoSupervisor = true;
            try
            {
                string opNom = OperacionSeleccionadaId > 0
                    ? (Operaciones.FirstOrDefault(x => x.id == OperacionSeleccionadaId)?.nombre ?? string.Empty)
                    : (ColaboradoresFiltrados.Select(x => x.OperacionNombre).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "Operaciones");

                string loteNom = LoteSeleccionadoId > 0
                    ? (LotesNomina.FirstOrDefault(x => x.Id == LoteSeleccionadoId)?.Nombre ?? string.Empty)
                    : string.Empty;

                var req = new EnviarConsolidadoSupervisorRequestDto
                {
                    ModoCanal = modoCanal,
                    NombreSupervisor = SupervisorNombre.Trim(),
                    TelefonoSupervisor = SupervisorTelefono.Trim(),
                    OperacionId = OperacionSeleccionadaId > 0 ? OperacionSeleccionadaId : null,
                    OperacionNombre = opNom,
                    PayslipRunId = LoteSeleccionadoId > 0 ? LoteSeleccionadoId : null,
                    LoteNombre = loteNom,
                    FechaDesde = FechaDesde ?? DateTime.Today,
                    FechaHasta = FechaHasta ?? DateTime.Today,
                    IncluirResumenColilla = SupervisorIncluirNeto,
                    ObservacionAdicional = SupervisorObservacion,
                    UsuarioEnvio = ObtenerUsuarioActual(),
                    Colaboradores = ColaboradoresFiltrados
                };

                var resp = await OdooService.EnviarWhatsappConsolidadoSupervisor(req);
                if (resp.Respuesta.ExisteError || resp.Model == null)
                {
                    Snackbar.Add($"Error al procesar consolidado: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                UltimoResultadoSupervisor = resp.Model;
                if (modoCanal == "SOLO_PDF" && !string.IsNullOrWhiteSpace(resp.Model.PdfBase64))
                {
                    await JS.InvokeVoidAsync("downloadFile", "application/pdf", resp.Model.PdfBase64, resp.Model.NombreArchivoPdf);
                    Snackbar.Add("PDF consolidado descargado.", Severity.Success);
                }
                else if (modoCanal == "PREPARAR_WA_WEB" && !string.IsNullOrWhiteSpace(resp.Model.UrlWhatsappWeb))
                {
                    await JS.InvokeVoidAsync("open", resp.Model.UrlWhatsappWeb, "_blank");
                    Snackbar.Add("WhatsApp abierto con el resumen y enlace del PDF consolidado.", Severity.Success);
                }
                else if (modoCanal == "META_API")
                {
                    if (resp.Model.Exito)
                        Snackbar.Add("Reporte consolidado enviado al supervisor por WhatsApp API.", Severity.Success);
                    else
                        Snackbar.Add(resp.Model.DetalleRespuesta, Severity.Warning);
                }
            }
            finally
            {
                ProcesandoSupervisor = false;
            }
        }

        // --- Modal Configuración Canal WhatsApp ---
        protected async Task AbrirModalConfigAsync()
        {
            var resp = await OdooService.GetConfigCanalWhatsapp();
            if (!resp.Respuesta.ExisteError && resp.Model != null)
                ConfigActual = resp.Model;
            MostrarModalConfig = true;
        }

        protected async Task GuardarConfigAsync()
        {
            GuardandoConfig = true;
            try
            {
                ConfigActual.UsuarioActualizacion = ObtenerUsuarioActual();
                var resp = await OdooService.GuardarConfigCanalWhatsapp(ConfigActual);
                if (resp.Respuesta.ExisteError)
                {
                    Snackbar.Add($"Error al guardar configuración: {resp.Respuesta.MensajeError}", Severity.Error);
                    return;
                }

                var recarga = await OdooService.GetConfigCanalWhatsapp();
                if (recarga.Model != null)
                {
                    ConfigActual = recarga.Model;
                    TieneApiConfigurada = ConfigActual.TieneCredencialesApi;
                    OrigenCredenciales = ConfigActual.OrigenCredenciales;
                }

                MostrarModalConfig = false;
                Snackbar.Add("Configuración del canal WhatsApp actualizada.", Severity.Success);
            }
            finally
            {
                GuardandoConfig = false;
            }
        }

        private string ObtenerUsuarioActual()
        {
            return UserInfo?.User?.username
                ?? UserInfo?.User?.email
                ?? "Operaciones";
        }
    }
}
