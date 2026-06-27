using System.Collections.Generic;

namespace Modelo.Entidades.Entradas.Odoo
{
    public class nominatype
    {
        public string? cedula { set; get; } = string.Empty;
        public string? empleado { set; get; } = string.Empty;
        public string? tipoempleado { set; get; } = string.Empty;
        public decimal salbasico { set; get; } = 0;
        public decimal dias { set; get; } = 0;
        public decimal basico { set; get; } = 0;
        public decimal transporte { set; get; } = 0;
        public decimal alimento { set; get; } = 0;
        public decimal bonocump { set; get; } = 0;
        public decimal he { set; get; } = 0;
        public decimal horaextra { set; get; } = 0;
        public decimal vacaciones { set; get; } = 0;
        public decimal otrosing { set; get; } = 0;
        public decimal totaldev { set; get; } = 0;
        public decimal inss { set; get; } = 0;
        public decimal ir { set; get; } = 0;
        public decimal otdeduc { set; get; } = 0;
        public decimal totalded { set; get; } = 0;
        public decimal neto { set; get; } = 0;
        public decimal bi1000 { set; get; } = 0;
        public decimal bi500 { set; get; } = 0;
        public decimal bi200 { set; get; } = 0;
        public decimal bi100 { set; get; } = 0;
        public decimal bi50 { set; get; } = 0;
        public decimal bi20 { set; get; } = 0;
        public decimal bi10 { set; get; } = 0;
        public decimal bi5 { set; get; } = 0;
        public decimal bi1 { set; get; } = 0;
        public decimal bitotal { get { return (bi1000 * 1000) + (bi500 * 500) + (bi200 * 200) + (bi100 * 100) + (bi50 * 50) + (bi20 * 20) + (bi10 * 10) + (bi5 * 5) + bi1; } }

    }


    public class repnominapago
    {
        public string? fecha_ini { set; get; }
        public string? fecha_fin { set; get; }
        public List<nominaall>? nominatotal { set; get; }
        public List<nominaalltarjeta>? desglocetarjeta { set; get; }
        public List<nominadesgefectivo>? desgloceefectivo { set; get; }
        public string? titulo { set; get; }
    }
    public class nominaall
    {
        public string? cedula { set; get; } = string.Empty;
        public string? empleado { set; get; } = string.Empty;
        public string? tipoempleado { set; get; } = string.Empty;
        public bool tarjeta { set; get; } = false;
        public decimal salbasico { set; get; } = 0;
        public decimal dias { set; get; } = 0;
        public decimal basico { set; get; } = 0;
        public decimal transporte { set; get; } = 0;
        public decimal alimento { set; get; } = 0;
        public decimal bonocump { set; get; } = 0;
        public decimal he { set; get; } = 0;
        public decimal horaextra { set; get; } = 0;
        public decimal vacaciones { set; get; } = 0;
        public decimal otrosing { set; get; } = 0;
        public decimal totaldev { set; get; } = 0;
        public decimal inss { set; get; } = 0;
        public decimal ir { set; get; } = 0;
        public decimal otdeduc { set; get; } = 0;
        public decimal totalded { set; get; } = 0;
        public decimal neto { set; get; } = 0;
        public int pagina { set; get; } = 0;
    }

    public class nominaalltarjeta
    {
        public string? cedula { set; get; } = string.Empty;
        public string? empleado { set; get; } = string.Empty;
        public decimal neto { set; get; } = 0;

    }

    public class nominadesgefectivo
    {
        public string? cedula { set; get; } = string.Empty;
        public string? empleado { set; get; } = string.Empty;
        public decimal neto { set; get; } = 0;
        public decimal bi1000 { set; get; } = 0;
        public decimal bi500 { set; get; } = 0;
        public decimal bi200 { set; get; } = 0;
        public decimal bi100 { set; get; } = 0;
        public decimal bi50 { set; get; } = 0;
        public decimal bi20 { set; get; } = 0;
        public decimal bi10 { set; get; } = 0;
        public decimal bi5 { set; get; } = 0;
        public decimal bi1 { set; get; } = 0;
        public decimal bitotal { set; get; } = 0;
    }
}
