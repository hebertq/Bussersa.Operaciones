using Modelo.ClasesGenericas;
using Modelo.Interfaces;
using System.Threading.Tasks;

namespace HostService.Interfaces
{
    public partial interface IOdooService
    {
        Task<IListResponse<LoteNominaWhatsappItemDto>> GetLotesNominaWhatsapp();
        Task<ISingleResponse<ConsultaEnvioWhatsappResponseDto>> ConsultarEnvioWhatsappNomina(ConsultaEnvioWhatsappRequestDto request);
        Task<IResponse> ActualizarTelefonoColaboradorWhatsapp(ActualizarTelefonoColaboradorRequestDto request);
        Task<ISingleResponse<ConfigCanalWhatsappDto>> GetConfigCanalWhatsapp();
        Task<IResponse> GuardarConfigCanalWhatsapp(ConfigCanalWhatsappDto request);
        Task<IListResponse<ResultadoItemEnvioWhatsappDto>> EnviarWhatsappIndividualNomina(EnviarWhatsappIndividualRequestDto request);
        Task<ISingleResponse<ResultadoItemEnvioWhatsappDto>> EnviarWhatsappConsolidadoSupervisor(EnviarConsolidadoSupervisorRequestDto request);
    }
}
