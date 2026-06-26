using MediatR;
using Modelo.Interfaces;
using Modelo.Report;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public record CerrarMesNominaCommand(int Anio, int Mes) : IRequest<IListResponse<NominaMensualReportar>>;
}
