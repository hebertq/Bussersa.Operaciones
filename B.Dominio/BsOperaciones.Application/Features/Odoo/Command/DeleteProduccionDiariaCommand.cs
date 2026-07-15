using MediatR;
using Modelo.Interfaces;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public class DeleteProduccionDiariaCommand : IRequest<IResponse>
    {
        public int Id { get; set; }
    }
}
