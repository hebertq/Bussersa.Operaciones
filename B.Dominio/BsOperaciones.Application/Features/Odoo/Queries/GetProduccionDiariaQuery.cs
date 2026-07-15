using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using System;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetProduccionDiariaQuery(DateTime? inicio, DateTime? fin, string? cliente, string? estadoFactura) : IRequest<IListResponse<ProduccionDiariaDto>>;
}
