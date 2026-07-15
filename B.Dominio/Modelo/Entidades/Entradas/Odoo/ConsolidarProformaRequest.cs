using System.Collections.Generic;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class ConsolidarProformaRequest
    {
        public List<int> ids { get; set; } = new();
        public string no_proforma { get; set; } = string.Empty;
        public string? no_factura { get; set; }
    }
}
