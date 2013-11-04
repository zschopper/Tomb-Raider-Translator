using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace TRTR
{
    internal class MovieFile
    {
        internal static bool Process(FileEntry entry, Stream outStream, TranslationProvider tran)
        {
            bool ret = false;
            FileStream fs = TRGameInfo.FilePool.Open(entry);
            try
            {
                fs.Position = entry.Offset;
                ret = Process(entry, fs, entry.Raw.Length, outStream, tran);
            }
            finally
            {
                TRGameInfo.FilePool.Close(entry);
            }
            return ret;
        }

        internal static bool Process(FileEntry entry, Stream inStream, long contentLength, Stream outStream, TranslationProvider tp)
        {
            StringBuilder sb = new StringBuilder();
            bool write = outStream != Stream.Null;

            byte[] buf = new byte[contentLength];
            inStream.Read(buf, 0, (int)contentLength);

            string[] lines = Encoding.UTF8.GetString(buf).Split(new string[] { "\r\n", }, StringSplitOptions.RemoveEmptyEntries);
            buf = null;

            #region File Header processing
            if (lines.Length < 4)
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" nincs adat.", entry.Extra.FileNameForced)); // trans
            if (lines[0] != "Version 1")
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" ismeretlen verzió.", entry.Extra.FileNameForced));// trans
            if (lines[1] != "TextEntry")
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" hibás fájlformátum. Sor: 2", entry.Extra.FileNameForced));// trans
            if (lines[2] != "line0")
                throw new Exception(string.Format("Hibás sch fájl: \"{0}\" hibás fájlformátum. Sor: 3", entry.Extra.FileNameForced));// trans
            #endregion

            if (write)
            {
                sb.AppendLine(lines[0]);
                sb.AppendLine(lines[1]);
                sb.AppendLine(lines[2]);
            }

            for (int i = 3; i < lines.Length; i++)
            {

                Match m = Regex.Match(lines[i], "^lang_(\\w+) ([0-9\\-: ]+) \"(.*)\" *$");
                if (!m.Success)
                    throw new Exception("*sch file hiba: beolvasási hiba"); //xxtrans
                if (m.Groups.Count != 4)
                    throw new Exception("*sch file hiba: nyelv nem olvasható"); //xxtrans
                string lang = m.Groups[1].Value;
                if (lang == "english")
                {
                    string time = m.Groups[2].Value;
                    string[] sentences = Regex.Split(m.Groups[3].Value, @"\\");
                    List<string> translated = new List<string>();

                    foreach (string s in sentences)
                    {
                        Match m2 = Regex.Match(s, @"^(\([ 0-9:\.]+\)|)(\[.*?\]|)(.*)$");
                        if (m2.Success)
                        {
                            if (m2.Groups.Count != 4)
                                throw new Exception("*sch file hiba: szöveg nem olvasható: " + s); //xxtrans

                            string timeStr = m2.Groups[1].Value;
                            string prefix = m2.Groups[2].Value;
                            string text = m2.Groups[3].Value;

                            Dictionary<string, string> context = null;
                            if (tp.UseContext)
                                context = new Dictionary<string, string> { 
                                    //"index", i.ToString(),
                                    //"prefix", prefix,
                                    {"filename", entry.Extra.FileNameForced}, 
                                    {"hash", entry.HashText},
                                    {"bigfile", entry.BigFile.Name},
                                    {"time", timeStr},
                                };
                            //context = new Dictionary<string> { 
                            //        timeStr,
                            //    };
                            string translatedText = TRGameInfo.Conv.ToGameFormat(tp.GetTranslation(text, entry, context));
                            if (write)
                                translated.Add(timeStr + prefix + translatedText);
                        }
                    }
                    if (write)
                        sb.AppendLine(string.Format("lang_{0} {1} \"{2}\"", lang, time, string.Join("\\", translated.ToArray())));
                }
                else
                    if (write)
                    {
                        // non-english texts: simply add them to return value
                        sb.AppendLine(lines[i]);
                    }
            }

            if (write)
            {
                byte[] content = Encoding.UTF8.GetBytes(sb.ToString());
                outStream.Write(content, 0, content.Length);
            }
            return true;
        }
    }
}
