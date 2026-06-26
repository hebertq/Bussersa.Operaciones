using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.Entidades
{
    public class Menus
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public bool validate { get; set; }
        public bool create { get; set; }
        public bool update { get; set; }
        public bool delete { get; set; }

    }
}
