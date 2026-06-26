using BsOperaciones.Application.Features.Odoo.Command;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using Modelo.Report;


namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class CerrarMesNominaHandler : IRequestHandler<CerrarMesNominaCommand, IListResponse<NominaMensualReportar>>
    {
        private readonly IOdooService _Odo;
        public CerrarMesNominaHandler(IOdooService adm) { _Odo = adm; }
        public async Task<IListResponse<NominaMensualReportar>> Handle(CerrarMesNominaCommand request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odo.AddCerrarMesNomina(request.Anio, request.Mes));
        }
    }
}
