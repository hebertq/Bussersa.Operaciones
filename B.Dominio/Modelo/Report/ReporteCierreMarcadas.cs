using System;

namespace Modelo.Report
{
    public class ReporteCierreMarcadas
    {
        public string? nombre_cliente { get; set; }
        public DateTime fecha_asistencia { get; set; }
        public int id_empleado { get; set; }
        public int tarea_id { get; set; }
        public string? nombre_empleado { get; set; }
        public string? tipo_empleado { get; set; }
        public string? area_nombre { get; set; }
        public string? entrada_movimiento { get; set; }
        public string? salida_movimiento { get; set; }
        public decimal horas_totales { get; set; }
        public decimal horas_extras { get; set; }
        public DateTime fecha_registro { get; set; }
        public DateTime? fecha_actualizacion { get; set; }
    }
}
