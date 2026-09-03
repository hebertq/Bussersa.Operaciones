using System;

namespace Modelo.Entidades.Nomina
{
    public class CostoNominaMensualClienteDto
    {
        public int cliente { get; set; }
        public int total_empleados { get; set; }
        public decimal dias_trabajados { get; set; }
        public decimal salario_fijo { get; set; }
        public decimal pago_recibir { get; set; }
        public decimal acumulado_vacaciones { get; set; }
        public decimal acumulado_aguinaldo { get; set; }
        public decimal acumulado_indemnizacion { get; set; }
        public decimal inss_patronal { get; set; }
        public decimal costo_total_nomina { get; set; }
    }
}
