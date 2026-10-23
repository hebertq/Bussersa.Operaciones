using System;

namespace Modelo.Comercial
{
    public class TarifaClientePuestoDto
    {
        public int id { get; set; }
        public int operacion_id { get; set; }
        public string cliente_nombre { get; set; } = string.Empty;
        public string cargo { get; set; } = string.Empty;
        public string tipo_tarifa { get; set; } = "HORA_NORMAL";
        public decimal tarifa_venta_hora { get; set; }
        public decimal costo_real_hora { get; set; }
        public decimal margen_objetivo_pct { get; set; } = 25.00m;
        public bool activo { get; set; } = true;
        public string? notas { get; set; }
        public DateTime fecha_registro { get; set; }
        public DateTime fecha_actualizacion { get; set; }
        public int? usuario_registro { get; set; }

        public decimal utilidad_hora => tarifa_venta_hora - costo_real_hora;
        public decimal margen_porcentaje => tarifa_venta_hora > 0 
            ? Math.Round(((tarifa_venta_hora - costo_real_hora) / tarifa_venta_hora) * 100m, 2) 
            : 0m;
    }
}
