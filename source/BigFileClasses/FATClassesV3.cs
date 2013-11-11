﻿using System;
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
        private FileEntry entry;
        private string fileName = string.Empty;
        #endregion

        // (resolved) filename data
        internal bool FileNameResolved { get; set; }
        internal string FileName { get { return fileName; } set { fileName = value; } }
        internal string FileNameOnly { get { return FileName.Length > 0 ? Path.GetFileName(FileName) : ""; } }
        internal string FileNameOnlyForced { get { return FileName.Length > 0 ? Path.GetFileName(FileName) : entry.HashText; } }
        internal string FileNameForced { get { return FileName.Length > 0 ? FileName : entry.HashText; } }

        internal FileExtraInfo(FileEntry entry)
        {
            this.entry = entry;

            // try assign file name from hash code
            string matchedFileName;
            this.FileNameResolved = entry.BigFile.Parent.FileNameHashDict.TryGetValue(entry.Hash, out matchedFileName);
            if (this.FileNameResolved)
                this.FileName = matchedFileName;

        }
    }

    class FileEntry// : IComparable<FileEntry>
    {
        #region private variables
        private string hashText;
        private BigFileV3 bigFile;
        //        private BigFilePool filePool = null;

        private FATEntry raw;
        private FileExtraInfo extra = null;

        private FileEntryList parent = null;

        private FileTypeEnum fileType = FileTypeEnum.Unknown;
        internal Int64 Offset { get { return raw.Address; } }
        #endregion

        internal UInt32 Hash { get { return raw.Hash; } }
        internal string HashText { get { return hashText; } }
        internal FATEntry Raw { get { return raw; } set { raw = value; } }
        internal FileExtraInfo Extra { get { return getExtra(); } }
        internal FileTypeEnum FileType { get { return fileType; } set { fileType = value; } }

        internal FileEntryList Parent { get { return parent; } }
        internal UInt32 OriginalIndex { get { return raw.Index; } }
        internal UInt32 VirtualSize = 0;

        internal TranslationStatus Status = TranslationStatus.NotTranslatable; // it contains translatable text or data?
        internal BigFileV3 BigFile { get { return bigFile; } }

        // ctor
        internal FileEntry(FATEntry raw, FileEntryList parent, BigFileV3 bigFile)
        {
            this.raw = raw;
            this.parent = parent;
            this.bigFile = bigFile;
            this.extra = null;// new FileExtraInfo(this);
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
                    ret = raw.Locale.CompareTo(other.Raw.Locale);
                    break;
                case FileEntryCompareField.LangText:
                    throw new NotImplementedException();
                case FileEntryCompareField.Offset:
                    ret = Raw.Address.CompareTo(other.Raw.Address);
                    break;
                case FileEntryCompareField.Data:
                    throw new NotImplementedException();
                case FileEntryCompareField.Text:
                    throw new NotImplementedException(); // Extra.Text.CompareTo(other.Extra.Text);
                case FileEntryCompareField.FileName:
                    ret = extra.FileNameForced.CompareTo(other.Extra.FileNameForced);
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
            FileStream fs = TRGameInfo.FilePool.Open(this);
            try
            {
                fs.Seek(Raw.Address, SeekOrigin.Begin);
                Int32 readLength = (maxLen >= 0) ? maxLen : (Int32)raw.Length;
                ret = new byte[readLength];
                fs.Read(ret, 0, readLength);
            }
            finally
            {
                TRGameInfo.FilePool.Close(this);
            }
            return ret;
        }

        internal byte[] ReadContent(Int32 startPos, Int32 maxLen)
        {

            byte[] ret;
            FileStream fs = TRGameInfo.FilePool.Open(this);
            try
            {
                fs.Seek(Raw.Address + startPos, SeekOrigin.Begin);
                Int32 readLength = (maxLen >= 0) ? maxLen : (Int32)raw.Length - startPos;
                ret = new byte[readLength];
                fs.Read(ret, 0, readLength);
            }
            finally
            {
                TRGameInfo.FilePool.Close(this);
            }
            return ret;
        }

        internal Int32 ReadInt32(Int32 startPos)
        {
            FileStream fs = TRGameInfo.FilePool.Open(this);
            try
            {
                fs.Seek(Raw.Address + startPos, SeekOrigin.Begin);
                byte[] buf = new byte[4];
                fs.Read(buf, 0, 4);

                return BitConverter.ToInt32(buf, 0);
            }
            finally
            {
                TRGameInfo.FilePool.Close(this);
            }
        }

        internal UInt32 ReadUInt32(Int32 startPos)
        {
            FileStream fs = TRGameInfo.FilePool.Open(this);
            try
            {
                fs.Seek(Raw.Address + startPos, SeekOrigin.Begin);
                byte[] buf = new byte[4];
                fs.Read(buf, 0, 4);

                return BitConverter.ToUInt32(buf, 0);
            }
            finally
            {
                TRGameInfo.FilePool.Close(this);
            }
        }

    }

    // bigfile's file entry list
    class FileEntryList : List<FileEntry>
    {
        #region private declarations
        private BigFileV3 parentBigFile;
        #endregion

        private List<FileEntryCompareField> compareFields =
                new List<FileEntryCompareField>(3) { FileEntryCompareField.Location };

        internal List<FileEntryCompareField> CompareFields { get { return compareFields; } }
        internal BigFileV3 ParentBigFile { get { return parentBigFile; } }

        //internal bool simulateWrite = false;

        internal FileEntryList(BigFileV3 parent)
        {
            this.parentBigFile = parent;
        }

        private static int compareByPathLength(string file1, string file2)
        {
            string path1 = Path.GetDirectoryName(file1);
            string path2 = Path.GetDirectoryName(file2);

            int compareRes = path1.CompareTo(path2);

            if (compareRes != 0)
                return compareRes;

            return string.Compare(file1, path1.Length, file2, path2.Length, int.MaxValue);
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
    class BigFileV3 : IComparable
    {
        internal static readonly UInt32 Boundary = 0x7FF00000;
        internal static string[] knowBFLocalTexts = new string[] { "ARABIC", "ENGLISH", "FRENCH", "GERMAN", "ITALIAN", "POLISH", "RUSSIAN", "SPANISH" };
        internal static FileLocale[] knowBFLocals = new FileLocale[] { FileLocale.Arabic, FileLocale.English, FileLocale.French, FileLocale.German, 
            FileLocale.Italian, FileLocale.Polish, FileLocale.Russian, FileLocale.Spanish };

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
        private string name;
        private FileLocale locale;
        private string neutralName;
        //private bool isDLC;
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
        internal FileLocale Locale { get { return locale; } }
        internal string NeutralName { get { return neutralName; } }

        internal bool LoadedCompletely = false;

        // ctor
        internal BigFileV3(string fileName, BigFileList parent, bool isDLC)
        {
            this.parent = parent;
            HeaderChanged = false;
            relPath = fileName.Replace(parent.Folder, "").Trim('\\');
            name = Path.GetFileName(fileName).Replace(".000.tiger", "");
            filePatternFull = fileName.Replace("000", "{0:d3}");
            filePattern = Path.GetFileName(fileName).Replace("000", "{0:d3}");

            locale = FileLocale.Default;
            for (int j = 0; j < knowBFLocalTexts.Length; j++)
                if (name.ToUpper().EndsWith(knowBFLocalTexts[j]))
                {
                    locale = knowBFLocals[j];
                    neutralName = name.Substring(0, knowBFLocalTexts[j].Length) + "_LOCALIZED";
                }
            if (locale == FileLocale.Default)
                neutralName = name;

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
                    raw.Locale = (FileLocale)br.ReadUInt32();
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
                        raw.MaxLength = BigFileV3.Boundary - raw.Address;
                }
                if (itemsByLocation.Count > 0)
                {
                    // calculate last entry's virtual size
                    FATEntry lastEntry = itemsByLocation[itemsByLocation.Count - 1];
                    lastEntry.MaxLength = BigFileV3.Boundary - lastEntry.Address;
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
            byte[] buf = new byte[headerSize + entryCount * 0x10];
            fs.Read(buf, 0, buf.Length);
            //BigFileList.DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", this.name + ".fat_raw_src.txt"), buf);
            fs.Position = 0;

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
                    bw.Write((uint)(raw.Locale));
                    bw.Write(raw.Length);
                    bw.Write(raw.Location);
                }
                buf = new byte[headerSize + entryCount * 0x10];
                fs.Position = 0;
                fs.Read(buf, 0, buf.Length);
                //BigFileList.DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", this.name + ".fat_raw_mod.txt"), buf);
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
            dumpRawFatEntries();

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
                    thisEntry.VirtualSize = (BigFileV3.Boundary - thisEntry.Raw.Address);
                }

                //if (thisEntry.VirtualSize == 0)
                //    if (entryList.Count > i + 2)
                //        thisEntry.VirtualSize = (EntryList[i + 2].Raw.Location - thisEntry.Raw.Location) * 0x800;
                //    else
                //        thisEntry.VirtualSize = (BigFileV3.Boundary - thisEntry.Raw.Location) * 0x800;
            }
            // ... for last item, too
            if (entryCount > 0)
                EntryList[entryCount - 1].VirtualSize = EntryList[entryCount - 1].Raw.Length.ExtendToBoundary(0x800);

            EntryList.SortBy(FileEntryCompareField.Location);

            // filetype detection

            List<UInt32> specialFiles = new List<UInt32>(new UInt32[] {
                0x489CD608u, // pc-w\symbol.ids
                0x9809A8EEu, // pc-w\objectlist.txt
                0x97836E8Fu, // pc-w\objlist.dat
                0xF36C0FB8u, // pc-w\unitlist.txt
                0x478596A2u, // pc-w\padshock\padshocklib.tfb
                0x0A7B8340u, // ??
            });

            foreach (FileEntry entry in EntryList)
            {

                #region Filetype detection
                entry.FileType = FileTypeEnum.Unknown;

                if (specialFiles.Contains(entry.Hash))
                {
                    entry.FileType = FileTypeEnum.Special;
                    Directory.CreateDirectory(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", entry.BigFile.Name));

                    BigFileList.DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", entry.BigFile.Name, entry.Extra.FileNameOnlyForced), entry);
                }
                else
                    if (entry.Hash == 0x7CD333D3u) // pc-w\local\locals.bin
                    {
                        entry.FileType = FileTypeEnum.BIN_MNU;
                        if (entry.Raw.IsLocale(TRGameInfo.TransTextLang))
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
                            case "PCD9File":
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

                                if (entry.ReadInt32(4) == -1) // 0xFFFFFFFFu
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
            }

            #region dump file info
            dumpFatEntries();
            #endregion

            Log.LogDebugMsg(string.Format("{0} end of UpdateEntryList()", this.name));
            LoadedCompletely = true;
        }

        private void dumpRawFatEntries()
        {
            if (TRGameInfo.Game.debugMode)
            {
                if (!Directory.Exists(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT")))
                    Directory.CreateDirectory(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT"));

                TextWriter twEntriesByOrigOrder = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT", this.Name + "__fat_entries_raw.txt"));
                twEntriesByOrigOrder.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8}", "hash", "lang", "length", "location"));
                foreach (FileEntry entry in this.EntryList)
                    twEntriesByOrigOrder.WriteLine(string.Format("{0:X8} {1} {2:X8} {3:X8}", entry.Raw.Hash, entry.Raw.LocaleText, entry.Raw.Length, entry.Raw.Location));
                twEntriesByOrigOrder.Close();
            }
        }

        private void dumpFatEntries()
        {
            if (TRGameInfo.Game.debugMode)
            {
                if (!Directory.Exists(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT")))
                    Directory.CreateDirectory(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT"));
                TextWriter twEntries = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT", Name + "__fat_entries.txt"));
                twEntries.WriteLine(string.Format("{0,-8} {1,-8} {2,-8} {3,-8} | {4,-8} {5} {6,-8} {7} {8}",
                    "hash", "location", "length", "locale", "filetype", "language", "Offset", "origidx", "filename"));

                List<FileLocale> locales = new List<FileLocale>();

                foreach (FileEntry entry in EntryList)
                {
                    twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8} | {4,-8} {5} {6:X8} {7:D6} {8}",
                        entry.Raw.Hash,
                        entry.Raw.Location,
                        entry.Raw.Length,
                        (uint)(entry.Raw.Locale),
                        entry.FileType.ToString(),
                        entry.Raw.LocaleText,
                        entry.Raw.Address,
                        entry.OriginalIndex,
                        entry.Extra.FileNameForced));

                    if (!locales.Contains(entry.Raw.Locale))
                        locales.Add(entry.Raw.Locale);
                }

                twEntries.WriteLine("USED LOCALES:");
                FileLocale mask = (FileLocale.English | FileLocale.French | FileLocale.German | FileLocale.Italian | FileLocale.Spanish | FileLocale.Japanese | FileLocale.Portugese | FileLocale.Polish | /*FileLocale.EnglishUK | */ FileLocale.Russian | FileLocale.Czech | FileLocale.Dutch | /*FileLocale.Hungarian | FileLocale.Croatian | /**/ FileLocale.Arabic | FileLocale.Korean | FileLocale.Chinese);

                mask = mask | /*FileLocale.UnusedFlags | */FileLocale.UnusedFlags2 | FileLocale.UpperWord;


                foreach (FileLocale locale in locales)
                {
                    twEntries.WriteLine(string.Format("{0:X8}   {1,-32} | {2} |  {3:X8}", (uint)locale, Convert.ToString((uint)locale, 2), locale ^ (FileLocale.UnusedFlags2 | FileLocale.UpperWord), (uint)(locale ^ mask)));
                }

                twEntries.Close();
            }
        }

        // order by priority, name
        public int CompareTo(object other)
        {
            if (other == null)
                return 1;

            BigFileV3 otherBigFile = other as BigFileV3;
            if (otherBigFile == null)
                throw new ArgumentException("Object is not a " + this.GetType().ToString());

            int cmp = this.Priority.CompareTo(otherBigFile.Priority);
            if (cmp != 0)
                return cmp;
            return Name.CompareTo(otherBigFile.Name);
        }
    }

    class BigFileList : List<BigFileV3>
    {
        #region private declarations
        private string folder;
        private Dictionary<uint, string> fileNameHashDict = new Dictionary<uint, string>();
        private Dictionary<string, BigFileV3> itemsByName = new Dictionary<string, BigFileV3>();
        private Dictionary<FatEntryKey, FileEntry> transEntries = null;
        private FileLocale locale;
        #endregion

        internal string Folder { get { return folder; } }
        internal Dictionary<uint, string> FileNameHashDict { get { return fileNameHashDict; } }
        internal Dictionary<string, BigFileV3> ItemsByName { get { return itemsByName; } }

        internal Dictionary<FatEntryKey, FileEntry> TransEntries { get { return transEntries; } }

        // ctor
        internal BigFileList(string folder)
        {
            this.folder = folder;
            transEntries = new Dictionary<FatEntryKey, FileEntry>();

            // loading filelist for hash - filename resolution
            LoadFileNamesFile(Path.Combine(TRGameInfo.Game.WorkFolder, "filelist.txt"), out fileNameHashDict);

            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(folder, "*.000.tiger", SearchOption.TopDirectoryOnly));
            string dlcFolder = Path.Combine(folder, "Dlc");
            if (Directory.Exists(dlcFolder))
                files.AddRange(Directory.GetFiles(dlcFolder, "*.000.tiger", SearchOption.TopDirectoryOnly));

            Log.LogMsg(LogEntryType.Debug, string.Format("Building file structure: {0}", folder));

            List<string> dupeFilter = new List<string>();

            //dupeFilter.Add("bigfile.000.tiger");
            dupeFilter.Add("bigfile_english.000.tiger");
            dupeFilter.Add("patch.000.tiger");
            dupeFilter.Add("patch_english.000.tiger");
            dupeFilter.Add("patch2.000.tiger");
            dupeFilter.Add("patch2_english.000.tiger");
            dupeFilter.Add("title.000.tiger");
            dupeFilter.Add("title_english.000.tiger");
            dupeFilter.Add("pack4.000.tiger");
            dupeFilter.Add("pack5.000.tiger");
            dupeFilter.Add("pack6.000.tiger");
            dupeFilter.Add("pack7.000.tiger");
            dupeFilter.Add("pack8.000.tiger");

            dupeFilter.Clear();

            locale = FileLocale.Default;
            foreach (string file in files)
            {
                string fileNameOnly = Path.GetFileName(file).ToLower();

                if (!dupeFilter.Contains(fileNameOnly))
                {
                    dupeFilter.Add(fileNameOnly);
                    BigFileV3 bigFile = new BigFileV3(file, this, false);
                    this.Add(bigFile);
                    ItemsByName.Add(bigFile.Name, bigFile);
                    if (bigFile.Locale != FileLocale.Default)
                    {
                        if (locale == FileLocale.Default)
                            locale = bigFile.Locale;
                        else
                            if (locale != bigFile.Locale)
                                throw new Exception("Locale of game can not be determined: there are more bigfiles with different locales.");
                    }
                }
            }

            TRGameInfo.TransVoiceLang = locale;
            // bigfile list by items' priority (desc) then its name (name)
            Sort((bf1, bf2) =>
            {
                int i = (bf1.Priority.CompareTo(bf2.Priority));
                if (i != 0)
                    return -i;
                return bf1.Name.CompareTo(bf2.Name);
            });

            foreach (BigFileV3 bf in this)
                Log.LogMsg(LogEntryType.Debug, string.Format("{0,-20}  entries: {1,5:d}  files: {2}  priority: {3,2} (0x{4:X2})", bf.Name, bf.EntryCount, bf.FileCount, bf.Priority, bf.Priority));

        }

        private void UpdateBigFiles()
        {
            foreach (BigFileV3 bigFile in this)
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
                            string.Format("{0}.{1}.{2}.{3}.txt", entry.BigFile.Name, entry.Extra.FileNameOnlyForced, entry.FileType, entry.Raw.LocaleText));
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

        internal BigFileV3 GetBigFileByPriority(uint priority, bool localized = false)
        {
            foreach (BigFileV3 bf in this)
            {
                if (bf.Priority == priority && (localized ^ (bf.Locale == FileLocale.Default)))
                    return bf;
            }
            return null;
        }

        internal bool AddTransEntry(FileEntry entry)
        {
            FatEntryKey key = new FatEntryKey(entry.Raw.Hash, entry.Raw.Locale);
            FileEntry foundEntry = null;
            if (transEntries.TryGetValue(key, out foundEntry))
            {
                if (foundEntry.BigFile.Priority < entry.BigFile.Priority)
                {
                    transEntries[key] = entry;
                    foundEntry.Status = TranslationStatus.SkippedDueUpdated;
                    Log.LogDebugMsg(string.Format("ATE: {0}.{1}.{2} discarded with file from {3}", foundEntry.BigFile.Name, entry.Extra.FileNameForced, foundEntry.Raw.LocaleText, entry.BigFile.Name));
                    return true;
                }
                Log.LogDebugMsg(string.Format("ATE: {0}.{1}.{2} kept - other {3}", foundEntry.BigFile.Name, foundEntry.Extra.FileNameForced, foundEntry.Raw.LocaleText, entry.BigFile.Name));
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
            List<FileEntry> transEntries = new List<FileEntry>();
            foreach (BigFileV3 bigFile in this)
            {
                foreach (FileEntry entry in bigFile.EntryList)
                {
                    switch (entry.FileType)
                    {
                        case FileTypeEnum.MUL_CIN:
                            {
                                if (entry.Raw.IsLocale(TRGameInfo.TransVoiceLang) || entry.Raw.IsLocale(FileLocale.Default))
                                {
                                    //CineFile.Process(entry, Stream.Null, tp)
                                    //transEntries.Add(entry);
                                }
                                break;
                            }
                        case FileTypeEnum.BIN_MNU:
                            {
                                if (entry.Raw.IsLocale(TRGameInfo.TransTextLang))
                                {
                                    //MenuFile.Process(entry, Stream.Null, tp);
                                    //transEntries.Add(entry);
                                }
                                break;
                            }
                        case FileTypeEnum.RAW_FNT:
                            {
                                //byte[] buf = entry.ReadContent();
                                //FileStream fs = new FileStream(Path.Combine(extractFolder, BigFileIdx.Name + "_font_original.raw"), FileMode.Create);
                                //try
                                //{
                                //    fs.Write(buf, 0, buf.Length);
                                //}
                                //finally
                                //{
                                //    fs.Close();
                                //}
                                break;
                            }
                        case FileTypeEnum.SCH:
                            {
                                //MovieFile.Process(entry, Stream.Null, tp);
                                //transEntries.Add(entry);
                                break;
                            }
                        case FileTypeEnum.DRM:
                            {
                                if (entry.Extra.FileName.Contains("generalbank.drm"))
                                    transEntries.Add(entry);

                                if (entry.Extra.FileName.Contains("fontuniversal.drm"))
                                    transEntries.Add(entry);
                                break;
                            }
                    }
                }
            }

            // TranslationProvider tp = new ResXExtractor(destFolder);
            //TranslationProvider tpTransSrc = new TMXProvider();
            //TranslationProvider tpTransSrc = new ResXDict(Path.Combine(TRGameInfo.Game.WorkFolder, "hu"));
            //TranslationProvider tpTransSrc = new NMSTranslationProvider(Path.Combine(TRGameInfo.Game.WorkFolder, "nemes"));


            //TranslationProvider tpTransSrc = new TMXProvider();
            TranslationProvider tpTransSrc = null;
            //tpTransSrc.Open();
            TranslationProvider tp = new TMXExtractor(Path.Combine(destFolder, "extract.tmx"), tpTransSrc);
            //tp.Open();
            int lastReported = 0;
            //BigFileIdx.EntryList.SortBy(FileEntryCompareField.FileName);

            transEntries.Sort((e1, e2) =>
            {
                int i = 0;
                i = e1.BigFile.Priority.CompareTo(e2.BigFile.Priority);
                if (i != 0)
                    return i;
                i = e1.BigFile.Name.CompareTo(e2.BigFile.Name);
                if (i != 0)
                    return i;
                return e1.Raw.Location.CompareTo(e2.Raw.Location);
            });

            for (int i = 0; i < transEntries.Count; i++)
            {
                FileEntry entry = transEntries[i];

                switch (entry.FileType)
                {
                    case FileTypeEnum.MUL_CIN:
                        {
                            CineFile.Process(entry, Stream.Null, tp);
                            break;
                        }
                    case FileTypeEnum.BIN_MNU:
                        {
                            MenuFile.Process(entry, Stream.Null, tp);
                            break;
                        }
                    case FileTypeEnum.SCH:
                        {
                            MovieFile.Process(entry, Stream.Null, tp);
                            break;
                        }
                    case FileTypeEnum.DRM:
                        {
                            FontV3.Process(entry, Stream.Null, tp);
                            break;
                        }
                }
                Int32 percent = i * 100 / transEntries.Count;
                //if (percent > lastReported)
                {
                    Log.LogProgress(string.Format("Processing {0}", entry.Extra.FileNameOnlyForced), percent);
                    lastReported = percent;
                }
            }
            //tp.Close();
            //tpTransSrc.Close();
        }

        internal void Translate(bool simulated)
        {
            Int32 lastReported = 0;
            Log.LogProgress(StaticTexts.translating, 0);

            int i = 0;
            UpdateBigFiles();

            List<FileEntry> transEntryList = new List<FileEntry>(TransEntries.Values.ToArray<FileEntry>());

            transEntryList.Sort((e1, e2) =>
            {
                int j = 0;
                j = e1.BigFile.Priority.CompareTo(e2.BigFile.Priority);
                if (j != 0)
                    return j;
                j = e1.BigFile.Name.CompareTo(e2.BigFile.Name);
                if (j != 0)
                    return j;
                return e1.Raw.Location.CompareTo(e2.Raw.Location);
            });

            foreach (FileEntry entry in transEntryList)
            {
                if (entry.Status == TranslationStatus.Translatable)
                {
                    string dumpFileName = string.Empty;
                    bool dump = false;
                    if (dump)
                    {
                        FileStream ContentStream = TRGameInfo.FilePool.Open(entry.BigFile.Name, entry.Raw.BigFileIndex);
                        try
                        {
                            dumpFileName = string.Format("{0}.{1}.{2}.{3}.txt", entry.Parent.ParentBigFile.Name, entry.Extra.FileNameOnlyForced, entry.FileType, entry.Raw.LocaleText);
                            byte[] bufRead = new byte[entry.Raw.Length];
                            ContentStream.Position = entry.Raw.Address;
                            ContentStream.Read(bufRead, 0, (int)entry.Raw.Length);
                            DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "source1", dumpFileName), bufRead);
                        }
                        finally
                        {
                            TRGameInfo.FilePool.Close(entry.BigFile.Name, entry.Raw.BigFileIndex);
                            ContentStream = null;
                        }
                    }
                }
            }

            TranslationProvider tp = new TMXProvider();
            tp.Open();

            foreach (FileEntry entry in transEntryList)
            {
                if (entry.Status == TranslationStatus.Translatable)
                {
                    switch (entry.FileType)
                    {
                        case FileTypeEnum.MUL_CIN:
                            {
                                if (entry.Raw.IsLocale(TRGameInfo.TransVoiceLang) || entry.Raw.IsLocale(FileLocale.Default))
                                {
                                    //string fileName;

                                    //fileName = Path.Combine(new string[] { TRGameInfo.Game.WorkFolder, "extract", "cine_new", "orig", entry.BigFileV3.Name, entry.Extra.FileNameForced });
                                    //if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                                    //    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                                    //DumpToFile(fileName, entry);


                                    MemoryStream ms = new MemoryStream();
                                    try
                                    {
                                        if (CineFile.Process(entry, ms, tp))
                                        {
                                            //fileName = Path.Combine(new string[] { TRGameInfo.Game.WorkFolder, "extract", "cine_new", "trans", entry.BigFileV3.Name, entry.Extra.FileNameForced });
                                            //DumpToFile(fileName, ms.ToArray());
                                            entry.BigFile.Parent.WriteFile(entry.BigFile, entry, ms.ToArray(), simulated);
                                        }
                                    }
                                    finally
                                    {
                                        ms.Close();
                                    }
                                }
                                break;
                            }
                        case FileTypeEnum.BIN_MNU:
                            {
                                MemoryStream ms = new MemoryStream();
                                try
                                {
                                    if (MenuFile.Process(entry, ms, tp))
                                    {
                                        //fileName = Path.Combine(new string[] { TRGameInfo.Game.WorkFolder, "extract", "cine_new", "trans", entry.BigFileV3.Name, entry.Extra.FileNameForced });
                                        //DumpToFile(fileName, ms.ToArray());
                                        entry.BigFile.Parent.WriteFile(entry.BigFile, entry, ms.ToArray(), simulated);
                                    }
                                }
                                finally
                                {
                                    ms.Close();
                                }
                                break;
                            }
                        case FileTypeEnum.SCH:
                            {
                                MemoryStream ms = new MemoryStream();
                                try
                                {
                                    if (MovieFile.Process(entry, ms, tp))
                                    {
                                        //fileName = Path.Combine(new string[] { TRGameInfo.Game.WorkFolder, "extract", "cine_new", "trans", entry.BigFileV3.Name, entry.Extra.FileNameForced });
                                        //DumpToFile(fileName, ms.ToArray());
                                        entry.BigFile.Parent.WriteFile(entry.BigFile, entry, ms.ToArray(), simulated);
                                    }
                                }
                                finally
                                {
                                    ms.Close();
                                }
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
            tp.Close();
            Log.LogProgress(StaticTexts.translationDone, 100);
            if (!simulated)
            {
                foreach (BigFileV3 bigFile in this)
                {
                    if (bigFile.HeaderChanged)
                        bigFile.WriteFAT();
                }
            }
            TRGameInfo.FilePool.CloseAll();
        }

        internal void GenerateFilesTxt()
        {
            UpdateBigFiles();
            foreach (BigFileV3 bigFile in this)
            {
                Int32 lastReported = 0;
                Log.LogProgress(StaticTexts.creatingFilesTxt, lastReported);

                if (TRGameInfo.Game.debugMode)
                {
                    if (!Directory.Exists(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT")))
                        Directory.CreateDirectory(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT"));


                    TextWriter twEntries = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT", "fat.entries.txt"));
                    twEntries.WriteLine("hash  location  length  Offset  lang");
                    foreach (FileEntry entry in bigFile.EntryList)
                    {
                        twEntries.WriteLine(string.Format("{0:X8} {1:X8} {2:X8} {3:X8} {4}", entry.Hash, entry.Raw.Location, entry.Raw.Length, entry.Raw.Address, entry.Raw.LocaleText));
                    }
                    twEntries.Close();

                    bigFile.EntryList.SortBy(FileEntryCompareField.Location);

                    TextWriter tw = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "FAT", "custom.files.txt"));
                    foreach (FileEntry entry in bigFile.EntryList)
                    {
                        string magic = string.Empty;
                        bool writeIt = false;

                        if (entry.Raw.IsLocale(FileLocale.Default) || entry.Raw.IsLocale(TRGameInfo.TransVoiceLang))
                        {
                            writeIt = true; // entry.FileType == FileTypeEnum.MUL_CIN;
                        }
                        if (writeIt)
                            tw.WriteLine(string.Format("{0:X8}\t{1:X8}\t{2}", entry.Hash, entry.Hash, entry.FileType.ToString(), entry.Raw.LocaleText));
                    }
                    tw.Close();
                }
            }
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

        internal static void DumpToFile(string fileName, FileEntry entry)
        {
            DumpToFile(fileName, entry.ReadContent());
        }

        internal void WriteFile(BigFileV3 bigFile, FileEntry entry, byte[] content, bool simulate)
        {
            if (simulate)
                return;
            FileStream ContentStream = null;
            FATEntry raw;

            #region prepare file write
            // try to find entry
            FileEntry foundEntry = bigFile.EntryList.Find(
                bk => bk.Hash == entry.Hash &&
                    bk.Raw.Locale == entry.Raw.Locale);

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
                    ContentStream = TRGameInfo.FilePool.Open(bigFile.Name, bigFile.FileCount - 1);
                    try
                    {
                        contentFits = ContentStream.Length.ExtendToBoundary(0x800) + content.Length <= BigFileV3.Boundary;
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
                        TRGameInfo.FilePool.Close(bigFile.Name, bigFile.FileCount - 1);
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
                FileStream LastBFStream = TRGameInfo.FilePool.Open(bigFile.Name, bigFile.FileCount - 1);
                try
                {
                    contentFits = LastBFStream.Length.ExtendToBoundary(0x800) + content.Length <= BigFileV3.Boundary;
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
                    TRGameInfo.FilePool.Close(bigFile.Name, bigFile.FileCount - 1);
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
            ContentStream = TRGameInfo.FilePool.Open(foundEntry.BigFile.Name, foundEntry.Raw.BigFileIndex);
            try
            {
                #region dump #1
                bool dump = false;
                string dumpFileName = string.Empty;
                if (dump)
                {
                    dumpFileName = string.Format("{0}.{1}.{2}.{3}.txt", entry.Parent.ParentBigFile.Name, entry.Extra.FileNameOnlyForced, entry.FileType, entry.Raw.LocaleText);
                    byte[] bufRead = new byte[entry.Raw.Length];
                    ContentStream.Position = entry.Raw.Address;
                    ContentStream.Read(bufRead, 0, (int)entry.Raw.Length);
                    DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "source2", dumpFileName), bufRead);

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
                TRGameInfo.FilePool.Close(entry.BigFile.Name, entry.Raw.BigFileIndex);
            }
            #endregion
        }

    }
}
