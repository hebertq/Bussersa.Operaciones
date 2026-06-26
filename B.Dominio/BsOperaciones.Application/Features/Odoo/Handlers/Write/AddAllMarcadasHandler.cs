using BsOperaciones.Application.Features.Odoo.Commands;
using HostService.Interfaces;
using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Handlers.Write
{
    public class AddAllMarcadasHandler : IRequestHandler<AddAllMarcadasCommand, IResponse>
    {
        private readonly IOdooService _Odo;
        private readonly IRedmineService _redmineService;
        public AddAllMarcadasHandler(IOdooService adm, IRedmineService redmineService) { _Odo = adm; _redmineService = redmineService; }
        public async Task<IResponse> Handle(AddAllMarcadasCommand request, CancellationToken cancellationToken)
        {
            // 1. Identificar registros que tienen beneficios para Redmine
            var itemsConBeneficios = request.model
                .Where(x => x.bono > 0)
                .ToList();

            if (itemsConBeneficios.Any())
            {
                var redmineTasks = new List<Task>();

                foreach (var item in itemsConBeneficios)
                {
                    // Disparamos a Redmine para seguimiento
                    if (item.bono > 0)
                        redmineTasks.Add(_redmineService.CrearSolicituBonosAsync(item, request.opname));
                }

                // Esperamos confirmación de Redmine
                await Task.WhenAll(redmineTasks);

                // 2. LIMPIEZA: Una vez enviados a Redmine, ponemos a 0 en el modelo
                // para que Odoo reciba solo el tiempo (Entrada/Salida) sin los montos
                foreach (var item in request.model)
                {
                    item.bono = 0;
                }
            }

            // 3. Persistencia en Odoo / DB (Ahora va sin bonos ni comida)
            var result = await _Odo.AddAllMarcadas(request.model, request.operacion);

            return result;
        }
    }
}
