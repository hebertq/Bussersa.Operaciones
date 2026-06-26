namespace Modelo.Entidades.Nomina
{
    // Clase interna para manejar el mapeo exacto de la función JSON
    public class SeveranceDetail
    {
        public int id_severance { get; set; } // ID de tbl_severance_payment
        public int employee_id { get; set; }
        public int contrato { get; set; }
        public string nombre { get; set; }
        public string identificacion { get; set; }
        public decimal diasvac { get; set; }
        public decimal diasagui { get; set; }
        public decimal diasinde { get; set; }
        public decimal diasnomina { get; set; }
        public decimal horaextnom { get; set; }
        public decimal bonosnomina { get; set; }
        public decimal otrosingre { get; set; }
        public decimal prestamos { get; set; }
        public decimal otrasdeduc { get; set; }
    }
}
