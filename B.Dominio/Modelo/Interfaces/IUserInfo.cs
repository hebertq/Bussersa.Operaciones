using Modelo.Admin;
using Modelo.ClasesGenericas;
using Modelo.Entidades;

namespace Modelo.Interfaces
{
    /// <summary>
    /// Información de la transacción actual de un usuario
    /// </summary>
    public interface IUserInfo
    {
        public int UserId { get; }
        string UserName { get; }
        string? RedmineUser { get; }
        User? User { get; }
        int IdMenu { get; }
        public PermisosPagina Permisos {get;}
        void SetUserInfo(User info);
        void SetPermisos(Menus menu);
        void SetMenu(int menu);
    }
}
