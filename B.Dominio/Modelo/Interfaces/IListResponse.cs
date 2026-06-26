using System.Collections.Generic;

namespace Modelo.Interfaces
{
    /// <summary>
    /// Objeto genérico de respuesta
    /// </summary>
    public interface IListResponse<TModel> : IResponse
    {
        /// <summary>
        /// Lista genérica de modelo retornada
        /// </summary>
        List<TModel> Model { get; set; }        
    }
}
