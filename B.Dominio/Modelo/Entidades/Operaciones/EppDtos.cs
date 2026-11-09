using System;
using System.Collections.Generic;

namespace Modelo.Entidades.Operaciones
{
    public class EppCatalogoItemDto
    {
        public int TemplateId { get; set; }
        public string TemplateNombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int VariantesCount { get; set; }
        public string IconUrl { get; set; } = string.Empty;
        public List<EppVarianteItemDto> Variantes { get; set; } = new();
    }

    public class EppVarianteItemDto
    {
        public int ProductId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateNombre { get; set; } = string.Empty;
        public string DefaultCode { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string VarianteDescripcion { get; set; } = string.Empty;
        public decimal StockDisponible { get; set; }
    }

    public class EppEntregaDto
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string EmpleadoCodigo { get; set; } = string.Empty;
        public int ProductTmplId { get; set; }
        public int ProductId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string VarianteDescripcion { get; set; } = string.Empty;
        public decimal Cantidad { get; set; } = 1;
        public DateTime FechaEntrega { get; set; } = DateTime.Now;
        public string Motivo { get; set; } = "Dotación Inicial";
        public string EntregadoPor { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public string Estado { get; set; } = "ENTREGADO";
        public string FirmaDigital { get; set; } = string.Empty;
        public int? StockPickingId { get; set; }
        public DateTime? CreadoEl { get; set; }
    }
}
