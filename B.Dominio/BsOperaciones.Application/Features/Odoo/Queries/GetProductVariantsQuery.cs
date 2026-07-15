using MediatR;
using Modelo.ClasesGenericas;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetProductVariantsQuery(int templateId) : IRequest<IListResponse<OdooVariantDto>>;
}
