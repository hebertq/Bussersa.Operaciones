using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Tipos de Contextos Permitidos
    /// </summary>
    public enum InfoTipoContext
    {
        ///<summary>
        /// Contexto para SISCO
        /// </summary>
        SISCODB,
        ///<summary>
        /// Contexto para AS400 Nicaragua
        /// </summary>
        AS400NI,
        ///<summary>
        /// Contexto para AS400 Costa Rica
        /// </summary>
        AS400CR,
        ///<summary>
        /// Contexto para AS400 Honduras
        /// </summary>
        AS400HN,
        ///<summary>
        /// Contexto para AS400 Panama
        /// </summary>
        BIZRD
    }

    public enum InfoContext
    {
        ///<summary>
        /// Contexto para AS400 Nicaragua
        /// </summary>
        AS400NI = 1,
        ///<summary>
        /// Contexto para AS400 Costa Rica
        /// </summary>
        AS400CR = 2,
        ///<summary>
        /// Contexto para AS400 Honduras
        /// </summary>
        AS400HN = 3,
        ///<summary>
        /// Contexto para AS400 Dominicana
        /// </summary>
        AS400RD = 4
    }
}