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

        // Propiedades de compatibilidad PascalCase
        public int OperacionId => operacion_id;
        public string ClienteNombre => cliente_nombre;
        public string Cargo => cargo;
        public decimal HorasTrabajadas => horas_trabajadas;
        public decimal CostoRealTotal => costo_real_total;
        public decimal CostoRealHora => costo_real_hora;
        public decimal IngresoTotal => ingreso_total;
        public decimal TarifaPromedioHora => tarifa_promedio_hora;
        public decimal UtilidadNeta => utilidad_neta;
        public decimal MargenPorcentaje => margen_porcentaje;
        public decimal MargenHora => margen_hora;
        public string EstadoRentabilidad => estado_rentabilidad;
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

        // Propiedades de compatibilidad PascalCase
        public int OperacionId => operacion_id;
        public string ClienteNombre => cliente_nombre;
        public decimal IngresosTotales => ingresos_totales;
        public decimal CostoNominaAnterior => costo_nomina_anterior;
        public decimal CostoNominaReal => costo_nomina_real;
        public decimal AjustePrestamos => ajuste_prestamos;
        public decimal UtilidadAnterior => utilidad_anterior;
        public decimal UtilidadReal => utilidad_real;
        public decimal MargenAnterior => margen_anterior;
        public decimal MargenReal => margen_real;
        public decimal TotalHorasReales => total_horas_reales;
        public int TotalEmpleadosReales => total_empleados_reales;
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

        // Propiedades de compatibilidad PascalCase
        public int IdEmpleado => id_empleado;
        public string NombreEmpleado => nombre_empleado;
        public int OpNominaId => op_nomina_id;
        public string OpNominaNombre => op_nomina_nombre;
        public int OpTrabajoId => op_trabajo_id;
        public string OpTrabajoNombre => op_trabajo_nombre;
        public string Cargo => cargo;
        public decimal HorasTrabajadas => horas_trabajadas;
        public decimal CostoHoraEmpleado => costo_hora_empleado;
        public decimal CostoTransferido => costo_transferido;
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
}
