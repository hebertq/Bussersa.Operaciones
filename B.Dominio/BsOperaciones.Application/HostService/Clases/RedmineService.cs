using Blazored.LocalStorage;
using HostService.ClasesGenericas;
using HostService.Interfaces;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Utilidades.Interfaces;

namespace HostService.Clases
{
    public class RedmineService : ServiceHost, IRedmineService
    {
        public RedmineService(IUtilidades _Util, ILocalStorageService ls) : base(_Util, ls) { }
    
        public async Task<IResponse> CrearSolicituBonosAsync(HoraEntrada modelo,string cliente)
        {
            IResponse response = new ErrorResponse();
            var projectId = "rrhh-bs";
            var trackerId = 11;
            string metodo = $"RedmineService_{MethodBase.GetCurrentMethod().Name}";
            var peticion = new RedmineRequest
            {
                issue = new RedmineIssue
                {
                    project_id = projectId,
                    tracker_id = trackerId,
                    start_date = modelo.fecha,
                    subject = $"Solicitud de bono para empelado: {modelo.nombre}",
                    description = BuildDescription(modelo),
                    custom_fields = new List<RedmineCustomField>
                    {
                        new() { id = 49, value = modelo.id.ToString() },
                        new() { id = 12, value = modelo.bono.ToString()},
                        new() { id = 1, value  = cliente },
                        new() { id = 50, value  = modelo.nombre }
                    }
                }
            };
            try
            {
                var requestUrl = CreateRequestUriRedmine("issues.json");
                var registro = await PostAsyncRedmine(requestUrl, peticion);
                if (!registro.IsSuccess)
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }           

            return response;
        }
        private string BuildDescription(HoraEntrada m) =>
            $"Numero de empelado: {m.id}\n" +
            $"Nombre completo: {m.nombre}\n" +
            $"Solicita bono de cumplimiento el dia: {m.fecha}\n";
    }
}
