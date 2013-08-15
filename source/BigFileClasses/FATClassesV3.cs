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
using System.Linq;

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
    }

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

    //[Flags]
    //enum FileNameResolveStatus
    //{
    //    Unknown = 0,
    //    Resolved = 1,
    //    NotResolved = 2
    //}

    //enum BoundaryDirection
    //{
    //    Up = 1,
    //    Down = 0
    //}

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

    #endregion

    // calculated, converted & stored data about files in bigfiles
    class FileExtraInfo
    {
        internal static readonly UInt32 BIGFILE_BOUNDARY = 0x7FF00000;
        //const uint BLOCK_COUNT_IN_BIGFILES = 0x12C00; // TRA/TRL v1.0.0.6
        //const uint BLOCK_COUNT_IN_BIGFILES = 0xFFE00;  // TRU/LCGOL 0x7FF00000 / 0x800

        #region private declarations
        private string langText;
        private string hashText;
        private Int32 bigFileIndex;
        private string bigfileName;
        private string bigfilePrefix;
        private Int64 offset;
        private Int64 absOffset;
        private byte[] data = null;
        private string text = string.Empty;
        private FileEntry entry;
        private string fileName = string.Empty;
        private FileTypeEnum fileType = FileTypeEnum.Unknown;
        private string magic = string.Empty;
        #endregion

        internal string HashText { get { return hashText; } }
        internal string LangText { get { return langText; } }
        internal FileLanguage Language { get { return entry.Raw.Language; } }
        internal Int64 Offset { get { return offset; } set { offset = value; } }
        internal Int64 AbsOffset { get { return absOffset; } set { absOffset = value; } }
        internal byte[] Data { get { return data; } set { data = value; } }
        internal string Text { get { return text; } set { text = value; } }
        internal Int32 BigFileIndex { get { return bigFileIndex; } set { bigFileIndex = value; } }
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
            if (this.Language != FileLanguage.Unknown)
                this.langText = Language.ToString();
            else
                this.langText = string.Format("UNK_{0:X8}", entry.Raw.LangCode);
            this.AbsOffset = entry.Raw.Location; //xx checkthis: *0x800;
            UInt32 loc = (entry.Raw.Location / 0x800) * 0x800;

            this.offset = loc % BIGFILE_BOUNDARY;
            this.bigFileIndex = (int)(entry.Raw.Location & 0x0F);
        }
    }

    class FileEntry// : IComparable<FileEntry>
    {
        #region private variables
        private BigFile bigFile;
        //        private BigFilePool filePool = null;
        private UInt32 hash;
        private RawFileInfo raw;
        private FileExtraInfo extra = null;

        private FileEntryList parent = null;
        private Int32 originalIndex;

        private BigFilePool filePool { get { return bigFile.Parent.FilePool; } }
        #endregion

        internal UInt32 Hash { get { return hash; } }
        internal RawFileInfo Raw { get { return raw; } }
        internal FileExtraInfo Extra { get { return extra; } }

        internal FileEntryList Parent { get { return parent; } }
        internal Int32 OriginalIndex { get { return originalIndex; } }
        internal UInt32 VirtualSize = 0;

        internal bool Translatable = false; // it contains translatable text or data?
        internal BigFile BigFile { get { return bigFile; } }

        // ctor
        internal FileEntry(RawFileInfo raw, Int32 originalIndex, FileEntryList parent, BigFile bigFile)
        {
            this.hash = raw.Hash;
            this.raw = raw;
            this.originalIndex = originalIndex;
            this.parent = parent;
            this.bigFile = bigFile;
            this.extra = new FileExtraInfo(this);
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

        internal void WriteContent(byte[] content, bool updateFAT = false)
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
                bool allowEnlarge = true;
                if (extra.Offset + content.Length > fs.Length)
                {
                    if (!allowEnlarge)
                    {
                        Exception ex = new Exception(Errors.NewFileIsTooBig);
                        ex.Data.Add("bigfile", Extra.BigFileName);
                        ex.Data.Add("hash", Extra.HashText);
                        ex.Data.Add("diff", (content.Length - VirtualSize).ToString() + " byte(s)");
                        throw ex;
                    }
                    else
                    {
                        if (!parent.simulateWrite)
                        {
                            const int BUF_LEN = 1024 * 512;

                            byte[] buf = new byte[BUF_LEN];

                            // go to end of file
                            fs.Position = fs.Seek(0, SeekOrigin.End);
                            // fill gap with zeros
                            while (fs.Length < extra.Offset)
                            {
                                int grow = (extra.Offset > fs.Length + BUF_LEN) ? BUF_LEN : (int)(extra.Offset - fs.Length);
                                fs.Write(buf, 0, grow);
                            }
                        }
                    }
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

                    if (fsLen != fs.Length && !allowEnlarge)
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

        internal void CloseAll()
        {
            if (Stream != null)
            {
                FileStream fs = Stream;
                Stream = null;
                fs.Close();
            }
            count = 0;
        }

        internal FilePoolEntry(string fileName)
        {
            this.fileName = fileName;
        }
    }

    internal class BigFilePool
    {
        #region private declarations
        private Dictionary<string, FilePoolEntry> pool = new Dictionary<string, FilePoolEntry>();
        private BigFileList bigFiles;
        #endregion

        internal BigFilePool(BigFileList bigFiles)
        {
            this.bigFiles = bigFiles;
        }

        internal static string CreateKey(string name, int index)
        {
            return string.Format("{0}.{1:D3}", name, index);
        }

        internal static string CreateKey(FileEntry entry)
        {
            return CreateKey(entry.BigFile.Name, entry.Extra.BigFileIndex);
        }

        internal FileStream Open(FileEntry entry)
        {
            return Open(entry.BigFile.Name, entry.Extra.BigFileIndex);
        }

        internal FileStream Open(string name, int index)
        {

            FilePoolEntry entry;
            string key = CreateKey(name, index);

            if (!pool.ContainsKey(key))
            {
                BigFile bigFile = null;
                if (!bigFiles.ItemsByName.TryGetValue(name, out bigFile))
                {
                    bigFile = null;
                }
                entry = new FilePoolEntry(string.Format(bigFile.FilePatternFull, index));
                pool.Add(key, entry);
            }
            else
                entry = pool[key];

            entry.Open();
            return entry.Stream;
        }


        internal void CloseAll()
        {
            foreach (string key in pool.Keys)
                pool[key].CloseAll();
            pool.Clear();
        }

        internal void Close(string name, int index)
        {
            Close(CreateKey(name, index));
        }

        internal void Close(FileEntry entry)
        {
            Close(CreateKey(entry));
        }

        internal void Close(string key)
        {
            pool[key].Close();
        }
    }

    class FileEntryList : List<FileEntry>
    {
        const UInt32 MaxFileEntryCount = 20000;
        const UInt32 MaxFileSize = 30000000;
        const Int32 FATEntriesStartOfs = 0x34;

        #region private declarations
        private Int32 entryCount;
        private string filePattern;
        private string filePrefix;
        #endregion

        private List<FileEntryCompareField> compareFields =
                new List<FileEntryCompareField>(3) { FileEntryCompareField.Location };

        internal Int32 EntryCount { get { return entryCount; } }

        internal List<FileEntryCompareField> CompareFields { get { return compareFields; } }
        internal string FilePattern { get { return filePattern; } }
        internal string FilePrefix { get { return filePrefix; } }

        internal bool simulateWrite = false;
        private BackgroundWorker worker;

        internal FileEntryList(BigFile parent)
        {

        }

        internal void ReadFAT_old()
        {
            //byte[] buf = null;

            //// loading filelist for hash - filename resolution
            //string fileNameFileList = Path.Combine(TRGameInfo.Game.WorkFolder, "filelist.txt");
            //Dictionary<uint, string> hashFileNames = new Dictionary<uint, string>();
            //LoadFileNamesFile(fileNameFileList, ref hashFileNames);

            //string fileNamePathAliases = Path.Combine(TRGameInfo.Game.WorkFolder, "path_aliases.txt");
            //Dictionary<int, string> folderAliases, fileNameAliases;
            //LoadPathAliasesFile(fileNamePathAliases, out folderAliases, out fileNameAliases);
            //string fileName = string.Format(filePattern, 0);
            //string fileNameOnly = Path.GetFileName(fileName);
            //FileStream fs = filePool.Open("", 0);
            //BinaryReader br = new BinaryReader(fs);
            //try
            //{
            //    // read header
            //    magic = br.ReadBytes(4);
            //    version = br.ReadUInt32();
            //    priority = br.ReadUInt32();
            //    entryCount = br.ReadInt32();
            //    unknown1 = br.ReadUInt32();
            //    pathPrefix = br.ReadBytes(0x20);
            //    headerSize = (UInt32)(fs.Position);

            //    buf = new byte[entryCount * RawFileInfo.Size];

            //    fs.Read(buf, 0, buf.Length);
            //    Log.LogMsg(LogEntryType.Debug, string.Format("ReatFat: {0} entry count: {1} priority {2}", fileNameOnly, entryCount, priority));
            //}
            //finally
            //{
            //    filePool.Close("", 0);
            //}
            //if (entryCount > 0)
            //{
            //    RawFileInfo[] fileInfo = new RawFileInfo[entryCount];

            //    Clear();

            //    for (Int32 i = 0; i < entryCount; i++)
            //    {
            //        // fileInfo[i].ToRawFileInfo(buf, RawFileInfo.Size * i);

            //        int startIndex = RawFileInfo.Size * i;

            //        fileInfo[i].Hash = BitConverter.ToUInt32(buf, startIndex + 0x00);
            //        fileInfo[i].LangCode = BitConverter.ToUInt32(buf, startIndex + 0x04);
            //        fileInfo[i].Length = BitConverter.ToUInt32(buf, startIndex + 0x08);
            //        fileInfo[i].Location = BitConverter.ToUInt32(buf, startIndex + 0x0C);

            //        RawFileInfo raw = fileInfo[i];

            //        FileEntry entry = new FileEntry(fileInfo[i].Hash, fileInfo[i], i, this, filePool);
            //        Add(entry);

            //        // try assign file name from hash code
            //        string matchedFileName = string.Empty;
            //        entry.Extra.NameResolved = hashFileNames.TryGetValue(entry.Hash, out matchedFileName);

            //        entry.Extra.ResXFileName = string.Empty;
            //        if (entry.Extra.NameResolved)
            //        {
            //            entry.Extra.FileName = matchedFileName;
            //        }
            //    }

            //    // raw dump of fat entries
            //    //TextWriter twEntriesByOrigOrder = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, filePrefix + "__fat_entries_raw.txt"));
            //    //twEntriesByOrigOrder.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8}", "hash", "lang", "length", "location"));
            //    //foreach (FileEntry entry in this)
            //    //    twEntriesByOrigOrder.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8}", entry.Raw.Hash, entry.Raw.LangCode, entry.Raw.Length, entry.Raw.Location));
            //    //twEntriesByOrigOrder.Close();

            //    // calculate virtual sizes
            //    SortBy(FileEntryCompareField.Location);
            //    for (Int32 i = 0; i <= entryCount - 2; i++)
            //    {
            //        this[i].VirtualSize = (this[i + 1].Raw.Location - this[i].Raw.Location) * 0x800;
            //        if (this[i].VirtualSize == 0)
            //            this[i].VirtualSize = (this[i + 2].Raw.Location - this[i].Raw.Location) * 0x800;
            //    }
            //    this[entryCount - 1].VirtualSize = (uint)Boundary.Extend((int)(this[entryCount - 1].Raw.Length), 0x800);

            //    SortBy(FileEntryCompareField.Location);


            //    for (Int32 i = 0; i < entryCount; i++)
            //    {
            //        FileEntry entry = this[i];
            //        entry.Extra.FileType = FileTypeEnum.Unknown;
            //        List<string> specialFiles = new List<string>(new string[] {
            //            "489CD608", // pc-w\symbol.ids
            //            "9809A8EE", // pc-w\objectlist.txt
            //            "97836E8F", // pc-w\objlist.dat
            //            "F36C0FB8", // pc-w\unitlist.txt
            //            "478596A2", // pc-w\padshock\padshocklib.tfb
            //            "0A7B8340", // ??

            //        });

            //        if (specialFiles.Contains(entry.Extra.HashText))
            //            entry.Extra.FileType = FileTypeEnum.Special;
            //        else
            //            if (entry.Extra.HashText == "7CD333D3") // pc-w\local\locals.bin
            //            {
            //                entry.Extra.FileType = FileTypeEnum.BIN_MNU;
            //                if (entry.Raw.Language == FileLanguage.English)
            //                    entry.Translatable = true;
            //            }
            //            else
            //            {
            //                #region Filetype detection
            //                byte[] bufMagic = entry.ReadContent(4);

            //                entry.Extra.Magic = Encoding.Default.GetString(bufMagic);
            //                switch (entry.Extra.Magic)
            //                {
            //                    case "CDRM":
            //                        entry.Extra.FileType = FileTypeEnum.CDRM;
            //                        break;
            //                    case "!WAR":
            //                        entry.Extra.FileType = FileTypeEnum.RAW;
            //                        break;
            //                    case "FSB4":
            //                        entry.Extra.FileType = FileTypeEnum.FSB4;
            //                        break;
            //                    case "MUS!":
            //                        entry.Extra.FileType = FileTypeEnum.MUS;
            //                        break;
            //                    case "Vers":
            //                        entry.Extra.FileType = FileTypeEnum.SCH;
            //                        break;
            //                    case "PCD9":
            //                        entry.Extra.FileType = FileTypeEnum.PCD9;
            //                        break;
            //                    case "\x16\x00\x00\x00":
            //                        entry.Extra.FileType = FileTypeEnum.DRM;
            //                        break;
            //                    default:
            //                        entry.Extra.FileType = FileTypeEnum.Unknown;
            //                        if (entry.Raw.Length > 0x814)
            //                            if (Encoding.ASCII.GetString(entry.ReadContent(0x810, 4)) == "ENIC")
            //                            {
            //                                entry.Extra.FileType = FileTypeEnum.MUL_CIN;
            //                                break;
            //                            }

            //                        if (entry.Raw.Length > 0x2014)
            //                            if (Encoding.ASCII.GetString(entry.ReadContent(0x2010, 4)) == "ENIC")
            //                            {
            //                                entry.Extra.FileType = FileTypeEnum.MUL_CIN;
            //                                break;
            //                            }

            //                        if (entry.ReadInt32(4) == -1) // 0xFFFFFFFF
            //                        {
            //                            entry.Extra.FileType = FileTypeEnum.MUL2;
            //                            break;
            //                        }

            //                        UInt32 m = entry.ReadUInt32(0);
            //                        if (m == 0xBB80 || m == 0xAC44)
            //                        {
            //                            entry.Extra.FileType = FileTypeEnum.MUL2;
            //                            break;
            //                        }

            //                        if (entry.Extra.FileType == FileTypeEnum.Unknown)
            //                            Debug.WriteLine(string.Format("Unknown file type for: {0}", entry.Extra.FileNameForced));
            //                        break;
            //                }
            //                #endregion
            //                if (entry.Extra.FileType == FileTypeEnum.MUL_CIN || entry.Extra.FileType == FileTypeEnum.RAW_FNT || entry.Extra.FileType == FileTypeEnum.SCH)
            //                    entry.Translatable = true;
            //            }

            //        #region Search filename or path alias
            //        if (entry.Extra.NameResolved && entry.Translatable)
            //        {
            //            string path = Path.GetDirectoryName(entry.Extra.FileName);
            //            int pathHash = path.GetHashCode();
            //            string alias;

            //            if (fileNameAliases.TryGetValue(pathHash, out alias))
            //                entry.Extra.ResXFileName = alias;
            //            else
            //                if (folderAliases.TryGetValue(pathHash, out alias))
            //                    entry.Extra.ResXFileName = Path.ChangeExtension(entry.Extra.FileName.Replace(path, alias), ".resx");

            //        }
            //        if (entry.Extra.ResXFileName == string.Empty)
            //            entry.Extra.ResXFileName = entry.Extra.HashText + ".resx";
            //        #endregion

            //        bool debugDumpIt = false;
            //        // debugDumpIt = entry.Extra.FileType == FileTypeEnum.DRM && entry.Raw.Length < 1024;
            //        // debugDumpIt = entry.Extra.FileType == FileTypeEnum.BIN_MNU && entry.Extra.Language == FileLanguage.English;
            //        // debugDumpIt = fileEntry.Translatable;
            //        // debugDumpIt = fileEntry.Raw.Language == FileLanguage.NoLang || fileEntry.Raw.Language == FileLanguage.Unknown || fileEntry.Raw.Language == FileLanguage.English;
            //        // debugDumpIt = fileEntry.Extra.FileType == FileTypeEnum.MUL_CIN;
            //        // debugDumpIt = (entry.Extra.FileType == FileTypeEnum.BIN_MNU && entry.Extra.Language == FileLanguage.English) || entry.Extra.FileType == FileTypeEnum.Unknown; 
            //        // debugDumpIt = entry.Translatable;
            //        // debugDumpIt = entry.Extra.FileType == FileTypeEnum.DRM && entry.Raw.Length < 1024;

            //        if (debugDumpIt)
            //        {
            //            byte[] content = entry.ReadContent();
            //            string extractFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "raw",
            //                    string.Format("{0}.{1}.{2}.txt", entry.Extra.BigFilePrefix, entry.Extra.FileNameOnlyForced, entry.Extra.LangText));
            //            Directory.CreateDirectory(Path.GetDirectoryName(extractFileName));
            //            FileStream fx = new FileStream(extractFileName, FileMode.Create, FileAccess.ReadWrite);
            //            fx.Write(content, 0, content.Length);
            //            fx.Close();
            //        }
            //    }

            //    // dump file info
            //    //TextWriter twEntries = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, filePrefix + "__fat_entries.txt"));
            //    //twEntries.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8} | {4,-8} {5} {6} {7,-8} {8} {9} {10}",
            //    //    "hash", "location", "length", "lang", "filetype", "language", "magic", "offset", "bfindex", "origidx", "filename"));

            //    //foreach (FileEntry entry in this)
            //    //{
            //    //    twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8} | {4,-8} {5} \"{6}\" {7:X8} {8:D3} {9:D6} {10}",
            //    //        entry.Raw.Hash,
            //    //        entry.Raw.Location,
            //    //        entry.Raw.Length,
            //    //        entry.Raw.LangCode,
            //    //        entry.Extra.FileType.ToString(),
            //    //        entry.Raw.Language.ToString(),
            //    //        entry.Extra.Magic,
            //    //        entry.Extra.Offset,
            //    //        entry.Extra.BigFileIndex,
            //    //        entry.OriginalIndex,
            //    //        entry.Extra.FileNameForced));
            //    //}
            //    //twEntries.Close();
            //}
        }

        internal void CreateRestoration()
        {
            Int32 lastReportedProgress = 0;
            worker.ReportProgress(0, StaticTexts.creatingRestorationPoint);
            ReadFAT_old();
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
                if (percent > lastReportedProgress)
                {
                    worker.ReportProgress(percent, StaticTexts.translating);
                    lastReportedProgress = percent;
                }
            }
            doc.Save(".\\" + TRGameInfo.Game.Name + ".res.xml");
            worker.ReportProgress(100, StaticTexts.creatingRestorationPointDone);
        }

        internal void GenerateFilesTxt()
        {
            Int32 lastReported = 0;
            worker.ReportProgress(lastReported, StaticTexts.creatingFilesTxt);
            ReadFAT_old();

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

            ReadFAT_old();
            SortBy(FileEntryCompareField.Location);

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
            //Int32 lastReported = 0;
            //worker.ReportProgress(0, StaticTexts.restoring);
            //ReadFAT_old();
            //for (Int32 i = 0; i < this.Count; i++)
            //{
            //    FileEntry entry = this[i];
            //    switch (entry.Extra.FileType)
            //    {
            //        case FileTypeEnum.MUL_CIN:
            //            {
            //                if (entry.Raw.Language == FileLanguage.English)
            //                {
            //                    CineFile cine = new CineFile(entry);
            //                    cine.Restore();
            //                }
            //                break;
            //            }
            //        case FileTypeEnum.BIN_MNU:
            //            {
            //                if (entry.Raw.Language == FileLanguage.English)
            //                {
            //                    MenuFile menu = new MenuFile(entry);
            //                    menu.Restore();
            //                }
            //                break;
            //            }
            //        case FileTypeEnum.RAW_FNT:
            //            {
            //                FontFile font = new FontFile(entry);
            //                font.Restore();
            //                break;
            //            }
            //    } // switch
            //    // notify user about translation progress
            //    Int32 percent = i * 100 / this.Count;
            //    if (percent > lastReported)
            //    {
            //        worker.ReportProgress(percent, StaticTexts.restoring);
            //        lastReported = percent;
            //    }
            //}
            //worker.ReportProgress(100, StaticTexts.restorationDone);

            //filePool.CloseAll();
        }

        internal void Translate(bool simulated)
        {
            //Int32 lastReported = 0;
            //ReadFAT_old();
            //Log.LogProgress(StaticTexts.translating, 0);
            //for (Int32 i = 0; i < this.Count; i++)
            //{
            //    FileEntry entry = this[i];
            //    switch (entry.Extra.FileType)
            //    {
            //        case FileTypeEnum.MUL_CIN:
            //            {
            //                //if (entry.Raw.Language == FileLanguage.English || entry.Raw.Language == FileLanguage.NoLang)
            //                //{
            //                //    CineFile cine = new CineFile(entry);
            //                //    cine.Translate(simulated);
            //                //}
            //                break;
            //            }
            //        case FileTypeEnum.BIN_MNU:
            //            {
            //                if (entry.Raw.Language == FileLanguage.English)
            //                {
            //                    MenuFile menu = new MenuFile(entry);
            //                    menu.Translate(simulated);
            //                }
            //                break;
            //            }
            //        case FileTypeEnum.RAW_FNT:
            //            {
            //                //FontFile font = new FontFile(entry);
            //                //font.Translate(simulated);
            //                break;
            //            }
            //    } // switch

            //    // notify user about translation progress
            //    Int32 percent = i * 100 / this.Count;
            //    if (percent > lastReported)
            //    {
            //        Log.LogProgress(StaticTexts.translating, percent);
            //        lastReported = percent;
            //    }
            //}
            //Log.LogProgress(StaticTexts.translationDone, 100);
            //filePool.CloseAll();
        }

        internal void UpdateFATEntry(FileEntry entry)
        {
            //FileStream fs = filePool.Open(entry.BigFile.Name, 0); // entry.Extra.BigFileName
            //try
            //{
            //    Int32 offset = entry.OriginalIndex * RawFileInfo.Size + FATEntriesStartOfs;

            //    fs.Position = offset;
            //    byte[] readBuf = new byte[RawFileInfo.Size];
            //    fs.Read(readBuf, 0, RawFileInfo.Size);
            //    RawFileInfo info = new RawFileInfo();
            //    info.ToRawFileInfo(readBuf, 0);
            //    //if (entry.Raw.Location != info.Location)
            //    //    throw new Exception(String.Format(DebugErrors.FATUpdateLocationError, entry.Hash));

            //    fs.Position = offset;
            //    byte[] buf = new byte[RawFileInfo.Size];
            //    if (entry.Raw.Location >= FileExtraInfo.BIGFILE_BOUNDARY)
            //        throw new Exception(String.Format(DebugErrors.FATUpdateLocationError, entry.Hash));
            //    if (entry.Raw.Location >= FileExtraInfo.BIGFILE_BOUNDARY)
            //        throw new Exception(String.Format(DebugErrors.FATUpdateLocationError, entry.Hash));
            //    entry.Raw.GetBytes(buf, 0);
            //    if (!simulateWrite)
            //        fs.Write(buf, 0, buf.Length);
            //}
            //finally
            //{
            //    filePool.Close(entry.BigFile.Name, 0); // entry.Extra.BigFileName
            //}
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


    class BigFile : IComparable
    {
        #region private declarations
        private byte[] magic;
        private UInt32 version;
        private UInt32 fileCount;
        private Int32 entryCount;
        private UInt32 priority;
        private byte[] pathPrefix;
        private FileEntryList entryList;
        private UInt32 headerSize;
        private string filePattern;
        private string filePatternFull;

        private string relPath;
        private List<RawFileInfo> itemsByIndex;
        private List<RawFileInfo> itemsByLocation;
        //private Dictionary<UInt32, RawFileInfo> itemsByHash;
        private BigFileList parent;
        string name;
        #endregion

        internal byte[] Magic { get { return magic; } }
        internal UInt32 Version { get { return version; } }
        internal UInt32 FileCount { get { return fileCount; } }
        internal Int32 EntryCount { get { return entryCount; } }
        internal UInt32 Priority { get { return priority; } }
        internal byte[] PathPrefix { get { return pathPrefix; } }
        internal UInt32 HeaderSize { get { return headerSize; } }

        internal string FilePattern { get { return filePattern; } }
        internal string FilePatternFull { get { return filePatternFull; } }
        internal string RelPath { get { return relPath; } }
        internal string Name { get { return name; } }
        internal FileEntryList EntryList { get { return entryList; } }
        internal BigFileList Parent { get { return parent; } }

        internal BigFile(string fileName, BigFileList parent)
        {
            relPath = fileName.Replace(parent.Folder, "").Trim('\\');
            this.parent = parent;
            FileStream fs = new FileStream(fileName, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            try
            {
                // read header
                magic = br.ReadBytes(4);
                version = br.ReadUInt32();
                fileCount = br.ReadUInt32();
                entryCount = br.ReadInt32();
                priority = br.ReadUInt32();
                pathPrefix = br.ReadBytes(0x20);

                // derivate some useful data
                headerSize = (UInt32)(fs.Position);
                name = Path.GetFileName(fileName).Replace(".000.tiger", "");
                filePatternFull = fileName.Replace("000", "{0:d3}");
                filePattern = Path.GetFileName(fileName).Replace("000", "{0:d3}");

                // read FAT into RawFileInfo
                itemsByIndex = new List<RawFileInfo>(entryCount);
                //itemsByHash = new Dictionary<UInt32, RawFileInfo>(entryCount);
                itemsByLocation = new List<RawFileInfo>(entryCount);
                entryList = new FileEntryList(this);


                for (int i = 0; i < entryCount; i++)
                {
                    RawFileInfo raw = new RawFileInfo();
                    raw.Hash = br.ReadUInt32();
                    raw.LangCode = br.ReadUInt32();
                    raw.Length = br.ReadUInt32();
                    raw.Location = br.ReadUInt32();
                    itemsByIndex.Add(raw);
                    //if (itemsByHash.ContainsKey(raw.Hash))
                    //    Noop.DoIt();
                    //itemsByHash.Add(raw.Hash, raw);
                }

                // UpdateEntryList();
            }
            finally
            {
                fs.Close();
            }
        }

        internal RawFileInfo GetRawDataByIdx(int index)
        {
            return itemsByIndex[index];
        }

        //internal RawFileInfo GetRawDataByHash(UInt32 hash)
        //{
        //    return itemsByHash[hash];
        //}

        internal void UpdateEntryList()
        {
            Log.LogDebugMsg(string.Format("{0} UpdateEntryList()", this.name));
            for (int i = 0; i < entryCount; i++)
            {
                FileEntry entry = new FileEntry(itemsByIndex[i], i, this.EntryList, this);
                EntryList.Add(entry);

                // try assign file name from hash code
                string matchedFileName = string.Empty;

                entry.Extra.NameResolved = parent.FileNameHashDict.TryGetValue(entry.Hash, out matchedFileName);
                if (entry.Extra.NameResolved)
                    entry.Extra.FileName = matchedFileName;

                entry.Extra.ResXFileName = string.Empty;
                /**/
                if (entry.Extra.NameResolved && entry.Translatable)
                {
                    string path = Path.GetDirectoryName(entry.Extra.FileName);
                    int pathHash = path.GetHashCode();
                    string alias;

                    if (parent.FileNameAliasDict.TryGetValue(pathHash, out alias))
                        entry.Extra.ResXFileName = alias;
                    else
                        if (parent.FolderAliasDict.TryGetValue(pathHash, out alias))
                            entry.Extra.ResXFileName = Path.ChangeExtension(entry.Extra.FileName.Replace(path, alias), ".resx");
                }
            }

            // raw dump of fat entries
            //TextWriter twEntriesByOrigOrder = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, filePrefix + "__fat_entries_raw.txt"));
            //twEntriesByOrigOrder.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8}", "hash", "lang", "length", "location"));
            //foreach (FileEntry entry in this)
            //    twEntriesByOrigOrder.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8}", entry.Raw.Hash, entry.Raw.LangCode, entry.Raw.Length, entry.Raw.Location));
            //twEntriesByOrigOrder.Close();

            // calculate virtual sizes
            EntryList.SortBy(FileEntryCompareField.Location);
            for (Int32 i = 0; i <= entryCount - 2; i++)
            {
                EntryList[i].VirtualSize = (EntryList[i + 1].Raw.Location - EntryList[i].Raw.Location) * 0x800;
                if (EntryList[i].VirtualSize == 0)
                    EntryList[i].VirtualSize = (EntryList[i + 2].Raw.Location - EntryList[i].Raw.Location) * 0x800;
            }
            if (entryCount > 0)
                EntryList[entryCount - 1].VirtualSize = (uint)Boundary.Extend((int)(EntryList[entryCount - 1].Raw.Length), 0x800);

            EntryList.SortBy(FileEntryCompareField.Location);

            // filetype detection

            foreach (FileEntry entry in EntryList)
            {
                #region Filetype detection
                List<string> specialFiles = new List<string>(new string[] {
                                    "489CD608", // pc-w\symbol.ids
                                    "9809A8EE", // pc-w\objectlist.txt
                                    "97836E8F", // pc-w\objlist.dat
                                    "F36C0FB8", // pc-w\unitlist.txt
                                    "478596A2", // pc-w\padshock\padshocklib.tfb
                                    "0A7B8340", // ??

                                });

                entry.Extra.FileType = FileTypeEnum.Unknown;

                if (specialFiles.Contains(entry.Extra.HashText))
                    entry.Extra.FileType = FileTypeEnum.Special;
                else
                    if (entry.Extra.HashText == "7CD333D3") // pc-w\local\locals.bin
                    {
                        entry.Extra.FileType = FileTypeEnum.BIN_MNU;
                        if (entry.Raw.Language == FileLanguage.English)
                            entry.Translatable = true;
                    }
                    else
                    {
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
                                entry.Extra.FileType = FileTypeEnum.DRM;
                                break;
                            default:
                                entry.Extra.FileType = FileTypeEnum.Unknown;
                                if (entry.Raw.Length > 0x814)
                                    if (Encoding.ASCII.GetString(entry.ReadContent(0x810, 4)) == "ENIC")
                                    {
                                        entry.Extra.FileType = FileTypeEnum.MUL_CIN;
                                        break;
                                    }

                                if (entry.Raw.Length > 0x2014)
                                    if (Encoding.ASCII.GetString(entry.ReadContent(0x2010, 4)) == "ENIC")
                                    {
                                        entry.Extra.FileType = FileTypeEnum.MUL_CIN;
                                        break;
                                    }

                                if (entry.ReadInt32(4) == -1) // 0xFFFFFFFF
                                {
                                    entry.Extra.FileType = FileTypeEnum.MUL2;
                                    break;
                                }

                                UInt32 m = entry.ReadUInt32(0);
                                if (m == 0xBB80 || m == 0xAC44)
                                {
                                    entry.Extra.FileType = FileTypeEnum.MUL2;
                                    break;
                                }

                                if (entry.Extra.FileType == FileTypeEnum.Unknown)
                                    Debug.WriteLine(string.Format("Unknown file type for: {0}", entry.Extra.FileNameForced));
                                break;
                        }
                        if (entry.Extra.FileType == FileTypeEnum.MUL_CIN || entry.Extra.FileType == FileTypeEnum.RAW_FNT || entry.Extra.FileType == FileTypeEnum.SCH)
                            entry.Translatable = true;
                    }
                #endregion

                #region Determine .resx filename
                if (entry.Extra.NameResolved && entry.Translatable)
                {
                    string path = Path.GetDirectoryName(entry.Extra.FileName);
                    int pathHash = path.GetHashCode();
                    string alias;

                    if (parent.FileNameAliasDict.TryGetValue(pathHash, out alias))
                        entry.Extra.ResXFileName = alias;
                    else
                        if (parent.FolderAliasDict.TryGetValue(pathHash, out alias))
                            entry.Extra.ResXFileName = Path.ChangeExtension(entry.Extra.FileName.Replace(path, alias), ".resx");

                }
                if (entry.Extra.ResXFileName == string.Empty)
                    entry.Extra.ResXFileName = entry.Extra.HashText + ".resx";
                #endregion

                bool debugDumpIt = false;
                // debugDumpIt = entry.Extra.FileType == FileTypeEnum.DRM && entry.Raw.Length < 1024;
                // debugDumpIt = entry.Extra.FileType == FileTypeEnum.BIN_MNU && entry.Extra.Language == FileLanguage.English;
                // debugDumpIt = fileEntry.Translatable;
                // debugDumpIt = fileEntry.Raw.Language == FileLanguage.NoLang || fileEntry.Raw.Language == FileLanguage.Unknown || fileEntry.Raw.Language == FileLanguage.English;
                // debugDumpIt = fileEntry.Extra.FileType == FileTypeEnum.MUL_CIN;
                // debugDumpIt = (entry.Extra.FileType == FileTypeEnum.BIN_MNU && entry.Extra.Language == FileLanguage.English) || entry.Extra.FileType == FileTypeEnum.Unknown; 
                // debugDumpIt = entry.Translatable;
                // debugDumpIt = entry.Extra.FileType == FileTypeEnum.DRM && entry.Raw.Length < 1024;
                debugDumpIt = !entry.Extra.NameResolved || entry.Extra.FileType == FileTypeEnum.Special;

                if (debugDumpIt)
                {
                    byte[] content = entry.ReadContent();
                    string extractFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "raw",
                            string.Format("{0}.{1}.{2}.{3}.txt", entry.BigFile.Name, entry.Extra.FileNameOnlyForced, entry.Extra.FileType, entry.Extra.LangText));
                    Directory.CreateDirectory(Path.GetDirectoryName(extractFileName));
                    FileStream fx = new FileStream(extractFileName, FileMode.Create, FileAccess.ReadWrite);
                    fx.Write(content, 0, content.Length);
                    fx.Close();
                }
            }

            // dump file info
            //TextWriter twEntries = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, filePrefix + "__fat_entries.txt"));
            //twEntries.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8} | {4,-8} {5} {6} {7,-8} {8} {9} {10}",
            //    "hash", "location", "length", "lang", "filetype", "language", "magic", "offset", "bfindex", "origidx", "filename"));

            //foreach (FileEntry entry in EntryList)
            //{
            //    twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8} | {4,-8} {5} \"{6}\" {7:X8} {8:D3} {9:D6} {10}",
            //        entry.Raw.Hash,
            //        entry.Raw.Location,
            //        entry.Raw.Length,
            //        entry.Raw.LangCode,
            //        entry.Extra.FileType.ToString(),
            //        entry.Raw.Language.ToString(),
            //        entry.Extra.Magic,
            //        entry.Extra.Offset,
            //        entry.Extra.BigFileIndex,
            //        entry.OriginalIndex,
            //        entry.Extra.FileNameForced));
            //}
            //twEntries.Close();
            Log.LogDebugMsg(string.Format("{0} end of UpdateEntryList()", this.name));

        }

        // order by priority, name
        public int CompareTo(object other)
        {
            if (other == null)
                return 1;

            BigFile otherBigFile = other as BigFile;
            if (otherBigFile == null)
                throw new ArgumentException("Object is not a " + this.GetType().ToString());

            int cmp = this.Priority.CompareTo(otherBigFile.Priority);
            if (cmp != 0)
                return cmp;
            return Name.CompareTo(otherBigFile.Name);
        }
    }

    class BigFileList : List<BigFile>
    {
        #region private declarations
        private string folder;
        private Dictionary<uint, string> fileNameHashDict = new Dictionary<uint, string>();
        private Dictionary<int, string> folderAliasDict = new Dictionary<int, string>();
        private Dictionary<int, string> fileNameAliasDict = new Dictionary<int, string>();
        private Dictionary<string, BigFile> itemsByName = new Dictionary<string, BigFile>();
        private BigFilePool filePool = null;
        #endregion

        internal string Folder { get { return folder; } }
        internal Dictionary<uint, string> FileNameHashDict { get { return fileNameHashDict; } }
        internal Dictionary<int, string> FolderAliasDict { get { return folderAliasDict; } }
        internal Dictionary<int, string> FileNameAliasDict { get { return fileNameAliasDict; } }
        internal BigFilePool FilePool { get { return filePool; } }
        internal Dictionary<string, BigFile> ItemsByName { get { return itemsByName; } }



        // ctor
        internal BigFileList(string folder)
        {
            this.folder = folder;
            filePool = new BigFilePool(this);

            List<string> files = new List<string>(Directory.GetFiles(folder, "*.000.tiger", SearchOption.AllDirectories));
            List<string> dupeFilter = new List<string>();

            // loading filelist for hash - filename resolution
            LoadFileNamesFile(Path.Combine(TRGameInfo.Game.WorkFolder, "filelist.txt"), out fileNameHashDict);
            LoadPathAliasesFile(Path.Combine(TRGameInfo.Game.WorkFolder, "path_aliases.txt"), out folderAliasDict, out fileNameAliasDict);

            Log.LogMsg(LogEntryType.Debug, string.Format("Building file structure: {0}", folder));
            foreach (string file in files)
            {
                string fileNameOnly = Path.GetFileName(file);
                if (!dupeFilter.Contains(fileNameOnly))
                {
                    dupeFilter.Add(fileNameOnly);

                    BigFile bigFile = new BigFile(file, this);
                    this.Add(bigFile);
                    ItemsByName.Add(bigFile.Name, bigFile);
                    bigFile.UpdateEntryList();
                }
            }
            Sort(); // by priority then name

            foreach (BigFile bf in this)
                Log.LogMsg(LogEntryType.Debug, string.Format("{0,-20}  entries: {1,5:d}  files: {2}  priority: {3}", bf.Name, bf.EntryCount, bf.FileCount, bf.Priority));
        }

        private void LoadFileNamesFile(string fileName, out Dictionary<uint, string> dict)
        {
            dict = new Dictionary<uint, string>();
            if (File.Exists(fileName))
            {
                TextReader rdr = new StreamReader(fileName);

                string line;
                while ((line = rdr.ReadLine()) != null)
                    dict.Add((uint)(Hash.MakeFileNameHash(line)), line);
            }
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

    }
}
