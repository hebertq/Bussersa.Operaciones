namespace Modelo.Entidades.Entradas.Odoo
{
    public class DiaFeriado
    {
        public int anyo { set; get; } = 0;
        public int mes { set; get; } = 0;
        public int dia { set; get; } = 0;
        public string descripcion { set; get; } = "";
        public int municipio { set; get; } = 0;
    }
}
