using MediatR;
using HostService.Interfaces;
using Modelo.Interfaces;
using Modelo.Comercial;
using Modelo.ClasesGenericas;
using Modelo.Report;
using System.Threading;
using System.Threading.Tasks;
using BsOperaciones.Application.Features.Comercial.Commands;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Comercial.Handlers
{
    public class SaveCotizacionHandler : IRequestHandler<SaveCotizacionCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public SaveCotizacionHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(SaveCotizacionCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.SaveCotizacion(request.Cotizacion);
        }
    }

    public class DeleteCotizacionHandler : IRequestHandler<DeleteCotizacionCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public DeleteCotizacionHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(DeleteCotizacionCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.DeleteCotizacion(request.Id);
        }
    }

    public class SaveCatalogoEppHandler : IRequestHandler<SaveCatalogoEppCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public SaveCatalogoEppHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(SaveCatalogoEppCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.SaveCatalogoEpp(request.Epp);
        }
    }

    public class DeleteCatalogoEppHandler : IRequestHandler<DeleteCatalogoEppCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public DeleteCatalogoEppHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(DeleteCatalogoEppCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.DeleteCatalogoEpp(request.Id);
        }
    }

    public class SaveCatalogoViaticoHandler : IRequestHandler<SaveCatalogoViaticoCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public SaveCatalogoViaticoHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(SaveCatalogoViaticoCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.SaveCatalogoViatico(request.Viatico);
        }
    }

    public class DeleteCatalogoViaticoHandler : IRequestHandler<DeleteCatalogoViaticoCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public DeleteCatalogoViaticoHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(DeleteCatalogoViaticoCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.DeleteCatalogoViatico(request.Id);
        }
    }

    public class SaveCatalogoMaquinariaHandler : IRequestHandler<SaveCatalogoMaquinariaCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public SaveCatalogoMaquinariaHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(SaveCatalogoMaquinariaCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.SaveCatalogoMaquinaria(request.Machinery);
        }
    }

    public class DeleteCatalogoMaquinariaHandler : IRequestHandler<DeleteCatalogoMaquinariaCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public DeleteCatalogoMaquinariaHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(DeleteCatalogoMaquinariaCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.DeleteCatalogoMaquinaria(request.Id);
        }
    }

    public class SaveCatalogoMaterialHandler : IRequestHandler<SaveCatalogoMaterialCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public SaveCatalogoMaterialHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(SaveCatalogoMaterialCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.SaveCatalogoMaterial(request.Material);
        }
    }

    public class DeleteCatalogoMaterialHandler : IRequestHandler<DeleteCatalogoMaterialCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public DeleteCatalogoMaterialHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(DeleteCatalogoMaterialCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.DeleteCatalogoMaterial(request.Id);
        }
    }

    public class SaveCargosSocialesConfigHandler : IRequestHandler<SaveCargosSocialesConfigCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public SaveCargosSocialesConfigHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<IResponse> Handle(SaveCargosSocialesConfigCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.SaveCargosSocialesConfig(request.Config);
        }
    }

    public class ParseProductsExcelHandler : IRequestHandler<ParseProductsExcelCommand, SingleResponse<List<string>>>
    {
        private readonly IOdooService _Odoo;
        public ParseProductsExcelHandler(IOdooService odoo) { _Odoo = odoo; }
        public async Task<SingleResponse<List<string>>> Handle(ParseProductsExcelCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.ParseProductsExcel(request.FileBytes, request.FileName);
        }
    }
}
