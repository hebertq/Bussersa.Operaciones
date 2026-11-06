using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using MudBlazor;
using Newtonsoft.Json;
using Modelo.Entidades;

namespace BsOperaciones.Services
{
    public class ModuloHerramientaItem
    {
        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public int MenuResourceId { get; set; }
        public string Color { get; set; } = "#2563eb";
        public string Badge { get; set; } = string.Empty;
    }

    public class ModuloAppItem
    {
        public string Clave { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;
        public string DescripcionLarga { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public string IconColor { get; set; } = "#0f172a";
        public string ColorFondoGradiente { get; set; } = string.Empty;
        public string ColorSombra { get; set; } = "rgba(15, 23, 42, 0.12)";
        public string RutaPrincipal { get; set; } = string.Empty;
        public int MenuResourceId { get; set; }
        public int[] ChildMenuIds { get; set; } = Array.Empty<int>();
        public string[] PrefijosRuta { get; set; } = Array.Empty<string>();
        public List<ModuloHerramientaItem> Herramientas { get; set; } = new();
    }

    public class ModuloNavigationService
    {
        public List<ModuloAppItem> TodosLosModulos { get; } = new()
        {
            // 1. MÓDULO OPERACIONES
            new ModuloAppItem
            {
                Clave = "operaciones",
                Titulo = "Operaciones",
                Subtitulo = "Control de campo, asistencias, movimientos y producción",
                DescripcionLarga = "Gestión centralizada de personal operativo en instalaciones y bodegas de clientes, registro de asistencia móvil, rotación de turnos y cierres de operaciones.",
                Icono = Icons.Material.Filled.PrecisionManufacturing,
                IconColor = "#1e3a8a", // Azul Marino Corporativo
                ColorFondoGradiente = "linear-gradient(135deg, #0f172a 0%, #1e3a8a 100%)",
                ColorSombra = "rgba(30, 58, 138, 0.25)",
                RutaPrincipal = "/operaciones",
                MenuResourceId = 15,
                ChildMenuIds = new[] { 15, 16, 18, 55, 56, 64, 36, 37, 57, 54, 58, 66, 67, 68 },
                PrefijosRuta = new[] { "/operaciones" },
                Herramientas = new()
                {
                    new ModuloHerramientaItem
                    {
                        Titulo = "Movimientos (Tablet)",
                        Subtitulo = "Registro de asistencia y control de personal en campo por bodega y turno.",
                        Icono = Icons.Material.Filled.TabletAndroid,
                        Ruta = "/operaciones/registro-movimientos-supervisor",
                        MenuResourceId = 55,
                        Color = "#2563eb",
                        Badge = "Móvil / Tablet"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Consultar Asistencia",
                        Subtitulo = "Reporte detallado en tiempo real de asistencias y novedades de colaboradores.",
                        Icono = Icons.Material.Filled.FactCheck,
                        Ruta = "/operaciones/consulta-asistencia",
                        MenuResourceId = 64,
                        Color = "#0284c7"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Cerrar Marcadas",
                        Subtitulo = "Cierre operativo de marcadas y preparación para facturación comercial.",
                        Icono = Icons.Material.Filled.CloseFullscreen,
                        Ruta = "/operaciones/marcadasxfacturar",
                        MenuResourceId = 16,
                        Color = "#0f766e"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Consultar Marcadas",
                        Subtitulo = "Auditoría e historial de cierres operativos de clientes finalizados.",
                        Icono = Icons.Material.Filled.FindInPage,
                        Ruta = "/operaciones/consulta-cierres",
                        MenuResourceId = 18,
                        Color = "#4338ca"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Cargar Movimientos",
                        Subtitulo = "Importación masiva de turnos y asistencias mediante plantillas Excel.",
                        Icono = Icons.Material.Filled.PostAdd,
                        Ruta = "/operaciones/movimientos-personal-excel",
                        MenuResourceId = 56,
                        Color = "#059669"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Producción Diaria",
                        Subtitulo = "Control de rendimientos diarios, asignación de asistencia y productos.",
                        Icono = Icons.Material.Filled.Assignment,
                        Ruta = "/operaciones/produccion-diaria",
                        MenuResourceId = 36,
                        Color = "#d97706"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Sellar Asistencias",
                        Subtitulo = "Agrupación y sellado de lotes de horas laboradas por cuadrilla.",
                        Icono = Icons.Material.Filled.AssignmentTurnedIn,
                        Ruta = "/operaciones/asistencia-produccion",
                        MenuResourceId = 57,
                        Color = "#0284c7"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Actividades (Tablet)",
                        Subtitulo = "Registro táctil de producción por variante y servicio (Pepsi / CBC).",
                        Icono = Icons.Material.Filled.TouchApp,
                        Ruta = "/operaciones/actividades-produccion",
                        MenuResourceId = 67,
                        Color = "#ea580c",
                        Badge = "Tablet / Táctil"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Rotación de Turnos",
                        Subtitulo = "Planificación y asignación de roles de turnos operativos para el personal.",
                        Icono = Icons.Material.Filled.SwapHorizontalCircle,
                        Ruta = "/operaciones/rotacion-turnos",
                        MenuResourceId = 37,
                        Color = "#7c3aed"
                    }
                }
            },

            // 2. MÓDULO ASISTENCIAS
            new ModuloAppItem
            {
                Clave = "asistencias",
                Titulo = "Asistencias",
                Subtitulo = "Control biométrico de marcadas y días laborados",
                DescripcionLarga = "Módulo de captura, procesamiento y auditoría de marcadas biométricas de personal y cálculo de días laborados por centro de costo.",
                Icono = Icons.Material.Filled.CalendarMonth,
                IconColor = "#0284c7",
                ColorFondoGradiente = "linear-gradient(135deg, #0369a1 0%, #0284c7 100%)",
                ColorSombra = "rgba(2, 132, 199, 0.25)",
                RutaPrincipal = "/asistencias",
                MenuResourceId = 3,
                ChildMenuIds = new[] { 3, 4, 5, 6 },
                PrefijosRuta = new[] { "/asistencias", "/asistencia" },
                Herramientas = new()
                {
                    new ModuloHerramientaItem
                    {
                        Titulo = "Ingresar Marcadas",
                        Subtitulo = "Carga y captura de marcadas de asistencia desde relojes biométricos.",
                        Icono = Icons.Material.Filled.CalendarMonth,
                        Ruta = "/asistencias/marcadas",
                        MenuResourceId = 4,
                        Color = "#0284c7"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Días Trabajados",
                        Subtitulo = "Resumen, validación y control de días trabajados por colaborador.",
                        Icono = Icons.Material.Filled.EditCalendar,
                        Ruta = "/asistencia/diastrabajados",
                        MenuResourceId = 5,
                        Color = "#0891b2"
                    }
                }
            },

            // 3. MÓDULO NÓMINA
            new ModuloAppItem
            {
                Clave = "nomina",
                Titulo = "Nómina",
                Subtitulo = "Cálculo de días por pagar, conciliación INSS y cierres",
                DescripcionLarga = "Administración integral de planillas, consolidación de pre-nómina quincenal, reglas de jornadas laborales y conciliación con el INSS.",
                Icono = Icons.Material.Filled.Payments,
                IconColor = "#059669",
                ColorFondoGradiente = "linear-gradient(135deg, #065f46 0%, #059669 100%)",
                ColorSombra = "rgba(5, 150, 105, 0.25)",
                RutaPrincipal = "/nomina",
                MenuResourceId = 8,
                ChildMenuIds = new[] { 8, 9, 21, 39, 51, 52, 60, 30, 31, 32 },
                PrefijosRuta = new[] { "/nomina", "/rrhh" },
                Herramientas = new()
                {
                    new ModuloHerramientaItem
                    {
                        Titulo = "Días por Pagar",
                        Subtitulo = "Cálculo consolidado de pre-nómina quincenal y días a pagar al personal.",
                        Icono = Icons.Material.Filled.RequestQuote,
                        Ruta = "/nomina/diasxpagar",
                        MenuResourceId = 9,
                        Color = "#059669"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Reglas de Horarios",
                        Subtitulo = "Catálogo de jornadas de trabajo y asignación de reglas por cliente.",
                        Icono = Icons.Material.Filled.FormatListBulleted,
                        Ruta = "/nomina/reglas-jornadas",
                        MenuResourceId = 39,
                        Color = "#10b981"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Conciliación Factura INSS",
                        Subtitulo = "Auditoría y conciliación cruzada de empleados cotizantes contra el INSS.",
                        Icono = Icons.Material.Filled.FactCheck,
                        Ruta = "/nomina/conciliacion-inss",
                        MenuResourceId = 60,
                        Color = "#0d9488"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Cierre de Nóminas Activas",
                        Subtitulo = "Cierre formal de nóminas activas y mensuales para el procesamiento contable.",
                        Icono = Icons.Material.Filled.LockClock,
                        Ruta = "/nomina/cerrar/activa",
                        MenuResourceId = 12,
                        Color = "#047857"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Descriptores de Puesto",
                        Subtitulo = "Gestión de descriptores de puesto, funciones, requisitos y competencias.",
                        Icono = Icons.Material.Filled.Assignment,
                        Ruta = "/rrhh/descriptores",
                        MenuResourceId = 31,
                        Color = "#0284c7"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Matriz de Descriptores",
                        Subtitulo = "Comparación y visualización matricial de descriptores por puesto.",
                        Icono = Icons.Material.Filled.TableChart,
                        Ruta = "/rrhh/matriz-descriptores",
                        MenuResourceId = 32,
                        Color = "#0891b2"
                    }
                }
            },

            // 4. MÓDULO COMERCIAL
            new ModuloAppItem
            {
                Clave = "comercial",
                Titulo = "Comercial",
                Subtitulo = "Costeo de servicios, presupuestos de personal y rentabilidad",
                DescripcionLarga = "Herramienta de presupuestación, cotización de tarifas de colaboradores, cálculo de márgenes comerciales y rentabilidad por contrato.",
                Icono = Icons.Material.Filled.BusinessCenter,
                IconColor = "#f97316", // Naranja Corporativo Bussersa
                ColorFondoGradiente = "linear-gradient(135deg, #c2410c 0%, #f97316 100%)",
                ColorSombra = "rgba(249, 115, 22, 0.25)",
                RutaPrincipal = "/comercial",
                MenuResourceId = 26,
                ChildMenuIds = new[] { 26, 27, 28, 29, 33, 34, 35, 38 },
                PrefijosRuta = new[] { "/comercial" },
                Herramientas = new()
                {
                    new ModuloHerramientaItem
                    {
                        Titulo = "Costeo de Personal",
                        Subtitulo = "Simulador y estimador de costos de colaboradores, prestaciones y márgenes.",
                        Icono = Icons.Material.Filled.Paid,
                        Ruta = "/comercial/costeo-personal",
                        MenuResourceId = 26,
                        Color = "#f97316"
                    }
                }
            },

            // 5. MÓDULO SUPERVISIÓN
            new ModuloAppItem
            {
                Clave = "supervision",
                Titulo = "Supervisión",
                Subtitulo = "Áreas del cliente, asignación de personal y viáticos",
                DescripcionLarga = "Administración de sedes y bodegas asignadas a cada supervisor, configuración de reglas de viáticos por ruta y dotación de personal.",
                Icono = Icons.Material.Filled.SupervisorAccount,
                IconColor = "#6366f1",
                ColorFondoGradiente = "linear-gradient(135deg, #4338ca 0%, #6366f1 100%)",
                ColorSombra = "rgba(99, 102, 241, 0.25)",
                RutaPrincipal = "/supervision",
                MenuResourceId = 61,
                ChildMenuIds = new[] { 61, 62, 63, 65 },
                PrefijosRuta = new[] { "/supervision", "/admon/supervision" },
                Herramientas = new()
                {
                    new ModuloHerramientaItem
                    {
                        Titulo = "Áreas del Cliente",
                        Subtitulo = "Configuración y mapeo de bodegas y sucursales donde opera Bussersa.",
                        Icono = Icons.Material.Filled.Domain,
                        Ruta = "/admon/supervision/areas-cliente",
                        MenuResourceId = 62,
                        Color = "#6366f1"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Personal por Cliente",
                        Subtitulo = "Asignación de colaboradores de campo a clientes y supervisores responsables.",
                        Icono = Icons.Material.Filled.Badge,
                        Ruta = "/admon/supervision/personal-cliente",
                        MenuResourceId = 63,
                        Color = "#8b5cf6"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Reglas de Viáticos",
                        Subtitulo = "Matriz de cálculo automático de viáticos, transporte y alimentación.",
                        Icono = Icons.Material.Filled.PriceCheck,
                        Ruta = "/admon/supervision/reglas-viaticos",
                        MenuResourceId = 65,
                        Color = "#a855f7"
                    }
                }
            },

            // 6. MÓDULO ADMINISTRACIÓN
            new ModuloAppItem
            {
                Clave = "admon",
                Titulo = "Administración",
                Subtitulo = "Usuarios, roles de seguridad y configuración del sistema",
                DescripcionLarga = "Panel de control administrativo para la gestión de cuentas, asignación de perfiles de acceso, administración de recursos y parámetros globales.",
                Icono = Icons.Material.Filled.Settings,
                IconColor = "#334155",
                ColorFondoGradiente = "linear-gradient(135deg, #0f172a 0%, #334155 100%)",
                ColorSombra = "rgba(51, 65, 85, 0.25)",
                RutaPrincipal = "/admon",
                MenuResourceId = 22,
                ChildMenuIds = new[] { 22, 23, 24, 25, 59 },
                PrefijosRuta = new[] { "/admon" },
                Herramientas = new()
                {
                    new ModuloHerramientaItem
                    {
                        Titulo = "Usuarios",
                        Subtitulo = "Creación, edición y control de acceso de cuentas de usuario.",
                        Icono = Icons.Material.Filled.People,
                        Ruta = "/admon/usuarios",
                        MenuResourceId = 23,
                        Color = "#334155"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Roles y Grupos",
                        Subtitulo = "Configuración de perfiles de usuario y permisos asignados por grupo.",
                        Icono = Icons.Material.Filled.Security,
                        Ruta = "/admon/roles",
                        MenuResourceId = 24,
                        Color = "#475569"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Menús del Sistema",
                        Subtitulo = "Administración de recursos, pantallas y árboles de navegación.",
                        Icono = Icons.Material.Filled.Menu,
                        Ruta = "/admon/menus",
                        MenuResourceId = 25,
                        Color = "#64748b"
                    },
                    new ModuloHerramientaItem
                    {
                        Titulo = "Firma de Correo",
                        Subtitulo = "Generación de firmas electrónicas institucionales para colaboradores.",
                        Icono = Icons.Material.Filled.Badge,
                        Ruta = "/admon/firma-correo",
                        MenuResourceId = 59,
                        Color = "#f97316"
                    }
                }
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

                // 4. Nómina (8 o 9 o 60 o 30 o 31 o 32)
                if (modulo.Clave == "nomina")
                {
                    return menus.Any(m => m.id == 8 || m.id == 9 || m.id == 60 || m.id == 30 || m.id == 31 || m.id == 32);
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

        public bool TienePermisoHerramienta(ClaimsPrincipal? user, ModuloHerramientaItem herramienta)
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

                return menus.Any(m => m.id == herramienta.MenuResourceId);
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

        public List<ModuloHerramientaItem> ObtenerHerramientasPermitidas(ClaimsPrincipal? user, ModuloAppItem modulo)
        {
            if (user == null || modulo == null) return new List<ModuloHerramientaItem>();
            return modulo.Herramientas.Where(h => TienePermisoHerramienta(user, h)).ToList();
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

            // Excepción específica: /admon/supervision o /supervision va a Supervisión
            if (url.StartsWith("admon/supervision") || url.StartsWith("supervision"))
            {
                return TodosLosModulos.FirstOrDefault(m => m.Clave == "supervision");
            }

            foreach (var mod in TodosLosModulos)
            {
                foreach (var prefijo in mod.PrefijosRuta)
                {
                    string p = prefijo.ToLowerInvariant().TrimStart('/');
                    if (url == p || url.StartsWith(p + "/"))
                    {
                        return mod;
                    }
                }
            }

            return null;
        }
    }
}
