using System;
using System.Collections.Generic;

namespace Modelo.Comercial
{
    public class RentabilidadServicioPrecisionDto
    {
        public int operacion_id { get; set; }
        public int OperacionId { get => operacion_id; set => operacion_id = value; }

        public string operacion_nombre { get; set; }
        public string OperacionNombre { get => operacion_nombre; set => operacion_nombre = value; }

        public string cargo { get; set; }
        public string Cargo { get => cargo; set => cargo = value; }

        public long colaboradores_activos { get; set; }
        public long ColaboradoresActivos { get => colaboradores_activos; set => colaboradores_activos = value; }

        public decimal total_horas_trabajadas { get; set; }
        public decimal TotalHorasTrabajadas { get => total_horas_trabajadas; set => total_horas_trabajadas = value; }

        public decimal total_costo_laboral_real { get; set; }
        public decimal TotalCostoLaboralReal { get => total_costo_laboral_real; set => total_costo_laboral_real = value; }

        public decimal costo_promedio_hora { get; set; }
        public decimal CostoPromedioHora { get => costo_promedio_hora; set => costo_promedio_hora = value; }

        public decimal total_ingreso_facturado { get; set; }
        public decimal TotalIngresoFacturado { get => total_ingreso_facturado; set => total_ingreso_facturado = value; }

        public decimal tarifa_promedio_hora { get; set; }
        public decimal TarifaPromedioHora { get => tarifa_promedio_hora; set => tarifa_promedio_hora = value; }

        public decimal utilidad_bruta { get; set; }
        public decimal UtilidadBruta { get => utilidad_bruta; set => utilidad_bruta = value; }

        public decimal margen_porcentaje { get; set; }
        public decimal MargenPorcentaje { get => margen_porcentaje; set => margen_porcentaje = value; }

        public string estado_rentabilidad { get; set; }
        public string EstadoRentabilidad { get => estado_rentabilidad; set => estado_rentabilidad = value; }

        public decimal tarifa_sugerida_minima { get; set; }
        public decimal TarifaSugeridaMinima { get => tarifa_sugerida_minima; set => tarifa_sugerida_minima = value; }

        public decimal tarifa_actual_vs_sugerida_pct { get; set; }
        public decimal TarifaActualVsSugeridaPct { get => tarifa_actual_vs_sugerida_pct; set => tarifa_actual_vs_sugerida_pct = value; }
    }

    public class RentabilidadClienteRedistribuidaDto
    {
        public int operacion_id { get; set; }
        public int OperacionId { get => operacion_id; set => operacion_id = value; }

        public string operacion_nombre { get; set; }
        public string OperacionNombre { get => operacion_nombre; set => operacion_nombre = value; }

        public long colaboradores_nomina { get; set; }
        public long ColaboradoresNomina { get => colaboradores_nomina; set => colaboradores_nomina = value; }

        public decimal costo_nomina_nominal { get; set; }
        public decimal CostoNominaNominal { get => costo_nomina_nominal; set => costo_nomina_nominal = value; }

        public long colaboradores_trabajando { get; set; }
        public long ColaboradoresTrabajando { get => colaboradores_trabajando; set => colaboradores_trabajando = value; }

        public decimal horas_trabajadas_totales { get; set; }
        public decimal HorasTrabajadasTotales { get => horas_trabajadas_totales; set => horas_trabajadas_totales = value; }

        public decimal costo_laboral_redistribuido { get; set; }
        public decimal CostoLaboralRedistribuido { get => costo_laboral_redistribuido; set => costo_laboral_redistribuido = value; }

        public decimal ingreso_total_facturado { get; set; }
        public decimal IngresoTotalFacturado { get => ingreso_total_facturado; set => ingreso_total_facturado = value; }

        public decimal utilidad_nominal { get; set; }
        public decimal UtilidadNominal { get => utilidad_nominal; set => utilidad_nominal = value; }

        public decimal margen_nominal_pct { get; set; }
        public decimal MargenNominalPct { get => margen_nominal_pct; set => margen_nominal_pct = value; }

        public decimal utilidad_redistribuida { get; set; }
        public decimal UtilidadRedistribuida { get => utilidad_redistribuida; set => utilidad_redistribuida = value; }

        public decimal margen_redistribuido_pct { get; set; }
        public decimal MargenRedistribuidoPct { get => margen_redistribuido_pct; set => margen_redistribuido_pct = value; }

        public decimal diferencia_costo { get; set; }
        public decimal DiferenciaCosto { get => diferencia_costo; set => diferencia_costo = value; }

        public string efecto_distorsion { get; set; }
        public string EfectoDistorsion { get => efecto_distorsion; set => efecto_distorsion = value; }
    }

    public class ConciliacionPrestamoPersonalDto
    {
        public int colaborador_id { get; set; }
        public int ColaboradorId { get => colaborador_id; set => colaborador_id = value; }

        public string colaborador_nombre { get; set; }
        public string ColaboradorNombre { get => colaborador_nombre; set => colaborador_nombre = value; }

        public string cargo { get; set; }
        public string Cargo { get => cargo; set => cargo = value; }

        public int cliente_origen_id { get; set; }
        public int ClienteOrigenId { get => cliente_origen_id; set => cliente_origen_id = value; }

        public string cliente_origen_nombre { get; set; }
        public string ClienteOrigenNombre { get => cliente_origen_nombre; set => cliente_origen_nombre = value; }

        public decimal costo_mensual_origen { get; set; }
        public decimal CostoMensualOrigen { get => costo_mensual_origen; set => costo_mensual_origen = value; }

        public int cliente_destino_id { get; set; }
        public int ClienteDestinoId { get => cliente_destino_id; set => cliente_destino_id = value; }

        public string cliente_destino_nombre { get; set; }
        public string ClienteDestinoNombre { get => cliente_destino_nombre; set => cliente_destino_nombre = value; }

        public decimal horas_en_destino { get; set; }
        public decimal HorasEnDestino { get => horas_en_destino; set => horas_en_destino = value; }

        public decimal horas_totales_mes { get; set; }
        public decimal HorasTotalesMes { get => horas_totales_mes; set => horas_totales_mes = value; }

        public decimal porcentaje_tiempo_destino { get; set; }
        public decimal PorcentajeTiempoDestino { get => porcentaje_tiempo_destino; set => porcentaje_tiempo_destino = value; }

        public decimal costo_transferido_destino { get; set; }
        public decimal CostoTransferidoDestino { get => costo_transferido_destino; set => costo_transferido_destino = value; }
    }

    public class RentabilidadPrecisionConsolidadoDto
    {
        public List<RentabilidadServicioPrecisionDto> Servicios { get; set; } = new List<RentabilidadServicioPrecisionDto>();
        public List<RentabilidadClienteRedistribuidaDto> Clientes { get; set; } = new List<RentabilidadClienteRedistribuidaDto>();
        public List<ConciliacionPrestamoPersonalDto> Prestamos { get; set; } = new List<ConciliacionPrestamoPersonalDto>();

        // Resumen KPIs
        public decimal TotalIngresos { get; set; }
        public decimal TotalCostoReal { get; set; }
        public decimal TotalUtilidadReal { get; set; }
        public decimal MargenGlobalPct { get; set; }
        public decimal TotalHorasTrabajadas { get; set; }
        public int TotalColaboradoresActivos { get; set; }
        public int ServiciosEnPerdida { get; set; }
        public int ServiciosEnRiesgo { get; set; }
        public int ServiciosRentables { get; set; }
        public decimal TotalCostoTransferido { get; set; }
        public decimal TotalHorasPrestadas { get; set; }
    }
}
