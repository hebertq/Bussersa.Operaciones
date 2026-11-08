using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetReporteLiquidacionesHandler : IRequestHandler<GetReporteLiquidacionesQuery, IListResponse<SettlementReportRecord>>
    {
        private readonly IOdooService _odoo;

        public GetReporteLiquidacionesHandler(IOdooService odoo)
        {
            _odoo = odoo;
        }

        public async Task<IListResponse<SettlementReportRecord>> Handle(GetReporteLiquidacionesQuery request, CancellationToken cancellationToken)
        {
            return await _odoo.GetReporteLiquidaciones(request.anio, request.mes, request.clienteId);
        }
    }
}
