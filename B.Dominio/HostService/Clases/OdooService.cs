using Blazored.LocalStorage;
using HostService.ClasesGenericas;
using HostService.Interfaces;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;
using Modelo.Report;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Utilidades.Interfaces;

namespace HostService.Clases
{
    public class OdooService : ServiceHost, IOdooService
    {
        public OdooService(IUtilidades _Util, ILocalStorageService ls) : base(_Util, ls) { }

        public async Task<IListResponse<PayrollMonthRecord>> GetAllNominaMensualCierre(int AnioMes)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<PayrollMonthRecord> response = new ListResponse<PayrollMonthRecord>();
            try
            {
                var requestUrl = CreateRequestUri($"OdQuery/GetAllNominaCierreMensual/{AnioMes}");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<PayrollMonthRecord>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }
        public async Task<IListResponse<Combos>> GetAllCombosHost(string combo)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<Combos> response = new ListResponse<Combos>();
            try
            {
                var requestUrl = CreateRequestUri($"OdQuery/GetAllCombos/{combo}");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<Combos>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }
        public async Task<IListResponse<ReporteCierreMarcadas>> GetCierreGlobal(int Anio, int Mes)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<ReporteCierreMarcadas> response = new ListResponse<ReporteCierreMarcadas>();
            try
            {
                var requestUrl = CreateRequestUri($"OdQuery/GetCierreGlobal/Anio/{Anio}/Mes/{Mes}");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<ReporteCierreMarcadas>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IListResponse<ReporteCierreMarcadas>> GetCierreFacturaMes(int Anio, int Mes, int Operacion)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<ReporteCierreMarcadas> response = new ListResponse<ReporteCierreMarcadas>();
            try
            {
                var requestUrl = CreateRequestUri($"OdQuery/GetCierreFacturaMes/Cliente/{Operacion}/Anio/{Anio}/Mes/{Mes}");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<ReporteCierreMarcadas>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IListResponse<ReporteCierreMarcadas>> GetCierreFacturaRango(int Operacion, DateTime Inicio, DateTime Fin)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<ReporteCierreMarcadas> response = new ListResponse<ReporteCierreMarcadas>();
            try
            {
                // Formateamos las fechas a yyyy-MM-dd para evitar los slashes '/'
                string fchaInicio = Inicio.ToString("yyyy-MM-dd");
                string fchaFin = Fin.ToString("yyyy-MM-dd");

                // Construimos la URL con las fechas ya formateadas
                var requestUrl = CreateRequestUri($"OdQuery/GetCierreFacturaRango/Cliente/{Operacion}/Inicio/{fchaInicio}/Fin/{fchaFin}");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<ReporteCierreMarcadas>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IListResponse<EmpleadosActivos>> GetAllEmpleadosActivos()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<EmpleadosActivos> response = new ListResponse<EmpleadosActivos>();
            try
            {
                var requestUrl = CreateRequestUri($"OdQuery/GetAllEmpleadosActivos");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<EmpleadosActivos>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }


        public async Task<IListResponse<DiaFeriado>> GetAllDiasFeriados()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<DiaFeriado>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GetAllDiasFeriados");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<DiaFeriado>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }
            return response;
        }

        public async Task<IListResponse<SeveranceDetail>> GetAllPendingSeverance()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<SeveranceDetail>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GetPendingSeverance");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<SeveranceDetail>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }
            return response;
        }

        public async Task<IResponse> Crudferiado(DiaFeriado model, int op)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"OCommand/Crudferiado/{op}");
                var registro = await PostAsync(requestUrl, model);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.SetErrHost(reg);
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IResponse> CreateSettlementPayroll(DateTime Inicio, DateTime Fin, string Nombre, List<SeveranceDetail> Param)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var model = new LiquidacionRequest
                {
                    Inicio = Inicio,
                    Fin = Fin,
                    Nombre = Nombre,
                    Param = Param
                };

                var requestUrl = CreateRequestUri($"OCommand/CreateSettlementPayroll");
                var registro = await PostAsync(requestUrl, model);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.SetErrHost(reg);
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }
        
        public async Task<ISingleResponse<DiasTrabajados>> AddMarcadasId(DiasTrabajados param, int operacion)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            ISingleResponse<DiasTrabajados> response = new SingleResponse<DiasTrabajados>();
            try
            {
                var requestUrl = CreateRequestUri($"OCommand/AddMarcadasId/{operacion}");
                var registro = await PostAsync(requestUrl, param);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess && reg.detail != null)
                        response.Model = _Util.ObtenerRegistro<DiasTrabajados>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }
        public async Task<IResponse> AddAllMarcadas(List<HoraEntrada> model, int operacion)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"OCommand/AddAllMarcadas/{operacion}");
                var registro = await PostAsync(requestUrl, model);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.SetErrHost(reg);
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IResponse> AddCierreMarcadaFacturar(List<DiasTrabajadosAreas> model, int operacion)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"OCommand/CierreMarcadasXFacturar/{operacion}");
                var registro = await PostAsync(requestUrl, model);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.SetErrHost(reg);
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<ISingleResponse<DiasTrabajados>> GetMarcadaId(int idmarcada)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new SingleResponse<DiasTrabajados>();
            try
            {
                var requestUrl = CreateRequestUri($"OdQuery/GetMarcadaId/{idmarcada}");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerRegistro<DiasTrabajados>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }
            return response;
        }

        public async Task<IResponse> CerrarNominaActivas(int model)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"OCommand/CerrarNominaActivas");
                var registro = await PostAsync<int>(requestUrl, model);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                    {
                        response.Respuesta.SetErrHost(reg);
                    }                      
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IResponse> CrearNomina(SolicitarNomina model)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("OCommand/CrearNomina");
                var registro = await PostAsync(requestUrl, model);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.SetErrHost(reg);
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }
            return response;
        }

        public async Task<IResponse> ActualizarEmpleadosInss(List<EmpleadosActivos> model)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("OCommand/ActualizarEmpleadosInss");
                var registro = await PostAsync(requestUrl, model);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.SetErrHost(reg);
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }
            return response;
        }

        public async Task<IListResponse<NominaMensualReportar>> AddCerrarMesNomina(int Anio, int Mes)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<NominaMensualReportar>();
            try
            {
                var requestUrl = CreateRequestUri($"OCommand/CerrarMesNomina/{Anio}/{Mes}");
                var registro = await PostAsync(requestUrl, new { });
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<NominaMensualReportar>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }
            return response;
        }
        public async Task<IListResponse<DiasTrabajados>> GetAllDiasTrabajados(typeeinout rango)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<DiasTrabajados>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GetAllDiasTrabajados");
                var registro = await GetAsync(requestUrl, rango);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<DiasTrabajados>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IListResponse<DiasxempleadosOpera>> GetAllDiasTrabajadosOperacion(typeeinout rango)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<DiasxempleadosOpera>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GetAllDiasTrabajadosOperacion");
                var registro = await GetAsync(requestUrl, rango);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<DiasxempleadosOpera>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }
      
        public async Task<IListResponse<DiasTrabajadosAreas>> GetAllMarcadasFacturar(typeeinout rango)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<DiasTrabajadosAreas>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GetAllMarcadasFacturar");
                var registro = await GetAsync(requestUrl, rango);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<DiasTrabajadosAreas>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IListResponse<diasxpagarperiodo>> GetAllDiasxPagar(typeeinout rango)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<diasxpagarperiodo>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GetAllDiasxPagar");
                var registro = await GetAsync(requestUrl, rango);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<diasxpagarperiodo>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }

        public async Task<IListResponse<nominatype>> GetAllPagoNomina(int idnomina)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<nominatype>();
            try
            {
                var requestUrl = CreateRequestUri($"OdQuery/GetAllPagoNomina/{idnomina}");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<nominatype>(reg.detail.ToString());
                    else
                        response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");

                }
                else
                    response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
            }
            catch (CoreException ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }

            return response;
        }
    }
}

