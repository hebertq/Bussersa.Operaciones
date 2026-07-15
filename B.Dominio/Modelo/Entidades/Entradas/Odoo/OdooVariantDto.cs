namespace Modelo.Entidades.Entradas.Odoo
{
    public class OdooVariantDto
    {
        public int id { get; set; }
        public string default_code { get; set; }
        public string nombre { get; set; }
        public decimal precio { get; set; }
        public int template_id { get; set; }
        public string template_name { get; set; } = string.Empty;
    }
}
