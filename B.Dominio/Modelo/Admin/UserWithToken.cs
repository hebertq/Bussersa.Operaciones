namespace Modelo.Admin
{
    public class UserWithToken : User
    {      
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public void SetData(User user)
        {
            this.id          = user.id;
            this.email       = user.email;
            this.nombres     = user.nombres;
            this.apellidos   = user.apellidos;
            this.grupoid     = user.grupoid;
            this.nav_menu    = user.nav_menu; 
            this.password    = string.Empty;
        }
    }
}
