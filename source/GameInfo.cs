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

namespace TRTR
{
    public delegate void GameChangeHandler();

    static class TRGameInfo
    {
        internal static TextConv textConv = new TextConv(new char[0], new char[0], Encoding.UTF8);
        internal static FileLanguage OverwriteLang = FileLanguage.English; 
        
        private static List<string> processors = new List<string>();

        internal static TRGameStatus GameStatus { get { return gameStatus; } }
        private static GameChangeHandler ChangeDelegate = null;
        private static Object changeLock = new Object();
        private static TRGameStatus gameStatus = new TRGameStatus();
        internal static event GameChangeHandler Change2
        {
            add { lock (changeLock) { ChangeDelegate += value; } }
            remove { lock (changeLock) { ChangeDelegate -= value; } }
        }

        static internal void OnChange()
        {
            if (ChangeDelegate != null)
                ChangeDelegate();
        }

        static internal class Settings
        {
            internal static string ProfileName;
            internal static Int32 LangCode = -1;
            internal static string LangText { get { return LangCode >= 0 ? LangNames.Names[LangCode] : GeneralTexts.Undefined; } }
            internal static Int32 SubLangCode = -1;
            internal static string SubLangText { get { return SubLangCode >= 0 ? LangNames.Names[SubLangCode] : GeneralTexts.Undefined; } }
        }

        static internal class Trans
        {
            private static XmlDocument infoDoc = null;
            private static XmlDocument traDoc = null;
            private static XmlDocument resDoc = null;
            private static string infoDocFileName;
            private static string traDocFileName;
            private static string resDocFileName;

            internal static string TransVersion;
            internal static string InfoDocFileName { get { return infoDocFileName; } set { infoDocFileName = value; } }
            internal static string TranslationDocumentFileName { get { return traDocFileName; } set { traDocFileName = value; } }
            internal static string RestorationDocumentFileName { get { return resDocFileName; } set { resDocFileName = value; } }
            internal static XmlDocument InfoDoc { get { return infoDoc; } set { infoDoc = value; } }
            internal static XmlDocument TranslationDocument { get { return traDoc; } set { traDoc = value; } }
            internal static XmlDocument RestorationDocument { get { return resDoc; } set { resDoc = value; } }
            internal static string TranslationResourceDirectory { get { return string.Empty; } }
            internal static string TranslationSourceDirectory { get { return Path.Combine(Directory.GetCurrentDirectory(), "trans"); } }

        }

        static internal class InstallInfo
        {
            internal enum InstallTypeEnum { Unknown, Steam, Regular };

            #region private variables
            private static string installPath;
            private static string exeName;
            private static Boolean oneBigFile;
            private static InstallTypeEnum installType = InstallTypeEnum.Unknown;
            private static string gameName;
            private static string versionString;
            private static int majorVersion;
            private static int minorVersion;
            private static string dataFile;
            #endregion

            #region properties
            internal static string ExeName { get { return exeName; } set { exeName = value; } }
            internal static string GameName { get { return gameName; } set { gameName = value; } } // "Tomb Raider Legend"
            internal static string GameNameFull { get { return gameName + " " + versionString; } } // Full name with version string (eg: "Tomb Raider Legend v1.0")
            //internal static string GameNameShort { get { return gameNameAbbrev; } } // short game name (TRL for Legend)
            //internal static string GameNameAbbrev { get { return gameNameAbbrev; } } // short game name (TRL for Legend)
            //internal static string GameNameAbbrevFull { get { return gameNameAbbrev + " " + versionString; } } // short game name with version (TRL v1.0 for Legend 1.0)
            internal static Int32 MajorVersion { get { return majorVersion; } set { majorVersion = value; } }
            internal static Int32 MinorVersion { get { return minorVersion; } set { minorVersion = value; } }
            internal static string VersionString { get { return versionString; } set { versionString = value; } }
            internal static bool OneBigFile { get { return oneBigFile; } set { oneBigFile = value; } }
            internal static InstallTypeEnum InstallType { get { return installType; } set { installType = value; } }
            internal static string DataFile { get { return dataFile; } set { dataFile = value; } }


            internal static string InstallPath
            {
                get { return installPath; }
                set { installPath = FileNameUtils.IncludeTrailingBackSlash(value); }
            }

            internal static string ExePath__ { get { return Path.Combine(installPath, exeName); } }

            #endregion

        }

        internal static List<string> Processors { get { return processors; } }

        private static XmlDocument gameDataDocument;

        private static void processGameRegistry()
        {

            // process game install data
            
            RegistryKey reg;
            reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Crystal Dynamics\" + InstallInfo.GameName);
            if (reg == null)
                reg = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics\" + InstallInfo.GameName);
            if (reg == null)
                throw new Exception(Errors.CannotFoundInstalledGame);

            InstallInfo.InstallPath = (string)reg.GetValue("InstallPath"); // + ""
            if (InstallInfo.InstallPath == string.Empty)
                throw new Exception(Errors.InstallDirectoryNotExist);
            Int32 version = (Int32)reg.GetValue("Version");
            InstallInfo.MajorVersion = version / 0x100;
            InstallInfo.MinorVersion = version % 0x100;
            InstallInfo.VersionString = string.Format("{0:X}.{1,00:X}", InstallInfo.MajorVersion, InstallInfo.MinorVersion);
            Settings.ProfileName = string.Empty;
            Settings.LangCode = -1;
            // Settings.SubLangCode = -1;
            //InstallInfo.GameNameShort = string.Empty;

            // process game user settings data

            reg = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Crystal Dynamics\" + InstallInfo.GameName);
            if (reg == null)
                reg = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\wow6432node\Crystal Dynamics\" + InstallInfo.GameName);

            if (reg != null)
            {
                string[] valueNames = reg.GetValueNames();
                if (Array.IndexOf(valueNames, "MRUProfileName") >= 0)
                    Settings.ProfileName = (string)reg.GetValue("MRUProfileName");
                if (Array.IndexOf(valueNames, "Language") >= 0)
                    Settings.LangCode = (Int32)reg.GetValue("Language");
                if (Array.IndexOf(valueNames, "SubtitleLanguage") >= 0)
                    Settings.SubLangCode = (Int32)reg.GetValue("SubtitleLanguage");
            }
        }

        private static void processSteamGameData()
        {
/**/
            // process game install data

            InstallInfo.InstallPath = @"c:\Program Files (x86)\LCGOL";//"c:\tools\lcgol";
            Int32 version = 103;
            InstallInfo.MajorVersion = version / 0x100;
            InstallInfo.MinorVersion = version % 0x100;
            InstallInfo.VersionString = "1.03";
            Settings.ProfileName = string.Empty;
            Settings.LangCode = -1;
            // Settings.SubLangCode = -1;
            //InstallInfo.GameNameShort = string.Empty;

            Settings.ProfileName = "";
            Settings.LangCode = 0;
            Settings.SubLangCode = 0;
/**/
        
        }

        internal static void Load(string name)
        {
            //XmlNode n;
            XmlNode interfacesNode = null;
            XmlNode rootNode;


            string gameDataDocumentFileName = Path.GetFullPath(TextConv.CanonizeFileName(string.Format("{0}.xml", name)));
            try
            {
                if (!File.Exists(gameDataDocumentFileName))
                    throw new Exception(string.Format("Game information file ({0}) isn't exist", gameDataDocumentFileName));

                gameDataDocument = new XmlDocument();
                gameDataDocument.Load(gameDataDocumentFileName);

                rootNode = gameDataDocument.SelectSingleNode("/gamedata");

                // extract install type
                switch (rootNode.SelectSingleNodeAttrDef(@"entry[@name=""installtype""]/@value", "regular"))
                {
                    case "steam":
                        InstallInfo.InstallType = InstallInfo.InstallTypeEnum.Steam;
                        // load game data from steam source
                        processSteamGameData();
                        break;
                    case "regular":
                        InstallInfo.InstallType = InstallInfo.InstallTypeEnum.Regular;
                        // load game data from registry
                        processGameRegistry();

                        break;
                    default:
                        throw new Exception(Errors.UnknownInstallType);
                }

                // extract exename
                string exeName = rootNode.SelectSingleNodeAttrDef(@"entry[@name=""exename""]/@value", "");
                if (exeName.Length == 0)
                    throw new Exception("");
                TRGameInfo.InstallInfo.ExeName = exeName;

                // extract main datafile
                string dataFile = rootNode.SelectSingleNodeAttrDef(@"entry[@name=""datafile""]/@value", "bigfile.000");
                TRGameInfo.InstallInfo.DataFile = dataFile;
                int dummy;
                // oneBigFile = File.Exists(Path.Combine(installPath, "bigfile.dat"));
                TRGameInfo.InstallInfo.OneBigFile = !int.TryParse(Path.GetExtension(dataFile).Replace(".", ""), out dummy);


                // extract interfaces
                interfacesNode = rootNode.SelectSingleNode("interfaces");
                if (interfacesNode == null)
                    throw new Exception("");
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }



            InstallInfo.GameName = name;
            string gameName = name;

            processors.Clear();
            if (interfacesNode.SelectSingleNode(@"interface[@type=""MNU""]") != null)
                processors.Add("MNU");
            if (interfacesNode.SelectSingleNode(@"interface[@type=""CINE""]") != null)
                processors.Add("CINE");


            if (InstallInfo.InstallType == InstallInfo.InstallTypeEnum.Regular)
            {
            }


            Trans.InfoDocFileName = ".\\" + InstallInfo.GameNameFull + ".xml";
            Trans.RestorationDocumentFileName = ".\\" + InstallInfo.GameNameFull + ".res.xml";
            Trans.TranslationDocumentFileName = ".\\" + InstallInfo.GameNameFull + ".tra.xml";

            if (File.Exists(Trans.InfoDocFileName))
            {
                Trans.InfoDoc = new XmlDocument();
                Trans.InfoDoc.Load(Trans.InfoDocFileName);
            }

            if (File.Exists(Trans.TranslationDocumentFileName))
            {
                Trans.TranslationDocument = new XmlDocument();
                Trans.TranslationDocument.Load(Trans.TranslationDocumentFileName);
                XmlNode node = Trans.TranslationDocument.SelectSingleNode("/translation");
                if (node != null)
                {
                    XmlAttribute attr = node.Attributes["version"];
                    if (attr != null)
                        Trans.TransVersion = attr.Value;
                }
            }

            if (File.Exists(Trans.RestorationDocumentFileName))
            {
                Trans.RestorationDocument = new XmlDocument();
                Trans.RestorationDocument.Load(Trans.RestorationDocumentFileName);
            }

            OnChange();
        }

        internal static void LoadAsync(string name)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            //bw.DoWork += new DoWorkEventHandler(workerLoad_DoWork);
            //bw.ProgressChanged += new ProgressChangedEventHandler(Worker.ProgressChanged);
            //bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker.RunWorkerCompleted);

            bw.DoWork += workerLoad_DoWork;
            bw.ProgressChanged += Worker.ProgressChanged;
            bw.RunWorkerCompleted += Worker.RunWorkerCompleted;

            bw.RunWorkerAsync(name);
        }

        static private void workerLoad_DoWork(object sender, DoWorkEventArgs e)
        {
            Load((string)(e.Argument));
        }

        static internal class Worker
        {
            static public DoWorkEventHandler WorkerStart = null;
            static public ProgressChangedEventHandler ProgressChanged = null;
            static public RunWorkerCompletedEventHandler RunWorkerCompleted = null;
            static public CultureInfo CurrentCulture = null;
        }
    }

    internal static class Hash
    {

        private static Encoding enc = new UTF8Encoding();
        private static HashAlgorithm cr = new MD5CryptoServiceProvider();

        internal static bool Check(string text, string hash)
        {
            return hash == Get(text);
        }

        internal static string Get(string text)
        {
            return HexEncode.Encode(cr.ComputeHash(enc.GetBytes(text)));
        }

        /*
 

        #include <iostream>
        #include <string>
        using namespace std;

        const int xorvalue = 0x4C11DB7;

        void main()
        {
            string filename;
            cout << "Enter a filename: " << endl;
            cin >> filename;

            int length = filename.length();
            int hashcode = 0xFFFFFFFF;

            for(int i = 0; i < length; i++)
            {
                int ascii = (int)filename.c_str()[i];
                ascii <<= 0x18;

                hashcode ^= ascii;

                for(int j = 0; j < 8; j++)
                {
                    if(hashcode < 0)
                    {
                        hashcode *= 2;
                        hashcode ^= xorvalue;
                    }
                    else
                    {
                        hashcode <<= 1;
                    }
                }
            }

            hashcode ^= 0xFFFFFFFF;
            cout << uppercase << hex << hashcode << endl;

            system("pause");
        } 
         */
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        /*        public override unsafe int GetHashCode()
                {
                    fixed (char* str = ((char*)this))
                    {
                        char* chPtr = str;
                        int num = 0x15051505;
                        int num2 = num;
                        int* numPtr = (int*)chPtr;
                        for (int i = this.Length; i > 0; i -= 4)
                        {
                            num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
                            if (i <= 2)
                            {
                                break;
                            }
                            num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
                            numPtr += 2;
                        }
                        return (num + (num2 * 0x5d588b65));
                    }
                }
         */
    }

    [Flags]
    enum TRGameStatus
    {
        None = 0x0,
        OK = 0x1,
        InstallDirectoryNotExist = 0x2,
        DataFilesNotFound = 0x4,
        FileInfoFileNotFound = 0x8,
        TranslationDataFileNotFound = 0x10,
    }



    class TRGameTransInfo
    {
        private string version;

        private string translationVersion;
        private string langEnglishName;
        private string langLocalName;
        private string langLocale;
        private string menuFileName;
        private string subtitleFileName;
        private string moviesFileName;
        private string rawFontOriginal;
        private string rawFontReplaced;
        private char[] originalChars;
        private char[] replacedChars;
        private bool needToReplaceChars;
        private string folder;

        internal string Version { get { return version; } set { version = value; } }
        internal string TranslationVersion { get { return translationVersion; } set { translationVersion = value; } }
        internal string LangEnglishName { get { return langEnglishName; } set { langEnglishName = value; } }
        internal string LangLocalName { get { return langLocalName; } set { langLocalName = value; } }
        internal string LangLocale { get { return langLocale; } set { langLocale = value; } }
        internal string MenuFileName { get { return menuFileName; } set { menuFileName = value; } }
        internal string SubtitleFileName { get { return subtitleFileName; } set { subtitleFileName = value; } }
        internal string MoviesFileName { get { return moviesFileName; } set { moviesFileName = value; } }
        internal string RawFontOriginal { get { return rawFontOriginal; } set { rawFontOriginal = value; } }
        internal string RawFontReplaced { get { return rawFontReplaced; } set { rawFontReplaced = value; } }
        internal char[] OriginalChars { get { return originalChars; } set { originalChars = value; } }
        internal char[] ReplacedChars { get { return replacedChars; } set { replacedChars = value; } }
        // pre-calculated fields
        internal bool NeedToReplaceChars { get { return needToReplaceChars; } set { needToReplaceChars = value; } }

        public TRGameTransInfo(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            folder = FileNameUtils.IncludeTrailingBackSlash(FileNameUtils.FilePath(fileName));
            doc.Load(fileName);
            Parse(doc);
        }

        private static string nodeValue(XmlNode node, string path, string msg)
        {
            XmlAttribute attr = (XmlAttribute)node.SelectSingleNode(path);
            if (attr == null)
                throw new Exception(string.Format("The \"{0}\" attribute isn't defined in trans_info.xml", msg));
            return attr.Value;
        }

        private string nodeValueDef(XmlNode node, string path, string msg, string def)
        {
            XmlAttribute attr = (XmlAttribute)node.SelectSingleNode(path);
            if (attr == null)
                return def;
            return attr.Value;
        }

        private void Parse(XmlDocument doc)
        {

            XmlNode node = doc.SelectSingleNode("/info");
            version = node.SelectSingleNode("@version").Value;

            //            translationVersion = nodeValue(node, "translation/@version", "translationVersion");
            langEnglishName = nodeValue(node, "language/@english", "langEnglishName");
            langLocalName = nodeValue(node, "language/@local", "langLocalName");
            langLocale = nodeValue(node, "language/@locale", "langLocale");

            menuFileName = nodeValue(node, "menu/@filename", "menuFileName");
            subtitleFileName = nodeValue(node, "subtitle/@filename", "subtitleFileName");
            moviesFileName = nodeValue(node, "movies/@filename", "moviesFileName");

            rawFontOriginal = nodeValue(node, "font/@original", "fontOriginal");
            rawFontReplaced = nodeValue(node, "font/@replaced", "fontReplaced");
            originalChars = nodeValue(node, "font/@originalChars", "originalChars").ToCharArray();
            replacedChars = nodeValue(node, "font/@replacedChars", "replacedChars").ToCharArray();

            needToReplaceChars = originalChars.Length > 0;
        }
    }

}
