using Modelo.Interfaces;
using System;
using System.Reflection;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Objeto contenedor de la información de la transaccón.
    /// </summary>
    public class ErrorResponse : IResponse
    {
        /// <summary>
        /// Objeto de tipo información de transacción
        /// </summary>
        public ResultadoGenerico Respuesta { get; set; }

        public Object ObjModel { set; get; }

        public void SetObjet()
        {
            ObjModel = null;
        }
        /// <summary>
        /// Contructor de la clase
        /// </summary>
        public ErrorResponse()
        {
            Respuesta = new ResultadoGenerico();
        }
    }
}
