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
        internal UInt32 Hash;
        internal UInt32 LangCode;
        internal UInt32 Length;
        internal UInt32 Location;
        internal FileLanguage Language { get { return getLanguage(); } }

        private FileLanguage getLanguage()
        {
            if (LangCode == 0xFFFFFFFF)
                return FileLanguage.NoLang;

            if ((LangCode & 1) != 0)
                return FileLanguage.English;
            if ((LangCode & 1 << 0x1) != 0)
                return FileLanguage.French;
            if ((LangCode & 1 << 0x2) != 0)
                return FileLanguage.German;
            if ((LangCode & 1 << 0x3) != 0)
                return FileLanguage.Italian;
            if ((LangCode & 1 << 0x4) != 0)
                return FileLanguage.Spanish;
            if ((LangCode & 1 << 0x6) != 0)
                return FileLanguage.Portuguese;
            if ((LangCode & 1 << 0x7) != 0)
                return FileLanguage.Polish;
            if ((LangCode & 1 << 0x9) != 0)
                return FileLanguage.Russian;
            if ((LangCode & 1 << 0xA) != 0)
                return FileLanguage.Czech;
            if ((LangCode & 1 << 0xB) != 0)
                return FileLanguage.Dutch;
            if ((LangCode & 1 << 0xD) != 0)
                return FileLanguage.Arabic;
            if ((LangCode & 1 << 0xE) != 0)
                return FileLanguage.Korean;
            if ((LangCode & 1 << 0xF) != 0)
                return FileLanguage.Chinese;

            return FileLanguage.Unknown;
        }

        static internal Int32 Size = 0x10;

        internal void ToRawFileInfo(byte[] buf, Int32 startIndex, Int32 ofs = 0)
        {
            Hash = BitConverter.ToUInt32(buf, startIndex + 0x00);
            LangCode = BitConverter.ToUInt32(buf, startIndex + 0x04);
            Length = BitConverter.ToUInt32(buf, startIndex + 0x08);
            Location = BitConverter.ToUInt32(buf, startIndex + 0x0C);
            if (ofs < 0)
                Location = checked((UInt32)(Location + ofs));
        }

        internal void GetBytes(byte[] buf, Int32 startIndex)
        {
            Array.Copy(BitConverter.GetBytes(Hash), 0, buf, startIndex + 0x00, 4);
            Array.Copy(BitConverter.GetBytes(LangCode), 0, buf, startIndex + 0x04, 4);
            Array.Copy(BitConverter.GetBytes(Length), 0, buf, startIndex + 0x08, 4);
            Array.Copy(BitConverter.GetBytes(Location), 0, buf, startIndex + 0x0C, 4);
        }
    }

    /*
        static class RawFileInfoSize
        {
            internal static Int32 Size;

            static RawFileInfoSize()
            {
                Size = Marshal.SizeOf(typeof(RawFileInfo));
            }
        }
    */
    [Flags]
    enum FileTypeEnum
    {
        Unknown = 0,
        Special = 1,
        DRM = 2,
        CDRM = 4,
        MUL_CIN = 8,
        MUL2 = 16,
        MUL4 = 32,
        MUL6 = 64,
        RAW = 128,
        RAW_FNT = 256,
        BIN = 512,
        BIN_MNU = 1024,
        PNG = 2048,
        FSB4 = 4096,
        MUS = 8192,
        SCH = 16384,
        PCD9 = 32768
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

    // calc
    class FileExtraInfo
    {
        //uint something_size = 0x12C00; // TRA/TRL v1.0.0.6
        //uint something_size = 0xFFE00;  // TRU/LCGOL v1.1.0.7

        #region private declarations
        private FileLanguage language;
        private string langText;
        private string hashText;
        private UInt32 bigFileIndex;
        private string bigfileName;
        private string bigfilePrefix;
        private UInt32 offset;
        private UInt32 absOffset;
        private byte[] data = null;
        private string text = string.Empty;
        private FileEntry entry;
        private string fileName = string.Empty;
        private FileTypeEnum fileType = FileTypeEnum.Unknown;
        private string magic = string.Empty;
        #endregion

        internal string HashText { get { return hashText; } }
        internal string LangText { get { return langText; } }
        internal FileLanguage Language { get { return language; } }
        internal UInt32 Offset { get { return offset; } set { offset = value; } }
        internal UInt32 AbsOffset { get { return absOffset; } set { absOffset = value; } }
        internal byte[] Data { get { return data; } set { data = value; } }
        internal string Text { get { return text; } set { text = value; } }
        internal UInt32 BigFileIndex { get { return bigFileIndex; } set { bigFileIndex = value; } }
        internal string BigFileName { get { return bigfileName; } set { bigfileName = value; } }
        internal string BigFilePrefix { get { return bigfilePrefix; } set { bigfilePrefix = value; } }

        // stored infos
        internal string FileName { get { return fileName; } set { fileName = value; } }
        internal FileTypeEnum FileType { get { return fileType; } set { fileType = value; } }
        internal string Magic { get { return magic; } set { magic = value; } }
        internal bool NameResolved { get; set; }
        internal string FileNameOnly { get { return FileName.Length > 0 ? Path.GetFileName(FileName) : ""; } }
        internal string FileNameOnlyForced { get { return FileName.Length > 0 ? Path.GetFileName(FileName) : hashText; } }
        internal string FileNameForced { get { return FileName.Length > 0 ? FileName : hashText; } }
        internal string ResXFileName { get; set; }

        internal FileExtraInfo(FileEntry entry)
        {
            this.entry = entry;
            this.hashText = entry.Hash.ToString("X8");
            UpdateLangText();
            if (this.language != FileLanguage.Unknown)
                this.langText = LangNames.Dict[language];
            else
                this.langText = string.Format("UNK_{0:X8}", entry.Raw.LangCode);
            this.AbsOffset = entry.Raw.Location; //xx checkthis: *0x800;
            UInt32 loc = (entry.Raw.Location / 0x800) * 0x800;

            this.offset = loc % 0x7FF00000;
            this.bigFileIndex = entry.Raw.Location & 0x0F;
            if (this.bigFileIndex > 0)
                Debug.WriteLine("test");
            this.bigfileName = string.Format(entry.Parent.FilePattern, bigFileIndex);
            this.bigfilePrefix = entry.Parent.FilePrefix;
        }

        private void UpdateLangText()
        {
            switch (entry.Raw.LangCode & 0xFFFF)
            {
                case 0xDD41:
                case 0x1101:
                    { language = FileLanguage.English; return; }
                case 0x1102:
                    { language = FileLanguage.French; return; }
                case 0x1104:
                    { language = FileLanguage.German; return; }
                case 0x1108:
                    { language = FileLanguage.Italian; return; }
                case 0x1110:
                    { language = FileLanguage.Spanish; return; }
                case 0x1140:
                    { language = FileLanguage.Portuguese; return; }
                case 0x1180:
                    { language = FileLanguage.Polish; return; }
                case 0x1300:
                    { language = FileLanguage.Russian; return; }
                case 0x1500:
                    { language = FileLanguage.Czech; return; }
                case 0x1900:
                    { language = FileLanguage.Dutch; return; }
                case 0x3100:
                    { language = FileLanguage.Arabic; return; }
                case 0x5100:
                    { language = FileLanguage.Korean; return; }
                case 0x9100:
                    { language = FileLanguage.Chinese; return; }
                case 0xFFFF:
                    { language = FileLanguage.NoLang; return; }
                default:
                    { language = FileLanguage.Unknown; return; } // throw new Exception(Errors.InvalidLanguageCode);
            }
        }
    }

    class FileEntry// : IComparable<FileEntry>
    {
        #region private variables
        private FilePool filePool = null;
        private UInt32 hash;
        private RawFileInfo raw;
        private FileExtraInfo extra = null;

        private FileEntryList parent = null;
        private Int32 originalIndex;
        #endregion

        internal UInt32 Hash { get { return hash; } }
        internal RawFileInfo Raw { get { return raw; } }
        internal FileExtraInfo Extra { get { if (extra == null) extra = new FileExtraInfo(this); return extra; } }

        internal FileEntryList Parent { get { return parent; } }
        internal Int32 OriginalIndex { get { return originalIndex; } }
        internal UInt32 VirtualSize = 0;

        internal bool Translatable = false; // it contains translatable text or data?

        internal FileEntry(UInt32 hash, RawFileInfo raw, Int32 originalIndex, FileEntryList parent, FilePool filePool)
        {
            this.hash = hash;
            this.raw = raw;
            this.originalIndex = originalIndex;
            this.parent = parent;
            this.filePool = filePool;
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
                    ret = extra.FileName.CompareTo(other.Extra.FileName);
                    break;
                case FileEntryCompareField.FileType:
                    ret = extra.FileType.CompareTo(other.Extra.FileType);
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
            FileStream fs = filePool.Open(this);
            try
            {
                fs.Seek(Extra.Offset, SeekOrigin.Begin);
                Int32 readLength = (maxLen >= 0) ? maxLen : (Int32)raw.Length;
                ret = new byte[readLength];
                fs.Read(ret, 0, readLength);
            }
            finally
            {
                filePool.Close(this);
            }
            return ret;
        }

        internal byte[] ReadContent(Int32 startPos, Int32 maxLen)
        {

            byte[] ret;
            FileStream fs = filePool.Open(this);
            try
            {
                fs.Seek(Extra.Offset + startPos, SeekOrigin.Begin);
                Int32 readLength = (maxLen >= 0) ? maxLen : (Int32)raw.Length - startPos;
                ret = new byte[readLength];
                fs.Read(ret, 0, readLength);
            }
            finally
            {
                filePool.Close(this);
            }
            return ret;
        }

        internal Int32 ReadInt32(Int32 startPos)
        {
            FileStream fs = filePool.Open(this);
            try
            {
                fs.Seek(Extra.Offset + startPos, SeekOrigin.Begin);
                byte[] buf = new byte[4];
                fs.Read(buf, 0, 4);

                return BitConverter.ToInt32(buf, 0);
            }
            finally
            {
                filePool.Close(this);
            }
        }

        internal UInt32 ReadUInt32(Int32 startPos)
        {
            FileStream fs = filePool.Open(this);
            try
            {
                fs.Seek(Extra.Offset + startPos, SeekOrigin.Begin);
                byte[] buf = new byte[4];
                fs.Read(buf, 0, 4);

                return BitConverter.ToUInt32(buf, 0);
            }
            finally
            {
                filePool.Close(this);
            }
        }
        internal uint CopyContentToStream(Stream str, UInt32 startPos = 0, UInt32 maxLen = 0)
        {
            uint readLength = (maxLen > 0) ? maxLen : raw.Length - startPos;
            uint bufSize = 1024 * 1024;
            byte[] buf = new byte[bufSize + 1];
            FileStream fs = filePool.Open(this);
            try
            {
                int read = 0;
                fs.Position = Extra.Offset + startPos;

                while (read < readLength)
                {
                    int readBytes = fs.Read(buf, 0, (int)(Math.Min(readLength - read, bufSize)));
                    read += readBytes;
                    str.Write(buf, 0, readBytes);
                }
                if (str.Length != readLength)
                    Log.LogDebugMsg("Steam copy error"); //xx translate?

            }
            finally
            {
                filePool.Close(this);
            }
            return readLength;
        }

        internal uint DumpToFile(string fileName)
        {
            uint ret = 0;
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            try
            {
                ret = CopyContentToStream(fs);
            }
            finally
            {
                fs.Close();
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
                        hash, originalIndex, raw.Location, raw.Length, content.Length, content.Length - raw.Length, extra.FileName);
                }
                finally
                {
                    fatWriter.Close();
                }
            }
#endif
            if (testWrite)
            {
                string fileName = Path.Combine(TRGameInfo.Game.WorkFolder,
                    ((Extra.FileName.Length > 0)
                        ? Extra.FileName.Replace(@"\", "[bs]")
                        : Extra.HashText));
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

                FileStream fs = filePool.Open(this);
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
                    filePool.Close(this);
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

    // Physical file
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

    internal class FilePool
    {
        #region private declarations
        private Dictionary<UInt32, FilePoolEntry> pool = new Dictionary<UInt32, FilePoolEntry>();
        private string pattern;
        #endregion

        internal FilePool(string pattern)
        {
            this.pattern = pattern;
        }

        internal FileStream Open(FileEntry entry)
        {
            FileStream fs = Open(entry.Extra.BigFileIndex);
            fs.Position = entry.Extra.Offset;
            return fs;
        }

        internal FileStream Open(UInt32 Index)
        {
            FilePoolEntry entry;

            if (!pool.ContainsKey(Index))
            {
                entry = new FilePoolEntry(string.Format(pattern, Index));
                pool.Add(Index, entry);
            }
            else
                entry = pool[Index];

            entry.Open();
            return entry.Stream;
        }

        internal void CloseAll()
        {
            foreach (UInt32 key in pool.Keys)
            {
                pool[key].CloseAll();
            }
            pool.Clear();
        }

        internal void Close(UInt32 Index)
        {
            pool[Index].Close();
        }

        internal void Close(FileEntry entry)
        {
            pool[entry.Extra.BigFileIndex].Close();
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

    class FileStructElement
    {
        public enum BigFileType { Main, Optional };

        private string name;
        private string folder;
        private string fileNameMask;
        private BigFileType fileType;
        private bool singleFile;
        private int fileCount;

        public string Name { get { return name; } set { name = value; } }
        public string Folder { get { return folder; } set { folder = value; } }
        public BigFileType FileType { get { return fileType; } set { fileType = value; } }

        public bool SingleFile { get { return singleFile; } set { singleFile = value; } }
        public string FileNameMask { get { return fileNameMask; } set { fileNameMask = value; } }
        public int FileCount { get { return fileCount; } set { fileCount = value; } }

        public string GetNthFileName(int i) { return string.Format(fileNameMask, i); }
        public string GetNthPullPath(int i) { return Path.Combine(folder, GetNthFileName(i)); }

    }

    class FileEntryList : List<FileEntry>
    {
        const UInt32 MaxFileEntryCount = 20000;
        const UInt32 MaxFileSize = 30000000;
        #region private declarations
        private Int32 entryCount;
        private FilePool filePool;
        string filePattern;
        string filePrefix;
        #endregion

        private List<FileEntryCompareField> compareFields =
                new List<FileEntryCompareField>(3) { FileEntryCompareField.Location };

        internal Int32 EntryCount { get { return entryCount; } }
        internal List<FileEntryCompareField> CompareFields { get { return compareFields; } }
        internal string FilePattern { get { return filePattern; } }
        internal string FilePrefix { get { return filePrefix; } }

        internal bool simulateWrite = false;
        private BackgroundWorker worker;

        internal FileEntryList(BackgroundWorker worker, string filePattern, string filePrefix)
        {
            this.worker = worker;
            this.filePattern = filePattern;
            string prefix = string.Format(filePattern, 0);
            string tmpPrefix;
            do
            {
                tmpPrefix = prefix;
                prefix = Path.GetFileNameWithoutExtension(tmpPrefix);

            } while (tmpPrefix != prefix);
            this.filePrefix = prefix;

            this.filePool = new FilePool(this.filePattern);
        }

        internal Dictionary<uint, string> LoadFileNamesFile(string fileName)
        {
            Dictionary<uint, string> dict = new Dictionary<uint, string>();
            if (File.Exists(fileName))
            {
                TextReader rdr = new StreamReader(fileName);

                string line;
                while ((line = rdr.ReadLine()) != null)
                    dict.Add((uint)(Hash.MakeFileNameHash(line)), line);
            }
            return dict;
        }

        private void LoadPathAliasesFile(string fileName, out Dictionary<int, string> folderAlias, out Dictionary<int, string> fileNameAlias)
        {
            folderAlias = new Dictionary<int, string>();
            fileNameAlias = new Dictionary<int, string>();
            if (File.Exists(fileName))
            {
                TextReader rdr = new StreamReader(fileName);

                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    string[] elements = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (elements.Length == 3)
                    {
                        if (elements[0] == "S")
                            folderAlias.Add(elements[1].GetHashCode(), elements[2]);
                        else
                            fileNameAlias.Add(elements[1].GetHashCode(), elements[2]);
                    }
                    else
                        if (elements.Length != 0)
                        {
                            throw new Exception(string.Format("Invalid path alias entry: \"{0}\"", line));
                        }
                }
            }
        }

        internal void ReadFAT()
        {
            byte[] buf = null;

            Dictionary<uint, string> hashFileNames = LoadFileNamesFile(Path.Combine(TRGameInfo.Game.WorkFolder, string.Format("filelist.txt", filePrefix)));
            Dictionary<int, string> folderAliases, fileNameAliases;
            LoadPathAliasesFile(Path.Combine(TRGameInfo.Game.WorkFolder, "path_aliases.txt"), out folderAliases, out fileNameAliases);
            string fileName = string.Format(filePattern, 0);
            string fileNameOnly = Path.GetFileName(fileName);
            FileStream fs = filePool.Open(0);
            BinaryReader br = new BinaryReader(fs);
            try
            {
                fs.Position = 0x0C;
                entryCount = br.ReadInt32();
                if (/*entryCount == 0 ||*/ entryCount > MaxFileEntryCount)
                    throw new Exception(string.Format(Errors.FATEntryCountError, entryCount));
                buf = new byte[entryCount * RawFileInfo.Size];
                fs.Position = 0x34;
                fs.Read(buf, 0, buf.Length);
                Log.LogMsg(LogEntryType.Debug, string.Format("ReatFat: {0} entry count: {1}", fileNameOnly, entryCount));
            }
            finally
            {
                filePool.Close(0);
            }
            if (entryCount > 0)
            {
                RawFileInfo[] fileInfo = new RawFileInfo[entryCount];

                // fill hashTable & fileinfo table;
                // **hashes[i] = BitConverter.ToUInt32(buf, i * 4);
                TextWriter twEntriesByOrigOrder = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, filePrefix + "__fat_entries_raw.txt"));
                twEntriesByOrigOrder.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8}", "hash", "lang", "length", "location"));
                for (Int32 i = 0; i < entryCount; i++)
                {
                    // fileInfo[i].ToRawFileInfo(buf, RawFileInfo.Size * i);

                    int startIndex = RawFileInfo.Size * i;

                    fileInfo[i].Hash = BitConverter.ToUInt32(buf, startIndex + 0x00);
                    fileInfo[i].LangCode = BitConverter.ToUInt32(buf, startIndex + 0x04);
                    fileInfo[i].Length = BitConverter.ToUInt32(buf, startIndex + 0x08);
                    fileInfo[i].Location = BitConverter.ToUInt32(buf, startIndex + 0x0C);

                    RawFileInfo raw = fileInfo[i];
                    //if (ofs < 0)
                    //    raw.Location = checked((UInt32)(Location + ofs));

                    //if ((fileInfo[i].Location & 0x000000FF) == 3)
                    //    fileInfo[i].Location -= 0xDEDB003;
                    twEntriesByOrigOrder.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8}",
                        raw.Hash,
                        raw.LangCode,
                        raw.Length,
                        raw.Location
                        ));
                }
                twEntriesByOrigOrder.Close();

                Clear();
                // assign file name from hash code

                for (Int32 i = 0; i < entryCount; i++)
                {
                    FileEntry entry = new FileEntry(fileInfo[i].Hash, fileInfo[i], i, this, filePool);
                    Add(entry);
                    string matchedFileName = string.Empty;
                    entry.Extra.NameResolved = hashFileNames.TryGetValue(entry.Hash, out matchedFileName);
                    entry.Extra.ResXFileName = string.Empty;
                    if (entry.Extra.NameResolved)
                    {
                        entry.Extra.FileName = matchedFileName;
                    }
                }

                // calculate virtual sizes
                SortBy(FileEntryCompareField.Location);
                for (Int32 i = 0; i <= entryCount - 2; i++)
                {
                    this[i].VirtualSize = (this[i + 1].Raw.Location - this[i].Raw.Location) * 0x800;
                    if (this[i].VirtualSize == 0)
                    {
                        // if (i == entryCount - 2)
                        //     ;//                        throw new Exception(string.Format(DebugErrors.FATParseError, this[i].Hash));
                        // else
                        this[i].VirtualSize = (this[i + 2].Raw.Location - this[i].Raw.Location) * 0x800;
                    }
                }
                this[entryCount - 1].VirtualSize = (uint)Boundary.Extend((int)(this[entryCount - 1].Raw.Length), 0x800);

                // add stored infos
                SortBy(FileEntryCompareField.Hash);
                //FileStoredInfoList infoList = new FileStoredInfoList();
                /**/
                // determine file type
                /*
                 * // unnecessary
                if ( File.Exists(".\\" + TRGameInfo.Game.Name + ".files.txt"))
                {
                    infoList.LoadFromFile(".\\" + TRGameInfo.Game.Name + ".files.txt");
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

                TextWriter twEntries = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, filePrefix + "__fat_entries.txt"));
                twEntries.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8} | {4,-8} {5} {6} {7,-8} {8} {9} {10} {11}", "hash", "location", "length", "lang", "filetype", "language", "magic", "offset", "bfindex", "origidx", "langmask", "filename"));
                SortBy(FileEntryCompareField.Location);
                for (Int32 i = 0; i < entryCount; i++)
                {
                    FileEntry entry = this[i];
                    entry.Extra.FileType = FileTypeEnum.Unknown;
                    List<string> specialFiles = new List<string>(new string[] {
                        "489CD608", // pc-w\symbol.ids
                        "9809A8EE", // pc-w\objectlist.txt
                        "97836E8F", // pc-w\objlist.dat
                        "F36C0FB8", // pc-w\unitlist.txt
                        "478596A2", // pc-w\padshock\padshocklib.tfb
                        "0A7B8340", // ??

                    });

                    if (specialFiles.Contains(entry.Extra.HashText))
                        entry.Extra.FileType = FileTypeEnum.Special;
                    else
                        if (entry.Extra.HashText == "7CD333D3") // pc-w\local\locals.bin
                        {
                            entry.Extra.FileType = FileTypeEnum.BIN_MNU;
                            if (entry.Raw.Language == FileLanguage.English)
                                entry.Translatable = true;

                            //Log.LogMsg(LogEntryType.Debug, string.Format("ReatFat: {0} locals.bin ({1}) found ", Path.GetFileName(fileName), fileEntry.Extra.LangText));
                        }
                        else
                        {
                            #region Filetype detection
                            byte[] bufMagic = entry.ReadContent(4);

                            entry.Extra.Magic = Encoding.Default.GetString(bufMagic);
                            switch (entry.Extra.Magic)
                            {
                                case "CDRM":
                                    entry.Extra.FileType = FileTypeEnum.CDRM;
                                    break;
                                case "!WAR":
                                    entry.Extra.FileType = FileTypeEnum.RAW;
                                    break;
                                case "FSB4":
                                    entry.Extra.FileType = FileTypeEnum.FSB4;
                                    break;
                                case "MUS!":
                                    entry.Extra.FileType = FileTypeEnum.MUS;
                                    break;
                                case "Vers":
                                    entry.Extra.FileType = FileTypeEnum.SCH;
                                    break;
                                case "PCD9":
                                    entry.Extra.FileType = FileTypeEnum.PCD9;
                                    break;
                                case "\x16\x00\x00\x00":
                                    // MUL file test
                                    entry.Extra.FileType = FileTypeEnum.DRM;
                                    if (entry.Raw.Length > 8)
                                    {
                                        bufMagic = entry.ReadContent(4, 4);
                                        //int magicInt = BitConverter.ToInt32(bufMagic, 0);
                                        //if (magicInt == -1 || magicInt == 0)
                                        //{
                                        //    entry.Extra.FileType = FileTypeEnum.MUL2;
                                        //}
                                    }
                                    break;
                                default:
                                    entry.Extra.FileType = FileTypeEnum.Unknown;
                                    if (entry.Raw.Length > 0x814)
                                        if (Encoding.ASCII.GetString(entry.ReadContent(0x810, 4)) == "ENIC")
                                            entry.Extra.FileType = FileTypeEnum.MUL_CIN;

                                    if (entry.Extra.FileType == FileTypeEnum.Unknown && entry.Raw.Length > 0x2014)
                                        if (Encoding.ASCII.GetString(entry.ReadContent(0x2010, 4)) == "ENIC")
                                            entry.Extra.FileType = FileTypeEnum.MUL_CIN;

                                    if (entry.Extra.FileType == FileTypeEnum.Unknown)
                                    {
                                        UInt32 m = entry.ReadUInt32(0);
                                        if (m == 0xBB80 || m == 0xAC44)
                                            entry.Extra.FileType = FileTypeEnum.MUL2;
                                        else
                                            Debug.WriteLine("what?");
                                    }
                                    break;
                            }
                            #endregion
                            if (entry.Extra.FileType == FileTypeEnum.MUL_CIN || entry.Extra.FileType == FileTypeEnum.RAW_FNT || entry.Extra.FileType == FileTypeEnum.SCH)
                                entry.Translatable = true;
                        }

                    //if (entry.Extra.HashText == "3533B8DF" && entry.Extra.BigFilePrefix == "patch")
                    //    Debug.WriteLine("stop here!");

                    #region Search filename or path alias
                    if (entry.Extra.NameResolved && entry.Translatable)
                    {
                        string path = Path.GetDirectoryName(entry.Extra.FileName);
                        int pathHash = path.GetHashCode();
                        string alias;

                        if (fileNameAliases.TryGetValue(pathHash, out alias))
                            entry.Extra.ResXFileName = alias;
                        else
                            if (folderAliases.TryGetValue(pathHash, out alias))
                                entry.Extra.ResXFileName = Path.ChangeExtension(entry.Extra.FileName.Replace(path, alias), ".resx");

                    }
                    if (entry.Extra.ResXFileName == string.Empty)
                        entry.Extra.ResXFileName = entry.Extra.HashText + ".resx";
                    #endregion

                    bool dumpIt = entry.Extra.FileType == FileTypeEnum.Unknown; // && entry.Raw.Length <= 68; // fileEntry.Translatable; //false //xx

                    //if (fileEntry.Raw.Language == FileLanguage.NoLang || fileEntry.Raw.Language == FileLanguage.Unknown || fileEntry.Raw.Language == FileLanguage.English)
                    //{
                    //    dumpIt = true; // fileEntry.Extra.FileType == FileTypeEnum.MUL_CIN;
                    //}
                    //if (dumpIt)
                    {

                        twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8} | {4,-8} {5} \"{6}\" {7:X8} {8:D3} {9:D6} {10} {11}",
                            entry.Raw.Hash,
                            entry.Raw.Location,
                            entry.Raw.Length,
                            entry.Raw.LangCode,
                            entry.Extra.FileType.ToString(),
                            entry.Raw.Language.ToString(),
                            entry.Extra.Magic,
                            entry.Extra.Offset,
                            entry.Extra.BigFileIndex,
                            entry.OriginalIndex,
                            Convert.ToString(entry.Raw.LangCode, 2),
                            entry.Extra.FileNameForced));
                    }

                    if (dumpIt) //(((fileEntry.Extra.FileType & (/*FileTypeEnum.RAW_FNT | FileTypeEnum.MUL_CIN |*/ FileTypeEnum.SCH)) != 0))
                    {
                        byte[] content = entry.ReadContent();
                        string extractFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "raw",
                                string.Format("{0}.{1}.{2}.txt", entry.Extra.BigFilePrefix, entry.Extra.FileNameOnlyForced, entry.Extra.LangText));
                        Directory.CreateDirectory(Path.GetDirectoryName(extractFileName));
                        FileStream fx = new FileStream(extractFileName, FileMode.Create, FileAccess.ReadWrite);
                        fx.Write(content, 0, content.Length);
                        fx.Close();
                    }


                }
                twEntries.Close();
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
                switch (entry.Extra.FileType)
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
            doc.Save(".\\" + TRGameInfo.Game.Name + ".res.xml");
            worker.ReportProgress(100, StaticTexts.creatingRestorationPointDone);
        }

        internal void GenerateFilesTxt()
        {
            Int32 lastReported = 0;
            worker.ReportProgress(lastReported, StaticTexts.creatingFilesTxt);
            ReadFAT();

            if (!Directory.Exists(TRGameInfo.Game.WorkFolder))
                Directory.CreateDirectory(TRGameInfo.Game.WorkFolder);
            TextWriter twEntries = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "fat.entries.txt"));
            twEntries.WriteLine("hash  location  length  lang  offset ");
            for (Int32 i = 0; i < this.Count; i++)
            {
                twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8} {4:X8}", this[i].Hash, this[i].Raw.Location, this[i].Raw.Length, this[i].Raw.LangCode, this[i].Extra.Offset));
            }
            twEntries.Close();

            SortBy(FileEntryCompareField.Location);
            TextWriter tw = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "custom.files.txt"));

            for (Int32 i = 0; i < this.Count; i++)
            {

                FileEntry entry = this[i];
                string magic = string.Empty;
                bool writeIt = false;

                if (entry.Raw.Language == FileLanguage.NoLang || entry.Raw.Language == FileLanguage.Unknown || entry.Raw.Language == FileLanguage.English)
                {
                    writeIt = true; // entry.Extra.FileType == FileTypeEnum.MUL_CIN;
                }
                if (writeIt)
                    tw.WriteLine(string.Format("{0:X8}\t{1:X8}\t{2}", entry.Hash, entry.Hash, entry.Extra.FileType.ToString(), entry.Raw.Language.ToString()));

            }
            tw.Close();
        }

        private static int compareByPathLength(string file1, string file2)
        {
            string path1 = Path.GetDirectoryName(file1);
            string path2 = Path.GetDirectoryName(file2);

            int compareRes = path1.CompareTo(path2);

            if (compareRes == 0)
                compareRes = string.Compare(file1, path1.Length, file2, path2.Length, int.MaxValue);

            return compareRes;

        }

        internal void Extract(string destFolder, bool useDict)
        {
            string extractFolder = Path.Combine(destFolder, filePrefix);

            if (Directory.Exists(extractFolder))
            {
                List<string> delFiles = new List<string>(Directory.GetFiles(extractFolder, "*.resx", SearchOption.AllDirectories));
                delFiles.Sort(compareByPathLength);
                List<string> delFolders = new List<string>();
                foreach (string file in delFiles)
                {
                    File.Delete(file);
                    string filePath = Path.GetDirectoryName(file);
                    if (delFolders.IndexOf(filePath) == -1)
                        delFolders.Add(filePath);
                }
                Log.LogDebugMsg(string.Format("delfolders: {0}\r\n{1}", extractFolder, string.Join("\r\n", delFolders.ToArray())));

                Log.LogDebugMsg("/delfolders\r\n");
                delFolders = new List<string>(Directory.GetDirectories(extractFolder, "*.*", SearchOption.AllDirectories));
                delFolders.Reverse();

                foreach (string folder in delFolders)
                    if (Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0)
                        try
                        {
                            Directory.Delete(folder);
                        }
                        catch { }
            }


            if (!Directory.Exists(extractFolder))
                Directory.CreateDirectory(extractFolder);

            ReadFAT();
            SortBy(FileEntryCompareField.Location);

            //            Int32 lastBF = -1;
            TextWriter cineWriter = null;
            TextWriter menuWriter = null;

            try
            {
                SortBy(FileEntryCompareField.FileName);
                foreach (FileEntry entry in this)
                {
                    switch (entry.Extra.FileType)
                    {
                        case FileTypeEnum.MUL_CIN:
                            {
                                if (entry.Raw.Language == FileLanguage.English || entry.Raw.Language == FileLanguage.NoLang)
                                {
                                    //break; //xxbreak
                                    CineFile.Extract(extractFolder, entry, useDict);
                                }
                                break;
                            }
                        case FileTypeEnum.BIN_MNU:
                            {
                                if (entry.Raw.Language == FileLanguage.English)
                                {
                                    MenuFile menu = new MenuFile(entry);
                                    menu.Extract(extractFolder, useDict);
                                }
                                break;
                            }
                        case FileTypeEnum.RAW_FNT:
                            {
                                byte[] buf = entry.ReadContent();
                                FileStream fs = new FileStream(Path.Combine(extractFolder, filePrefix + "_font_original.raw"), FileMode.Create);
                                try
                                {
                                    fs.Write(buf, 0, buf.Length);
                                }
                                finally
                                {
                                    fs.Close();
                                }
                                break;
                            }
                        case FileTypeEnum.SCH:
                            {
                                MovieFile movie = new MovieFile(entry);
                                movie.Extract(extractFolder, useDict);
                                break;
                            }
                    }
                }
                ResXPool.CloseAll();
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
                switch (entry.Extra.FileType)
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
            filePool.CloseAll();
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
                switch (entry.Extra.FileType)
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
            filePool.CloseAll();
        }

        internal void UpdateFATEntry(FileEntry entry)
        {
            FileStream fs = filePool.Open(0); // entry.Extra.BigFileName
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
                filePool.Close(0); // entry.Extra.BigFileName
            }
        }

        internal bool SortBy(FileEntryCompareField field)
        {
            bool needReSort = compareFields.Count == 0;
            if (!needReSort)
                needReSort = compareFields[0] == field;
            if (true || needReSort)
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
