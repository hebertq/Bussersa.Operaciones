using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using System;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetProduccionDiariaQuery(DateTime? inicio, DateTime? fin, int? operacionId, string? estadoFactura) : IRequest<IListResponse<ProduccionDiariaDto>>;
}
