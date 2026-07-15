using BsOperaciones.Application.Features.Odoo.Queries;
using MediatR;
using Modelo.Entidades.Operaciones;
using Modelo.Interfaces;
using HostService.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetProgramacionTurnosHandler : IRequestHandler<GetProgramacionTurnosQuery, IListResponse<ProgramacionTurnoDto>>
    {
        private readonly IOdooService _odooService;
        public GetProgramacionTurnosHandler(IOdooService odooService)
        {
            _odooService = odooService;
        }

        public async Task<IListResponse<ProgramacionTurnoDto>> Handle(GetProgramacionTurnosQuery request, CancellationToken cancellationToken)
        {
            return await _odooService.GetProgramacionTurnos(request.fechaInicio);
        }
    }
}
