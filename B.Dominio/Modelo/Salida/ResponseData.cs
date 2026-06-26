using Modelo.ClasesGenericas;
using Modelo.Interfaces;
using System;

namespace Modelo.Salida
{
    public class ResponseData
    {
        public ResponseData() { }
        public ResponseData(Guid idg, int code, string tit, object error, object salida)
        {
            title = tit;
            id = idg;
            var statuscode = code switch
            {
                400 => EResponse.BadRequest,
                404 => EResponse.NotFound,
                500 => EResponse.InternalServerError,
                401 => EResponse.Unauthorized,
                _ => EResponse.UnSuccess
            };

            status = statuscode.ToString();
            sucess = false;
            errors = error;
            detail = salida;
        }
        public ResponseData(EResponse code, object data, object dataresp, string tit)
        {
            status = code.ToString();
            title = tit;
            id = Guid.NewGuid();
            var response = data as IResponse;

            if (response != null)
            {
                response.SetObjet();
                errors = response.Respuesta.MensajeError;
                sucess = !response.Respuesta.ExisteError;
                title = sucess ? title : "Excepción de error de la aplicación";

                if (response.Respuesta.Status == ErrorType.Validación)
                    status = EResponse.ValidationError.ToString();
                if (response.Respuesta.Status == ErrorType.Servicio)
                    status = EResponse.UnSuccess.ToString();
                if (response.Respuesta.Status == ErrorType.Datos)
                    status = EResponse.UnexpectedError.ToString();
                else if (response.ObjModel != null)
                {
                    detail = response.ObjModel;
                }
            }
            else
            {

                if (dataresp != null)
                {
                    detail = dataresp;
                    errors = "Solicitud/Transacción procesada exitosamente";
                    sucess = true;
                }
                else
                {
                    errors = data;
                    sucess = false;
                    title = "Excepción de error de la aplicación";
                }
            }
        }
        public Guid id { set; get; }
        public string title { set; get; } = "";
        public string status { set; get; } = EResponse.OK.ToString();
        public bool sucess { set; get; } = true;
        public object errors { set; get; }
        public object detail { set; get; } = null;
        //public string type { set; get; } = "";
    }
}

