using Blazored.LocalStorage;
using Modelo;
using Modelo.ClasesGenericas;
using Modelo.Salida;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using Utilidades.Interfaces;

namespace HostService.ClasesGenericas
{
    public abstract class ServiceHost
    {
        private  HttpClient _httpClient;
        private   ILocalStorageService _localStorageService { get; }
        protected IUtilidades _Util;
        public ServiceHost(IUtilidades util, ILocalStorageService ls)
        {         
            _localStorageService = ls;
            _Util = util;           
        }
        public async Task<Message<ResponseData>> GetAsync(Uri requestUrl)
        {
            string metodo = $"HttpClient_{MethodBase.GetCurrentMethod().Name}_{requestUrl}";
            _httpClient = new HttpClient();
            await addHeaders();
            Message<ResponseData> Result = new Message<ResponseData>();
            try
            {
                using (HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        response.EnsureSuccessStatusCode();
                        var data = await response.Content.ReadAsStringAsync();
                        Result.Data = _Util.ObtenerRegistro<ResponseData>(data);
                    }
                    else
                    {
                        string status = response.StatusCode.ToString();
                        Result.SetErroAcces("Error al aceder a " + requestUrl.ToString(), metodo, status);
                    }
                }
            }
            catch (CoreException ex)
            {
                Result.SetErrorExep(ex, metodo);
            }

            return Result;
        }
        public async Task<Message<ResponseData>> GetAsync<T>(Uri requestUrl,T content)
        {
            string metodo = $"HttpClient_{MethodBase.GetCurrentMethod().Name}_{requestUrl}";
            _httpClient = new HttpClient();
            await addHeaders();
            Message<ResponseData> Result = new Message<ResponseData>();
            try
            {
                var requestContent = _Util.CreateHttpContent(content);
                var request        = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Content    = requestContent;

                using (HttpResponseMessage response = await _httpClient.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        response.EnsureSuccessStatusCode();
                        var data = await response.Content.ReadAsStringAsync();
                        Result.Data = _Util.ObtenerRegistro<ResponseData>(data);
                    }
                    else
                    {
                        string status = response.StatusCode.ToString();
                        Result.SetErroAcces("Error al aceder a " + requestUrl.ToString(), metodo, status);
                    }
                }               
            }
            catch (CoreException ex)
            {
                Result.SetErrorExep(ex, metodo);
            }
            
            return await Task.FromResult(Result);
        }
    

        /// <summary>
        /// Common method for making POST calls
        /// </summary>
        public async Task<Message<ResponseData>> PostAsync<T>(Uri requestUrl, T content)
        {
            string metodo = $"HttpClient_{MethodBase.GetCurrentMethod().Name}_{requestUrl}";
            _httpClient = new HttpClient();
            await addHeaders();
            Message<ResponseData> Result = new Message<ResponseData>();
            try
            {                   
                using (HttpResponseMessage response = await _httpClient.PostAsync(requestUrl.ToString(), _Util.CreateHttpContent(content)))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        response.EnsureSuccessStatusCode();
                        var data = await response.Content.ReadAsStringAsync();
                        Result.Data = _Util.ObtenerRegistro<ResponseData>(data);
                    }
                    else
                    {
                        string status = response.StatusCode.ToString();
                        string errorBody = string.Empty;
                        try
                        {
                            errorBody = await response.Content.ReadAsStringAsync();
                        }
                        catch { }
                        Result.SetErroAcces("Error al aceder a " + requestUrl.ToString() + " - Detalle: " + errorBody, metodo, status);
                    }
                }                               
            }
            catch (CoreException ex)
            {
                Result.SetErrorExep(ex, metodo);
            }

            return Result;
        }

        public async Task<Message<ResponseData>> DeleteAsync(Uri requestUrl)
        {
            string metodo = $"HttpClient_{MethodBase.GetCurrentMethod().Name}_{requestUrl}";
            _httpClient = new HttpClient();
            await addHeaders();
            Message<ResponseData> Result = new Message<ResponseData>();
            try
            {
                using (HttpResponseMessage response = await _httpClient.DeleteAsync(requestUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        response.EnsureSuccessStatusCode();
                        var data = await response.Content.ReadAsStringAsync();
                        Result.Data = _Util.ObtenerRegistro<ResponseData>(data);
                    }
                    else
                    {
                        string status = response.StatusCode.ToString();
                        Result.SetErroAcces("Error al aceder a " + requestUrl.ToString(), metodo, status);
                    }
                }
            }
            catch (CoreException ex)
            {
                Result.SetErrorExep(ex, metodo);
            }

            return Result;
        }

        public async Task<Message<bool>> PostAsyncRedmine<TRequest>(Uri requestUrl, TRequest data)
        {
            string metodo = $"HttpClient_{MethodBase.GetCurrentMethod().Name}_{requestUrl}";
            Message<bool> Result = new Message<bool>();
            _httpClient = new HttpClient();
            addRedmineHeaders();
            try
            {
                var response = await _httpClient.PostAsJsonAsync(requestUrl, data);

                if (response.IsSuccessStatusCode) // Captura 200, 201, 204
                {
                    Result.Data = true;
                }
                else
                {
                    // Leemos el cuerpo del error (Redmine suele enviar detalles aquí)
                    var errorJson = await response.Content.ReadAsStringAsync();
                    string status = ((int)response.StatusCode).ToString();

                    // Error de validación (422) o de permisos (401/403)
                    Result.SetErroAcces($"Redmine API Error {status}: {errorJson}", metodo, status);
                    Result.Data = false;
                }
            }
            catch (CoreException ex) // Cambié a Exception general por si falla la red
            {
                Result.SetErrorExep(ex, metodo);
                Result.Data = false;
            }

            return Result; // No necesitas Task.FromResult aquí si el método ya es async
        }

        public Uri CreateRequestUri(string relativePath, string queryString = "")
        {
            string path = string.Format(System.Globalization.CultureInfo.InvariantCulture, relativePath);
            var endpoint = new Uri(new Uri(_Util.BaseUrlApiLog), path);
            var uriBuilder = new UriBuilder(endpoint);
            uriBuilder.Query = queryString;
            return uriBuilder.Uri;
        }

        public Uri CreateRequestUriRedmine(string relativePath, string queryString = "")
        {
            string path = string.Format(System.Globalization.CultureInfo.InvariantCulture, relativePath);
            var endpoint = new Uri(new Uri(_Util.BaseUrlRedmine), path);
            var uriBuilder = new UriBuilder(endpoint);
            uriBuilder.Query = queryString;
            return uriBuilder.Uri;
        }

        private async Task addHeaders()
        {
           
            _httpClient.DefaultRequestHeaders.Remove("userIP");
            _httpClient.DefaultRequestHeaders.Add("userIP", "192.168.1.1");
            _httpClient.BaseAddress = new Uri(_Util.BaseUrlApiLog);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.ConnectionClose = false;

            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", "BussersaSecureApiKey2026*");

            if (_localStorageService != null)
            {
                var token = await _localStorageService.GetItemAsync<string>("accessToken");
                if (token != null)
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private void addRedmineHeaders()
        {
            _httpClient.BaseAddress = new Uri(_Util.BaseUrlRedmine); ; // Tu URL de Redmine
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // IMPORTANTE: Redmine usa este Header personalizado para la autenticación
            _httpClient.DefaultRequestHeaders.Remove("X-Redmine-API-Key");
            _httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", _Util.ApiKeyRedmine);
        }
    }
}




