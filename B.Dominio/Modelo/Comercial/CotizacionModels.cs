using System;
using System.Collections.Generic;

namespace Modelo.Comercial
{
    public class Cotizacion
    {
        public Guid Id { get; set; }
        public string ClienteNombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string CreadoPor { get; set; }
        public string TipoCosteo { get; set; }
        public decimal UtilidadPorcentaje { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal TarifaSugerida { get; set; }
        public decimal TarifaAcordada { get; set; }
        public string Estado { get; set; }
        
        public CotizacionPersonalDetalle PersonalDetalle { get; set; }
        public CotizacionProduccionDetalle ProduccionDetalle { get; set; }
    }

    public class CotizacionPersonalDetalle
    {
        public Guid CotizacionId { get; set; }
        public decimal SalarioBase { get; set; }
        public decimal PrestacionesFactor { get; set; }
        public decimal ViaticosTotales { get; set; }
        public decimal EppTotales { get; set; }
        public decimal Supervision { get; set; }
        public decimal Cargos { get; set; }
        public decimal Seguros { get; set; }
        public decimal GastosOperativos { get; set; }
    }

    public class CotizacionProduccionDetalle
    {
        public Guid CotizacionId { get; set; }
        public string SkuNombre { get; set; }
        public int ProduccionDiaria { get; set; }
        public decimal ManoObraUnitaria { get; set; }
        public decimal MaterialesTotales { get; set; }
        public decimal MermaPorcentaje { get; set; }
        public decimal AmortizacionUnitaria { get; set; }
    }

    public class SaveCotizacionRequest
    {
        public string ClienteNombre { get; set; }
        public string TipoCosteo { get; set; }
        public decimal UtilidadPorcentaje { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal TarifaSugerida { get; set; }
        public decimal TarifaAcordada { get; set; }
        
        public CotizacionPersonalDetalle PersonalDetalle { get; set; }
        public CotizacionProduccionDetalle ProduccionDetalle { get; set; }
    }
}
