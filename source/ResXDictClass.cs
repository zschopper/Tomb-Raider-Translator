using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace TRTR
{
    class ResXDictEntry
    {
        internal int SourceHash { get; set; }
        internal int TranslationHash { get; set; }
        internal string SourceText { get; set; }
        internal string Translation { get; set; }
        internal string FileName { get; set; }
        internal Dictionary<string, string> Values = new Dictionary<string, string>();

        internal ResXDictEntry(string source, string translation, string comments, string fileName)
        {
            this.SourceText = source;
            this.Translation = translation;
            this.FileName = fileName;
            string[] commentLines = comments.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in commentLines)
            {
                string[] keyValuePair = line.Split(new string[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValuePair.Length > 1)
                    Values.Add(keyValuePair[0], keyValuePair[1]);
                else
                    //throw new Exception("wrong comment: " + line);
                    Values.Add(keyValuePair[0], "");
            }

            this.SourceHash = SourceText.GetHashCode();
            this.TranslationHash = Translation.GetHashCode();
        }
    }

    class ResXDictEntryList : List<ResXDictEntry>
    {
        public bool IsUnique = true;
    }

    static class ResXDict
    {
        /*
        blockNo: 00001B11
        prefix: [c4]
        filename: pc-w\cinstream\worldbackhome.mul
        hash: D5AFAF53
        */
        private static Dictionary<int, ResXDictEntryList> dict = new Dictionary<int, ResXDictEntryList>();

        public static void ReadResXFile(string fileName)
        {
            System.ComponentModel.Design.ITypeResolutionService typeRes = null;
            ResXResourceReader rdr = new ResXResourceReader(fileName);
            rdr.UseResXDataNodes = true;
            foreach (DictionaryEntry rdrDictEntry in rdr)
            {
                ResXDataNode node = (ResXDataNode)(rdrDictEntry.Value);
                string key = rdrDictEntry.Key.ToString();
                string value = node.GetValue(typeRes).ToString();
                string comment = node.Comment;
                ResXDictEntry entry = new ResXDictEntry(key, value, comment, fileName);

                ResXDictEntryList entryList;
                if (dict.TryGetValue(entry.SourceHash, out entryList))
                {
                    // add new translation
                    //if (entry.Translation != value)
                    //    entry.IsUnique = false;
                }
                else
                {
                    // new entry
                    entryList = new ResXDictEntryList();
                    dict.Add(entry.SourceHash, entryList);
                }
                entryList.Add(entry);

                if (entry.SourceHash == 0x7C16E747)
                    Debug.WriteLine(string.Format("{0} {1}", entry.FileName, entry.Translation));

                if (entryList.IsUnique && entryList.Count > 1)
                    for (int i = 1; i < entryList.Count; i++)
                    {
                        if (entryList[0].TranslationHash != entryList[i].TranslationHash)
                            entryList.IsUnique = false;
                    }
            }

        }
        public static void Report(string fileName)
        {
            TextWriter tw = null;
            foreach (ResXDictEntryList entryList in dict.Values)
            {
                if (!entryList.IsUnique)
                {
                    if (tw == null)
                        tw = new StreamWriter(fileName);
                    tw.WriteLine("");
                    //tw.WriteLine(string.Format("Unique: {0}", entryList.IsUnique ? "yes" : "no"));
                    //tw.WriteLine(string.Format("Count: {0}", entryList.Count));
                    tw.WriteLine(string.Format("Original: {0:X8}\r\n{1}", entryList[0].SourceHash, entryList[0].SourceText));
                    foreach (ResXDictEntry entry in entryList)
                    {
                        tw.WriteLine(string.Format("File: {0}\r\n{1}", entry.FileName, entry.Translation));
                    }

                }

            }
            if (tw != null)
                tw.Close();
        }
    }
}
