using MudBlazor;

namespace BsOperaciones.Services
{
    public class AppSettings
    {
        public bool DarkMode { get; set; }

        // MudBlazor equivalent concepts
        public DrawerVariant NavigationVariant { get; set; } = DrawerVariant.Persistent;
        public Color NavigationBackground { get; set; } = Color.Default;
        public bool NavigationOpen { get; set; } = true;
    }
}
