using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetAllPagoNominaQuery(int model) : IRequest<IListResponse<nominatype>>;
}
