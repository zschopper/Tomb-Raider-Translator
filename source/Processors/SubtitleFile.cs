using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TRTR
{
    // subtitle files scene1.sch, scene2.sch, etc.
    // subtitle file scene1.sch
    // languages english, deutsch, italian, etc.
    // sentences ...

    class MovieSubtitles
    { // all subtitle files

        #region internal variables
        private string dir;
        private Dictionary<string, MovieSubtitleFile> files = new Dictionary<string, MovieSubtitleFile>(); // filename, entry
        #endregion

        internal class MovieSubtitleFile
        { // one subtitle file

            #region internal variables
            string fileName;
            string version;
            string line0;
            List<SubtitleLanguage> entries = new List<SubtitleLanguage>(); // language, entry
            #endregion

            internal class SubtitleLanguage
            { // one per language

                #region internal variables
                internal string language;
                internal string time;
                internal Dictionary<int, SubtitleFileEntry> entries = new Dictionary<int, SubtitleFileEntry>(); // time, entry
                #endregion

                public class SubtitleFileEntry
                { // one per "sentence"

                    #region internal variables
                    internal string original;
                    internal string translated;
                    internal string timeStr;
                    #endregion

                    public string TimeStr { get { return timeStr; } set { timeStr = value; } }
                    public string Original { get { return original; } set { original = value; } }
                    public string Translated { get { return translated; } set { translated = value; } }
                } // end of SubtitleFileEntry class

                public string Language { get { return language; } set { language = value; } }
                public string Time { get { return time; } set { time = value; } }
                public Dictionary<int, SubtitleFileEntry> Entries { get { return entries; } set { } }

            } // end of SubtitleFileLanguage class

            public string FileName { get { return fileName; } set { fileName = value; } }
            public string Version { get { return version; } set { version = value; } }
            public string Line0 { get { return line0; } set { line0 = value; } }
            public List<SubtitleLanguage> Entries { get { return entries; } }

            // constructor
            internal MovieSubtitleFile(string fileName)
            {
                this.fileName = fileName;
                string content;
                TextReader tr = new StreamReader(fileName);
                content = tr.ReadToEnd();
                string[] lines = content.Split(new string[] { "\r\n" }, StringSplitOptions.None);

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
                    SubtitleLanguage lng = new SubtitleLanguage();

                    Match m = Regex.Match(lines[i], "^lang_(\\w+) ([0-9\\-: ]+) \"(.*)\" *$");
                    if (m.Success)
                    {
                        if (m.Groups.Count != 4)
                            throw new Exception("*sch file hiba: nyelv nem olvasható");
                        lng.language = m.Groups[1].Value;
                        string[] sentences = Regex.Split(m.Groups[3].Value, @"\\");

                        foreach (string s in sentences)
                        {
                            Match m2 = Regex.Match(s, @"^\(([0-9]*)\.?([0-9]*)\)(.*)$");
                            if (m2.Success)
                            {
                                SubtitleLanguage.SubtitleFileEntry entry = new SubtitleLanguage.SubtitleFileEntry();

                                switch (m2.Groups.Count)
                                {
                                    case 3: 
                                        entry.timeStr = m2.Groups[1].Value.PadLeft(3, '0') + ".00";
                                        entry.original = m2.Groups[2].Value;
                                        entry.translated = m2.Groups[2].Value;
                                        break;
                                    case 4:
                                        entry.timeStr = m2.Groups[1].Value.PadLeft(3, '0') + "." + m2.Groups[2].Value.PadRight(2, '0');
                                        entry.original = m2.Groups[3].Value;
                                        entry.translated = m2.Groups[3].Value;
                                        break;
                                    default: throw new Exception("*sch file hiba: szöveg nem olvasható: " + s);
                                }


                                string decimalsep = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
                                int key = Convert.ToInt32(float.Parse("0" + entry.timeStr.Replace(".", decimalsep)) * 100);
//                                entry.timeStr = key.ToString("D5");

                                lng.Entries.Add(key, entry);
                            }
                        }

                    }
                    entries.Add(lng);
                }
                //OriginalFileContent = content;
            }

        } // end of SubtitleFileFile class

        public Dictionary<string, MovieSubtitleFile> Files { get { return files; } }

        // constructor
        internal MovieSubtitles()
        {
            this.dir = TRGameInfo.InstallInfo.InstallPath;
            string[] fileNames = System.IO.Directory.GetFiles(Path.Combine(dir, "movies"), "*.sch");
            foreach (string file in fileNames)
            {
                files.Add(file, new MovieSubtitleFile(file));
            }
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

        internal void Extract(string destFolder)
        {
            TextWriter subtitleWriter = null;
            try
            {
                // write english subtitles of cinematics to text
                string transFolder = Path.Combine(destFolder, "trans");
                Directory.CreateDirectory(transFolder);

                subtitleWriter = new StreamWriter(Path.Combine(transFolder, "movies.txt"), false, Encoding.UTF8);
                subtitleWriter.WriteLine(";extracted from datafiles");

                foreach (string key in files.Keys) // iterate files
                {
                    MovieSubtitleFile bikFile = files[key];
                    subtitleWriter.WriteLine("@" + Path.GetFileName(key));
                    string fileName = Path.GetFileName(key);

                    foreach (MovieSubtitles.MovieSubtitleFile.SubtitleLanguage lang in bikFile.Entries)
                    {
                        if (lang.Language == "english") // export only english texts as translation source
                        {
                            string orig = string.Empty;
                            string tran = string.Empty;
                                
                            foreach (MovieSubtitles.MovieSubtitleFile.SubtitleLanguage.SubtitleFileEntry entry in lang.Entries.Values)
                            {
                                orig += "#" + entry.TimeStr + ":" + entry.Original + "\r\n";
                                tran += entry.TimeStr + ":" + entry.Original + "\r\n";
                            }
                            subtitleWriter.WriteLine(orig + tran);
                        }
                    }
                    // subtitleWriter.WriteLine("");
                }


            }
            finally
            {
                if (subtitleWriter != null)
                    subtitleWriter.Close();
            }
        }
    }
}
