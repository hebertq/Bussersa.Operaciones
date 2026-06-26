using BsOperaciones.Application.Features.Odoo.Commands;
using MediatR;
using Modelo.Interfaces;
using HostService.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class CrudferiadoHandler : IRequestHandler<CrudferiadoCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        public CrudferiadoHandler(IOdooService adm) { _Odo = adm; }
        public async Task<IResponse> Handle(CrudferiadoCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odo.Crudferiado(request.model,request.operacion));
        }
    }
}
