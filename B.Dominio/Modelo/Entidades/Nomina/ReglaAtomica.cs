using System;
using System.Collections.Generic;

namespace Modelo.Entidades.Nomina
{
    public class ReglaAtomica
    {
        public string CodigoRegla { get; set; }
        public string NombreRegla { get; set; }
        public string Categoria { get; set; } // 'JORNADA', 'DESCANSO', 'FERIADO', 'EXTRAS', 'TURNO_ESPECIAL', 'MARCADAS', 'BENEFICIOS'
        public string ArticuloLey { get; set; } // 'Art. 51 CT', 'Art. 52 CT', etc.
        public string Descripcion { get; set; }
        public string ParametrosJson { get; set; } = "{}";
        public string EvaluadorFunc { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
