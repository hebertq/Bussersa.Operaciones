using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Manejador de tipos de errores
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Error de datos
        /// </summary>
        Datos = 1,
        /// <summary>
        /// Error de regla de negocio
        /// </summary>
        Validación = 2,
        /// <summary>
        /// Error del servicio
        /// </summary>
        Servicio = 3,
        /// <summary>
        /// Error de permisos
        /// </summary>
        Permiso = 4,
        /// <summary>
        /// Sin errores
        /// </summary>
        Ok = 5
    }
}
