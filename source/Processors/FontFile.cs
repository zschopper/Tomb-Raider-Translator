using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TRTR
{

    class FontFile
    {
        internal byte[] Original;
        internal byte[] Translation;
        internal byte[] Current;
        private FileEntry entry;

        internal FontFile(FileEntry entry)
        {
            this.entry = entry;
            ParseFile();
        }

        void ParseFile()
        {
            Current = entry.ReadContent();
        }

        internal void Translate(bool simulated)
        {
            XmlNode fontNode = TRGameInfo.Trans.TranslationDocument.SelectSingleNode("/translation/font");
            if (fontNode != null)
            {
                XmlAttribute attr = fontNode.Attributes["translation"];
                if (attr != null)
                {
                    Translation = HexEncode.Decode(attr.Value);
                    entry.WriteContent(Translation);
                }
            }
        }
        internal void Restore()
        {
            XmlNode fontNode = TRGameInfo.Trans.RestorationDocument.SelectSingleNode("/restoration/font");
            if (fontNode != null)
            {
                XmlAttribute attr = fontNode.Attributes["original"];
                if (attr != null)
                {
                    Original = HexEncode.Decode(attr.Value);
                    entry.WriteContent(Original);
                }
            }
        }

        internal void CreateRestoration(XmlElement fontElement, XmlNode fontNode)
        {
            fontElement.SetAttribute("original", HexEncode.Encode(entry.ReadContent()));
        }
    }


}
