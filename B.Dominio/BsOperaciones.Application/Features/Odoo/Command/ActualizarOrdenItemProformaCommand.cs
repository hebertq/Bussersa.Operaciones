using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record ActualizarOrdenItemProformaCommand(int itemId, int orden) : IRequest<IResponse>;
}
