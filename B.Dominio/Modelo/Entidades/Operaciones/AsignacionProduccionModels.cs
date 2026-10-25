using System;
using System.Collections.Generic;

namespace Modelo.Entidades.Operaciones
{
    public class CatalogoServicioPuestoAreaDto
    {
        public int id { get; set; }
        public int operacion_id { get; set; }
        public string cliente_nombre { get; set; } = string.Empty;
        public string area_nombre { get; set; } = string.Empty;
        public string cargo { get; set; } = string.Empty;
        public string turno { get; set; } = "*"; // *, Diurno, Nocturno, Mixto
        public string tipo_hora { get; set; } = "ORDINARIA"; // ORDINARIA, EXTRA, DOMINGO_FERIADO, NOCTURNA
        public string servicio_codigo { get; set; } = string.Empty;
        public string servicio_descripcion { get; set; } = string.Empty;
        public string unidad_medida { get; set; } = "HORAS"; // HORAS, DIAS
        public decimal factor_conversion_dias { get; set; } = 8.00m;
        public decimal tarifa_venta { get; set; }
        public bool activo { get; set; } = true;
        public DateTime? fecha_creacion { get; set; }
        public DateTime? fecha_modificacion { get; set; }
    }

    public class MarcadaAsistenciaProduccionDto
    {
        public int id_cierre { get; set; }
        public int operacion_id { get; set; }
        public int id_empleado { get; set; }
        public string nombre_empleado { get; set; } = string.Empty;
        public DateTime fecha_asistencia { get; set; }
        public string cargo { get; set; } = string.Empty;
        public string area_nombre { get; set; } = string.Empty;
        public string turno { get; set; } = "Diurno"; // Diurno, Nocturno, Mixto, Sin Turno
        public string tipo_empleado { get; set; } = string.Empty;
        public string entrada_movimiento { get; set; } = string.Empty;
        public string salida_movimiento { get; set; } = string.Empty;
        public decimal horas_totales { get; set; }
        public decimal horas_ordinarias { get; set; }
        public decimal horas_extras { get; set; }
        public bool es_supervisor { get; set; }
        public string regla_aplicada { get; set; } = "Estándar";
        public bool asignado_produccion { get; set; }
        public string? numero_asignacion_produccion { get; set; }
        public DateTime? fecha_asignacion_produccion { get; set; }
        public string? servicio_codigo_ord { get; set; }
        public string? servicio_desc_ord { get; set; }
        public decimal tarifa_ord { get; set; }
        public string? servicio_codigo_ext { get; set; }
        public string? servicio_desc_ext { get; set; }
        public decimal tarifa_ext { get; set; }
        public bool tiene_regla_catalogo { get; set; }
        public bool IsSelected { get; set; } = true;
    }

    public class GrillaProduccionSemanalDto
    {
        public string cliente_nombre { get; set; } = string.Empty;
        public string area_cliente { get; set; } = string.Empty;
        public string cargo { get; set; } = string.Empty;
        public string turno { get; set; } = "*";
        public string tipo_hora { get; set; } = string.Empty;
        public string servicio_codigo { get; set; } = string.Empty;
        public string servicio_descripcion { get; set; } = string.Empty;
        public string unidad_medida { get; set; } = "HORAS";
        public decimal tarifa_venta { get; set; }
        public decimal cantidad_horas { get; set; }
        public decimal cantidad_facturable { get; set; }
        public decimal subtotal_facturable { get; set; }
        public int cantidad_empleados { get; set; }
        public int cantidad_registros { get; set; }
    }

    public class AsignarMarcadasProduccionRequest
    {
        public List<int> ids { get; set; } = new();
        public string numero_asignacion { get; set; } = string.Empty;
        public int usuario_id { get; set; }
    }

    public class HistoricoAsignacionProduccionDto
    {
        public string numero_asignacion { get; set; } = string.Empty;
        public int operacion_id { get; set; }
        public string cliente_nombre { get; set; } = string.Empty;
        public DateTime fecha_min { get; set; }
        public DateTime fecha_max { get; set; }
        public int cantidad_empleados { get; set; }
        public int total_marcadas { get; set; }
        public decimal total_horas { get; set; }
        public DateTime? fecha_asignacion { get; set; }
        public decimal horas_pendientes_en_rango { get; set; }
        public int marcadas_pendientes_en_rango { get; set; }
    }
}
