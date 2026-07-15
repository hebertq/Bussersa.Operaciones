using MediatR;
using Modelo.Entidades.Operaciones;
using Modelo.Interfaces;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public record SaveProgramacionTurnosCommand(List<ProgramacionTurnoDto> turnos) : IRequest<IResponse>;
}
