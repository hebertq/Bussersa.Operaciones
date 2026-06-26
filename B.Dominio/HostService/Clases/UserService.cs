using Blazored.LocalStorage;
using HostService.ClasesGenericas;
using HostService.Interfaces;
using Modelo.Admin;
using Modelo.ClasesGenericas;
using Modelo.Interfaces;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Utilidades.Interfaces;

namespace HostService.Clases
{
    public class UserService: ServiceHost, IUserService
    {
        public UserService(IUtilidades _Util, ILocalStorageService ls) : base(_Util,ls) { }
        public async Task<ISingleResponse<UserWithToken>> LoginAsync(UserLogin request)
        {
            ISingleResponse<UserWithToken> response = new SingleResponse<UserWithToken>();
            try
            {                
                var requestUrl = CreateRequestUri("Auth/Login");
                var datamodel = new { model = request };
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model =  _Util.ObtenerRegistro<UserWithToken>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(),ErrorType.Servicio,"");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, MethodBase.GetCurrentMethod().ToString());

            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, MethodBase.GetCurrentMethod().ToString());
            }

            return response;
        }

        public async Task<ISingleResponse<User>> GetUserByAccessToken(RefreshRequest request)
        {
            ISingleResponse<User> response = new SingleResponse<User>();
            try
            {
                var datamodel = new { refreshRequest = request };
                var requestUrl = CreateRequestUri("Auth/GetUserByAccessToken");
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerRegistro<User>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, MethodBase.GetCurrentMethod().ToString());

            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, MethodBase.GetCurrentMethod().ToString());
            }

            return response;
        }       
    }
}
