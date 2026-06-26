using System.ComponentModel.DataAnnotations;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class diasxpagarperiodo
    {
        [Display(Name = "Area")]
        public string area { get; set; }

        [Display(Name = "Tipo Empleado")]
        public string tipoempleado { get; set; }

        [Display(Name = "Id")]
        public int id { get; set; }

        [Display(Name = "Nombre")]
        public string nombre { get; set; }

        [Display(Name = "Dias Habiles")]
        public decimal dias_habiles { get; set; }

        [Display(Name = "Dias Trabajados")]
        public decimal diastrabajados { get; set; }

        [Display(Name = "Descansadas")]
        public decimal vacdes { get; set; }

        [Display(Name = "Pagadas")]
        public decimal vacpag { get; set; }
        public decimal subsidios { get; set; }
        public decimal justificados { get; set; }
        public decimal injustificados { get; set; }
        public decimal cuarentena { get; set; }
        public decimal suspension { get; set; }

        [Display(Name = "Septimo")]
        public decimal septimo { get; set; }

        [Display(Name = "Dias a Pagar")]
        public decimal totaldias { get; set; }

        [Display(Name = "HE a Pagar")]
        public decimal hexpagar { get; set; }

        [Display(Name = "Bono")]
        public decimal bono { get; set; }

        [Display(Name = "Feriados")]
        public decimal diasferiados { get; set; }

        [Display(Name = "Aguinaldo")]
        public decimal aguinaldo { get; set; } = 0;

        [Display(Name = "Otros ingresos")]
        public decimal otros_ingresos { get; set; } = 0;

        [Display(Name = "Otras deducciones")]
        public decimal otras_deducciones { get; set; } = 0;
    }
}
