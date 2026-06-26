using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllDiasTrabajadosHandler : IRequestHandler<GetAllDiasTrabajadosQuery, IListResponse<DiasTrabajados>>
    {
        private readonly IOdooService _Odoo;
        public GetAllDiasTrabajadosHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<IListResponse<DiasTrabajados>> Handle(GetAllDiasTrabajadosQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllDiasTrabajados(request.model));
        }
    }
}
