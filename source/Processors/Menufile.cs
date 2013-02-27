using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace TRTR
{
    class MenuFile
    {
        private FileEntry entry;
        private UInt32 entryCount;
        private List<MenuFileEntry> menuEntries;
        private static TextConv textConv = new TextConv(new char[] { }, new char[] { }, Encoding.UTF8); // null;

        internal FileEntry Entry { get { return entry; } }
        internal List<MenuFileEntry> MenuEntries { get { return menuEntries; } }


        internal MenuFile(FileEntry entry)
        {
            menuEntries = new List<MenuFileEntry>();
            this.entry = entry;
            ParseFile();
        }

        void ParseFile()
        {
            // 4 byte = lang code (eng = 0x00)
            // 4 byte = entry count (+1?)
            // 4 byte = 0
            // entries

            byte[] content = entry.ReadContent();
            entryCount = BitConverter.ToUInt32(content, 4) - 1;
            
            for (Int32 i = 0; i < entryCount; i++)
            {
                MenuFileEntry menuEntry = new MenuFileEntry();
                menuEntry.index = i;
                menuEntry.Original = string.Empty;
                menuEntry.Current = string.Empty;
                menuEntry.Translation = string.Empty;
                menuEntry.EndIdx = 0;
                menuEntry.StartIdx = BitConverter.ToUInt32(content, (Int32)((i + 3) * 4));
                MenuEntries.Add(menuEntry);
            }

            // last processed non-empty entry
            MenuFileEntry lastValidEntry = null;
            // process all except last entry
            for (Int32 i = 0; i < entryCount; i++)
            {
                MenuFileEntry menuEntry = menuEntries[i];
                // StartIdx isn't zero if it has content
                if (menuEntry.StartIdx > 0)
                {
                    if (lastValidEntry != null)
                    {
                        UInt32 startIdx = menuEntry.StartIdx;

                        if (i == 1417)
                            lastValidEntry.EndIdx = 0;
                        lastValidEntry.EndIdx = menuEntry.StartIdx;
                        Int32 textLen = (Int32)(lastValidEntry.EndIdx - lastValidEntry.StartIdx - 1);
                        if (textLen < 0)
                            textLen = 0;
                        byte[] textBuf = new byte[textLen];
                        Array.Copy(content, lastValidEntry.StartIdx, textBuf, 0, textLen);

                        lastValidEntry.Current = textConv.Enc.GetString(textBuf);
                    }
                    lastValidEntry = menuEntry;
                }
            }
            Int32 lastNotNull = Array.IndexOf(content, (byte)0, (Int32)lastValidEntry.StartIdx) - 1;
            Int32 lastTextLen = lastNotNull - (Int32)lastValidEntry.StartIdx + 1;
            if (lastTextLen > 0)
            {
                byte[] lastTextBuf = new byte[lastTextLen];
                Array.Copy(content, lastValidEntry.StartIdx, lastTextBuf, 0, lastTextLen);
                lastValidEntry.EndIdx = (UInt32)lastNotNull;
                lastValidEntry.Current = textConv.Enc.GetString(lastTextBuf);
            }
        }

        internal void Translate()
        {
            MemoryStream msIndex = new MemoryStream();
            MemoryStream msEntries = new MemoryStream();
            try
            {
                msIndex.Write(BitConverter.GetBytes((Int32)FileLanguage.English), 0, 4);
                msIndex.Write(BitConverter.GetBytes(menuEntries.Count + 1), 0, 4);
                msIndex.Write(BitConverter.GetBytes(0), 0, 4);

                XmlNode menuNode = TRGameInfo.Trans.TranslationDocument.SelectSingleNode("/translation/menu");
                Int32 indexSize = (menuEntries.Count + 3) * 4;
                for (Int32 i = 0; i < menuEntries.Count; i++)
                {
                    Int32 indexOffset = 0;
                    MenuFileEntry menuEntry = menuEntries[i];
                    if (!menuEntry.Empty)
                    {
                        XmlNode node = menuNode.SelectSingleNode("entry[@no=\"" + i.ToString("d4") + "\"]");
                        if (node != null)
                        {
                            XmlAttribute attr = node.Attributes["translation"];
                            string translation = string.Empty;
                            if (attr != null)
                            {
                                try
                                {
                                    translation = attr.Value;
                                    if (translation.Length > 0)
                                    {
#if !DONT_CHECK_CHECKSUM
                                        //if (!Hash.Check(translation + "m" + i.ToString("d4") +
                                        //    TRGameInfo.InstallInfo.GameNameAbbrevFull, node.Attributes["checksum"].Value))
                                        //{
                                        //    Exception ex = new Exception(Errors.CorruptedTranslation);
                                        //    ex.Data.Add("entryNo", i.ToString("d4"));
                                        //    ex.Data.Add("storedChecksum", node.Attributes["checksum"].Value);
                                        //    ex.Data.Add("textChecksum", Hash.Get(translation + "m" + i.ToString("d4") +
                                        //        TRGameInfo.InstallInfo.GameNameAbbrevFull));
                                        //    throw ex;
                                        //}
#endif

                                        XmlAttribute setupAttr = node.Attributes["setup"];
                                        bool replaceChars = true;
                                        if (setupAttr != null)
                                            replaceChars = setupAttr.Value != "true";
                                        if (replaceChars)
                                            translation = MenuFile.textConv.ToGameFormat(attr.Value.Replace("\r\n", "\n")) + (char)(0);
                                        else
                                            translation = attr.Value.Replace("\r\n", "\n") + (char)(0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(Errors.CorruptedTranslation, ex);
                                }

                                byte[] textBuf = MenuFile.textConv.Enc.GetBytes(translation);
                                indexOffset = (Int32)msEntries.Length + indexSize;
                                msEntries.Write(textBuf, 0, textBuf.Length);
                            }
                        }
                    }
                    msIndex.Write(BitConverter.GetBytes(indexOffset), 0, 4);
                }
                msIndex.Position = msIndex.Length;
                msIndex.Write(msEntries.ToArray(), 0, (Int32)msEntries.Length);
                entry.WriteContent(msIndex.ToArray());
            }
            finally
            {
                msEntries.Close();
                msIndex.Close();
            }
        }

        internal void Restore()
        {
            MemoryStream msIndex = new MemoryStream();
            MemoryStream msEntries = new MemoryStream();
            try
            {
                msIndex.Write(BitConverter.GetBytes((Int32)FileLanguage.English), 0, 4);
                msIndex.Write(BitConverter.GetBytes(menuEntries.Count + 1), 0, 4);
                msIndex.Write(BitConverter.GetBytes(0), 0, 4);

                XmlNode menuNode = TRGameInfo.Trans.RestorationDocument.SelectSingleNode("/restoration/menu");
                Int32 entryCount = Convert.ToInt32(menuNode.Attributes["count"].Value);
                Int32 indexSize = (entryCount + 3) * 4;
                for (Int32 i = 0; i < entryCount; i++)
                {
                    Int32 indexOffset = 0;
                    XmlNode node = menuNode.SelectSingleNode("entry[@no=\"" + i.ToString("d4") + "\"]");
                    if (node != null)
                    {
                        byte[] textBuf = MenuFile.textConv.Enc.GetBytes(MenuFile.textConv.ToGameFormat(node.Attributes["original"].Value) + (char)(0));
                        indexOffset = (Int32)msEntries.Length + indexSize;
                        msEntries.Write(textBuf, 0, textBuf.Length);
                    }
                    msIndex.Write(BitConverter.GetBytes(indexOffset), 0, 4);
                }
                msIndex.Position = msIndex.Length;
                msIndex.Write(msEntries.ToArray(), 0, (Int32)msEntries.Length);
                entry.WriteContent(msIndex.ToArray());
            }
            finally
            {
                msEntries.Close();
                msIndex.Close();
            }
        }

        internal void CreateRestoration(XmlElement menuElement, XmlNode menuNode)
        {
            menuElement.SetAttribute("count", menuEntries.Count.ToString());
            for (Int32 i = 0; i < menuEntries.Count; i++)
            {
                MenuFileEntry menuEntry = menuEntries[i];
                if (menuEntry.Current.Length > 0)
                {
                    XmlElement entryElement = menuNode.OwnerDocument.CreateElement("entry");
                    XmlNode entryNode = menuElement.AppendChild(entryElement);
                    entryElement.SetAttribute("no", i.ToString("d4"));
                    entryElement.SetAttribute("original", MenuFile.textConv.ToOriginalFormat(menuEntry.Current));
                }
            }
        }
    }

    class MenuFileEntry
    {
        internal bool Empty { get { return StartIdx == 0; } }
        internal string Current;
        internal string Original;
        internal string Translation;
        internal UInt32 StartIdx;
        internal UInt32 EndIdx;
        internal Int32 index;
    }
}
