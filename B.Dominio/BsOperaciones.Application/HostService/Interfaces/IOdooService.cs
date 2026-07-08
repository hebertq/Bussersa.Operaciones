using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;
using Modelo.Report;
using Modelo.Admin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HostService.Interfaces
{
    public interface IOdooService
    {
        Task<IListResponse<PayrollMonthRecord>> GetAllNominaMensualCierre(int AnioMes);
        Task<IListResponse<Combos>> GetAllCombosHost(string combo);
        Task<IListResponse<DiaFeriado>> GetAllDiasFeriados();
        Task<IResponse> Crudferiado(DiaFeriado model, int op);
        Task<IResponse> AddAllMarcadas(List<HoraEntrada> model, int operacion, string opname, string fecha);
        Task<ISingleResponse<DiasTrabajados>> AddMarcadasId(DiasTrabajados param, int operacion);
        Task<ISingleResponse<DiasTrabajados>> GetMarcadaId(int idmarcada);
        Task<IResponse> CerrarNominaActivas(int model);
        Task<IResponse> CrearNomina(SolicitarNomina model);
        Task<IListResponse<DiasTrabajados>> GetAllDiasTrabajados(typeeinout rango);
        Task<IListResponse<DiasxempleadosOpera>> GetAllDiasTrabajadosOperacion(typeeinout rango);
        Task<IListResponse<DiasTrabajadosAreas>> GetAllMarcadasFacturar(typeeinout rango);
        Task<IListResponse<diasxpagarperiodo>> GetAllDiasxPagar(typeeinout rango);
        Task<IListResponse<nominatype>> GetAllPagoNomina(int idnomina);
        Task<IListResponse<EmpleadosActivos>> GetAllEmpleadosActivos();
        Task<IResponse> ActualizarEmpleadosInss(List<EmpleadosActivos> model);
        Task<IListResponse<NominaMensualReportar>> AddCerrarMesNomina(int Anio,int Mes);
        Task<IResponse> AddCierreMarcadaFacturar(List<DiasTrabajadosAreas> model, int operacion);
        Task<IListResponse<ReporteCierreMarcadas>> GetCierreFacturaRango(int Operacion, DateTime Inicio, DateTime Fin);
        Task<IListResponse<ReporteCierreMarcadas>> GetCierreFacturaMes(int Anio, int Mes, int Operacion);
        Task<IListResponse<ReporteCierreMarcadas>> GetCierreGlobal(int Anio, int Mes);
        Task<IListResponse<SeveranceDetail>> GetAllPendingSeverance();
        Task<IResponse> CreateSettlementPayroll(DateTime Inicio, DateTime Fin, string Nombre, List<SeveranceDetail> Param);
        Task<ISingleResponse<FileNameString>> PrintPayrollPdf(string nombre, repnominapago modelo);
        Task<ISingleResponse<FileNameString>> PrintCotizacionPdf(List<Guid> ids);
        Task<ISingleResponse<FileNameString>> PrintCotizacionDesglosePdf(List<Guid> ids);
        Task<ISingleResponse<FileNameString>> GenerateExcel(MultiSheetExcelRequest request);
        Task<IListResponse<User>> GetUsers();
        Task<IResponse> AddUser(User user);
        Task<IResponse> UpdateUser(User user);
        Task<IResponse> DeleteUser(int id);
        Task<IListResponse<AdmonGroup>> GetGroups();
        Task<IResponse> AddGroup(AdmonGroup group);
        Task<IResponse> UpdateGroup(AdmonGroup group);
        Task<IResponse> DeleteGroup(int id);
        Task<IListResponse<AdmonRole>> GetGroupRoles(int groupId);
        Task<IResponse> SaveGroupRoles(Modelo.Admin.GroupRolesRequest request);
        Task<IListResponse<AdmonRole>> GetRoles();
        Task<IResponse> AddRole(AdmonRole role);
        Task<IResponse> UpdateRole(AdmonRole role);
        Task<IResponse> DeleteRole(int id);
        Task<IListResponse<AdmonMenu>> GetRoleMenus(int roleId);
        Task<IResponse> SaveRoleMenus(Modelo.Admin.RoleMenusRequest request);
        Task<IListResponse<AdmonMenu>> GetMenus();
        Task<IResponse> AddMenu(AdmonMenu menu);
        Task<IResponse> UpdateMenu(AdmonMenu menu);
        Task<IResponse> DeleteMenu(int id);

        // --- MÓDULO COMERCIAL Y COTIZADOR ---
        Task<IListResponse<Modelo.Comercial.CatalogoResponse>> GetCatalogosComercial();
        Task<IResponse> SaveCotizacion(Modelo.Comercial.Cotizacion cotizacion);
        Task<IListResponse<Modelo.Comercial.Cotizacion>> GetCotizaciones();
        Task<IResponse> DeleteCotizacion(Guid id);
        Task<IResponse> SaveCatalogoEpp(Modelo.Comercial.CatalogoEpp epp);
        Task<IResponse> DeleteCatalogoEpp(int id);
        Task<IResponse> SaveCatalogoViatico(Modelo.Comercial.CatalogoViatico viatico);
        Task<IResponse> DeleteCatalogoViatico(int id);
        Task<IResponse> SaveCatalogoMaquinaria(Modelo.Comercial.CatalogoMaquinaria machinery);
        Task<IResponse> DeleteCatalogoMaquinaria(int id);
        Task<IResponse> SaveCatalogoMaterial(Modelo.Comercial.CatalogoMaterial material);
        Task<IResponse> DeleteCatalogoMaterial(int id);
        Task<IResponse> SaveCargosSocialesConfig(Modelo.Comercial.CargosSocialesConfig config);

        // --- DASHBOARD DE RENTABILIDAD ---
        Task<IListResponse<Modelo.Comercial.DashboardResponse>> GetDashboardData(string mes, string turno);
        Task<IListResponse<string>> GetDashboardMonths();
        Task<IResponse> UploadDashboardExcel(byte[] fileBytes, string fileName, string mes);
        Task<SingleResponse<List<string>>> ParseProductsExcel(byte[] fileBytes, string fileName);
    }
}
