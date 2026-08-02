using System;

namespace Modelo.Entidades.Operaciones
{
    public class ProductoAdicionalConfig
    {
        public int Id { get; set; }
        public int? OperacionId { get; set; }
        public string? OperacionNombre { get; set; }
        public string? ServicioCodigo { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        public string NombreProductoAdicional { get; set; } = string.Empty;
        public string? CodigoItem { get; set; }
        public string TipoCalculo { get; set; } = "FIJO"; // 'FIJO' o 'PROPORCIONAL'
        public decimal CantidadFactor { get; set; } = 1.0m;
        public decimal CostoUnitario { get; set; } = 0.0m;
        public bool Activo { get; set; } = true;
        public string? Observaciones { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
