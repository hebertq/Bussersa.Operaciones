using BsOperaciones.Application.Features.Odoo.Command;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class ActualizarEstadosEmpleadosHandler : IRequestHandler<ActualizarEstadosEmpleadosCommand, IResponse>
    {
        // Aquí inyectarías tu servicio de Odoo o repositorio
        private readonly IOdooService _odooService;

        public ActualizarEstadosEmpleadosHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IResponse> Handle(ActualizarEstadosEmpleadosCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _odooService.ActualizarEmpleadosInss(request.model));
        }
    }
}
