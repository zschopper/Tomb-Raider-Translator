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
        public readonly FileLocale Locale;

        public FatEntryKey(UInt32 hash, FileLocale locale)
        {
            Hash = hash;
            Locale = locale;
        }
        // Equals and GetHashCode ommitted
    }

    public struct FATEntry
    {
        internal UInt32 Hash;
        //private UInt32 LangCode;
        internal UInt32 Length;
        internal UInt32 Location;
        internal UInt32 Index;
        internal UInt32 MaxLength;
        internal FileLocale Locale;

        //internal FileLanguage Language { get { return getLanguage(LangCode); } }
        internal UInt32 BigFileIndex;
        internal UInt32 Address;
        internal IBigFile BigFile { get; set; }
        internal string LocaleText { get { return getLocaleText(); } }

        internal bool IsLocale(FileLocale locale) 
        {
            return (this.Locale & locale) == locale;
        }

        private string getLocaleText()
        {
            if (Locale == FileLocale.Default)
                return FileLocale.Default.ToString();

//            return (Locale ^ (FileLocale.UpperWord)).ToString(); //xx for debugging!
            return (Locale & TRGameInfo.Game.GameDefaults.Locales).ToString(); //xx for debugging!
//            return (Locale ^ (FileLocale.UpperWord | FileLocale.UnusedFlags2)).ToString();

        }
        
        // internal UInt32 Priority { get { return Location % 0x800 } }

        internal static int InfoSize = 0x10;

        private static FileLanguage getLanguage(UInt32 langCode)
        {
            if (langCode == 0xFFFFFFFF)
                return FileLanguage.NoLang;

            Dictionary<FileLanguage, int> langs = TRGameInfo.Game.GameDefaults.Langs;
            foreach (FileLanguage key in langs.Keys)
            {
                if ((langCode & 1 << langs[key]) != 0)
                    return key;
            }

            //if (langCode == 0xFFFFFFFF)
            //    return FileLanguage.NoLang;

            //if ((langCode & 1) != 0)
            //    return FileLanguage.English;
            //if ((langCode & 1 << 0x1) != 0)
            //    return FileLanguage.French;
            //if ((langCode & 1 << 0x2) != 0)
            //    return FileLanguage.German;
            //if ((langCode & 1 << 0x3) != 0)
            //    return FileLanguage.Italian;
            //if ((langCode & 1 << 0x4) != 0)
            //    return FileLanguage.Spanish;
            //if ((langCode & 1 << 0x6) != 0)
            //    return FileLanguage.Portuguese;
            //if ((langCode & 1 << 0x7) != 0)
            //    return FileLanguage.Polish;
            //if ((langCode & 1 << 0x8) != 0)
            //    return FileLanguage.UKEnglish;
            //if ((langCode & 1 << 0x9) != 0)
            //    return FileLanguage.Russian;
            //if ((langCode & 1 << 0xA) != 0)
            //    return FileLanguage.Czech;
            //if ((langCode & 1 << 0xB) != 0)
            //    return FileLanguage.Dutch;
            //if ((langCode & 1 << 0xC) != 0)
            //    return FileLanguage.Hungarian;
            //if ((langCode & 1 << 0xD) != 0)
            //    return FileLanguage.Arabic;
            //if ((langCode & 1 << 0xE) != 0)
            //    return FileLanguage.Korean;
            //if ((langCode & 1 << 0xF) != 0)
            //    return FileLanguage.Chinese;

            return FileLanguage.Unknown;
        }

        internal void WriteToStream(Stream st)
        {
            BinaryWriter bw = new BinaryWriter(st);
            bw.Write(Hash);
            bw.Write((uint)Locale);
            bw.Write(Length);
            bw.Write(Location);
        }

        internal void ReadFromStream(Stream st)
        {
            BinaryReader br = new BinaryReader(st);
            Hash = br.ReadUInt32();
            Locale = ((FileLocale)br.ReadUInt32()) & TRGameInfo.Game.GameDefaults.Locales;
            Length = br.ReadUInt32();
            Location = br.ReadUInt32();
            //BigFileIndex = { get { return Location & 0x0F; } }
            
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
            return item1.Locale.CompareTo(item2.Locale);
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
