using MediatR;
using Modelo.Interfaces;
using Modelo.Entidades.Operaciones;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Operaciones.Commands
{
    public record SaveCatalogoServicioPuestoAreaCommand(CatalogoServicioPuestoAreaDto Dto) : IRequest<IResponse>;
    public record DeleteCatalogoServicioPuestoAreaCommand(int Id) : IRequest<IResponse>;
    public record AsignarMarcadasProduccionCommand(AsignarMarcadasProduccionRequest Request) : IRequest<IResponse>;
    public record DesasignarMarcadasProduccionCommand(string NumeroAsignacion) : IRequest<IResponse>;
}
