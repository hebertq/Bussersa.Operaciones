using Modelo.Entidades.Entradas.Odoo;
using Modelo.Enum;

namespace BsOperaciones.Application.Common.Interface
{
    public interface IRelojStrategy
    {
        TipoReloj Tipo { get; }
        List<HoraEntrada> Parsear(Stream fileStream, DateTime fechaCarga);
    }
}
