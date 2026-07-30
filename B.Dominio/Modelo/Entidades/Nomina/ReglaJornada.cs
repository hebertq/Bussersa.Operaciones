using System;
using System.Collections.Generic;

namespace Modelo.Entidades.Nomina
{
    public class ReglaJornada
    {
        public int Id { get; set; }
        public string NombreRegla { get; set; }
        public string Descripcion { get; set; }
        public string TipoRegla { get; set; } = "CODIGO_TRABAJO"; // 'CODIGO_TRABAJO' o 'OPERATIVA_CUSTOM'
        public string ModoCalculoLaboral { get; set; } = "SEMANAL_48H";
        public string ModoCalculoFacturable { get; set; } = "SEMANAL_48H";
        public decimal HorasSemanalesLimite { get; set; } = 48.00m;
        public decimal HorasQuincenalesLimite { get; set; } = 96.00m;
        public decimal FactorDiasTurno12h { get; set; } = 1.50m;
        public bool DomingoEsDiaOrdinario { get; set; } = true;
        public bool PermiteMarcaUnica8h { get; set; } = false;
        public bool AplicaViaticoAlimentos { get; set; } = false;
        public string HoraEntradaMaxViatico { get; set; } = "06:00";
        public string HoraSalidaMinViatico { get; set; } = "19:00";
        public decimal MontoViaticoAlimentos { get; set; } = 0.00m;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public class AsignacionJornadaCliente
    {
        public int Id { get; set; }
        public int? ReglaId { get; set; }
        public string ReglaNombre { get; set; }
        public string TipoRegla { get; set; }
        public int? OperacionId { get; set; }
        public string OperacionNombre { get; set; }
        public int? EstructuraSalarialId { get; set; }
        public string EstructuraSalarialNombre { get; set; }
        public string CorteNominaTipo { get; set; } = "CORTE_8_23";
        public string ReglasActivasJson { get; set; } = "[]";
        public List<string> ReglasActivas { get; set; } = new List<string>();
        public string Observaciones { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
