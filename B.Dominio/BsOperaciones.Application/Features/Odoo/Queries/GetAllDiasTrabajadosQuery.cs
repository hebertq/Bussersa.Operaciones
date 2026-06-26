using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetAllDiasTrabajadosQuery(typeeinout model) : IRequest<IListResponse<DiasTrabajados>>;
}
