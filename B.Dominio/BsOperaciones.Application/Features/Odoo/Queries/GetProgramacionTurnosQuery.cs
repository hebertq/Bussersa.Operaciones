using MediatR;
using Modelo.Entidades.Operaciones;
using Modelo.Interfaces;
using System;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetProgramacionTurnosQuery(DateTime fechaInicio, int operacionId) : IRequest<IListResponse<ProgramacionTurnoDto>>;
}
