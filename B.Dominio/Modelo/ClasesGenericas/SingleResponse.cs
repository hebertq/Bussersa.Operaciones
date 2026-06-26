using Modelo.Interfaces;
using System;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Objeto genérico de respuesta
    /// </summary>
    public class SingleResponse<TModel> : ISingleResponse<TModel> where TModel : new()
    {
        /// <summary>
        /// Inicialización de las variables
        /// </summary>
        public SingleResponse()
        {
            Model = new TModel();
            Respuesta = new ResultadoGenerico();
        }

        /// <summary>
        /// Detalle de la transacción 
        /// </summary>
        public ResultadoGenerico Respuesta { get; set; }
        /// <summary>
        /// Modelo retornado como respuesta
        /// </summary>
        public TModel Model { get; set; }
        public Object ObjModel { set; get; }

        public void SetObjet()
        {
            ObjModel = Model;
        }
    }
}