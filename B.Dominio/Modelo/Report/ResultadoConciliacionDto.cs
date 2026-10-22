using System;
using System.Collections.Generic;

namespace Modelo.Report
{
    public class ResultadoConciliacionDto
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // --- CUADRO COMPARATIVO GENERAL ---
        // Bussersa (Cierre Real de Marcadas Cobrables)
        public int TotalTurnosBussersa { get; set; }
        public decimal TotalHorasOrdBussersa { get; set; }
        public decimal TotalHorasExtBussersa { get; set; }
        public decimal TotalHorasBussersa { get; set; }

        // Cliente - Facturado y Pagado
        public int TotalTurnosClientePagado { get; set; }
        public decimal TotalHorasOrdClientePagado { get; set; }
        public decimal TotalHorasExtClientePagado { get; set; }
        public decimal TotalHorasClientePagado { get; set; }

        // Cliente - Facturas en Estado PENDIENTE
        public int TotalTurnosClientePendiente { get; set; }
        public decimal TotalHorasOrdClientePendiente { get; set; }
        public decimal TotalHorasExtClientePendiente { get; set; }
        public decimal TotalHorasClientePendiente { get; set; }

        // Cliente - Total Registrado en su archivo
        public int TotalTurnosClienteTotal { get; set; }
        public decimal TotalHorasOrdClienteTotal { get; set; }
        public decimal TotalHorasExtClienteTotal { get; set; }
        public decimal TotalHorasClienteTotal { get; set; }

        // Diferencia Neta Global (Bussersa - Cliente Total)
        public int DiferenciaTurnosGlobal { get; set; }
        public decimal DiferenciaHorasOrdGlobal { get; set; }
        public decimal DiferenciaHorasExtGlobal { get; set; }
        public decimal DiferenciaTotalHorasGlobal { get; set; }

        // Brecha Directa vs lo Efectivamente Pagado (Bussersa - Cliente Pagado)
        public int BrechaPagadoTurnos { get; set; }
        public decimal BrechaPagadoHorasOrd { get; set; }
        public decimal BrechaPagadoHorasExt { get; set; }
        public decimal BrechaPagadoTotalHoras { get; set; }

        // --- DETALLES ---
        public List<TurnoFaltanteClienteDto> TurnosFaltantes { get; set; } = new();
        public List<RegistroPendienteClienteDto> FacturasPendientes { get; set; } = new();
        public List<DiferenciaDiaComunDto> DiferenciasDiasComunes { get; set; } = new();
        public List<ResumenColaboradorFaltanteDto> ResumenColaboradores { get; set; } = new();
        public List<RegistroSoloClienteDto> RegistrosSoloCliente { get; set; } = new();
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

    public class RegistroPendienteClienteDto
    {
        public int No { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string Bodega { get; set; } = string.Empty;
        public decimal HorasOrd { get; set; }
        public decimal HorasExt { get; set; }
        public decimal TotalHoras => HorasOrd + HorasExt;
        public string NoFactura { get; set; } = string.Empty;
        public string EstadoPago { get; set; } = "PENDIENTE";
        public string Observacion { get; set; } = string.Empty;
    }

    public class DiferenciaDiaComunDto
    {
        public int No { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreEmpleado { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public decimal HorasOrdBussersa { get; set; }
        public decimal HorasExtBussersa { get; set; }
        public decimal HorasBussersa => HorasOrdBussersa + HorasExtBussersa;
        public decimal HorasOrdCliente { get; set; }
        public decimal HorasExtCliente { get; set; }
        public decimal HorasCliente => HorasOrdCliente + HorasExtCliente;
        public decimal DiferenciaHoras => HorasBussersa - HorasCliente;
        public string NoFacturaCliente { get; set; } = string.Empty;
        public string EstadoCliente { get; set; } = string.Empty;
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
        public string NoFactura { get; set; } = string.Empty;
        public string EstadoPago { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
    }
}
