using Modelo.ClasesGenericas;
using System;

namespace Modelo.Interfaces
{
    /// <summary>
    /// Detalle de la transacción 
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// Detalle de la transacción 
        /// </summary>
        ResultadoGenerico Respuesta { set; get; }
        Object ObjModel { set; get; }
        void SetObjet();
    }
}

