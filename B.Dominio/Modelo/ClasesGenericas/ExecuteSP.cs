using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Objeto enviado como parametro para ejecutar los procedimientos almacenados
    /// </summary>
    public class ExecuteSP<TModel> : StoreProcedure<TModel> where TModel : new()
    {
        /// <summary>
        /// Contexto de base de datos usado para la conexión.
        /// </summary>
        public InfoContextDB2 Contexto { set; get; }
    }

    public class ListExecuteSP<TModel> : StoreProcedure<TModel> where TModel : new()
    {
        /// <summary>
        /// Listado de procedimientos a ejecurar.
        /// </summary>
        public List<StoreProcedure<TModel>> SPExec { set; get; } = new List<StoreProcedure<TModel>>();
        /// <summary>
        /// Contexto de base de datos usado para la conexión.
        /// </summary>
        public InfoContextDB2 Contexto { set; get; }
        /// <summary>
        /// Variable para detener la ejecucón en caso que uno de los sp de la lista retorne error
        /// </summary>
        public bool DetenerExec { set; get; } = false;
    }
    public class StoreProcedure<TModel> : IStoreProcedure<TModel> where TModel : new()
    {
        /// <summary>
        /// Nombre del Sp a ejecutar.
        /// </summary>
        public string Nombre_SP { set; get; } = string.Empty;
        /// <summary>
        /// Parametros de entrada y salida ordenados de la misma forma que se crearon en el proceimineto almacenado. 
        /// </summary>
        public TModel Parametros { set; get; } = new TModel();
        /// <summary>
        /// Orden de ejecucion del procedimiento.
        /// </summary>
        public int Orden { set; get; } = 0;
    }

    /// <summary>
    /// Objeto genérico de respuesta
    /// </summary>
    public interface IStoreProcedure<TModel>
    {
        /// <summary>
        /// Detalle del modeloretornado
        /// </summary>
        TModel Parametros { get; set; }
    }
}

