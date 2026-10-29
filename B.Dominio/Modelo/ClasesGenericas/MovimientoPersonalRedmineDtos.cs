using System;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class CrearMovimientosPersonalRequestDto
    {
        public int OperacionId { get; set; }
        public string ClienteRedmine { get; set; } = string.Empty; // e.g. "4-WalMart"
        public string ModoGeneracion { get; set; } = "Ambos";      // "Ambos", "SoloMovimientos", "SoloViaticos"
        public string? UsuarioRedmine { get; set; }
        public List<MovimientoPersonalItemDto> Movimientos { get; set; } = new();
    }

    public class MovimientoPersonalItemDto
    {
        public int FilaIndex { get; set; }
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty; // yyyy-MM-dd
        public string HoraInicio { get; set; } = string.Empty; // HH:mm
        public string HoraFin { get; set; } = string.Empty; // HH:mm
        public int CantidadEmpleados { get; set; }
        public string IdsEmpleados { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public string Bonos { get; set; } = "0.00";
        public string DobleTurno { get; set; } = "0";
        public string TiempoComida { get; set; } = "0.00";
        public string EsOperacionMixta { get; set; } = "0";
        public string? VtcoTrasp { get; set; } = "0";
        public string? VtcoAlim { get; set; } = "0";
        public string? Cliente { get; set; } = string.Empty;
    }

    public class ResultadoCreacionMovimientosDto
    {
        public int TotalProcesados { get; set; }
        public int Exitosos { get; set; }
        public int Fallidos { get; set; }
        public List<ResultadoMovimientoItemDto> Detalles { get; set; } = new();
    }

    public class ResultadoMovimientoItemDto
    {
        public int FilaIndex { get; set; }
        public bool Exitoso { get; set; }
        public int? RedmineIssueId { get; set; }
        public int? RedmineViaticosIssueId { get; set; }
        public string? Mensaje { get; set; }
        public string? MensajeViaticos { get; set; }
    }

    public class MovimientoPersonalDiarioDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int OperacionId { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public int IdEmpleado { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string HoraEntrada { get; set; } = "06:00";
        public string HoraSalida { get; set; } = "14:00";
        public string TurnoNombre { get; set; } = "Diurno";
        public string? Bnc { get; set; } = string.Empty;
        public decimal VtcoTrasp { get; set; }
        public decimal VtcoAlim { get; set; }
        public string? TiempoComida { get; set; } = "1.00";
        public string? Observaciones { get; set; } = string.Empty;
        public bool Asistio { get; set; } = true;
        public string Estado { get; set; } = "GUARDADO";
        public int? TicketRedmineId { get; set; }
        public int? TicketViaticoId { get; set; }
        public string? UsuarioRegistro { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public class GuardarMovimientosPersonalDiarioRequest
    {
        public int OperacionId { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = "Supervisor";
        public List<MovimientoPersonalDiarioDto> Movimientos { get; set; } = new();
    }

    public class ClonarMovimientosDiaAnteriorRequest
    {
        public int OperacionId { get; set; }
        public DateTime FechaOrigen { get; set; }
        public DateTime FechaDestino { get; set; }
        public string Usuario { get; set; } = "Supervisor";
    }

    public class ActualizarSyncHelpdeskRequest
    {
        public List<int> Ids { get; set; } = new();
        public int? TicketRedmineId { get; set; }
        public int? TicketViaticoId { get; set; }
        public string Estado { get; set; } = "SINCRONIZADO";
    }

    public class SupervisorPlantillaItemDto
    {
        public int Id { get; set; }
        public string UsuarioSupervisor { get; set; } = string.Empty;
        public int IdEmpleado { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string ClienteDefault { get; set; } = string.Empty;
        public string AreaDefault { get; set; } = "Operaciones";
        public string PuestoDefault { get; set; } = "6-Operador de almacen";
        public string TurnoDefault { get; set; } = "Diurno";
        public string HoraEntradaDefault { get; set; } = "06:00";
        public string HoraSalidaDefault { get; set; } = "14:00";
        public string TiempoComidaDefault { get; set; } = "1.00";
        public decimal VtcoTraspDefault { get; set; }
        public decimal VtcoAlimDefault { get; set; }
        public decimal BonoBncDefault { get; set; }
        public string TipoEmpleado { get; set; } = "Permanente"; // "Permanente" o "Prestamo"
        public bool Activo { get; set; } = true;
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public class GuardarSupervisorPlantillaRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public List<SupervisorPlantillaItemDto> Items { get; set; } = new();
    }

    public class PersonalClienteItemDto
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string UsuarioSupervisor { get; set; } = string.Empty;
        public int IdEmpleado { get; set; }
        public int CodigoEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string AreaDefault { get; set; } = "Operaciones";
        public string PuestoDefault { get; set; } = "6-Operador de almacen";
        public string TurnoDefault { get; set; } = "Diurno";
        public string HoraEntradaDefault { get; set; } = "06:00";
        public string HoraSalidaDefault { get; set; } = "14:00";
        public string TiempoComidaDefault { get; set; } = "1.00";
        public decimal VtcoTraspDefault { get; set; }
        public decimal VtcoAlimDefault { get; set; }
        public decimal BonoBncDefault { get; set; }
        public string TipoEmpleado { get; set; } = "Permanente";
        public bool Activo { get; set; } = true;
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public string? UsuarioCreacion { get; set; }
    }

    public class GuardarPersonalClienteRequest
    {
        public string ClienteNombre { get; set; } = string.Empty;
        public string UsuarioSupervisor { get; set; } = string.Empty;
        public List<PersonalClienteItemDto> Items { get; set; } = new();
    }

    public class PrecargarPersonalClienteRequest
    {
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string UsuarioSupervisor { get; set; } = string.Empty;
    }

    public class ClienteAreaDto
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string AreaNombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
        public DateTime? FechaCreacion { get; set; }
        public string? Usuario { get; set; }
    }

    public class GuardarClienteAreaRequest
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string AreaNombre { get; set; } = string.Empty;
        public string Usuario { get; set; } = "Supervisor";
    }
}
