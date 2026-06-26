using Modelo.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    public class PermisosPagina
    {
        private Menus menupermisos = new Menus();
        public void SetPermisos(Menus menu)
        {
            menupermisos = menu;
        }
        public bool Delete { get { return menupermisos.delete; } }
        public bool Create { get { return menupermisos.create; } }
        public bool Update { get { return menupermisos.update; } }
    }
}
