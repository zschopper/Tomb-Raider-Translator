using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Collections;
using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace TRTR
{
    class ResXDictEntry 
    {
        internal int SourceHash { get; set; }
        internal int TranslationHash { get; set; }
        internal string SourceText { get; set; }
        internal string Translation { get; set; }
        internal string FileName { get; set; }
        internal Dictionary<string, string> ContextData = new Dictionary<string, string>();
        
        internal ResXDictEntry(string source, string translation, string comments, string fileName)
        {
            this.SourceText = source;
            this.Translation = translation;
            this.FileName = fileName;
            string[] dataLines = comments.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in dataLines)
            {
                string[] keyValuePair = line.Split(new string[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (keyValuePair.Length > 1)
                    ContextData.Add(keyValuePair[0], keyValuePair[1]);
                else
                    //throw new Exception("wrong comment: " + line);
                    ContextData.Add(keyValuePair[0], "");
            }

            this.SourceHash = SourceText.GetHashCode();
            this.TranslationHash = Translation.GetHashCode();
        }
    }

    class ResXDictEntryList : List<ResXDictEntry>
    {
        public bool IsUnique = true;
        public bool Translated = false;
    }

    class ResXDict : TranslationProvider
    {
        #region private declarations
        private Dictionary<int, ResXDictEntryList> dict = new Dictionary<int, ResXDictEntryList>();
        private string[] files = null;
        #endregion

        // ctor
        internal ResXDict(string[] files)
        {
            this.files = files;
        }

        internal override void Open() { }
        internal override void Close() { }

        protected override bool getUseContext() { return true; }

        internal override void LoadTranslations()
        {
            foreach (string file in files)
                ReadResXFile(file);
            Report(Path.Combine(TRGameInfo.Game.WorkFolder, "translation report.txt"));
            Log.LogDebugMsg(string.Format("{0} translation entries added", dict.Count));

            string zippedFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "hu.zip");
            if(File.Exists(zippedFileName))
                ReadCompressedResX(zippedFileName);
        }

        internal override void Clear()
        {
            dict = new Dictionary<int, ResXDictEntryList>();
        }

        internal override string GetTranslation(string text, FileEntry entry, string[] context)
        {
            if (text.Trim().Length == 0)
                return text;

            ResXDictEntryList dictEntries = null;
            int sourceHash = text.GetHashCode();

            string[] innerContext = new string[] {
                            string.Format("FileType: {0}", entry.FileType),
                            string.Format("BigFile: {0}", entry.BigFile),
                            string.Format("FileName: {0}", entry.Extra.FileNameForced),
                        };
            if (!dict.TryGetValue(sourceHash, out dictEntries))
            {
                //throw new Exception(string.Format("No translation for \"{0}\"", text));
                Log.LogDebugMsg(string.Format("No translation for \"{0}\"", text));
                Log.LogDebugMsg(string.Format("  Context: \"{0}\"", string.Join("; ", innerContext)));
                return text;
            }

            if (!dictEntries.IsUnique)
            {
                foreach (ResXDictEntry dictEntry in dictEntries)
                {
                    if (dictEntry.SourceHash != dictEntry.TranslationHash) // text is localized
                        return dictEntry.Translation;
                }
            }

            if (dictEntries[0].SourceHash == dictEntries[0].TranslationHash)
            {
                Log.LogDebugMsg(string.Format("Text not translated: \"{0}\"", text));
                Log.LogDebugMsg(string.Format("  Context: \"{0}\"", string.Join("; ", innerContext)));
            }
            return dictEntries[0].Translation;
        }

        public void ReadCompressedResX(string fileName)
        {
            //ZipFile file = new ZipFile(fileName);
            //List<string> files = new List<string>();
            //foreach(ZipEntry entry in file)
            //{
            //    if (entry.IsFile)
            //    {
            //        files.Add(entry.Name);
            //        Log.LogDebugMsg("zip:" + entry.Name);
            //    }
            //}
        }

        public void ReadResXFile(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            try
            {
                ReadResXFile(fs, fileName);
            }
            finally
            {
                fs.Close();
            }
        }

        public void ReadResXFile(Stream stream, string fileName = "")
        {
            System.ComponentModel.Design.ITypeResolutionService typeRes = null;
            ResXResourceReader rdr = new ResXResourceReader(stream);
            rdr.UseResXDataNodes = true;
            foreach (DictionaryEntry rdrDictEntry in rdr)
            {
                ResXDataNode node = (ResXDataNode)(rdrDictEntry.Value);
                string key = rdrDictEntry.Key.ToString();
                string value = node.GetValue(typeRes).ToString();
                string comment = node.Comment;
                ResXDictEntry entry = new ResXDictEntry(key, value, comment, fileName);

                ResXDictEntryList entryList;
                if (!dict.TryGetValue(entry.SourceHash, out entryList))
                {
                    // new entry
                    entryList = new ResXDictEntryList();
                    dict.Add(entry.SourceHash, entryList);
                }
                entryList.Add(entry);
                if (!entryList.Translated)
                    if (entry.SourceHash != entry.TranslationHash)
                        entryList.Translated = true;

                if (entryList.IsUnique && entryList.Count > 1)
                    for (int i = 1; i < entryList.Count; i++)
                    {
                        if (entryList[0].TranslationHash != entryList[i].TranslationHash)
                            entryList.IsUnique = false;
                    }
            }
        }

        public void Report(string fileName)
        {
            TextWriter tw = null;
            foreach (ResXDictEntryList entryList in dict.Values)
            {
                if (!entryList.IsUnique)
                {
                    //string 
                    
                    bool translationsIsUnique = true;
                    Nullable<int> firstTranslated = null;

                    foreach (ResXDictEntry entry in entryList)
                    {
                        if (entry.SourceHash != entry.TranslationHash) // text is localized
                        {
                            if (firstTranslated == null)    
                                firstTranslated = entry.TranslationHash;
                            else
                                if (firstTranslated != entry.TranslationHash) // text isn't match with first localized
                                    translationsIsUnique = false;
                        }

                    }

                    if (!translationsIsUnique)
                    {
                        if (tw == null)
                            tw = new StreamWriter(fileName);
                        tw.WriteLine("");
                        //tw.WriteLine(string.Format("Unique: {0}", entryList.IsUnique ? "yes" : "no"));
                        //tw.WriteLine(string.Format("Count: {0}", entryList.Count));
                        tw.WriteLine(string.Format("Original: \"{0:X8}\"\r\n{1}", entryList[0].SourceHash, entryList[0].SourceText));
                        foreach (ResXDictEntry entry in entryList)
                        {
                            tw.WriteLine(string.Format("File: {0} Hash: {1:X8}\r\n{2}", entry.FileName, entry.TranslationHash, entry.Translation));
                        }
                    }

                }

            }
            if (tw != null)
                tw.Close();
        }
    }
}
