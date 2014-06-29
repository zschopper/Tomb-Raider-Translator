using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using System.Xml;
using System.Security.Cryptography;
using System.Globalization;
using ExtensionMethods;
using System.Diagnostics;

namespace TRTR
{
    /*
     * PREDEFINED GAME CONSTANTS
     */

    internal class KnownGame
    {
        internal class KnownBigfileData
        {
            public static int DefaultPriority = -1;

            internal string Name = string.Empty;
            internal string Pattern = string.Empty;
            internal Int32 EntryOfs = 0;
            public int Priority = KnownBigfileData.DefaultPriority;
        }

        public string SteamAppId;
        public string BigfileVersion;
        public string ExeName;
        public string Name;
        public KnownBigfileData[] BigFiles;
        public Dictionary<FileLanguage, Int32> Langs;
        public FileLocale Locales;
        //public Dictionary<char, string> MenuPlaceholderChars = null;
        public List<char> MenuPlaceholderChars = null;
    }

    internal static class KnownGames
    {
        public static List<KnownGame> Items = new List<KnownGame>();

        static KnownGames()
        {
            #region hidden known games
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Legend demo",
                SteamAppId = "7000",
                ExeName = "TRL.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ Name = "bigfile", Pattern = "bigfile.{D3}", EntryOfs = 0 }
                },

            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Legend",
                SteamAppId = "7000",
                ExeName = "TRL.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ Name = "bigfile", Pattern = "bigfile.{D3}", EntryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Anniversary demo",
                SteamAppId = "8000",
                ExeName = "TRA.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ Name = "bigfile", Pattern = "bigfile.{D3}", EntryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Anniversary",
                SteamAppId = "8000",
                ExeName = "TRA.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ Name = "bigfile", Pattern = "bigfile.{D3}", EntryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Underworld demo",
                SteamAppId = "8140",
                ExeName = "TRU.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ Name = "bigfile", Pattern = "bigfile.{D3}", EntryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Underworld",
                SteamAppId = "8140",
                ExeName = "TRU.exe",
                BigfileVersion = "2",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ Name = "bigfile", Pattern = "bigfile.{D3}", EntryOfs = 0 }
                },
            });

            Items.Clear(); // Old games disabled until app isn't compatible with it
            #endregion

            Items.Add(new KnownGame
            {
                Name = "Lara Croft and the Guardian of Light",
                SteamAppId = "35130",
                ExeName = "LCGOL.exe",
                BigfileVersion = "2",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ Name = "bigfile", Pattern = "bigfile.{D3}", EntryOfs = 0, Priority = 0 },
                    new KnownGame.KnownBigfileData{ Name = "patch", Pattern = "patch.{D3}", EntryOfs = 0, Priority = 65 },
                    new KnownGame.KnownBigfileData{ Name = "pack1", Pattern = "pack1.{D3}", EntryOfs = 0, Priority = 1 },
                    new KnownGame.KnownBigfileData{ Name = "pack2", Pattern = "pack2.{D3}", EntryOfs = 0, Priority = 2 },
                    new KnownGame.KnownBigfileData{ Name = "pack3", Pattern = "pack3.{D3}", EntryOfs = 0, Priority = 3 },
                    new KnownGame.KnownBigfileData{ Name = "pack4", Pattern = "pack4.{D3}", EntryOfs = 0, Priority = 4 },
                    new KnownGame.KnownBigfileData{ Name = "pack5", Pattern = "pack5.{D3}", EntryOfs = 0, Priority = 5 },
                },
                Langs = new Dictionary<FileLanguage, Int32>() {
                    { FileLanguage.Default, -1 },
                    { FileLanguage.English, 1 << 0 },       //      1 0x0001  0001000100000001
                    { FileLanguage.French, 1 << 1 },        //      2 0x0002  0001000100000010
                    { FileLanguage.German, 1 << 2 },        //      4 0x0004  0001000100000100
                    { FileLanguage.Italian, 1 << 3 },       //      8 0x0008  0001000100001000
                    { FileLanguage.Spanish, 1 << 4 },       //      0 0x0000  0001000100010000
                },
                Locales = FileLocale.English | FileLocale.French | FileLocale.German | FileLocale.Italian | FileLocale.Spanish,
                MenuPlaceholderChars = new List<char> { 
                    '\u1800', '\u1801', '\u1802', '\u1803', '\u1804', '\u1805', '\u1806', '\u1807', 
                    '\u1808', '\u1809', '\u180A', '\u180B', '\u180C', '\u180D', '\u180F', 
                    '\u1810', '\u1811',
                    '\u1827', '\u1828',
                    '\u182A', '\u182B', '\u182C'},
                //MenuPlaceholderChars = new Dictionary<char, string> {
                //    {'\u1800', "{P1}"}, 
                //    {'\u1801', "{P2}"}, 
                //    {'\u1803', "{P3}"}, 
                //    {'\u1808', "{P4}"}, 
                //    {'\u1809', "{P5}"}, 
                //    {'\u180A', "{P6}"}, 
                //    {'\u180B', "{P7}"}, 
                //    {'\u182A', "{P8}"}, 
                //     },
            });

            Items.Add(new KnownGame
            {
                Name = "Tomb Raider",
                SteamAppId = "203160", //203178 - dlc VIDEO
                ExeName = "TombRaider.exe",
                BigfileVersion = "3",
                Langs = new Dictionary<FileLanguage, Int32>() {
                    { FileLanguage.Default, -1 },
                    { FileLanguage.English, 1 << 0 },       //      1 0x0001  0001000100000001
                    { FileLanguage.French, 1 << 1 },        //      2 0x0002  0001000100000010
                    { FileLanguage.German, 1 << 2 },        //      4 0x0004  0001000100000100
                    { FileLanguage.Italian, 1 << 3 },       //      8 0x0008  0001000100001000
                    { FileLanguage.Spanish, 1 << 4 },       //     16 0x0010  0001000100010000
                    { FileLanguage.Japanese, 1 << 5 },      //     32 0x0020  0001000100100000
                    { FileLanguage.Portuguese, 1 << 6 },    //     64 0x0040  0001000101000000
                    { FileLanguage.Polish, 1 << 7 },        //    128 0x0080  0001000110000000
                  //{ FileLanguage.EnglishUK, 1 << 8 },     //    256 0x0100         1        
                    { FileLanguage.Russian, 1 << 9 },       //    512 0x0200  0001001100000000
                    { FileLanguage.Czech, 1 << 10 },        //   1024 0x0400  0001010100000000
                    { FileLanguage.Dutch, 1 << 11 },        //   2048 0x0800  0001100100000000
                  //{ FileLanguage.Hungarian, 1 << 12 },    //   4096 0x1000     1            
                    { FileLanguage.Arabic, 1 << 13 },       //   8192 0x2000  0011000100000000
                    { FileLanguage.Korean, 1 << 14 },       //  32768 0x4000  0101000100000000
                    { FileLanguage.Chinese, 1 << 15 },      //  65536 0x8000  1001000100000000
                },
                Locales = FileLocale.English | FileLocale.French | FileLocale.German | FileLocale.Italian | FileLocale.Spanish | 
                    FileLocale.Japanese | FileLocale.Portuguese | FileLocale.Polish | FileLocale.Russian | FileLocale.Czech | 
                    FileLocale.Dutch | FileLocale.Arabic | FileLocale.Korean | FileLocale.Chinese,

                //BigFiles = new KnownGame.KnownBigfileData[] { 
                //    new KnownGame.KnownBigfileData{ name = "bigfile",     pattern = "bigfile.{0:D3}.tiger", entryOfs = 0 },
                //    new KnownGame.KnownBigfileData{ name = "bigfile_loc", pattern = "bigfile_ENGLISH.{0:D3}.tiger", entryOfs = 0 },
                //    new KnownGame.KnownBigfileData{ name = "patch", pattern = "patch.{0:D3}.tiger", entryOfs = 0 },
                //    new KnownGame.KnownBigfileData{ name = "patch_loc", pattern = "patch_ENGLISH.{0:D3}.tiger", entryOfs = 0 },
                //    new KnownGame.KnownBigfileData{ name = "patch2", pattern = "patch2.{0:D3}.tiger", entryOfs = 0 },
                //    new KnownGame.KnownBigfileData{ name = "patch2_loc", pattern = "patch2_ENGLISH.{0:D3}.tiger", entryOfs = 0 },
                //    new KnownGame.KnownBigfileData{ name = "title", pattern = "title.{0:D3}.tiger", entryOfs = 0 },
                //    new KnownGame.KnownBigfileData{ name = "title_loc", pattern = "title_ENGLISH.{0:D3}.tiger", entryOfs = 0 },
                //},
            });
        }
    }
}
