using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Clase de inicialización de los contextos configurados
    /// </summary>
    public class InfoContex
    {
        /// <summary>
        /// Ip del servidor de base de datos
        /// </summary>
        public string IpServer { set; get; }
        /// <summary>
        /// Usuario de base de datos
        /// </summary>
        public string UserId { set; get; }
        /// <summary>
        /// Contraseña de usuario de base de datos
        /// </summary>
        public string PassId { set; get; }
        /// <summary>
        /// Nombre del contexto de base de datos
        /// </summary>
        public string ContexName { set; get; }
        /// <summary>
        /// Nombre de la abse de datos
        /// </summary>
        public string Database { set; get; }
        /// <summary>
        /// Nombre del esquema para acceder a base de datos
        /// </summary>
        public string Esquema { set; get; }
        /// <summary>
        /// Numero del puerto de conección a base de datos
        /// </summary>
        public string Puerto { set; get; }
    }
}



