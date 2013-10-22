using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

using System.Globalization;

namespace TRTR
{
    class TMXDictEntry
    {
        #region private declarations
        private int refNo = 0;
        private string text;
        #endregion
        internal string Source { get; set; }
        internal string Text { get { return getText(); } set { text = value; } }
        internal int RefNo { get { return refNo; } }

        private string getText()
        {
            refNo++;
            return text;
        }
    }

    class TMXProvider : TranslationProvider
    {
        #region private declarations
        private Dictionary<int, TMXDictEntry> dict = new Dictionary<int, TMXDictEntry>();
        private string langCode = string.Empty;
        #endregion

        internal override void Open()
        {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);
            dict.Clear();
            langCode = string.Empty;
            int i = 0;
            string fileName = string.Empty;
            while (langCode == string.Empty && i < cultures.Length)
            {
                string checkFileName = string.Format(".\\{0}.tmx", cultures[i].TwoLetterISOLanguageName);
                if (!File.Exists(checkFileName))
                    checkFileName = Path.Combine(TRGameInfo.Game.WorkFolder, cultures[i].TwoLetterISOLanguageName + ".tmx");

                if (File.Exists(checkFileName))
                {
                    langCode = cultures[i].TwoLetterISOLanguageName.ToLower();
                    fileName = checkFileName;
                }
                i++;
            }

            if (File.Exists(fileName))
            {
                XmlDocument doc = new XmlDocument();
                XmlNamespaceManager mgr = new XmlNamespaceManager(doc.NameTable);
                mgr.AddNamespace("xml", "http://www.w3.org/XML/1998/namespace");
                doc.Load(fileName);
                foreach (XmlNode node in doc.SelectNodes("/tmx/body/tu"))
                {

                    string source = node.SelectSingleNode("tuv[@xml:lang='en']/seg", mgr).InnerText;
                    string value = node.SelectSingleNode("tuv[@xml:lang='" + langCode + "']/seg", mgr).InnerText;

                    //string replaced;

                    //replaced = source.Replace("&#13;", "\\r");//.Replace("\n", "\\n");
                    //if (replaced != source)
                    //    Noop.DoIt();

                    //source = replaced;// source.Replace("&#13;", "\\r").Replace("\n", "\\n");

                    //replaced = normalizeText(source);
                    value = value.Replace("&#13;", "\r").Replace("&#10;", "\n");//.Replace("\n", "\\n");

                    int key = normalizedTextHash(source);
                    TMXDictEntry value1;
                    if (dict.TryGetValue(key, out value1))
                    {
                        if (value1.Text == value)
                            Log.LogDebugMsg("Key exists, values are matching.");
                        else
                            Log.LogDebugMsg("Key exists, values are DIFFERENT.");

                        Log.LogDebugMsg(string.Format("  Key: \"{0}\"", source));
                        if (value1.Text != value)
                        {
                            Log.LogDebugMsg(string.Format("  Value1: \"{0}\"", value1.Text));
                            Log.LogDebugMsg(string.Format("  Value2: \"{0}\"", value));
                        }
                    }
                    else
                    {
                        dict.Add(key, new TMXDictEntry { Source = source, Text = value });
                    }
                }
                if (dict.Count == 0)
                    Log.LogDebugMsg(string.Format("no loaded from \"{0}\"", fileName));
                else
                    if (dict.Count == 1)
                        Log.LogDebugMsg(string.Format("{0} translation loaded from \"{1}\"", dict.Count, fileName));
                    else
                        if (dict.Count > 1)
                            Log.LogDebugMsg(string.Format("{0} translations loaded from \"{1}\"", dict.Count, fileName));
            }
            else
            {
                Log.LogDebugMsg("No translation files found.");
                //throw new Exception("No translation files found.");
            }
        }
        internal override void Close() { DoStat(); Clear(); }

        private void DoStat()
        {

            Log.LogDebugMsg("Translation source statistics:");
            foreach (KeyValuePair<int, TMXDictEntry> dictEntry in dict)
            {
                if (dictEntry.Value.RefNo == 0)
                {
                    Log.LogDebugMsg(string.Format("Translation not used: \r\n\"{0}\"", dictEntry.Value.Source));
                }
                //else
                //{
                //    Log.LogDebugMsg(string.Format("Translation used: {0} {1}", dictEntry.Key, dictEntry.Value.RefNo));
                //}
            }
        }

        protected override bool getUseContext() { return false; }

        protected static Regex normalizeRx = new Regex("[\r\n ]+");

        protected int normalizedTextHash(string text)
        {
            return text.Replace("&#13;", "\r").Replace("&#10;", "\n").Replace("\r\n", "\n").GetHashCode();
            // return normalizeRx.Replace(text.Replace("&#13;", "\\r").Replace("&#10;", "\\n"), " ").Trim().GetHashCode();
        }

        internal override void Clear()
        {
            dict.Clear();
        }

        internal override string GetTranslation(string text, FileEntry entry, string[] context)
        {
            TMXDictEntry dictEntry = null;
            int hash = normalizedTextHash(text);
            if (!dict.TryGetValue(hash, out dictEntry))
            {
                Log.LogDebugMsg(string.Format("No translation for \"{0}\"", text));
                return text;
            }
            return dictEntry.Text;
        }
    }
}
