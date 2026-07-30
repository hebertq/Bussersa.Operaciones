using System;

namespace Modelo.Entidades.Nomina
{
    public class JornadaConfiguracion
    {
        public int Id { get; set; }
        public int? OperacionId { get; set; }
        public string OperacionNombre { get; set; }
        public int? EstructuraSalarialId { get; set; }
        public string EstructuraSalarialNombre { get; set; }
        public string NombreConfiguracion { get; set; }
        public string CorteNominaTipo { get; set; } = "CORTE_8_23";
        public string ModoCalculoLaboral { get; set; } = "SEMANAL_48H";
        public decimal FactorDiasTurno12h { get; set; } = 1.50m;
        public bool DomingoEsDiaOrdinario { get; set; } = true;
        public bool PermiteMarcaUnica8h { get; set; } = false;
        public bool AplicaViaticoAlimentos { get; set; } = false;
        public string HoraEntradaMaxViatico { get; set; } = "06:00";
        public string HoraSalidaMinViatico { get; set; } = "19:00";
        public decimal MontoViaticoAlimentos { get; set; } = 0.00m;
        public decimal HorasSemanalesLimite { get; set; } = 48.00m;
        public decimal HorasQuincenalesLimite { get; set; } = 96.00m;
        public string ModoCalculoFacturable { get; set; } = "SEMANAL_48H";
        public bool Activo { get; set; } = true;
    }
}
