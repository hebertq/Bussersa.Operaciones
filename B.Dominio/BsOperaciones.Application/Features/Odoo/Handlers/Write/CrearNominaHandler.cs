using BsOperaciones.Application.Features.Odoo.Commands;
using MediatR;
using Modelo.Interfaces;
using HostService.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class CrearNominaHandler : IRequestHandler<CrearNominaComnnad, IResponse>
    {
        private readonly IOdooService _Odo;
        public CrearNominaHandler(IOdooService adm) { _Odo = adm; }
        public async Task<IResponse> Handle(CrearNominaComnnad request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odo.CrearNomina(request.model));
        }
    }
}
