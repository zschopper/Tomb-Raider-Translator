using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;

namespace TRTR
{

    #region Primitives

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RawFileInfo
    {
        public uint Length;
        public uint Location;
        public uint LangCode;
        public uint Unknown;
        public FileLanguage Language
        {
            get
            {
                switch (LangCode & 0x0F)
                {
                    case 0x0:
                        return FileLanguage.Espanol;
                    case 0x1:
                        return FileLanguage.English;
                    case 0x2:
                        return FileLanguage.French;
                    case 0x4:
                        return FileLanguage.German;
                    case 0x8:
                        return FileLanguage.Italian;
                    case 0xF:
                        return FileLanguage.NoLang;
                    default:
                        throw new Exception(Errors.InvalidLanguageCode);
                }

            }
        }
    }

    static class RawFileInfoSize
    {
        public static int Size;

        static RawFileInfoSize()
        {
            Size = Marshal.SizeOf(typeof(RawFileInfo));
        }
    }

    enum FileTypeEnum
    {
        Unknown = 0,
        MUL = 1,
        MUL_CIN = 2,
        RAW = 3,
        RAW_FNT = 4,
        BIN = 5,
        BIN_MNU = 6
    }

    /*enum FileLanguage
    {
        NoLang = 0,
        English = 1,
        German = 2,
        Italian = 3,
        Espanol = 4,
        France = 5
    }*/

    enum FileNameResolveStatus
    {
        Unknown = 0,
        Resolved = 1,
        NotResolved = 2
    }

    interface ITypedFile
    {
        void Translate();
        void Restore();
        void LoadTranslations();
        //bool CheckTranslatable();
        XmlDocument TranslationDocument { get; set; }
        XmlDocument RestorationDocument { get; set; }
    }

    #endregion

    static class FileTypeStrings
    {
        public static Dictionary<FileTypeEnum, string> Value;
        static FileTypeStrings()
        {
            Value = new Dictionary<FileTypeEnum, string>();
            Value.Add(FileTypeEnum.Unknown, TextConsts.Unknown);
            Value.Add(FileTypeEnum.MUL, "MUL");
            Value.Add(FileTypeEnum.MUL_CIN, "CIN");
            Value.Add(FileTypeEnum.RAW, "RAW");
            Value.Add(FileTypeEnum.RAW_FNT, "FNT");
            Value.Add(FileTypeEnum.BIN_MNU, "MNU");
        }

        public static FileTypeEnum Key(string value)
        {
            FileTypeEnum ret = FileTypeEnum.Unknown;
            foreach (FileTypeEnum e in Value.Keys)
                if (Value[e] == value)
                    ret = e;
            return ret;
        }
    }

    class FileStoredInfo
    {
        public string FileName = "";
        public FileTypeEnum FileType = FileTypeEnum.Unknown;
    }

    
    class FileStoredInfoList : Dictionary<uint, FileStoredInfo>
    {
        public void LoadFromFile(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            try
            {
                string line;
                string[] values;
                FileStoredInfo info;
                uint key;
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    values = line.Split('\t');
                    if (values.Length != 3)
                        throw new Exception(Errors.InvalidStoredFileInfo);
                    info = new FileStoredInfo();
                    key = uint.Parse(values[0], System.Globalization.NumberStyles.HexNumber);
                    if (values[0] != values[1])
                        info.FileName = values[1];
                    info.FileType = FileTypeStrings.Key(values[2]);
                    Add(key, info);
                }
            }
            finally
            {
                sr.Close();
                fs.Close();
            }
        }
    }

    class FileExtraInfo
    {
        private FileLanguage language;
        private string hashText;

        public string HashText { get { return hashText; } }
        public string LangText { get { return LangNames.Dict[language]; } }
        public FileLanguage Language { get { return language; } }
        public uint Offset;
        public uint AbsOffset;
        public byte[] Data = null;
        public string Text = "";
        public int BigFile;
        private FileEntry parent;
        public string BigFileName { get { return parent.Parent.Path + "bigfile." + (parent.Parent.OneBigFile ? "dat" : BigFile.ToString("d3")); } }

        public FileExtraInfo(FileEntry parent)
        {
            this.parent = parent;
            this.hashText = parent.Hash.ToString("X8");
            UpdateLangText();
            this.AbsOffset = parent.Raw.Location * 0x800;
            if (parent.Parent.OneBigFile)
            {
                this.BigFile = -1;
                this.Offset = AbsOffset;
            }
            else
            {
                this.BigFile = (int)parent.Raw.Location / 0x12C00;
                this.Offset = parent.Raw.Location % 0x12C00 * 0x800;
            }
        }

        private void UpdateLangText()
        {
            switch (parent.Raw.LangCode & 0x0F)
            {
                case 0x0:
                    { language = FileLanguage.Espanol; return; }
                case 0x1:
                    { language = FileLanguage.English; return; }
                case 0x2:
                    { language = FileLanguage.French; return; }
                case 0x4:
                    { language = FileLanguage.German; return; }
                case 0x8:
                    { language = FileLanguage.Italian; return; }
                case 0xF:
                    { language = FileLanguage.NoLang; return; }
                default:
                    { throw new Exception(Errors.InvalidLanguageCode); }
            }
        }
    }

    class FileEntry// : IComparable<FileEntry>
    {
        private uint hash;
        private RawFileInfo raw;
        private FileExtraInfo extra;
        private FileStoredInfo stored;

        private FileEntryList parent;
        private int originalIndex;
        private byte[] data = null;
        //private ITypedFile typed = null;

        public uint Hash { get { return hash; } }
        public RawFileInfo Raw { get { return raw; } }
        public FileExtraInfo Extra { get { if (extra == null) extra = new FileExtraInfo(this); return extra; } }
        public FileStoredInfo Stored { get { if (stored == null) stored = new FileStoredInfo(); return stored; } set { stored = value; } }

        public FileEntryList Parent { get { return parent; } }
        public int OriginalIndex { get { return originalIndex; } }
        public byte[] Data { get { if (data == null) data = ReadContent(); return data; } }
        public uint VirtualSize = 0;

        public bool Translatable = false; // it contains translatable text or data?

        public FileEntry(uint hash, RawFileInfo raw, int originalIndex, FileEntryList parent)
        {
            this.hash = hash;
            this.raw = raw;
            this.originalIndex = originalIndex;
            this.parent = parent;
        }

        public int CompareTo(FileEntry other)
        {
            switch (Parent.CompareFields[0])
            {
                case FileEntryCompareField.OriginalIndex:
                    return OriginalIndex.CompareTo(other.OriginalIndex);
                case FileEntryCompareField.Hash:
                    return Hash.CompareTo(other.Hash);
                case FileEntryCompareField.Length:
                    return raw.Length.CompareTo(other.Raw.Length);
                case FileEntryCompareField.Location:
                    return raw.Location.CompareTo(other.Raw.Location);
                case FileEntryCompareField.LngCode:
                    return raw.LangCode.CompareTo(other.Raw.LangCode);
                case FileEntryCompareField.LangText:
                    return Extra.LangText.CompareTo(other.Extra.LangText);
                case FileEntryCompareField.Offset:
                    return Extra.Offset.CompareTo(other.Extra.Offset);
                case FileEntryCompareField.Data:
                    {
                        if (data == null && other.data == null)
                            return 0;
                        else
                            if (data == null)
                                return -1;
                            else
                                if (other.data == null)
                                    return 1;
                                else
                                {
                                    for (int i = 0; i < Math.Max(data.Length, other.Raw.Length); i++)
                                    {
                                        if (other.Data.Length < i && data.Length < i)
                                        {
                                            if (data[i] != other.Data[i])
                                                return data[i] - other.Data[i];
                                        }
                                        else
                                            if (other.data.Length >= i)
                                                return 1;
                                            else
                                                if (data.Length >= i)
                                                    return -1;
                                    }
                                    return 0;
                                }

                    }
                case FileEntryCompareField.Text:
                    return 0;// Extra.Text.CompareTo(other.Extra.Text);
                case FileEntryCompareField.FileName:
                    return stored.FileName.CompareTo(other.Stored.FileName);
                case FileEntryCompareField.FileType:
                    return stored.FileType.CompareTo(other.Stored.FileType);
                case FileEntryCompareField.VirtualLength:
                    return VirtualSize.CompareTo(other.VirtualSize);
                default:
                    throw new Exception(Errors.InvalidSortMode);
            }
        }

        public byte[] ReadContent()
        {
            return ReadContent(-1);
        }

        public byte[] ReadContent(int maxLen)
        {
            
            string fileExt;
            byte[] ret;
            if (parent.OneBigFile)
                fileExt = "dat";
            else
                fileExt = Extra.BigFile.ToString("d3");
            FileStream fs = new FileStream(parent.Path + "bigfile." + fileExt, FileMode.Open);
            try
            {
                fs.Seek(Extra.Offset, SeekOrigin.Begin);
                int readLength = (maxLen >= 0) ? maxLen : (int)raw.Length;
                ret = new byte[readLength];
                fs.Read(ret, 0, readLength);
            }
            finally
            {
                fs.Close();
            }
            return ret;
        }

        public void WriteContent(byte[] content)
        {
            string fileExt;
            if (VirtualSize < content.Length)
                throw new Exception(Errors.NewFileIsTooBig);
            if (parent.OneBigFile)
                fileExt = "dat";
            else
                fileExt = Extra.BigFile.ToString("d3");
            FileStream fs = new FileStream(parent.Path + "bigfile." + fileExt, FileMode.Open);
            try
            {
                FileStream fsfat = new FileStream(parent.Path + "bigfile." + (parent.OneBigFile ? ".dat" : "000"), FileMode.Open);
                try
                {
                    //fs.Seek(Offset, SeekOrigin.Begin);
                    //fs.Write(content, 0, content.Length);
                    if (raw.Length != content.Length)
                    {
                        // modify FAT
                        raw.Length = (uint)content.Length;
                        parent.UpdateFATEntry(this);
                    }
                }
                finally
                {
                    fsfat.Close();
                }
            }
            finally
            {
                fs.Close();
            }
            return;
        }
    }

    enum FileEntryCompareField
    {
        OriginalIndex = 0,
        Hash = 1,
        Length = 2,
        Location = 3,
        LngCode = 4,
        LangText = 5,
        BigFile = 6,
        Offset = 7,
        Data = 8,
        Text = 9,
        FileName = 10,
        //ResolveStatus = 11,
        FileType = 12,
        VirtualLength = 13
    }

    class FileEntryList : List<FileEntry>
    {
        private int entryCount;
        private bool oneBigFile;
        private string path;
        private List<FileEntryCompareField> compareFields =
                new List<FileEntryCompareField>(3) { FileEntryCompareField.Location };

        public int EntryCount { get { return entryCount; } }
        public bool OneBigFile { get { return oneBigFile; } }
        public string Path { get { return path; } }
        public List<FileEntryCompareField> CompareFields { get { return compareFields; } }
        public GameInfo gameInfo;
        //public string CheckSum;


        public FileEntryList(GameInfo gameInfo)
        {
            this.gameInfo = gameInfo;
            path = FileNameUtils.IncludeTrailingBackSlash(gameInfo.InstallPath);
            oneBigFile = File.Exists(path + "bigfile.dat");
        }

        private void ReadFAT()
        {
            int hashBlockSize;
            int fileBlockSize;
            byte[] workBuf = new byte[0];
            byte[] buf;

            FileStream fs = new FileStream(path + "bigfile." + (oneBigFile ? "dat" : "000"), FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            try
            {
                entryCount = br.ReadInt32();
                hashBlockSize = entryCount * sizeof(Int32);
                fileBlockSize = entryCount * RawFileInfoSize.Size;
                buf = br.ReadBytes(hashBlockSize + fileBlockSize);
            }
            finally
            {
                br.Close();
                fs.Close();
            }
            workBuf = new byte[hashBlockSize];
            Array.Copy(buf, 0, workBuf, 0, hashBlockSize);
            uint[] hashes = ArrayUtils.ByteArrayToStruct<uint>(workBuf, (int)entryCount);
            workBuf = new byte[fileBlockSize];
            Array.Copy(buf, hashBlockSize, workBuf, 0, fileBlockSize);
            RawFileInfo[] fileInfo = ArrayUtils.ByteArrayToStruct<RawFileInfo>(workBuf, (int)entryCount);

            Clear();
            for (int i = 0; i < entryCount; i++)
                Add(new FileEntry(hashes[i], fileInfo[i], i, this));

            // calculate virtual sizes
            compareFields.Clear();
            compareFields.Insert(0, FileEntryCompareField.Location);
            Sort(Comparison);
            for (int i = 0; i < entryCount - 1; i++)
                this[i].VirtualSize = (this[i + 1].Raw.Location - this[i].Raw.Location) * 0x800;
            this[entryCount - 1].VirtualSize = (this[entryCount - 1].Raw.Length + 0x800) - (this[entryCount - 1].Raw.Length + 0x800) % 0x800;

            // add stored infos
            compareFields.Clear();
            compareFields.Insert(0, FileEntryCompareField.Hash);
            Sort(Comparison);
            FileStoredInfoList infoList = new FileStoredInfoList();
            infoList.LoadFromFile(gameInfo.InstallPath + "trans\\TRTRStoredFileInfo.txt");
            for (int i = 0; i < entryCount - 1; i++)
                if (infoList.ContainsKey(this[i].Hash))
                    this[i].Stored = infoList[this[i].Hash];
        }

        public bool SortBy(FileEntryCompareField field)
        {
            bool needReSort = compareFields.Count == 0;
            if (!needReSort)
                needReSort = compareFields[0] == field;
            if (needReSort)
            {
                compareFields.Clear();
                compareFields.Insert(0, field);
                Sort(Comparison);
            }
            return needReSort;
        }

        public void CreateRestoration()
        {
            SortBy(FileEntryCompareField.Location);
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.AppendChild(doc.CreateProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\""));
                XmlElement rootElement = doc.CreateElement("restoration");
                XmlNode rootNode = doc.AppendChild(rootElement);
                rootElement.SetAttribute("version", "2.0");

                XmlElement menuElement = doc.CreateElement("menu");
                XmlNode menuNode = rootNode.AppendChild(menuElement);
                XmlElement subtitleElement = doc.CreateElement("subtitle");
                XmlNode subtitleNode = rootNode.AppendChild(subtitleElement);

                // write english subtitles of cinematics to xml & text
                foreach (FileEntry entry in this)
                    // initialize cinematics
                    if ((entry.Stored.FileType == FileTypeEnum.MUL_CIN) &&
                        (entry.Raw.Language == FileLanguage.English))
                    {
                        CineFile cine = new CineFile(entry);
                        XmlElement cineElement = doc.CreateElement("cine");
                        cineElement.SetAttribute("hash", entry.Extra.HashText);
                        if (entry.Stored.FileName.Length > 0)
                            cineElement.SetAttribute("filename", entry.Stored.FileName);
                        XmlNode cineNode = subtitleNode.AppendChild(cineElement);

                        for (int j = 0; j < cine.Blocks.Count; j++)
                        {
                            CineBlock block = cine.Blocks[j];
                            if (block.subtitles != null)
                            {
                                XmlElement blockElement = doc.CreateElement("block");
                                XmlNode blockNode = cineNode.AppendChild(blockElement);
                                blockElement.SetAttribute("no", j.ToString("d4"));
                                //blockElement.SetAttribute("translation", TextConv.ToOriginalFormat(block.subtitles[FileLanguage.English]));
                                foreach (FileLanguage lang in block.subtitles.Keys)
                                    blockElement.SetAttribute(LangNames.Dict[lang], TextConv.ToOriginalFormat(block.subtitles[lang]));

                            }
                        }
                    }
                    else
                    {
                        if ((entry.Stored.FileType == FileTypeEnum.BIN_MNU) &&
                            (entry.Raw.Language == FileLanguage.English))
                        {
                            MenuFile menu = new MenuFile(entry);
                            for (int i = 0; i < menu.MenuEntries.Count; i++)
                            {
                                MenuFileEntry menuEntry = menu.MenuEntries[i];
                                if (menuEntry.Original.Length > 0)
                                {
                                    XmlElement entryElement = doc.CreateElement("entry");
                                    XmlNode entryNode = menuElement.AppendChild(entryElement);
                                    entryElement.SetAttribute("no", i.ToString());
                                    //entryElement.SetAttribute("translation", TextConv.ToOriginalFormat(menuEntry.Translation));
                                    entryElement.SetAttribute(LangNames.Dict[FileLanguage.English], TextConv.ToOriginalFormat(menuEntry.Original));
                                }
                            }
                        }
                    }
            }
            finally
            {
                #region Crypt xml
                /*
                DESCryptoServiceProvider key = new DESCryptoServiceProvider();
                CryptoStream encStream = new CryptoStream(ms, key.CreateDecryptor(), CryptoStreamMode.Read);
                StreamReader sr = new StreamReader(encStream);
                BinaryReader bre = new BinaryReader(encStream);
                byte[] content;
                bre.Read(content, 0, 
                FileStream fsXML = new FileStream(gameInfo.InstallPath + gameInfo.GameNameAbbrev + "tra.xml", FileMode.Create);
                try
                {
                    encStream.Write(content, 0, (int)encStream.Length);
                    fsXML.Read(content, 0, content.Length);
                }
                finally
                {
                    fsXML.Close();
                }
                */
                #endregion
                doc.Save(gameInfo.InstallPath + "trans\\" + gameInfo.GameNameAbbrev + ".res.xml");
            }
        }




















































        public void Extract()
        {
            ReadFAT();
            compareFields.Clear();
            compareFields.Insert(0, FileEntryCompareField.Location);
            Sort(Comparison);

            int lastBF = -1;
            TextWriter cineWriter = null;
            TextWriter menuWriter = null;
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.AppendChild(doc.CreateProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\""));
                XmlElement rootElement = doc.CreateElement("restoration");
                XmlNode rootNode = doc.AppendChild(rootElement);
                rootElement.SetAttribute("version", "2.0");

                XmlElement menuElement = doc.CreateElement("menu");
                XmlNode menuNode = rootNode.AppendChild(menuElement);
                XmlElement subtitleElement = doc.CreateElement("subtitle");
                XmlNode subtitleNode = rootNode.AppendChild(subtitleElement);

                // write english subtitles of cinematics to xml & text
                foreach (FileEntry entry in this)
                    // initialize cinematics
                    if ((entry.Stored.FileType == FileTypeEnum.MUL_CIN) &&
                        (entry.Raw.Language == FileLanguage.English))
                    {
                        CineFile cine = new CineFile(entry);
                        XmlElement cineElement = doc.CreateElement("cine");
                        cineElement.SetAttribute("hash", entry.Extra.HashText);
                        if (entry.Stored.FileName.Length > 0)
                            cineElement.SetAttribute("filename", entry.Stored.FileName);
                        XmlNode cineNode = subtitleNode.AppendChild(cineElement);
                        if (entry.Extra.BigFile != lastBF)
                        {
                            if (cineWriter != null)
                                cineWriter.Close();
                            cineWriter = new StreamWriter(gameInfo.InstallPath + "trans\\bigfile." +
                                entry.Extra.BigFile.ToString("d3") + ".1.txt", false, Encoding.UTF8);
                            lastBF = entry.Extra.BigFile;
                        }

                        for (int j = 0; j < cine.Blocks.Count; j++)
                        {
                            CineBlock block = cine.Blocks[j];
                            if (block.subtitles != null)
                                if (block.subtitles.ContainsKey(FileLanguage.English))
                                {
                                    cineWriter.WriteLine("HASH: " + entry.Extra.HashText + "," + j.ToString("d") + "  " +
                                        ((entry.Stored.FileName.Length > 0) ? entry.Stored.FileName : entry.Extra.HashText));
                                    cineWriter.WriteLine('#' + TextConv.ToOriginalFormat(block.subtitles[FileLanguage.English]).Replace("\n", "\n#"));
                                    cineWriter.WriteLine(TextConv.ToOriginalFormat(block.subtitles[FileLanguage.English]) + "\r\n");
                                    XmlElement blockElement = doc.CreateElement("block");
                                    XmlNode blockNode = cineNode.AppendChild(blockElement);
                                    blockElement.SetAttribute("no", j.ToString("d4"));
                                    blockElement.SetAttribute("translation", TextConv.ToOriginalFormat(block.subtitles[FileLanguage.English]));
                                    foreach (FileLanguage lang in block.subtitles.Keys)
                                        blockElement.SetAttribute(LangNames.Dict[lang], TextConv.ToOriginalFormat(block.subtitles[lang]));
//                                    blockElement.SetAttribute("original", block.subtitles[FileLanguage.English]);
                                }
                        }
                    }
                    else
                    {
                        if ((entry.Stored.FileType == FileTypeEnum.BIN_MNU) &&
                            (entry.Raw.Language == FileLanguage.English))
                        {
                            if (menuWriter == null)
                                menuWriter = new StreamWriter(gameInfo.InstallPath + "trans\\menu.txt", false, Encoding.UTF8);
                            MenuFile menu = new MenuFile(entry);
                            for (int i = 0; i < menu.MenuEntries.Count; i++)
                            {
                                MenuFileEntry menuEntry = menu.MenuEntries[i];
                                if (menuEntry.Original.Length > 0)
                                {
                                    XmlElement entryElement = doc.CreateElement("entry");
                                    XmlNode entryNode = menuElement.AppendChild(entryElement);
                                    entryElement.SetAttribute("no", i.ToString());
                                    entryElement.SetAttribute("translation", TextConv.ToOriginalFormat(menuEntry.Translation));
                                    entryElement.SetAttribute("original", TextConv.ToOriginalFormat(menuEntry.Original));

                                    if (menuEntry.Original.Length > 0)
                                    {
                                        menuWriter.WriteLine("@" + (menuEntry.index + 1).ToString("d4"));
                                        menuWriter.WriteLine('#' + TextConv.ToOriginalFormat(menuEntry.Original).Replace("\n", "\n#"));
                                        menuWriter.WriteLine(TextConv.ToOriginalFormat(menuEntry.Translation) + "\r\n");
                                    }
                                }
                            }
                        }
                    }
            }
            finally
            {
                MemoryStream ms = new MemoryStream();
                doc.Save(ms);

                #region Crypt xml
                /*
                DESCryptoServiceProvider key = new DESCryptoServiceProvider();
                CryptoStream encStream = new CryptoStream(ms, key.CreateDecryptor(), CryptoStreamMode.Read);
                StreamReader sr = new StreamReader(encStream);
                BinaryReader bre = new BinaryReader(encStream);
                byte[] content;
                bre.Read(content, 0, 
                FileStream fsXML = new FileStream(gameInfo.InstallPath + gameInfo.GameNameAbbrev + "tra.xml", FileMode.Create);
                try
                {
                    encStream.Write(content, 0, (int)encStream.Length);
                    fsXML.Read(content, 0, content.Length);
                }
                finally
                {
                    fsXML.Close();
                }
                */
                #endregion

                doc.Save(gameInfo.InstallPath + "trans\\" + gameInfo.GameNameAbbrev + "_extracted.xml");
                if (cineWriter != null)
                    cineWriter.Close();
                if (menuWriter != null)
                    menuWriter.Close();
            }
        }

        public void Restore()
        {
            ReadFAT();
        }

        public void Translate()
        {
            ReadFAT();
            foreach (FileEntry entry in this)
            {
                if (entry.Stored.FileType == FileTypeEnum.MUL_CIN)
                    if (entry.Raw.Language == FileLanguage.English)
                    {
                        CineFile cine = new CineFile(entry);
                        cine.Translate();
                    }
                        //if (entry.Hash == 0x44F36963) //hackhack
            }
        }

        public void UpdateFATEntry(FileEntry entry)
        {
            FileStream fs = new FileStream(path + "bigfile." + (oneBigFile ? "dat" : "000"), FileMode.Open);
            BinaryWriter bw = new BinaryWriter(fs);
            try
            {
                int offset = sizeof(uint) + entryCount * 4 + entry.OriginalIndex * RawFileInfoSize.Size;
                bw.Seek(offset, SeekOrigin.Begin);
                byte[] buf = ArrayUtils.StructToByteArray<RawFileInfo>(entry.Raw);
                //bw.Write(buf);
            }
            finally
            {
                bw.Close();
                fs.Close();
            }
        }

        private static int Comparison(FileEntry entry1, FileEntry entry2)
        {
            return entry1.CompareTo(entry2);
        }
    }
}
