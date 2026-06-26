using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using HostService.Interfaces;
using BsOperaciones.Application.Features.Odoo.Queries;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllDiasTrabajadosOperacionHandler : IRequestHandler<GetAllDiasTrabajadosOperacionQuery, IListResponse<DiasxempleadosOpera>>
    {
        private readonly IOdooService _Odoo;
        public GetAllDiasTrabajadosOperacionHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<IListResponse<DiasxempleadosOpera>> Handle(GetAllDiasTrabajadosOperacionQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllDiasTrabajadosOperacion(request.model));
        }
    }
}
