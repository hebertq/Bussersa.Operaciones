using System;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class ClienteConfigHorasExtrasDto
    {
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public bool PermiteAutorizacionHorasExtras { get; set; }
        public bool RequiereFirmaSupervisor { get; set; } = true;
        public bool RequiereFirmaGerente { get; set; } = false;
        public string? UsuarioActualizacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public class AutorizacionHorasExtrasDto
    {
        public int Id { get; set; }
        public string TokenRecibo { get; set; } = string.Empty;
        public int? MovimientoDiarioId { get; set; }
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public int? EmployeeId { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string HoraEntradaJornada { get; set; } = string.Empty;
        public string HoraSalidaJornada { get; set; } = string.Empty;
        public string HoraInicioExtra { get; set; } = "17:00";
        public string HoraFinExtra { get; set; } = "20:00";
        public decimal HorasExtrasAutorizadas { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string SupervisorNombre { get; set; } = string.Empty;
        public string SupervisorCargo { get; set; } = "Supervisor del Cliente";
        public string? FirmaSupervisorBase64 { get; set; }
        public DateTime? FechaFirmaSupervisor { get; set; }
        public string GerenteNombre { get; set; } = string.Empty;
        public string GerenteCargo { get; set; } = "Gerente del Cliente";
        public string? FirmaGerenteBase64 { get; set; }
        public DateTime? FechaFirmaGerente { get; set; }
        public int? RedmineAutorizadoPorId { get; set; }
        public string AutorizadoPorResumen { get; set; } = string.Empty;
        public int? TicketRedmineId { get; set; }
        public int EstadoRedmineId { get; set; } = 1;
        public string Estado { get; set; } = "PENDIENTE";
        public bool SincronizadoOdoo { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime? FechaCreacion { get; set; }
        public List<ColaboradorAutorizacionItemDto> ColaboradoresGrupo { get; set; } = new();

        public bool TieneFirmaSupervisor => !string.IsNullOrWhiteSpace(FirmaSupervisorBase64);
        public bool TieneFirmaGerente => !string.IsNullOrWhiteSpace(FirmaGerenteBase64);
    }

    public class ColaboradorAutorizacionItemDto
    {
        public int? MovimientoDiarioId { get; set; }
        public int? EmployeeId { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string HoraEntradaJornada { get; set; } = string.Empty;
        public string HoraSalidaJornada { get; set; } = string.Empty;
        public string HoraInicioExtra { get; set; } = "17:00";
        public string HoraFinExtra { get; set; } = "20:00";
        public decimal HorasExtrasAutorizadas { get; set; }
        public string? MotivoIndividual { get; set; }
    }

    public class CrearAutorizacionHorasExtrasRequestDto
    {
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty; // yyyy-MM-dd
        public string HoraInicioExtra { get; set; } = "17:00";
        public string HoraFinExtra { get; set; } = "20:00";
        public decimal HorasExtrasAutorizadas { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string SupervisorNombre { get; set; } = string.Empty;
        public string SupervisorCargo { get; set; } = "Supervisor del Cliente";
        public string? FirmaSupervisorBase64 { get; set; }
        public string GerenteNombre { get; set; } = string.Empty;
        public string GerenteCargo { get; set; } = "Gerente del Cliente";
        public string? FirmaGerenteBase64 { get; set; }
        public int? RedmineAutorizadoPorId { get; set; }
        public string UsuarioRegistro { get; set; } = string.Empty;
        public string BaseUrlPortal { get; set; } = "https://operaciones.bussersa.com";
        public List<ColaboradorAutorizacionItemDto> Colaboradores { get; set; } = new();
    }

    public class ResultadoAutorizacionItemDto
    {
        public int Id { get; set; }
        public string TokenRecibo { get; set; } = string.Empty;
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public bool Exitoso { get; set; }
        public int? TicketRedmineId { get; set; }
        public string? Mensaje { get; set; }
    }

    public class ResultadoCreacionAutorizacionHeDto
    {
        public int TotalProcesados { get; set; }
        public int Exitosos { get; set; }
        public int Fallidos { get; set; }
        public List<ResultadoAutorizacionItemDto> Detalles { get; set; } = new();
    }

    public class AgregarFirmaAutorizacionRequestDto
    {
        public int AutorizacionId { get; set; }
        public string TipoFirma { get; set; } = "GERENTE"; // "SUPERVISOR" o "GERENTE"
        public string NombreFirmante { get; set; } = string.Empty;
        public string CargoFirmante { get; set; } = string.Empty;
        public string FirmaBase64 { get; set; } = string.Empty;
        public string UsuarioActualizacion { get; set; } = string.Empty;
    }

    public class SincronizarAutorizacionesHeResultDto
    {
        public int TotalEvaluadas { get; set; }
        public int NuevasResueltasOdoo { get; set; }
        public int NuevasRechazadas { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
