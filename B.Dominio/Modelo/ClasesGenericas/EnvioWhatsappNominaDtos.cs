using System;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class LoteNominaWhatsappItemDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int CantidadColillas { get; set; }
        public int? OperacionSugeridaId { get; set; }
        public string OperacionSugeridaNombre { get; set; } = string.Empty;
    }

    public class SupervisorOperacionWhatsappDto
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string OperacionNombre { get; set; } = string.Empty;
    }

    public class ConfigCanalWhatsappDto
    {
        public int Id { get; set; } = 1;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "v20.0";
        public bool UsarPlantillaFallback { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateLang { get; set; } = "es";
        public string MensajeEncabezado { get; set; } = "BUSSERSA — Comprobante de Días Laborados y Colilla de Pago";
        public bool Activo { get; set; } = true;
        public bool TieneCredencialesApi { get; set; }
        public string OrigenCredenciales { get; set; } = "SIN_CONFIGURAR";
        public string? UsuarioActualizacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public class ConsultaEnvioWhatsappRequestDto
    {
        public int? PayslipRunId { get; set; }
        public int? OperacionId { get; set; }
        public DateTime FechaDesde { get; set; } = DateTime.Today.AddDays(-15);
        public DateTime FechaHasta { get; set; } = DateTime.Today;
        public bool IncluirColilla { get; set; } = true;
        public bool IncluirDiasLaborados { get; set; } = true;
    }

    public class LineaColillaWhatsappDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string Categoria { get; set; } = "INGRESO"; // INGRESO | DEDUCCION
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public class DetalleDiaLaboradoWhatsappDto
    {
        public DateTime Fecha { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string HoraEntrada { get; set; } = string.Empty;
        public string HoraSalida { get; set; } = string.Empty;
        public decimal HorasOrdinarias { get; set; }
        public decimal DiasEquivalentes { get; set; }
        public decimal HorasExtras { get; set; }
        public decimal ViaticosBono { get; set; }
        public bool Asistio { get; set; } = true;
        public string EstadoTexto { get; set; } = string.Empty;
        public string Observacion { get; set; } = string.Empty;
    }

    public class ColaboradorEnvioWhatsappDto
    {
        public int EmployeeId { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string TipoContrato { get; set; } = string.Empty;
        public int OperacionId { get; set; }
        public string OperacionNombre { get; set; } = string.Empty;
        public string TelefonoWhatsapp { get; set; } = string.Empty;
        public bool PagoPorTarjeta { get; set; }

        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public bool IncluirColilla { get; set; } = true;
        public bool IncluirDiasLaborados { get; set; } = true;

        // Consolidado de Días Laborados
        public decimal DiasHabilesPeriodo { get; set; }
        public int DiasAsistidosCalendario { get; set; }
        public decimal DiasTrabajadosOrdinarios { get; set; }
        public decimal SeptimoDia { get; set; }
        public decimal DiasFeriados { get; set; }
        public decimal DiasVacacionesDescansadas { get; set; }
        public decimal DiasVacacionesPagadas { get; set; }
        public decimal DiasSubsidio { get; set; }
        public decimal DiasJustificados { get; set; }
        public decimal DiasInjustificados { get; set; }
        public decimal TotalDiasPagar { get; set; }
        public decimal HorasOrdinariasTotales { get; set; }
        public decimal HorasExtrasPeriodo { get; set; }
        public decimal ViaticosBonoPeriodo { get; set; }

        public List<DetalleDiaLaboradoWhatsappDto> DetalleDias { get; set; } = new();

        // Colilla de Pago
        public bool TieneColilla { get; set; }
        public int? PayslipRunId { get; set; }
        public string LoteNombre { get; set; } = string.Empty;
        public int? PayslipId { get; set; }
        public string NumeroRecibo { get; set; } = string.Empty;
        public decimal SalarioMensualContrato { get; set; }
        public decimal DiasNomina { get; set; }
        public decimal SalarioDevengado { get; set; }
        public decimal HorasExtrasColilla { get; set; }
        public decimal MontoHorasExtras { get; set; }
        public decimal ViaticoTransporte { get; set; }
        public decimal ViaticoAlimentacion { get; set; }
        public decimal ViaticoCombustible { get; set; }
        public decimal VacacionesPagadasMonto { get; set; }
        public decimal OtrosIngresos { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal InssLaboral { get; set; }
        public decimal IrRetencion { get; set; }
        public decimal PrestamosPersonales { get; set; }
        public decimal OtrasDeducciones { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal NetoAPagar { get; set; }

        public List<LineaColillaWhatsappDto> LineasIngresos { get; set; } = new();
        public List<LineaColillaWhatsappDto> LineasDeducciones { get; set; } = new();

        // Estado del último envío en el período/lote
        public long? UltimoEnvioId { get; set; }
        public string TokenPublico { get; set; } = string.Empty;
        public string UltimoEstadoEnvio { get; set; } = string.Empty;
        public string UltimoCanalEnvio { get; set; } = string.Empty;
        public DateTime? FechaUltimoEnvio { get; set; }
        public string DetalleUltimoEnvio { get; set; } = string.Empty;
        public string UrlPdfPublico { get; set; } = string.Empty;
        public string UrlWhatsappWeb { get; set; } = string.Empty;
    }

    public class ConsultaEnvioWhatsappResponseDto
    {
        public List<ColaboradorEnvioWhatsappDto> Colaboradores { get; set; } = new();
        public List<SupervisorOperacionWhatsappDto> SupervisoresSugeridos { get; set; } = new();
        public bool TieneApiConfigurada { get; set; }
        public string OrigenCredenciales { get; set; } = "SIN_CONFIGURAR";
    }

    public class EnviarWhatsappIndividualRequestDto
    {
        public string ModoCanal { get; set; } = "META_API"; // META_API | PREPARAR_WA_WEB | SOLO_PDF
        public bool IncluirColilla { get; set; } = true;
        public bool IncluirDiasLaborados { get; set; } = true;
        public string BaseUrlPublica { get; set; } = "https://api.bussersa.com/v1";
        public string UsuarioEnvio { get; set; } = string.Empty;
        public List<ColaboradorEnvioWhatsappDto> Colaboradores { get; set; } = new();
    }

    public class ResultadoItemEnvioWhatsappDto
    {
        public int EmployeeId { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreDestinatario { get; set; } = string.Empty;
        public string TelefonoNormalizado { get; set; } = string.Empty;
        public bool Exito { get; set; }
        public string CanalUsado { get; set; } = string.Empty;
        public string EstadoEnvio { get; set; } = string.Empty;
        public string TokenPublico { get; set; } = string.Empty;
        public string UrlPdfPublico { get; set; } = string.Empty;
        public string UrlWhatsappWeb { get; set; } = string.Empty;
        public string MensajeResumen { get; set; } = string.Empty;
        public string? PdfBase64 { get; set; }
        public string NombreArchivoPdf { get; set; } = string.Empty;
        public string? WhatsappMessageId { get; set; }
        public string DetalleRespuesta { get; set; } = string.Empty;
    }

    public class EnviarConsolidadoSupervisorRequestDto
    {
        public string ModoCanal { get; set; } = "META_API"; // META_API | PREPARAR_WA_WEB | SOLO_PDF
        public string NombreSupervisor { get; set; } = string.Empty;
        public string TelefonoSupervisor { get; set; } = string.Empty;
        public int? OperacionId { get; set; }
        public string OperacionNombre { get; set; } = string.Empty;
        public int? PayslipRunId { get; set; }
        public string LoteNombre { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public bool IncluirResumenColilla { get; set; }
        public string ObservacionAdicional { get; set; } = string.Empty;
        public string BaseUrlPublica { get; set; } = "https://api.bussersa.com/v1";
        public string UsuarioEnvio { get; set; } = string.Empty;
        public List<ColaboradorEnvioWhatsappDto> Colaboradores { get; set; } = new();
    }

    public class ActualizarTelefonoColaboradorRequestDto
    {
        public int EmployeeId { get; set; }
        public int CodigoEmpleado { get; set; }
        public string TelefonoWhatsapp { get; set; } = string.Empty;
        public string UsuarioActualizacion { get; set; } = string.Empty;
    }
}
