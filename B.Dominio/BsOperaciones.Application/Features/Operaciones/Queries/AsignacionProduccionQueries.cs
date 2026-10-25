using MediatR;
using Modelo.Interfaces;
using Modelo.Entidades.Operaciones;
using System;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Operaciones.Queries
{
    public record GetCatalogoServicioPuestoAreaQuery(int OperacionId) : IRequest<IListResponse<CatalogoServicioPuestoAreaDto>>;
    public record GetMarcadasSemanalesAsignacionQuery(int OperacionId, DateTime Inicio, DateTime Fin, bool SoloPendientes = false, bool ExcluirSupervisores = true, string? SupervisoresExcluidos = null, bool AplicarReglas = true) : IRequest<IListResponse<MarcadaAsistenciaProduccionDto>>;
    public record GetGrillaProduccionSemanalQuery(int OperacionId, DateTime Inicio, DateTime Fin, bool SoloPendientes = false, bool ExcluirSupervisores = true, string? SupervisoresExcluidos = null, bool AplicarReglas = true) : IRequest<IListResponse<GrillaProduccionSemanalDto>>;
    public record GetHistoricoAsignacionesProduccionQuery(int OperacionId, int Anio) : IRequest<IListResponse<HistoricoAsignacionProduccionDto>>;
}
