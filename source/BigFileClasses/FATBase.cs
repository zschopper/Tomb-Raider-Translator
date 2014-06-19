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

    public enum FileEntryCompareField
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
    public enum TranslationStatus
    {
        NotTranslatable,
        Translatable,
        SkippedDueUpdated
    }

    [Flags]
    public enum FileTypeEnum
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

    #endregion

    public interface IBigFile
    {
        // header data
        byte[] Magic { get; }
        UInt32 Version { get; }
        UInt32 FileCount { get; set; }
        Int32 EntryCount { get; set; }
        UInt32 Priority { get; }
        byte[] PathPrefix { get; }
        UInt32 HeaderSize { get; }
        bool HeaderChanged { get; set; }

        // Filename-related data
        string Name { get; }
        string FilePattern { get; }
        string FilePatternFull { get; }
        string RelPath { get; }

        IFileEntryList EntryList { get; }
        IBigFileList Parent { get; }
        FileLocale Locale { get; }
        string NeutralName { get; }

        //bool LoadedCompletely = false;

        void ReadFAT();
        void WriteFAT();
    }

    public interface IFileExtraInfo {
        bool FileNameResolved { get; set; }
        string FileName { get; set; }
        string FileNameOnly { get; }
        string FileNameOnlyForced { get; }
        string FileNameForced { get; }

    }

    public interface IFileEntry {
        FATEntry Raw { get; set; }
        IBigFile BigFile { get; }

        UInt32 Hash { get; }
        string HashText { get; }

        IFileExtraInfo Extra { get; }
        FileTypeEnum FileType { get; set; }

        Int64 Offset { get; }
        IFileEntryList Parent { get; }
        UInt32 OriginalIndex { get; }
        UInt32 VirtualSize { get; set; }

        TranslationStatus Status { get; set; }

        Int32 CompareTo(IFileEntry other);
        Int32 CompareTo(IFileEntry other, Int32 level);

        byte[] ReadContent();
    }

    public interface IBigFileList
    {
        Dictionary<string, IBigFile> ItemsByName { get; }
        Dictionary<uint, string> FileNameHashDict { get; }
        
        bool AddTransEntry(IFileEntry entry);
        void Extract(string destFolder, bool useDict);
        void Translate(bool simulated);
        void GenerateFilesTxt();

        IBigFile GetBigFileByPriority(uint priority, bool localized = false);
        void WriteFile(IBigFile bigFile, IFileEntry entry, byte[] content, bool simulate);
        void DumpToFile(string fileName, byte[] content);
        void DumpToFile(string fileName, IFileEntry entry);

    }


    public interface IFileEntryList : IList<IFileEntry> { 
        IBigFile ParentBigFile { get; }
        List<FileEntryCompareField> CompareFields { get; }

        bool SortBy(FileEntryCompareField field);
        IFileEntry Find(Predicate<IFileEntry> match);
        
    }

}
