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
                claimsIdentity = new ClaimsIdentity(new[]
                                {
                                    new Claim(ClaimTypes.Name, user.email),
                                    new Claim("UserMenu", user.nav_menu)
                                }, "apiauth_type");
            }

            return claimsIdentity;
        }
    }
}


