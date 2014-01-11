using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnusefulPlayer
{
    public class Skin
    {
        public Dictionary<string, System.IO.MemoryStream> Resources { get; set; }
        public System.Xml.XmlDocument Xml { get; set; }
        public Skin(System.Xml.XmlDocument xml, Dictionary<string, System.IO.MemoryStream> resources)
        {
            this.Resources = resources;
            this.Xml = xml;
        }
    }
}
