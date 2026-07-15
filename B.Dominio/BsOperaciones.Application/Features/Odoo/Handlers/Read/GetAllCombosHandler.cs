using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.ClasesGenericas;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetAllCombosHandler : IRequestHandler<GetAllCombosQuery, IListResponse<Combos>>
    {
        private readonly IOdooService _Odoo;
        public GetAllCombosHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }
        public async Task<IListResponse<Combos>> Handle(GetAllCombosQuery request, CancellationToken cancellationToken)
        {
            IListResponse<Combos> response = new ListResponse<Combos>();           
            response = request.combo switch
            {
                "Nominas"     => await _Odoo.GetAllCombosHost("Nominas"),
                "Empleados"   => await _Odoo.GetAllCombosHost("Empleados"),
                "Municipios"  => await _Odoo.GetAllCombosHost("Municipios"),
                "Operaciones" => await _Odoo.GetAllCombosHost("Operaciones"),
                "Plantillas"  => await _Odoo.GetAllCombosHost("Plantillas"),
                "Templates"   => await _Odoo.GetAllCombosHost("Templates"),
                _ => throw new NotImplementedException("No existe el catalogo")
            };

            return await Task.FromResult(response);
        }
    }
}
