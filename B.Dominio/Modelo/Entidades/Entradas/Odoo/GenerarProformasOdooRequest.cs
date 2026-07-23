using System.Collections.Generic;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class GenerarProformasOdooRequest
    {
        public int OperacionId { get; set; }
        public List<ProformaGrupoDto> Grupos { get; set; } = new();
    }

    public class ProformaGrupoDto
    {
        public string Cliente { get; set; } = string.Empty;
        public string Agrupacion { get; set; } = string.Empty;
        public List<ProformaItemDto> Items { get; set; } = new();
    }

    public class ProformaItemDto
    {
        public string Area { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Tarifa { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class ProformaMappingResultDto
    {
        public string cliente { get; set; } = string.Empty;
        public string agrupacion { get; set; } = string.Empty;
        public string no_proforma { get; set; } = string.Empty;
    }
}
