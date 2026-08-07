using System;
using System.Collections.Generic;

namespace Modelo.Entidades.Nomina
{
    public enum EstadoConciliacionInss
    {
        CoincideCorrecto,
        DiferenciaCotizacion,
        SoloEnFacturaInss,
        SoloEnNomina
    }

    public class FacturaInssLinea
    {
        public string Nss { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Cedula { get; set; } = "";
        public string Periodo { get; set; } = "";
        public string Semanas { get; set; } = "";
        public decimal SalarioCotizado { get; set; }
        public decimal SalarioReal { get; set; }
        public decimal AporteLaboral { get; set; }
        public decimal AportePatronal { get; set; }
        public decimal Interes { get; set; }
        public decimal Total { get; set; }
    }

    public class ItemConciliacionInss
    {
        public int EmpleadoId { get; set; }
        public int Contrato { get; set; }
        public string Cedula { get; set; } = "";
        public string Nss { get; set; } = "";
        public string NombreEmpleado { get; set; } = "";
        public string Area { get; set; } = "";

        public decimal DiasLaboradosNomina { get; set; }
        public decimal HorasExtrasNomina { get; set; }
        public string SemanasInssFactura { get; set; } = "";
        public int SemanasInssNomina { get; set; }
        
        public EstadoConciliacionInss Estado { get; set; }
        public string EstadoBadge => Estado switch
        {
            EstadoConciliacionInss.CoincideCorrecto => "COINCIDE EXACTO",
            EstadoConciliacionInss.DiferenciaCotizacion => "DIFERENCIA MONTO",
            EstadoConciliacionInss.SoloEnFacturaInss => "SOLO EN FACTURA INSS (RECLAMO)",
            EstadoConciliacionInss.SoloEnNomina => "SOLO EN NÓMINA EMPRESA",
            _ => "DESCONOCIDO"
        };

        // Valores de Factura INSS (DetalleFactura.csv)
        public decimal SalarioCotizadoInss { get; set; }
        public decimal AporteLaboralInss { get; set; }
        public decimal AportePatronalInss { get; set; }
        public decimal TotalInss { get; set; }

        // Valores de Nómina Mensual Empresa (PayrollMonthRecord)
        public decimal SalarioBasicoNomina { get; set; }
        public decimal SalarioCotizableNomina { get; set; }
        public decimal InssRetenidoNomina { get; set; }
        public decimal InssPatronalNomina { get; set; }
        public decimal TotalNomina => InssRetenidoNomina + InssPatronalNomina;

        // Diferencias (INSS - Nómina)
        public decimal DiferenciaSalario => SalarioCotizadoInss - SalarioCotizableNomina;
        public decimal DiferenciaLaboral => AporteLaboralInss - InssRetenidoNomina;
        public decimal DiferenciaPatronal => AportePatronalInss - InssPatronalNomina;
        public decimal DiferenciaNetaTotal => TotalInss - TotalNomina;

        public string ObservacionReclamo { get; set; } = "";
    }

    public class ResumenConciliacionInss
    {
        public int TotalPersonasFacturaInss { get; set; }
        public int TotalPersonasNomina { get; set; }
        public int TotalCoincidentesExactos { get; set; }
        public int TotalDiferenciasMonto { get; set; }
        public int TotalSoloFacturaInss { get; set; }
        public int TotalSoloNomina { get; set; }

        public decimal TotalMontoFacturaInss { get; set; }
        public decimal TotalMontoNominaEmpresa { get; set; }
        public decimal DiferenciaNetaTotal => TotalMontoFacturaInss - TotalMontoNominaEmpresa;

        public decimal TotalCobradoDemasInss { get; set; }
        public decimal TotalNoCobradoInss { get; set; }
    }
}
