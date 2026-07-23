using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class UpdateProduccionDiariaHandler : IRequestHandler<UpdateProduccionDiariaCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public UpdateProduccionDiariaHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IResponse> Handle(UpdateProduccionDiariaCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.UpdateProduccionDiaria(request.model);
        }
    }
}
