using System;

namespace Modelo.Admin
{
    public class AdmonMenu
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public string url { get; set; }
        public string icon { get; set; }
        public int parentid { get; set; }
    }
}
