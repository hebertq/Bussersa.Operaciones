using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class ActualizarPreciosVariantesHandler : IRequestHandler<ActualizarPreciosVariantesCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public ActualizarPreciosVariantesHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IResponse> Handle(ActualizarPreciosVariantesCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.ActualizarPreciosVariantes(request.model);
        }
    }
}
