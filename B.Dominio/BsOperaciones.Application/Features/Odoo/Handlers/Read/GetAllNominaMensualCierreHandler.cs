using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllNominaMensualCierreHandler : IRequestHandler<GetAllNominaMensualCierreQuery, IListResponse<PayrollMonthRecord>>
    {
        private readonly IOdooService _Odoo;
        public GetAllNominaMensualCierreHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }
        public async Task<IListResponse<PayrollMonthRecord>> Handle(GetAllNominaMensualCierreQuery request, CancellationToken cancellationToken)
        {
            IListResponse<PayrollMonthRecord> response = await _Odoo.GetAllNominaMensualCierre(request.model);
            return await Task.FromResult(response);
        }
    }
}
