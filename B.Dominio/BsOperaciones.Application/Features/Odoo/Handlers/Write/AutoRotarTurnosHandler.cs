using BsOperaciones.Application.Features.Odoo.Command;
using MediatR;
using Modelo.Interfaces;
using HostService.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class AutoRotarTurnosHandler : IRequestHandler<AutoRotarTurnosCommand, IResponse>
    {
        private readonly IOdooService _odooService;
        public AutoRotarTurnosHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IResponse> Handle(AutoRotarTurnosCommand request, CancellationToken cancellationToken)
        {
            return await _odooService.AutoRotarTurnos(request.fechaInicioActual, request.fechaInicioSiguiente, request.operacionId);
        }
    }
}
