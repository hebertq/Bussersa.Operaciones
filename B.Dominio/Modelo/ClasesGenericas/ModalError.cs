

namespace Modelo.ClasesGenericas
{
    public class ModalError
    {
        public tipoError ErrorType { set; get; } = tipoError.info;
        public bool IsNotifica { set; get; } = false;
        public string mensaje { set; get; } = "";

        public void setErrorInfo(string msj)
        {
            IsNotifica = true;
            mensaje = msj;
            ErrorType = tipoError.info;
        }

        public void setError(string msj)
        {
            IsNotifica = true;
            mensaje = msj;
            ErrorType = tipoError.error;
        }

        public void setErrorWarn(string msj)
        {
            IsNotifica = true;
            mensaje = msj;
            ErrorType = tipoError.warnig;
        }

        public void setNoError()
        {
            IsNotifica = false;
        }
    }



    public enum tipoError
    {
        info = 1,
        error = 2,
        warnig = 3
    }
}

