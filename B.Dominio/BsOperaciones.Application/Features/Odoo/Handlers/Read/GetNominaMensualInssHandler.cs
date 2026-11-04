using System.Threading;
using System.Threading.Tasks;
using BsOperaciones.Application.Features.Odoo.Queries;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using Modelo.Report;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Read
{
    public class GetNominaMensualInssHandler : IRequestHandler<GetNominaMensualInssQuery, IListResponse<NominaMensualReportar>>
    {
        private readonly IOdooService _Odoo;
        public GetNominaMensualInssHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IListResponse<NominaMensualReportar>> Handle(GetNominaMensualInssQuery request, CancellationToken cancellationToken)
        {
            return await _Odoo.GetNominaMensualInss(request.Anio, request.Mes);
        }
    }
}
