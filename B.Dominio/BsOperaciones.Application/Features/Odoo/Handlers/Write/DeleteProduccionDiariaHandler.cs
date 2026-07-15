using BsOperaciones.Application.Features.Odoo.Command;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class DeleteProduccionDiariaHandler : IRequestHandler<DeleteProduccionDiariaCommand, IResponse>
    {
        private readonly IOdooService _odooService;

        public DeleteProduccionDiariaHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IResponse> Handle(DeleteProduccionDiariaCommand request, CancellationToken cancellationToken)
        {
            return await _odooService.DeleteProduccionDiaria(request.Id);
        }
    }
}
