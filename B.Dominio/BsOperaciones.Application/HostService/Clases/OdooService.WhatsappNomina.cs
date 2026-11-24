using Modelo.ClasesGenericas;
using Modelo.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HostService.Clases
{
    public partial class OdooService
    {
        private const string BotWhatsappRelayBaseUrl = "https://bot.bussersa.com";
        private const string BotOperationsApiKey = "BussersaSecureApiKey2026*";
        private static readonly HttpClient _botRelayHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(35) };

        private class BotEstadoCanalResp
        {
            public bool activo { get; set; }
            public string? phone_number_id { get; set; }
            public string? origen { get; set; }
        }

        private class BotEnviarDocResp
        {
            public bool success { get; set; }
            public string? media_id { get; set; }
            public string? message_id { get; set; }
            public string? detail { get; set; }
        }

        private static async Task<BotEstadoCanalResp?> ConsultarEstadoCanalBotAsync()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{BotWhatsappRelayBaseUrl}/whatsapp/estado-canal");
                req.Headers.Add("x-api-key", BotOperationsApiKey);
                using var resp = await _botRelayHttp.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<BotEstadoCanalResp>(json);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<BotEnviarDocResp> EnviarDocumentoPorRelayBotAsync(
            string telefono,
            string fileName,
            string pdfBase64,
            string caption,
            string recipientName,
            string periodLabel,
            ConfigCanalWhatsappDto? cfg = null,
            string? overridePhoneId = null,
            string? overrideAccessToken = null)
        {
            try
            {
                var payload = new
                {
                    telefono,
                    filename = fileName,
                    pdf_base64 = pdfBase64,
                    caption,
                    recipient_name = recipientName,
                    period_label = periodLabel,
                    usar_plantilla_fallback = cfg?.UsarPlantillaFallback ?? false,
                    template_name = cfg?.TemplateName ?? string.Empty,
                    template_lang = cfg?.TemplateLang ?? "es",
                    custom_phone_number_id = !string.IsNullOrWhiteSpace(overridePhoneId) ? overridePhoneId : (cfg?.PhoneNumberId ?? string.Empty),
                    custom_access_token = !string.IsNullOrWhiteSpace(overrideAccessToken) ? overrideAccessToken : (cfg?.AccessToken ?? string.Empty),
                    api_version = cfg?.ApiVersion ?? "v20.0"
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, $"{BotWhatsappRelayBaseUrl}/whatsapp/enviar-documento")
                {
                    Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                };
                req.Headers.Add("x-api-key", BotOperationsApiKey);

                using var resp = await _botRelayHttp.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    return new BotEnviarDocResp
                    {
                        success = false,
                        detail = $"Error en servicio WhatsApp ({(int)resp.StatusCode}): {(body.Length > 180 ? body.Substring(0, 180) : body)}"
                    };
                }

                return JsonConvert.DeserializeObject<BotEnviarDocResp>(body) ?? new BotEnviarDocResp
                {
                    success = false,
                    detail = "Respuesta vacía del servicio WhatsApp."
                };
            }
            catch (Exception ex)
            {
                return new BotEnviarDocResp
                {
                    success = false,
                    detail = $"No fue posible conectar con bot.bussersa.com: {ex.Message}"
                };
            }
        }

        private async Task RegistrarResultadoRelayEnApiAsync(RegistrarResultadoRelayWhatsappDto dto)
        {
            try
            {
                var url = CreateRequestUri("Operaciones/RegistrarResultadoRelayWhatsapp");
                await PostAsync(url, dto);
            }
            catch
            {
            }
        }

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
                var taskApi = PostAsync(requestUrl, request);
                var taskBot = ConsultarEstadoCanalBotAsync();
                await Task.WhenAll(taskApi, taskBot);

                var registro = taskApi.Result;
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                    {
                        response.Model = JsonConvert.DeserializeObject<ConsultaEnvioWhatsappResponseDto>(reg.detail.ToString()!) ?? new();
                        if (!response.Model.TieneApiConfigurada && taskBot.Result?.activo == true)
                        {
                            response.Model.TieneApiConfigurada = true;
                            response.Model.OrigenCredenciales = taskBot.Result.origen ?? "AWS_SSM_BOT";
                        }
                    }
                    else if (!reg.sucess)
                    {
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al consultar colaboradores.", ErrorType.Servicio, "");
                    }
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
                var taskApi = GetAsync(requestUrl);
                var taskBot = ConsultarEstadoCanalBotAsync();
                await Task.WhenAll(taskApi, taskBot);

                var registro = taskApi.Result;
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                    {
                        var cfg = JsonConvert.DeserializeObject<ConfigCanalWhatsappDto>(reg.detail.ToString()!) ?? new();
                        if (!cfg.TieneCredencialesApi && taskBot.Result?.activo == true)
                        {
                            cfg.TieneCredencialesApi = true;
                            cfg.OrigenCredenciales = taskBot.Result.origen ?? "AWS_SSM_BOT";
                            if (string.IsNullOrWhiteSpace(cfg.PhoneNumberId) && !string.IsNullOrWhiteSpace(taskBot.Result.phone_number_id))
                                cfg.PhoneNumberId = taskBot.Result.phone_number_id!;
                        }
                        response.Model = cfg;
                    }
                    else if (!reg.sucess)
                    {
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al leer configuración.", ErrorType.Servicio, "");
                    }
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
                bool usarRelayBot = string.Equals(request.ModoCanal, "META_API", StringComparison.OrdinalIgnoreCase);
                string modoOriginal = request.ModoCanal;
                if (usarRelayBot)
                {
                    request.ModoCanal = "PREPARAR_RELAY";
                }

                var requestUrl = CreateRequestUri("Operaciones/EnviarWhatsappIndividualNomina");
                var registro = await PostAsync(requestUrl, request);
                request.ModoCanal = modoOriginal;

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                    {
                        var items = JsonConvert.DeserializeObject<List<ResultadoItemEnvioWhatsappDto>>(reg.detail.ToString()!) ?? new();
                        if (usarRelayBot && items.Count > 0)
                        {
                            var cfgResp = await GetConfigCanalWhatsapp();
                            var cfg = cfgResp.Model;

                            foreach (var item in items)
                            {
                                var colabSrc = request.Colaboradores.FirstOrDefault(x => x.EmployeeId == item.EmployeeId);
                                string periodLbl = colabSrc != null
                                    ? $"{colabSrc.FechaDesde:dd/MM/yyyy} al {colabSrc.FechaHasta:dd/MM/yyyy}"
                                    : "Período actual";

                                if (string.IsNullOrWhiteSpace(item.TelefonoNormalizado) || item.TelefonoNormalizado.Length < 10)
                                {
                                    item.Exito = false;
                                    item.EstadoEnvio = "ERROR";
                                    item.DetalleRespuesta = "Colaborador sin número de WhatsApp válido.";
                                    await RegistrarResultadoRelayEnApiAsync(new RegistrarResultadoRelayWhatsappDto
                                    {
                                        EnvioId = item.EnvioId,
                                        Exito = false,
                                        CanalEnvio = "META_API",
                                        EstadoEnvio = "ERROR",
                                        DetalleRespuesta = item.DetalleRespuesta
                                    });
                                    continue;
                                }

                                if (string.IsNullOrWhiteSpace(item.PdfBase64))
                                {
                                    item.Exito = false;
                                    item.EstadoEnvio = "ERROR";
                                    item.DetalleRespuesta = "No se obtuvo el archivo PDF para adjuntar.";
                                    continue;
                                }

                                var botRes = await EnviarDocumentoPorRelayBotAsync(
                                    item.TelefonoNormalizado,
                                    item.NombreArchivoPdf,
                                    item.PdfBase64,
                                    item.MensajeResumen,
                                    item.NombreDestinatario,
                                    periodLbl,
                                    cfg,
                                    item.RelayPhoneNumberId,
                                    item.RelayAccessToken);

                                item.Exito = botRes.success;
                                item.CanalUsado = "META_API";
                                item.EstadoEnvio = botRes.success ? "ENVIADO_API" : "ERROR";
                                item.WhatsappMessageId = botRes.message_id;
                                item.DetalleRespuesta = botRes.detail ?? string.Empty;

                                await RegistrarResultadoRelayEnApiAsync(new RegistrarResultadoRelayWhatsappDto
                                {
                                    EnvioId = item.EnvioId,
                                    Exito = botRes.success,
                                    CanalEnvio = "META_API",
                                    EstadoEnvio = item.EstadoEnvio,
                                    MediaId = botRes.media_id,
                                    MessageId = botRes.message_id,
                                    DetalleRespuesta = item.DetalleRespuesta
                                });
                            }
                        }

                        response.Model = items;
                    }
                    else if (!reg.sucess)
                    {
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error en el envío.", ErrorType.Servicio, "");
                    }
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
                bool usarRelayBot = string.Equals(request.ModoCanal, "META_API", StringComparison.OrdinalIgnoreCase);
                string modoOriginal = request.ModoCanal;
                if (usarRelayBot)
                {
                    request.ModoCanal = "PREPARAR_RELAY";
                }

                var requestUrl = CreateRequestUri("Operaciones/EnviarWhatsappConsolidadoSupervisor");
                var registro = await PostAsync(requestUrl, request);
                request.ModoCanal = modoOriginal;

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                    {
                        var item = JsonConvert.DeserializeObject<ResultadoItemEnvioWhatsappDto>(reg.detail.ToString()!) ?? new();
                        if (usarRelayBot)
                        {
                            var cfgResp = await GetConfigCanalWhatsapp();
                            var botRes = await EnviarDocumentoPorRelayBotAsync(
                                item.TelefonoNormalizado,
                                item.NombreArchivoPdf,
                                item.PdfBase64 ?? string.Empty,
                                item.MensajeResumen,
                                item.NombreDestinatario,
                                $"{request.FechaDesde:dd/MM/yyyy} al {request.FechaHasta:dd/MM/yyyy}",
                                cfgResp.Model,
                                item.RelayPhoneNumberId,
                                item.RelayAccessToken);

                            item.Exito = botRes.success;
                            item.CanalUsado = "META_API";
                            item.EstadoEnvio = botRes.success ? "ENVIADO_API" : "ERROR";
                            item.WhatsappMessageId = botRes.message_id;
                            item.DetalleRespuesta = botRes.detail ?? string.Empty;

                            await RegistrarResultadoRelayEnApiAsync(new RegistrarResultadoRelayWhatsappDto
                            {
                                EnvioId = item.EnvioId,
                                Exito = botRes.success,
                                CanalEnvio = "META_API",
                                EstadoEnvio = item.EstadoEnvio,
                                MediaId = botRes.media_id,
                                MessageId = botRes.message_id,
                                DetalleRespuesta = item.DetalleRespuesta
                            });
                        }
                        response.Model = item;
                    }
                    else if (!reg.sucess)
                    {
                        response.Respuesta.SetError(reg.errors?.ToString() ?? "Error al enviar consolidado.", ErrorType.Servicio, "");
                    }
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
