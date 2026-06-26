using System;

namespace Modelo.Report
{
    public class NominaMensualReportar
    {
        public int id { get; set; }
        public int? anyo_mes_cierre { get; set; }
        public DateTime? fec_inicio { get; set; }
        public DateTime? fec_fin { get; set; }

        // Relación con empleados
        public int emp_find { get; set; }
        public int emp_nomina { get; set; }
        public string emp_noinss { get; set; } = string.Empty;
        public bool? emp_activo_inss { get; set; }
        public string emp_nombre { get; set; } = string.Empty;

        // Datos laborales
        public string emp_semanas_labs { get; set; } = string.Empty;
        public decimal? emp_diaslab { get; set; }
        public decimal? emp_no_hras_extra { get; set; }

        // Ingresos y Salarios
        public decimal? emp_sal_fijo { get; set; }
        public decimal? emp_sal_basico { get; set; }
        public decimal? emp_pago_hras_extras { get; set; }

        // Viáticos y depreciación
        public decimal? emp_viatico_trasporte { get; set; }
        public decimal? emp_viatico_alimentacion { get; set; }
        public decimal? emp_viatico_combustible { get; set; }
        public decimal? emb_depre_vehiculo { get; set; }

        // Prestaciones legales
        public decimal? emp_vacaciones { get; set; }
        public decimal? emp_aguinaldo { get; set; }

        // Totales de ingresos
        public decimal? emp_otros_ingresos { get; set; }
        public decimal? emp_total_ingresos { get; set; }
        public decimal? emp_ingresos_deducir { get; set; }

        // Deducciones
        public decimal? emp_deduc_inss { get; set; }
        public decimal? emp_otras_deduciones { get; set; }
        public decimal? emp_prestamos { get; set; }
        public decimal? emp_ir_reportar { get; set; }
        public decimal? emp_total_deducciones { get; set; }

        // Lógica de Novedad: 3 para ingresos nuevos, 9 para el resto
        public int emp_novedad
        {
            get
            {
                if (emp_sal_fijo != (emp_ingresos_deducir - emp_total_deducciones))
                    return 9; // Ingresos nuevos

                return 3; // Por defecto 9
            }
        }

        // Pago final 
        public decimal? emp_pago_reportar
        {
            get
            {
                return emp_ingresos_deducir - emp_total_deducciones;
            }
        }
        public decimal? emp_pago_recibir { get; set; }
        public decimal emp_depresiacion_reportar { get; set; }
    }
}
