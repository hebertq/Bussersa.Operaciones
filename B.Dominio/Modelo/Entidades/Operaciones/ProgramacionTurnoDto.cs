using System;

namespace Modelo.Entidades.Operaciones
{
    public class ProgramacionTurnoDto
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int EmpleadoId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty; // "Diurno" o "Nocturno"
        public int? SupervisorId { get; set; }
        public int OperacionId { get; set; }
    }
}
