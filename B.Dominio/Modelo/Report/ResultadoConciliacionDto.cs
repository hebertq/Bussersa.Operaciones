using System;
using System.Collections.Generic;
using System.Linq;

namespace Modelo.Report
{
    public class ResultadoConciliacionDto
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // --- RESUMEN GENERAL BUSSERSA ---
        public int TotalTurnosBussersa { get; set; }
        public decimal TotalHorasOrdBussersa { get; set; }
        public decimal TotalHorasExtBussersa { get; set; }
        public decimal TotalHorasBussersa { get; set; }

        // --- RESUMEN GENERAL CLIENTE (SINSA) ---
        public int TotalTurnosClienteTotal { get; set; }
        public decimal TotalHorasOrdClienteTotal { get; set; }
        public decimal TotalHorasExtClienteTotal { get; set; }
        public decimal TotalHorasClienteTotal { get; set; }

        // --- DIFERENCIAS GLOBALES (BUSSERSA - CLIENTE) ---
        public int DiferenciaTurnosGlobal => TotalTurnosBussersa - TotalTurnosClienteTotal;
        public decimal DiferenciaHorasOrdGlobal => Math.Round(TotalHorasOrdBussersa - TotalHorasOrdClienteTotal, 2);
        public decimal DiferenciaHorasExtGlobal => Math.Round(TotalHorasExtBussersa - TotalHorasExtClienteTotal, 2);
        public decimal DiferenciaTotalHorasGlobal => Math.Round(TotalHorasBussersa - TotalHorasClienteTotal, 2);

        // --- CONSOLIDADO POR ÁREA Y PUESTO (EJE PRINCIPAL DE CONCILIACIÓN) ---
        public List<ConsolidadoAreaPuestoDto> ConsolidadoAreaPuesto { get; set; } = new();
        public List<ConsolidadoSoloAreaDto> ConsolidadoSoloArea { get; set; } = new();

        public int AreasConDiferencia => ConsolidadoAreaPuesto.Count(x => !x.Coincide);
        public int AreasCoincidentes => ConsolidadoAreaPuesto.Count(x => x.Coincide);
        public bool ConsolidadoCoincideTotalmente => ConsolidadoAreaPuesto.Any() && ConsolidadoAreaPuesto.All(x => x.Coincide);

        // --- DETALLE POR COLABORADOR ---
        public List<TurnoFaltanteClienteDto> TurnosFaltantes { get; set; } = new();
        public List<RegistroSoloClienteDto> RegistrosSoloCliente { get; set; } = new();
        public List<DiferenciaDiaComunDto> DiferenciasDiasComunes { get; set; } = new();
        public List<ResumenColaboradorFaltanteDto> ResumenColaboradores { get; set; } = new();

        public bool TieneDiferencias => DiferenciasDiasComunes.Any() || TurnosFaltantes.Any() || RegistrosSoloCliente.Any() || AreasConDiferencia > 0;
    }

    public class ConsolidadoAreaPuestoDto
    {
        public int No { get; set; }
        public string Area { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;

        // Turnos
        public int TurnosBussersa { get; set; }
        public int TurnosCliente { get; set; }
        public int DiferenciaTurnos => TurnosBussersa - TurnosCliente;

        // Horas Ordinarias
        public decimal HorasOrdBussersa { get; set; }
        public decimal HorasOrdCliente { get; set; }
        public decimal DiferenciaHorasOrd => Math.Round(HorasOrdBussersa - HorasOrdCliente, 2);

        // Horas Extras
        public decimal HorasExtBussersa { get; set; }
        public decimal HorasExtCliente { get; set; }
        public decimal DiferenciaHorasExt => Math.Round(HorasExtBussersa - HorasExtCliente, 2);

        // Total Horas
        public decimal TotalHorasBussersa => Math.Round(HorasOrdBussersa + HorasExtBussersa, 2);
        public decimal TotalHorasCliente => Math.Round(HorasOrdCliente + HorasExtCliente, 2);
        public decimal DiferenciaTotalHoras => Math.Round(TotalHorasBussersa - TotalHorasCliente, 2);

        // Estado
        public bool Coincide => Math.Abs(DiferenciaTotalHoras) < 0.01m &&
                                Math.Abs(DiferenciaHorasOrd) < 0.01m &&
                                Math.Abs(DiferenciaHorasExt) < 0.01m;
    }

    public class ConsolidadoSoloAreaDto
    {
        public int No { get; set; }
        public string Area { get; set; } = string.Empty;

        // Turnos
        public int TurnosBussersa { get; set; }
        public int TurnosCliente { get; set; }
        public int DiferenciaTurnos => TurnosBussersa - TurnosCliente;

        // Horas Ordinarias
        public decimal HorasOrdBussersa { get; set; }
        public decimal HorasOrdCliente { get; set; }
        public decimal DiferenciaHorasOrd => Math.Round(HorasOrdBussersa - HorasOrdCliente, 2);

        // Horas Extras
        public decimal HorasExtBussersa { get; set; }
        public decimal HorasExtCliente { get; set; }
        public decimal DiferenciaHorasExt => Math.Round(HorasExtBussersa - HorasExtCliente, 2);

        // Total Horas
        public decimal TotalHorasBussersa => Math.Round(HorasOrdBussersa + HorasExtBussersa, 2);
        public decimal TotalHorasCliente => Math.Round(HorasOrdCliente + HorasExtCliente, 2);
        public decimal DiferenciaTotalHoras => Math.Round(TotalHorasBussersa - TotalHorasCliente, 2);

        // Estado
        public bool Coincide => Math.Abs(DiferenciaTotalHoras) < 0.01m &&
                                Math.Abs(DiferenciaHorasOrd) < 0.01m &&
                                Math.Abs(DiferenciaHorasExt) < 0.01m;
    }

    public class TurnoFaltanteClienteDto
    {
        public int No { get; set; }
        public int IdEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public decimal HorasOrdCobrables { get; set; }
        public decimal HorasExtCobrables { get; set; }
        public decimal TotalHorasCobrables => HorasOrdCobrables + HorasExtCobrables;
        public string CategoriaFaltante { get; set; } = string.Empty;
        public string MotivoJustificacion { get; set; } = string.Empty;
    }

    public class RegistroSoloClienteDto
    {
        public int No { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Bodega { get; set; } = string.Empty;
        public decimal HorasOrd { get; set; }
        public decimal HorasExt { get; set; }
        public decimal TotalHoras => HorasOrd + HorasExt;
        public string Diagnostico { get; set; } = string.Empty;
    }

    public class DiferenciaDiaComunDto
    {
        public int No { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string AreaBussersa { get; set; } = string.Empty;
        public string BodegaCliente { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public decimal HorasOrdBussersa { get; set; }
        public decimal HorasExtBussersa { get; set; }
        public decimal HorasBussersa => HorasOrdBussersa + HorasExtBussersa;
        public decimal HorasOrdCliente { get; set; }
        public decimal HorasExtCliente { get; set; }
        public decimal HorasCliente => HorasOrdCliente + HorasExtCliente;
        public decimal DiferenciaHorasOrd => Math.Round(HorasOrdBussersa - HorasOrdCliente, 2);
        public decimal DiferenciaHorasExt => Math.Round(HorasExtBussersa - HorasExtCliente, 2);
        public decimal DiferenciaHoras => Math.Round(HorasBussersa - HorasCliente, 2);
        public string DetalleDiferencia { get; set; } = string.Empty;
    }

    public class ResumenColaboradorFaltanteDto
    {
        public int No { get; set; }
        public int IdEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string AreaPrincipal { get; set; } = string.Empty;
        public string CargoPrincipal { get; set; } = string.Empty;
        public int DiasOmitidos { get; set; }
        public decimal HorasOrdOmitidas { get; set; }
        public decimal HorasExtOmitidas { get; set; }
        public decimal TotalHorasOmitidas => HorasOrdOmitidas + HorasExtOmitidas;
        public string CategoriaFaltante { get; set; } = string.Empty;
        public string CausaRaiz { get; set; } = string.Empty;
        public string FechasOmitidas { get; set; } = string.Empty;
    }
}
