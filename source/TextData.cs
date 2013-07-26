using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace TRTR
{
    enum TextSection { Unknown = 1, Key = 2, Value = 4 }

    struct TextData
    {
        internal string Prefix { get; set; }
        internal string Text { get; set; }
        internal string Suffix { get; set; }
        internal string Full { get { return Prefix + Text + Suffix; } }

        //        static Regex rx = new Regex(@"^([ \r\n\t]*)([^ \r\n\t].*?[^ \r\n\t])([ \r\n\t]*)$", RegexOptions.Multiline | RegexOptions.Compiled); //
        static Regex rx = new Regex(@"^([ \r\n\t]*)(.*?)([ \r\n\t]*)$", RegexOptions.Multiline | RegexOptions.Compiled); //

        internal static TextData ParseString(string text)
        {
            TextData ret = new TextData { Prefix = string.Empty, Text = string.Empty, Suffix = string.Empty };
            char[] whitespaces = { '\r', '\n', ' ', '\t' };

            int prefidx = 0;
            while ((prefidx < text.Length) && whitespaces.Contains(text[prefidx]))
                prefidx++;

            int suffidx = text.Length;
            while ((suffidx > prefidx) && whitespaces.Contains(text[suffidx - 1]))
                suffidx--;

            ret.Prefix = text.Substring(0, prefidx);
            ret.Suffix = text.Substring(suffidx, text.Length - suffidx);
            ret.Text = text.Substring(prefidx, suffidx - prefidx);

            #region regex parse
            /*
            MatchCollection matches = rx.Matches(text);
            // Log.LogDebugMsg("=====");
            // Log.LogDebugMsg(string.Format("text: \"{0}\"", text.Replace("\n", "\\n").Replace("\r", "\\r")));
            int cnt = matches.Count;
            Match[] matchlist = null;
            if (cnt > 0)
            {
                matchlist = new Match[cnt + 1];
                matches.CopyTo(matchlist, 0);

                for (int i = 0; i < cnt; i++)
                {
                    // Log.LogDebugMsg(string.Format("{0}. 1. \"{1}\" ", i, matchlist[i].Groups[1].Value.Replace("\n", "\\n").Replace("\r", "\\r")));
                    // Log.LogDebugMsg(string.Format("{0}. 2. \"{1}\" ", i, matchlist[i].Groups[2].Value.Replace("\n", "\\n").Replace("\r", "\\r")));
                    // Log.LogDebugMsg(string.Format("{0}. 3. \"{1}\" ", i, matchlist[i].Groups[3].Value.Replace("\n", "\\n").Replace("\r", "\\r")));
                    // Log.LogDebugMsg(string.Format("{0}. 4. \"{1}\" ", i, matchlist[i].Groups[4].Value.Replace("\n", "\\n").Replace("\r", "\\r")));
                    if (matchlist[i].Success)
                    {
                        if (i == 0)
                            ret.Prefix = matchlist[i].Groups[1].Value;
                        else
                            ret.Text += matchlist[i].Groups[1].Value;

                        ret.Text += matchlist[i].Groups[2].Value;

                        if (i == cnt - 1)
                            ret.Suffix = matchlist[i].Groups[3].Value;
                        else
                            if (matchlist[i].Groups[3].Value.Length > 0)
                                ret.Text += matchlist[i].Groups[3].Value + (matchlist[i].Groups[3].Value[matchlist[i].Groups[3].Value.Length - 1] != '\n' ? "\n" : "");
                    }
                }
            }
            ////else
            //    ret.Text = Text;
*/
            #endregion
            if (ret.Full != text)
            {
                Log.LogDebugMsg("=====");
                Log.LogDebugMsg(string.Format("text: \"{0}\"", text.Replace("\n", "\\n").Replace("\r", "\\r")));
                Log.LogDebugMsg(string.Format("PREF: \"{0}\"", ret.Prefix.Replace("\n", "\\n").Replace("\r", "\\r")));
                Log.LogDebugMsg(string.Format("TEXT: \"{0}\"", ret.Text.Replace("\n", "\\n").Replace("\r", "\\r")));
                Log.LogDebugMsg(string.Format("SUFF: \"{0}\"", ret.Suffix.Replace("\n", "\\n").Replace("\r", "\\r")));
                Log.LogDebugMsg(string.Format("HASH: \"{0:X8}\"", ret.Text.GetHashCode()));
                throw new Exception("hibás feldolgozás."); //xxtrans
            }
            return ret;
        }
    }

    class TextParser
    {

        #region private declarations
        //        private static Dictionary<string, TextData> dict = new Dictionary<string, TextData>();
        private static Dictionary<uint, TextData> dict2 = new Dictionary<uint, TextData>();
        private static StreamWriter dictDumpStream = null;
        private static StreamWriter dictMissingLogStream = null;
        #endregion

        static TextParser()
        {
            Update();
        }

        internal static void Update()
        {
            if (dictMissingLogStream != null)
            {
                dictMissingLogStream.Close();
                dictMissingLogStream = null;
            }
            string dictFolder = Path.Combine(TRGameInfo.Game.WorkFolder, "dict");
            if (!Directory.Exists(dictFolder))
                Directory.CreateDirectory(dictFolder);
            dictMissingLogStream = new StreamWriter(
                Path.Combine(TRGameInfo.Game.WorkFolder, "dict", "missing.txt"), false);
            dict2.Clear();  
            dictDumpStream = null;
        }

        internal static uint GenerateHash(string text)
        {
            //(uint)(data.Text.GetHashCode())
            return (uint)(string.Join("", text.Split(new char[] { '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)).GetHashCode());
        }

        internal static string GetText(string key, string info = "")
        {
            if (key.Trim() == string.Empty)
                return string.Empty;
            TextData data = TextData.ParseString(key);
            uint hash = GenerateHash(key); // (uint)(data.Text.GetHashCode());
            if (Dict2.ContainsKey(hash))
            {
                TextData ret = Dict2[hash];
                return data.Prefix + ret.Text + data.Suffix;
            }
            dict2.Add(hash, new TextData());
            Log.LogDebugMsg(string.Format("Translation not found for: ({0:X8}) \"{1}\"", hash, data.Text.Replace("\n", "\\n").Replace("\r", "\\r")));
            if (info.Length > 0)
                Log.LogDebugMsg(string.Format("  info: \"{0}\"", info));


            dictMissingLogStream.WriteLine("#" + hash.ToString());
            dictMissingLogStream.WriteLine("{ENG");
            dictMissingLogStream.WriteLine(key);
            dictMissingLogStream.WriteLine("}");
            dictMissingLogStream.WriteLine("{HUN");
            dictMissingLogStream.WriteLine("");
            dictMissingLogStream.WriteLine("}");
            dictMissingLogStream.Flush();

            return key;
        }

        //internal Dictionary<string, TextData> Dict { get { return dict; } }
        internal static Dictionary<uint, TextData> Dict2 { get { return dict2; } }

        internal static void Load(string fileName)
        {
            if (!File.Exists(fileName))
                return;
            Log.LogDebugMsg(string.Format("Parsing {0}", fileName));
            TextReader rdr = new StreamReader(fileName);
            string line = string.Empty;
            string lines = string.Empty;
            string key = string.Empty;
            string value = string.Empty;
            int i = 0;

            TextSection section = TextSection.Unknown;

            while ((line = rdr.ReadLine()) != null)
            {
                i++;
                KeyValuePair<string, TextData> last;

                switch (line)
                {
                    case "{ENG":
                        section = TextSection.Key;
                        break;
                    case "{HUN":
                        section = TextSection.Value;
                        break;
                    case "}":
                        if (section == TextSection.Key)
                        {
                            key = lines;
                            lines = string.Empty;
                        }
                        else
                        {
                            key = TextData.ParseString(key).Text;
                            if (key.Length > 0)
                            {
                                uint hash = GenerateHash(key); //(uint)(key.GetHashCode());
                                last = new KeyValuePair<string, TextData>(key, TextData.ParseString(lines));
                                if (dict2.ContainsKey(hash))
                                {
                                    Log.LogDebugMsg(string.Format("Dict duplicate key: {0:X8} \"{1}\"", hash, key));
                                }
                                else
                                {
                                    //    dict.Add(last.Key, last.Value);
                                    dict2.Add(hash, TextData.ParseString(lines));
                                    if (!Directory.Exists(TRGameInfo.Game.WorkFolder))
                                        Directory.CreateDirectory(TRGameInfo.Game.WorkFolder);
                                    if (dictDumpStream == null)
                                        dictDumpStream = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "DICT.txt"), false);
                                    dictDumpStream.WriteLine(string.Format("== hash: {0:X8}", hash));
                                    dictDumpStream.WriteLine("#" + key);
                                    dictDumpStream.WriteLine("\"" + TextData.ParseString(lines).Full + "\"\r\n");

                                }
                            }
                            lines = string.Empty;
                            section = TextSection.Unknown;
                        }

                        break;
                    default:
                        if ((section & (TextSection.Key | TextSection.Value)) != 0)
                        {
                            if (lines.Length == 0)
                                lines = line;
                            else
                                lines += "\r\n" + line;
                        }
                        break;
                }
            }
        }
    }
}
