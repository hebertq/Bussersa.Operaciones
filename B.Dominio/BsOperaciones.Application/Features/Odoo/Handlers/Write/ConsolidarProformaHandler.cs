using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class ConsolidarProformaHandler : IRequestHandler<ConsolidarProformaCommand, IResponse>
    {
        private readonly IOdooService _Odoo;
        public ConsolidarProformaHandler(IOdooService odoo)
        {
            _Odoo = odoo;
        }

        public async Task<IResponse> Handle(ConsolidarProformaCommand request, CancellationToken cancellationToken)
        {
            return await _Odoo.ConsolidarProforma(request.model);
        }
    }
}
