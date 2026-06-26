using BsOperaciones.Application.Features.Odoo.Queries;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using HostService.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetMarcadaIdHandler : IRequestHandler<GetMarcadaIdQuery, ISingleResponse<DiasTrabajados>>
    {
        private readonly IOdooService _Odoo;
        public GetMarcadaIdHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<ISingleResponse<DiasTrabajados>> Handle(GetMarcadaIdQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetMarcadaId(request.model));
        }
    }
}
