using MediatR;
using HostService.Interfaces;
using Modelo.Interfaces;
using Modelo.Comercial;
using Modelo.ClasesGenericas;
using Modelo.Report;
using System.Threading;
using System.Threading.Tasks;
using BsOperaciones.Application.Features.Comercial.Queries;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Comercial.Handlers
{
    public class GetCatalogosComercialHandler : IRequestHandler<GetCatalogosComercialQuery, IListResponse<CatalogoResponse>>
    {
        private readonly IOdooService _Odoo;
        public GetCatalogosComercialHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IListResponse<CatalogoResponse>> Handle(GetCatalogosComercialQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.GetCatalogosComercial();
        }
    }

    public class GetCotizacionesHandler : IRequestHandler<GetCotizacionesQuery, IListResponse<Cotizacion>>
    {
        private readonly IOdooService _Odoo;
        public GetCotizacionesHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IListResponse<Cotizacion>> Handle(GetCotizacionesQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.GetCotizaciones();
        }
    }

    public class PrintCotizacionPdfHandler : IRequestHandler<PrintCotizacionPdfQuery, ISingleResponse<FileNameString>>
    {
        private readonly IOdooService _Odoo;
        public PrintCotizacionPdfHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<ISingleResponse<FileNameString>> Handle(PrintCotizacionPdfQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.PrintCotizacionPdf(request.Ids);
        }
    }

    public class PrintCotizacionDesglosePdfHandler : IRequestHandler<PrintCotizacionDesglosePdfQuery, ISingleResponse<FileNameString>>
    {
        private readonly IOdooService _Odoo;
        public PrintCotizacionDesglosePdfHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<ISingleResponse<FileNameString>> Handle(PrintCotizacionDesglosePdfQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.PrintCotizacionDesglosePdf(request.Ids);
        }
    }

    public class PrintDescriptorPdfHandler : IRequestHandler<PrintDescriptorPdfQuery, ISingleResponse<FileNameString>>
    {
        private readonly IOdooService _Odoo;
        public PrintDescriptorPdfHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<ISingleResponse<FileNameString>> Handle(PrintDescriptorPdfQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.PrintDescriptorPdf(request.Id);
        }
    }

    public class PrintMatrizDescriptoresPdfHandler : IRequestHandler<PrintMatrizDescriptoresPdfQuery, ISingleResponse<FileNameString>>
    {
        private readonly IOdooService _Odoo;
        public PrintMatrizDescriptoresPdfHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<ISingleResponse<FileNameString>> Handle(PrintMatrizDescriptoresPdfQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.PrintMatrizDescriptoresPdf(request.MatrizId);
        }
    }
}
