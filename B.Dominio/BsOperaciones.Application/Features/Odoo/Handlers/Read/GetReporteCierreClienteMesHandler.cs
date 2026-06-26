using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using Modelo.Report;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetReporteCierreClienteMesHandler : IRequestHandler<GetReporteCierreClienteMesQuery, IListResponse<ReporteCierreMarcadas>>
    {
        private readonly IOdooService _repository;
        public GetReporteCierreClienteMesHandler(IOdooService repository) => _repository = repository;

        public async Task<IListResponse<ReporteCierreMarcadas>> Handle(GetReporteCierreClienteMesQuery request, CancellationToken cancellationToken)
        {

            return await _repository.GetCierreFacturaMes(request.Anio, request.Mes, request.OperacionId);
        }
    }

    public class GetReporteCierreClienteRangoHandler : IRequestHandler<GetReporteCierreClienteRangoQuery, IListResponse<ReporteCierreMarcadas>>
    {
        private readonly IOdooService _repository;
        public GetReporteCierreClienteRangoHandler(IOdooService repository) => _repository = repository;

        public async Task<IListResponse<ReporteCierreMarcadas>> Handle(GetReporteCierreClienteRangoQuery request, CancellationToken cancellationToken)
        {

            return await _repository.GetCierreFacturaRango(request.OperacionId,request.Inicio, request.Fin);
        }
    }

    public class GetReporteCierreGlobalMesHandler : IRequestHandler<GetReporteCierreGlobalMesQuery, IListResponse<ReporteCierreMarcadas>>
    {
        private readonly IOdooService _repository;
        public GetReporteCierreGlobalMesHandler(IOdooService repository) => _repository = repository;

        public async Task<IListResponse<ReporteCierreMarcadas>> Handle(GetReporteCierreGlobalMesQuery request, CancellationToken cancellationToken)
        {

            return await _repository.GetCierreGlobal(request.Anio, request.Mes);
        }
    }
}
