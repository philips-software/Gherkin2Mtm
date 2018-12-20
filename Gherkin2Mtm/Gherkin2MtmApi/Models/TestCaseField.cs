using System;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]
namespace Gherkin2MtmApi.Models
{
    [Serializable]
    public class TestCaseField
    {
        [System.Xml.Serialization.XmlElement("Name")]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlElement("Tag")]
        public string Tag { get; set; }

        [System.Xml.Serialization.XmlElement("Required")]
        public bool Required { get; set; }

        [System.Xml.Serialization.XmlElement("AllowMultiple")]
        public bool AllowMultiple { get; set; }

        [System.Xml.Serialization.XmlElement("Prefix")]
        public string Prefix { get; set; }
    }
}
