using MediatR;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetReporteLiquidacionesQuery(int anio, int mes, int clienteId) : IRequest<IListResponse<SettlementReportRecord>>;
}
