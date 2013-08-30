using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;
namespace TRTR
{
    class TMXProvider : TranslationProvider
    {
        Dictionary<int, string> dict = new Dictionary<int, string>();
        string langCode = string.Empty;

        internal override void LoadTranslations()
        {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);
            dict.Clear();
            langCode = string.Empty;
            int i = 0;
            string fileName = string.Empty;
            while (langCode == string.Empty && i < cultures.Length)
            {
                string checkFileName = string.Format(".\\{0}.tmx", cultures[i].TwoLetterISOLanguageName);
                if (!File.Exists(fileName))
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

                    string replaced = source.Replace("&#13;", "\\r");//.Replace("\n", "\\n");
                    if (replaced != source)
                        Noop.DoIt();

                    source = replaced;// source.Replace("&#13;", "\\r").Replace("\n", "\\n");
                    value = value.Replace("&#13;", "\\r");//.Replace("\n", "\\n");

                    int key = source.GetHashCode();
                    string value1;
                    if (dict.TryGetValue(key, out value1))
                    {
                        Log.LogDebugMsg("Key exists.");
                        Log.LogDebugMsg(string.Format("  Key: \"{0}\"", source));
                        Log.LogDebugMsg(string.Format("  Value1: \"{0}\"", value1));
                        Log.LogDebugMsg(string.Format("  Value2: \"{0}\"", value));
                    }
                    else
                        dict.Add(key, value);
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

        internal override void Clear()
        {
            dict.Clear();
        }

        internal override string GetTranslation(string text, FileEntry entry, string[] context)
        {
            string ret = string.Empty;
            string replaced = text.Replace("\r", "");//.Replace("\r", "\\r").Replace("\n", "\\n");
            if (!dict.TryGetValue(replaced.GetHashCode(), out ret))
            {
                if (!dict.TryGetValue(replaced.Trim().GetHashCode(), out ret))
                {
//                    Log.LogDebugMsg(string.Format("No translation for \"{0}\"", text));
                    return text;
                }
            }
            return ret;

        }
    }
}
