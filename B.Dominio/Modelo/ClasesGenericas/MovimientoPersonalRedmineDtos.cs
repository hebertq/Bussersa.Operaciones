using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class CrearMovimientosPersonalRequestDto
    {
        public int OperacionId { get; set; }
        public string ClienteRedmine { get; set; } = string.Empty; // e.g. "4-WalMart"
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
        public string? Mensaje { get; set; }
    }
}
