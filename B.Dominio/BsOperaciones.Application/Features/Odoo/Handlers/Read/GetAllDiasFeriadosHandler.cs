using BsOperaciones.Application.Features.Odoo.Queries;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using HostService.Interfaces;

namespace ApiApplication.Features.Odoo.Handlers.Read
{
    public class GetAllDiasFeriadosHandler : IRequestHandler<GetAllDiasFeriadosQuery, IListResponse<DiaFeriado>>
    {
        private readonly IOdooService _Odoo;
        public GetAllDiasFeriadosHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<IListResponse<DiaFeriado>> Handle(GetAllDiasFeriadosQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllDiasFeriados());
        }
    }
}
