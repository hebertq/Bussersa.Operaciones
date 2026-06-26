using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record AddMarcadasIdCommand(DiasTrabajados model, int operacion) : IRequest<ISingleResponse<DiasTrabajados>>;
}
