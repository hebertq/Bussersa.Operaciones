using Modelo.ClasesGenericas;
using System.Runtime.Serialization;

namespace Modelo
{
    [DataContract]
    public class Message<T>
    {
        public Message()
        {
            IsSuccess = true;
            ReturnMessage = "Ok";
        }

        [DataMember(Name = "IsSuccess")]
        public bool IsSuccess { get; set; }

        [DataMember(Name = "ReturnMessage")]
        public string ReturnMessage { get; set; }

        [DataMember(Name = "Data")]
        public T Data { get; set; }

        public void SetErrorExep(CoreException ex, string usuario)
        {
            IsSuccess = false;
            if (ex != null)
            {
                this.ReturnMessage = string.Format("Usuario: {0}, Tipo de Error: ", usuario) + ErrorType.Servicio + System.Environment.NewLine +
                                "                           Codigo: " + ex.HResult.ToString() + System.Environment.NewLine +
                                "                           Aplicación: " + ex.Source + System.Environment.NewLine +
                                "                           Mensaje: " + ex.Message;
            }
        }

        public void SetErroAcces(string msj, string usuario, string codigo)
        {
            IsSuccess = false;
            this.ReturnMessage = string.Format("Usuario: {0}, Tipo de Error: ", usuario) + ErrorType.Servicio + System.Environment.NewLine +
                                "                           Codigo: " + codigo + System.Environment.NewLine +
                                "                           Aplicación: " + "Api" +
                                "                           Mensaje: " + msj;

        }
    }
}
