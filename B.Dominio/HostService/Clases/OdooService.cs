using Blazored.LocalStorage;
using HostService.ClasesGenericas;
using HostService.Interfaces;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;
using Modelo.Report;
using Modelo.Admin;
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

        public async Task<ISingleResponse<FileNameString>> PrintPayrollPdf(string nombre, repnominapago modelo)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new SingleResponse<FileNameString>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GeneratePayrollPdf", $"nombre={Uri.EscapeDataString(nombre)}");
                var registro = await PostAsync(requestUrl, modelo);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerRegistro<FileNameString>(reg.detail.ToString());
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

        public async Task<ISingleResponse<FileNameString>> PrintCotizacionPdf(List<Guid> ids)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new SingleResponse<FileNameString>();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/GenerateCotizacionPdf");
                var registro = await PostAsync(requestUrl, ids);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerRegistro<FileNameString>(reg.detail.ToString());
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

        public async Task<ISingleResponse<FileNameString>> GenerateExcel(MultiSheetExcelRequest request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new SingleResponse<FileNameString>();
            try
            {
                var requestUrl = CreateRequestUri("OdQuery/GenerateExcel");
                var registro = await PostAsync(requestUrl, request);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                    {
                        response.Model = _Util.ObtenerRegistro<FileNameString>(reg.detail.ToString());
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

        public async Task<IListResponse<User>> GetUsers()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<User>();
            try
            {
                var requestUrl = CreateRequestUri("Admin/GetUsers");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<User>(reg.detail.ToString());
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

        public async Task<IResponse> AddUser(User user)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/AddUser");
                var registro = await PostAsync(requestUrl, user);
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

        public async Task<IResponse> UpdateUser(User user)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/UpdateUser");
                var registro = await PostAsync(requestUrl, user);
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

        public async Task<IResponse> DeleteUser(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Admin/DeleteUser/{id}");
                var registro = await DeleteAsync(requestUrl);
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

        public async Task<IListResponse<AdmonGroup>> GetGroups()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<AdmonGroup>();
            try
            {
                var requestUrl = CreateRequestUri("Admin/GetGroups");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<AdmonGroup>(reg.detail.ToString());
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

        public async Task<IResponse> AddGroup(AdmonGroup group)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/AddGroup");
                var registro = await PostAsync(requestUrl, group);
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

        public async Task<IResponse> UpdateGroup(AdmonGroup group)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/UpdateGroup");
                var registro = await PostAsync(requestUrl, group);
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

        public async Task<IResponse> DeleteGroup(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Admin/DeleteGroup/{id}");
                var registro = await DeleteAsync(requestUrl);
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

        public async Task<IListResponse<AdmonRole>> GetGroupRoles(int groupId)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<AdmonRole>();
            try
            {
                var requestUrl = CreateRequestUri($"Admin/GetGroupRoles/{groupId}");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<AdmonRole>(reg.detail.ToString());
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

        public async Task<IResponse> SaveGroupRoles(Modelo.Admin.GroupRolesRequest request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/SaveGroupRoles");
                var registro = await PostAsync(requestUrl, request);
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

        public async Task<IListResponse<AdmonRole>> GetRoles()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<AdmonRole>();
            try
            {
                var requestUrl = CreateRequestUri("Admin/GetRoles");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<AdmonRole>(reg.detail.ToString());
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

        public async Task<IResponse> AddRole(AdmonRole role)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/AddRole");
                var registro = await PostAsync(requestUrl, role);
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

        public async Task<IResponse> UpdateRole(AdmonRole role)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/UpdateRole");
                var registro = await PostAsync(requestUrl, role);
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

        public async Task<IResponse> DeleteRole(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Admin/DeleteRole/{id}");
                var registro = await DeleteAsync(requestUrl);
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

        public async Task<IListResponse<AdmonMenu>> GetRoleMenus(int roleId)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<AdmonMenu>();
            try
            {
                var requestUrl = CreateRequestUri($"Admin/GetRoleMenus/{roleId}");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<AdmonMenu>(reg.detail.ToString());
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

        public async Task<IResponse> SaveRoleMenus(Modelo.Admin.RoleMenusRequest request)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/SaveRoleMenus");
                var registro = await PostAsync(requestUrl, request);
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

        public async Task<IListResponse<AdmonMenu>> GetMenus()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            var response = new ListResponse<AdmonMenu>();
            try
            {
                var requestUrl = CreateRequestUri("Admin/GetMenus");
                var registro = await GetAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<AdmonMenu>(reg.detail.ToString());
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

        public async Task<IResponse> AddMenu(AdmonMenu menu)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/AddMenu");
                var registro = await PostAsync(requestUrl, menu);
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

        public async Task<IResponse> UpdateMenu(AdmonMenu menu)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Admin/UpdateMenu");
                var registro = await PostAsync(requestUrl, menu);
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

        public async Task<IResponse> DeleteMenu(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Admin/DeleteMenu/{id}");
                var registro = await DeleteAsync(requestUrl);
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

        // --- MÓDULO COMERCIAL Y COTIZADOR ---
        public async Task<IListResponse<Modelo.Comercial.CatalogoResponse>> GetCatalogosComercial()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<Modelo.Comercial.CatalogoResponse> response = new ListResponse<Modelo.Comercial.CatalogoResponse>();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/GetCatalogos");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<Modelo.Comercial.CatalogoResponse>(reg.detail.ToString());
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

        public async Task<IResponse> SaveCatalogoEpp(Modelo.Comercial.CatalogoEpp epp)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/SaveEpp");
                var registro = await PostAsync(requestUrl, epp);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> DeleteCatalogoEpp(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Comercial/DeleteEpp/{id}");
                var registro = await DeleteAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> SaveCatalogoViatico(Modelo.Comercial.CatalogoViatico viatico)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/SaveViatico");
                var registro = await PostAsync(requestUrl, viatico);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> DeleteCatalogoViatico(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Comercial/DeleteViatico/{id}");
                var registro = await DeleteAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> SaveCatalogoMaquinaria(Modelo.Comercial.CatalogoMaquinaria machinery)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/SaveMaquinaria");
                var registro = await PostAsync(requestUrl, machinery);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> DeleteCatalogoMaquinaria(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Comercial/DeleteMaquinaria/{id}");
                var registro = await DeleteAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> SaveCatalogoMaterial(Modelo.Comercial.CatalogoMaterial material)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/SaveMaterial");
                var registro = await PostAsync(requestUrl, material);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> DeleteCatalogoMaterial(int id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Comercial/DeleteMaterial/{id}");
                var registro = await DeleteAsync(requestUrl);
                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IResponse> SaveCotizacion(Modelo.Comercial.Cotizacion cotizacion)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/SaveCotizacion");
                var registro = await PostAsync(requestUrl, cotizacion);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        public async Task<IListResponse<Modelo.Comercial.Cotizacion>> GetCotizaciones()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<Modelo.Comercial.Cotizacion> response = new ListResponse<Modelo.Comercial.Cotizacion>();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/GetCotizaciones");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<Modelo.Comercial.Cotizacion>(reg.detail.ToString());
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

        public async Task<IResponse> DeleteCotizacion(Guid id)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri($"Comercial/DeleteCotizacion/{id}");
                var registro = await DeleteAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Respuesta.ExisteError = false;
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

        // --- DASHBOARD DE RENTABILIDAD ---
        public async Task<IListResponse<Modelo.Comercial.DashboardResponse>> GetDashboardData(string mes, string turno)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<Modelo.Comercial.DashboardResponse> response = new ListResponse<Modelo.Comercial.DashboardResponse>();
            try
            {
                var requestUrl = CreateRequestUri($"Comercial/GetDashboard/{mes}/{turno}");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<Modelo.Comercial.DashboardResponse>(reg.detail.ToString());
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

        public async Task<IListResponse<string>> GetDashboardMonths()
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IListResponse<string> response = new ListResponse<string>();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/GetDashboardMonths");
                var registro = await GetAsync(requestUrl);

                if (registro.IsSuccess)
                {
                    var reg = registro.Data;
                    if (reg.sucess)
                        response.Model = _Util.ObtenerDato<string>(reg.detail.ToString());
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

        public async Task<IResponse> UploadDashboardExcel(byte[] fileBytes, string fileName, string mes)
        {
            string metodo = $"OdooService_{MethodBase.GetCurrentMethod().Name}";
            IResponse response = new ErrorResponse();
            try
            {
                var requestUrl = CreateRequestUri("Comercial/ImportDashboard");
                using (var content = new System.Net.Http.MultipartFormDataContent())
                {
                    var fileContent = new System.Net.Http.ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    content.Add(fileContent, "file", fileName);
                    content.Add(new System.Net.Http.StringContent(mes), "mes");

                    var registro = await PostMultipartAsync(requestUrl, content);
                    if (registro.IsSuccess)
                    {
                        var reg = registro.Data;
                        if (reg.sucess)
                            response.Respuesta.ExisteError = false;
                        else
                            response.Respuesta.SetError(reg.errors.ToString(), ErrorType.Servicio, "");
                    }
                    else
                        response.Respuesta.SetErrorApi(registro.ReturnMessage, metodo);
                }
            }
            catch (Exception ex)
            {
                response.Respuesta.SetErrorExep(ErrorType.Datos, ex, metodo);
            }
            return response;
        }
    }
}

