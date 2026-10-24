using BsOperaciones.Application.Features.Operaciones.Commands;
using BsOperaciones.Application.Features.Operaciones.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using Modelo.Entidades.Operaciones;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Operaciones.Handlers
{
    public class GetCatalogoServicioPuestoAreaHandler : IRequestHandler<GetCatalogoServicioPuestoAreaQuery, IListResponse<CatalogoServicioPuestoAreaDto>>
    {
        private readonly IOdooService _odooService;
        public GetCatalogoServicioPuestoAreaHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IListResponse<CatalogoServicioPuestoAreaDto>> Handle(GetCatalogoServicioPuestoAreaQuery request, CancellationToken cancellationToken)
            => await _odooService.GetCatalogoServicioPuestoArea(request.OperacionId);
    }

    public class SaveCatalogoServicioPuestoAreaHandler : IRequestHandler<SaveCatalogoServicioPuestoAreaCommand, IResponse>
    {
        private readonly IOdooService _odooService;
        public SaveCatalogoServicioPuestoAreaHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IResponse> Handle(SaveCatalogoServicioPuestoAreaCommand request, CancellationToken cancellationToken)
            => await _odooService.SaveCatalogoServicioPuestoArea(request.Dto);
    }

    public class DeleteCatalogoServicioPuestoAreaHandler : IRequestHandler<DeleteCatalogoServicioPuestoAreaCommand, IResponse>
    {
        private readonly IOdooService _odooService;
        public DeleteCatalogoServicioPuestoAreaHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IResponse> Handle(DeleteCatalogoServicioPuestoAreaCommand request, CancellationToken cancellationToken)
            => await _odooService.DeleteCatalogoServicioPuestoArea(request.Id);
    }

    public class GetMarcadasSemanalesAsignacionHandler : IRequestHandler<GetMarcadasSemanalesAsignacionQuery, IListResponse<MarcadaAsistenciaProduccionDto>>
    {
        private readonly IOdooService _odooService;
        public GetMarcadasSemanalesAsignacionHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IListResponse<MarcadaAsistenciaProduccionDto>> Handle(GetMarcadasSemanalesAsignacionQuery request, CancellationToken cancellationToken)
            => await _odooService.GetMarcadasSemanalesAsignacion(request.OperacionId, request.Inicio, request.Fin, request.SoloPendientes);
    }

    public class GetGrillaProduccionSemanalHandler : IRequestHandler<GetGrillaProduccionSemanalQuery, IListResponse<GrillaProduccionSemanalDto>>
    {
        private readonly IOdooService _odooService;
        public GetGrillaProduccionSemanalHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IListResponse<GrillaProduccionSemanalDto>> Handle(GetGrillaProduccionSemanalQuery request, CancellationToken cancellationToken)
            => await _odooService.GetGrillaProduccionSemanal(request.OperacionId, request.Inicio, request.Fin, request.SoloPendientes);
    }

    public class AsignarMarcadasProduccionHandler : IRequestHandler<AsignarMarcadasProduccionCommand, IResponse>
    {
        private readonly IOdooService _odooService;
        public AsignarMarcadasProduccionHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IResponse> Handle(AsignarMarcadasProduccionCommand request, CancellationToken cancellationToken)
            => await _odooService.AsignarMarcadasProduccion(request.Request);
    }

    public class DesasignarMarcadasProduccionHandler : IRequestHandler<DesasignarMarcadasProduccionCommand, IResponse>
    {
        private readonly IOdooService _odooService;
        public DesasignarMarcadasProduccionHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IResponse> Handle(DesasignarMarcadasProduccionCommand request, CancellationToken cancellationToken)
            => await _odooService.DesasignarMarcadasProduccion(request.NumeroAsignacion);
    }

    public class GetHistoricoAsignacionesProduccionHandler : IRequestHandler<GetHistoricoAsignacionesProduccionQuery, IListResponse<HistoricoAsignacionProduccionDto>>
    {
        private readonly IOdooService _odooService;
        public GetHistoricoAsignacionesProduccionHandler(IOdooService odooService) => _odooService = odooService;
        public async Task<IListResponse<HistoricoAsignacionProduccionDto>> Handle(GetHistoricoAsignacionesProduccionQuery request, CancellationToken cancellationToken)
            => await _odooService.GetHistoricoAsignacionesProduccion(request.OperacionId, request.Anio);
    }
}
