using System.Xml.Serialization;

namespace Modelo.Xpath
{
    [XmlType("Emisiondetarjetasinstant")]
    public class AvanzarEvent
    {
        [XmlElement("c_ctasCaso")]
        public EmiCtas entidad { set; get; }

        public AvanzarEvent()
        {
            entidad = new EmiCtas();
        }
    }

    public class EmiCtas
    {
        [XmlElement("ACD_cCuentasPropias")]
        public EmiCtasTarjetas CtaPropias { get; set; }
        public EmiCtas()
        {
            CtaPropias = new EmiCtasTarjetas();
        }
    }

    public class EmiCtasTarjetas
    {
        [XmlAttribute("businessKey")]
        public string businessKey { get; set; } = "noCuenta = ";
        [XmlElement("noCuenta")]
        public decimal noCuenta { get; set; }
        [XmlElement("m_TD")]
        public EmiTarejtas Tarjetas { get; set; }
        public EmiCtasTarjetas()
        {
            Tarjetas = new EmiTarejtas();
        }

        [XmlIgnore]
        public string AddKey
        {
            get { return businessKey; }
            set { businessKey = businessKey + value; }
        }
    }

    public class EmiTarejtas
    {
        [XmlElement("noTarjeta")]
        public decimal NoTarjeta { get; set; }
        [XmlElement("noGestion")]
        public int noGestion { get; set; }

        [XmlElement("noGestionBaja")]
        public int noGestionBaja { get; set; }

        [XmlElement("noTarDevuelta")]
        public decimal NoTarDevuelta { get; set; }
        public EmiTarejtas()
        {
            NoTarjeta = 0;
            noGestion = 0;
            noGestionBaja = 0;
            NoTarDevuelta = 0;
        }
    }
}
