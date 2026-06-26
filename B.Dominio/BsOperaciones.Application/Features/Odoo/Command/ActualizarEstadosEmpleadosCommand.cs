using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public record ActualizarEstadosEmpleadosCommand(List<EmpleadosActivos> model) : IRequest<IResponse>;
}
