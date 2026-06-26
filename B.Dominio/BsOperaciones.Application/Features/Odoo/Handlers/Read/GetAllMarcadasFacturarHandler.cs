using BsOperaciones.Application.Features.Odoo.Queries;
using MediatR;
using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using HostService.Interfaces;


namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllMarcadasFacturarHandler : IRequestHandler<GetAllMarcadasFacturarQuery, IListResponse<DiasTrabajadosAreas>>
    {
        private readonly IOdooService _Odoo;
        public GetAllMarcadasFacturarHandler(IOdooService adm) { _Odoo = adm; }
        public async Task<IListResponse<DiasTrabajadosAreas>> Handle(GetAllMarcadasFacturarQuery request, CancellationToken cancellationToken)
        {
            return await Task.FromResult(await _Odoo.GetAllMarcadasFacturar(request.model));
        }
    }
}
