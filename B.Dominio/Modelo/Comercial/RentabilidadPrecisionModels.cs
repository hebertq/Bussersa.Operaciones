using System;
using System.Collections.Generic;

namespace Modelo.Comercial
{
    public class RentabilidadServicioPrecisionDto
    {
        public int operacion_id { get; set; }
        public string cliente_nombre { get; set; } = string.Empty;
        public string cargo { get; set; } = string.Empty;
        public decimal horas_trabajadas { get; set; }
        public decimal costo_real_total { get; set; }
        public decimal costo_real_hora { get; set; }
        public decimal ingreso_total { get; set; }
        public decimal tarifa_promedio_hora { get; set; }
        public decimal utilidad_neta { get; set; }
        public decimal margen_porcentaje { get; set; }
        public decimal margen_hora { get; set; }
        public decimal precio_sugerido { get; set; }
        public string estado_rentabilidad { get; set; } = string.Empty;
    }

    public class RentabilidadClienteRedistribuidaDto
    {
        public int operacion_id { get; set; }
        public string cliente_nombre { get; set; } = string.Empty;
        public decimal ingresos_totales { get; set; }
        public decimal costo_nomina_anterior { get; set; }
        public decimal costo_nomina_real { get; set; }
        public decimal ajuste_prestamos { get; set; }
        public decimal utilidad_anterior { get; set; }
        public decimal utilidad_real { get; set; }
        public decimal margen_anterior { get; set; }
        public decimal margen_real { get; set; }
        public decimal total_horas_reales { get; set; }
        public int total_empleados_reales { get; set; }
    }

    public class ConciliacionPrestamoPersonalDto
    {
        public int id_empleado { get; set; }
        public string nombre_empleado { get; set; } = string.Empty;
        public int op_nomina_id { get; set; }
        public string op_nomina_nombre { get; set; } = string.Empty;
        public int op_trabajo_id { get; set; }
        public string op_trabajo_nombre { get; set; } = string.Empty;
        public string cargo { get; set; } = string.Empty;
        public decimal horas_trabajadas { get; set; }
        public decimal costo_hora_empleado { get; set; }
        public decimal costo_transferido { get; set; }
    }

    public class RentabilidadPrecisionConsolidadoDto
    {
        public List<RentabilidadServicioPrecisionDto> Servicios { get; set; } = new List<RentabilidadServicioPrecisionDto>();
        public List<RentabilidadClienteRedistribuidaDto> Clientes { get; set; } = new List<RentabilidadClienteRedistribuidaDto>();
        public List<ConciliacionPrestamoPersonalDto> Prestamos { get; set; } = new List<ConciliacionPrestamoPersonalDto>();

        // Resumen KPIs Reales
        public decimal TotalIngresos { get; set; }
        public decimal TotalCostoReal { get; set; }
        public decimal TotalUtilidadReal { get; set; }
        public decimal MargenGlobalPct { get; set; }
        public decimal TotalHorasTrabajadas { get; set; }
        public int ServiciosEnPerdida { get; set; }
        public int ServiciosRentables { get; set; }
        public decimal TotalCostoTransferido { get; set; }
        public decimal TotalHorasPrestadas { get; set; }
    }

    public class RentabilidadCargoConsolidadoDto
    {
        public string cargo { get; set; } = string.Empty;
        public int total_operaciones { get; set; }
        public decimal horas_totales { get; set; }
        public decimal ingreso_total { get; set; }
        public decimal costo_real_total { get; set; }
        public decimal tarifa_promedio_hora { get; set; }
        public decimal costo_promedio_hora { get; set; }
        public decimal utilidad_neta { get; set; }
        public decimal margen_porcentaje { get; set; }
        public decimal margen_hora { get; set; }
        public decimal precio_sugerido { get; set; }
        public decimal participacion_ingreso_pct { get; set; }
        public string estado_rentabilidad { get; set; } = string.Empty;
    }

    public class AuditoriaIncidenciasOperacionDto
    {
        public int operacion_id { get; set; }
        public string operacion_nombre { get; set; } = string.Empty;
        public int total_registros { get; set; }
        public int registros_ok { get; set; }
        public int total_desbordes { get; set; }
        public decimal horas_desborde { get; set; }
        public int total_cruces { get; set; }
        public int total_sin_reloj { get; set; }
        public int total_sin_tarea { get; set; }
        public decimal indice_calidad_pct { get; set; }
        public string estado_calidad { get; set; } = string.Empty;
    }
}
