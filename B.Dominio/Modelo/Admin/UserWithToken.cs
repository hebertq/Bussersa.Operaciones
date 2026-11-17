namespace Modelo.Admin
{
    public class UserWithToken : User
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public void SetData(User user)
        {
            this.id                    = user.id;
            this.username              = user.username;
            this.email                 = user.email;
            this.nombres               = user.nombres;
            this.apellidos             = user.apellidos;
            this.grupoid               = user.grupoid;
            this.grupo_nombre          = user.grupo_nombre;
            this.rol_nombre            = user.rol_nombre;
            this.redmine_user          = user.redmine_user;
            this.nav_menu              = user.nav_menu;
            this.activo                = user.activo;
            this.odoo_employee_id      = user.odoo_employee_id;
            this.codigo_empleado       = user.codigo_empleado;
            this.empleado_nombre_admin = user.empleado_nombre_admin;
            this.empleado_activo_admin = user.empleado_activo_admin;
            this.odoo_user_id          = user.odoo_user_id;
            this.odoo_user_login       = user.odoo_user_login;
            this.odoo_user_activo      = user.odoo_user_activo;
            this.redmine_user_id       = user.redmine_user_id;
            this.redmine_status        = user.redmine_status;
            this.foto_url              = user.foto_url;
            this.fecha_baja            = user.fecha_baja;
            this.motivo_baja           = user.motivo_baja;
            this.password              = string.Empty;
        }
    }
}
