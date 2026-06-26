using Modelo.Admin;
using Modelo.Entidades;
using Modelo.Interfaces;

namespace Modelo.ClasesGenericas
{
    /// <summary>
    /// Información de la transacción actual de un usuario
    /// </summary>  
    public class UserInfo : IUserInfo
    {
        private User Info { set; get; }
        private int MenuId { set; get; }
        private PermisosPagina InfoPermisos { set; get; }
        public int UserId { get { return Info.id; } }
        public string? UserName { get { return Info.username; } }
        public int IdMenu { get { return MenuId; } }
        public PermisosPagina Permisos { get { return InfoPermisos; } }
        public void SetPermisos(Menus menu)
        {
            InfoPermisos = new PermisosPagina();
            InfoPermisos.SetPermisos(menu);
        }
        public void SetUserInfo(User info)
        {
            this.Info = info;
        }
        public void SetMenu(int menu)
        {
            MenuId = menu;
        }
    }
}