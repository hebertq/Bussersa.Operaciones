using BsOperaciones.Application.Features.Odoo.Queries;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using HostService.Interfaces;
using MediatR;

namespace ApiAppBsOperaciones.Applicationlication.Features.Odoo.Handlers.Read
{
    public class GetAllDiasxPagarHandler : IRequestHandler<GetAllDiasxPagarQuery, IListResponse<diasxpagarperiodo>>
    {
        private readonly IOdooService _Odoo;
        public GetAllDiasxPagarHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<IListResponse<diasxpagarperiodo>> Handle(GetAllDiasxPagarQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllDiasxPagar(request.model));
        }
    }
}
