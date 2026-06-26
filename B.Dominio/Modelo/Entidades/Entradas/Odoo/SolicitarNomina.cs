using System.Collections.Generic;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class SolicitarNomina
    {
        public string nombre { set; get; } = string.Empty;
        public string cliente { set; get; } = string.Empty;
        public List<diasxpagarperiodo> detalle { set; get; } = new List<diasxpagarperiodo>();
        public typeeinout perido { set; get; } = new typeeinout();
    }
}
