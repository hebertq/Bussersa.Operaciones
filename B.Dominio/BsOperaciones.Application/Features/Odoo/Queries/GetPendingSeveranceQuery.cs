using MediatR;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetPendingSeveranceQuery() : IRequest<IListResponse<SeveranceDetail>>;
}
