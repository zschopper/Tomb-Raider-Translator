using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

namespace TRTR.Processors
{

    class CineFileIntf : ITransProc
    {
        //CineTransFileList cineTransFiles = new CineTransFileList();
        string ITransProc.Name { get { return "CINE"; } }
        void ITransProc.Initialize()
        {
        }

        string[] ITransProc.GetFileList() { return new string[0]; }

        bool IsMyFile(FileEntry file)
        {
            return "ENIC" == Encoding.ASCII.GetString(file.ReadContent(80, 4));
        }

        // extracts translatable data
        void ITransProc.Extract(FileEntryList entryList)
        {
            string valueSep = ";";
            TextWriter cineWriter = null;
            // write english subtitles of cinematics to text
            //            Directory.CreateDirectory(TRGameInfo.Game.WorkFolder);
            cineWriter = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "subtitles.txt"), false, Encoding.UTF8);
            cineWriter.WriteLine(";extracted from datafiles");

            foreach (FileEntry file in entryList)
            {
                if ((file.Raw.Language == FileLanguage.NoLang || file.Raw.Language == TRGameInfo.OverwriteLang) && IsMyFile(file))
                {
                    CineFile cine = new CineFile(file);
                    #region regi

                    /*
                                    string header = "HASH: " + entry.HashText + 
                                        (entry.Extra.FileName.Length > 0 ? valueSep + "FILENAME: " + entry.Extra.FileName : string.Empty)
                                        //                            + " BLOCKS: "
                                        ;
                                    string blocks = string.Empty;
                                    string original = string.Empty; // ";original\r\n";
                                    string translated = string.Empty; // ";translation\r\n";

                                    for (Int32 j = 0; j < cine.Blocks.Count; j++)
                                    {
                                        CineBlock block = cine.Blocks[j];
                                        string blockNo = j.ToString("d4");
                                        if (block.BlockTypeNo == 1 || block.IsEnic)
                                        {
                                            if (blocks.Length == 0)
                                                blocks = blockNo;
                                            else
                                                blocks += ", " + blockNo;
                                        }
                                        if (block.subtitles != null)
                                            if (block.subtitles.ContainsKey(FileLanguage.English))
                                            {
                                                string blockOrig = block.subtitles[FileLanguage.English];
                                                // strip trailing spaces
                                                blockOrig = blockOrig.Replace(" \n", "\n");
                                                // replace original texts' newlines (cr) to normal text format (crlf)
                                                blockOrig = blockOrig.Replace("\n", "\r\n");
                                                // add double quotes if contains newline, double quote or comma
                                                Int32 hashCode = blockOrig.GetHashCode();
                                                if (!subTransEntries.ContainsKey(hashCode))
                                                    throw new Exception("Key not found: \"" + blockOrig + "\"");
                                                TranslationCovertFileEntry transEntry = subTransEntries[hashCode];
                                                if (rxDblQuoteNeeded.IsMatch(blockOrig))
                                                    blockOrig = "\"" + blockOrig + "\"";

                                                string blockTrans = transEntry.Translation.Trim(strippedChars);
                                                // strip trailing spaces
                                                blockTrans = blockTrans.Replace(" \n", "\n");
                                                // replace original texts' newlines (cr) to normal text format (crlf)
                                                blockTrans = blockTrans.Replace("\n", "\r\n");
                                                blockTrans = TRGameInfo.TextConv.ToOriginalFormat(blockTrans);
                                                // add double quotes if contains newline, double quote or comma
                                                if (rxDblQuoteNeeded.IsMatch(blockTrans))
                                                    blockTrans = "\"" + blockTrans + "\"";

                                                string blockDirectives =
                                                    "$OLDHASH=" + transEntry.Hash + "\r\n" +
                                                    (transEntry.Directives.Length > 0 ? transEntry.Directives + "\r\n" : string.Empty);

                                                original +=
                                                    //blockDirectives + 
                                                    ";" + blockNo + valueSep + blockOrig + "\r\n";
                                                translated +=
                                                    //(transEntry.Comments.Length > 0 ? transEntry.Comments + "\r\n" : string.Empty) + 
                                                    blockNo + valueSep + blockTrans + "\r\n";
                                                // write to file
                                            }
                                    }
                                    byte[] buf =
                                        TRGameInfo.TextConv.Enc.GetBytes(
                                        header +
                                        //blocks + 
                                        "\r\n" +
                                        original +
                                        translated +
                                        valueSep + "\r\n");
                                    fs.Write(buf, 0, buf.Length);
                                } 




                                     */

                    #endregion
                    string header = "HASH: " + file.HashText +
                        (file.Extra.FileName.Length > 0 ? valueSep + "FILENAME: " + file.Extra.FileName : string.Empty);
                    if (file.Raw.Language == FileLanguage.English)
                        header += ";sub";
                    else
                        header += ";dir";

                    string blockOrig = String.Empty;
                    string blockTrans = String.Empty;
                    for (Int32 j = 0; j < cine.Blocks.Count; j++)
                    {
                        CineBlock block = cine.Blocks[j];
                        if (block.Subtitles != null)
                        {
                            UInt32 textCount = block.Subtitles.TextCount(FileLanguage.English);
                            for (UInt32 k = 0; k < textCount; k++)
                            {
                                string text = block.Subtitles.Entry(FileLanguage.English, k).Text;
                                text = text.Replace("\r\n", "\n");
                                text = text.Replace(" \n", "\n");
                                text = text.Replace("\n", "\r\n");
                                text = j.ToString("d5") + valueSep /*+ TRGameInfo.TextConv.ToOriginalFormat(text)*/;
                                if (blockOrig.Length > 0)
                                    blockOrig += "\r\n" + TransConsts.OriginalPrefix + text.Replace("\r\n", "\r\n" + TransConsts.OriginalPrefix);
                                else
                                    blockOrig += TransConsts.OriginalPrefix + text.Replace("\r\n", "\r\n" + TransConsts.OriginalPrefix);
                                if (blockTrans.Length > 0)
                                    blockTrans += "\r\n" + text;
                                else
                                    blockTrans += text;
                            }
                        }
                    }
                    cineWriter.WriteLine(header + "\r\n" + blockOrig + "\r\n" + blockTrans + "\r\n");
                }
            }
        }

        // creates translation xml
        void ITransProc.CreateTranslation(FileEntryList entryList, XmlNode node, string dir)
        {

            //string fileName = Path.Combine(TRGameInfo.Trans.TranslationSourceDirectory, "Subtitle.txt");
            //string textContent = File.ReadAllText(fileName, CineFile.textConv.Enc);

            //List<string> entryTexts = new List<string>(Regex.Split(textContent, "^HASH: ", RegexOptions.Multiline));

            //if (!Regex.Match(entryTexts[0], @"^([0-9A-F]{7,8});[^\n]*(dir|sub)\r\n", RegexOptions.Multiline).Success)
            //    entryTexts.RemoveAt(0);

            //Regex rxHash = new Regex(@"^([0-9A-F]{7,8});[^\n]*(dir|sub)\r\n", RegexOptions.Multiline);
            //Regex rxOriginal = new Regex(@"^#[^\r\n]*\r\n", RegexOptions.Multiline);
            //Regex rxDirective = new Regex(@"^\$[^\r\n]*\r\n", RegexOptions.Multiline);
            //Regex rxComment = new Regex(@"^;[^\r\n]*\r\n", RegexOptions.Multiline);
            //Regex rxIsNewText = new Regex(@"^([0-9]{5});([^\r\n]*)$");
            //Regex rxTrans = new Regex(@"^(.*)\r\n", RegexOptions.Multiline);

            //for (Int32 i = 0; i < entryTexts.Count; i++)
            //{
            //    string txt = entryTexts[i];

            //    //                string[] entryLines = txt.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            //    TranslationFileEntry transEntry = new TranslationFileEntry();
            //    Match mtch = null;
            //    MatchCollection mtchs = null;

            //    // parse hash & language;
            //    mtch = rxHash.Match(txt);
            //    UInt32 hash = 0;

            //    if (!mtch.Success)
            //        throw new Exception("Internal error: Subtitle identifier (hash) not found.");

            //    hash = UInt32.Parse(mtch.Result("$1"), System.Globalization.NumberStyles.HexNumber);
            //    bool isSub = mtch.Result("$2") == "sub";
            //    txt = rxHash.Replace(txt, string.Empty, 1);

            //    // parse original
            //    txt = rxOriginal.Replace(txt, string.Empty);
            //    // parse comments
            //    txt = rxComment.Replace(txt, string.Empty);

            //    CineTransBlockList transBlocks = new CineTransBlockList();
            //    cineTransFiles.Add(hash, transBlocks);

            //    CineBlockTranslationTextList blockTexts = null;

            //    // process directives
            //    mtchs = rxDirective.Matches(txt);
            //    foreach (Match m in mtchs)
            //        if (transEntry.Directives.Length == 0)
            //            transEntry.Directives = m.Result("$1").Replace("\r\n$", "\r\n");
            //        else
            //            transEntry.Directives += "\r\n" + m.Result("$1").Replace("\r\n$", "\r\n");
            //    txt = rxDirective.Replace(txt, string.Empty);

            //    // process translations
            //    mtchs = rxTrans.Matches(txt);
            //    string translation = string.Empty;
            //    for (Int32 k = 0; k < mtchs.Count - 1; k++)
            //    {
            //        Match m = mtchs[k];
            //        if (translation.Length == 0)
            //            translation = m.Result("$1");
            //        else
            //            translation += "\r\n" + m.Result("$1");
            //    }
            //    translation = translation.Replace("!TESZT! ", string.Empty);

            //    #region Apply directives
            //    //// $del
            //    //if (transEntry.Directives.Contains("DEL"))
            //    //    transEntry.Translation = "?";
            //    //// $setup
            //    //transEntry.KeepAccentedChars = transEntry.Directives.Contains("SETUP");
            //    //menuTransEntries.Add(Convert.ToInt32(transEntry.Hash), transEntry);

            //    #endregion

            //    List<string> transLines = new List<string>(Regex.Split(translation, @"\r\n"));
            //    for (Int32 j = 0; j < transLines.Count - 0; j++)
            //    {
            //        mtch = rxIsNewText.Match(transLines[j]);
            //        string blockText = string.Empty;
            //        if (mtch.Success)
            //        {
            //            UInt32 blockNo = UInt32.Parse(mtch.Result("$1"));
            //            blockText = mtch.Result("$2");

            //            if (!transBlocks.ContainsKey(blockNo))
            //            {
            //                blockTexts = new CineBlockTranslationTextList();
            //                transBlocks.Add(blockNo, blockTexts);
            //            }
            //            else // duplicate key
            //                blockTexts = transBlocks[blockNo];
            //            blockTexts.Add(blockText);
            //        }
            //        else
            //        {
            //            // add value to end of last entry.
            //            Int32 index = blockTexts.Count - 1;
            //            blockTexts[index] += "\r\n" + transLines[j];
            //        }
            //    }
            //}
        }

        // creates restoration xml
        void ITransProc.CreateRestoration(FileEntryList entryList, XmlNode node)
        {
            XmlElement subtitleElement = node.OwnerDocument.CreateElement("subtitle");
            XmlNode subtitleNode = node.AppendChild(subtitleElement);
            foreach (FileEntry file in entryList)
            {
                if ((file.Raw.Language == FileLanguage.English || file.Raw.Language == FileLanguage.NoLang) && IsMyFile(file))
                {
                    CineFile cine = new CineFile(file);
                    cine.CreateRestoration(subtitleElement, subtitleNode);
                }
            }
        }

        // translates game
        void ITransProc.Translate(FileEntryList entryList, XmlNode node, bool simulated)
        {
            foreach (FileEntry file in entryList)
            {
                if ((file.Raw.Language == FileLanguage.English /*|| file.Raw.Language == FileLanguage.NoLang*/) && IsMyFile(file))
                {
                    CineFile cine = new CineFile(file);
                    cine.Translate(simulated);
                }
            }
        }

        // restores game
        void ITransProc.Restore(FileEntryList entryList, XmlNode node, bool simulated)
        {
            foreach (FileEntry file in entryList)
            {
                if ((file.Raw.Language == FileLanguage.English /*|| file.Raw.Language == FileLanguage.NoLang*/) && IsMyFile(file))
                {
                    CineFile cine = new CineFile(file);
                    cine.Restore();
                }
            }
        }
    }
}
