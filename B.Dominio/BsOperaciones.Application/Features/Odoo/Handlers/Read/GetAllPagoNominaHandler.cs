using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllPagoNominaHandler : IRequestHandler<GetAllPagoNominaQuery, IListResponse<nominatype>>
    {
        private readonly IOdooService _Odoo;
        public GetAllPagoNominaHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<IListResponse<nominatype>> Handle(GetAllPagoNominaQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllPagoNomina(request.model));
        }
    }
}
