using System.Xml.Serialization;

namespace Modelo.ClasesGenericas
{
    public class FileNameString
    {
        [XmlAttribute("fileName")]
        public string FileName { get; set; }
        [XmlText]
        public string File { get; set; }
    }
}
