namespace Modelo.Entidades.Entradas.Odoo
{
    public class GenerarFormatoRequest
    {
        public string cliente { get; set; } = string.Empty;
        public string area { get; set; } = string.Empty;
        public int templateId { get; set; }
    }
}
