using System;

namespace Modelo.Report
{
    public class ConsolidadoCierre
    {
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public DateTime FechaMin { get; set; }
        public DateTime FechaMax { get; set; }
        public int DiasLaborados { get; set; }
        public decimal TotalHoras { get; set; }
        public decimal TotalExtras { get; set; }
        public decimal TotalHorasReales { get; set; }
        public decimal TotalExtrasReales { get; set; }
        public int TotalColaboradores { get; set; }
    }

    public class ReporteCierreMarcadasDetalle
    {
        public DateTime fecha_asistencia { get; set; }
        public int id_empleado { get; set; }
        public string nombre_empleado { get; set; } = string.Empty;
        public string tipo_empleado { get; set; } = string.Empty;
        public string area_nombre { get; set; } = string.Empty;
        public string? cargo { get; set; }
        public string Turno { get; set; } = string.Empty;
        public string entrada_movimiento { get; set; } = string.Empty;
        public string salida_movimiento { get; set; } = string.Empty;
        public decimal horas_totales { get; set; }
        public decimal horas_extras { get; set; }
        public decimal HorasNormalesCobrables { get; set; }
        public decimal HorasExtrasCobrables { get; set; }
        public string ReglaAplicada { get; set; } = string.Empty;
        public DateTime fecha_registro { get; set; }
        public DateTime? fecha_actualizacion { get; set; }
    }

    public class ReglasCobroConfig
    {
        public bool AplicarReglas { get; set; } = true;
        public string ModoAgrupacion { get; set; } = "AreaCargoTurno"; // "AreaCargoTurno" o "AreaCargo"

        // Montacargas
        public bool MontacargasActivo { get; set; } = true;
        public string MontacargasKeywords { get; set; } = "montacarga, montacarguista";
        public decimal MontacargasHorasNormales { get; set; } = 8.0m;
        public decimal MontacargasHorasExtras12h { get; set; } = 4.0m;

        // Almacén / Operarios
        public bool AlmacenActivo { get; set; } = true;
        public string AlmacenKeywords { get; set; } = "almacen, almacén, operario, bodega, ayudante";
        public decimal AlmacenHorasNormalesDiurno { get; set; } = 8.0m;
        public decimal AlmacenHorasBaseNocturno { get; set; } = 7.0m;
        public decimal AlmacenHorasNormalesNocturnoCobro { get; set; } = 8.0m;

        // Conductores / Choferes
        public bool ConductoresActivo { get; set; } = true;
        public string ConductoresKeywords { get; set; } = "conductor, chofer, motorista";
        public decimal ConductoresHorasFijas { get; set; } = 8.0m;
    }
}
