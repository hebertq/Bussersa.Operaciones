using Microsoft.AspNetCore.Authorization;

namespace BsOperaciones.Application.Extensions
{
    public static class Policies
    {
        public const string IsPage = "IsPage";
        public const string IsMenu = "IsMenu";

        public static AuthorizationPolicy IsPagePolicy()
        {
            return new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                                   .RequireClaim("UserMenu")
                                                   .AddRequirements(new ProfileOwnerRequirement("IsPagePolicy"))
                                                   .Build();
        }

        public static AuthorizationPolicy IIsMenuPolicy()
        {
            return new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                                   .RequireClaim("UserMenu")
                                                   .AddRequirements(new ProfileOwnerRequirement("IIsMenuPolicy"))
                                                   .Build();
        }
    }
}
