using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using Modelo.Entidades.Entradas.Odoo;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GenerarFormatoExcelHandler : IRequestHandler<GenerarFormatoExcelQuery, ISingleResponse<FileResponseDto>>
    {
        private readonly IOdooService _Odoo;
        public GenerarFormatoExcelHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<ISingleResponse<FileResponseDto>> Handle(GenerarFormatoExcelQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.GenerarFormatoExcel(request.model);
        }
    }
}
