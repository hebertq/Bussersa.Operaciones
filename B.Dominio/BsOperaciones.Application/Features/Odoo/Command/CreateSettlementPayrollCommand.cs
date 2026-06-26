using MediatR;
using Modelo.Entidades.Nomina;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public record CreateSettlementPayrollCommand() : IRequest<IResponse>
    {
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public string? Nombre { get; set; }
        public List<SeveranceDetail>? model { get; set; } // Aquí viaja el JSON serializado de la grilla
    }
}
