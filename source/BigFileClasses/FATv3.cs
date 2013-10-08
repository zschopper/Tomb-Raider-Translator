using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ExtensionMethods;

namespace TRTR
{
    struct FatEntryKey
    {
        public readonly UInt32 Hash;
        public readonly FileLanguage Language;

        public FatEntryKey(UInt32 hash, FileLanguage lang)
        {
            Hash = hash;
            Language = lang;
        }
        // Equals and GetHashCode ommitted
    }

    internal struct FATEntry
    {
        internal UInt32 Hash;
        internal UInt32 LangCode;
        internal UInt32 Length;
        internal UInt32 Location;
        internal UInt32 Index;
        internal UInt32 MaxLength;

        internal FileLanguage Language { get { return getLanguage(LangCode); } }
        internal UInt32 BigFileIndex { get { return Location & 0x0F; } }
        internal UInt32 Address { get { return Location.ShrinkToBoundary(0x800); } }
        internal BigFile BigFile { get; set; }
        // internal UInt32 Priority { get { return Location % 0x800 } }

        internal static int InfoSize = 0x10;

        internal static FileLanguage getLanguage(UInt32 langCode)
        {
            if (langCode == 0xFFFFFFFF)
                return FileLanguage.NoLang;

            if ((langCode & 1) != 0)
                return FileLanguage.English;
            if ((langCode & 1 << 0x1) != 0)
                return FileLanguage.French;
            if ((langCode & 1 << 0x2) != 0)
                return FileLanguage.German;
            if ((langCode & 1 << 0x3) != 0)
                return FileLanguage.Italian;
            if ((langCode & 1 << 0x4) != 0)
                return FileLanguage.Spanish;
            if ((langCode & 1 << 0x6) != 0)
                return FileLanguage.Portuguese;
            if ((langCode & 1 << 0x7) != 0)
                return FileLanguage.Polish;
            if ((langCode & 1 << 0x9) != 0)
                return FileLanguage.Russian;
            if ((langCode & 1 << 0xA) != 0)
                return FileLanguage.Czech;
            if ((langCode & 1 << 0xB) != 0)
                return FileLanguage.Dutch;
            if ((langCode & 1 << 0xD) != 0)
                return FileLanguage.Arabic;
            if ((langCode & 1 << 0xE) != 0)
                return FileLanguage.Korean;
            if ((langCode & 1 << 0xF) != 0)
                return FileLanguage.Chinese;

            return FileLanguage.Unknown;
        }

        internal void WriteToStream(Stream st)
        {
            BinaryWriter bw = new BinaryWriter(st);
            bw.Write(Hash);
            bw.Write(LangCode);
            bw.Write(Length);
            bw.Write(Location);
        }

        internal void ReadFromStream(Stream st)
        {
            BinaryReader br = new BinaryReader(st);
            Hash = br.ReadUInt32();
            LangCode = br.ReadUInt32();
            Length = br.ReadUInt32();
            Location = br.ReadUInt32();
            Index = 0;
        }
    }

    // all FAT entry of a bigfile/tiger
    class FATEntryList : List<FATEntry>
    {
        internal void SortByHash()
        {
            Sort(compareByHash);
        }

        internal void SortByAddress()
        {
            Sort(compareByLocation);
        }

        internal void SortByIndex()
        {
            Sort(compareByIndex);
        }

        private int compareByHash(FATEntry item1, FATEntry item2)
        {
            int cmp = item1.Hash.CompareTo(item2.Hash);
            if (cmp != 0)
                return cmp;
            return item1.LangCode.CompareTo(item2.LangCode);
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

        private int compareByIndex(FATEntry item1, FATEntry item2)
        {
            // compare by index
            return item1.Index.CompareTo(item2.Index);
        }
    }
}
