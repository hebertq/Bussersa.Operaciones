using Modelo.Salida;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Modelo.ClasesGenericas
{
    public class ResultadoGenerico
    {
        public int CodError;
        public string MensajeError;
        public string MensajeTecnico;
        public bool ExisteError;
        public ErrorType Status;
        public ResultadoGenerico()
        {
            CodError = 0;
            MensajeError = "Solicitud/Transacción procesada exitosamente";
            MensajeTecnico = string.Format("Fecha Hora: {0}.{1} Mensaje: {2}", DateTime.Now, Environment.NewLine, MensajeError);
            ExisteError = false;
            Status = ErrorType.Ok;
        }

        /// <summary>
        /// Actualiza la transaccón con los errores provocados por exepciones del sistema
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ex"></param>
        public void SetErrorExep(ErrorType type, Exception ex, string proc)
        {
            ExisteError = true;
            CodError = 1;
            Status = type;
            if (ex != null)
            {
                MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, ex.Message + " StackTrace: " + ex.StackTrace + " InnerException: " + ex.InnerException);
                MensajeTecnico = string.Format("Tipo de Error: {0}Proceso: {1}Mensaje: {2}Fecha Hora:{3}",
                                type + Environment.NewLine,
                                proc + System.Environment.NewLine,
                                ex.Message + Environment.NewLine,
                                DateTime.Now);

            }
        }

        public void SetErrorDB2Exep(ErrorType type, Exception ex, string proc)
        {
            ExisteError = true;
            CodError = 1;
            Status = type;
            if (ex != null)
            {
                MensajeTecnico = string.Format("Tipo de Error: {0}Codigo: {1}Proceso: {2}Mensaje: {3}Fecha Hora:{4}",
                                type + Environment.NewLine,
                                ex.HResult.ToString() + System.Environment.NewLine,
                                proc + System.Environment.NewLine,
                                ex.Message + " StackTrace: " + ex.StackTrace + " InnerException: " + ex.InnerException + Environment.NewLine,
                                DateTime.Now);

                MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, ex.Message + " StackTrace: " + ex.StackTrace + " InnerException: " + ex.InnerException);
            }
        }

        /// <summary>
        /// Actualiza la información de la transacción con los errores personalizados de base de datos
        /// </summary>
        /// <param name="db"></param>
        public void SetErrorDb(ErrorDB2 db, string proc)
        {

            if (db.CodError != 0)
            {
                ExisteError = true;
                CodError = 1;
                Status = ErrorType.Datos;
                MensajeTecnico = string.Format("Tipo de Error: {0}Codigo: {1}Proceso: {2}Mensaje: {3}Fecha Hora:{4}",
                                 ErrorType.Datos + Environment.NewLine,
                                 "1" + System.Environment.NewLine,
                                 proc + System.Environment.NewLine,
                                 db.OVARMENSAJEERROR + Environment.NewLine,
                                 DateTime.Now);
                this.MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, db.OVARMENSAJEERROR);
            }
            else
            {
                ExisteError = false;
            }
        }

        /// <summary>
        /// Actualiza la información de la transacción con los errores personalizados de los Bot
        /// </summary>
        /// <param name="db"></param>
        public void SetErrorBot(RespBoot bt, string proc)
        {
            if (bt.ResponseCode != 0 && proc != "AddTransaccionBot")
            {
                ExisteError = false;
                CodError = 0;
            }
            else if (bt.ResponseCode == 0 && proc == "AddTransaccionBot")
            {
                ExisteError = false;
                CodError = 0;
            }
            else
            {
                ExisteError = true;
                CodError = -1;
            }
            MensajeTecnico = string.Format("Tipo de Error: {0}Codigo: {1}Proceso: {2}Mensaje: {3}Fecha Hora:{4}",
                                ErrorType.Servicio + Environment.NewLine,
                                "1" + System.Environment.NewLine,
                                proc + System.Environment.NewLine,
                                bt.Mensaje + Environment.NewLine,
                                DateTime.Now);
            this.MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, bt.Mensaje);
        }

        public void SetErrorDb(ErrorMjsBd db, string proc, bool isok = false)
        {
            ExisteError = db.iserror;
            CodError = -1;
            if (db.iserror && !isok)
            {
                Status = ErrorType.Datos;
                MensajeTecnico = string.Format("Tipo de Error: {0}Codigo: {1}Proceso: {2}Mensaje: {3}Fecha Hora:{4}",
                                 ErrorType.Datos + Environment.NewLine,
                                 db.codeerror.ToString() + System.Environment.NewLine,
                                 proc + System.Environment.NewLine,
                                 db.mjserror + Environment.NewLine,
                                 DateTime.Now);

                this.MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, db.mjserror);
            }
            else if(isok)
                this.MensajeError = db.mjserror;
        }
        public void SetErrorValidation(InformacionTransaccion info, string proc)
        {
            Status = ErrorType.Validación;
            if (info.IsError)
            {
                ExisteError = true;
                CodError = 1;
                MensajeTecnico = string.Format("{0}Proceso: {1}",
                                info.ReturnMessage + Environment.NewLine, proc);
                this.MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, info.ErrorLog);
            }
            else
            {
                ExisteError = false;
            }
        }

        // <summary>
        /// Actualiza la información de la transacción con los errores personalizados de base de datos
        /// </summary>
        /// <param name="db"></param>
        public void SetError(string ErrorMsj, ErrorType TypeError, string proc)
        {
            Status = TypeError;
            ExisteError = true;
            CodError = 1;
            MensajeTecnico = string.Format("Tipo de Error: {0}Codigo: {1}Proceso: {2}Mensaje: {3}Fecha Hora:{4}",
                                TypeError + Environment.NewLine,
                                "1" + System.Environment.NewLine,
                                proc + System.Environment.NewLine,
                                ErrorMsj + Environment.NewLine,
                                DateTime.Now);

            this.MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, ErrorMsj);
        }

        // <summary>
        /// Actualiza la información de la transacción con los errores personalizados de base de datos
        /// </summary>
        /// <param name="db"></param>
        public void SetErrorMsj(string ErrorMsj, string proc)
        {
            Status = ErrorType.Datos;
            ExisteError = true;
            CodError = 1;

            this.MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, ErrorMsj);
            MensajeTecnico = string.Format("Tipo de Error: {0}Codigo: {1}Proceso: {2}Mensaje: {3}Fecha Hora:{4}",
                             ErrorType.Datos + Environment.NewLine,
                             "1" + System.Environment.NewLine,
                             proc + System.Environment.NewLine,
                             ErrorMsj + Environment.NewLine,
                             DateTime.Now);
        }

        public void SetErrHost(ResponseData resp)
        {
            Status = ErrorType.Servicio;
            ExisteError = false;
            CodError    = 0;

            this.MensajeError   = resp.errors.ToString();
            this.MensajeTecnico = resp.errors.ToString();

            if (!resp.sucess)
            {
                ExisteError = true;
                CodError    = 1;
            }
        }

        // <summary>
        /// Actualiza la información de la transacción con los errores personalizados de base de datos
        /// </summary>
        /// <param name="db"></param>
        public void SetErrorApi(string ErrorMsj, string proc)
        {
            ExisteError = true;
            CodError = 1;
            Status = ErrorType.Servicio;
            this.MensajeError = string.Format("Proceso: {0}Mensaje: {1}", proc + System.Environment.NewLine, ErrorMsj);
            MensajeTecnico = string.Format("Tipo de Error: {0}Codigo: {1}Proceso: {2}Mensaje: {3}Fecha Hora:{4}",
                             ErrorType.Servicio + Environment.NewLine,
                             "1" + System.Environment.NewLine,
                             proc + System.Environment.NewLine,
                             ErrorMsj + Environment.NewLine,
                             DateTime.Now);
        }
    }
}

