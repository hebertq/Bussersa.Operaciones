using System;

namespace Modelo.Comercial
{
    public class CatalogoEpp
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal MesesProrrateo { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal CostoMensual { get; set; }
    }

    public class CatalogoViatico
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal CostoMensual { get; set; }
    }

    public class CatalogoMaquinaria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public int MesesProyeccion { get; set; }
        public decimal ProyeccionMensual { get; set; }
    }

    public class CatalogoMaterial
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class CatalogoResponse
    {
        public System.Collections.Generic.List<CatalogoEpp> Epp { get; set; } = new System.Collections.Generic.List<CatalogoEpp>();
        public System.Collections.Generic.List<CatalogoViatico> Viaticos { get; set; } = new System.Collections.Generic.List<CatalogoViatico>();
        public System.Collections.Generic.List<CatalogoMaquinaria> Maquinaria { get; set; } = new System.Collections.Generic.List<CatalogoMaquinaria>();
        public System.Collections.Generic.List<CatalogoMaterial> Materiales { get; set; } = new System.Collections.Generic.List<CatalogoMaterial>();
    }
}
