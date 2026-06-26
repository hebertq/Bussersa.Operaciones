using BsOperaciones.Application.Features.Odoo.Commands;
using MediatR;
using Modelo.Interfaces;
using HostService.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class CerrarNominaActivasHandler : IRequestHandler<CerrarNominaActivasCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        public CerrarNominaActivasHandler(IOdooService adm) { _Odo = adm; }
        public async Task<IResponse> Handle(CerrarNominaActivasCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odo.CerrarNominaActivas(request.model));
        }
    }
}
