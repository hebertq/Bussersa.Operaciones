using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetEmployeesForRotationQuery(int operacionId) : IRequest<IListResponse<OdooEmployeeDto>>;
}
