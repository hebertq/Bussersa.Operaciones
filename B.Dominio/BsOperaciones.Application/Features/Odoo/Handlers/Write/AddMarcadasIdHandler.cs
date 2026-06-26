using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class AddMarcadasIdHandler : IRequestHandler<AddMarcadasIdCommand, ISingleResponse<DiasTrabajados>>
    {
        private readonly IOdooService _Odoo;
        public AddMarcadasIdHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<ISingleResponse<DiasTrabajados>> Handle(AddMarcadasIdCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.AddMarcadasId(request.model,request.operacion));
        }
    }
}
