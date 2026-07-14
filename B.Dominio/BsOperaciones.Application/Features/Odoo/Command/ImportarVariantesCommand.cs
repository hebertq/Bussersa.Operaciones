using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Odoo.Commands
{
    public record ImportarVariantesCommand(List<ImportarVarianteItem> model) : IRequest<IResponse>;
}
