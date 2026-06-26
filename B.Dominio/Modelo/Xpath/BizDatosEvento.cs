using System.Xml.Serialization;

namespace Modelo.Xpath
{
    public class BizDatosEvento
    {
        [XmlElement("idCase")]
        public int IdCase { get; set; }

        [XmlElement("eventName")]
        public string EventName { get; set; }

        public BizDatosEvento()
        {
            IdCase = 0;
            EventName = "";
        }
    }
}
