using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}

namespace ExtensionMethods
{
    public static class MyExtensions
    {
        public static string SelectSingleNodeAttrDef(this XmlNode node, string path, string defValue)
        {
            try
            {
                XmlNode n = node.SelectSingleNode(path);
                if (n is XmlAttribute)
                    return ((XmlAttribute)n).Value;
            }
            catch {
            }
            return defValue;
        }
    }
}
