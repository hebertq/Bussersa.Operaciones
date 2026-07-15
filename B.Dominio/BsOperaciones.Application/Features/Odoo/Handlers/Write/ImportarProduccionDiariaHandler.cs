using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class ImportarProduccionDiariaHandler : IRequestHandler<ImportarProduccionDiariaCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public ImportarProduccionDiariaHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IResponse> Handle(ImportarProduccionDiariaCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.ImportarProduccionDiaria(request.model);
        }
    }
}
