using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetAllDiasTrabajadosOperacionQuery(typeeinout model) : IRequest<IListResponse<DiasxempleadosOpera>>;
}
