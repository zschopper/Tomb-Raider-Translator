using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;

namespace TRTR
{
    
    #region Primitives

    struct RawFileInfo
    {
        internal UInt32 Length;
        internal UInt32 Location;
        internal UInt32 LangCode;
        internal UInt32 Unknown;
        internal FileLanguage Language
        {
            get
            {
                switch (LangCode & 0x0F)
                {
                    case 0x0:
                        return FileLanguage.Spanish;
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

        static internal Int32 Size = 0x10;

        internal void ToRawFileInfo(byte[] buf, Int32 startIndex)
        {
            Length = BitConverter.ToUInt32(buf, startIndex + 0x00);
            Location = BitConverter.ToUInt32(buf, startIndex + 0x04);
            LangCode = BitConverter.ToUInt32(buf, startIndex + 0x08);
            Unknown = BitConverter.ToUInt32(buf, startIndex + 0x0C);
        }

        internal void GetBytes(byte[] buf, Int32 startIndex)
        {
            Array.Copy(BitConverter.GetBytes(Length), 0, buf, startIndex + 0x00, 4);
            Array.Copy(BitConverter.GetBytes(Location), 0, buf, startIndex + 0x04, 4);
            Array.Copy(BitConverter.GetBytes(LangCode), 0, buf, startIndex + 0x08, 4);
            Array.Copy(BitConverter.GetBytes(Unknown), 0, buf, startIndex + 0x0C, 4);
        }
    }

/*
 * static class RawFileInfoSize
    {
        internal static Int32 Size;

        static RawFileInfoSize()
        {
            Size = Marshal.SizeOf(typeof(RawFileInfo));
        }
    }
*/
    enum FileTypeEnum
    {
        Unknown = 0,
        CDRM = 1,
        MUL_CIN = 2,
        MUL2 = 3,
        MUL4 = 4,
        MUL6 = 5,
        RAW = 6,
        RAW_FNT = 7,
        BIN = 8,
        BIN_MNU = 9,
        PNG = 10,
        FSB4 = 11,
        MUS = 12,

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

    //[Flags]
    enum FileNameResolveStatus
    {
        Unknown = 0,
        Resolved = 1,
        NotResolved = 2
    }

    enum BoundaryDirection
    { 
        Up = 1,
        Down = 0
    }

    static class Boundary
    {
        internal static Int32 Up(Int32 value, Int32 boundary)
        {
            return (boundary - 1) - (value - 1) % boundary;
        }

        internal static Int32 Down(Int32 value, Int32 boundary)
        {
            return value % boundary;
        }

        internal static Int32 Extend(Int32 value, Int32 boundary)
        {
            return value + (boundary - 1) - (value - 1) % boundary;
        }

        internal static Int32 Shrink(Int32 value, Int32 boundary)
        {
            return value - (value % boundary);
        }
    }

    #endregion

    static class FileTypeStrings
    {
        internal static Dictionary<FileTypeEnum, string> Value;
        static FileTypeStrings()
        {
            Value = new Dictionary<FileTypeEnum, string>();
            Value.Add(FileTypeEnum.Unknown, GeneralTexts.Unknown);
            Value.Add(FileTypeEnum.CDRM, "CDRM");
            Value.Add(FileTypeEnum.MUL_CIN, "CIN");
            Value.Add(FileTypeEnum.RAW, "RAW");
            Value.Add(FileTypeEnum.RAW_FNT, "FNT");
            Value.Add(FileTypeEnum.BIN_MNU, "MNU");
        }

        internal static FileTypeEnum Key(string value)
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
        internal string FileName = string.Empty;
        internal FileTypeEnum FileType = FileTypeEnum.Unknown;
    }

    class FileStoredInfoList : Dictionary<UInt32, FileStoredInfo>
    {
        internal void LoadFromFile(string fileName)
        {
            string line;
            if (!File.Exists(fileName))
            {
                Exception ex = new Exception(Errors.MissingFileInfo);
                ex.Data.Add("filename", fileName);
                throw ex;
            }
            
            StreamReader sr = new StreamReader(fileName);
            try
            {
                Int32 i = 0;
                while (!sr.EndOfStream)
                {
                    //string
                    i++;
                    line = sr.ReadLine();
                    if (line.Length > 0 && !line.StartsWith(TransConsts.CommentPrefix + "") && !line.StartsWith(TransConsts.OriginalPrefix + ""))
                    {
                        string[] values = line.Split('\t');
                        if (values.Length < 3)
                        {
                            Exception ex = new Exception(Errors.InvalidStoredFileInfo);
                            ex.Data.Add("line", i.ToString());
                            throw ex;
                        }
                        FileStoredInfo info = new FileStoredInfo();
                        UInt32 key = UInt32.Parse(values[0], System.Globalization.NumberStyles.HexNumber);
                        if (values[0] != values[1])
                            info.FileName = values[1];
                        info.FileType = FileTypeStrings.Key(values[2]);
                        Add(key, info);
                    }
                }
            }
            finally
            {
                sr.Close();
            }
        }
    }

    class FileExtraInfo
    {
        //uint something_size = 0x12C00; // TRA/TRL v1.0.0.6
        uint something_size = 0xFFE00;  // TRU/LCGOL v1.1.0.7

        private FileLanguage language;
        private string hashText;

        internal string HashText { get { return hashText; } }
        internal string LangText { get { return LangNames.Dict[language]; } }
        internal FileLanguage Language { get { return language; } }
        internal UInt32 Offset;
        internal UInt32 AbsOffset;
        internal byte[] Data = null;
        internal string Text = string.Empty;
        internal Int32 BigFile;
        private FileEntry parent;
        internal string BigFileName { get { return parent.Parent.Path + "bigfile." + 
            (parent.Parent.OneBigFile ? "dat" : BigFile.ToString("d3")); } }

        internal FileExtraInfo(FileEntry parent)
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
                this.BigFile = (Int32)(parent.Raw.Location / something_size); 
                this.Offset = parent.Raw.Location % something_size * 0x800;
            }
        }

        private void UpdateLangText()
        {
            switch (parent.Raw.LangCode & 0x0F)
            {
                case 0x0:
                    { language = FileLanguage.Spanish; return; }
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
        private UInt32 hash;
        private RawFileInfo raw;
        private FileExtraInfo extra = null;
        private FileStoredInfo stored = null;

        private FileEntryList parent = null;
        private Int32 originalIndex;

        internal UInt32 Hash { get { return hash; } }
        internal RawFileInfo Raw { get { return raw; } }
        internal FileExtraInfo Extra { get { if (extra == null) extra = new FileExtraInfo(this); return extra; } }
        internal FileStoredInfo Stored { 
            get { if (stored == null) stored = new FileStoredInfo(); return stored; } 
            set { stored = value; } }

        internal FileEntryList Parent { get { return parent; } }
        internal Int32 OriginalIndex { get { return originalIndex; } }
        internal UInt32 VirtualSize = 0;

        internal bool Translatable = false; // it contains translatable text or data?

        internal FileEntry(UInt32 hash, RawFileInfo raw, Int32 originalIndex, FileEntryList parent)
        {
            this.hash = hash;
            this.raw = raw;
            this.originalIndex = originalIndex;
            this.parent = parent;
        }

        internal Int32 CompareTo(FileEntry other)
        {
            return CompareTo(other, 0);
        }

        internal Int32 CompareTo(FileEntry other, Int32 level)
        {
            Int32 ret = 0;
            switch (Parent.CompareFields[level])
            {
                case FileEntryCompareField.OriginalIndex:
                    ret = OriginalIndex.CompareTo(other.OriginalIndex);
                    break;
                case FileEntryCompareField.Hash:
                    ret = Hash.CompareTo(other.Hash);
                    break;
                case FileEntryCompareField.Length:
                    ret = raw.Length.CompareTo(other.Raw.Length);
                    break;
                case FileEntryCompareField.Location:
                    ret = raw.Location.CompareTo(other.Raw.Location);
                    break;
                case FileEntryCompareField.LngCode:
                    ret = raw.LangCode.CompareTo(other.Raw.LangCode);
                    break;
                case FileEntryCompareField.LangText:
                    ret = Extra.LangText.CompareTo(other.Extra.LangText);
                    break;
                case FileEntryCompareField.Offset:
                    ret = Extra.Offset.CompareTo(other.Extra.Offset);
                    break;
                case FileEntryCompareField.Data:
                    throw new NotImplementedException();
                case FileEntryCompareField.Text:
                    throw new NotImplementedException(); // Extra.Text.CompareTo(other.Extra.Text);
                case FileEntryCompareField.FileName:
                    ret = stored.FileName.CompareTo(other.Stored.FileName);
                    break;
                case FileEntryCompareField.FileType:
                    ret = stored.FileType.CompareTo(other.Stored.FileType);
                    break;
                case FileEntryCompareField.VirtualLength:
                    ret = VirtualSize.CompareTo(other.VirtualSize);
                    break;
//                case FileEntryCompareField.EndOffset:
//                    ret = (raw.Location + raw.Length).CompareTo(other.Raw.Location + other.Raw.Length);
//                    break;
                default:
                    throw new Exception(Errors.InvalidSortMode);
            }
            if (ret == 0 && level < Parent.CompareFields.Count - 1)
                ret = CompareTo(other, level + 1);
            return ret;
        }

        internal byte[] ReadContent()
        {
            return ReadContent(-1);
        }

        internal byte[] ReadContent(Int32 maxLen)
        {
            
            byte[] ret;
            FileStream fs = FilePool.Open(this);
            try
            {
                fs.Seek(Extra.Offset, SeekOrigin.Begin);
                Int32 readLength = (maxLen >= 0) ? maxLen : (Int32)raw.Length;
                ret = new byte[readLength];
                fs.Read(ret, 0, readLength);
            }
            finally
            {
                FilePool.Close(this);
            }
            return ret;
        }

        internal byte[] ReadContent(Int32 startPos, Int32 maxLen)
        {

            byte[] ret;
            FileStream fs = FilePool.Open(this);
            try
            {
                fs.Seek(Extra.Offset + startPos, SeekOrigin.Begin);
                Int32 readLength = (maxLen >= 0) ? maxLen : (Int32)raw.Length - startPos;
                ret = new byte[readLength];
                fs.Read(ret, 0, readLength);
            }
            finally
            {
                FilePool.Close(this);
            }
            return ret;
        }

        internal void WriteContent(byte[] content)
        {
            bool testWrite = false;
#if DEBUG_
            if (raw.Length != content.Length && Settings.TransRootDir.Length > 0 && Directory.Exists(Settings.TransRootDir))
            {

                StreamWriter fatWriter = new StreamWriter(Settings.TransRootDir + "trans\\tmp\\FATChanges.txt", true, Encoding.UTF8);
                try
                {
                    fatWriter.WriteLine("H:{0:X8}\tO:{1:d6}\tRLo:{3:d12}\tRLn:{3:d12}\tCLn:{4:d12}\tDi:{5:d12}\tFn:{6}",
                        hash, originalIndex, raw.Location, raw.Length, content.Length, content.Length - raw.Length, stored.FileName);
                }
                finally
                {
                    fatWriter.Close();
                }
            }
#endif
            if (testWrite)
            {
                string fileName = Parent.Path + @"trans\" +
                    ((Stored.FileName.Length > 0)
                        ? Stored.FileName.Replace(@"\", "[bs]")
                        : Extra.HashText);
                FileStream fstmp = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                try
                {
                    fstmp.Write(content, 0, content.Length);
                }
                finally
                {
                    fstmp.Close();
                }
            }
            else
            {
                if (VirtualSize < content.Length)
                {
                    Exception ex = new Exception(Errors.NewFileIsTooBig);
                    ex.Data.Add("bigfile", Extra.BigFileName);
                    ex.Data.Add("hash", Extra.HashText);
                    ex.Data.Add("diff", (content.Length - VirtualSize).ToString() + " byte(s)");
                    throw ex;
                }

                FileStream fs = FilePool.Open(this);
                try
                {
                    if (extra.Offset + content.Length > fs.Length)
                    {
                        Exception ex = new Exception(Errors.NewFileIsTooBig);
                        ex.Data.Add("bigfile", Extra.BigFileName);
                        ex.Data.Add("hash", Extra.HashText);
                        ex.Data.Add("diff", (content.Length - VirtualSize).ToString() + " byte(s)");
                        throw ex;
                    }

                    fs.Position = extra.Offset;
                    if (!parent.simulateWrite)
                    {
                        Int64 fsLen = fs.Length;
                        fs.Write(content, 0, content.Length);
                        // fill virtual space
                        Int32 fillLen = (Int32)(VirtualSize - content.Length);
                        // .. but don't grow file
                        if (fs.Position + fillLen > fs.Length)
                            fillLen = (Int32)(fs.Length - fs.Position);
                        if (fillLen > 0)
                            fs.Write(new byte[fillLen], 0, fillLen);

                        if (fsLen != fs.Length)
                        {
                            Exception ex = new Exception("Bigfile resized during write");
                            ex.Data.Add("bigfile", Extra.BigFileName);
                            ex.Data.Add("hash", Extra.HashText);
                            ex.Data.Add("diff", (fs.Length - fsLen).ToString() + " byte(s)");
                            throw ex; 
                        }
                    }
                }
                finally
                {
                    FilePool.Close(this);
                }
                if (raw.Length != content.Length)
                {
                    // modify FAT
                    raw.Length = (UInt32)content.Length;
                    parent.UpdateFATEntry(this);
                }
            }
        }
    }

    class FilePoolEntry
    {
        internal FileStream Stream = null;
        private Int32 count = 0;
        Int32 Count { get { return count; } }
        string fileName = string.Empty;

        internal Int32 Open()
        {
            if (count++ == 0)
                Stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            return count;
        }

        internal Int32 Close()
        {
            if (--count == 0)
            {
                if (Stream != null)
                {
                    FileStream fs = Stream;
                    Stream = null;
                    fs.Close();
                }
            }
            return count;
        }

        internal Int32 CloseAll()
        {
            if (Stream != null)
            {
                FileStream fs = Stream;
                Stream = null;
                fs.Close();
            }
            count = 0;
            return count;
        }

        internal FilePoolEntry(string fileName)
        {
            this.fileName = fileName;
        }
    }

    static class FilePool
    {
        static private Dictionary<Int32, FilePoolEntry> pool = new Dictionary<Int32, FilePoolEntry>();

        static internal FileStream Open(FileEntry entry)
        {
            FileStream fs = Open(entry.Extra.BigFile);
            fs.Position = entry.Extra.Offset;
            return fs;
        }

        static internal FileStream Open(Int32 index)
        {
            FilePoolEntry entry;
            
            if (!pool.ContainsKey(index))
            {

                if (TRGameInfo.InstallInfo.OneBigFile)
                    entry = new FilePoolEntry(TRGameInfo.InstallInfo.InstallPath + "bigfile.dat");
                else
                    entry = new FilePoolEntry(TRGameInfo.InstallInfo.InstallPath + "bigfile." + index.ToString("d3"));
                pool.Add(index, entry);
            }
            else
                entry = pool[index];
            entry.Open();
            return entry.Stream;
        }

        static internal void CloseAll()
        {
            foreach (Int32 key in pool.Keys)
            {
                pool[key].CloseAll();
            }
            pool.Clear();
        }

        static internal void Close(Int32 Index)
        {
            pool[Index].Close();
        }

        static internal void Close(FileEntry entry)
        {
            pool[entry.Extra.BigFile].Close();
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
        //EndOffset = 14
    }

    class FileEntryList : List<FileEntry>
    {
        const UInt32 MaxFileEntryCount = 20000;
        const UInt32 MaxFileSize = 30000000;
        private Int32 entryCount;
        private bool oneBigFile;
        private string path;
        private List<FileEntryCompareField> compareFields =
                new List<FileEntryCompareField>(3) { FileEntryCompareField.Location };

        internal Int32 EntryCount { get { return entryCount; } }
        internal bool OneBigFile { get { return oneBigFile; } }
        internal string Path { get { return path; } }
        internal List<FileEntryCompareField> CompareFields { get { return compareFields; } }

        internal bool simulateWrite = false;
        private BackgroundWorker worker;

        internal FileEntryList(BackgroundWorker worker)
        {
            this.worker = worker;
            path = FileNameUtils.IncludeTrailingBackSlash(TRGameInfo.InstallInfo.InstallPath);
            oneBigFile = File.Exists(path + "bigfile.dat");
        }

        internal void ReadFAT()
        {
            Int32 hashBlockSize;
            Int32 fileBlockSize;
            byte[] buf = null;

            FileStream fs = FilePool.Open(0);
            try
            {
                buf = new byte[4];
                fs.Read(buf, 0, 4);
                entryCount = BitConverter.ToInt32(buf, 0);
                if (entryCount == 0 || entryCount > MaxFileEntryCount)
                    throw new Exception(string.Format(Errors.FATEntryCountError, entryCount));
                hashBlockSize = entryCount * sizeof(Int32);
                fileBlockSize = entryCount * RawFileInfo.Size;
                buf = new byte[hashBlockSize + fileBlockSize];
                fs.Read(buf, 0, buf.Length);
            }
            finally
            {
                FilePool.Close(0);
            }
            UInt32[] hashes = new UInt32[entryCount];
            RawFileInfo[] fileInfo = new RawFileInfo[entryCount];

            // fill hashTable & fileinfo table;
            for (Int32 i = 0; i < entryCount; i++)
            {
                hashes[i] = BitConverter.ToUInt32(buf, i * 4);
                fileInfo[i].ToRawFileInfo(buf, hashBlockSize + i * 0x10);
            }

            Clear();
            for (Int32 i = 0; i < entryCount; i++)
                Add(new FileEntry(hashes[i], fileInfo[i], i, this));

            // calculate virtual sizes
            SortBy(FileEntryCompareField.Location);
            for (Int32 i = 0; i <= entryCount - 2; i++)
            {
                this[i].VirtualSize = (this[i + 1].Raw.Location - this[i].Raw.Location) * 0x800;
                if (this[i].VirtualSize == 0)
                {
//                    if (i == entryCount - 2)
//                        ;//                        throw new Exception(string.Format(DebugErrors.FATParseError, this[i].Hash));
//                    else
                     this[i].VirtualSize = (this[i + 2].Raw.Location - this[i].Raw.Location) * 0x800;
                }
            }
            this[entryCount - 1].VirtualSize = (uint)Boundary.Extend((int)(this[entryCount - 1].Raw.Length), 0x800);

            // add stored infos
            SortBy(FileEntryCompareField.Hash);
            FileStoredInfoList infoList = new FileStoredInfoList();
/**/
            // determine file type
/*
 * // unnecessary
            if ( File.Exists(".\\" + TRGameInfo.InstallInfo.GameNameFull + ".files.txt"))
            {
                infoList.LoadFromFile(".\\" + TRGameInfo.InstallInfo.GameNameFull + ".files.txt");
                for (Int32 i = 0; i < entryCount - 1; i++)
                    if (infoList.ContainsKey(this[i].Hash))
                    {
                        if (this[i].Raw.Language == FileLanguage.Unknown ||
                            this[i].Raw.Language == FileLanguage.NoLang ||
                            this[i].Raw.Language == FileLanguage.English)
                        {
                            this[i].Stored = infoList[this[i].Hash];
                            this[i].Translatable = true;
                        }
                    }
            }

 */ 
            for (Int32 i = 0; i < entryCount - 1; i++)
            {
                this[i].Stored.FileType = FileTypeEnum.Unknown;
                if (this[i].Hash.ToString("X8") == "7CD333D3")
                {
                    this[i].Stored.FileType = FileTypeEnum.BIN_MNU;
                    this[i].Translatable = true;
                }
                else
                {
                    byte[] bufMagic = this[i].ReadContent(4);
                    //if(buf.
                    string magic = Encoding.ASCII.GetString(bufMagic);
                    switch (magic)
                    {
                        case "CDRM":
                            this[i].Stored.FileType = FileTypeEnum.CDRM;
                            break;
                        case "!WAR":
                            this[i].Stored.FileType = FileTypeEnum.RAW;
                            break;
                        case "FSB4":
                            this[i].Stored.FileType = FileTypeEnum.FSB4;
                            break;
                        case "MUS!":
                            this[i].Stored.FileType = FileTypeEnum.MUS;
                            break;

                        default:
                            // MUL file test
                            if (this[i].Raw.Length > 8) {
                                bufMagic = this[i].ReadContent(4, 4);
                                int magicInt = BitConverter.ToInt32(bufMagic, 0);
                                if (magicInt == -1 || magicInt == 0) {
                                    this[i].Stored.FileType = FileTypeEnum.MUL2;
                                }
                            }

                            if (this[i].Raw.Length > 0x814)
                                if (Encoding.ASCII.GetString(this[i].ReadContent(0x810, 4)) == "ENIC")
                                    this[i].Stored.FileType = FileTypeEnum.MUL_CIN;
                            break;
                    }
                    if (this[i].Stored.FileType == FileTypeEnum.MUL_CIN || this[i].Stored.FileType == FileTypeEnum.RAW_FNT)
                        this[i].Translatable = true;
                }
            }
        }

        internal void CreateRestoration()
        {
            Int32 lastReported = 0;
            worker.ReportProgress(0, StaticTexts.creatingRestorationPoint);
            ReadFAT();
            SortBy(FileEntryCompareField.Location);
            XmlDocument doc = new XmlDocument();

            doc.AppendChild(doc.CreateProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\""));
            XmlElement rootElement = doc.CreateElement("restoration");
            XmlNode rootNode = doc.AppendChild(rootElement);
            rootElement.SetAttribute("version", "2.2");

            XmlElement menuElement = doc.CreateElement("menu");
            XmlNode menuNode = rootNode.AppendChild(menuElement);
            XmlElement subtitleElement = doc.CreateElement("subtitle");
            XmlNode subtitleNode = rootNode.AppendChild(subtitleElement);
            XmlElement fontElement = doc.CreateElement("font");
            XmlNode fontNode = rootNode.AppendChild(fontElement);

            // write english subtitles of cinematics to xml & text
            for (Int32 i = 0; i < this.Count; i++)
            {
                FileEntry entry = this[i];
                switch (entry.Stored.FileType)
                {
                    case FileTypeEnum.BIN_MNU:
                        {
                            if (entry.Raw.Language == FileLanguage.English)
                            {
                                MenuFile menu = new MenuFile(entry);
                                menu.CreateRestoration(menuElement, menuNode);
                            }
                            break;
                        }
                    case FileTypeEnum.MUL_CIN:
                        {
                            if (entry.Raw.Language == FileLanguage.English || entry.Raw.Language == FileLanguage.NoLang)
                            {
                                CineFile cine = new CineFile(entry);
                                cine.CreateRestoration(subtitleElement, subtitleNode);
                            } 
                            break;
                        }
                    case FileTypeEnum.RAW_FNT:
                        {
                            FontFile font = new FontFile(entry);
                            font.CreateRestoration(fontElement, fontNode);
                            break;
                        }
                }
                Int32 percent = i * 100 / this.Count;
                if (percent > lastReported)
                {
                    worker.ReportProgress(percent, StaticTexts.translating);
                    lastReported = percent;
                }
            }
            doc.Save(".\\" + TRGameInfo.InstallInfo.GameNameFull + ".res.xml");
            worker.ReportProgress(100, StaticTexts.creatingRestorationPointDone);
        }

        internal void GenerateFilesTxt()
        {
            Int32 lastReported = 0;
            worker.ReportProgress(lastReported, StaticTexts.creatingFilesTxt);
            ReadFAT();
            TextWriter twEntries = new StreamWriter(@"c:\tmp\fat.entries.txt");
            for (Int32 i = 0; i < this.Count; i++) {
                twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8}", this[i].Hash, this[i].Raw.Location, this[i].Raw.Length, this[i].Raw.LangCode));
            }
            twEntries.Close();

            SortBy(FileEntryCompareField.Location);
            TextWriter tw = new StreamWriter(@"c:\tmp\custom.files.txt");
            
            for (Int32 i = 0; i < this.Count; i++)
            {
                
                FileEntry entry = this[i];
                string magic = string.Empty;
                bool writeIt = false;

                
                if (entry.Raw.Language == FileLanguage.NoLang || entry.Raw.Language == FileLanguage.Unknown || entry.Raw.Language ==  FileLanguage.English)
                {
                    writeIt = true; // entry.Stored.FileType == FileTypeEnum.MUL_CIN;
                }
                if(writeIt)
                    tw.WriteLine(string.Format("{0:X8}\t{1:X8}\t{2}", entry.Hash, entry.Hash, entry.Stored.FileType.ToString(), entry.Raw.Language.ToString()));
                
                
            }
            tw.Close();
            
        }

        [Conditional("DEBUG")]
        internal void Extract(string destFolder)
        {
            ReadFAT();
            SortBy(FileEntryCompareField.Location);

//            Int32 lastBF = -1;
            TextWriter cineWriter = null;
            TextWriter menuWriter = null;
            string valueSep = ";";
            try
            {
                // write english subtitles of cinematics to text
                Directory.CreateDirectory(destFolder);
                
                
                cineWriter = new StreamWriter(System.IO.Path.Combine(destFolder, "subtitles.txt"), false, Encoding.UTF8);
                cineWriter.WriteLine(";extracted from datafiles");
                foreach (FileEntry entry in this)
                {
                    switch (entry.Stored.FileType)
                    {
                        case FileTypeEnum.MUL_CIN:
                            {
                                if (entry.Raw.Language == FileLanguage.English || entry.Raw.Language == FileLanguage.NoLang)
                                {
                                    CineFile cine = new CineFile(entry);
                                    #region old method

                                    /*
                                    string header = "HASH: " + entry.Extra.HashText + 
                                        (entry.Stored.FileName.Length > 0 ? valueSep + "FILENAME: " + entry.Stored.FileName : string.Empty)
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
                                        (entry.Stored.FileName.Length > 0 ? valueSep + "FILENAME: " + entry.Stored.FileName : string.Empty);
                                    if (entry.Raw.Language == FileLanguage.English)
                                        header += ";sub";
                                    else
                                        header += ";dir";

                                    string blockOrig = String.Empty;
                                    string blockTrans = String.Empty;
                                    for (Int32 j = 0; j < cine.Blocks.Count; j++)
                                    {
                                        CineBlock block = cine.Blocks[j];
                                        if (block.subtitles != null)
                                        {
                                            UInt32 textCount = block.subtitles.TextCount(FileLanguage.English);
                                            for(UInt32 k = 0 ; k < textCount; k++)
                                            {
                                                string text = block.subtitles.Entry(FileLanguage.English, k).Text;
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
                                break;
                            }
                        case FileTypeEnum.BIN_MNU:
                            {
                                if (entry.Raw.Language == FileLanguage.English)
                                {
                                    if (menuWriter == null)
                                        menuWriter = new StreamWriter(System.IO.Path.Combine(destFolder, "menu.txt"), false, Encoding.UTF8);
                                        menuWriter.WriteLine(";extracted from datafiles");
                                    MenuFile menu = new MenuFile(entry);
                                    for (Int32 i = 0; i < menu.MenuEntries.Count; i++)
                                    {
                                        MenuFileEntry menuEntry = menu.MenuEntries[i];
                                        if (menuEntry.Current.Length > 0)
                                        {
                                            menuWriter.WriteLine(TransConsts.MenuEntryHeader + (menuEntry.index).ToString("d4"));
                                            menuWriter.WriteLine(TransConsts.OriginalPrefix + CineFile.textConv.ToOriginalFormat(menuEntry.Current).Replace("\n", "\r\n" + TransConsts.OriginalPrefix));
                                            menuWriter.WriteLine(CineFile.textConv.ToOriginalFormat(menuEntry.Current).Replace("\n", "\r\n") + "\r\n");
                                        }
                                    }
                                }
                                break;
                            }
                        case FileTypeEnum.RAW_FNT:
                            {
                                byte[] buf = entry.ReadContent();
                                FileStream fs = new FileStream(TRGameInfo.InstallInfo.InstallPath + "trans\\font_original.raw", FileMode.Create);
                                try
                                {
                                    fs.Write(buf, 0, buf.Length);
                                }
                                finally {
                                    fs.Close();
                                }
                                break;
                            }
                    }
                }
            }
            finally
            {
                if (cineWriter != null)
                    cineWriter.Close();
                if (menuWriter != null)
                    menuWriter.Close();
            }
        }

        internal void Restore()
        {
            Int32 lastReported = 0;
            worker.ReportProgress(0, StaticTexts.restoring);
            ReadFAT();
            for (Int32 i = 0; i < this.Count; i++)
            {
                FileEntry entry = this[i];
                switch (entry.Stored.FileType)
                {
                    case FileTypeEnum.MUL_CIN:
                        {
                            if (entry.Raw.Language == FileLanguage.English)
                            {
                                CineFile cine = new CineFile(entry);
                                cine.Restore();
                            }
                            break;
                        }
                    case FileTypeEnum.BIN_MNU:
                        {
                            if (entry.Raw.Language == FileLanguage.English)
                            {
                                MenuFile menu = new MenuFile(entry);
                                menu.Restore();
                            }
                            break;
                        }
                    case FileTypeEnum.RAW_FNT:
                        {
                            FontFile font = new FontFile(entry);
                            font.Restore();
                            break;
                        }
                } // switch
                // notify user about translation progress
                Int32 percent = i * 100 / this.Count;
                if (percent > lastReported)
                {
                    worker.ReportProgress(percent, StaticTexts.restoring);
                    lastReported = percent;
                }
            }
            worker.ReportProgress(100, StaticTexts.restorationDone);

            // $setup a menünél
            FilePool.CloseAll();
        }

        internal void Translate()
        {
            Int32 lastReported = 0;
            worker.ReportProgress(0, StaticTexts.translating);
            // Log.Write("reading FAT: started");
            ReadFAT();
            // Log.Write("reading FAT: finished");
            for (Int32 i = 0; i < this.Count; i++)
            {
                FileEntry entry = this[i];
                switch (entry.Stored.FileType)
                {
                    case FileTypeEnum.MUL_CIN:
                        {
                            if (entry.Raw.Language == FileLanguage.English)
                            {
                                CineFile cine = new CineFile(entry);
                                cine.Translate();
                            }
                            break;
                        }
                    case FileTypeEnum.BIN_MNU:
                        {
                            if (entry.Raw.Language == FileLanguage.English)
                            {
                                MenuFile menu = new MenuFile(entry);
                                menu.Translate();
                            }
                            break;
                        }
                    case FileTypeEnum.RAW_FNT:
                        {
                            FontFile font = new FontFile(entry);
                            font.Translate();
                            break;
                        }
                } // switch

                // notify user about translation progress
                Int32 percent = i * 100 / this.Count;
                if (percent > lastReported)
                {
                    worker.ReportProgress(percent, StaticTexts.translating);
                    lastReported = percent;
                }
            }
            worker.ReportProgress(100, StaticTexts.translationDone);
            FilePool.CloseAll();
        }

        internal void UpdateFATEntry(FileEntry entry)
        {
            FileStream fs = FilePool.Open(0);
            try
            {
                Int32 offset = sizeof(UInt32) + entryCount * 4 + entry.OriginalIndex * RawFileInfo.Size;
#if DEBUG
                fs.Position = offset;
                byte[] readBuf = new byte[RawFileInfo.Size];
                fs.Read(readBuf, 0, RawFileInfo.Size);
                RawFileInfo info = new RawFileInfo();
                info.ToRawFileInfo(readBuf, 0);
                if (entry.Raw.Location != info.Location)
                    throw new Exception(String.Format(DebugErrors.FATUpdateLocationError, entry.Hash));
#endif
                fs.Position = offset;
                byte[] buf = new byte[RawFileInfo.Size];
                entry.Raw.GetBytes(buf, 0);
                if (!simulateWrite)
                    fs.Write(buf, 0, buf.Length);
            }
            finally
            {
                FilePool.Close(0);
            }
        }

        internal bool SortBy(FileEntryCompareField field)
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

        private static Int32 Comparison(FileEntry entry1, FileEntry entry2)
        {
            return entry1.CompareTo(entry2);
        }
    }
}
