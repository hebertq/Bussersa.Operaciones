using Modelo.Entidades.Entradas.Odoo;
using Modelo.Interfaces;
using System.Threading.Tasks;

namespace HostService.Interfaces
{
    public interface IRedmineService
    {
        Task<IResponse> CrearSolicituBonosAsync(HoraEntrada modelo, string cliente);
    }
}
