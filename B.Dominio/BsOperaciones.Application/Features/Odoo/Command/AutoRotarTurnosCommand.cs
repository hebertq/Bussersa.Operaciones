using MediatR;
using Modelo.Interfaces;
using System;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public record AutoRotarTurnosCommand(DateTime fechaInicioActual, DateTime fechaInicioSiguiente) : IRequest<IResponse>;
}
