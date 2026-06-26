using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record CrearNominaComnnad(SolicitarNomina model) : IRequest<IResponse>;
}
