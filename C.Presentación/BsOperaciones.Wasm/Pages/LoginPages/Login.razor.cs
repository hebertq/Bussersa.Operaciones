using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MediatR;
using MudBlazor;
using BsOperaciones.Application.Features.Admin.Commands;
using Modelo.Admin;
using Modelo.Interfaces;
using BsOperaciones.Services;
using Utilidades.Interfaces;
using BsOperaciones.Application.Extensions;

namespace BsOperaciones.Pages.LoginPages
{
    public partial class Login : ComponentBase
    {
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected IMediator _mediator { get; set; }
        [Inject] protected IUtilidades _Util { get; set; }
        [Inject] protected IUserInfo _Iuser { get; set; }

        protected UserLogin user = new();
        protected string LoginMesssage { get; set; }
        protected bool _isProcessing = false;

        // Lógica para mostrar/ocultar contraseña
        protected bool _passwordVisible = false;
        protected InputType _passwordInput = InputType.Password;
        protected string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

        [CascadingParameter]
        protected Task<AuthenticationState> authenticationStateTask { get; set; }

        protected async override Task OnInitializedAsync()
        {
            var authState = await authenticationStateTask;
            if (authState.User.Identity.IsAuthenticated)
            {
                NavigationManager.NavigateTo("/index");
            }
        }

        protected void TogglePasswordVisibility()
        {
            if (_passwordVisible)
            {
                _passwordVisible = false;
                _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
                _passwordInput = InputType.Password;
            }
            else
            {
                _passwordVisible = true;
                _passwordInputIcon = Icons.Material.Filled.Visibility;
                _passwordInput = InputType.Text;
            }
        }

        protected async Task ProcessLogin()
        {
            if (_isProcessing) return;

            _isProcessing = true;
            LoginMesssage = string.Empty;

            try
            {
                await ValidateUser();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoginException] {ex}");
                LoginMesssage = $"Error: {ex.Message} (Consulte F12 para detalles)";
            }
            finally
            {
                _isProcessing = false;
            }
        }

        protected async Task<bool> ValidateUser()
        {
            // Validaciones locales
            if (string.IsNullOrEmpty(user.Email_Address) || string.IsNullOrEmpty(user.Password))
            {
                LoginMesssage = "Por favor, complete todos los campos.";
                return false;
            }

            string originalPassword = user.Password;
            user.Password = _Util.Encrypt(user.Password);

            var response = await _mediator.Send(new AddLoginUserCommand(user));

            if (response.Model?.email != null)
            {
                await ((CustomAuthenticationStateProvider)AuthenticationStateProvider).MarkUserAsAuthenticated(response.Model);
                _Iuser.SetUserInfo(response.Model);

                var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                if (Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("redirectUrl", out var redirectUrl) &&
                    !string.IsNullOrWhiteSpace(redirectUrl) &&
                    !redirectUrl.ToString().Equals("login", StringComparison.OrdinalIgnoreCase) &&
                    !redirectUrl.ToString().Equals("Login", StringComparison.OrdinalIgnoreCase))
                {
                    NavigationManager.NavigateTo("/" + redirectUrl.ToString().TrimStart('/'));
                }
                else
                {
                    NavigationManager.NavigateTo("/index");
                }
                return true;
            }

            LoginMesssage = "Credenciales no válidas.";
            user.Password = originalPassword;
            return false;
        }
    }
}
