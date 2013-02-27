using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Cryptography;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace TRTR
{
    // convert new format text translations to xml.
    // [Conditional("oldparser")]
    //#if oldParser
    class TranslationParser
    {
        private string folder;
        FileEntryList entries;

        // Dictionary<Int32, TranslationFileEntry> subTransEntries;
        Dictionary<Int32, TranslationFileEntry> menuTransEntries;
        Dictionary<string, List<TranslationFileEntry>> moviesTransEntries2;
        List<KeyValuePair<string, TranslationFileEntry>> moviesTransEntries;
        CineTransFileList cineTransFiles;
        BackgroundWorker worker;

        private TRGameTransInfo gti;

        internal TranslationParser(string lang)
        {
            this.worker = null;
            Initialize(lang);
        }

        internal TranslationParser(string lang, BackgroundWorker worker)
        {
            this.worker = worker;
            Initialize(lang);
        }

        [Conditional("DEBUG")]
        internal void Initialize(string lang)
        {
            folder = FileNameUtils.IncludeTrailingBackSlash(Settings.TransRootDir) +
                string.Format("{0}.{1}\\", TRGameInfo.InstallInfo.GameNameFull, lang);

            if (!File.Exists(folder + "trans_info.xml"))
                throw new Exception(string.Format("Translation info file ({0}) does not exist", Path.Combine(folder, "trans_info.xml")));
            gti = new TRGameTransInfo(folder + "trans_info.xml");

            // prepare 
            cineTransFiles = new CineTransFileList();
            menuTransEntries = new Dictionary<Int32, TranslationFileEntry>();
            moviesTransEntries = new List<KeyValuePair<string, TranslationFileEntry>>();
            entries = new FileEntryList(worker);
            entries.ReadFAT();

            

            // parse
            ParseSubtitles(null);
            ParseMenu();
            ParseMovies(null);
            entries.SortBy(FileEntryCompareField.Location);

            // create xml
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.AppendChild(doc.CreateProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\""));
                XmlElement rootElement = doc.CreateElement("translation");
                XmlNode rootNode = doc.AppendChild(rootElement);
                rootElement.SetAttribute("version", "2.2");

                XmlElement menuElement = doc.CreateElement("menu");
                XmlNode menuNode = rootNode.AppendChild(menuElement);

                XmlElement subtitleElement = doc.CreateElement("subtitle");
                XmlNode subtitleNode = rootNode.AppendChild(subtitleElement);

                XmlElement moviesElement = doc.CreateElement("movies");
                XmlNode moviesNode = rootNode.AppendChild(moviesElement);

                WriteXML(doc);
            }
            finally
            {
                doc.Save(".\\" + TRGameInfo.InstallInfo.GameNameFull + ".tra.xml");
            }
        }

        [Conditional("DEBUG")]
        internal void ParseMenu()
        {
            //TODO: USE NEW FILE !!
            string fileName = folder + gti.MenuFileName;
            string textContent = File.ReadAllText(fileName, Encoding.UTF8);

            List<string> entryTexts = new List<string>(Regex.Split(textContent, "^@", RegexOptions.Multiline));

            // remove comments from first line
            if (!Regex.Match(entryTexts[0], @"^[0-9]{4}\r\n", RegexOptions.Multiline).Success)
                entryTexts.RemoveAt(0);

            Regex rxHash = new Regex(@"^([0-9]{4})\r\n", RegexOptions.Multiline);
            Regex rxOriginal = new Regex(@"^#(.*)\r\n", RegexOptions.Multiline);
            Regex rxDirective = new Regex(@"^\$(.*)\r\n", RegexOptions.Multiline);
            Regex rxComment = new Regex(@"^;(.*)\r\n", RegexOptions.Multiline);
            Regex rxTrans = new Regex(@"^(.*)\r\n", RegexOptions.Multiline);

            TranslationFileEntry transEntry = null;
            for (Int32 j = 0; j < entryTexts.Count; j++)
            {
                string txt = entryTexts[j];
                transEntry = new TranslationFileEntry();
                MatchCollection mtchs = null;
                Match mtch = null;

                // process hash
                mtch = rxHash.Match(txt);
                if (!mtch.Success)
                    throw new Exception("Internal error: Menu identifier not found.");
                transEntry.Id = mtch.Result("$1");
                txt = rxHash.Replace(txt, string.Empty, 1);

                // process originals
                mtchs = rxOriginal.Matches(txt);
                foreach (Match m in mtchs)
                    if (transEntry.Original.Length == 0)
                        transEntry.Original = m.Result("$1").Replace("\r\n#", "\r\n");
                    else
                        transEntry.Original += "\r\n" + m.Result("$1").Replace("\r\n#", "\r\n");
                txt = rxOriginal.Replace(txt, string.Empty);

                // process comments
                mtchs = rxComment.Matches(txt);
                foreach (Match m in mtchs)
                    if (transEntry.Comments.Length == 0)
                        transEntry.Comments = m.Result("$1").Replace("\r\n;", "\r\n");
                    else
                        transEntry.Comments += "\r\n" + m.Result("$1").Replace("\r\n;", "\r\n");
                txt = rxComment.Replace(txt, string.Empty);

                // process directives
                mtchs = rxDirective.Matches(txt);
                foreach (Match m in mtchs)
                    if (transEntry.Directives.Length == 0)
                        transEntry.Directives = m.Result("$1").Replace("\r\n$", "\r\n");
                    else
                        transEntry.Directives += "\r\n" + m.Result("$1").Replace("\r\n$", "\r\n");
                txt = rxDirective.Replace(txt, string.Empty);

                // process translations
                mtchs = rxTrans.Matches(txt);
                for (Int32 k = 0; k < mtchs.Count - 1; k++)
                {
                    Match m = mtchs[k];
                    if (transEntry.Translation.Length == 0)
                        transEntry.Translation = m.Result("$1");
                    else
                        transEntry.Translation += "\r\n" + m.Result("$1");
                }
                transEntry.Translation = transEntry.Translation.Replace("!TESZT! ", string.Empty);
                // $del
                if (transEntry.Directives.Contains("DEL"))
                    transEntry.Translation = "?";
                // $setup
                transEntry.KeepAccentedChars = transEntry.Directives.Contains("SETUP");
                menuTransEntries.Add(Convert.ToInt32(transEntry.Id), transEntry);
            }
        }

        [Conditional("DEBUG")]
        private void ParseSubtitles(XmlNode node)
        {
            string fileName = folder + gti.SubtitleFileName;
            string textContent = File.ReadAllText(fileName, Encoding.UTF8);

            List<string> entryTexts = new List<string>(Regex.Split(textContent, "^HASH: ", RegexOptions.Multiline));

            if (!Regex.Match(entryTexts[0], @"^([0-9A-F]{7,8});[^\n]*(dir|sub)\r\n", RegexOptions.Multiline).Success)
                entryTexts.RemoveAt(0);

            Regex rxHash = new Regex(@"^([0-9A-F]{7,8});[^\n]*(dir|sub)\r\n", RegexOptions.Multiline);
            Regex rxOriginal = new Regex(@"^#[^\r\n]*\r\n", RegexOptions.Multiline);
            Regex rxDirective = new Regex(@"^\$[^\r\n]*\r\n", RegexOptions.Multiline);
            Regex rxComment = new Regex(@"^;[^\r\n]*\r\n", RegexOptions.Multiline);
            Regex rxIsNewText = new Regex(@"^([0-9]{5});([^\r\n]*)$");
            Regex rxTrans = new Regex(@"^(.*)\r\n", RegexOptions.Multiline);

            for (Int32 i = 0; i < entryTexts.Count; i++)
            {
                string txt = entryTexts[i];

                //                string[] entryLines = txt.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                TranslationFileEntry transEntry = new TranslationFileEntry();
                Match mtch = null;
                MatchCollection mtchs = null;

                // parse hash & language;
                mtch = rxHash.Match(txt);
                UInt32 hash = 0;

                if (!mtch.Success)
                    throw new Exception("Internal error: Subtitle identifier (hash) not found.");

                hash = UInt32.Parse(mtch.Result("$1"), System.Globalization.NumberStyles.HexNumber);
                bool isSub = mtch.Result("$2") == "sub";
                txt = rxHash.Replace(txt, string.Empty, 1);

                // parse original
                txt = rxOriginal.Replace(txt, string.Empty);
                // parse comments
                txt = rxComment.Replace(txt, string.Empty);

                CineTransBlockList transBlocks = new CineTransBlockList();
                cineTransFiles.Add(hash, transBlocks);

                CineBlockTranslationTextList blockTexts = null;

                // process directives
                mtchs = rxDirective.Matches(txt);
                foreach (Match m in mtchs)
                    if (transEntry.Directives.Length == 0)
                        transEntry.Directives = m.Result("$1").Replace("\r\n$", "\r\n");
                    else
                        transEntry.Directives += "\r\n" + m.Result("$1").Replace("\r\n$", "\r\n");
                txt = rxDirective.Replace(txt, string.Empty);

                // process translations
                mtchs = rxTrans.Matches(txt);
                string translation = string.Empty;
                for (Int32 k = 0; k < mtchs.Count - 1; k++)
                {
                    Match m = mtchs[k];
                    if (translation.Length == 0)
                        translation = m.Result("$1");
                    else
                        translation += "\r\n" + m.Result("$1");
                }
                translation = translation.Replace("!TESZT! ", string.Empty);

                #region Apply directives
                //// $del
                //if (transEntry.Directives.Contains("DEL"))
                //    transEntry.Translation = "?";
                //// $setup
                //transEntry.KeepAccentedChars = transEntry.Directives.Contains("SETUP");
                //menuTransEntries.Add(Convert.ToInt32(transEntry.Hash), transEntry);

                #endregion

                List<string> transLines = new List<string>(Regex.Split(translation, @"\r\n"));
                for (Int32 j = 0; j < transLines.Count - 0; j++)
                {
                    mtch = rxIsNewText.Match(transLines[j]);
                    string blockText = string.Empty;
                    if (mtch.Success)
                    {
                        UInt32 blockNo = UInt32.Parse(mtch.Result("$1"));
                        blockText = mtch.Result("$2");

                        if (!transBlocks.ContainsKey(blockNo))
                        {
                            blockTexts = new CineBlockTranslationTextList();
                            transBlocks.Add(blockNo, blockTexts);
                        }
                        else // duplicate key
                            blockTexts = transBlocks[blockNo];
                        blockTexts.Add(blockText);
                    }
                    else
                    {
                        // add value to end of last entry.
                        Int32 index = blockTexts.Count - 1;
                        blockTexts[index] += "\r\n" + transLines[j];
                    }
                }
            }

        }

        [Conditional("DEBUG")]
        private void ParseMovies(XmlNode node)
        {
            string fileName = folder + gti.MoviesFileName;
            string textContent = File.ReadAllText(fileName, Encoding.UTF8);

            List<string> entryTexts = new List<string>(Regex.Split(textContent, "^@", RegexOptions.Multiline));

            // remove comments from first line
            if (!Regex.Match(entryTexts[0], @"^@(.*):(.*)\r\n", RegexOptions.Multiline).Success)
                entryTexts.RemoveAt(0);

            Regex rxFileName = new Regex(@"^@(.*)\r\n", RegexOptions.Multiline);
            Regex rxOriginal = new Regex(@"^#(.*?):(.*)\r\n", RegexOptions.Multiline);
            Regex rxComment = new Regex(@"^;(.*)\r\n", RegexOptions.Multiline);
            Regex rxTrans = new Regex(@"^(.*?):(.*)\r\n", RegexOptions.Multiline);

            // Dictionary<string, List<TranslationFileEntry>> moviesTransEntries2; 
            string movieFileName = string.Empty;
            TranslationFileEntry transEntry = null;
            for (Int32 j = 0; j < entryTexts.Count; j++)
            {
                if (entryTexts[j].Length > 0)
                {
                    // IDE
                }
                string txt = entryTexts[j];
                transEntry = new TranslationFileEntry();
                MatchCollection mtchs = null;
                Match mtch = null;

                // process filename
                mtch = rxFileName.Match(txt);
                if (mtch.Success)
                {
                    movieFileName = mtch.Groups[1].Value;
                    txt = string.Empty;
                }
                
                if(txt.Length > 0)
                { 
                    
                }
                transEntry.Id = mtch.Result("$1");
                txt = rxFileName.Replace(txt, string.Empty, 1);

                // process originals
                mtchs = rxOriginal.Matches(txt);
                foreach (Match m in mtchs)
                    if (transEntry.Original.Length == 0)
                        transEntry.Original = m.Result("$1").Replace("\r\n#", "\r\n");
                    else
                        transEntry.Original += "\r\n" + m.Result("$1").Replace("\r\n#", "\r\n");
                txt = rxOriginal.Replace(txt, string.Empty);

                // process comments
                mtchs = rxComment.Matches(txt);
                foreach (Match m in mtchs)
                    if (transEntry.Comments.Length == 0)
                        transEntry.Comments = m.Result("$1").Replace("\r\n;", "\r\n");
                    else
                        transEntry.Comments += "\r\n" + m.Result("$1").Replace("\r\n;", "\r\n");
                txt = rxComment.Replace(txt, string.Empty);
/*
                // process directives
                mtchs = rxDirective.Matches(txt);
                foreach (Match m in mtchs)
                    if (transEntry.Directives.Length == 0)
                        transEntry.Directives = m.Result("$1").Replace("\r\n$", "\r\n");
                    else
                        transEntry.Directives += "\r\n" + m.Result("$1").Replace("\r\n$", "\r\n");
                txt = rxDirective.Replace(txt, string.Empty);
*/
                // process translations
                mtchs = rxTrans.Matches(txt);
                for (Int32 k = 0; k < mtchs.Count - 1; k++)
                {
                    Match m = mtchs[k];
                    if (transEntry.Translation.Length == 0)
                        transEntry.Translation = m.Result("$1");
                    else
                        transEntry.Translation += "\r\n" + m.Result("$1");
                }
                transEntry.Translation = transEntry.Translation.Replace("!TESZT! ", string.Empty);
                // $del
                if (transEntry.Directives.Contains("DEL"))
                    transEntry.Translation = "?";
                // $setup
                transEntry.KeepAccentedChars = transEntry.Directives.Contains("SETUP");
                moviesTransEntries.Add(new KeyValuePair<string, TranslationFileEntry>(transEntry.Id, transEntry));
            }

        }

        [Conditional("DEBUG")]
        private void WriteXML(XmlDocument doc)
        {
            XmlNode menuNode = doc.SelectSingleNode("/translation/menu");
            XmlNode subtitleNode = doc.SelectSingleNode("/translation/subtitle");
            // write english subtitles of cinematics to xml & text
            char[] strippedChars = " \r\n".ToCharArray();

            #region write menu
            foreach (FileEntry entry in entries)
            {
                if (entry.Stored.FileType == FileTypeEnum.BIN_MNU &&
                    entry.Raw.Language == FileLanguage.English)
                {
                    MenuFile menu = new MenuFile(entry);
                    foreach (Int32 key in menuTransEntries.Keys)
                    {
                        if (menu.MenuEntries[key].StartIdx != 0)
                        {
                            TranslationFileEntry transEntry = menuTransEntries[key];
                            XmlElement entryElement = doc.CreateElement("entry");
                            XmlNode entryNode = menuNode.AppendChild(entryElement);

                            entryElement.SetAttribute("no", transEntry.Id);
                            string translation = TRGameInfo.textConv.ToOriginalFormat(transEntry.Translation.Trim(strippedChars));
                            entryElement.SetAttribute("translation", translation);
                            if (translation.Length > 0)
                                entryElement.SetAttribute("checksum", Hash.Get(translation + "m" + transEntry.Id + TRGameInfo.InstallInfo.GameNameFull));
                            if (transEntry.KeepAccentedChars)
                                entryElement.SetAttribute("setup", "true");
                        }
                    }
                }
            }
            #endregion

            #region write subtitles
            foreach (FileEntry entry in entries)
            {
                if (entry.Stored.FileType == FileTypeEnum.MUL_CIN &&
                    (entry.Raw.Language == FileLanguage.English || entry.Raw.Language == FileLanguage.NoLang))
                {

                    if (cineTransFiles.ContainsKey(entry.Hash))
                    {
                        CineTransBlockList transBlocks = cineTransFiles[entry.Hash];
                        CineFile cine = new CineFile(entry);
                        XmlElement cineElement = doc.CreateElement("cine");
                        cineElement.SetAttribute("hash", entry.Extra.HashText);
                        if (entry.Stored.FileName.Length > 0)
                            cineElement.SetAttribute("filename", entry.Stored.FileName);
                        XmlNode cineNode = null;

                        for (UInt32 j = 0; j < cine.Blocks.Count; j++)
                        {
                            CineBlock block = cine.Blocks[(Int32)j];
                            if (block.subtitles != null)
                                if (block.subtitles.Count > 0)
                                {
                                    UInt32 textCount = block.subtitles.TextCount(FileLanguage.English);
                                    if (textCount > 0)
                                    {
                                        CineBlockTranslationTextList blockTexts = transBlocks[j];

                                        for (UInt32 k = 0; k < blockTexts.Count; k++)
                                        {
                                            if (cineNode == null)
                                                cineNode = subtitleNode.AppendChild(cineElement);

                                            XmlElement blockElement = doc.CreateElement("block");
                                            XmlNode blockNode = cineNode.AppendChild(blockElement);
                                            blockElement.SetAttribute("no", j.ToString("d5"));
                                            //if (transEntries.ContainsKey(hashCode))
                                            string original = block.subtitles.Entry(FileLanguage.English, k).Text.Replace("\n", "\r\n");//checkthis
                                            original = original.Replace(" \r", "\r");
                                            UInt32 hashCode = (UInt32)original.GetHashCode();

                                            string translation = TRGameInfo.textConv.ToOriginalFormat(blockTexts[(Int32)k].Trim(strippedChars));
                                            blockElement.SetAttribute("translation", translation);
                                            if (translation.Length > 0)
                                                blockElement.SetAttribute("checksum", Hash.Get(translation + "s" + j.ToString("d5") + TRGameInfo.InstallInfo.GameNameFull));
                                        }
                                    }
                                }
                        }
                    }
                }
            }
            #endregion

            #region write movies
            XmlNode movieNode = doc.SelectSingleNode("/translation/movies");

            TextWriter subtitleWriter = null;
            try
            {
/*
                //List<KeyValuePair<string, TranslationFileEntry>> moviesTransEntries;
                foreach (KeyValuePair<string, TranslationFileEntry> movieFile in moviesTransEntries) // iterate files
                {
                    XmlElement movieFileElement = doc.CreateElement("file");
                    movieFileElement.SetAttribute("filename", movieFile.Key);
                    XmlNode movieFileNode = movieNode.AppendChild(movieFileElement);

                    foreach (MovieSubtitles.MovieSubtitleFile.SubtitleLanguage lang in movieFile.Value.Translation)
                    {
                        if (lang.Language == "english") // export only english texts as translation source
                        {
                            foreach (MovieSubtitles.MovieSubtitleFile.SubtitleLanguage.SubtitleFileEntry entry in lang.Entries.Values)
                            {
                                XmlElement movieTransEntry = doc.CreateElement("trans");
                                movieTransEntry.SetAttribute("timestr", entry.TimeStr);
                                movieTransEntry.SetAttribute("timestr", entry.Translated);
                                XmlNode movieTransNode = movieFileNode.AppendChild(movieTransEntry);
                            }
                        }
                    }
                }
*/
            }
            finally
            {
                if (subtitleWriter != null)
                    subtitleWriter.Close();
            }

















            #endregion

            #region write RAW font
            // translate font
            if (gti.NeedToReplaceChars)
                foreach (FileEntry entry in entries)
                {
                    if (entry.Stored.FileType == FileTypeEnum.RAW_FNT)
                    {
                        string fileName = string.Format("{0}trans\\{1}", folder, gti.RawFontReplaced);
                        if (File.Exists(fileName))
                        {
                            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                            try
                            {
                                byte[] buf = new byte[fs.Length];
                                fs.Read(buf, 0, (Int32)fs.Length);
                                XmlElement fontElement = doc.CreateElement("font");
                                XmlNode fontNode = doc.SelectSingleNode("translation").AppendChild(fontElement);

                                fontElement.SetAttribute("originalChars", new string(gti.OriginalChars));
                                fontElement.SetAttribute("replacedChars", new string(gti.ReplacedChars));
                                fontElement.SetAttribute("translation", HexEncode.Encode(buf));
                            }
                            finally
                            {
                                fs.Close();
                            }
                        }
                    }
                }
            #endregion
        }

    }
    //#endif

    // CINE files
    class CineTransFileList : Dictionary</*entry hash*/UInt32,  /*blocks*/CineTransBlockList> { }

    // blocks 
    class CineTransBlockList : Dictionary</*blockNo*/UInt32, /*texts*/CineBlockTranslationTextList> { }

    // entries of blocks
    class CineBlockTranslationTextList : List<string> { }


    class TranslationFileEntry
    {
        internal string Id = string.Empty;
        internal string Original = string.Empty;
        internal string Directives = string.Empty;
        internal string Comments = string.Empty;
        internal string Translation = string.Empty;
        internal bool KeepAccentedChars = false;
    }
}