using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetPendingSeveranceHandler : IRequestHandler<GetPendingSeveranceQuery, IListResponse<SeveranceDetail>>
    {
        private readonly IOdooService _Odoo;
        public GetPendingSeveranceHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }
        public async Task<IListResponse<SeveranceDetail>> Handle(GetPendingSeveranceQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllPendingSeverance());
        }
    }
}
