using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllEmpleadosHandler : IRequestHandler<GetAllEmpleadosQuery, IListResponse<EmpleadosActivos>>
    {
        private readonly IOdooService _Odoo;
        public GetAllEmpleadosHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<IListResponse<EmpleadosActivos>> Handle(GetAllEmpleadosQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllEmpleadosActivos());
        }
    }
}
