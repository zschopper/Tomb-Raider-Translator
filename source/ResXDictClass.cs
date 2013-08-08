using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Collections;


namespace TRTR
{
    class ResXDictEntry
    {
        internal int SourceHash { get; set; }
        internal int TranslationHash { get; set; }
        internal string SourceText { get; set; }
        internal string Translation { get; set; }
        internal Dictionary<string, string> Values = new Dictionary<string, string>();

        internal ResXDictEntry(string source, string translation, string comments)
        {
            this.SourceText = source;
            this.Translation = translation;
            string[] commentLines = comments.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in commentLines)
            {
                string[] keyValuePair = line.Split(new string[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValuePair.Length > 1)
                    Values.Add(keyValuePair[0], keyValuePair[1]);
                else
                    throw new Exception("ehh");
            }

            this.SourceHash = SourceText.GetHashCode();
            this.TranslationHash = Translation.GetHashCode();
        }
    }

    class ResXDictEntryList : List<ResXDictEntry> { }

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
                ResXDictEntry entry = new ResXDictEntry(key, value, comment);

                ResXDictEntryList entryList;

                if (dict.TryGetValue(entry.SourceHash, out entryList))
                {
                    // modification
                    //if (entry.Translation != value)
                    //    entry.IsUnique = false;
                }
                else
                {
                    //entry = new ResXDictEntry()
                    //{
                    //    SourceText = rdrDictEntry.Key.ToString(),
                    //    IsUnique = true,
                    //    Translation = value
                    //};
                    //dict.Add(hash, entry);
                }
            }
        }
    }
}
