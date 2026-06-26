using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record AddAllMarcadasCommand(List<HoraEntrada> model,int operacion,string opname) : IRequest<IResponse>;
}
