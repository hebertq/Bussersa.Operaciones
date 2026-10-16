using System;

namespace Modelo.Entidades.Operaciones
{
    public class ReglaCobroClienteDto
    {
        public int Id { get; set; }
        public int OperacionId { get; set; }
        public string NombreRegla { get; set; } = string.Empty;
        public string CargoPatron { get; set; } = string.Empty;
        public string TipoRegla { get; set; } = "MONTACARGA_12H";
        public decimal HorasNormales { get; set; } = 8.00m;
        public decimal HorasExtrasBase { get; set; } = 0.00m;
        public decimal HorasBaseTurno { get; set; } = 8.00m;
        public bool Activa { get; set; } = true;
        public int Orden { get; set; } = 1;
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }
}
