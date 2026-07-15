using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class ActualizarOrdenItemProformaHandler : IRequestHandler<ActualizarOrdenItemProformaCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public ActualizarOrdenItemProformaHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IResponse> Handle(ActualizarOrdenItemProformaCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.ActualizarOrdenItemProforma(request.itemId, request.orden);
        }
    }
}
