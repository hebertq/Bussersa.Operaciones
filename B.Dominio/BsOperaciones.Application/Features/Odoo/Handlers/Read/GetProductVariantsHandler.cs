using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetProductVariantsHandler : IRequestHandler<GetProductVariantsQuery, IListResponse<OdooVariantDto>>
    {
        private readonly IOdooService _Odoo;
        public GetProductVariantsHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IListResponse<OdooVariantDto>> Handle(GetProductVariantsQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.GetProductVariants(request.templateId);
        }
    }
}
