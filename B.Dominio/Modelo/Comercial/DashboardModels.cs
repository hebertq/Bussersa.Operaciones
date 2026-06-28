using System;
using System.Collections.Generic;

namespace Modelo.Comercial
{
    public class DashboardKpis
    {
        public decimal IngresosTotales { get; set; }
        public decimal CostoTotalOperativo { get; set; }
        public decimal UtilidadProducida { get; set; }
        public decimal MargenProduccionPercent { get; set; }
        public decimal UtilidadNeta { get; set; }
        public decimal MargenNetoPercent { get; set; }
        
        public decimal UnidadesProducidas { get; set; }
        public decimal EtiquetasProcesadas { get; set; }
        public decimal ReposicionPaletizante { get; set; }
        public decimal IngresosHExtras { get; set; }
        
        public string ProductoMayorVolumen { get; set; }
        public string ProductoMenorVolumen { get; set; }
        public string ProductoMenorMargen { get; set; }
    }

    public class SkuPerformance
    {
        public string Producto { get; set; }
        public decimal UnidadesProducidas { get; set; }
        public decimal IngresoTotal { get; set; }
        public decimal CostoTotalAsignado { get; set; }
        public decimal UtilidadNetaReal { get; set; }
        public decimal MargenNetoPercent { get; set; }
    }

    public class HeatmapCell
    {
        public string Semana { get; set; }
        public string DiaSemana { get; set; }
        public decimal MargenPercent { get; set; }
    }

    public class TurnComparison
    {
        public string Metrica { get; set; }
        public string TurnoDia { get; set; }
        public string TurnoNoche { get; set; }
        public string Diferencia { get; set; }
        public string MejorTurno { get; set; }
    }

    public class DashboardResponse
    {
        public DashboardKpis Kpis { get; set; } = new DashboardKpis();
        public List<SkuPerformance> SkuPerformanceList { get; set; } = new List<SkuPerformance>();
        public List<HeatmapCell> Heatmap { get; set; } = new List<HeatmapCell>();
        public List<TurnComparison> TurnComparisonList { get; set; } = new List<TurnComparison>();
    }
}
