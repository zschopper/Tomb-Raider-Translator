using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using System.Resources; //resource writer

namespace TRTR
{
    // subtitle files scene1.sch, scene2.sch, etc.
    // subtitle file scene1.sch
    // languages english, deutsch, italian, etc.
    // sentences ...

    internal class MovieFile
    { // one subtitle file

        #region internal variables
        string fileName;
        string version;
        string line0;
        FileEntry entry;

        List<MovieLanguage> langs = new List<MovieLanguage>(); // language, entry
        #endregion

        internal class MovieLanguage
        { // one per language

            #region internal variables
            internal string language;
            internal string time;
            internal List<MovieFileEntry> entries = new List<MovieFileEntry>(); // time, entry
            #endregion

            public class MovieFileEntry
            { // one per "sentence"

                #region private variables
                private string original;
                private string translated;
                private string timeStr;
                private string prefix;
                #endregion

                public string TimeStr { get { return timeStr; } set { timeStr = value; } }
                public string Prefix { get { return prefix; } set { prefix = value; } }
                public string Original { get { return original; } set { original = value; } }
                public string Translated { get { return translated; } set { translated = value; } }
            } // end of SubtitleFileEntry class

            public string Language { get { return language; } set { language = value; } }
            public string Time { get { return time; } set { time = value; } }
            public List<MovieFileEntry> Entries { get { return entries; } set { } }

        } // end of MovieLanguage class

        public string FileName { get { return fileName; } set { fileName = value; } }
        public string Version { get { return version; } set { version = value; } }
        public string Line0 { get { return line0; } set { line0 = value; } }
        public List<MovieLanguage> Langs { get { return langs; } }
        public FileEntry Entry { get { return entry; } }

        // constructor
        internal MovieFile(FileEntry entry)
        {
            this.entry = entry;
            ParseFile();
        }

        void ParseFile()
        {
            //MemoryStream ms = new MemoryStream();

            //entry.CopyContentToStream(ms);
            //ms.Position = 0;

            string[] lines = Encoding.UTF8.GetString(entry.ReadContent()).Split(new string[] { "\r\n" },
                StringSplitOptions.RemoveEmptyEntries);

            #region File Header processing
            if (lines.Length < 4)
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" nincs adat.", fileName));
            if (lines[0] != "Version 1")
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" ismeretlen verzió.", fileName));
            if (lines[1] != "TextEntry")
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" hibás fájlformátum. Sor: 2", fileName));
            if (lines[2] != "line0")
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" hibás fájlformátum. Sor: 3", fileName));
            #endregion

            for (int i = 3; i < lines.Length; i++)
            {

                Match m = Regex.Match(lines[i], "^lang_(\\w+) ([0-9\\-: ]+) \"(.*)\" *$");
                if (!m.Success)
                    throw new Exception("*sch file hiba: beolvasási hiba"); //xxtrans
                if (m.Groups.Count != 4)
                    throw new Exception("*sch file hiba: nyelv nem olvasható"); //xxtrans
                MovieLanguage lng = new MovieLanguage();
                lng.language = m.Groups[1].Value;
                string[] sentences = Regex.Split(m.Groups[3].Value, @"\\");

                foreach (string s in sentences)
                {
                    Match m2 = Regex.Match(s, @"^(\([ 0-9:\.]+\)|)(\[.*?\]|)(.*)$");
                    if (m2.Success)
                    {
                        MovieLanguage.MovieFileEntry fileEntry = new MovieLanguage.MovieFileEntry();

                        if (m2.Groups.Count != 4)
                            throw new Exception("*sch file hiba: szöveg nem olvasható: " + s); //xxtrans
                        fileEntry.TimeStr = m2.Groups[1].Value;
                        fileEntry.Prefix = m2.Groups[2].Value;
                        fileEntry.Original = m2.Groups[3].Value;

                        if (lng.language == "english")
                        {
                            // Log.LogDebugMsg(string.Format("{0} {1}", m2.Groups.Count, s));

                            fileEntry.Translated = TextParser.GetText(fileEntry.Original,
                                string.Format("BF: {0} File: {1} Line: {2}", entry.Extra.BigFileName, entry.Extra.FileNameForced, i));
                        }
                        else
                            fileEntry.Translated = fileEntry.Original;

                        string decimalsep = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                        lng.Entries.Add(fileEntry);
                    }
                }
                langs.Add(lng);
            }
            //OriginalFileContent = content;
        }

        internal void Translate()
        {
            XmlNode subtNode = TRGameInfo.Trans.TranslationDocument.SelectSingleNode("/translation/subtitle");
            if (subtNode != null)
            {
                XmlAttribute attr = subtNode.Attributes["translation"];
                if (attr != null)
                {
                    // Translation = HexEncode.Decode(attr.Value);
                    // entry.WriteContent(Translation);
                }
            }
        }

        internal void Restore()
        {
            XmlNode fontNode = TRGameInfo.Trans.RestorationDocument.SelectSingleNode("/restoration/font");
            if (fontNode != null)
            {
                XmlAttribute attr = fontNode.Attributes["original"];
                if (attr != null)
                {
                    //Original = HexEncode.Decode(attr.Value);
                    //entry.WriteContent(Original);
                }
            }
        }

        internal void CreateRestoration(XmlElement fontElement, XmlNode fontNode)
        {
            //fontElement.SetAttribute("original", HexEncode.Encode(entry.ReadContent()));
        }

        internal void CreateTranslation()
        {
            //XmlNode subtNode = TRGameInfo.Trans.TranslationDocument.SelectSingleNode(String.Format("/translation/movie/cine[@hash=\"{0}\"]", entry.Extra.HashText));
        }

        internal void Extract(string destFolder, bool useDict)
        {

            foreach (MovieFile.MovieLanguage lang in this.langs)
            {
                if (lang.Language == "english") // export only english texts as translation source
                {
                    // ExtractText(destFolder, lang);
                    // ExtractXML(destFolder, lang);
                    ExtractResX(destFolder, lang, useDict);
                }
            }
        }

        private void ExtractResX(string destFolder, MovieFile.MovieLanguage lang, bool useDict)
        {
            if (entry.Extra.HashText == "3533B8DF" && entry.Extra.BigFilePrefix == "patch")
                Debug.WriteLine("ez");

            string resXFileName = Path.Combine(destFolder, entry.Extra.ResXFileName);

            ResXHelper helper = ResXPool.GetResX(resXFileName);
            if (!helper.TryLockFor(ResXLockMode.Write))
                throw new Exception(string.Format("Can not lock {0} for write", resXFileName));

            // Add resources to the file.
            List<int> keys = new List<int>();
            foreach (MovieFile.MovieLanguage.MovieFileEntry mfEntry in lang.Entries)
            {
                int hash = mfEntry.Original.GetHashCode();
                if (mfEntry.Original.Length > 0 && !keys.Contains(hash))
                {
                    keys.Add(hash);
                    ResXDataNode resNode = new ResXDataNode(mfEntry.Original,
                        useDict
                        ? mfEntry.Translated
                        : mfEntry.Original
                        );
                    resNode.Comment = mfEntry.TimeStr;
                    helper.Writer.AddResource(resNode);
                }
            }
        }

        private void ExtractXML(string destFolder, MovieFile.MovieLanguage lang)
        {
            XmlDocument doc = new XmlDocument();
            
            XmlElement elemRoot = doc.CreateElement("movies");
            XmlNode nodeRoot = doc.AppendChild(elemRoot);

            string orig = string.Empty;
            string tran = string.Empty;

            foreach (MovieFile.MovieLanguage.MovieFileEntry mfEntry in lang.Entries)
            {
                XmlElement elemEntry = doc.CreateElement("entry_");
                elemEntry.SetAttribute("time", mfEntry.TimeStr);
                elemEntry.SetAttribute("p", mfEntry.Prefix);
                elemEntry.InnerText = mfEntry.Original;
                nodeRoot.AppendChild(elemEntry);
                doc.Save(fileName);
            }
            doc.Save(Path.Combine(destFolder, entry.Extra.BigFilePrefix + "_" + entry.Extra.FileNameOnlyForced) + ".xml");

        }

        private void ExtractText(string destFolder, MovieFile.MovieLanguage lang)
        {
            TextWriter subtitleWriter = new StreamWriter(
                Path.Combine(destFolder, entry.Extra.BigFilePrefix + "_" + entry.Extra.FileNameOnlyForced) + ".txt", false, Encoding.UTF8);
            try
            {
                // write english subtitles of cinematics to text

                //subtitleWriter = new StreamWriter(fileName, false, Encoding.UTF8);
                subtitleWriter.WriteLine(";extracted from datafiles");

                string orig = string.Empty;
                string tran = string.Empty;

                foreach (MovieFile.MovieLanguage.MovieFileEntry mfEntry in lang.Entries)
                {
                    orig += "#" + mfEntry.TimeStr + ":" + mfEntry.Original + "\r\n";
                    tran += mfEntry.TimeStr + ":" + mfEntry.Translated + "\r\n";
                }
                subtitleWriter.WriteLine(orig + tran);

                //subtitleWriter.WriteLine("@" + entry.Path.GetFileName(key));

                // subtitleWriter.WriteLine("");
            }
            finally
            {
                subtitleWriter.Close();
            }

        }
    } // end of SubtitleFileFile class
}
