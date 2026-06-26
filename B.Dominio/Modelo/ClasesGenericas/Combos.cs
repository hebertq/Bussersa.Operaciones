using System;

namespace Modelo.ClasesGenericas
{
    public class Combos : IDisposable
    {
        public int id { set; get; } = 1;
        public string nombre { set; get; } = string.Empty;
        public void Dispose() { }
    }
}
