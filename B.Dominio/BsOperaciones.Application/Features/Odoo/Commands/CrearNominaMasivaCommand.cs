using System.Collections.Generic;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record CrearNominaMasivaCommand(List<SolicitarNomina> model) : IRequest<IResponse>;
}
