using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using Modelo.Admin;

namespace BsOperaciones.Pages.LoginPages
{
    public partial class SignUp : ComponentBase
    {
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected ILocalStorageService localStorageService { get; set; }

        protected User user { get; set; }
        protected string LoginMesssage { get; set; }

        protected override Task OnInitializedAsync()
        {
            user = new User();
            return base.OnInitializedAsync();
        }

        protected async Task<bool> RegisterUser()
        {
            // assume that user is valid
            // user.Source = "APPC";
            // var returnedUser = await userService.RegisterUserAsync(user);

            // if(returnedUser.EmailAddress != null)
            // {   
            //    ((CustomAuthenticationStateProvider)AuthenticationStateProvider).MarkUserAsAuthenticated(returnedUser);
            //    NavigationManager.NavigateTo("/");
            // }
            // else
            // {
            //    LoginMesssage = "Invalid username or password";
            // }        
            
            return await Task.FromResult(true);
        }
    }
}
