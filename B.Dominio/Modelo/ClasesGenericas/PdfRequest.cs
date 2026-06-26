using Modelo.Interfaces;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class PdfRequest<TModel> : IPdfRequest<TModel> where TModel : new()
    {
        public TModel Model { get; set; } = new TModel();
        public Dictionary<string, object> viewData { get; set; } = new Dictionary<string, object>();
        public string vista { get; set; } = string.Empty;
        public string filename { get; set; } = string.Empty;
        public string PathImage { get; set; } = string.Empty;
        public bool Horizontal { get; set; } = false;
    }
}
