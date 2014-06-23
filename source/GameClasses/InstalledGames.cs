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
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace TRTR
{

    internal enum InstallTypeEnum { Unknown, Standalone, Steam, Custom };

    // installed instance of a known game
    class GameTransFile
    {
        internal string Game { get; set; }
        internal string FileName { get; set; }
        internal string Version { get; set; }
        internal string Description { get; set; }
        internal Dictionary<char, char> ReplaceChars { get; set; }
        internal string TranslationLang { get; set; }
        internal string TranslationProvider { get; set; }
        internal string TranslationFile { get; set; }
        internal string TranslationVersion { get; set; }
    }

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
        private string installFolder;
        private string workFolder;
        private Settings userSettings = new Settings();
        List<GameTransFile> transFiles = new List<GameTransFile>();
        #endregion

        internal string Name { get; set; }
        internal InstallTypeEnum InstallType { get; set; }
        internal string InstallFolder { get { return installFolder; } set { setInstallFolder(value); } }

        internal string WorkFolder { get { return workFolder; } }
        internal string VersionString { get; set; }
        internal KnownGame GameDefaults { get; set; }
        internal Settings UserSettings { get { return userSettings; } }
        internal List<GameTransFile> TransFiles { get { return transFiles; } }
        internal bool debugMode = false;
        //        internal bool 

        private void setInstallFolder(string value)
        {
            installFolder = value;
            if (Directory.Exists(Path.Combine(value, workingFolderName)))
            {
                workFolder = Path.Combine(value, workingFolderName);
                debugMode = true;
            }
            else
            {
                workFolder = Path.GetFullPath(".");
            }
        }

        private void processGameCustomConfig()
        {
            try
            {
                VersionString = FileVersionInfo.GetVersionInfo(Path.Combine(installFolder, GameDefaults.ExeName)).FileVersion;
            }
            catch
            {
                VersionString = "N/A";
            }
            UserSettings.ProfileName = string.Empty;
            UserSettings.LangCode = -1;
            UserSettings.ProfileName = "";
            UserSettings.LangCode = 0;
            UserSettings.SubLangCode = 0;
        }

        private void processGameRegistry()
        {
            RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Crystal Dynamics\" + Name);
            if (reg == null)
                reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics\" + Name);

            installFolder = (string)reg.GetValue("InstallPath");
            if (installFolder == string.Empty)
                throw new Exception(Errors.InstallDirectoryNotExist);

            int version = (int)reg.GetValue("Version");
            VersionString = string.Format("{0:X}.{1,00:X}", version / 0x100, version % 0x100);
            reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Crystal Dynamics\" + Name);
            if (reg == null)
                reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics\" + Name);

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
            string steamInstallFolder = string.Empty;
            RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
            if (reg == null)
                reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam");

            if (reg == null)
                throw new Exception("Steam registry settings not found."); // xx translate/localize

            steamInstallFolder = (string)reg.GetValue("InstallPath");
            reg.Close();

            if (!Directory.Exists(steamInstallFolder))
                throw new Exception("Steam install folder is not exist."); // xx translate/localize

            //\\\Registry\HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam\InstallPath 
            SteamVDFDoc configVDF = new SteamVDFDoc(Path.Combine(steamInstallFolder, "config", "config.vdf"));

            VDFNode gameNode = configVDF.ItemByPath(@"InstallConfigStore\Software\Valve\Steam\apps\" + GameDefaults.SteamAppId);

            VDFNode installFolderNode = gameNode.ChildItemByName("installdir");
            if (installFolderNode != null)
            {
                installFolder = installFolderNode.Value;

            }
            else
            {
                VDFNode steamNode = configVDF.ItemByPath(@"InstallConfigStore\Software\Valve\Steam");
                VDFNode baseInstallFolderNode = null;
                int installFolderIdx = 0;
                installFolder = string.Empty;
                do
                {
                    installFolderIdx++;
                    baseInstallFolderNode = steamNode.ChildItemByName("BaseInstallFolder_" + installFolderIdx);
                    if (baseInstallFolderNode != null)
                    {
                        string steamFolder = baseInstallFolderNode.Value;
                        string appManifestFileName = Path.Combine(steamFolder, "steamapps", "appmanifest_" + GameDefaults.SteamAppId + ".acf");
                        if (File.Exists(appManifestFileName))
                        {
                            SteamVDFDoc appManifest = new SteamVDFDoc(appManifestFileName);
                            VDFNode appStateNode = appManifest.ItemByPath(@"AppState");
                            VDFNode installDirNode = appStateNode.ChildItemByName("installdir");
                            if (installDirNode != null)
                            {
                                installFolder = Path.Combine(steamFolder, "steamapps", "common", installDirNode.Value);
                                if (!Directory.Exists(installFolder))
                                    installFolder = string.Empty;
                            }
                        }

                    }
                } while (installFolder == string.Empty && baseInstallFolderNode != null);

            }
            try
            {
                VersionString = FileVersionInfo.GetVersionInfo(Path.Combine(installFolder, GameDefaults.ExeName)).FileVersion;
            }
            catch
            {
                VersionString = "N/A";
            }

            UserSettings.ProfileName = string.Empty;
            UserSettings.LangCode = -1;
            UserSettings.ProfileName = "";
            UserSettings.LangCode = 0;
            UserSettings.SubLangCode = 0;
        }

        internal TRGameStatus Load()
        {

            // load install type-dependent data
            switch (InstallType)
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
            preloadGameTextDataFiles();

            TRGameStatus ret = TRGameStatus.None;
            if (!Directory.Exists(installFolder))
                ret = ret | TRGameStatus.InstallDirectoryNotExist;
            if (transFiles.Count == 0)
                ret = ret | TRGameStatus.TranslationDataFileNotFound;

            Log.RemoveListener("gamelog");
            if (Directory.Exists(workFolder))
                Log.AddListener("gamelog", new FileLogListener(Path.Combine(workFolder, "trtr.log")));

            return ret;
        }

        private void preloadGameTextDataFiles()
        {
            string[] transFiles = Directory.GetFiles(this.WorkFolder, "*.trtr", SearchOption.TopDirectoryOnly);
            foreach (string fileName in transFiles)
            {
                ZipFile zipFile = new ZipFile(fileName);
                List<string> files = new List<string>();
                ZipEntry infoEntry = zipFile.GetEntry("info.xml");
                if (infoEntry != null)
                {
                    Stream zipStream = zipFile.GetInputStream(infoEntry);
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(zipStream);
                        XmlNode gameNode = doc.SelectSingleNode("/game/info");
                        if (gameNode != null)
                        {
                            if (gameNode.Attributes["name"].Value == this.Name)
                                if (gameNode.Attributes["version"].Value == this.VersionString)
                                {
                                    GameTransFile gtf = new GameTransFile();
                                    gtf.FileName = fileName;
                                    gtf.Game = gameNode.Attributes["name"].Value;
                                    gtf.Version = gameNode.Attributes["version"].Value;

                                    XmlAttribute attr = (XmlAttribute)(gameNode.Attributes.GetNamedItem("description"));
                                    if (attr != null)
                                        gtf.Description = attr.Value;
                                    else
                                        gtf.Description = string.Format("No description: {0} version {1}", gtf.Game, gtf.Version);
                                    TransFiles.Add(gtf);
                                }
                        }
                    }
                    finally
                    {
                        zipStream.Close();
                    }
                }
            }
        }

        internal void loadGameTextDataFile(GameTransFile gtf)
        {
            string fileName = gtf.FileName;
            ZipFile zipFile = new ZipFile(fileName);
            ZipEntry infoEntry = zipFile.GetEntry("info.xml");
            if (infoEntry != null)
            {
                Stream zipStream = zipFile.GetInputStream(infoEntry);
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(zipStream);
                    XmlNode gameNode = doc.SelectSingleNode("/game/info");
                    if (gameNode != null)
                    {
                        if (gameNode.Attributes["name"].Value == this.Name)
                            if (gameNode.Attributes["version"].Value == this.VersionString)
                            {
                                gtf.Game = gameNode.Attributes["name"].Value;
                                gtf.FileName = fileName;
                                gtf.Version = gameNode.Attributes["version"].Value;

                                XmlAttribute attr = (XmlAttribute)(gameNode.Attributes.GetNamedItem("description"));
                                if (attr != null)
                                    gtf.Description = attr.Value;
                                else
                                    gtf.Description = string.Format("No description: {0} version {1}", gtf.Game, gtf.Version);

                                XmlNode transNode = doc.SelectSingleNode("/game/translation");

                                gtf.TranslationLang = transNode.Attributes["lang"].Value;
                                gtf.TranslationProvider = transNode.Attributes["provider"].Value;
                                gtf.TranslationFile = transNode.Attributes["file"].Value;
                                gtf.TranslationVersion = transNode.Attributes["version"].Value;

                                XmlNodeList nodesReplace = doc.SelectNodes("/game/translation/replace");
                                if (nodesReplace.Count > 0)
                                {
                                    gtf.ReplaceChars = new Dictionary<char, char>();
                                    foreach (XmlNode node in nodesReplace)
                                    {
                                        char src = node.Attributes["src"].Value[0];
                                        char rpl = node.Attributes["rpl"].Value[0];
                                        gtf.ReplaceChars.Add(src, rpl);
                                    }
                                }
                            }
                    }
                }
                finally
                {
                    zipStream.Close();
                }
            }
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
                reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Crystal Dynamics");

            if (reg != null)
            {
                foreach (KnownGame knownGame in KnownGames.Items)
                {
                    reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Crystal Dynamics\" + knownGame.Name);
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
                RegistryKey reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + knownGame.SteamAppId);
                if (reg == null)
                    reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + knownGame.SteamAppId);
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
