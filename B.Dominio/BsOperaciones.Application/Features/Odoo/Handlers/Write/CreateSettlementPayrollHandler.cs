using BsOperaciones.Application.Features.Odoo.Command;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class CreateSettlementPayrollHandler : IRequestHandler<CreateSettlementPayrollCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        public CreateSettlementPayrollHandler(IOdooService adm) { _Odo = adm; }
        public async Task<IResponse> Handle(CreateSettlementPayrollCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odo.CreateSettlementPayroll(request.Inicio, request.Fin, request.Nombre, request.model));
        }
    }
}
