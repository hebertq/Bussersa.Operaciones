using System;

namespace Modelo.Comercial
{
    public class JobFunction
    {
        public int id { get; set; }
        public int matriz_id { get; set; }
        public string area { get; set; } = "";
        public string actividad { get; set; } = "";
        public int orden { get; set; }
    }
}
