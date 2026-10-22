using System;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class GastoOperativoDto
    {
        public int Id { get; set; }
        public int RedmineIssueId { get; set; }
        public int OperacionId { get; set; }
        public string? Cliente { get; set; }
        public string? AreaCliente { get; set; }
        public string? Asunto { get; set; }
        public string? Descripcion { get; set; }
        public decimal Monto { get; set; }
        public DateTime? FechaGasto { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public int Periodo { get; set; }
        public string? AutorizadoPor { get; set; }
        public string? AsignadoA { get; set; }
        public string? Estado { get; set; }
        public string? AdjuntosJson { get; set; }
        public DateTime? FechaCreacionRedmine { get; set; }
        public DateTime? FechaActualizacionRedmine { get; set; }
        public DateTime? FechaSincronizacion { get; set; }
    }

    public class SincronizarGastosOperativosResponseDto
    {
        public int TotalEncontrados { get; set; }
        public int TotalInsertados { get; set; }
        public int TotalActualizados { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<GastoOperativoDto> Gastos { get; set; } = new();
    }
}