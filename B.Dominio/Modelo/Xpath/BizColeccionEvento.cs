using System.Collections.Generic;
using System.Xml.Serialization;

namespace Modelo.Xpath
{
    [XmlType("Event")]
    public class BizColeccionEvento<Tmodel>
    {
        [XmlElement("EventData")]
        public BizDatosEvento BizDatosEvento { get; set; }

        [XmlArray("Entities")]
        public List<Tmodel> Entidades { get; set; }

        public BizColeccionEvento()
        {
            BizDatosEvento = new BizDatosEvento();
            Entidades = new List<Tmodel>();
        }
    }
}
