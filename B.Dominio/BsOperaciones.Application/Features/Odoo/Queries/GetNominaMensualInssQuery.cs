using MediatR;
using Modelo.Interfaces;
using Modelo.Report;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GetNominaMensualInssQuery(int Anio, int Mes) : IRequest<IListResponse<NominaMensualReportar>>;
}
