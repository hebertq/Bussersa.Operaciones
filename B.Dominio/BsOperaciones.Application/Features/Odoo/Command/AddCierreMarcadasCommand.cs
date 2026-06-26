using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public record AddCierreMarcadasCommand(List<DiasTrabajadosAreas> model, int operacion) : IRequest<IResponse>;
}
