using System.Collections.Generic;
using Modelo.Entidades.Entradas.Odoo;

namespace Modelo.Entidades.Operaciones
{
    public class CatalogoActividadesTabletDto
    {
        public List<ClienteCatalogoDto> Clientes { get; set; } = new();
    }

    public class ClienteCatalogoDto
    {
        public int operacion_id { get; set; }
        public string cliente_nombre { get; set; } = string.Empty;
        public List<string> areas { get; set; } = new();
        public int servicios_count { get; set; }
        public List<ServicioCatalogoDto> servicios { get; set; } = new();
    }

    public class ServicioCatalogoDto
    {
        public int template_id { get; set; }
        public string template_nombre { get; set; } = string.Empty;
        public string area_nombre { get; set; } = string.Empty;
        public bool tiene_turno { get; set; }
        public int variantes_count { get; set; }
        public List<OdooVariantDto> variantes { get; set; } = new();
    }
}
