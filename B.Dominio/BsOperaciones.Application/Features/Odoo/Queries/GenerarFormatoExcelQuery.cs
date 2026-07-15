using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Queries
{
    public record GenerarFormatoExcelQuery(GenerarFormatoRequest model) : IRequest<ISingleResponse<FileResponseDto>>;
}
