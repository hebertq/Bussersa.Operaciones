using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.Entidades
{
    public class NavMenuModel
    {
        public int id { get; set; }
        public int parentid { get; set; }
        public string content { get; set; }
        public string icontype { get; set; }
        public string url { get; set; }
        public int order { get; set; }
        public bool IsOpened { get; set; }
    }

    public class MenuViewModel
    {
        public int id { get; set; }
        public string content { get; set; }
        public string icontype { get; set; }
        public string url { get; set; }
        public IList<MenuViewModel> Children { get; set; }
        public bool IsOpened { get; set; }  /*Add This*/
    }
}
