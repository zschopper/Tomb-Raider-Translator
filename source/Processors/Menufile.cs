using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Resources; //resource writer

namespace TRTR
{
    class MenuFile
    {
        #region private declarations
        private FileEntry entry;
        private UInt32 entryCount;
        private UInt32 entryCount1;
        private UInt32 entryCount2;
        private List<MenuFileEntry> menuEntries;
        private static TextConv textConv = new TextConv(new char[] { }, new char[] { }, Encoding.UTF8); // null;
        #endregion

        internal FileEntry Entry { get { return entry; } }
        internal List<MenuFileEntry> MenuEntries { get { return menuEntries; } }

        // constructor
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

            entryCount1 = BitConverter.ToUInt32(content, 4);
            entryCount2 = BitConverter.ToUInt32(content, 8);
            entryCount = entryCount1 + entryCount2;
            Log.LogDebugMsg(string.Format("Menu parsing: {0} Entry count: {1} ({2}+{3}), ", entry.Extra.FileName, entryCount, entryCount1, entryCount2));

            int validEntryCount = 0;
            for (int i = 0; i < entryCount; i++)
            {
                //bigfilev3
                MenuFileEntry menuEntry = new MenuFileEntry();
                menuEntry.Index = i;
                menuEntry.StartIdx = BitConverter.ToUInt32(content, (Int32)((i + 3) * 4));
                menuEntry.PlaceHolder = (menuEntry.StartIdx <= entryCount * 4);
                MenuEntries.Add(menuEntry);
                if (menuEntry.PlaceHolder)
                    validEntryCount++;
            }

            entryCount = (UInt32)(MenuEntries.Count);

            // last processed non-empty entry
            MenuFileEntry lastValidEntry = null;
            // process all except last entry

            int debugPlaceHolderCount = 0;
            int debugValidEntryCount = 0;
            for (int i = 0; i < entryCount; i++)
            {
                MenuFileEntry menuEntry = menuEntries[i];
                // StartIdx isn't zero if it has content
                if (menuEntry.PlaceHolder)
                    debugPlaceHolderCount++;
                if ((menuEntry.StartIdx > 0) && !menuEntry.PlaceHolder)
                {
                    if (lastValidEntry != null)
                    {
                        UInt32 startIdx = menuEntry.StartIdx;

                        lastValidEntry.EndIdx = menuEntry.StartIdx;
                        Int32 textLen = (Int32)(lastValidEntry.EndIdx - lastValidEntry.StartIdx - 1);
                        if (textLen < 0)
                            textLen = 0;
                        byte[] textBuf = new byte[textLen];
                        Array.Copy(content, lastValidEntry.StartIdx, textBuf, 0, textLen);

                        lastValidEntry.Current = textConv.Enc.GetString(textBuf);
                        lastValidEntry.Translation = TranslationDict.GetTranslation(lastValidEntry.Current.Replace("\n", "\r\n"),this.Entry);
                    }
                    lastValidEntry = menuEntry;
                    debugValidEntryCount++;
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
                lastValidEntry.Translation = TranslationDict.GetTranslation(lastValidEntry.Current.Replace("\n", "\r\n"), this.Entry);
            }
            Log.LogDebugMsg(string.Format("Valid menu entries: {0} placeholders {1}", debugValidEntryCount, debugPlaceHolderCount));

        }

        internal void Translate(bool simulated)
        {
            MemoryStream msIndex = new MemoryStream();
            MemoryStream msEntries = new MemoryStream();
            try
            {
                msIndex.Write(BitConverter.GetBytes((Int32)FileLanguage.English), 0, 4);
                msIndex.Write(BitConverter.GetBytes(entryCount1), 0, 4);
                msIndex.Write(BitConverter.GetBytes(entryCount2), 0, 4);

                Int32 indexSize = (menuEntries.Count + 3) * 4;
                for (Int32 i = 0; i < menuEntries.Count; i++)
                {
                    Int32 indexOffset = 0;
                    MenuFileEntry menuEntry = menuEntries[i];
                    if (!menuEntry.PlaceHolder)
                    {
                        {
                            string translation = string.Empty;
                            {
                                try
                                {
                                    translation = TranslationDict.GetTranslation(menuEntry.Current.Replace("\n", "\r\n"), this.Entry);
                                    if (translation.Length > 0)
                                    {
                                        //XmlAttribute setupAttr = node.Attributes["setup"];
                                        //bool replaceChars = true;
                                        //if (setupAttr != null)
                                        //    replaceChars = setupAttr.Value != "true";
                                        //if (replaceChars)
                                        //    translation = MenuFile.textConv.ToGameFormat(attr.Value.Replace("\r\n", "\n")) + (char)(0);
                                        //else
                                        //    translation = attr.Value.Replace("\r\n", "\n") + (char)(0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(Errors.CorruptedTranslation, ex);
                                }

                                byte[] textBuf = MenuFile.textConv.Enc.GetBytes(translation.Replace("\r\n", "\n") + (char)(0));
                                indexOffset = (Int32)msEntries.Length + indexSize;
                                msEntries.Write(textBuf, 0, textBuf.Length);
                            }
                        }
                        msIndex.Write(BitConverter.GetBytes(indexOffset), 0, 4);
                    }
                    else
                        msIndex.Write(BitConverter.GetBytes(menuEntry.StartIdx), 0, 4);

                }
                msIndex.Position = msIndex.Length;
                msIndex.Write(msEntries.ToArray(), 0, (Int32)msEntries.Length);
                //if(!simulated)
                //    entry.WriteContent(msIndex.ToArray());

                byte[] content = msIndex.ToArray();
                string extractFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "simulate",
                        string.Format("{0}.{1}.{2}.txt", entry.Parent.ParentBigFile.Name, entry.Extra.FileNameOnlyForced, entry.Extra.LangText));
                Directory.CreateDirectory(Path.GetDirectoryName(extractFileName));
                FileStream fx = new FileStream(extractFileName, FileMode.Create, FileAccess.ReadWrite);
                fx.Write(content, 0, content.Length);
                fx.Close();
            }
            finally
            {
                msEntries.Close();
                msIndex.Close();
            }
        }

        internal void Translate_old()
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
                    if (!menuEntry.PlaceHolder)
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

        internal void ExtractN(string fileName, MenuFile menu)
        {
            TextWriter menuWriter = new StreamWriter(fileName + "_nemes", false, Encoding.UTF8);
            int j = 0;
            foreach (MenuFileEntry menuEntry in menu.MenuEntries)
            {
                if (!menuEntry.PlaceHolder)
                {
                    menuWriter.WriteLine("#" + j.ToString());
                    menuWriter.WriteLine("{ENG");
                    menuWriter.WriteLine(CineFile.textConv.ToOriginalFormat(menuEntry.Current).Replace("\n", "\r\n"));
                    menuWriter.WriteLine("}");
                    menuWriter.WriteLine("{HUN");
                    //menuWriter.WriteLine(CineFile.textConv.ToOriginalFormat(menuEntry.Current).Replace("\n", "\r\n"));
                    menuWriter.WriteLine("");
                    menuWriter.WriteLine("}");
                    j++;
                }
            }
            menuWriter.Close();
        }

        internal void Extract(string destFolder, bool useDict)
        {
            if (entry.Raw.Language == FileLanguage.English)
            {
                MenuFile menu = new MenuFile(entry);
                //ExtractText(fileName, menu);
                //ExtractN(fileName, menu);
                ExtractResX(destFolder, menu, useDict);
            }
        }

        private static void ExtractText(string fileName, MenuFile menu, bool useDict)
        {
            TextWriter menuWriter = new StreamWriter(fileName, false, Encoding.UTF8);
            menuWriter.WriteLine(";extracted from datafiles");

            foreach (MenuFileEntry menuEntry in menu.MenuEntries)
            {
                if (!menuEntry.PlaceHolder)
                {
                    menuWriter.WriteLine(TransConsts.MenuEntryHeader + (menuEntry.Index).ToString("d4"));
                    menuWriter.WriteLine(TransConsts.OriginalPrefix + CineFile.textConv.ToOriginalFormat(menuEntry.Current).Replace("\n", "\r\n" + TransConsts.OriginalPrefix));
                    menuWriter.WriteLine(CineFile.textConv.ToOriginalFormat(menuEntry.Translation) + "\r\n");
                }
            }
            menuWriter.Close();
        }

        private void ExtractResX(string destFolder, MenuFile menu, bool useDict)
        {
            // Create a resource writer.
            string resXFileName = Path.Combine(destFolder, entry.Extra.ResXFileName);

            ResXHelper helper = ResXPool.GetResX(resXFileName);
            if (!helper.TryLockFor(ResXLockMode.Write))
                throw new Exception(string.Format("Can not lock {0} for write", resXFileName));


            // Add resources to the file.
            List<int> keys = new List<int>();
            foreach (MenuFileEntry menuEntry in menu.MenuEntries)
            {
                if (!menuEntry.PlaceHolder)
                {
                    string key = CineFile.textConv.ToOriginalFormat(menuEntry.Current).Replace("\n", "\r\n");
                    int hash = key.GetHashCode();
                    //if (!keys.Contains(hash))
                    {
                        ResXDataNode resNode = new ResXDataNode(
                            CineFile.textConv.ToOriginalFormat(menuEntry.Current).Replace("\n", "\r\n"),
                            useDict
                                ? CineFile.textConv.ToOriginalFormat(TranslationDict.GetTranslation(menuEntry.Current.Replace("\n", "\r\n"), this.Entry))
                                : CineFile.textConv.ToOriginalFormat(menuEntry.Current.Replace("\n", "\r\n"))
                            );
                        resNode.Comment = "";
                        helper.Writer.AddResource(resNode);
                        keys.Add(hash);
                    }
                }
            }
        }
    }

    class MenuFileEntry
    {
        //internal bool Empty { get { return StartIdx == < } }
        internal string Current = string.Empty;
        internal string Original = string.Empty;
        internal string Translation = string.Empty;
        internal UInt32 StartIdx = 0;
        internal UInt32 EndIdx = 0;
        internal Int32 Index = 0;
        internal bool PlaceHolder = false;
    }
}
