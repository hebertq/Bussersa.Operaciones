using BsOperaciones.Application.Features.Odoo.Queries;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using HostService.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetEmployeesForRotationHandler : IRequestHandler<GetEmployeesForRotationQuery, IListResponse<OdooEmployeeDto>>
    {
        private readonly IOdooService _odooService;
        public GetEmployeesForRotationHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IListResponse<OdooEmployeeDto>> Handle(GetEmployeesForRotationQuery request, CancellationToken cancellationToken)
        {
            return await _odooService.GetEmployeesForRotation(request.operacionId);
        }
    }
}
