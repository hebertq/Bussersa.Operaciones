using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetAllMarcadasFacturarQuery(typeeinout model) : IRequest<IListResponse<DiasTrabajadosAreas>>;
}
