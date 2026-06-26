using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record CrudferiadoCommand(DiaFeriado model,int operacion) : IRequest<IResponse>;
}
