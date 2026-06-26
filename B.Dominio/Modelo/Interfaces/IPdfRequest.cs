using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.Interfaces
{
    public interface IPdfRequest<TModel>
    {

        /// <summary>
        /// Detalle del modelo pasado para conversión 
        /// </summary>
        TModel Model { get; set; }      
    }
}
