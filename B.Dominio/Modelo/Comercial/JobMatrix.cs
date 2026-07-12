using System.Collections.Generic;

namespace Modelo.Comercial
{
    public class JobMatrix
    {
        public int id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public List<int> puestoIds { get; set; } = new();
    }
}
