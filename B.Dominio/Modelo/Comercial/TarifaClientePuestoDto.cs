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
        public decimal tarifa_ordinaria_hora { get; set; }
        public decimal tarifa_extra_hora { get; set; }
        public decimal margen_objetivo_pct { get; set; } = 25.00m;
        public bool activo { get; set; } = true;
        public string? notas { get; set; }
        public DateTime fecha_registro { get; set; }
        public DateTime fecha_actualizacion { get; set; }
        public int? usuario_registro { get; set; }

        public decimal tarifa_venta_hora 
        { 
            get => tarifa_ordinaria_hora; 
            set => tarifa_ordinaria_hora = value; 
        }
        public decimal costo_real_hora 
        { 
            get => tarifa_extra_hora; 
            set => tarifa_extra_hora = value; 
        }
    }
}
