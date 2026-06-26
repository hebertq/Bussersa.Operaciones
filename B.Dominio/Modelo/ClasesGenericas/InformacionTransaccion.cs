using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Información de la transacción actual
    /// </summary>
    public class InformacionTransaccion
    {
        /// <summary>
        /// Verificador de Error
        /// </summary>
        public bool IsError { get; set; }
        /// <summary>
        /// Descripción del Error 
        /// </summary>
        public string ReturnMessage { get; set; }
        /// <summary>
        /// Identificador del tipo de Error
        /// </summary>
        public string ErrorCode { get; set; }
        /// <summary>
        /// Verificador de autenticación de la conexión
        /// </summary>
        public bool IsAuthenicated { get; set; }
        /// <summary>
        /// Tipo de Error
        /// </summary>
        public ErrorType TipoError { get; set; }
        /// <summary>
        /// Informacion de la transacción para generar log de errores
        /// </summary>
        [XmlIgnoreAttribute]
        public string ErrorLog { get; set; }
        /// <summary>
        /// Dirección de redireccionamiento
        /// </summary>
        [XmlIgnoreAttribute]
        public string url { get; set; }

        /// <summary>
        /// Errores de validación.
        /// </summary>
        [XmlIgnoreAttribute]
        public string ValidationMessage { get; set; }
        /// <summary>
        /// Causas de errores por validación de entrada
        /// </summary>
        [XmlIgnoreAttribute]
        public Hashtable ValidationErrors { get; set; }

        /// <summary>
        /// Inicializacíón de las variables
        /// </summary>
        public InformacionTransaccion()
        {
            this.ReturnMessage = "Solicitud/Transaccion procesada exitosamente";
            this.IsError = false;
            this.IsAuthenicated = true;
            this.TipoError = ErrorType.Ok;
            this.ValidationErrors = new Hashtable();
            this.ErrorCode = "01";
            this.url = "";
            this.ErrorLog = "";
            this.ValidationMessage = "";
        }
        /// <summary>
        /// Actualiza la transaccón con los errores provocados por exepciones del sistema
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ex"></param>
        /// <param name="usuario"></param>
        public void SetErrorExep(ErrorType type, Exception ex, string usuario)
        {
            this.TipoError = type;
            this.IsError = true;
            this.ErrorCode = "-1";

            if (ex != null)
            {
                this.ErrorLog = string.Format("Usuario: {0}, Tipo de Error: ", usuario) + type + System.Environment.NewLine +
                                "                           Codigo: " + ex.HResult.ToString() + System.Environment.NewLine +
                                "                           Aplicación: " + ex.Source + System.Environment.NewLine +
                                "                           Mensaje: " + ex.Message;

                this.ReturnMessage = "Hubo un error interno, póngase en contacto con soporte técnico.";
            }
        }
        /// <summary>
        /// Actualiza la transacción con los errores de validacion de las reglas de negocio
        /// </summary>
        /// <param name="type"></param>
        /// <param name="mensaje"></param>
        /// <param name="codigo"></param>
        /// <param name="usuario"></param>
        public void SetErrorValid(ErrorType type, string mensaje, string codigo, string usuario)
        {
            this.TipoError = type;
            this.IsError = true;
            if (codigo != null)
                this.ErrorCode = codigo;
            else
                this.ErrorCode = "0";

            if (mensaje != null)
                this.ReturnMessage = string.Format("Usuario: {0}, Tipo de Error: ", usuario) + type + System.Environment.NewLine +
                                     "                           Codigo: " + codigo + System.Environment.NewLine +
                                     "                           Mensaje: " + mensaje;
        }
        /// <summary>
        /// Actualiza la información de la transacción con los errores personalizados de base de datos
        /// </summary>
        /// <param name="db"></param>
        //public void SetErrorDb(ErrorMjsBd db)
        //{
        //    IsError = db;
        //    if (db.IsError)
        //    {
        //        TipoError = ErrorType.Datos;
        //        ReturnMessage = "Codigo de Error: " + db.CodeError.ToString() + System.Environment.NewLine + "Mensaje: " + db.MjsError;
        //        this.ErrorLog = string.Format("Tipo de Error: ", ErrorType.Datos) + System.Environment.NewLine +
        //                       "                           Codigo: " + db.CodeError.ToString() + System.Environment.NewLine +
        //                       "                           número: " + db.ErrState.ToString() + System.Environment.NewLine +
        //                       "                           Mensaje: " + db.MjsError;
        //        ErrorCode = db.CodeError.ToString();
        //    }
        //    else
        //    {
        //        TipoError = ErrorType.Ok;
        //        ErrorCode = db.CodId.ToString();
        //        ReturnMessage = db.MjsError;
        //        ErrorLog = string.Empty;
        //    }
        //}
        /// <summary>
        /// Errores de autenticacion
        /// </summary>
        /// <param name="msj"></param>
        public void SetErrorAutt(int permiso)
        {
            string mensaje = "";
            if (permiso == 0 || permiso == 3)
                mensaje = "AUTENTICACION INVALIDA -Null Credentials";
            else if (permiso == 1)
                mensaje = "AUTENTICACION INVALIDA -USUARIO O CONTRASEÑA NO COINCIDEN";
            else if (permiso == 4)
                mensaje = "Autorización fallida, no tiene permiso al recurso";

            this.TipoError = ErrorType.Permiso;
            this.IsError = true;
            this.ErrorCode = "0";
            this.IsAuthenicated = false;
            this.ErrorLog = "Codigo de Error: " + this.ErrorCode + System.Environment.NewLine + "Mensaje: " + mensaje;
            this.ReturnMessage = "Codigo de Error: " + this.ErrorCode + ". Mensaje: " + mensaje;
        }

        public void SetErrordb(string msj)
        {
            this.TipoError = ErrorType.Validación;
            this.IsError = true;
            this.ErrorCode = "-1";
            this.ErrorLog = msj;
            this.ReturnMessage = "Hubo un error interno, póngase en contacto con soporte técnico.";
        }

        public void SetErrorDB2(ErrorDB2 msj)
        {
            this.TipoError     = ErrorType.Validación;
            this.IsError       = true;
            this.ErrorCode     = msj.CodError.ToString();
            this.ErrorLog      = msj.OVARMENSAJEERROR;
            this.ReturnMessage = msj.OVARMENSAJEERROR;
        }

        public void SetErrorApi(string msj)
        {
            this.TipoError = ErrorType.Servicio;
            this.IsError = true;
            this.ErrorCode = "404";
            this.ErrorLog = msj;
            this.ReturnMessage = "Hubo un error interno, póngase en contacto con soporte técnico.";
        }

        /// <summary>
        /// Errores de validación del modelo de datos
        /// </summary>
        public void SetErrorModel()
        {
            this.TipoError = ErrorType.Validación;
            this.IsError = true;
            this.ErrorCode = "63";
            this.ErrorLog = "Errores de validación";
            this.ReturnMessage = "Hubo un error interno, póngase en contacto con soporte técnico.";
        }
    }
}

