using System.Collections.Generic;
using Modelo.Entidades.Entradas.Odoo;

namespace Modelo.Entidades.Operaciones
{
    public class CatalogoActividadesTabletDto
    {
        public List<ClienteTabItemDto> Clientes { get; set; } = new();
        public List<ClienteActividadConfigDto> Configuraciones { get; set; } = new();
        public List<OdooVariantDto> Variantes { get; set; } = new();
    }

    public class ClienteTabItemDto
    {
        public int OperacionId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public int ServiciosCount { get; set; }
        public List<string> Areas { get; set; } = new();
    }
}
