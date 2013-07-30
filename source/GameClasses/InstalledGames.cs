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

    internal enum InstallTypeEnum { Unknown, Standalone, Steam, Custom };

    // installed instance of a known game
    class GameInstance
    {

        internal class Settings
        {
            internal int LangCode = -1;
            internal string ProfileName = string.Empty;
            internal int SubLangCode = -1;
            internal string LangText
            {
                get
                {
                    if (LangCode < 0)
                    {
                        return GeneralTexts.Undefined;
                    }
                    return LangNames.Names[LangCode];
                }
            }
            internal string SubLangText
            {
                get
                {
                    if (SubLangCode < 0)
                    {
                        return GeneralTexts.Undefined;
                    }
                    return LangNames.Names[SubLangCode];
                }
            }
        }

        #region private variables
        private string workingFolderName = "trans";
        private string name;
        private InstallTypeEnum installType;
        private string installFolder;
        private string workFolder;
        private string versionString = "";
        private KnownGame gameDefaults;
        private Settings userSettings = new Settings();
        #endregion

        internal string Name { get { return name; } set { name = value; } }
        internal InstallTypeEnum InstallType { get { return installType; } set { installType = value; } }
        internal string InstallFolder { get { return installFolder; } set { installFolder = value; workFolder = Path.Combine(value, workingFolderName); } }
        internal string WorkFolder { get { return workFolder; } }
        internal string ExtractFolder { get { return Path.Combine(workFolder, "extract", versionString); } }
        internal string VersionString { get { return versionString; } set { versionString = value; } }
        internal KnownGame GameDefaults { get { return gameDefaults; } set { gameDefaults = value; } }
        internal Settings UserSettings { get { return userSettings; } }

        private void processGameCustomConfig()
        {
            try
            {
                versionString = FileVersionInfo.GetVersionInfo(Path.Combine(installFolder, gameDefaults.ExeName)).FileVersion;
            }
            catch
            {
                versionString = "N/A";
            }
            UserSettings.ProfileName = string.Empty;
            UserSettings.LangCode = -1;
            UserSettings.ProfileName = "";
            UserSettings.LangCode = 0;
            UserSettings.SubLangCode = 0;
        }

        private void processGameRegistry()
        {
            RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Crystal Dynamics\" + name);
            if (reg == null)
                reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics\" + name);

            installFolder = (string)reg.GetValue("InstallPath");
            if (installFolder == string.Empty)
                throw new Exception(Errors.InstallDirectoryNotExist);

            int version = (int)reg.GetValue("Version");
            versionString = string.Format("{0:X}.{1,00:X}", version / 0x100, version % 0x100);
            reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Crystal Dynamics\" + name);
            if (reg == null)
                reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics\" + name);

            if (reg != null)
            {
                string[] valueNames = reg.GetValueNames();
                if (Array.IndexOf<string>(valueNames, "MRUProfileName") >= 0)
                {
                    UserSettings.ProfileName = (string)reg.GetValue("MRUProfileName");
                }
                if (Array.IndexOf<string>(valueNames, "Language") >= 0)
                {
                    UserSettings.LangCode = (int)reg.GetValue("Language");
                }
                if (Array.IndexOf<string>(valueNames, "SubtitleLanguage") >= 0)
                {
                    UserSettings.SubLangCode = (int)reg.GetValue("SubtitleLanguage");
                }
            }
        }

        private void processSteamGameData()
        {

            SteamVDFDoc configVDF = new SteamVDFDoc(@"c:\Program Files (x86)\Steam\config\config.vdf");
            VDFNode gameNode = configVDF.ItemByPath(@"InstallConfigStore\Software\Valve\Steam\apps\" + gameDefaults.AppId);

            installFolder = gameNode.ChildItemByName("installdir").Value;
            try
            {
                versionString = FileVersionInfo.GetVersionInfo(Path.Combine(installFolder, gameDefaults.ExeName)).FileVersion;
            }
            catch
            {
                versionString = "N/A";
            }

            UserSettings.ProfileName = string.Empty;
            UserSettings.LangCode = -1;
            UserSettings.ProfileName = "";
            UserSettings.LangCode = 0;
            UserSettings.SubLangCode = 0;
        }

        internal void Load()
        {
            // load install type-dependent data
            switch (installType)
            {
                case InstallTypeEnum.Standalone:
                    // load game data from registry
                    processGameRegistry();
                    break;

                case InstallTypeEnum.Steam:
                    // load game data from steam source
                    processSteamGameData();
                    break;

                case InstallTypeEnum.Custom:
                    processGameCustomConfig();
                    // load game data from external config 
                    break;

                default:
                    throw new Exception(Errors.UnknownInstallType);
            }
            Log.RemoveListener("gamelog");
            Log.AddListener("gamelog", new FileLogListener(Path.Combine(workFolder, "trtr.log")));
        }

    }

    // installed game list for game selection
    static class InstalledGames
    {

        #region private variables
        private static List<GameInstance> items = new List<GameInstance>();
        #endregion

        public static List<GameInstance> Items { get { return items; } }

        static InstalledGames()
        {
            items.Clear();
            getStandaloneGames();
            getSteamGames();
            getCustomGames();
        }

        private static void getStandaloneGames()
        {
            RegistryKey reg;
            reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Crystal Dynamics");
            if (reg == null)
                reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics");

            if (reg != null)
            {
                foreach (KnownGame knownGame in KnownGames.Items)
                {
                    reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics\" + knownGame.Name);
                    if (reg != null)
                    {
                        items.Add(new GameInstance
                        {
                            Name = knownGame.Name,
                            InstallType = InstallTypeEnum.Standalone,
                            InstallFolder = reg.GetValue("InstallPath").ToString(),
                            GameDefaults = knownGame
                        });
                    }
                }
            }
        }

        private static void getSteamGames()
        {
            foreach (KnownGame knownGame in KnownGames.Items)
            {
                RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + knownGame.AppId);
                if (reg == null)
                    reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\wow6432node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + knownGame.AppId);
                if (reg != null)
                {
                    items.Add(new GameInstance
                    {
                        Name = reg.GetValue("DisplayName").ToString(),
                        InstallType = InstallTypeEnum.Steam,
                        InstallFolder = reg.GetValue("InstallLocation").ToString(),
                        GameDefaults = knownGame
                    });
                }
            }
        }

        private static void getCustomGames()
        {
            if (File.Exists(@".\games.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@".\games.xml");
                foreach (XmlNode node in doc.SelectNodes("/games/game"))
                {
                    string name = node.Attributes["name"].Value;
                    KnownGame gameFind = null;

                    foreach (KnownGame game in KnownGames.Items)
                    {
                        if (game.Name == name)
                        {
                            gameFind = game;
                            break;
                        }
                    }
                    if (gameFind != null)
                    {
                        items.Add(new GameInstance
                        {
                            Name = node.Attributes["name"].Value,
                            InstallType = InstallTypeEnum.Custom,
                            InstallFolder = node.Attributes["folder"].Value,
                            GameDefaults = gameFind
                        });
                    }
                }
            }
        }
    }
}
