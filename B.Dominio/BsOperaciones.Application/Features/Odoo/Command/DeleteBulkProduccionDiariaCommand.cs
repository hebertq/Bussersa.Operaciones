using MediatR;
using Modelo.Interfaces;
using System.Collections.Generic;

namespace BsOperaciones.Application.Features.Odoo.Command
{
    public class DeleteBulkProduccionDiariaCommand : IRequest<IResponse>
    {
        public List<int> Ids { get; set; } = new();
    }
}
