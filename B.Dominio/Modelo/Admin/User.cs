using System;
using System.Collections.Generic;

namespace Modelo.Admin
{
    public class User
    {
        public int id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string nombres { get; set; }
        public string apellidos { get; set; }
        public string nav_menu { get; set; }
        public int grupoid { get; set; }
        public string? grupo_nombre { get; set; }
        public string? rol_nombre { get; set; }
        public string? redmine_user { get; set; }

        // Estado y vínculos unificados (Operaciones + Admin/Odoo + Helpdesk/Redmine)
        public bool activo { get; set; } = true;
        public int? odoo_employee_id { get; set; }
        public int? codigo_empleado { get; set; }
        public string? empleado_nombre_admin { get; set; }
        public bool? empleado_activo_admin { get; set; }
        public int? odoo_user_id { get; set; }
        public string? odoo_user_login { get; set; }
        public bool? odoo_user_activo { get; set; }
        public int? redmine_user_id { get; set; }
        public int? redmine_status { get; set; }
        public string? foto_url { get; set; }
        public DateTime? fecha_baja { get; set; }
        public string? motivo_baja { get; set; }
    }

    public class UserLogin
    {
        public string Email_Address { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public bool Validate { get; set; }
    }
}
