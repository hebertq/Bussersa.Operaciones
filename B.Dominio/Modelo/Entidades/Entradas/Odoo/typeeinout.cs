using System;
using System.ComponentModel.DataAnnotations;


namespace Modelo.Entidades.Entradas.Odoo
{
    public class typeeinout
    {
        public int id { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime entrada { get; set; }
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime salida { get; set; }
    }
}
