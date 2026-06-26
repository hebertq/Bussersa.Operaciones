using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    // <summary>
    /// Objeto retornado por los procedimientos alamacenados desde la base de datos
    /// </summary>
    public class ErrorMjsBd
    {
        public bool iserror { set; get; }
        public int codeerror { set; get; }
        public string mjserror { set; get; }
        public string errcontext { set; get; }
        public int codid { set; get; }
        public string querydb { set; get; }
    }
}
