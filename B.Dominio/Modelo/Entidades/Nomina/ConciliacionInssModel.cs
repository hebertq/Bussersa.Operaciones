using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        [Display(Name = "Estado")]
        public string EstadoBadge => Estado switch
        {
            EstadoConciliacionInss.CoincideCorrecto => "COINCIDE EXACTO",
            EstadoConciliacionInss.DiferenciaCotizacion => "DIFERENCIA MONTO",
            EstadoConciliacionInss.SoloEnFacturaInss => "SOLO EN FACTURA INSS (RECLAMO)",
            EstadoConciliacionInss.SoloEnNomina => "SOLO EN NÓMINA EMPRESA",
            _ => "DESCONOCIDO"
        };

        [Display(Name = "No. Contrato")]
        public int Contrato { get; set; }

        [Display(Name = "ID Empleado")]
        public int EmpleadoId { get; set; }

        [Display(Name = "No. Cédula")]
        public string Cedula { get; set; } = "";

        [Display(Name = "No. INSS")]
        public string Nss { get; set; } = "";

        [Display(Name = "Nombre Empleado")]
        public string NombreEmpleado { get; set; } = "";

        [Display(Name = "Salario Fijo")]
        public decimal SalarioFijoNomina { get; set; }

        [Display(Name = "Días Laborados")]
        public decimal DiasLaboradosNomina { get; set; }

        [Display(Name = "Horas Extras")]
        public decimal HorasExtrasNomina { get; set; }

        [Display(Name = "Salario Básico")]
        public decimal SalarioBasicoNomina { get; set; }

        [Display(Name = "Reportado al INSS (Nómina)")]
        public decimal SalarioCotizableNomina { get; set; }

        [Display(Name = "Semanas INSS (Archivo)")]
        public string SemanasInssFactura { get; set; } = "";

        [Display(Name = "Semanas INSS (Nómina)")]
        public int SemanasInssNomina { get; set; }

        [Display(Name = "Salario Cotizado (INSS Archivo)")]
        public decimal SalarioCotizadoInss { get; set; }

        [Display(Name = "Aporte Laboral (INSS)")]
        public decimal AporteLaboralInss { get; set; }

        [Display(Name = "Aporte Patronal (INSS)")]
        public decimal AportePatronalInss { get; set; }

        [Display(Name = "Cobrado Real INSS (Archivo)")]
        public decimal TotalInss { get; set; }

        [Display(Name = "INSS Retenido 7% (Nómina)")]
        public decimal InssRetenidoNomina { get; set; }

        [Display(Name = "INSS Patronal 22.5% (Nómina)")]
        public decimal InssPatronalNomina { get; set; }

        [Display(Name = "Total Retenido/Patronal (Nómina)")]
        public decimal TotalNomina => InssRetenidoNomina + InssPatronalNomina;

        [Display(Name = "Diferencia C$")]
        public decimal DiferenciaNetaTotal => TotalInss - TotalNomina;

        [Display(Name = "Detalle de Auditoría / Reclamo")]
        public string ObservacionReclamo { get; set; } = "";

        public EstadoConciliacionInss Estado { get; set; }
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
