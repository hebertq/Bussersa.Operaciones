using BsOperaciones.Application.Features.Odoo.Command;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class DeleteBulkProduccionDiariaHandler : IRequestHandler<DeleteBulkProduccionDiariaCommand, IResponse>
    {
        private readonly IOdooService _odooService;

        public DeleteBulkProduccionDiariaHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IResponse> Handle(DeleteBulkProduccionDiariaCommand request, CancellationToken cancellationToken)
        {
            return await _odooService.DeleteBulkProduccionDiaria(request.Ids);
        }
    }
}
