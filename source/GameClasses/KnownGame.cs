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
            internal string name = string.Empty;
            internal string pattern = string.Empty;
            internal Int32 entryOfs = 0;
        }

        public string AppId;
        public string BigfileVersion;
        public string ExeName;
        public string Name;
        public KnownBigfileData[] BigFiles;
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
                AppId = "7000",
                ExeName = "TRL.exe",
                BigfileVersion = "1",
                //BigFileClass = 
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ name = "bigfile", pattern = "bigfile.{D3}", entryOfs = 0 }
                },

            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Legend",
                AppId = "7000",
                ExeName = "TRL.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ name = "bigfile", pattern = "bigfile.{D3}", entryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Anniversary demo",
                AppId = "8000",
                ExeName = "TRA.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ name = "bigfile", pattern = "bigfile.{D3}", entryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Anniversary",
                AppId = "8000",
                ExeName = "TRA.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ name = "bigfile", pattern = "bigfile.{D3}", entryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Underworld demo",
                AppId = "8140",
                ExeName = "TRU.exe",
                BigfileVersion = "1",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ name = "bigfile", pattern = "bigfile.{D3}", entryOfs = 0 }
                },
            });
            Items.Add(new KnownGame
            {
                Name = "Tomb Raider: Underworld",
                AppId = "8140",
                ExeName = "TRU.exe",
                BigfileVersion = "2",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ name = "bigfile", pattern = "bigfile.{D3}", entryOfs = 0 }
                },
            });

            Items.Clear(); // Old games disabled until app isn't compatible with it
            #endregion

            Items.Add(new KnownGame
            {
                Name = "Lara Croft and the Guardian of Light",
                AppId = "35130",
                ExeName = "LCGOL.exe",
                BigfileVersion = "2",
                BigFiles = new KnownGame.KnownBigfileData[] { 
                    new KnownGame.KnownBigfileData{ name = "bigfile", pattern = "bigfile.{D3}", entryOfs = 0 }
                },
            });

            Items.Add(new KnownGame
            {
                Name = "Tomb Raider",
                AppId = "203160", //203178 - dlc VIDEO
                ExeName = "TombRaider.exe",
                BigfileVersion = "3",
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
