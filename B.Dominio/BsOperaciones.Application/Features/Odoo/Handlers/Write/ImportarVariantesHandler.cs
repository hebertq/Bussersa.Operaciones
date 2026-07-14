using BsOperaciones.Application.Features.Odoo.Commands;
using MediatR;
using Modelo.Interfaces;
using HostService.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class ImportarVariantesHandler : IRequestHandler<ImportarVariantesCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        public ImportarVariantesHandler(IOdooService odo) { _Odo = odo; }

        public async Task<IResponse> Handle(ImportarVariantesCommand request, CancellationToken cancellationToken)
        {
            return await _Odo.ImportarVariantes(request.model);
        }
    }
}
