using System;
using System.Xml.Serialization;

namespace Modelo.ClasesGenericas
{
    [XmlRoot(ElementName = "GenericResponse")]
    public class RespBoot
    {
        [XmlElement("ResponseCode")]
        public int ResponseCode { get; set; } = 0;
        [XmlElement("Mensaje")]
        public string Mensaje { get; set; } = string.Empty;
        [XmlElement("IdTransaccion")]
        public int IdTransaccion { get; set; } = 0;
        [XmlElement("TiempoEjecucion")]
        public int TiempoEjecucion { get; set; } = 0;
        [XmlElement("EstadoTransaccion")]
        public int EstadoTransaccion { get; set; } = 0;
    }

    public class Transacciones
    {
        public DateTime? FechaError { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public DateTime? FechaGenera { get; set; }
        public DateTime? FechaInicia { get; set; }
        public int IdEstado { get; set; } = 0;
        public int IdSistemaEnvia { get; set; } = 0;
        public int IdTransaccion { get; set; } = 0;
        public int Intentos { get; set; } = 0;
        string MensajeError { get; set; } = string.Empty;
    }
}