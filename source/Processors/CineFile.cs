using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Media;
using System.Threading;
using System.Security.Cryptography;

namespace TRTR
{
    static class CineConsts
    {
        internal static UInt32 MinBlockSize = 7; // length indicator (4 bytes) + lang code (1-2 byte(s)) + separator (1 byte) closing separator (1 byte)
        internal static UInt32 MaxBlockSize = 0x200000; // tra/trl = 0x20000; tru = 0x200000; // v1.0.0.6
        internal static UInt32 MaxTextBlockSize = 1000;
        internal static UInt32 CineHeaderSize = 0x800;
        internal static UInt32 CineBlockHeaderSize = 0x10;
        internal static UInt32 CineCineBlockMagicBytes = 0x43494E45;  //ENIC
        internal static List<FileLanguage> StrippedLangs;

        static CineConsts()
        {
            StrippedLangs = new List<FileLanguage>();
            //StrippedLangs.Add(FileLanguage.Russian);
            StrippedLangs.Add(FileLanguage.French);
        }
    }

    class CineFile
    {
        private FileEntry entry;
        private byte[] header;

        internal List<CineBlock> Blocks = new List<CineBlock>();
        internal byte[] Header { get { return header; } }
        internal FileEntry Entry { get { return entry; } }

        internal static TextConv textConv = new TextConv(new char[] { }, new char[] { }, Encoding.UTF8); // null;

        internal CineFile(FileEntry entry)
        {
            this.entry = entry;
            ParseFile();
        }

        private void ParseFile()
        {
            byte[] content = entry.ReadContent();
            header = new byte[CineConsts.CineHeaderSize];
            Array.Copy(content, 0, header, 0, CineConsts.CineHeaderSize);
            UInt32 offset = CineConsts.CineHeaderSize;
            UInt32 blockNo = 0;
            while (offset < content.Length)
            {
                UInt32 blockSize = BitConverter.ToUInt32(content, (Int32)(offset + 4)) + CineConsts.CineBlockHeaderSize;
                byte[] blockData = new byte[blockSize];
                Array.Copy(content, offset, blockData, 0, blockSize);
                CineBlock block = new CineBlock(this, blockNo, blockData);
                Blocks.Add(block);
                offset += blockSize;
                blockNo++;
            }
        }

        internal void Translate()
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                try
                {
                    ms.Write(header, 0, (Int32)CineConsts.CineHeaderSize);
                    XmlNode cineNode = TRGameInfo.Trans.TranslationDocument.SelectSingleNode(String.Format("/translation/subtitle/cine[@hash=\"{0}\"]", entry.Extra.HashText));
                    if (cineNode != null)
                    {
                        foreach (CineBlock block in Blocks)
                        {
                            XmlNode blockNode = cineNode.SelectSingleNode(String.Format("block[@no=\"{0:d5}\"]", block.BlockNo));
                            block.Translate(blockNode);
                            ms.Write(block.TranslatedData, 0, block.TranslatedData.Length);
                        }
                    }
                    entry.WriteContent(ms.ToArray());
                }
                finally
                {
                    ms.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("CineFile.Translate", ex);
            }
        }

        internal void Restore()
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                ms.Write(header, 0, (Int32)CineConsts.CineHeaderSize);
                XmlNode cineNode = TRGameInfo.Trans.RestorationDocument.SelectSingleNode(String.Format("/restoration/subtitle/cine[@hash=\"{0}\"]", entry.Extra.HashText));
                foreach (CineBlock block in Blocks)
                {
                    XmlNode blockNode = cineNode.SelectSingleNode(String.Format("block[@no=\"{0:d5}\"]", block.BlockNo));
                    block.Restore(blockNode);
                    ms.Write(block.TranslatedData, 0, block.TranslatedData.Length);
                }
                entry.WriteContent(ms.ToArray());
            }
            finally
            {
                ms.Close();
            }
        }

        internal void CreateRestoration(XmlElement subtitleElement, XmlNode subtitleNode)
        {
            XmlDocument resDoc = subtitleNode.OwnerDocument;
            XmlElement cineElement = resDoc.CreateElement("cine");
            cineElement.SetAttribute("hash", entry.Extra.HashText);
            if (entry.Stored.FileName.Length > 0)
                cineElement.SetAttribute("filename", entry.Stored.FileName);
            XmlNode cineNode = subtitleNode.AppendChild(cineElement);

            for (Int32 j = 0; j < Blocks.Count; j++)
            {
                CineBlock block = Blocks[j];
                if (block.subtitles != null)
                    if (block.subtitles.Count > 0)
                    {
                        XmlElement blockElement = resDoc.CreateElement("block");
                        XmlNode blockNode = cineNode.AppendChild(blockElement);
                        blockElement.SetAttribute("no", j.ToString("d5"));
                        //blockElement.SetAttribute("offs", block.Data.Length.ToString("x8"));

                        //blockElement.SetAttribute("translation", TextConv.ToOriginalFormat(block.subtitles[FileLanguage.English]));
                        for (Int32 key = 0; key < block.subtitles.Count; key++)
                        {
                            XmlElement langElement = resDoc.CreateElement("text");
                            XmlNode langNode = blockNode.AppendChild(langElement);
                            CineSubtitleEntry subtitleEntry = block.subtitles[key];
                            langElement.SetAttribute("id", key.ToString("d2"));
                            langElement.SetAttribute("langid", ((Int32)subtitleEntry.Language).ToString("d2"));
                            langElement.SetAttribute("lang", LangNames.Dict[subtitleEntry.Language]);
                            langElement.SetAttribute("value", subtitleEntry.Text);
                        }
                    }
            }
        }
    }

    class CineBlock // block in a mul/cine file
    {
        private CineFile cineFile = null;
        private bool isEnic;
        private UInt32 blockTypeNo;
        private UInt32 blockNo;
        private byte[] data = null;
        private byte[] translatedData = null;
        private string text = string.Empty;
        private string translation;

        internal UInt32 BlockTypeNo { get { return blockTypeNo; } }
        internal UInt32 BlockNo { get { return blockNo; } }
        internal CineFile CineFile { get { return cineFile; } }
        internal bool IsEnic { get { return isEnic; } }
        internal byte[] Data { get { return data; } }
        internal byte[] TranslatedData { get { return translatedData; } }
        internal UInt32 VirtualSize { get { UInt32 blockLen = (UInt32)Data.Length; return blockLen + 0x0F - (blockLen - 1) % 0x10; } }
        internal string Translation { get { return translation; } set { translation = value; } }
        internal CineSubtitles subtitles;

        internal CineBlock(CineFile cineFile, UInt32 blockNo, byte[] block)
        {
            this.cineFile = cineFile;
            this.blockNo = blockNo;
            ParseBlock(block);
        }

        private void ParseBlock(byte[] block)
        {
            if (cineFile.Entry.Extra.HashText == "C90245A2" && blockNo == 959)
                Noop.DoIt(); 
               
            // process header
            //File.WriteAllBytes(string.Format(@"c:\tmp\{0}_{1}", cineFile.Entry.Extra.HashText, blockNo), block);
            blockTypeNo = BitConverter.ToUInt32(block, 0x00);
           if (blockTypeNo != 0 && blockTypeNo != 1)
                throw new Exception(Errors.ParseErrorBlockTypeError);

           else //{  // v1.0.0.6
           if (blockTypeNo == 0 || blockTypeNo == 1) {  // v1.0.0.6
               UInt32 virtualBlockSize = BitConverter.ToUInt32(block, 0x04); // virtual size (rounded up to 0x10 boundary)
               // !checkthis!
               UInt32 blockSize;
               if (block.Length > 0x14)
                   blockSize = BitConverter.ToUInt32(block, 0x10); // exact size (when blocktypeno == 1 and it is non-enic)
               else
                   blockSize = (UInt32)Math.Max(block.Length - 0x10, 0);

               isEnic = blockSize == CineConsts.CineCineBlockMagicBytes;

               UInt32 contentOffset;

               if (isEnic || blockTypeNo == 0)
               {
                   blockSize = virtualBlockSize;
                   contentOffset = 0x10;
               }
               else
                   contentOffset = 0x14;

               if (blockSize > virtualBlockSize)
                   throw new Exception(Errors.ParseErrorSizeMismatch);
               if (blockSize > CineConsts.MaxBlockSize)
                   throw new Exception(Errors.ParseErrorTooBig);


               // search for text block
               // sets textOffset & hasSubtitles
               UInt32 textOffset = blockSize;
               string subtitleText = string.Empty;
               bool hasSubtitles = (blockTypeNo == 1 || isEnic);

               // !!! search offset start at every 4 byte boundary.

               #region search subtitles


               if (hasSubtitles)
               {
                   // search last non 0x00 byte
                   UInt32 maxTextBlockSize = Math.Min(CineConsts.MaxTextBlockSize, blockSize - contentOffset);
                   UInt32 lastNotNull;
                   for (lastNotNull = (UInt32)(block.Length - 1); lastNotNull >= CineConsts.MinBlockSize && block[lastNotNull] == 0; lastNotNull--) ;

                   // it can have subtitles, if last non zero is 0x0D
                   hasSubtitles = block[lastNotNull] == 0x0D;
                   if (hasSubtitles)
                   {
                       UInt32 j = lastNotNull;
                       UInt32 textBlockLen = 0;
                       while (j >= CineConsts.MinBlockSize - 4 && textBlockLen == 0)
                       {
                           // search first 0 character (length value of textblock)
                           if (block[j] == 0)
                           {
                               textOffset = (UInt32)j - 3 - contentOffset; //includes length indicator
                               textBlockLen = (UInt32)lastNotNull - j + 4; //includes length indicator
                               Int32 size = BitConverter.ToInt32(block, (Int32)(textOffset + contentOffset));
                               if (size == textBlockLen - 4)
                                   subtitleText = CineFile.textConv.Enc.GetString(block, (Int32)(textOffset + contentOffset), (Int32)textBlockLen);
                           }
                           j--;
                       }
                   }
               }
               #endregion


               #region search subtitles (4 byte version)
               /*
            
            if (hasSubtitles)
            {
                // search last non 0x00 byte
                uint maxTextBlockSize = Math.Min(CineConsts.MaxTextBlockSize, blockSize - contentOffset);
                uint lastNotNull;
                for (lastNotNull = (uint)(block.Length - 1); lastNotNull >= CineConsts.MinBlockSize && block[lastNotNull] == 0; lastNotNull--) ;

                // it can have subtitles, if last non zero is 0x0D
                hasSubtitles = block[lastNotNull] == 0x0D;
                if (hasSubtitles)
                {
                    uint j = (uint)(Boundary.Down((int)(lastNotNull - CineConsts.MinBlockSize), 4));
                    uint textBlockLen = 0;
                    while (j >= CineConsts.MinBlockSize - 4 && textBlockLen == 0)
                    {
                        Int32 size = BitConverter.ToInt32(block, (int)j);
                        if (j < 0xFFFFFF)
                            // end of search - reach first 
                        // search first 0 character (length value of textblock)
//                            Array.LastIndexOf(block,
                        if (block[j] == 0)
                        {
                            textOffset = (uint)j - 3 - contentOffset; //includes length indicator
                            textBlockLen = (uint)lastNotNull - j + 4; //includes length indicator
                            //Int32 
                                size = BitConverter.ToInt32(block, (Int32)(textOffset + contentOffset));
                            if (size == textBlockLen - 4)
                            {
                                Log.Write(string.Format("Hash: {0:s14}, textOffset: {2:d4}, textBlockLen: {3:d4}, size: {4:d4}, contentOffset: {5:d4} FileName: {6}",
                                    cineFile.Entry.Extra.HashText + "," + blockNo.ToString(), blockNo, textOffset, textBlockLen, size, contentOffset, cineFile.Entry.Stored.FileName));
                                subtitleText = cineFile.Entry.Parent.GameInfo.TextConv.Enc.GetString(block, (Int32)(textOffset + contentOffset), (Int32)textBlockLen);
                            }
                        }
                        j -= 4;
                    }
                }Array.LastIndexOf(
            }
*/
               #endregion

               if (subtitleText.Length == 0)
                   textOffset = blockSize;

               // store binary (untranslated) data
               this.subtitles = new CineSubtitles(this, subtitleText);
               this.data = new byte[textOffset];
               Array.Copy(block, contentOffset, this.data, 0, textOffset);
           }
        }  // v1.0.0.6

        internal void Translate(XmlNode blockNode)
        {
            Write(subtitles.GetTranslatedSubtitleBlock(blockNode, 0)); //checkthis
        }

        internal void Restore(XmlNode blockNode)
        {
            Write(subtitles.GetRestoredSubtitleBlock(blockNode));
        }

        private void Write(byte[] subtitleBlock)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                UInt32 headerLength;
                if (BlockTypeNo == 0 || IsEnic)
                    headerLength = CineConsts.CineBlockHeaderSize;
                else
                    headerLength = CineConsts.CineBlockHeaderSize + 4;

                ms.Position = headerLength;

                if (subtitleBlock.Length == 0)
                    // write block content
                    ms.Write(Data, 0, data.Length);
                else
                {
                    if (this.isEnic)
                        ms.Write(Data, 0, data.Length);
                    else
                    {
                        // strip 4 byte zeros before subtitles
                        Int32 lastNonNullInData = Math.Max(data.Length - 1, 0);

                        Int32 pos;
                        if (data.Length > 0)
                        {
                            while (data[lastNonNullInData] == 0)
                                lastNonNullInData--;
                            lastNonNullInData++;
                            pos = (Int32)ms.Position + lastNonNullInData;
                            pos = pos + 3 - (pos - 1) % 4;
                        }
                        else
                            pos = (Int32)ms.Position;

                        // write CORRECTED block content
                        ms.Write(data, 0, pos - (Int32)ms.Length - (Int32)headerLength);
                    }
                    // write subtitles
                    ms.Write(BitConverter.GetBytes(subtitleBlock.Length), 0, 4);
                    ms.Write(subtitleBlock, 0, subtitleBlock.Length);
                }

                // fill remaining "virtual" space
                Int32 virtSizeDiff = Boundary.Up((Int32)ms.Length, 0x10);
                Int32 virtSize = (Int32)ms.Length + virtSizeDiff;
                if (virtSizeDiff > 0)
                {
                    byte[] virtSpace = new byte[virtSizeDiff];
                    //Array.Clear(virtSpace, 0, virtSizeDiff);
                    //virtSpace[virtSizeDiff - 1] = 0xFE;
                    ms.Write(virtSpace, 0, virtSizeDiff);
                }

                // write header

                ms.Position = 0;
                ms.Write(BitConverter.GetBytes(BlockTypeNo), 0, 4); // typeno
                ms.Write(BitConverter.GetBytes(virtSize - 0x10), 0, 4); // virtual size
                if (blockTypeNo == 1 && !isEnic)
                {
                    ms.Position = 0x10;
                    ms.Write(BitConverter.GetBytes(virtSize - 0x14), 0, 4); // size
                }

                // update translatedData
                translatedData = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(translatedData, 0, (Int32)ms.Length);
            }
            finally
            {
                ms.Close();
            }
        }
    }

    class CineSubtitleEntry
    {
        public FileLanguage Language;
        public string Text;
        public CineSubtitleEntry(FileLanguage language, string text)
        {
            this.Language = language;
            this.Text = text;
        }
    }

    class CineSubtitles : List<CineSubtitleEntry>
    {
        private string text;
        private CineBlock cineBlock;

        internal string Text { get { return text; } set { text = value; ParseBlockText(); } }

        internal CineSubtitleEntry Entry(FileLanguage lang, UInt32 index)
        {
            Int32 ret = -1;
            foreach (CineSubtitleEntry entry in this)
            {
                if (entry.Language == lang)
                    ret++;
                if (ret == index)
                    return entry;
            }
            return null;
        }

        internal UInt32 TextCount(FileLanguage lang)
        {
            UInt32 ret = 0;
            foreach (CineSubtitleEntry entry in this)
            {
                if (entry.Language == lang)
                    ret++;
            }
            return ret;
        }

        internal bool ContainsLang(FileLanguage lang)
        {
            foreach (CineSubtitleEntry entry in this)
                if (entry.Language == lang)
                    return true;
            return false;
        }

        internal CineSubtitles(CineBlock block, string text)
        {
            cineBlock = block;
            this.Text = text;
        }

        internal void noop()
        {
        }

        internal byte[] GetTranslatedSubtitleBlock(XmlNode blockNode, UInt32 index)
        {
            // load translation, otherwise use english text to translation
            string translation = string.Empty;
            //Int32 checksum = 0;
            if (blockNode != null)
            {
                //                if (cineBlock.CineFile.Entry.Hash == 0x7BC53226 && cineBlock.BlockNo == 14)
                //                    noop(); 
                XmlAttribute attr = blockNode.Attributes["translation"];
                if (attr != null)
                {
                    try
                    {
                        translation = attr.Value;
                        if (translation.Length > 0)
                        {
#if !DONT_CHECK_CHECKSUM
                            //if (!Hash.Check(translation + "s" + cineBlock.BlockNo.ToString("d5") +
                            //    TRGameInfo.InstallInfo.GameNameAbbrevFull, 
                            //    blockNode.Attributes["checksum"].Value))
                            //{
                            //    Exception newEx = new Exception(Errors.CorruptedTranslation);
                            //    newEx.Data.Add("checksum", blockNode.Attributes["checksum"].Value);
                            //    throw newEx;
                            //}
#endif
                        }

                    }
                    catch (Exception ex)
                    {
                        throw new Exception("GetTranslatedSubtitleBlock(): Translation Error", ex);
                    }
                }
            }
            //if (translation.Length == 0)
            //    if (ContainsKey(FileLanguage.English))
            //        translation = this[FileLanguage.English];

            StringBuilder sb = new StringBuilder();
            if (translation.Length > 0)
            {
                sb.Append((Int32)FileLanguage.English);
                sb.Append((char)0x0D);
                sb.Append(CineFile.textConv.ToGameFormat(translation).Trim().Replace("\r\n", "\n"));
                sb.Append((char)0x0D);
            }
            for (Int32 key = 0; key < Count; key++)
            {
                FileLanguage lang = this[key].Language;
                if (lang != FileLanguage.English && !CineConsts.StrippedLangs.Contains(lang))
                {
                    sb.Append((Int32)lang);
                    sb.Append((char)0x0D);
                    if (lang == FileLanguage.English)
                        sb.Append(CineFile.textConv.ToGameFormat(translation).Trim().Replace("\r\n", "\n"));
                    else
                        sb.Append(this[key].Text);
                    sb.Append((char)0x0D);
                }
            }
            return CineFile.textConv.Enc.GetBytes(sb.ToString());
        }

        internal byte[] GetRestoredSubtitleBlock(XmlNode blockNode)
        {
            string translation = string.Empty;
            if (blockNode != null)
            {
                XmlNodeList langNodeList = blockNode.SelectNodes("text");
                StringBuilder sb = new StringBuilder();
                foreach (XmlNode node in langNodeList)
                {
                    string langIdStr = UInt32.Parse(node.Attributes["langid"].Value).ToString();
                    string text = node.Attributes["value"].Value;
                    sb.Append(langIdStr);
                    sb.Append((char)0x0D);
                    sb.Append(text);
                    sb.Append((char)0x0D);
                }
                translation = sb.ToString();
            }
            return CineFile.textConv.Enc.GetBytes(translation);
        }

        private void ParseBlockText()
        {
            string[] elements; // for debug
            try
            {
                Clear();
                if (text.Length > 5)
                {
                    //string[] elements = text.Substring(4).Split((char)0xD); // remove content-length value and split texts at delimiters
                    elements = text.Substring(4).Split((char)0xD); // remove content-length value and split texts at delimiters
                    // add subtitles to dictionary
                    for (Int32 i = 0; i < elements.Length - 1; i += 2)
                    {
                        FileLanguage lang;
                        Int32 value;
                        if (!Int32.TryParse(elements[i], out value))
                        {
                            Exception newEx = new Exception(Errors.ParseError);
                            newEx.Data.Add("language code", elements[i]);
                            throw newEx;
                        }
                        lang = (FileLanguage)value;
                        Add(new CineSubtitleEntry(lang, elements[i + 1]));
                    }
                }
            }
            catch (Exception ex)
            {
                Exception newEx = new Exception("ParseBlockText(): Wrong textblock", ex);
                newEx.Data.Add("hash", this.cineBlock.CineFile.Entry.Extra.HashText);
                newEx.Data.Add("blockNo", this.cineBlock.BlockNo);
                newEx.Data.Add("content", this.text);
                newEx.Data.Add("TranslationDocumentFileName", TRGameInfo.Trans.TranslationDocumentFileName);
                if (TRGameInfo.Trans.TranslationDocument != null)
                    newEx.Data.Add("traDocURI", TRGameInfo.Trans.TranslationDocument.BaseURI);
                else
                    newEx.Data.Add("traDoc", "[null]");
                throw newEx;
            }

        }
    }
}