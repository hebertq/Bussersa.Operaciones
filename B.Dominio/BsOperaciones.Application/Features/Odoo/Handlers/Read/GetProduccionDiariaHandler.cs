using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetProduccionDiariaHandler : IRequestHandler<GetProduccionDiariaQuery, IListResponse<ProduccionDiariaDto>>
    {
        private readonly IOdooService _Odoo;
        public GetProduccionDiariaHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IListResponse<ProduccionDiariaDto>> Handle(GetProduccionDiariaQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.GetProduccionDiaria(request.inicio, request.fin, request.cliente, request.estadoFactura);
        }
    }
}
