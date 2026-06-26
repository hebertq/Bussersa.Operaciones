using MediatR;
using Modelo.ClasesGenericas;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetAllCombosQuery(string combo) : IRequest<IListResponse<Combos>>;
}
