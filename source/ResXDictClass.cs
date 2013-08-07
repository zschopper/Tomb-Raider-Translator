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
        private List<KeyValuePair<string, string>> translations = new List<KeyValuePair<string, string>>();
        internal uint Hash { get; set; }
        internal string SourceText { get; set; }
        internal string Translation { get; set; }
        internal List<KeyValuePair<string, string>> Translations { get { return translations; } }
        internal bool IsUnique { get; set; }

        internal bool AddTranslation(string fileName, string translation)
        {
            
            return IsUnique;
        }
    }

    static class ResXDict
    {
        /*
        blockNo: 00001B11
        prefix: [c4]
        filename: pc-w\cinstream\worldbackhome.mul
        hash: D5AFAF53<
        */
        private static Dictionary<int, ResXDictEntry> dict = new Dictionary<int, ResXDictEntry>();
        public static void ReadResXFile(string fileName)
        {
            System.ComponentModel.Design.ITypeResolutionService typeRes = null;
            ResXResourceReader rdr = new ResXResourceReader(fileName);
            rdr.UseResXDataNodes = true;
            foreach (DictionaryEntry rdrDictEntry in rdr)
            {
                ResXDataNode node = (ResXDataNode)(rdrDictEntry.Value);
                int hash = rdrDictEntry.Key.GetHashCode();// node.Name.GetHashCode();
                ResXDictEntry entry;
                    string[] comments = node.Comment.Split(new string[] { "\n" }, StringSplitOptions.None);
                    string value = node.GetValue(typeRes).ToString();

                if (dict.TryGetValue(hash, out entry))
                {
                    // modification
                    if (entry.Translation != value)
                        entry.IsUnique = false;
                    foreach (string comment in comments)
                    {
                        string[] keyValuePair = comment.Split(new string[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);


                    }
                }
                else
                {
                    entry = new ResXDictEntry()
                    {
                        SourceText = rdrDictEntry.Key.ToString(),
                        IsUnique = true,
                        Translation = value
                    };
                    dict.Add(hash, entry);
                }
            }
        }
    }
}
