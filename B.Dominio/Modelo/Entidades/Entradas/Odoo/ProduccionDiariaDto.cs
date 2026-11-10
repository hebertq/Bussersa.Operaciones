using System;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class ProduccionDiariaDto
    {
        public int id { get; set; }
        public string hoja_servicio { get; set; } = string.Empty;
        public string? actividad { get; set; }
        public string? cliente { get; set; }
        public string? area_cliente { get; set; }
        public DateTime? fecha_inicio { get; set; }
        public DateTime? fecha_fin { get; set; }
        public string? hora_inicio { get; set; }
        public string? hora_fin { get; set; }
        public string? nombre_producto { get; set; }
        public string? no_lote { get; set; }
        public string? oc { get; set; }
        public string? no_marchamo { get; set; }
        public decimal peso { get; set; }
        public decimal cantidad_producto { get; set; }
        public decimal costo_producto { get; set; }
        public string? servicio_codigo { get; set; }
        public string? servicio_descripcion { get; set; }
        public string? asignado_a { get; set; }
        public bool facturada { get; set; }
        public string? no_proforma { get; set; }
        public string? no_factura { get; set; }
        public DateTime fecha_registro { get; set; }
        public int usuario_registro { get; set; }
        public int? proforma_orden { get; set; }
        public int? operacion_id { get; set; }
        public int? parent_id { get; set; }
        public string? parent_producto_nombre { get; set; }
        public int? template_id { get; set; }
        public string? template_nombre { get; set; }

                public static bool EsCodigoHorasLaborales(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return false;
            var c = codigo.Trim().ToUpperInvariant();
            return c.EndsWith("-OPR") || c.EndsWith("-ADB") || c.EndsWith("-ADE") || 
                   c.EndsWith("-ADV") || c.EndsWith("-MTC") || c.EndsWith("-CHO") ||
                   c.StartsWith("SVTC") || c.StartsWith("HOR-") ||
                   c.Contains("-DIU-") || c.Contains("-EXT-") || c.Contains("-NOC-");
        }

        public static bool EsActividadDeProduccion(ProduccionDiariaDto? item)
        {
            if (item == null) return false;
            if (item.template_id == 9 || item.template_id == 865) return false;
            if (!string.IsNullOrWhiteSpace(item.template_nombre))
            {
                var tn = item.template_nombre;
                if (tn.Contains("horas", StringComparison.OrdinalIgnoreCase) ||
                    tn.Contains("viatico", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            if (EsCodigoHorasLaborales(item.servicio_codigo)) return false;
            if (!string.IsNullOrWhiteSpace(item.actividad))
            {
                var act = item.actividad;
                if (act.Contains("horas laborales", StringComparison.OrdinalIgnoreCase) ||
                    act.StartsWith("viatico", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
