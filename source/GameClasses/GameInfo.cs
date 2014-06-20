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
using System.Resources;
using System.Windows;

namespace TRTR
{
    public delegate void GameChangeHandler();

    // (currently) selected game
    static class TRGameInfo
    {
        #region private variables
        //private static GameChangeHandler ChangeDelegate = null;
        private static Object changeLock = new object();
        private static TRGameStatus gameStatus = TRGameStatus.None;
        private static List<string> processors = new List<string>();
        //private static XmlDocument gameDataDocument;
        private static GameInstance game = null;

        private static BigFilePool filePool = null;
        private static IBigFileList bigFiles = null;
        #endregion

        internal static TextConv Conv = new TextConv(new char[] { (char)0x0150, (char)0x0151, (char)0x0170, (char)0x0171 }, new char[] { (char)0x00D4, (char)0x00F4, (char)0x00DB, (char)0x00FB }, Encoding.UTF8);

        internal static TRGameStatus GameStatus { get { return gameStatus; } }
        internal static List<string> Processors { get { return processors; } }
        internal static GameInstance Game { get { return game; } set { game = value; } }

        internal static BigFilePool FilePool { get { return filePool; } }
        internal static IBigFileList BigFiles { get { return bigFiles; } }
        internal static FileLocale TransTextLang { get { return FileLocale.English; } }
        internal static FileLocale TransVoiceLang { get; set; }

        /*
                internal static event GameChangeHandler Change2
                {
                    add { lock (changeLock) { ChangeDelegate = (GameChangeHandler)Delegate.Combine(ChangeDelegate, value); } }
                    remove { lock (changeLock) { ChangeDelegate = (GameChangeHandler)Delegate.Remove(ChangeDelegate, value); } }
                }

                 private static void OnChange()
                {
                    if (ChangeDelegate != null)
                        ChangeDelegate();
                }
        */

        internal static void Load(GameInstance game)
        {
            TRGameInfo.Game = game;
            // load install type-dependent data

            Log.LogDebugMsg("Loading game...");
            gameStatus = game.Load();
            Log.LogDebugMsg("Game loaded.");
            Log.LogDebugMsg("Parsing files...");

            switch (game.GameDefaults.BigfileVersion)
            {
                case "2":
                    bigFiles = new BigFileListV2(game.InstallFolder);
                    break;
                case "3":
                    bigFiles = new BigFileListV3(game.InstallFolder);
                    break;
            }


            //bigFiles = new (game.BigFileClass);// IBigFileList(game.InstallFolder);
            Log.LogDebugMsg("Parsing files finished.");
            if (filePool != null)
                filePool.CloseAll();

            filePool = new BigFilePool(bigFiles);
            Log.LogDebugMsg("Load OK.");

            //OnChange();
        }

        internal static void LoadAsync(GameInstance game)
        {
            BackgroundWorker bw = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            bw.DoWork += new DoWorkEventHandler(TRGameInfo.workerLoad_DoWork);
            bw.ProgressChanged += Worker.ProgressChanged;
            bw.RunWorkerCompleted += Worker.RunWorkerCompleted;

            bw.RunWorkerAsync(game);
        }

        private static void workerLoad_DoWork(object sender, DoWorkEventArgs e)
        {
            Load((GameInstance)e.Argument);
        }

        //internal static class Trans
        //{
        //    private static XmlDocument infoDoc = null;
        //    private static string infoDocFileName;
        //    private static XmlDocument resDoc = null;
        //    private static string resDocFileName;
        //    private static XmlDocument traDoc = null;
        //    private static string traDocFileName;
        //    internal static string TransVersion { get; set; }

        //    internal static XmlDocument InfoDoc { get { return infoDoc; } set { infoDoc = value; } }

        //    internal static string InfoDocFileName { get { return infoDocFileName; } set { infoDocFileName = value; } }
        //    internal static XmlDocument RestorationDocument { get { return resDoc; } set { resDoc = value; } }
        //    internal static string RestorationDocumentFileName { get { return resDocFileName; } set { resDocFileName = value; } }

        //    internal static XmlDocument TranslationDocument { get { return traDoc; } set { traDoc = value; } }
        //    internal static string TranslationDocumentFileName { get { return traDocFileName; } set { traDocFileName = value; } }
        //    internal static string TranslationResourceDirectory { get { return string.Empty; } }
        //    internal static string TranslationSourceDirectory { get { return Path.Combine(Directory.GetCurrentDirectory(), "trans"); } }
        //}

        internal static class Worker
        {
            public static DoWorkEventHandler WorkerStart = null;
            public static ProgressChangedEventHandler ProgressChanged = null;
            public static RunWorkerCompletedEventHandler RunWorkerCompleted = null;
            public static CultureInfo CurrentCulture = null;
        }

        internal static void CreateRestorationPoint() { }

    }

    // Filename hasher algorythm
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

        internal static int MakeFileNameHash(string fileName)
        {
            int hashCode = -1; //(int)0xFFFFFFFF;

            for (int i = 0; i < fileName.Length; i++)
            {
                int ascii = (int)(fileName[i]);
                ascii <<= 0x18;
                hashCode ^= ascii;
                for (int j = 0; j < 8; j++)
                {
                    if (hashCode < 0)
                    {
                        hashCode *= 2;
                        hashCode ^= 0x4C11DB7;
                    }
                    else
                    {
                        hashCode <<= 1;
                    }
                }
            }
            unchecked
            {
                hashCode ^= (int)0xFFFFFFFF;
            }
            return hashCode;
        }

        internal static string MakeFileNameHashString(string name)
        {
            return string.Format("{0:X8}", MakeFileNameHash(name));
        }
    }

    [Flags]
    enum TRGameStatus
    {
        None = 0,
        InstallDirectoryNotExist = 1,
        DataFilesNotFound = 2,
        FileInfoFileNotFound = 4,
        TranslationDataFileNotFound = 8,
    }

    class TRGameTransInfo_REMOVED
    {
        private string version;

        #region private variables
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
        //private string folder;
        #endregion

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

        public TRGameTransInfo_REMOVED(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            //folder = Path.GetDirectoryName(fileName);
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
            moviesFileName = nodeValueDef(node, "movies/@filename", "moviesFileName", "");

            rawFontOriginal = nodeValue(node, "font/@original", "fontOriginal");
            rawFontReplaced = nodeValue(node, "font/@replaced", "fontReplaced");
            originalChars = nodeValue(node, "font/@originalChars", "originalChars").ToCharArray();
            replacedChars = nodeValue(node, "font/@replacedChars", "replacedChars").ToCharArray();

            needToReplaceChars = originalChars.Length > 0;
        }
    }
}
