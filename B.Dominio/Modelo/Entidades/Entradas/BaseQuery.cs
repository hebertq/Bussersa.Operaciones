using Modelo.ClasesGenericas;
using Newtonsoft.Json;

namespace Modelo.Entidades.Entradas
{
    public abstract class BaseQuery<T1, T2> where T1 : class, new() where T2 : class, new()
    {
        [JsonIgnore]
        public string query { set; get; } = string.Empty;
        [JsonIgnore]
        public Bancos pais { set; get; } = Bancos.BLNI;
        [JsonIgnore]
        public TipoServicio servicio { set; get; } = TipoServicio.CMN;
        public string casenumber { set; get; } = "000000000";
        public T1 Request { get; set; } = new T1();
        public T2 Response { get; set; } = new T2();
    }
}
