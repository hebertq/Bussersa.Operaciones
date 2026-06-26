using Modelo.Admin;
using Modelo.Interfaces;
using System.Threading.Tasks;

namespace HostService.Interfaces
{
    public interface IUserService
    {
        Task<ISingleResponse<UserWithToken>> LoginAsync(UserLogin model);
        Task<ISingleResponse<User>> GetUserByAccessToken(RefreshRequest model);
    }
}
