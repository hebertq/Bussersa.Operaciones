using BsOperaciones.Application.Features.Odoo.Command;
using MediatR;
using Modelo.Interfaces;
using HostService.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class SaveProgramacionTurnosHandler : IRequestHandler<SaveProgramacionTurnosCommand, IResponse>
    {
        private readonly IOdooService _odooService;
        public SaveProgramacionTurnosHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IResponse> Handle(SaveProgramacionTurnosCommand request, CancellationToken cancellationToken)
        {
            return await _odooService.SaveProgramacionTurnos(request.turnos);
        }
    }
}
