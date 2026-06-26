using Modelo.Interfaces;
using System;
using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Objeto genérico de respuesta
    /// </summary>
    public class ListResponse<TModel> : IListResponse<TModel>, IResponse
    {
        /// <summary>
        /// Detalle de la transacción 
        /// </summary>
        public ResultadoGenerico Respuesta { get; set; }
        /// <summary>
        /// Lista genérica de modelo retornada
        /// </summary>
        public List<TModel> Model { get; set; }

        public Object ObjModel { set; get; }

        public void SetObjet()
        {
            ObjModel = Model;
        }
        /// <summary>
        /// Constructor de la clase de la lista generica
        /// </summary>        
        public ListResponse()
        {
            Respuesta = new ResultadoGenerico();
            Model = new List<TModel>();
        }
    }
}