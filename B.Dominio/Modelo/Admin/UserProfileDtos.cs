using System;
using System.Collections.Generic;

namespace Modelo.Admin
{
    public class UserProfileDto
    {
        // Cuenta en Operaciones (admon.mstusuario)
        public int id { get; set; }
        public string username { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string nombres { get; set; } = string.Empty;
        public string apellidos { get; set; } = string.Empty;
        public int grupoid { get; set; }
        public string grupo_nombre { get; set; } = string.Empty;
        public string rol_nombre { get; set; } = string.Empty;
        public bool activo { get; set; } = true;
        public string? foto_url { get; set; }

        // Vínculos entre sistemas
        public int? odoo_employee_id { get; set; }
        public int? odoo_user_id { get; set; }
        public string? odoo_user_login { get; set; }
        public bool? odoo_user_activo { get; set; }
        public int? redmine_user_id { get; set; }
        public string? redmine_user { get; set; }
        public string? redmine_email { get; set; }
        public int? redmine_status { get; set; } // 1 = Activo, 2 = Registrado, 3 = Bloqueado

        // Datos Laborales Oficiales en Admin (Odoo hr_employee - Solo Lectura)
        public bool tiene_ficha_admin { get; set; }
        public int? codigo_empleado { get; set; }
        public string? nombre_completo_admin { get; set; }
        public string? cedula { get; set; }
        public string? inss { get; set; }
        public string? cargo { get; set; }
        public string? departamento { get; set; }
        public DateTime? fecha_ingreso { get; set; }
        public DateTime? fecha_nacimiento { get; set; }
        public bool? activo_en_admin { get; set; }

        // Datos Personales y de Contacto Editables por el Usuario (sincronizados con hr_employee en Admin)
        public string? telefono_corporativo { get; set; }
        public string? celular_corporativo { get; set; }
        public string? telefono_personal { get; set; }
        public string? correo_personal { get; set; }
        public string? direccion_domicilio { get; set; }
        public string? ciudad_domicilio { get; set; }
        public string? estado_civil { get; set; }
        public string? contacto_emergencia_nombre { get; set; }
        public string? contacto_emergencia_telefono { get; set; }
        public string? nivel_academico { get; set; }
        public string? profesion_estudio { get; set; }
        public string? centro_estudio { get; set; }
        public string? placa_vehiculo { get; set; }
    }

    public class UpdateMyProfileRequest
    {
        public string? foto_url { get; set; }
        public string? telefono_corporativo { get; set; }
        public string? celular_corporativo { get; set; }
        public string? telefono_personal { get; set; }
        public string? correo_personal { get; set; }
        public string? direccion_domicilio { get; set; }
        public string? ciudad_domicilio { get; set; }
        public string? estado_civil { get; set; }
        public string? contacto_emergencia_nombre { get; set; }
        public string? contacto_emergencia_telefono { get; set; }
        public string? nivel_academico { get; set; }
        public string? profesion_estudio { get; set; }
        public string? centro_estudio { get; set; }
        public string? placa_vehiculo { get; set; }
    }

    public class ChangeMyPasswordRequest
    {
        public string current_password_encrypted { get; set; } = string.Empty;
        public string new_password_encrypted { get; set; } = string.Empty;
    }

    public class OdooEmployeeOptionDto
    {
        public int id { get; set; }
        public int? codigo_empleado { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string? cedula { get; set; }
        public string? correo { get; set; }
        public string? cargo { get; set; }
        public string? departamento { get; set; }
        public bool activo { get; set; }
        public int? odoo_user_id { get; set; }
        public string? odoo_user_login { get; set; }

        public string DisplayLabel =>
            $"{(codigo_empleado.HasValue ? $"#{codigo_empleado} - " : "")}{nombre}{(!string.IsNullOrWhiteSpace(cargo) ? $" ({cargo})" : "")}{(activo ? "" : " [BAJA]")}";
    }

    public class OdooUserOptionDto
    {
        public int id { get; set; }
        public string login { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public bool activo { get; set; }

        public string DisplayLabel => $"{login} - {nombre}{(activo ? "" : " [INACTIVO]")}";
    }

    public class RedmineUserOptionDto
    {
        public int id { get; set; }
        public string login { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public int status { get; set; } // 1 = Activo, 2 = Registrado, 3 = Bloqueado
        public bool admin { get; set; }

        public string EffectiveHandle => !string.IsNullOrWhiteSpace(login) ? login : email;
        public string StatusText => status == 1 ? "Activo" : (status == 3 ? "Bloqueado" : "Registrado");
        public string DisplayLabel =>
            $"{(!string.IsNullOrWhiteSpace(login) ? $"@{login} - " : "")}{nombre}{(!string.IsNullOrWhiteSpace(email) ? $" <{email}>" : "")} [{StatusText}]";
    }

    public class UserLinkCatalogsDto
    {
        public List<OdooEmployeeOptionDto> empleados_admin { get; set; } = new();
        public List<OdooUserOptionDto> usuarios_admin_odoo { get; set; } = new();
        public List<RedmineUserOptionDto> usuarios_helpdesk { get; set; } = new();
    }

    public class ToggleUserStatusRequest
    {
        public int user_id { get; set; }
        public bool activar { get; set; }
        public string? motivo { get; set; }
    }

    public class ToggleUserStatusResultDto
    {
        public int user_id { get; set; }
        public string username { get; set; } = string.Empty;
        public bool activo { get; set; }
        public bool operaciones_afectado { get; set; }
        public bool admin_odoo_afectado { get; set; }
        public bool helpdesk_redmine_afectado { get; set; }
        public string resumen { get; set; } = string.Empty;
    }
}
