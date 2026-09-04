using System;

namespace Modelo.Entidades.Operaciones
{
    public class AsociacionOperacionDto
    {
        public int Id { get; set; }
        public int OperacionOrigenId { get; set; }
        public string? OperacionOrigenNombre { get; set; }
        public int OperacionDestinoId { get; set; }
        public string? OperacionDestinoNombre { get; set; }
        public bool Activo { get; set; } = true;
        public int? UserAdd { get; set; }
        public DateTime? DateAdd { get; set; }
    }
}
