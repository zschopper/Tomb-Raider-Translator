using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Media;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Resources;
using System.Text.RegularExpressions;

namespace TRTR
{
    static class CineConsts
    {
        internal static UInt32 MinBlockSize = 7; // length indicator (4 bytes) + lang code (1-2 byte(s)) + separator (1 byte) closing separator (1 byte)
        internal static UInt32 MaxBlockSize = 0x200000; // tra/trl = 0x20000; tru = 0x200000; // v1.0.0.6
        internal static UInt32 MaxTextBlockSize = 1000;
        internal static UInt32 CineHeaderSize = 0x2000; //800
        internal static UInt32 CineBlockHeaderSize = 0x10;
        internal static UInt32 CineCineBlockMagicBytes = 0x43494E45;  //ENIC
        internal static List<FileLanguage> StrippedLangs = new List<FileLanguage>();

        static CineConsts()
        {

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

                UInt32 blockSize2 = blockSize + 0x0F - (blockSize - 1) % 0x10;
                byte[] blockData = new byte[blockSize];
                Array.Copy(content, offset, blockData, 0, blockSize);
                CineBlock block = new CineBlock(this, blockNo, blockData);
                Blocks.Add(block);
                offset += blockSize2;
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

        internal static void Extract(string destFolder, FileEntry entry, bool useDict)
        {
            if (entry.Raw.Language == FileLanguage.English || entry.Raw.Language == FileLanguage.NoLang)
            {
                // ExtractText(destFolder, entry);
                ExtractResX(destFolder, entry, useDict);
            }
        }

        private static void ExtractResX(string destFolder, FileEntry entry, bool useDict)
        {
            string resXFileName = Path.Combine(destFolder, entry.Extra.ResXFileName);

            ResXHelper helper = ResXPool.GetResX(resXFileName);
            if (!helper.TryLockFor(ResXLockMode.Write))
                throw new Exception(string.Format("Can not lock {0} for write", resXFileName));
            //if (entry.Extra.BigFilePrefix == "bigfile_ENGLISH" /*&& entry.Extra.HashText == "3E2465EC"*/)
            //{
            //    Log.LogDebugMsg(string.Format("dump: {0:8,X} {1} {2:8,X} {3:8,X}", entry.Extra.HashText, entry.Extra.FileName, entry.Raw.Location, entry.Raw.Length));
            //    entry.DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, entry.Extra.HashText + ".dump"));
            //}

            CineFile cine = new CineFile(entry);
            string blockOrig = String.Empty;
            string blockTrans = String.Empty;
            //List<int> keys = new List<int>();

            for (Int32 blockNo = 0; blockNo < cine.Blocks.Count; blockNo++)
            {
                CineBlock block = cine.Blocks[blockNo];
                if (block.Subtitles != null)
                {
                    List<int> blockKeys = new List<int>();
                    UInt32 textCount = block.Subtitles.TextCount(FileLanguage.English);
                    for (UInt32 textIdx = 0; textIdx < textCount; textIdx++)
                    {
                        CineSubtitleEntry subtEntry = block.Subtitles.Entry(FileLanguage.English, textIdx);
                        if(subtEntry.Language == FileLanguage.English)
                        {
                            if (subtEntry.NormalizedText != string.Empty)
                            {
                                int hash = subtEntry.NormalizedText.GetHashCode() ;
                                // in some file, there is same text more than one in one block.
                                //if (!keys.Contains(hash) && !blockKeys.Contains(hash))
                                if (!blockKeys.Contains(hash))
                                {
                                    //if (entry.Extra.BigFilePrefix == "title_ENGLISH" && entry.Extra.HashText == "3E2465EC")
                                    //    Log.LogDebugMsg(string.Format("3E2465EC TEXTS: hash: {0:X8} blockNo: {1} idx: {2} text: {3}", hash, blockNo, textIdx, subtEntry.Text));

                                    //keys.Add(hash);
                                    blockKeys.Add(hash);
                                    ResXDataNode resNode = new ResXDataNode(subtEntry.NormalizedText,
                                        useDict
                                        ? subtEntry.Translated
                                        : subtEntry.NormalizedText
                                        );
                                    resNode.Comment = string.Format("blockNo: {0:X8}\r\nprefix: {1}\r\nfilename: {2}\r\nhash: {3}", blockNo, block.Subtitles.Entry(FileLanguage.English, textIdx).Prefix, block.Subtitles.ParentCineBlock.CineFile.Entry.Extra.FileNameForced, block.Subtitles.ParentCineBlock.CineFile.Entry.Extra.HashText);
                                    helper.Writer.AddResource(resNode);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ExtractText(string destFolder, FileEntry entry)
        {
            string valueSep = ";";
            string fileName = Path.Combine(destFolder, entry.Parent.FilePrefix + "_" + entry.Extra.FileNameOnlyForced + "_subtitles.txt");

            TextWriter cineWriter = new StreamWriter(fileName, false, Encoding.UTF8);
            cineWriter.WriteLine(";extracted from datafiles");

            Log.LogDebugMsg(string.Format("Extracting: {0}  {1} {2}", entry.Extra.BigFileName, entry.Extra.HashText, entry.Extra.FileName));

            CineFile cine = new CineFile(entry);
            #region old method

            /*
                                    string header = "HASH: " + entry.Extra.HashText + 
                                        (entry.Extra.FileName.Length > 0 ? valueSep + "FILENAME: " + entry.Extra.FileName : string.Empty)
                                        //                            + " BLOCKS: "
                                        ;
                                    string blocks = string.Empty;
                                    string original = string.Empty; // ";original\r\n";
                                    string translated = string.Empty; // ";translation\r\n";

                                    for (Int32 j = 0; j < cine.Blocks.Count; j++)
                                    {
                                        CineBlock block = cine.Blocks[j];
                                        string blockNo = j.ToString("d4");
                                        if (block.BlockTypeNo == 1 || block.IsEnic)
                                        {
                                            if (blocks.Length == 0)
                                                blocks = blockNo;
                                            else
                                                blocks += ", " + blockNo;
                                        }
                                        if (block.subtitles != null)
                                            if (block.subtitles.ContainsKey(FileLanguage.English))
                                            {
                                                string blockOrig = block.subtitles[FileLanguage.English];
                                                // strip trailing spaces
                                                blockOrig = blockOrig.Replace(" \n", "\n");
                                                // replace original texts' newlines (cr) to normal text format (crlf)
                                                blockOrig = blockOrig.Replace("\n", "\r\n");
                                                // add double quotes if contains newline, double quote or comma
                                                Int32 hashCode = blockOrig.GetHashCode();
                                                if (!subTransEntries.ContainsKey(hashCode))
                                                    throw new Exception("Key not found: \"" + blockOrig + "\"");
                                                TranslationCovertFileEntry transEntry = subTransEntries[hashCode];
                                                if (rxDblQuoteNeeded.IsMatch(blockOrig))
                                                    blockOrig = "\"" + blockOrig + "\"";

                                                string blockTrans = transEntry.Translation.Trim(strippedChars);
                                                // strip trailing spaces
                                                blockTrans = blockTrans.Replace(" \n", "\n");
                                                // replace original texts' newlines (cr) to normal text format (crlf)
                                                blockTrans = blockTrans.Replace("\n", "\r\n");
                                                blockTrans = TRGameInfo.TextConv.ToOriginalFormat(blockTrans);
                                                // add double quotes if contains newline, double quote or comma
                                                if (rxDblQuoteNeeded.IsMatch(blockTrans))
                                                    blockTrans = "\"" + blockTrans + "\"";

                                                string blockDirectives =
                                                    "$OLDHASH=" + transEntry.Hash + "\r\n" +
                                                    (transEntry.Directives.Length > 0 ? transEntry.Directives + "\r\n" : string.Empty);

                                                original +=
                                                    //blockDirectives + 
                                                    ";" + blockNo + valueSep + blockOrig + "\r\n";
                                                translated +=
                                                    //(transEntry.Comments.Length > 0 ? transEntry.Comments + "\r\n" : string.Empty) + 
                                                    blockNo + valueSep + blockTrans + "\r\n";
                                                // write to file
                                            }
                                    }
                                    byte[] buf =
                                        TRGameInfo.TextConv.Enc.GetBytes(
                                        header +
                                        //blocks + 
                                        "\r\n" +
                                        original +
                                        translated +
                                        valueSep + "\r\n");
                                    fs.Write(buf, 0, buf.Length);
                                } 
                                     */
            #endregion
            string header = "HASH: " + entry.Extra.HashText +
                (entry.Extra.FileName.Length > 0 ? valueSep + "FILENAME: " + entry.Extra.FileName : string.Empty);
            if (entry.Raw.Language == FileLanguage.English)
                header += ";sub";
            else
                header += ";dir";

            string blockOrig = String.Empty;
            string blockTrans = String.Empty;
            for (Int32 j = 0; j < cine.Blocks.Count; j++)
            {
                CineBlock block = cine.Blocks[j];
                if (block.Subtitles != null)
                {
                    UInt32 textCount = block.Subtitles.TextCount(FileLanguage.English);
                    for (UInt32 k = 0; k < textCount; k++)
                    {
                        string text = block.Subtitles.Entry(FileLanguage.English, k).Text;
                        text = text.Replace("\r\n", "\n");
                        text = text.Replace(" \n", "\n");
                        text = text.Replace("\n", "\r\n");
                        text = j.ToString("d5") + valueSep + TRGameInfo.textConv.ToOriginalFormat(text);
                        if (blockOrig.Length > 0)
                            blockOrig += "\r\n" + TransConsts.OriginalPrefix + text.Replace("\r\n", "\r\n" + TransConsts.OriginalPrefix);
                        else
                            blockOrig += TransConsts.OriginalPrefix + text.Replace("\r\n", "\r\n" + TransConsts.OriginalPrefix);
                        if (blockTrans.Length > 0)
                            blockTrans += "\r\n" + text;
                        else
                            blockTrans += text;
                    }
                }
            }
            cineWriter.WriteLine(header + "\r\n" + blockOrig + "\r\n" + blockTrans + "\r\n");
        }

        internal void CreateRestoration(XmlElement subtitleElement, XmlNode subtitleNode)
        {
            XmlDocument resDoc = subtitleNode.OwnerDocument;
            XmlElement cineElement = resDoc.CreateElement("cine");
            cineElement.SetAttribute("hash", entry.Extra.HashText);
            if (entry.Extra.FileName.Length > 0)
                cineElement.SetAttribute("filename", entry.Extra.FileName);
            XmlNode cineNode = subtitleNode.AppendChild(cineElement);

            for (Int32 j = 0; j < Blocks.Count; j++)
            {
                CineBlock block = Blocks[j];
                if (block.Subtitles != null)
                    if (block.Subtitles.Count > 0)
                    {
                        XmlElement blockElement = resDoc.CreateElement("block");
                        XmlNode blockNode = cineNode.AppendChild(blockElement);
                        blockElement.SetAttribute("no", j.ToString("d5"));
                        //blockElement.SetAttribute("offs", block.Data.Length.ToString("x8"));

                        //blockElement.SetAttribute("translation", TextConv.ToOriginalFormat(block.subtitles[FileLanguage.English]));
                        for (Int32 key = 0; key < block.Subtitles.Count; key++)
                        {
                            XmlElement langElement = resDoc.CreateElement("text");
                            XmlNode langNode = blockNode.AppendChild(langElement);
                            CineSubtitleEntry subtitleEntry = block.Subtitles[key];
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
        #region private variables
        private CineFile cineFile = null;
        private bool isEnic;
        private UInt32 blockTypeNo;
        private UInt32 blockNo;
        private byte[] data = null;
        private byte[] translatedData = null;
        private string text = string.Empty;
        private string translation;
        #endregion

        internal UInt32 BlockTypeNo { get { return blockTypeNo; } }
        internal UInt32 BlockNo { get { return blockNo; } }
        internal CineFile CineFile { get { return cineFile; } }
        internal bool IsEnic { get { return isEnic; } }
        internal byte[] Data { get { return data; } }
        internal byte[] TranslatedData { get { return translatedData; } }
        internal UInt32 VirtualSize { get { UInt32 blockLen = (UInt32)Data.Length; return blockLen + 0x0F - (blockLen - 1) % 0x10; } }
        internal string Translation { get { return translation; } set { translation = value; } }
        internal CineSubtitles Subtitles { get; set; }

        // ctor
        internal CineBlock(CineFile cineFile, UInt32 blockNo, byte[] block)
        {
            this.cineFile = cineFile;
            this.blockNo = blockNo;
            ParseBlock(block);
        }

        private void ParseBlock(byte[] block)
        {
            // process header
            //File.WriteAllBytes(string.Format(@"c:\tmp\{0}_{1}", cineFile.Entry.Extra.HashText, blockNo), block);
            blockTypeNo = BitConverter.ToUInt32(block, 0x00);
            if (blockTypeNo != 0 && blockTypeNo != 1 && blockTypeNo != 3)
                throw new Exception(Errors.ParseErrorBlockTypeError);

            //else //{  // v1.0.0.6
            if (blockTypeNo == 0 || blockTypeNo == 1 || blockTypeNo == 3)
            {  // v1.0.0.6
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
                                    cineFile.Entry.Extra.HashText + "," + blockNo.ToString(), blockNo, textOffset, textBlockLen, size, contentOffset, cineFile.Entry.Extra.FileName));
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
                this.Subtitles = new CineSubtitles(this, subtitleText);
                this.data = new byte[textOffset];
                Array.Copy(block, contentOffset, this.data, 0, textOffset);
            }
        }  // v1.0.0.6

        internal void Translate(XmlNode blockNode)
        {
            Write(Subtitles.GetTranslatedSubtitleBlock(blockNode, 0)); //checkthis
        }

        internal void Restore(XmlNode blockNode)
        {
            Write(Subtitles.GetRestoredSubtitleBlock(blockNode));
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

    class CineSubtitles : List<CineSubtitleEntry>
    {
        #region private variables
        private string text;
        private CineBlock parentCineBlock;
        #endregion

        internal string Text { get { return text; } set { text = value; ParseBlockText(); } }
        internal CineBlock ParentCineBlock { get { return parentCineBlock; } }

        // ctor
        internal CineSubtitles(CineBlock block, string text)
        {
            this.parentCineBlock = block;
            this.Text = text;
        }

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

        internal byte[] GetTranslatedSubtitleBlock(XmlNode blockNode, UInt32 index)
        {
            // load translation, otherwise use english text to translation
            string translation = string.Empty;
            //Int32 checksum = 0;
            if (blockNode != null)
            {
                XmlAttribute attr = blockNode.Attributes["translation"];
                if (attr != null)
                {
                    translation = attr.Value;
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
                    elements = text.Substring(4).Split(new char[] {(char)0xD}, StringSplitOptions.RemoveEmptyEntries); // remove content-length value and split texts at delimiters
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
                        Add(new CineSubtitleEntry(this, lang, elements[i + 1]));
                    }
                }
            }
            catch (Exception ex)
            {
                Exception newEx = new Exception("ParseBlockText(): Wrong textblock", ex);//xxtrans
                newEx.Data.Add("hash", this.parentCineBlock.CineFile.Entry.Extra.HashText);
                newEx.Data.Add("blockNo", this.parentCineBlock.BlockNo);
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

    class CineSubtitleEntry
    {
        private static Regex rx = new Regex(@"^(\[[0-9a-z\.]+\]|)(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private CineSubtitles parentSubTitles;

        public FileLanguage Language { get; set; }
        public string Text { get; set; }
        public string NormalizedText { get; set; }
        public string Prefix { get; set; }
        public string Translated { get; set; }
        public CineSubtitles ParentSubTitles { get { return parentSubTitles; } }

        //ctor
        public CineSubtitleEntry(CineSubtitles parentSubTitles, FileLanguage language, string text)
        {
            this.parentSubTitles = parentSubTitles;
            this.Language = language;
            Parse(text);
        }

        private void Parse(string text)
        {
            FileEntry fileEntry = parentSubTitles.ParentCineBlock.CineFile.Entry;
            Match m = rx.Match(text);
            if (m.Success)
            {
                this.Prefix = m.Groups[1].Value;
                this.Text = m.Groups[2].Value;
                if (this.Text == string.Empty)
                {
                    this.Prefix = string.Empty;
                    this.Text = string.Empty;
                    this.NormalizedText = string.Empty;
                    this.Translated = string.Empty;

                    Log.LogDebugMsg(string.Format("Empty CineFile text: \"{0}\" Lang: {1}", text, Language.ToString())); //trans
                }
                else
                {
                    if (this.Language == FileLanguage.English)
                    {
                        this.NormalizedText = this.Text.Replace("\r\n", "\n").Replace(" \n", "\n").Replace("\n", "\r\n");
                        this.Translated = TextParser.GetText(NormalizedText,
                            string.Format("BF: {0} File: {1}", fileEntry.Extra.BigFileName, fileEntry.Extra.FileNameForced));
                    }
                }
            }
            else
            {
                this.Prefix = string.Empty;
                this.Text = text;
                this.NormalizedText = text.Replace("\r\n", "\n").Replace(" \n", "\n").Replace("\n", "\r\n");
                if (this.Language == FileLanguage.English)
                {
                    this.Translated = TextParser.GetText(NormalizedText,
                        string.Format("BF: {0} File: {1}", fileEntry.Extra.BigFileName, fileEntry.Extra.FileNameForced));
                }
                else
                {
                    this.Translated = string.Empty;
                }
            }
        }
    }

}