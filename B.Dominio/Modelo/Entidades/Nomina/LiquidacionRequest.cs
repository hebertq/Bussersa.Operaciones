using System;
using System.Collections.Generic;

namespace Modelo.Entidades.Nomina
{
    public class LiquidacionRequest
    {
        public DateTime Inicio { get; set; }
        public DateTime Fin { set; get; }
        public string Nombre { set; get; }
        public List<SeveranceDetail> Param { set; get; }
    }
}
