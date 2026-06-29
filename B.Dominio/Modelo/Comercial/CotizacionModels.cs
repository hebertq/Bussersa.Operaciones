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
        public string? NumeroCotizacion { get; set; }
        
        public CotizacionPersonalDetalle PersonalDetalle { get; set; }
        public CotizacionProduccionDetalle ProduccionDetalle { get; set; }
        
        public List<CotizacionEppDetalle> EppDetalles { get; set; } = new List<CotizacionEppDetalle>();
        public List<CotizacionViaticoDetalle> ViaticoDetalles { get; set; } = new List<CotizacionViaticoDetalle>();
        public List<CotizacionMaterialDetalle> MaterialDetalles { get; set; } = new List<CotizacionMaterialDetalle>();
        public List<CotizacionMaquinariaDetalle> MaquinariaDetalles { get; set; } = new List<CotizacionMaquinariaDetalle>();
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
        public string Turno { get; set; } = "";
        public int HorasTurno { get; set; }
        public decimal TarifaExtra { get; set; }
        public decimal TarifaFeriado { get; set; }
        public decimal TarifaDomingo { get; set; }
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
        public Guid? PersonalCotizacionId { get; set; }
    }

    public class CotizacionEppDetalle
    {
        public Guid CotizacionId { get; set; }
        public int? EppId { get; set; }
        public string Nombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal MesesProrrateo { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CostoMensual { get; set; }
    }

    public class CotizacionViaticoDetalle
    {
        public Guid CotizacionId { get; set; }
        public int? ViaticoId { get; set; }
        public string Nombre { get; set; }
        public decimal CostoMensual { get; set; }
    }

    public class CotizacionMaterialDetalle
    {
        public Guid CotizacionId { get; set; }
        public int? MaterialId { get; set; }
        public string Nombre { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class CotizacionMaquinariaDetalle
    {
        public Guid CotizacionId { get; set; }
        public int? MaquinariaId { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public int MesesProyeccion { get; set; }
        public decimal ProyeccionMensual { get; set; }
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
        
        public List<CotizacionEppDetalle> EppDetalles { get; set; } = new List<CotizacionEppDetalle>();
        public List<CotizacionViaticoDetalle> ViaticoDetalles { get; set; } = new List<CotizacionViaticoDetalle>();
        public List<CotizacionMaterialDetalle> MaterialDetalles { get; set; } = new List<CotizacionMaterialDetalle>();
        public List<CotizacionMaquinariaDetalle> MaquinariaDetalles { get; set; } = new List<CotizacionMaquinariaDetalle>();
    }
}
