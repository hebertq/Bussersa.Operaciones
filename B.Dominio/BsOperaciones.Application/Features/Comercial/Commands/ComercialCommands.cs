using MediatR;
using Modelo.Interfaces;
using Modelo.Comercial;
using Modelo.ClasesGenericas;
using Modelo.Report;
using System;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Comercial.Commands
{
    public record SaveCotizacionCommand(Cotizacion Cotizacion) : IRequest<IResponse>;
    public record DeleteCotizacionCommand(Guid Id) : IRequest<IResponse>;
    public record SaveCatalogoEppCommand(CatalogoEpp Epp) : IRequest<IResponse>;
    public record DeleteCatalogoEppCommand(int Id) : IRequest<IResponse>;
    public record SaveCatalogoViaticoCommand(CatalogoViatico Viatico) : IRequest<IResponse>;
    public record DeleteCatalogoViaticoCommand(int Id) : IRequest<IResponse>;
    public record SaveCatalogoMaquinariaCommand(CatalogoMaquinaria Machinery) : IRequest<IResponse>;
    public record DeleteCatalogoMaquinariaCommand(int Id) : IRequest<IResponse>;
    public record SaveCatalogoMaterialCommand(CatalogoMaterial Material) : IRequest<IResponse>;
    public record DeleteCatalogoMaterialCommand(int Id) : IRequest<IResponse>;
    public record SaveCargosSocialesConfigCommand(CargosSocialesConfig Config) : IRequest<IResponse>;
    public record ParseProductsExcelCommand(byte[] FileBytes, string FileName) : IRequest<SingleResponse<List<string>>>;
}
