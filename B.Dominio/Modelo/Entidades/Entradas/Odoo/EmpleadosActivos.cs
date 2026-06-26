namespace Modelo.Entidades.Entradas.Odoo
{
    public class EmpleadosActivos
    {
        public int IdNomina { get; set; } = 0;
        public string NoCedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreCorto { get; set; } = string.Empty;
        public double NoInss { get; set; } = 0;
        public int NoContrato { get; set; } = 0;
        public bool ActivoInss { get; set; } = false;
        public AccionesEmp Accion { get; set; } = AccionesEmp.Ninguna;

    }

    public class EmpleadosActivosInss
    {
        public string NoCedula { get; set; } = string.Empty;
        public string PrimerNombre { get; set; } = string.Empty;
        public string PrimerApellido { get; set; } = string.Empty;
        public string SegundoNombre { get; set; } = string.Empty;
        public string SegundoApellido { get; set; } = string.Empty;
        public double NoInss { get; set; } = 0;

    }

    public enum AccionesEmp : int
    {
        Ninguna       = 1,
        Cedula        = 2,
        NoInss        = 3,
        Nuevo         = 4,
        NoActivoAdmin = 5,
        NoActivoInss  = 6
    }
}
