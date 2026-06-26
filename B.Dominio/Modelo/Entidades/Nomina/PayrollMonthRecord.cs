using System;

namespace Modelo.Entidades.Nomina
{
    public class PayrollMonthRecord
    {
        public DateTime fec_inicio { get; set; }
        public DateTime fec_fin { get; set; }
        public int id { get; set; }
        public int contrato { get; set; }
        public string noinss { get; set; }
        public bool activo_inss { get; set; }
        public string nombre { get; set; }
        public decimal sal_fijo { get; set; }
        public decimal diaslab { get; set; }
        public decimal no_hras_extra { get; set; }
        public decimal sal_basico { get; set; }
        public decimal pago_hras_extras { get; set; }
        public decimal pago_hras_descargas { get; set; }
        public decimal viatico_trasporte { get; set; }
        public decimal viatico_alimentacion { get; set; }
        public decimal viatico_combustible { get; set; }
        public decimal viatico_telefono { get; set; }
        public decimal bono_cumplimiento { get; set; }
        public decimal depre_vehiculo { get; set; }
        public decimal vacaciones { get; set; }
        public decimal aguinaldo { get; set; }
        public decimal otros_ingresos { get; set; }
        public decimal deduc_inss { get; set; }
        public decimal ir_reportar { get; set; }
        public decimal prestamos { get; set; }
        public decimal otras_deduciones { get; set; }
        public decimal pago_recibir { get; set; }
        public decimal acum_vac { get; set; }
        public decimal acum_agui { get; set; }
        public decimal acum_ind { get; set; }
        public decimal tot_vac { get; set; }
        public decimal tot_agui { get; set; }
        public decimal tot_ind { get; set; }
    }
}
