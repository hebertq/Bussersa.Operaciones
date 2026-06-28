using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class AddAllMarcadasHandler : IRequestHandler<AddAllMarcadasCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        public AddAllMarcadasHandler(IOdooService adm) 
        { 
            _Odo = adm; 
        }

        public async Task<IResponse> Handle(AddAllMarcadasCommand request, CancellationToken cancellationToken)
        {
            // La lógica de creación de incidencias de Redmine se ejecuta en el backend 
            // para evitar problemas de CORS del navegador (Failed to fetch).
            return await _Odo.AddAllMarcadas(request.model, request.operacion);
        }
    }
}
