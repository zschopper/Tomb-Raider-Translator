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
using ExtensionMethods;

namespace TRTR
{

    #region Primitives

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

    enum TranslationStatus
    {
        NotTranslatable,
        Translatable,
        SkippedDueUpdated
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
        // Block count = bigfile boundary / block size
        //const uint BLOCK_COUNT_IN_BIGFILES = 0x12C00; // TRA/TRL v1.0.0.6
        //const uint BLOCK_COUNT_IN_BIGFILES = 0xFFE00;  // TRU/LCGOL 0x7FF00000 / 0x800

        #region private declarations
        private string langText;
        private FileEntry entry;
        private string fileName = string.Empty;
        #endregion

        internal string LangText { get { return langText; } }

        // (resolved) filename data
        internal bool FileNameResolved { get; set; }
        internal string FileName { get { return fileName; } set { fileName = value; } }
        internal string FileNameOnly { get { return FileName.Length > 0 ? Path.GetFileName(FileName) : ""; } }
        internal string FileNameOnlyForced { get { return FileName.Length > 0 ? Path.GetFileName(FileName) : entry.HashText; } }
        internal string FileNameForced { get { return FileName.Length > 0 ? FileName : entry.HashText; } }

        internal string ResXFileName { get; set; }

        internal FileExtraInfo(FileEntry entry)
        {
            this.entry = entry;
            if (entry.Language != FileLanguage.Unknown)
                this.langText = entry.Language.ToString();
            else
                this.langText = string.Format("UNK_{0:X8}", entry.Raw.LangCode);

            // try assign file name from hash code
            string matchedFileName;

            this.ResXFileName = string.Empty;

            this.FileNameResolved = entry.BigFile.Parent.FileNameHashDict.TryGetValue(entry.Hash, out matchedFileName);
            if (this.FileNameResolved)
            {
                this.FileName = matchedFileName;
                string path = Path.GetDirectoryName(this.FileName);
                int pathHash = path.GetHashCode();
                string alias;

                if (entry.BigFile.Parent.FileNameAliasDict.TryGetValue(pathHash, out alias))
                    this.ResXFileName = alias;
                else
                    if (entry.BigFile.Parent.FolderAliasDict.TryGetValue(pathHash, out alias))
                        this.ResXFileName = Path.ChangeExtension(this.FileName.Replace(path, alias), ".resx");
            }

            if (this.ResXFileName == string.Empty)
                this.ResXFileName = entry.HashText + ".resx";

        }
    }

    class FileEntry// : IComparable<FileEntry>
    {
        #region private variables
        private string hashText;
        private BigFile bigFile;
        //        private BigFilePool filePool = null;

        private FATEntry raw;
        private FileExtraInfo extra = null;

        private FileEntryList parent = null;

        private BigFilePool filePool { get { return bigFile.Parent.FilePool; } }
        private FileTypeEnum fileType = FileTypeEnum.Unknown;
        internal Int64 Offset { get { return raw.Address; } }
        #endregion

        internal UInt32 Hash { get { return raw.Hash; } }
        internal string HashText { get { return hashText; } }
        internal FATEntry Raw { get { return raw; } set { raw = value; } }
        internal FileExtraInfo Extra { get { return getExtra(); } }
        internal FileTypeEnum FileType { get { return fileType; } set { fileType = value; } }
        internal FileLanguage Language { get { return raw.Language; } }

        internal FileEntryList Parent { get { return parent; } }
        internal UInt32 OriginalIndex { get { return raw.Index; } }
        internal UInt32 VirtualSize = 0;

        internal TranslationStatus Status = TranslationStatus.NotTranslatable; // it contains translatable text or data?
        internal BigFile BigFile { get { return bigFile; } }

        // ctor
        internal FileEntry(FATEntry raw, FileEntryList parent, BigFile bigFile)
        {
            this.raw = raw;
            this.parent = parent;
            this.bigFile = bigFile;
            this.extra = new FileExtraInfo(this);
            this.hashText = raw.Hash.ToString("X8");

        }

        internal FileExtraInfo getExtra()
        {
            if (extra == null)
                extra = new FileExtraInfo(this);
            return extra;
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
                    {
                        ret = raw.BigFileIndex.CompareTo(other.raw.BigFileIndex);
                        if (ret == 0)
                            ret = raw.Address.CompareTo(other.raw.Address);
                    }
                    break;
                case FileEntryCompareField.LngCode:
                    ret = raw.LangCode.CompareTo(other.Raw.LangCode);
                    break;
                case FileEntryCompareField.LangText:
                    ret = Extra.LangText.CompareTo(other.Extra.LangText);
                    break;
                case FileEntryCompareField.Offset:
                    ret = Raw.Address.CompareTo(other.Raw.Address);
                    break;
                case FileEntryCompareField.Data:
                    throw new NotImplementedException();
                case FileEntryCompareField.Text:
                    throw new NotImplementedException(); // Extra.Text.CompareTo(other.Extra.Text);
                case FileEntryCompareField.FileName:
                    ret = extra.FileName.CompareTo(other.Extra.FileName);
                    break;
                case FileEntryCompareField.FileType:
                    ret = FileType.CompareTo(other.FileType);
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
                fs.Seek(Raw.Address, SeekOrigin.Begin);
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
                fs.Seek(Raw.Address + startPos, SeekOrigin.Begin);
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
                fs.Seek(Raw.Address + startPos, SeekOrigin.Begin);
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
                fs.Seek(Raw.Address + startPos, SeekOrigin.Begin);
                byte[] buf = new byte[4];
                fs.Read(buf, 0, 4);

                return BitConverter.ToUInt32(buf, 0);
            }
            finally
            {
                filePool.Close(this);
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

        internal static string CreateKey(string name, UInt32 index)
        {
            return string.Format("{0}.{1:D3}", name, index);
        }

        internal static string CreateKey(FileEntry entry)
        {
            return CreateKey(entry.BigFile.Name, entry.Raw.BigFileIndex);
        }

        internal FileStream Open(FileEntry entry)
        {
            return Open(entry.BigFile.Name, entry.Raw.BigFileIndex);
        }

        internal FileStream Open(string name, UInt32 index)
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

        internal void Close(string name, UInt32 index)
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

    // bigfile's file entry list
    class FileEntryList : List<FileEntry>
    {

        #region private declarations
        private BigFile parentBigFile;
        #endregion

        private List<FileEntryCompareField> compareFields =
                new List<FileEntryCompareField>(3) { FileEntryCompareField.Location };

        //internal Int32 EntryCount { get { return entryCount; } }

        internal List<FileEntryCompareField> CompareFields { get { return compareFields; } }
        //internal string FilePattern { get { return filePattern; } }
        //internal string FilePrefix { get { return filePrefix; } }
        internal BigFile ParentBigFile { get { return parentBigFile; } }

        internal bool simulateWrite = false;
        //private BackgroundWorker worker;

        internal FileEntryList(BigFile parent)
        {
            this.parentBigFile = parent;
        }

        internal void GenerateFilesTxt()
        {
            Int32 lastReported = 0;
            Log.LogProgress(StaticTexts.creatingFilesTxt, lastReported);

            if (!Directory.Exists(TRGameInfo.Game.WorkFolder))
                Directory.CreateDirectory(TRGameInfo.Game.WorkFolder);
            TextWriter twEntries = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "fat.entries.txt"));
            twEntries.WriteLine("hash  location  length  lang  offset ");
            for (Int32 i = 0; i < this.Count; i++)
            {
                twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8} {4:X8}", this[i].Hash, this[i].Raw.Location, this[i].Raw.Length, this[i].Raw.LangCode, this[i].Raw.Address));
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
                    writeIt = true; // entry.FileType == FileTypeEnum.MUL_CIN;
                }
                if (writeIt)
                    tw.WriteLine(string.Format("{0:X8}\t{1:X8}\t{2}", entry.Hash, entry.Hash, entry.FileType.ToString(), entry.Raw.Language.ToString()));
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
            string extractFolder = Path.Combine(destFolder, ParentBigFile.Name);

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
                //Log.LogDebugMsg(string.Format("delfolders: {0}\r\n{1}", extractFolder, string.Join("\r\n", delFolders.ToArray())));
                //Log.LogDebugMsg("/delfolders\r\n");
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

            SortBy(FileEntryCompareField.Location);

            TextWriter cineWriter = null;
            TextWriter menuWriter = null;

            try
            {
                SortBy(FileEntryCompareField.FileName);
                foreach (FileEntry entry in this)
                {
                    switch (entry.FileType)
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
                                FileStream fs = new FileStream(Path.Combine(extractFolder, ParentBigFile.Name + "_font_original.raw"), FileMode.Create);
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
            Log.LogProgress(StaticTexts.restoring, 0);
            //for (Int32 i = 0; i < this.Count; i++)
            //{
            //    FileEntry entry = this[i];
            //    switch (entry.FileType)
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
            //        Log.LogProgress(StaticTexts.restoring, percent);
            //        lastReported = percent;
            //    }
            //}
            Log.LogProgress(StaticTexts.restorationDone, 100);

            //filePool.CloseAll();
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

    // data for a bigfile
    class BigFile : IComparable
    {
        internal static readonly UInt32 Boundary = 0x7FF00000;

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
        private List<FATEntry> itemsByIndex;
        private List<FATEntry> itemsByLocation;
        //private Dictionary<UInt32, RawFileInfo> itemsByHash;
        private BigFileList parent;
        string name;
        #endregion

        // header data
        internal byte[] Magic { get { return magic; } }
        internal UInt32 Version { get { return version; } }
        internal UInt32 FileCount { get { return fileCount; } set { fileCount = value; } }
        internal Int32 EntryCount { get { return entryCount; } set { entryCount = value; } }
        internal UInt32 Priority { get { return priority; } }
        internal byte[] PathPrefix { get { return pathPrefix; } }
        internal UInt32 HeaderSize { get { return headerSize; } }
        internal bool HeaderChanged { get; set; }

        // Filename-related data
        internal string Name { get { return name; } } // name without index and extension
        internal string FilePattern { get { return filePattern; } } // file name index replaced with {0:d3}
        internal string FilePatternFull { get { return filePatternFull; } } // same as FilePattern, but with full path
        internal string RelPath { get { return relPath; } } // relative path from install folder

        internal FileEntryList EntryList { get { return entryList; } }
        internal BigFileList Parent { get { return parent; } }

        internal bool LoadedCompletely = false;

        // ctor
        internal BigFile(string fileName, BigFileList parent)
        {
            this.parent = parent;
            HeaderChanged = false;
            relPath = fileName.Replace(parent.Folder, "").Trim('\\');
            name = Path.GetFileName(fileName).Replace(".000.tiger", "");
            filePatternFull = fileName.Replace("000", "{0:d3}");
            filePattern = Path.GetFileName(fileName).Replace("000", "{0:d3}");
            ReadFAT();
        }

        internal void ReadFAT()
        {
            string fileName = string.Format(filePatternFull, 0);
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

                // read FAT into RawFileInfo
                itemsByIndex = new List<FATEntry>(entryCount);
                itemsByLocation = new List<FATEntry>(entryCount);
                //itemsByHash = new Dictionary<UInt32, RawFileInfo>(entryCount);

                entryList = new FileEntryList(this);

                for (UInt32 i = 0; i < entryCount; i++)
                {
                    FATEntry raw = new FATEntry();
                    raw.BigFile = this;
                    raw.Hash = br.ReadUInt32();
                    raw.LangCode = br.ReadUInt32();
                    raw.Length = br.ReadUInt32();
                    raw.Location = br.ReadUInt32();
                    raw.Index = i;
                    itemsByIndex.Add(raw);
                    itemsByLocation.Add(raw);
                }
                itemsByLocation.Sort(compareByLocation);

                // calculate entrys' Max size
                for (Int32 i = 0; i < itemsByLocation.Count - 1; i++)
                {
                    FATEntry raw = itemsByLocation[i];
                    FATEntry next = itemsByLocation[i + 1];
                    if (raw.BigFileIndex == next.BigFileIndex)
                        raw.MaxLength = next.Address - raw.Address;
                    else
                        raw.MaxLength = BigFile.Boundary - raw.Address;
                }
                if (itemsByLocation.Count > 0)
                {
                    // calculate last entry's virtual size
                    FATEntry lastEntry = itemsByLocation[itemsByLocation.Count - 1];
                    lastEntry.MaxLength = BigFile.Boundary - lastEntry.Address;
                }
            }
            finally
            {
                fs.Close();
            }
        }
        internal void WriteFAT()
        {
            entryList.Sort(compareByHash);
            string fileName = string.Format(filePatternFull, 0);
            FileStream fs = new FileStream(fileName, FileMode.Open);
            BinaryWriter bw = new BinaryWriter(fs);
            try
            {
                // write header
                bw.Write(magic);
                bw.Write(version);
                bw.Write(fileCount);
                bw.Write(entryCount);
                bw.Write(priority);
                bw.Write(pathPrefix);


                for (int i = 0; i < entryCount; i++)
                {
                    FATEntry raw = entryList[i].Raw;
                    bw.Write(raw.Hash);
                    bw.Write(raw.LangCode);
                    bw.Write(raw.Length);
                    bw.Write(raw.Location);
                }
            }
            finally
            {
                fs.Close();
            }
        }

        private int compareByLocation(FATEntry item1, FATEntry item2)
        {
            // compare by bigfile
            int cmp = item1.BigFileIndex.CompareTo(item2.BigFileIndex);
            if (cmp != 0)
                return cmp;
            // compare by address
            return item1.Address.CompareTo(item2.Address);
        }

        private static int compareByHash(FileEntry entry1, FileEntry entry2)
        {
            int cmp = entry1.Raw.Hash.CompareTo(entry2.Raw.Hash);
            if (cmp != 0)
                return cmp;
            return entry1.Raw.Index.CompareTo(entry2.Raw.Index);
        }

        internal FATEntry GetRawDataByIdx(int index)
        {
            return itemsByIndex[index];
        }

        internal void UpdateEntryList()
        {
            Log.LogDebugMsg(string.Format("{0} UpdateEntryList()", this.name));
            for (int i = 0; i < entryCount; i++)
            {
                FileEntry entry = new FileEntry(itemsByIndex[i], this.EntryList, this);
                EntryList.Add(entry);
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
                FileEntry thisEntry = EntryList[i];
                FileEntry nextEntry = EntryList[i + 1];

                if (thisEntry.Raw.Address == nextEntry.Raw.Address)
                    Log.LogDebugMsg("??");
                if (thisEntry.Raw.Length == 0 || nextEntry.Raw.Length == 0)
                    Log.LogDebugMsg("??");
                if (thisEntry.Raw.Address >= nextEntry.Raw.Address)
                    Log.LogDebugMsg("??");

                if (thisEntry.Raw.BigFileIndex == nextEntry.Raw.BigFileIndex)
                {
                    thisEntry.VirtualSize = (nextEntry.Raw.Address - thisEntry.Raw.Address);
                }
                else
                {
                    thisEntry.VirtualSize = (BigFile.Boundary - thisEntry.Raw.Address);
                }

                //if (thisEntry.VirtualSize == 0)
                //    if (entryList.Count > i + 2)
                //        thisEntry.VirtualSize = (EntryList[i + 2].Raw.Location - thisEntry.Raw.Location) * 0x800;
                //    else
                //        thisEntry.VirtualSize = (BigFile.Boundary - thisEntry.Raw.Location) * 0x800;
            }
            // ... for last item, too
            if (entryCount > 0)
                EntryList[entryCount - 1].VirtualSize = EntryList[entryCount - 1].Raw.Length.ExtendToBoundary(0x800);

            EntryList.SortBy(FileEntryCompareField.Location);

            // filetype detection

            List<string> specialFiles = new List<string>(new string[] {
                "489CD608", // pc-w\symbol.ids
                "9809A8EE", // pc-w\objectlist.txt
                "97836E8F", // pc-w\objlist.dat
                "F36C0FB8", // pc-w\unitlist.txt
                "478596A2", // pc-w\padshock\padshocklib.tfb
                "0A7B8340", // ??
            });

            foreach (FileEntry entry in EntryList)
            {
                #region Filetype detection
                entry.FileType = FileTypeEnum.Unknown;

                if (specialFiles.Contains(entry.HashText))
                    entry.FileType = FileTypeEnum.Special;
                else
                    if (entry.HashText == "7CD333D3") // pc-w\local\locals.bin
                    {
                        entry.FileType = FileTypeEnum.BIN_MNU;
                        if (entry.Raw.Language == FileLanguage.English)
                            entry.Status = TranslationStatus.Translatable;
                    }
                    else
                    {
                        string magic = Encoding.Default.GetString(entry.ReadContent(4));
                        switch (magic)
                        {
                            case "CDRM":
                                entry.FileType = FileTypeEnum.CDRM;
                                break;
                            case "!WAR":
                                entry.FileType = FileTypeEnum.RAW;
                                break;
                            case "FSB4":
                                entry.FileType = FileTypeEnum.FSB4;
                                break;
                            case "MUS!":
                                entry.FileType = FileTypeEnum.MUS;
                                break;
                            case "Vers":
                                entry.FileType = FileTypeEnum.SCH;
                                break;
                            case "PCD9":
                                entry.FileType = FileTypeEnum.PCD9;
                                break;
                            case "\x16\x00\x00\x00":
                                entry.FileType = FileTypeEnum.DRM;
                                break;
                            default:
                                entry.FileType = FileTypeEnum.Unknown;
                                if (entry.Raw.Length > 0x814)
                                    if (Encoding.ASCII.GetString(entry.ReadContent(0x810, 4)) == "ENIC")
                                    {
                                        entry.FileType = FileTypeEnum.MUL_CIN;
                                        break;
                                    }

                                if (entry.Raw.Length > 0x2014)
                                    if (Encoding.ASCII.GetString(entry.ReadContent(0x2010, 4)) == "ENIC")
                                    {
                                        entry.FileType = FileTypeEnum.MUL_CIN;
                                        break;
                                    }

                                if (entry.ReadInt32(4) == -1) // 0xFFFFFFFF
                                {
                                    entry.FileType = FileTypeEnum.MUL2;
                                    break;
                                }

                                UInt32 m = entry.ReadUInt32(0);
                                if (m == 0xBB80 || m == 0xAC44)
                                {
                                    entry.FileType = FileTypeEnum.MUL2;
                                    break;
                                }

                                if (entry.FileType == FileTypeEnum.Unknown)
                                    Debug.WriteLine(string.Format("Unknown file type for: {0}", entry.Extra.FileNameForced));
                                break;
                        }
                        if (entry.FileType == FileTypeEnum.MUL_CIN || entry.FileType == FileTypeEnum.RAW_FNT || entry.FileType == FileTypeEnum.SCH)
                            entry.Status = TranslationStatus.Translatable;
                    }

                #endregion

                if (entry.Status == TranslationStatus.Translatable)
                    entry.BigFile.Parent.AddTransEntry(entry);

                #region Determine .resx filename
                if (entry.Status == TranslationStatus.Translatable)
                {
                    if (entry.Extra.FileNameResolved)
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
                    else
                        entry.Extra.ResXFileName = entry.HashText + ".resx";
                }
                #endregion
            }

            #region dump file info
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
            //        entry.FileType.ToString(),
            //        entry.Raw.Language.ToString(),
            //        entry.Extra.Magic,
            //        entry.Raw.Address,
            //        entry.Extra.BigFileIndex,
            //        entry.OriginalIndex,
            //        entry.Extra.FileNameForced));
            //}
            //twEntries.Close();
            #endregion
            Log.LogDebugMsg(string.Format("{0} end of UpdateEntryList()", this.name));
            LoadedCompletely = true;
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
        private Dictionary<FatEntryKey, FileEntry> transEntries = null;
        #endregion

        internal string Folder { get { return folder; } }
        internal Dictionary<uint, string> FileNameHashDict { get { return fileNameHashDict; } }
        internal Dictionary<int, string> FolderAliasDict { get { return folderAliasDict; } }
        internal Dictionary<int, string> FileNameAliasDict { get { return fileNameAliasDict; } }
        internal BigFilePool FilePool { get { return filePool; } }
        internal Dictionary<string, BigFile> ItemsByName { get { return itemsByName; } }

        internal Dictionary<FatEntryKey, FileEntry> TransEntries { get { return transEntries; } }

        // ctor
        internal BigFileList(string folder)
        {
            this.folder = folder;
            filePool = new BigFilePool(this);

            transEntries = new Dictionary<FatEntryKey, FileEntry>();
            List<string> files = new List<string>(Directory.GetFiles(folder, "*.000.tiger", SearchOption.AllDirectories));

            // loading filelist for hash - filename resolution
            LoadFileNamesFile(Path.Combine(TRGameInfo.Game.WorkFolder, "filelist.txt"), out fileNameHashDict);
            LoadPathAliasesFile(Path.Combine(TRGameInfo.Game.WorkFolder, "path_aliases.txt"), out folderAliasDict, out fileNameAliasDict);

            Log.LogMsg(LogEntryType.Debug, string.Format("Building file structure: {0}", folder));

            List<string> dupeFilter = new List<string>();

            //dupeFilter.Add("bigfile.000.tiger");
            //dupeFilter.Add("bigfile_english.000.tiger");
            //dupeFilter.Add("patch.000.tiger");
            //dupeFilter.Add("patch_english.000.tiger");
            //dupeFilter.Add("patch2.000.tiger");
            //dupeFilter.Add("patch2_english.000.tiger");
            //dupeFilter.Add("title.000.tiger");
            //dupeFilter.Add("title_english.000.tiger");
            //dupeFilter.Add("pack4.000.tiger");
            //dupeFilter.Add("pack5.000.tiger");
            //dupeFilter.Add("pack6.000.tiger");
            //dupeFilter.Add("pack7.000.tiger");
            //dupeFilter.Add("pack8.000.tiger");

            foreach (string file in files)
            {
                string fileNameOnly = Path.GetFileName(file).ToLower();
                ;
                if (!dupeFilter.Contains(fileNameOnly))
                {
                    dupeFilter.Add(fileNameOnly);

                    BigFile bigFile = new BigFile(file, this);
                    this.Add(bigFile);
                    ItemsByName.Add(bigFile.Name, bigFile);
                }
            }
            Sort(); // bigfile list by items' priority then its name

            foreach (BigFile bf in this)
                Log.LogMsg(LogEntryType.Debug, string.Format("{0,-20}  entries: {1,5:d}  files: {2}  priority: {3}", bf.Name, bf.EntryCount, bf.FileCount, bf.Priority));

        }

        private void UpdateBigFiles()
        {
            foreach (BigFile bigFile in this)
                if (!bigFile.LoadedCompletely)
                    bigFile.UpdateEntryList();

            #region debug dump file
            foreach (FileEntry entry in transEntries.Values)
            {
                bool debugDumpIt = false;
                // debugDumpIt = entry.FileType == FileTypeEnum.DRM && entry.Raw.Length < 1024;
                // debugDumpIt = entry.FileType == FileTypeEnum.BIN_MNU && entry.Extra.Language == FileLanguage.English;
                // debugDumpIt = fileEntry.Raw.Language == FileLanguage.NoLang || fileEntry.Raw.Language == FileLanguage.Unknown || fileEntry.Raw.Language == FileLanguage.English;
                // debugDumpIt = fileEntry.FileType == FileTypeEnum.MUL_CIN;
                // debugDumpIt = (entry.FileType == FileTypeEnum.BIN_MNU && entry.Extra.Language == FileLanguage.English) || entry.FileType == FileTypeEnum.Unknown; 
                // debugDumpIt = entry.Status == TranslationStatus.Translatable;
                // debugDumpIt = entry.FileType == FileTypeEnum.DRM && entry.Raw.Length < 1024;
                // debugDumpIt = !entry.Extra.FileNameResolved || entry.FileType == FileTypeEnum.Special;
                // debugDumpIt = entry.Status == TranslationStatus.Translatable;
                // debugDumpIt = entry.FileType == FileTypeEnum.SCH;
                // debugDumpIt = entry.Hash == 0xEF4C7C2Cu;

                if (debugDumpIt)
                {
                    byte[] content = entry.ReadContent();
                    string extractFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "raw",
                            string.Format("{0}.{1}.{2}.{3}.txt", entry.BigFile.Name, entry.Extra.FileNameOnlyForced, entry.FileType, entry.Extra.LangText));
                    Directory.CreateDirectory(Path.GetDirectoryName(extractFileName));
                    FileStream fx = new FileStream(extractFileName, FileMode.Create, FileAccess.ReadWrite);
                    fx.Write(content, 0, content.Length);
                    fx.Close();
                }
            }
            #endregion

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

        internal bool AddTransEntry(FileEntry entry)
        {
            FatEntryKey key = new FatEntryKey(entry.Raw.Hash, entry.Raw.Language);
            FileEntry foundEntry = null;
            if (transEntries.TryGetValue(key, out foundEntry))
            {
                if (foundEntry.BigFile.Priority < entry.BigFile.Priority)
                {
                    transEntries[key] = entry;
                    foundEntry.Status = TranslationStatus.SkippedDueUpdated;
                    Log.LogDebugMsg(string.Format("ATE: {0}.{1}.{2} discarded with file from {3}", foundEntry.BigFile.Name, entry.Extra.FileNameForced, foundEntry.Language, entry.BigFile.Name));
                    return true;
                }
                Log.LogDebugMsg(string.Format("ATE: {0}.{1}.{2} kept - other {3}", foundEntry.BigFile.Name, foundEntry.Extra.FileNameForced, foundEntry.Language, entry.BigFile.Name));
                return false;
            }

            transEntries.Add(key, entry);
            return true;
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
            UpdateBigFiles();
            foreach (BigFile bigFile in this)
            {
                string extractFolder = Path.Combine(destFolder, bigFile.Name);

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
                    //Log.LogDebugMsg(string.Format("delfolders: {0}\r\n{1}", extractFolder, string.Join("\r\n", delFolders.ToArray())));
                    //Log.LogDebugMsg("/delfolders\r\n");
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

                //SortBy(FileEntryCompareField.Location);

                TextWriter cineWriter = null;
                TextWriter menuWriter = null;

                try
                {
                    //SortBy(FileEntryCompareField.FileName);
                    foreach (FileEntry entry in bigFile.EntryList)
                    {
                        switch (entry.FileType)
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
                                    FileStream fs = new FileStream(Path.Combine(extractFolder, bigFile.Name + "_font_original.raw"), FileMode.Create);
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
        }

        internal void Translate(bool simulated)
        {
            Int32 lastReported = 0;
            Log.LogProgress(StaticTexts.translating, 0);

            int i = 0;
            UpdateBigFiles();

            List<FileEntry> transEntryList = new List<FileEntry>(TransEntries.Values.ToArray<FileEntry>());
            transEntryList.Sort((e1, e2) => e1.Raw.Address.CompareTo(e2.Raw.Address));

            foreach (FileEntry entry in transEntryList)
            {
                if (entry.Status == TranslationStatus.Translatable)
                {
                    switch (entry.FileType)
                    {
                        case FileTypeEnum.MUL_CIN:
                            {
                                if (entry.Raw.Language == FileLanguage.English || entry.Raw.Language == FileLanguage.NoLang)
                                {
                                    //if (entry.Hash == 0xD79F6A34u)
                                    {
                                        CineFile cine = new CineFile(entry);
                                        cine.Translate(simulated);
                                    }
                                }
                                break;
                            }
                        case FileTypeEnum.BIN_MNU:
                            {
                                if (entry.Raw.Language == FileLanguage.English)
                                {
                                    MenuFile menu = new MenuFile(entry);
                                    menu.Translate(simulated);
                                }
                                break;
                            }
                        case FileTypeEnum.SCH:
                            {
                                MovieFile movie = new MovieFile(entry);
                                movie.Translate(simulated);
                                break;
                            }
                        case FileTypeEnum.RAW_FNT:
                            {
                                //FontFile font = new FontFile(entry);
                                //font.Translate(simulated);
                                break;
                            }
                    } // switch
                }
                // notify user about translation progress
                Int32 percent = i * 100 / transEntries.Count;
                if (percent > lastReported)
                {
                    Log.LogProgress(StaticTexts.translating, percent);
                    lastReported = percent;
                }
                i++;
            }
            Log.LogProgress(StaticTexts.translationDone, 100);
            foreach (BigFile bigFile in this)
            {
                if (bigFile.HeaderChanged)
                    bigFile.WriteFAT();
            }
            filePool.CloseAll();
        }

        static Int64 ofs = 0;

        internal static void DumpToFile(string fileName, byte[] content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            FileStream fs = new FileStream(fileName, FileMode.Create);
            try
            {
                fs.Write(content, 0, content.Length);
            }
            finally
            {
                fs.Close();
            }


        }
        internal void WriteFile(BigFile bigFile, FileEntry entry, byte[] content, bool simulate)
        {
            if (simulate)
                return;
            FileStream ContentStream = null;
            FATEntry raw;

            #region prepare file write
            // try to find entry
            FileEntry foundEntry = bigFile.EntryList.Find(
                bk => bk.Hash == entry.Hash &&
                    bk.Language == entry.Language);

            Log.LogDebugMsg(string.Format("WriteFile: {0:X8} old len: {1:X8} old vlen: {2:X8} clen: {3:X8} vclen {4:X8}",
                entry.Hash, entry.Raw.Length, entry.Raw.Length.ExtendToBoundary(0x800), content.Length, content.Length.ExtendToBoundary(0x800)));

            // Determine whether bigfile contains entry
            if (foundEntry != null)
            {
                // update fat (length)
                raw = foundEntry.Raw;

                // determine whether file fits its place
                if (true && (raw.Length.ExtendToBoundary(0x800) >= content.Length)) // fits
                {
                    Log.LogDebugMsg(string.Format("  existing, fits"));
                    // update fat (length)
                    raw.Length = (UInt32)content.Length;
                    // prepare entry to write
                    foundEntry.Raw = raw;
                    bigFile.HeaderChanged = true;
                }
                else // not fit
                {
                    // determine there is free place in the end of last bigfile for content
                    bool contentFits = false;
                    ContentStream = bigFile.Parent.filePool.Open(bigFile.Name, bigFile.FileCount - 1);
                    try
                    {
                        contentFits = ContentStream.Length.ExtendToBoundary(0x800) + content.Length <= BigFile.Boundary;
                        if (contentFits) // fits in the end of last bigfile
                        {
                            Log.LogDebugMsg(string.Format("  existing, not fit"));
                            // update fat (location & length)
                            raw.Location = (UInt32)ContentStream.Length.ExtendToBoundary(0x800) + bigFile.Priority * 0x10 + bigFile.FileCount - 1;
                            raw.Length = (UInt32)content.Length;
                            // prepare entry to write
                            foundEntry.Raw = raw;
                            bigFile.HeaderChanged = true;
                        }
                    }
                    finally
                    {
                        filePool.Close(bigFile.Name, bigFile.FileCount - 1);
                    }
                    if (!contentFits) // not fits in the end of last bigfile
                    {
                        Log.LogDebugMsg(string.Format("  existing, new BF"));
                        // add a new bigfile
                        new FileStream(string.Format(bigFile.FilePatternFull, bigFile.FileCount), FileMode.CreateNew).Close();
                        // update header (increase file count)
                        bigFile.FileCount++;
                        // update fat (location & length)
                        raw.Location = (UInt32)(0) + bigFile.Priority * 0x10 + bigFile.FileCount - 1;
                        raw.Length = (UInt32)content.Length;
                        // prepare entry to write
                        foundEntry.Raw = raw;
                        bigFile.HeaderChanged = true;
                    }
                }
            }
            else // not contains
            {
                foundEntry = entry;
                raw = foundEntry.Raw;

                // determine there is free place in the end of last bigfile for content
                bool contentFits = false;
                FileStream LastBFStream = bigFile.Parent.filePool.Open(bigFile.Name, bigFile.FileCount - 1);
                try
                {
                    contentFits = LastBFStream.Length.ExtendToBoundary(0x800) + content.Length <= BigFile.Boundary;
                    if (contentFits) // fits in the end of last bigfile
                    {
                        Log.LogDebugMsg(string.Format("  new, fit BF"));
                        // update header (increase entry count)
                        bigFile.EntryCount++;
                        // update fat (location & length)
                        raw.Location = (UInt32)LastBFStream.Length.ExtendToBoundary(0x800) + bigFile.Priority * 0x10 + bigFile.FileCount - 1;
                        raw.Length = (UInt32)content.Length;
                        // prepare entry to write
                        foundEntry.Raw = raw;
                        // update fat (add new entry)
                        bigFile.EntryList.Add(foundEntry);
                        bigFile.HeaderChanged = true;
                    }
                }
                finally
                {
                    filePool.Close(bigFile.Name, bigFile.FileCount - 1);
                }
                if (!contentFits) // not fits in the end of last bigfile
                {
                    Log.LogDebugMsg(string.Format("  new, not fit BF"));
                    // add a new bigfile
                    new FileStream(string.Format(bigFile.FilePatternFull, bigFile.FileCount), FileMode.CreateNew).Close();
                    // update header (increase file count)
                    bigFile.FileCount++;
                    // update header (increase entry count)
                    bigFile.EntryCount++;
                    // update fat (location & length)
                    raw.Location = (UInt32)(0) + bigFile.Priority * 0x10 + bigFile.FileCount - 1;
                    raw.Length = (UInt32)content.Length;
                    // prepare entry to write
                    foundEntry.Raw = raw;
                    // update fat (add new entry)
                    bigFile.EntryList.Add(foundEntry);
                    bigFile.HeaderChanged = true;
                }
            }
            #endregion
            #region file write
            ContentStream = filePool.Open(foundEntry.BigFile.Name, foundEntry.Raw.BigFileIndex);
            try
            {
                #region dump #1
                bool dump = true;
                string dumpFileName = string.Empty;
                if (dump)
                {
                    dumpFileName = string.Format("{0}.{1}.{2}.{3}.txt", entry.Parent.ParentBigFile.Name, entry.Extra.FileNameOnlyForced, entry.FileType, entry.Extra.LangText);
                    byte[] bufRead = new byte[entry.Raw.Length];
                    ContentStream.Position = entry.Raw.Address;
                    ContentStream.Read(bufRead, 0, (int)entry.Raw.Length);
                    DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "source", dumpFileName), bufRead);

                    string extractTrnFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "translated", dumpFileName);
                    DumpToFile(extractTrnFileName, content);
                }

                ContentStream.Position = entry.Raw.Address;

                if (ContentStream.Position < ofs)
                {
                    Log.LogDebugMsg(string.Format("!!!!! diffff {0:X8}", ofs - ContentStream.Position));
                }
                #endregion

                // if file smallert than entry's start address, expand file
                if (ContentStream.Length < foundEntry.Raw.Address)
                {
                    // fill bigfile to its next file boundary
                    UInt32 newLength = (UInt32)ContentStream.Length.ExtendToBoundary(0x800);
                    byte[] PaddingBuffer = new byte[2000];
                    Array.Clear(PaddingBuffer, 0, PaddingBuffer.Length);
                    ContentStream.Seek(0, SeekOrigin.End);

                    while (ContentStream.Length < newLength)
                    {
                        int writeLen = (int)(newLength - ContentStream.Length > PaddingBuffer.Length
                            ? PaddingBuffer.Length
                            : newLength - ContentStream.Length);
                        ContentStream.Write(PaddingBuffer, 0, writeLen);
                    }
                }

                ContentStream.Position = entry.Raw.Address;
                ContentStream.Write(content, 0, content.Length);

                Log.LogDebugMsg(string.Format("  writing  from {0:X8} to {1:X8} len {2:X8}", foundEntry.Raw.Address, foundEntry.Raw.Address + content.Length, content.Length));

                // clean file end to its boundary
                if (ContentStream.Position < ContentStream.Length - 1)
                {
                    int padLen = (int)((foundEntry.Raw.Address + content.Length).ExtendToBoundary(0x800) - ContentStream.Position - 1);
                    if (padLen > 0)
                    {
                        byte[] buf = new byte[padLen];
                        Array.Clear(buf, 0, (int)padLen);

                        Log.LogDebugMsg(string.Format("  cleaning from {0:X8} to {1:X8} len {2:X8}", ContentStream.Position, ContentStream.Position + padLen, padLen));
                        ContentStream.Write(buf, 0, buf.Length);
                    }
                }
                ofs = ContentStream.Position;

                #region dump #2

                if (dump)
                {
                    string extractSrcFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "reread", dumpFileName);
                    byte[] bufRead = new byte[entry.Raw.Length];
                    ContentStream.Position = entry.Raw.Address;
                    ContentStream.Read(bufRead, 0, (int)entry.Raw.Length);
                    DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "source", dumpFileName), bufRead);
                    ContentStream.Position = ofs;
                }
                #endregion
            }
            finally
            {
                filePool.Close(entry.BigFile.Name, entry.Raw.BigFileIndex);
            }
            #endregion
        }

    }
}
