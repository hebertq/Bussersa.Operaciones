using System.Threading;
using System.Threading.Tasks;
using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class CrearNominaMasivaHandler : IRequestHandler<CrearNominaMasivaCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        public CrearNominaMasivaHandler(IOdooService adm) { _Odo = adm; }

        public async Task<IResponse> Handle(CrearNominaMasivaCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odo.CrearNominaMasiva(request.model));
        }
    }
}
