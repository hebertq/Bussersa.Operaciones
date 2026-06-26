using Microsoft.AspNetCore.Authorization;

namespace BsOperaciones.Application.Extensions
{
    public class ProfileOwnerRequirement : IAuthorizationRequirement
    {
        public ProfileOwnerRequirement(string sessionHeaderName)
        {
            SessionHeaderName = sessionHeaderName;
        }
        public string SessionHeaderName { get; }
    }
}
