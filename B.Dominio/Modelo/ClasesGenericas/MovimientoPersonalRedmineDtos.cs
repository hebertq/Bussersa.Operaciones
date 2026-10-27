using System;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class CrearMovimientosPersonalRequestDto
    {
        public int OperacionId { get; set; }
        public string ClienteRedmine { get; set; } = string.Empty; // e.g. "4-WalMart"
        public string ModoGeneracion { get; set; } = "Ambos";      // "Ambos", "SoloMovimientos", "SoloViaticos"
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
}
