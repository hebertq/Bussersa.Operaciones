using Modelo.ClasesGenericas;
using Modelo.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace HostService.Clases
{
    public partial class OdooService
    {
        public async Task<IListResponse<LoteNominaWhatsappItemDto>> GetLotesNominaWhatsapp()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod()?.Name}";
            var response = new ListResponse<LoteNominaWhatsappItemDto>();
            try
            {
                var requestUrl = CreateRequestUri("Operaciones/GetLotesNominaWhatsapp");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                        response.Model = JsonConvert.DeserializeObject<List<LoteNominaWhatsappItemDto>>(reg.detail.ToString()!) ?? new();
                    else if (!reg.sucess)
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al obtener lotes.", ErrorType.Servicio, "");
                }
                else
                {
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetError(ex.Message, ErrorType.Servicio, metodo);
            }
            return response;
        }

        public async Task<ISingleResponse<ConsultaEnvioWhatsappResponseDto>> ConsultarEnvioWhatsappNomina(ConsultaEnvioWhatsappRequestDto request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod()?.Name}";
            var response = new SingleResponse<ConsultaEnvioWhatsappResponseDto>();
            try
            {
                var requestUrl = CreateRequestUri("Operaciones/ConsultarEnvioWhatsappNomina");
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                        response.Model = JsonConvert.DeserializeObject<ConsultaEnvioWhatsappResponseDto>(reg.detail.ToString()!) ?? new();
                    else if (!reg.sucess)
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al consultar colaboradores.", ErrorType.Servicio, "");
                }
                else
                {
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetError(ex.Message, ErrorType.Servicio, metodo);
            }
            return response;
        }

        public async Task<IResponse> ActualizarTelefonoColaboradorWhatsapp(ActualizarTelefonoColaboradorRequestDto request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod()?.Name}";
            var response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Operaciones/ActualizarTelefonoColaboradorWhatsapp");
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (!reg.sucess)
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al actualizar teléfono.", ErrorType.Servicio, "");
                }
                else
                {
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetError(ex.Message, ErrorType.Servicio, metodo);
            }
            return response;
        }

        public async Task<ISingleResponse<ConfigCanalWhatsappDto>> GetConfigCanalWhatsapp()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod()?.Name}";
            var response = new SingleResponse<ConfigCanalWhatsappDto>();
            try
            {
                var requestUrl = CreateRequestUri("Operaciones/GetConfigCanalWhatsapp");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                        response.Model = JsonConvert.DeserializeObject<ConfigCanalWhatsappDto>(reg.detail.ToString()!) ?? new();
                    else if (!reg.sucess)
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al leer configuración.", ErrorType.Servicio, "");
                }
                else
                {
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetError(ex.Message, ErrorType.Servicio, metodo);
            }
            return response;
        }

        public async Task<IResponse> GuardarConfigCanalWhatsapp(ConfigCanalWhatsappDto request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod()?.Name}";
            var response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Operaciones/GuardarConfigCanalWhatsapp");
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (!reg.sucess)
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al guardar configuración.", ErrorType.Servicio, "");
                }
                else
                {
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetError(ex.Message, ErrorType.Servicio, metodo);
            }
            return response;
        }

        public async Task<IListResponse<ResultadoItemEnvioWhatsappDto>> EnviarWhatsappIndividualNomina(EnviarWhatsappIndividualRequestDto request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod()?.Name}";
            var response = new ListResponse<ResultadoItemEnvioWhatsappDto>();
            try
            {
                var requestUrl = CreateRequestUri("Operaciones/EnviarWhatsappIndividualNomina");
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                        response.Model = JsonConvert.DeserializeObject<List<ResultadoItemEnvioWhatsappDto>>(reg.detail.ToString()!) ?? new();
                    else if (!reg.sucess)
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error en el envío.", ErrorType.Servicio, "");
                }
                else
                {
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetError(ex.Message, ErrorType.Servicio, metodo);
            }
            return response;
        }

        public async Task<ISingleResponse<ResultadoItemEnvioWhatsappDto>> EnviarWhatsappConsolidadoSupervisor(EnviarConsolidadoSupervisorRequestDto request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod()?.Name}";
            var response = new SingleResponse<ResultadoItemEnvioWhatsappDto>();
            try
            {
                var requestUrl = CreateRequestUri("Operaciones/EnviarWhatsappConsolidadoSupervisor");
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                        response.Model = JsonConvert.DeserializeObject<ResultadoItemEnvioWhatsappDto>(reg.detail.ToString()!) ?? new();
                    else if (!reg.sucess)
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al enviar consolidado.", ErrorType.Servicio, "");
                }
                else
                {
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetError(ex.Message, ErrorType.Servicio, metodo);
            }
            return response;
        }
    }
}
