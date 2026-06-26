using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class DiasTrabajados
    {
        [Display(Name = "Fecha", Order = 1)]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime fecha { get; set; } = DateTime.Now;

        [Display(Order = 3)]
        public int id { get; set; } = 0;

        [Display(Order = 4)]
        public string nombre { get; set; } = "";

        [Display(Order = 5)]
        public string entrada { get; set; } = "08:00:00";

        [Display(Order = 6)]
        public string salida { get; set; } = "17:00:00";

        [Display(Order = 7)]
        public decimal horas { get; set; } = 0;

        [Display(Order = 8)]
        public decimal horasextras { get; set; } = 0;
        public string tipoempleado { get; set; } = "";
        public decimal bono { get; set; } = 0;
        public decimal comida { get; set; } = 0;
        public int idmarca { get; set; } = 0;
        public int areaid { get; set; } = 0;
        public string area { get; set; } = string.Empty;

        [Display(Order = 7)]
        public decimal dias { get; set; } = 0;
    }

    public class Diasxempleados
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string area { get; set; }
        public string tipoempleado { get; set; }
        public decimal dias { get; set; }
        public decimal horasextras { get; set; }
        public decimal bono { get; set; }
        public List<DiasTrabajados> Detalle { get; set; } = new List<DiasTrabajados>();
    }

    public class DiasxempleadosOpera
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public string area { get; set; }
        public string tipoempleado { get; set; }
        public decimal dias { get; set; }
        public decimal horasextras { get; set; }
        public decimal bono { get; set; }
        public string detalle { get; set; } = "";
        public List<DiasTrabajados> detalleall
        {
            get
            {
                List<DiasTrabajados> data = new List<DiasTrabajados>();
                if (detalle.Length > 0) data = JsonConvert.DeserializeObject<List<DiasTrabajados>>(detalle);

                if (data.Count > 0)
                    return data.OrderByDescending(x => x.fecha).ToList();
                else
                    return new List<DiasTrabajados>();
            }
        }
        // Nueva propiedad para controlar expansión
        public bool IsExpanded { get; set; } = false;
    }

    public class DiasTrabajadosAreas : DiasTrabajados
    {
        [Display(Order = 9)]
        public string areamov { get; set; }

        [Display(Order = 10)]
        public int tarea { get; set; }

        [Display(Order = 11)]
        public string? entrada_movimiento { get; set; }

        [Display(Order = 12)]
        public string? salida_movimineto { get; set; } 

        [Display(Order = 13)]
        public string comentario { get; set; }
        public bool state_contra { get; set; } = false;

        public string estado_calculado { get; set; }

        // --- NUEVAS COLUMNAS SEGÚN EL TYPE employee_area_total_record ---

        [Display(Name = "ID Movimiento", Order = 14)]
        public int movimiento_id { get; set; }

        [Display(Name = "Cliente Conflicto", Order = 15)]
        public string? cliente_conflicto { get; set; }

        [Display(Name = "Desbordamiento", Order = 16)]
        public decimal desbordamiento { get; set; }

        [Display(Name = "Bono Movimiento", Order = 17)]
        public decimal bono_movimiento { get; set; }

        [Display(Name = "Comida Movimiento", Order = 18)]
        public decimal comida_movimiento { get; set; }

        [Display(Name = "Doble Turno", Order = 21)]
        public bool doble_turno { get; set; } = false;

        [Display(Name = "Cierre", Order = 22)]
        public bool cierre { get; set; } = false;
        public bool es_op_mixta { get; set; } = false;
    }
}
