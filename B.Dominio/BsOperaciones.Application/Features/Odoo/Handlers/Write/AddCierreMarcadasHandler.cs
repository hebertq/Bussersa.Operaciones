using BsOperaciones.Application.Features.Odoo.Command;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class AddCierreMarcadasHandler : IRequestHandler<AddCierreMarcadasCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        public AddCierreMarcadasHandler(IOdooService adm) { _Odo = adm; }
        public async Task<IResponse> Handle(AddCierreMarcadasCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odo.AddCierreMarcadaFacturar(request.model, request.operacion));
        }
    }
}
