using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using MudBlazor;
using Newtonsoft.Json;
using Modelo.Entidades;

namespace BsOperaciones.Services
{
    public class ModuloAppItem
    {
        public string Clave { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public string IconColor { get; set; } = "#2563eb";
        public string ColorFondoGradiente { get; set; } = string.Empty;
        public string ColorSombra { get; set; } = "rgba(0,0,0,0.12)";
        public string RutaPrincipal { get; set; } = string.Empty;
        public int MenuResourceId { get; set; }
        public int[] ChildMenuIds { get; set; } = Array.Empty<int>();
        public string[] PrefijosRuta { get; set; } = Array.Empty<string>();
        public string ColorTexto { get; set; } = "#ffffff";
        public string BadgeTexto { get; set; } = string.Empty;
    }

    public class AppTileItem
    {
        public string Id { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;
        public string ModuloClave { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public string IconColor { get; set; } = "#2563eb";
        public string Ruta { get; set; } = string.Empty;
        public int MenuResourceId { get; set; }
        public bool EsModuloPrincipal { get; set; } = false;
        public string Categoria { get; set; } = "General";
    }

    public class ModuloNavigationService
    {
        public List<ModuloAppItem> TodosLosModulos { get; } = new()
        {
            new ModuloAppItem
            {
                Clave = "operaciones",
                Titulo = "Operaciones",
                Subtitulo = "Movimientos, Marcadas y Supervisión de Campo",
                Icono = Icons.Material.Filled.PrecisionManufacturing,
                IconColor = "#1d4ed8",
                ColorFondoGradiente = "linear-gradient(135deg, #1e3a8a 0%, #2563eb 100%)",
                ColorSombra = "rgba(37, 99, 235, 0.35)",
                RutaPrincipal = "/operaciones/registro-movimientos-supervisor",
                MenuResourceId = 15,
                ChildMenuIds = new[] { 15, 16, 18, 55, 56, 64, 36, 37, 57, 54, 58 },
                PrefijosRuta = new[] { "/operaciones" }
            },
            new ModuloAppItem
            {
                Clave = "asistencias",
                Titulo = "Asistencias",
                Subtitulo = "Control de Marcadas y Días Trabajados",
                Icono = Icons.Material.Filled.CalendarMonth,
                IconColor = "#0284c7",
                ColorFondoGradiente = "linear-gradient(135deg, #0284c7 0%, #38bdf8 100%)",
                ColorSombra = "rgba(2, 132, 199, 0.35)",
                RutaPrincipal = "/asistencias/marcadas",
                MenuResourceId = 3,
                ChildMenuIds = new[] { 3, 4, 5, 6 },
                PrefijosRuta = new[] { "/asistencias", "/asistencia" }
            },
            new ModuloAppItem
            {
                Clave = "nomina",
                Titulo = "Nómina",
                Subtitulo = "Días por Pagar, Conciliación INSS y Cierres",
                Icono = Icons.Material.Filled.Payments,
                IconColor = "#059669",
                ColorFondoGradiente = "linear-gradient(135deg, #059669 0%, #10b981 100%)",
                ColorSombra = "rgba(5, 150, 105, 0.35)",
                RutaPrincipal = "/nomina/diasxpagar",
                MenuResourceId = 8,
                ChildMenuIds = new[] { 8, 9, 21, 39, 51, 52, 60 },
                PrefijosRuta = new[] { "/nomina" }
            },
            new ModuloAppItem
            {
                Clave = "comercial",
                Titulo = "Comercial",
                Subtitulo = "Costeo de Servicios, Producción y Rentabilidad",
                Icono = Icons.Material.Filled.BusinessCenter,
                IconColor = "#d97706",
                ColorFondoGradiente = "linear-gradient(135deg, #d97706 0%, #f59e0b 100%)",
                ColorSombra = "rgba(217, 119, 6, 0.35)",
                RutaPrincipal = "/comercial/costeo-personal",
                MenuResourceId = 26,
                ChildMenuIds = new[] { 26, 27, 28, 29, 33, 34, 35, 38 },
                PrefijosRuta = new[] { "/comercial" }
            },
            new ModuloAppItem
            {
                Clave = "supervision",
                Titulo = "Supervisión",
                Subtitulo = "Áreas del Cliente, Personal y Reglas Viáticos",
                Icono = Icons.Material.Filled.SupervisorAccount,
                IconColor = "#7c3aed",
                ColorFondoGradiente = "linear-gradient(135deg, #7c3aed 0%, #a855f7 100%)",
                ColorSombra = "rgba(124, 58, 237, 0.35)",
                RutaPrincipal = "/admon/supervision/areas-cliente",
                MenuResourceId = 61,
                ChildMenuIds = new[] { 61, 62, 63, 65 },
                PrefijosRuta = new[] { "/admon/supervision" }
            },
            new ModuloAppItem
            {
                Clave = "admon",
                Titulo = "Administración",
                Subtitulo = "Usuarios, Roles y Menús del Sistema",
                Icono = Icons.Material.Filled.Settings,
                IconColor = "#334155",
                ColorFondoGradiente = "linear-gradient(135deg, #334155 0%, #64748b 100%)",
                ColorSombra = "rgba(51, 65, 85, 0.35)",
                RutaPrincipal = "/admon/usuarios",
                MenuResourceId = 22,
                ChildMenuIds = new[] { 22, 23, 24, 25, 59 },
                PrefijosRuta = new[] { "/admon" }
            }
        };

        public List<AppTileItem> TodosLosTiles { get; } = new()
        {
            // Módulos Principales (Estilo Odoo Apps)
            new AppTileItem
            {
                Id = "mod-operaciones",
                Titulo = "Operaciones",
                Subtitulo = "Movimientos y Marcadas",
                ModuloClave = "operaciones",
                Icono = Icons.Material.Filled.PrecisionManufacturing,
                IconColor = "#1d4ed8",
                Ruta = "/operaciones/registro-movimientos-supervisor",
                MenuResourceId = 15,
                EsModuloPrincipal = true,
                Categoria = "Operaciones"
            },
            new AppTileItem
            {
                Id = "app-tablet",
                Titulo = "Movimientos (Tablet)",
                Subtitulo = "Asistencia en Campo",
                ModuloClave = "operaciones",
                Icono = Icons.Material.Filled.TabletAndroid,
                IconColor = "#3b82f6",
                Ruta = "/operaciones/registro-movimientos-supervisor",
                MenuResourceId = 55,
                EsModuloPrincipal = false,
                Categoria = "Operaciones"
            },
            new AppTileItem
            {
                Id = "app-consultar-asistencia",
                Titulo = "Consultar Asistencia",
                Subtitulo = "Reporte de Asistencias",
                ModuloClave = "operaciones",
                Icono = Icons.Material.Filled.FactCheck,
                IconColor = "#0284c7",
                Ruta = "/operaciones/consulta-asistencia",
                MenuResourceId = 64,
                EsModuloPrincipal = false,
                Categoria = "Operaciones"
            },
            new AppTileItem
            {
                Id = "app-cerrar-marcadas",
                Titulo = "Cerrar Marcadas",
                Subtitulo = "Cierres por Facturar",
                ModuloClave = "operaciones",
                Icono = Icons.Material.Filled.CloseFullscreen,
                IconColor = "#0ea5e9",
                Ruta = "/operaciones/marcadasxfacturar",
                MenuResourceId = 16,
                EsModuloPrincipal = false,
                Categoria = "Operaciones"
            },
            new AppTileItem
            {
                Id = "app-produccion-diaria",
                Titulo = "Producción Diaria",
                Subtitulo = "Control de Producción",
                ModuloClave = "operaciones",
                Icono = Icons.Material.Filled.Assignment,
                IconColor = "#6366f1",
                Ruta = "/operaciones/produccion-diaria",
                MenuResourceId = 36,
                EsModuloPrincipal = false,
                Categoria = "Operaciones"
            },
            new AppTileItem
            {
                Id = "mod-asistencias",
                Titulo = "Asistencias",
                Subtitulo = "Ingreso y Control",
                ModuloClave = "asistencias",
                Icono = Icons.Material.Filled.CalendarMonth,
                IconColor = "#0284c7",
                Ruta = "/asistencias/marcadas",
                MenuResourceId = 3,
                EsModuloPrincipal = true,
                Categoria = "RRHH"
            },
            new AppTileItem
            {
                Id = "app-dias-trabajados",
                Titulo = "Días Trabajados",
                Subtitulo = "Reporte de Días",
                ModuloClave = "asistencias",
                Icono = Icons.Material.Filled.EditCalendar,
                IconColor = "#06b6d4",
                Ruta = "/asistencia/diastrabajados",
                MenuResourceId = 5,
                EsModuloPrincipal = false,
                Categoria = "RRHH"
            },
            new AppTileItem
            {
                Id = "mod-nomina",
                Titulo = "Nómina",
                Subtitulo = "Cálculo y Cierres",
                ModuloClave = "nomina",
                Icono = Icons.Material.Filled.Payments,
                IconColor = "#059669",
                Ruta = "/nomina/diasxpagar",
                MenuResourceId = 8,
                EsModuloPrincipal = true,
                Categoria = "Nómina"
            },
            new AppTileItem
            {
                Id = "app-dias-pagar",
                Titulo = "Días por Pagar",
                Subtitulo = "Planilla y Días",
                ModuloClave = "nomina",
                Icono = Icons.Material.Filled.RequestQuote,
                IconColor = "#10b981",
                Ruta = "/nomina/diasxpagar",
                MenuResourceId = 9,
                EsModuloPrincipal = false,
                Categoria = "Nómina"
            },
            new AppTileItem
            {
                Id = "app-conciliacion-inss",
                Titulo = "Conciliación INSS",
                Subtitulo = "Factura INSS",
                ModuloClave = "nomina",
                Icono = Icons.Material.Filled.RuleFolder,
                IconColor = "#14b8a6",
                Ruta = "/nomina/conciliacion-inss",
                MenuResourceId = 60,
                EsModuloPrincipal = false,
                Categoria = "Nómina"
            },
            new AppTileItem
            {
                Id = "mod-comercial",
                Titulo = "Comercial",
                Subtitulo = "Costeo y Rentabilidad",
                ModuloClave = "comercial",
                Icono = Icons.Material.Filled.BusinessCenter,
                IconColor = "#d97706",
                Ruta = "/comercial/costeo-personal",
                MenuResourceId = 26,
                EsModuloPrincipal = true,
                Categoria = "Comercial"
            },
            new AppTileItem
            {
                Id = "mod-supervision",
                Titulo = "Supervisión",
                Subtitulo = "Áreas y Personal",
                ModuloClave = "supervision",
                Icono = Icons.Material.Filled.SupervisorAccount,
                IconColor = "#7c3aed",
                Ruta = "/admon/supervision/areas-cliente",
                MenuResourceId = 61,
                EsModuloPrincipal = true,
                Categoria = "Supervisión"
            },
            new AppTileItem
            {
                Id = "app-areas-cliente",
                Titulo = "Áreas del Cliente",
                Subtitulo = "Configuración Áreas",
                ModuloClave = "supervision",
                Icono = Icons.Material.Filled.Domain,
                IconColor = "#8b5cf6",
                Ruta = "/admon/supervision/areas-cliente",
                MenuResourceId = 62,
                EsModuloPrincipal = false,
                Categoria = "Supervisión"
            },
            new AppTileItem
            {
                Id = "app-personal-cliente",
                Titulo = "Personal Cliente",
                Subtitulo = "Asignación Supervisión",
                ModuloClave = "supervision",
                Icono = Icons.Material.Filled.Badge,
                IconColor = "#a855f7",
                Ruta = "/admon/supervision/personal-cliente",
                MenuResourceId = 63,
                EsModuloPrincipal = false,
                Categoria = "Supervisión"
            },
            new AppTileItem
            {
                Id = "app-reglas-viaticos",
                Titulo = "Reglas Viáticos",
                Subtitulo = "Matriz de Viáticos",
                ModuloClave = "supervision",
                Icono = Icons.Material.Filled.PriceCheck,
                IconColor = "#c026d3",
                Ruta = "/admon/supervision/reglas-viaticos",
                MenuResourceId = 65,
                EsModuloPrincipal = false,
                Categoria = "Supervisión"
            },
            new AppTileItem
            {
                Id = "mod-admon",
                Titulo = "Ajustes / Admon",
                Subtitulo = "Usuarios y Seguridad",
                ModuloClave = "admon",
                Icono = Icons.Material.Filled.Settings,
                IconColor = "#334155",
                Ruta = "/admon/usuarios",
                MenuResourceId = 22,
                EsModuloPrincipal = true,
                Categoria = "Administración"
            },
            new AppTileItem
            {
                Id = "app-usuarios",
                Titulo = "Usuarios",
                Subtitulo = "Gestión de Cuentas",
                ModuloClave = "admon",
                Icono = Icons.Material.Filled.People,
                IconColor = "#475569",
                Ruta = "/admon/usuarios",
                MenuResourceId = 23,
                EsModuloPrincipal = false,
                Categoria = "Administración"
            },
            new AppTileItem
            {
                Id = "app-roles",
                Titulo = "Roles y Grupos",
                Subtitulo = "Control de Accesos",
                ModuloClave = "admon",
                Icono = Icons.Material.Filled.Security,
                IconColor = "#64748b",
                Ruta = "/admon/roles",
                MenuResourceId = 24,
                EsModuloPrincipal = false,
                Categoria = "Administración"
            }
        };

        public bool TienePermiso(ClaimsPrincipal? user, ModuloAppItem modulo)
        {
            if (user == null || !user.Identity?.IsAuthenticated == true) return false;

            string name = user.Identity?.Name?.ToLowerInvariant() ?? "";
            if (user.IsInRole("Admin") || user.IsInRole("Administrador") ||
                name == "admin" || name == "admin@bussersa.com" || name == "hquintero" || name == "hquintero@bussersa.com")
            {
                return true;
            }

            var menuClaim = user.Claims.FirstOrDefault(x => x.Type == "UserMenu")?.Value;
            if (string.IsNullOrWhiteSpace(menuClaim)) return false;

            try
            {
                var menus = JsonConvert.DeserializeObject<List<Menus>>(menuClaim);
                if (menus == null || !menus.Any()) return false;

                // 1. Supervisión (61) o cualquiera de sus submódulos
                if (modulo.Clave == "supervision")
                {
                    return menus.Any(m => m.id == 61 || m.id == 62 || m.id == 63 || m.id == 65);
                }

                // 2. Operaciones (15) o cualquiera de sus accesos
                if (modulo.Clave == "operaciones")
                {
                    return menus.Any(m => m.id == 15 || m.id == 55 || m.id == 64 || m.id == 16 || m.id == 18 || m.id == 56 || m.id == 36);
                }

                // 3. Asistencias (3)
                if (modulo.Clave == "asistencias")
                {
                    return menus.Any(m => m.id == 3 || m.id == 4 || m.id == 5);
                }

                // 4. Nómina (8 o 9 o 60)
                if (modulo.Clave == "nomina")
                {
                    return menus.Any(m => m.id == 8 || m.id == 9 || m.id == 60);
                }

                // 5. Comercial (26 o 27 o 33)
                if (modulo.Clave == "comercial")
                {
                    return menus.Any(m => m.id == 26 || m.id == 27 || m.id == 33);
                }

                // 6. Administración (22 o 23 o 24 o 25)
                if (modulo.Clave == "admon")
                {
                    return menus.Any(m => m.id == 22 || m.id == 23 || m.id == 24 || m.id == 25);
                }

                // Regla general directa
                if (menus.Any(m => m.id == modulo.MenuResourceId))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool TienePermisoTile(ClaimsPrincipal? user, AppTileItem tile)
        {
            if (user == null || !user.Identity?.IsAuthenticated == true) return false;

            string name = user.Identity?.Name?.ToLowerInvariant() ?? "";
            if (user.IsInRole("Admin") || user.IsInRole("Administrador") ||
                name == "admin" || name == "admin@bussersa.com" || name == "hquintero" || name == "hquintero@bussersa.com")
            {
                return true;
            }

            var menuClaim = user.Claims.FirstOrDefault(x => x.Type == "UserMenu")?.Value;
            if (string.IsNullOrWhiteSpace(menuClaim)) return false;

            try
            {
                var menus = JsonConvert.DeserializeObject<List<Menus>>(menuClaim);
                if (menus == null || !menus.Any()) return false;

                return menus.Any(m => m.id == tile.MenuResourceId);
            }
            catch
            {
                return false;
            }
        }

        public List<ModuloAppItem> ObtenerModulosPermitidos(ClaimsPrincipal? user)
        {
            if (user == null) return new List<ModuloAppItem>();
            return TodosLosModulos.Where(m => TienePermiso(user, m)).ToList();
        }

        public List<AppTileItem> ObtenerTilesPermitidos(ClaimsPrincipal? user)
        {
            if (user == null) return new List<AppTileItem>();
            return TodosLosTiles.Where(t => TienePermisoTile(user, t)).ToList();
        }

        public ModuloAppItem? DetectarModuloPorUrl(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return null;

            string url = relativeUrl.ToLowerInvariant().Trim();
            if (url.StartsWith("/")) url = url.Substring(1);

            // Si está en index o modulos o login, no hay módulo seleccionado
            if (string.IsNullOrWhiteSpace(url) || url == "index" || url == "modulos" || url == "apps")
            {
                return null;
            }

            // Excepción específica: /admon/supervision va a Supervisión
            if (url.StartsWith("admon/supervision"))
            {
                return TodosLosModulos.FirstOrDefault(m => m.Clave == "supervision")
                    ?? TodosLosModulos.FirstOrDefault(m => m.Clave == "admon");
            }

            foreach (var mod in TodosLosModulos)
            {
                foreach (var prefijo in mod.PrefijosRuta)
                {
                    string p = prefijo.ToLowerInvariant().TrimStart('/');
                    if (url.StartsWith(p))
                    {
                        return mod;
                    }
                }
            }

            return null;
        }
    }
}