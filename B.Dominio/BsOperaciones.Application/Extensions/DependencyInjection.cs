using BsOperaciones.Application.Common.Strategy.Reloj;
using Blazored.LocalStorage;
using HostService.Clases;
using HostService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Utilidades.ClasesGenericas;
using Utilidades.Interfaces;

namespace BsOperaciones.Application.Extensions
{
    public static class DependencyInjection
    {
        public static void AddAuthorizationPolicy(this IServiceCollection services)
        {
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            services.AddScoped<IAuthorizationHandler, ProfileHandler>();

            services.AddAuthorizationCore(config =>
            {
                config.AddPolicy(Policies.IsPage, Policies.IsPagePolicy());
                config.AddPolicy(Policies.IsMenu, Policies.IIsMenuPolicy());
            });

            services.AddBlazoredLocalStorage();
        }
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly));
            services.AddAutoMapper(typeof(DependencyInjection).Assembly);
            
            services.AddScoped<RelojSelector>();
            services.AddScoped<IUtilidades, Utilidad>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IOdooService, OdooService>();
            services.AddScoped<IRedmineService, RedmineService>();
        }       
    }
}
