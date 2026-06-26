using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record CerrarNominaActivasCommand(int model) : IRequest<IResponse>;
}
