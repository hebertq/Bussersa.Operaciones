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
    }
}
