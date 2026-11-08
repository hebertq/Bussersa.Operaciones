using System;

namespace Modelo.Entidades.Nomina
{
    public class SettlementReportRecord
    {
        public int id { get; set; }
        public int payslip_id { get; set; }
        public int payslip_run_id { get; set; }
        public string lote_nombre { get; set; } = string.Empty;
        public int no_empleado { get; set; }
        public string empleado_nombre { get; set; } = string.Empty;
        public string cedula { get; set; } = string.Empty;
        public int contrato_id { get; set; }
        public int cliente_id { get; set; }
        public string cliente_nombre { get; set; } = string.Empty;
        public DateTime? fecha_ingreso { get; set; }
        public DateTime? fecha_salida { get; set; }
        public DateTime fecha_liquidacion { get; set; }
        public decimal salario_base { get; set; }
        public decimal dias_laborados { get; set; }
        public decimal salario_ordinario_monto { get; set; }
        public decimal horas_extras_monto { get; set; }
        public decimal dias_vacaciones { get; set; }
        public decimal vacaciones_monto { get; set; }
        public decimal dias_aguinaldo { get; set; }
        public decimal aguinaldo_monto { get; set; }
        public decimal dias_indemnizacion { get; set; }
        public decimal indemnizacion_monto { get; set; }
        public decimal otros_ingresos { get; set; }
        public decimal total_devengado { get; set; }
        public decimal inss_laboral { get; set; }
        public decimal ir_laboral { get; set; }
        public decimal prestamos_deduccion { get; set; }
        public decimal otras_deducciones { get; set; }
        public decimal total_deducciones { get; set; }
        public decimal total_neto { get; set; }
        public int anio { get; set; }
        public int mes { get; set; }
        public DateTime fecha_cierre { get; set; }
    }
}
