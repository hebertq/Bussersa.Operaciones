using BsOperaciones.Application.Features.Odoo.Queries;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using HostService.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllActiveEmployeesForRotationHandler : IRequestHandler<GetAllActiveEmployeesForRotationQuery, IListResponse<OdooEmployeeDto>>
    {
        private readonly IOdooService _odooService;
        public GetAllActiveEmployeesForRotationHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IListResponse<OdooEmployeeDto>> Handle(GetAllActiveEmployeesForRotationQuery request, CancellationToken cancellationToken)
        {
            return await _odooService.GetAllActiveEmployeesForRotation();
        }
    }
}
