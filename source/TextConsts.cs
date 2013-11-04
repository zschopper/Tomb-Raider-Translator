using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Windows.Forms;
using System.Threading;
using System.Xml;
using System.IO;

//using System.Management;
using System.Management.Instrumentation;
using System.Resources;
using System.Reflection;


namespace TRTR
{

    enum FileLanguage
    {
        Default = -1,
        English = 0,
        French = 1,
        German = 2,
        Italian = 3,
        Spanish = 4,
        Japanese = 5,
        Portuguese = 6,
        Polish = 7,
        UKEnglish = 8,
        Russian = 9,
        Czech = 10,
        Dutch = 11, // v1.0.0.6
        Hungarian = 12, // v1.0.0.6
        Croatian = 13, // v1.0.0.6
        Arabic = 14,
        Korean = 15,
        Chinese = 16,
        NoLang = 0xF0,
        Unknown = 0xFF
    }

    static class TransConsts
    {
        internal static char MenuEntryHeader = '@';
        internal static string SubTitleEntryHeader = "HASH:";
        internal static char CommentPrefix = ';';
        internal static char OriginalPrefix = '#';
        internal static char DirectivePrefix = '$';
    }

    static class LangNames
    {
        private static Dictionary<FileLanguage, string> locStrings = new Dictionary<FileLanguage, string>();
        private static Dictionary<FileLanguage, string> dict;

        internal static string Default = "Default";
        internal static string English = "English";
        internal static string French = "French";
        internal static string German = "German";
        internal static string Italian = "Italian";
        internal static string Spanish = "Spanish";
        internal static string Japanese = "Japanese"; // 2013-02-21 LCGoL?
        internal static string Portuguese = "Portuguese";
        internal static string Polish = "Polish";
        internal static string UKEnglish = "UK English";
        internal static string Russian = "Russian";
        internal static string Czech = "Czech";
        internal static string Dutch = "Dutch"; // v1.0.0.6
        internal static string Hungarian = "Hungarian"; // v1.0.0.6
        internal static string Croatian = "Croatian"; // v1.0.0.6
        internal static string Arabic = "Arabic";
        internal static string Korean = "Korean";
        internal static string Chinese = "Chinese";
        internal static string NoLang = "No language";
        internal static string Unknown = GeneralTexts.Unknown;

        internal static string[] Names = {Default, English, French, German, Italian, Spanish, 
                                        Japanese, Portuguese, Polish, UKEnglish, Russian, 
                                        Czech, Dutch, Hungarian, Croatian, NoLang, Unknown}; // v1.0.0.6

        internal static string LocalizedDefault { get { return ResTexts.GetValue("LangDefault"); } }
        internal static string LocalizedEnglish { get { return ResTexts.GetValue("LangEnglish"); } }
        internal static string LocalizedFrench { get { return ResTexts.GetValue("LangFrench"); } }
        internal static string LocalizedGerman { get { return ResTexts.GetValue("LangGerman"); } }
        internal static string LocalizedItalian { get { return ResTexts.GetValue("LangItalian"); } }
        internal static string LocalizedSpanish { get { return ResTexts.GetValue("LangSpanish"); } }
        internal static string LocalizedJapanese { get { return ResTexts.GetValue("Japanese"); } } // 2013-02-21 lcgol?
        internal static string LocalizedPortuguese { get { return ResTexts.GetValue("LangPortuguese"); } }
        internal static string LocalizedPolish { get { return ResTexts.GetValue("LangPolish"); } }
        internal static string LocalizedUKEnglish { get { return ResTexts.GetValue("LangUKEnglish"); } }
        internal static string LocalizedRussian { get { return ResTexts.GetValue("LangRussian"); } }
        internal static string LocalizedCzech { get { return ResTexts.GetValue("LangCzech"); } }
        internal static string LocalizedDutch { get { return ResTexts.GetValue("LangDutch"); } } // v1.0.0.6
        internal static string LocalizedHungarian { get { return ResTexts.GetValue("LangHungarian"); } } // v1.0.0.6
        internal static string LocalizedCroatian { get { return ResTexts.GetValue("LangCroatian"); } } // v1.0.0.6
        internal static string LocalizedNoLang { get { return ResTexts.GetValue("LangNoLang"); } }
        internal static string LocalizedArabic { get { return ResTexts.GetValue("LangArabic"); } }


        static internal Dictionary<FileLanguage, string> Dict { get { return dict; } }

        static internal string FromCode(Int32 value) { return dict[(FileLanguage)value]; }

        static LangNames()
        {
            dict = new Dictionary<FileLanguage, string>();
            Refresh();
        }

        internal static void Refresh()
        {
            dict.Clear();
            dict.Add(FileLanguage.Default, Default);
            dict.Add(FileLanguage.English, English);
            dict.Add(FileLanguage.French, French);
            dict.Add(FileLanguage.German, German);
            dict.Add(FileLanguage.Italian, Italian);
            dict.Add(FileLanguage.Spanish, Spanish);
            dict.Add(FileLanguage.Japanese, Japanese); // lcgol?
            dict.Add(FileLanguage.Portuguese, Portuguese);
            dict.Add(FileLanguage.Polish, Polish);
            dict.Add(FileLanguage.UKEnglish, UKEnglish);
            dict.Add(FileLanguage.Russian, Russian);
            dict.Add(FileLanguage.Czech, Czech);
            dict.Add(FileLanguage.Dutch, Dutch); // v1.0.0.6
            dict.Add(FileLanguage.Hungarian, Hungarian); // v1.0.0.6
            dict.Add(FileLanguage.Croatian, Croatian); // v1.0.0.6
            dict.Add(FileLanguage.Arabic, Arabic);
            dict.Add(FileLanguage.Korean, Korean);
            dict.Add(FileLanguage.Chinese, Chinese);
            dict.Add(FileLanguage.NoLang, NoLang);
            dict.Add(FileLanguage.Unknown, Unknown);

            locStrings.Add(FileLanguage.Default, "LangDefault");
            locStrings.Add(FileLanguage.English, "LangEnglish");
            locStrings.Add(FileLanguage.French, "LangFrench");
            locStrings.Add(FileLanguage.German, "LangGerman");
            locStrings.Add(FileLanguage.Italian, "LangItalian");
            locStrings.Add(FileLanguage.Spanish, "LangSpanish");
            locStrings.Add(FileLanguage.Japanese, "LangJapanese"); // v1.0.0.6
            locStrings.Add(FileLanguage.Portuguese, "LangPortuguese");
            locStrings.Add(FileLanguage.Polish, "LangPolish");
            locStrings.Add(FileLanguage.UKEnglish, "LangUKEnglish");
            locStrings.Add(FileLanguage.Russian, "LangRussian");
            locStrings.Add(FileLanguage.Czech, "LangCzech");
            locStrings.Add(FileLanguage.Dutch, "LangDutch"); // v1.0.0.6
            locStrings.Add(FileLanguage.Hungarian, "LangHungarian"); // v1.0.0.6
            locStrings.Add(FileLanguage.Croatian, "LangCroatian"); // v1.0.0.6
            locStrings.Add(FileLanguage.Arabic, "LangArabic");
            locStrings.Add(FileLanguage.Korean, "LangKorean");
            locStrings.Add(FileLanguage.Chinese, "LangChinese");
            locStrings.Add(FileLanguage.NoLang, "LangNoLang");
            locStrings.Add(FileLanguage.Unknown, "LangUnknown");
        }
        internal static string Localized(FileLanguage lang)
        {
            return ResTexts.GetValue(locStrings[lang]);
        }

    }

    static internal class GeneralTexts
    {
        static internal string Ok { get { return ResTexts.GetValue("Ok"); } }
        static internal string Version { get { ResTexts.Refresh(); return ResTexts.GetValue("Version"); } }
        static internal string Undefined { get { return ResTexts.GetValue("Undefined"); } }
        static internal string Unknown { get { return ResTexts.GetValue("Unknown"); } }
        static internal string None { get { return ResTexts.GetValue("None"); } }
    }

    static class Errors
    {
        static internal string CorruptedTranslation { get { return ResTexts.GetValue("CorruptedTranslation"); } }
        static internal string CorruptedRestoration { get { return ResTexts.GetValue("CorruptedRestoration"); } }
        static internal string CorruptedFileInfo { get { return ResTexts.GetValue("CorruptedFileInfo"); } }
        static internal string MissingFileInfo { get { return ResTexts.GetValue("MissingFileInfo"); } }
        static internal string InvalidParameter { get { return ResTexts.GetValue("InvalidParameter"); } }
        static internal string ParseErrorTooBig { get { return ResTexts.GetValue("ParseErrorTooBig"); } }
        static internal string ParseErrorSizeMismatch { get { return ResTexts.GetValue("ParseErrorSizeMismatch"); } }
        static internal string ParseErrorBlockTypeError { get { return ResTexts.GetValue("ParseErrorBlockTypeError"); } }
        static internal string GameIsNotSelected { get { return ResTexts.GetValue("GameIsNotSelected"); } }
        static internal string InvalidLanguageCode { get { return ResTexts.GetValue("InvalidLanguageCode"); } }
        static internal string InvalidSortMode { get { return ResTexts.GetValue("InvalidSortMode"); } }
        static internal string InvalidStoredFileInfo { get { return ResTexts.GetValue("InvalidStoredFileInfo"); } }
        static internal string NewFileIsTooBig { get { return ResTexts.GetValue("NewFileIsTooBig"); } }
        static internal string TranslatingUninitializedBlock { get { return ResTexts.GetValue("TranslatingUninitializedBlock"); } }
        static internal string TranslationInformationsNotInitialized { get { return ResTexts.GetValue("TranslationInformationsNotInitialized"); } }
        static internal string InvalidTextReplaceSettings { get { return ResTexts.GetValue("InvalidTextReplaceSettings"); } }
        static internal string InstallDirectoryNotExist { get { return ResTexts.GetValue("InstallDirectoryNotExist"); } }
        static internal string DataFilesNotFound { get { return ResTexts.GetValue("DataFilesNotFound"); } }
        static internal string TranslationDataFileNotFound { get { return ResTexts.GetValue("TranslationDataFileNotFound"); } }
        static internal string FATEntryCountError { get { return ResTexts.GetValue("FATEntryCountError"); } }
        static internal string FileInfoFileNotFound { get { return ResTexts.GetValue("FileInfoFileNotFound"); } }
        static internal string CannotFoundInstalledGame { get { return ResTexts.GetValue("CannotFoundInstalledGame"); } }
        static internal string InitializationError { get { return ResTexts.GetValue("InitializationError"); } }
        static internal string ParseError { get { return ResTexts.GetValue("ParseError"); } }
        static internal string UnknownInstallType { get { return ResTexts.GetValue("UnknownInstallType"); } }
    }

    static class StaticTexts
    {
        internal static string buttonRestore { get { return ResTexts.GetValue("buttonRestore"); } }
        internal static string buttonTranslate { get { return ResTexts.GetValue("buttonTranslate"); } }
        internal static string groupBoxGameInfo { get { return ResTexts.GetValue("groupBoxGameInfo"); } }
        internal static string labelAvailableTranslationVersionText { get { return ResTexts.GetValue("labelAvailableTranslationVersionText"); } }
        internal static string labelExtra { get { return ResTexts.GetValue("labelExtra"); } }
        internal static string labelGame { get { return ResTexts.GetValue("labelGame"); } }
        internal static string labelInstalledVersionText { get { return ResTexts.GetValue("labelInstalledVersionText"); } }
        internal static string labelInstallPathText { get { return ResTexts.GetValue("labelInstallPathText"); } }
        internal static string labelLang { get { return ResTexts.GetValue("labelLang"); } }
        internal static string labelLastUsedProfileText { get { return ResTexts.GetValue("labelLastUsedProfileText"); } }
        internal static string labelProgress { get { return ResTexts.GetValue("labelProgress"); } }
        internal static string labelSelectedLanguageText { get { return ResTexts.GetValue("labelSelectedLanguageText"); } }
        internal static string labelSelectedSubtitleLanguageText { get { return ResTexts.GetValue("labelSelectedSubtitleLanguageText"); } }
        internal static string labelVersion { get { return ResTexts.GetValue("labelVersion"); } }
        internal static string linkLabel1 { get { return ResTexts.GetValue("linkLabel1"); } }
        internal static string menuItemCompileTexts { get { return ResTexts.GetValue("menuItemCompileTexts"); } }
        internal static string menuItemCreateRestoration { get { return ResTexts.GetValue("menuItemCreateRestoration"); } }
        internal static string menuItemExtractTexts { get { return ResTexts.GetValue("menuItemExtractTexts"); } }
        internal static string menuItemLangEN { get { return ResTexts.GetValue("menuItemLangEN"); } }
        internal static string menuItemLangHU { get { return ResTexts.GetValue("menuItemLangHU"); } }
        internal static string menuItemRunGame { get { return ResTexts.GetValue("menuItemRunGame"); } }
        internal static string menuItemRunGameWithConfiguration { get { return ResTexts.GetValue("menuItemRunGameWithConfiguration"); } }
        internal static string menuItemSimulateRestoration { get { return ResTexts.GetValue("menuItemSimulateRestoration"); } }
        internal static string menuItemSimulateTranslation { get { return ResTexts.GetValue("menuItemSimulateTranslation"); } }
        internal static string menuItemConvertOldTranslationsToNewFormat { get { return ResTexts.GetValue("menuItemConvertOldTranslationsToNewFormat"); } }

        internal static string translating { get { return ResTexts.GetValue("Translating"); } }
        internal static string translationDone { get { return ResTexts.GetValue("TranslationDone"); } }
        internal static string restoring { get { return ResTexts.GetValue("Restoring"); } }
        internal static string restorationDone { get { return ResTexts.GetValue("RestorationDone"); } }
        internal static string creatingRestorationPoint { get { return ResTexts.GetValue("CreatingRestorationPoint"); } }
        internal static string creatingRestorationPointDone { get { return ResTexts.GetValue("CreatingRestorationPointDone"); } }
        internal static string error { get { return ResTexts.GetValue("Error"); } }
        internal static string details { get { return ResTexts.GetValue("Details"); } }
        internal static string dialogDetails { get { return ResTexts.GetValue("DialogDetails"); } }
        internal static string copyDetails { get { return ResTexts.GetValue("CopyDetails"); } }
        internal static string creatingFilesTxt { get { return ResTexts.GetValue("CreatingFilesTxt"); } }
    }

    static class DebugErrors // do not translate
    {
        internal static string FATUpdateLocationError = "(Internal) FAT update error: Location isn't match (Hash: {0:x8})";
        internal static string FATParseError = "(Internal) FAT parse error: Virtual size of an Entry isn't determined (Hash: {0:x8})";
    }

    static class ResTexts
    {
        private static XmlDocument doc = new XmlDocument();
        private static bool initialized = false;
        static ResTexts()
        {
            // Refresh();
        }

        internal static void Refresh()
        {
            string fileName = string.Format("lang-{0}.xml", Application.CurrentCulture.Name);
            string fileNameEN = "lang-en-GB.xml";

            Assembly thisExe = Assembly.GetExecutingAssembly();
            string path = thisExe.GetName().Name + ".Resources.";
            Stream lng = thisExe.GetManifestResourceStream(path + fileName);
            if (lng == null)
                lng = thisExe.GetManifestResourceStream(path + fileNameEN);
            doc.Load(lng);
            initialized = true;
        }

        internal static string GetValue(string key)
        {
            if (!initialized)
                Refresh();
            XmlNode attr = doc.SelectSingleNode(string.Format("/translation/*/text[@key=\"{0}\"]/@value", key));
            string value = key;
            if (attr != null)
            {
                if (attr.Value.Length == 0)
                    throw new Exception(string.Format("No key for {0}", key));
                return attr.Value;
            }
            return string.Empty;
        }

        internal static string GetString(string key)
        {
            return GetValue(key);
        }
    }
}
