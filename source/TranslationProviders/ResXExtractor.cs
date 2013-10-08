using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Resources;

namespace TRTR
{
    class ResXExtractor : TranslationProvider
    {
        #region private declarations
        //private Dictionary<int, ResXDictEntryList> dict = new Dictionary<int, ResXDictEntryList>();
        private string extractFolder;
        Dictionary<int, string> folderAliasDict;
        Dictionary<int, string> fileNameAliasDict;
        private string lastPath;
        private string lastResXFileName;
        private ResXHelper lastHelper;
        private List<int> lastTransHashes = null;
        private string lastBigFile;
        private bool sepByBigfile;
        #endregion

        // ctor
        internal ResXExtractor(string extractFolder, bool EachBigfileInSeparatedFolder = true)
        {
            this.extractFolder = extractFolder;
            this.sepByBigfile = EachBigfileInSeparatedFolder;
        }

        internal ResXExtractor(bool EachBigfileInSeparatedFolder = true)
            : this(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "resX"), EachBigfileInSeparatedFolder)
        {
            // calls previous ctor with default path
        }

        internal override void Open()
        {

            // purge old translations
            // load filename aliases file
            lastPath = string.Empty;
            lastResXFileName = string.Empty;

            LoadPathAliasesFile(Path.Combine(TRGameInfo.Game.WorkFolder, "path_aliases.txt"), out folderAliasDict, out fileNameAliasDict);
            Clear();

            if (!Directory.Exists(extractFolder))
                Directory.CreateDirectory(extractFolder);
        }

        internal override void Close()
        {
            // flush files
            // compress files, if needed
            // dump statistics

            //foreach (string file in files)
            //    ReadResXFile(file);
            //Report(Path.Combine(TRGameInfo.Game.WorkFolder, "translation report.txt"));
            //Log.LogDebugMsg(string.Format("{0} translation entries added", dict.Count));

            //string zippedFileName = Path.Combine(TRGameInfo.Game.WorkFolder, "hu.zip");
            //if (File.Exists(zippedFileName))
            //    ReadCompressedResX(zippedFileName);
            ResXPoolSingleton.CloseAll();
        }

        protected override bool getUseContext() { return true; }

        internal override void LoadTranslations() { }

        private void LoadPathAliasesFile(string fileName, out Dictionary<int, string> folderAlias, out Dictionary<int, string> fileNameAlias)
        {
            folderAlias = new Dictionary<int, string>();
            fileNameAlias = new Dictionary<int, string>();
            if (File.Exists(fileName))
            {
                TextReader rdr = new StreamReader(fileName);

                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    string[] elements = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (elements.Length == 3)
                    {
                        if (elements[0] == "S")
                            folderAlias.Add(elements[1].GetHashCode(), elements[2]);
                        else
                            fileNameAlias.Add(elements[1].GetHashCode(), elements[2]);
                    }
                    else
                        if (elements.Length != 0)
                        {
                            throw new Exception(string.Format("Invalid path alias entry: \"{0}\"", line));
                        }
                }
            }
        }

        internal override void Clear()
        {
            if (Directory.Exists(extractFolder))
            {
                List<string> delFiles = new List<string>(Directory.GetFiles(extractFolder, "*.resx", SearchOption.AllDirectories));
                delFiles.Sort(compareByPathLength);
                List<string> delFolders = new List<string>();
                foreach (string file in delFiles)
                {
                    File.Delete(file);
                    string filePath = Path.GetDirectoryName(file);
                    if (delFolders.IndexOf(filePath) == -1)
                        delFolders.Add(filePath);
                }
                delFolders = new List<string>(Directory.GetDirectories(extractFolder, "*.*", SearchOption.AllDirectories));
                delFolders.Reverse();

                foreach (string folder in delFolders)
                    if (Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0)
                        try
                        {
                            Directory.Delete(folder);
                        }
                        catch
                        {
                            //Log.LogDebugMsg
                        }
            }
        }

        private static int compareByPathLength(string file1, string file2)
        {
            string path1 = Path.GetDirectoryName(file1);
            string path2 = Path.GetDirectoryName(file2);

            int compareRes = path1.CompareTo(path2);

            if (compareRes == 0)
                compareRes = string.Compare(file1, path1.Length, file2, path2.Length, int.MaxValue);

            return compareRes;
        }

        internal override string GetTranslation(string text, FileEntry entry, string[] context)
        {
            if (text == "")
                return "";

            string resXFileName = string.Empty;
            string bigFileName = string.Empty;
            ResXHelper helper;
            List<int> transHashes;

            if (!entry.Extra.FileNameResolved)
            {
                resXFileName = entry.HashText + ".resx";
                transHashes = new List<int>();
            }
            else
            {
                string path = Path.GetDirectoryName(entry.Extra.FileNameForced);
                // is it the previously processed file?
                if (lastHelper != null && path == lastPath && (!sepByBigfile || lastBigFile == entry.BigFile.Name))
                {
                    resXFileName = lastResXFileName;
                    helper = lastHelper;
                    transHashes = lastTransHashes;
                    bigFileName = entry.BigFile.Name;
                }
                else
                {
                    int pathHash = path.GetHashCode();
                    if (!entry.BigFile.Parent.FileNameAliasDict.TryGetValue(pathHash, out resXFileName))
                        if (entry.BigFile.Parent.FolderAliasDict.TryGetValue(pathHash, out resXFileName))
                            resXFileName = Path.ChangeExtension(entry.Extra.FileName.Replace(path, resXFileName), ".resx");
                    if (resXFileName == string.Empty)
                        resXFileName = entry.HashText + ".resx";

                    transHashes = new List<int>();
                }
            }

            int textHashCode = text.GetHashCode();
            if (!transHashes.Contains(textHashCode))
            {
                transHashes.Add(textHashCode);
                string _extractFolder;

                if (sepByBigfile)
                    _extractFolder = Path.Combine(extractFolder, entry.BigFile.Name);
                else
                    _extractFolder = extractFolder;

                if (!Directory.Exists(_extractFolder))
                    Directory.CreateDirectory(_extractFolder);

                helper = ResXPoolSingleton.GetResX(Path.Combine(_extractFolder, resXFileName));
                lastHelper = helper;
                lastResXFileName = resXFileName;
                lastTransHashes = transHashes;
                lastBigFile = entry.BigFile.Name;

                if (!helper.TryLockFor(ResXLockMode.Write))
                    throw new Exception(string.Format("Can not lock {0} for write", helper.FileName));

                //ResXDataNode resNode = new ResXDataNode(text, text);
                string _text = text; //.Replace(" \r\n", "\r\n");
                ResXDataNode resNode = new ResXDataNode(_text, _text);

                if (context != null)
                {
                    bool discardValue = false;
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < context.Length; i++)
                    {
                        if (i % 2 == 0)
                        {
                            discardValue = context[i] == "bigfile";
                            if (!discardValue)
                                sb.Append("\r\n" + context[i]);
                        }
                        else
                            if (!discardValue)
                                sb.Append(": " + context[i]);
                    }
                    resNode.Comment = sb.ToString().Trim();
                }

                helper.Writer.AddResource(resNode);
            }
            return text;
        }

        public void WriteCompressedResX(string fileName)
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

        public void WriteResXFile(string fileName)
        {
            //FileStream fs = new FileStream(fileName, FileMode.CreateNew);
            //try
            //{
            //    WriteResXFile(fs, fileName);
            //}
            //finally
            //{
            //    fs.Close();
            //}
        }

        public void WriteResXFile(Stream stream, string fileName = "")
        {
            //System.ComponentModel.Design.ITypeResolutionService typeRes = null;
            //ResXResourceReader rdr = new ResXResourceReader(stream);
            //rdr.UseResXDataNodes = true;
            //foreach (DictionaryEntry rdrDictEntry in rdr)
            //{
            //    ResXDataNode node = (ResXDataNode)(rdrDictEntry.Value);
            //    string key = rdrDictEntry.Key.ToString();
            //    string value = node.GetValue(typeRes).ToString();
            //    string comment = node.Comment;
            //    ResXDictEntry entry = new ResXDictEntry(key, value, comment, fileName);

            //    ResXDictEntryList entryList;
            //    if (!dict.TryGetValue(entry.SourceHash, out entryList))
            //    {
            //        // new entry
            //        entryList = new ResXDictEntryList();
            //        dict.Add(entry.SourceHash, entryList);
            //    }
            //    entryList.Add(entry);
            //    if (!entryList.Translated)
            //        if (entry.SourceHash != entry.TranslationHash)
            //            entryList.Translated = true;

            //    if (entryList.IsUnique && entryList.Count > 1)
            //        for (int i = 1; i < entryList.Count; i++)
            //        {
            //            if (entryList[0].TranslationHash != entryList[i].TranslationHash)
            //                entryList.IsUnique = false;
            //        }
            //}
        }

        public void Report(string fileName) { }

    }
}
