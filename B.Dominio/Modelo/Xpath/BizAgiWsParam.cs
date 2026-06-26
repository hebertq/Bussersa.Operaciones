using System.Collections.Generic;
using System.Xml.Serialization;

namespace Modelo.Xpath
{
    [XmlRoot(ElementName = "BizAgiWSParam")]
    public class BizAgiWsParam<Tmodel>
    {
        [XmlElement("domain")]
        public string Domain { get; set; }

        [XmlElement("userName")]
        public string UserName { get; set; }

        [XmlArray("Events")]
        public List<BizColeccionEvento<Tmodel>> BizEvento { get; set; }

        public BizAgiWsParam()
        {
            Domain = "";
            UserName = "";
            BizEvento = new List<BizColeccionEvento<Tmodel>>();
        }
    }
}
