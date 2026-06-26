using MediatR;
using Modelo.Interfaces;
using Modelo.Report;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetReporteCierreClienteMesQuery(int OperacionId, int Anio, int Mes)
     : IRequest<IListResponse<ReporteCierreMarcadas>>;

    public record GetReporteCierreClienteRangoQuery(int OperacionId, DateTime Inicio, DateTime Fin)
    : IRequest<IListResponse<ReporteCierreMarcadas>>;

    public record GetReporteCierreGlobalMesQuery(int Anio, int Mes)
    : IRequest<IListResponse<ReporteCierreMarcadas>>;
}
