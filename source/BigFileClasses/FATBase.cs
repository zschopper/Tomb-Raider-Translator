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
    public enum FileLocale : uint
    {
        Default = 0xFFFFFFFF,
        UpperWord = 0xFFFF0000,
        UnusedFlags2 = 0x00001100,//     0x1100  0001000100000000
        English = 1 << 0,       //      1 0x0001  0001000100000001
        French = 1 << 1,        //      2 0x0002  0001000100000010
        German = 1 << 2,        //      4 0x0004  0001000100000100
        Italian = 1 << 3,       //      8 0x0008  0001000100001000
        Spanish = 1 << 4,       //     16 0x0010  0001000100010000
        Japanese = 1 << 5,      //     32 0x0020  0001000100100000
        Portuguese = 1 << 6,    //     64 0x0040  0001000101000000
        Polish = 1 << 7,        //    128 0x0080  0001000110000000
        //EnglishUK = 1 << 8,   //    256 0x0100         1        
        Russian = 1 << 9,       //    512 0x0200  0001001100000000
        Czech = 1 << 10,        //   1024 0x0400  0001010100000000
        Dutch = 1 << 11,        //   2048 0x0800  0001100100000000
        //Hungarian = 1 << 12,  //   4096 0x1000     1            
        Arabic = 1 << 13,       //   8192 0x2000  0011000100000000
        Korean = 1 << 14,       //  32768 0x4000  0101000100000000
        Chinese = 1 << 15,      //  65536 0x8000  1001000100000000
        //                                             
    }

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
        void DumpToFile(string fileName);

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
    }

    public static class BigFileHelper {

        public static void DumpToFile(string fileName, byte[] content)
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

    }


    public interface IFileEntryList : IList<IFileEntry> { 
        IBigFile ParentBigFile { get; }
        List<FileEntryCompareField> CompareFields { get; }

        bool SortBy(FileEntryCompareField field);
        IFileEntry Find(Predicate<IFileEntry> match);
        
    }

}
