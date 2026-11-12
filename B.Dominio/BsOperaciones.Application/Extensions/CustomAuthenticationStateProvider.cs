using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using MediatR;
using BsOperaciones.Application.Features.Admin.Queries;
using Modelo.Admin;
using Modelo.Interfaces;
using Modelo.Entidades;

namespace BsOperaciones.Application.Extensions
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ILocalStorageService _localStorageService { get; }
        private readonly IMediator _Mediator;
        private readonly IUserInfo _Iuser;
        public CustomAuthenticationStateProvider(ILocalStorageService localStorageService,
            IMediator mediator, IUserInfo Iuser)
        {
            _localStorageService = localStorageService;
            _Mediator = mediator;
            _Iuser    = Iuser;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var accessToken = await _localStorageService.GetItemAsync<string>("accessToken");
            ClaimsIdentity identity;

            if (accessToken != null && accessToken != string.Empty)
            {
                RefreshRequest model = new RefreshRequest() { AccessToken = accessToken };
                var response = await _Mediator.Send(new GetUserByAccessTokenQuery(model));
                if (!response.Respuesta.ExisteError)
                {
                    _Iuser.SetUserInfo(response.Model);
                    identity = GetClaimsIdentity(response.Model);
                }
                else
                    identity = new ClaimsIdentity();
            }
            else
            {
                identity = new ClaimsIdentity();
                _Iuser.SetUserInfo(new User());
                _Iuser.SetMenu(0);
                _Iuser.SetPermisos(new Menus());
            }

            var claimsPrincipal = new ClaimsPrincipal(identity);

            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }

        public async Task MarkUserAsAuthenticated(UserWithToken user)
        {
            await _localStorageService.SetItemAsync("accessToken", user.AccessToken);
            await _localStorageService.SetItemAsync("refreshToken", user.RefreshToken);

            var identity = GetClaimsIdentity(user);

            var claimsPrincipal = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _localStorageService.RemoveItemAsync("refreshToken");
            await _localStorageService.RemoveItemAsync("accessToken");

            var identity = new ClaimsIdentity();

            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private ClaimsIdentity GetClaimsIdentity(User user)
        {
            var claimsIdentity = new ClaimsIdentity();

            if (user != null)
            {
                var userNameOrEmail = !string.IsNullOrWhiteSpace(user.email) ? user.email : (!string.IsNullOrWhiteSpace(user.username) ? user.username : "user");
                var navMenu = !string.IsNullOrWhiteSpace(user.nav_menu) ? user.nav_menu : "[]";

                string roleLabel = !string.IsNullOrWhiteSpace(user.grupo_nombre)
                    ? user.grupo_nombre
                    : (!string.IsNullOrWhiteSpace(user.rol_nombre)
                        ? user.rol_nombre
                        : user.grupoid switch
                        {
                            4 => "Administrador",
                            3 => "Oficial Bussersa",
                            5 => "Oficial de Facturación",
                            7 => "Supervisor",
                            8 => "Coordinador OPM",
                            _ => "Colaborador"
                        });

                var claims = new System.Collections.Generic.List<Claim>
                {
                    new Claim(ClaimTypes.Name, userNameOrEmail),
                    new Claim(ClaimTypes.Role, roleLabel),
                    new Claim("GroupId", user.grupoid.ToString()),
                    new Claim("UserMenu", navMenu)
                };

                if (user.grupoid == 4)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                }

                claimsIdentity = new ClaimsIdentity(claims, "apiauth_type");
            }

            return claimsIdentity;
        }
    }
}


