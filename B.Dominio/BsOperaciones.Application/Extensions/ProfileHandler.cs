using Microsoft.AspNetCore.Authorization;
using Modelo.Entidades;
using Modelo.Interfaces;
using Newtonsoft.Json;
using System.Security.Claims;


namespace BsOperaciones.Application.Extensions
{
    public class ProfileHandler : AuthorizationHandler<ProfileOwnerRequirement>
    {
        private readonly IUserInfo _Iuser;
        public ProfileHandler(IUserInfo Iuser)
        {
            _Iuser = Iuser;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProfileOwnerRequirement requirement)
        {
            if (context.User.Identity.IsAuthenticated)
            {                
                var resource  = context.Resource?.ToString();
                var hasParsed = int.TryParse(resource, out int profileID);
                if (hasParsed)
                {
                    if (requirement.SessionHeaderName == "IsPagePolicy")
                    {
                        if (IsVisible(context.User, profileID))
                        {
                            context.Succeed(requirement);
                        }
                    }
                    else if (requirement.SessionHeaderName == "IIsMenuPolicy")
                    {
                        if (IsOwner(context.User, profileID))
                        {
                            context.Succeed(requirement);
                        }
                    }                       
                }
            }

            return Task.CompletedTask;
        }
        private bool IsVisible(ClaimsPrincipal user, int menu)
        {
            var LstMenus = ListaPaginas(user);
            _Iuser.SetPermisos(new Menus());
            _Iuser.SetMenu(menu);

            var _Pagina = LstMenus.Where(x => x.id == menu).FirstOrDefault();
            if (_Pagina == null && menu == 65 && LstMenus.Any(x => x.id == 61 || x.id == 22))
                return true;
            if (_Pagina == null)
                return false;
            else
                _Iuser.SetPermisos(_Pagina);

            return !string.IsNullOrEmpty(_Pagina.nombre);
        }
        private bool IsOwner(ClaimsPrincipal user, int menu)
        {
            var LstMenus = ListaPaginas(user);

            var _profileID = LstMenus.Where(x => x.id == menu).FirstOrDefault();
            if (_profileID == null)
            {
                if (menu == 65 && LstMenus.Any(x => x.id == 61 || x.id == 22))
                    return true;
                return false;
            }

            return !string.IsNullOrEmpty(_profileID.nombre);
        }

        private List<Menus> ListaPaginas(ClaimsPrincipal user)
        {
            var LstMenus = new List<Menus>();

            var MenuData = user?.Claims?.FirstOrDefault(x => x.Type == "UserMenu")?.Value;
            if (!string.IsNullOrEmpty(MenuData))
            {
                try
                {
                    LstMenus = JsonConvert.DeserializeObject<List<Menus>>(MenuData) ?? new List<Menus>();
                }
                catch
                {
                    LstMenus = new List<Menus>();
                }
            }

            return LstMenus; 
        }
    }
}
