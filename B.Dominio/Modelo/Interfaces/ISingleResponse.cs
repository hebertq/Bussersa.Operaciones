namespace Modelo.Interfaces
{
    /// <summary>
    /// Objeto genérico de respuesta
    /// </summary>
    public interface ISingleResponse<TModel> : IResponse
    {
        /// <summary>
        /// Detalle del modeloretornado
        /// </summary>
        TModel Model { get; set; }
    }
}
