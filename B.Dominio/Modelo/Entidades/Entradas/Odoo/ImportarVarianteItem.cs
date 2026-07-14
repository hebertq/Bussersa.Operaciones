namespace Modelo.Entidades.Entradas.Odoo
{
    public class ImportarVarianteItem
    {
        public string name { get; set; } = string.Empty;
        public string default_code { get; set; } = string.Empty;
        public decimal lst_price { get; set; }
        public string product_template_variant_value_ids { get; set; } = string.Empty;
    }
}
