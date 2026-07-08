using MediatR;
using Modelo.Interfaces;
using Modelo.Comercial;
using Modelo.ClasesGenericas;
using Modelo.Report;
using System;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Comercial.Queries
{
    public record GetCatalogosComercialQuery : IRequest<IListResponse<CatalogoResponse>>;
    public record GetCotizacionesQuery : IRequest<IListResponse<Cotizacion>>;
    public record PrintCotizacionPdfQuery(List<Guid> Ids) : IRequest<ISingleResponse<FileNameString>>;
    public record PrintCotizacionDesglosePdfQuery(List<Guid> Ids) : IRequest<ISingleResponse<FileNameString>>;
    public record PrintDescriptorPdfQuery(string JobTitle) : IRequest<ISingleResponse<FileNameString>>;
    public record PrintMatrizDescriptoresPdfQuery() : IRequest<ISingleResponse<FileNameString>>;
}
