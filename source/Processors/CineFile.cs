using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using ExtensionMethods;

namespace TRTR
{
    static class CineConsts
    {
        internal static UInt32 MinBlockSize = 7; // length indicator (4 bytes) + lang code (1-2 byte(s)) + separator (1 byte) closing separator (1 byte)
        internal static UInt32 MaxBlockSize = 0x200000; // tra/trl = 0x20000; tru = 0x200000; // v1.0.0.6
        internal static UInt32 MaxTextBlockSize = 1000;
        internal static UInt32 CineHeaderSize = 0x2000; //800
        internal static UInt32 CineBlockHeaderSize = 0x10;
        internal static UInt32 CineCineBlockMagicBytes = 0x43494E45;  //ENIC
        internal static List<FileLanguage> StrippedLangs = new List<FileLanguage>();

        static CineConsts()
        {
            //StrippedLangs.Add(FileLanguage.Russian);
            //StrippedLangs.Add(FileLanguage.French);
            //StrippedLangs.Add(FileLanguage.Chinese);
            //StrippedLangs.Add(FileLanguage.Arabic);
        }
    }

    static class CineFile
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
            bool ret = false;
            //if (entry.Hash != 0x3E2465ECu)
            //    return;
            //Log.LogDebugMsg(string.Format("Processing: {0}", entry.Extra.FileNameOnlyForced));
            Int64 startInPos = inStream.Position;
            Int64 startOutPos = outStream.Position;

            // dump
            //BigFileList.DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, entry.Extra.FileNameOnlyForced), entry); // "cine_tmp.dump"
            //inStream.Position = startPos;

            // write _file_ header to output
            outStream.WriteFromStream(inStream, CineConsts.CineHeaderSize);

            UInt32 blockNo = 0;

            // processing blocks
            while (inStream.Position < startInPos + contentLength)
            {
                Int64 _debugBlockStart = inStream.Position - startInPos;
                UInt32 blockType = inStream.ReadUInt32();
                UInt32 blockSize = inStream.ReadUInt32();
                inStream.Position -= 2 * sizeof(UInt32);
                Int64 blockCheck = inStream.Position + (blockSize + 0x10).ExtendToBoundary(0x10);

                // parse cine block

                #region dump
                bool dump = false;
                if (dump)
                {
                    FileStream fs = new FileStream(Path.Combine(TRGameInfo.Game.WorkFolder, "tmp.dump"), FileMode.Create);
                    try
                    {
                        Int64 _inpos = inStream.Position;
                        //fs.Position = startPos;
                        fs.WriteFromStream(inStream, blockSize + 0x10);
                        inStream.Position = _inpos;
                        //inStream.Position = inStream.Position.ExtendToBoundary(0x10);
                    }
                    finally
                    {
                        fs.Close();
                    }
                }
                #endregion

                //if (entry.Hash == 0x3E2465ECu)
                //    Debug.WriteLine(string.Format("blockNo: {0} pos: {1:X8} len: {2:X8}", blockNo, inStream.Position - entry.Raw.Address, blockSize));

                switch (blockType)
                {
                    case 0:
                    case 3:
                        processBinaryBlock(inStream, outStream);
                        break;
                    case 1:
                        if (processType1Block(inStream, outStream, entry, blockNo, tp))
                            ret = true;
                        break;
                    default:
                        throw new Exception(Errors.ParseErrorBlockTypeError);
                }
                inStream.Position += inStream.Position.DiffToNextBoundary(0x10);

                //if ((outStream.Position - startOutPos) != (inStream.Position - startInPos))
                //    Noop.DoIt();

                //if ((outStream.Position - startOutPos).DiffToNextBoundary(0x10) != 0)
                //    Noop.DoIt();

                //if ((inStream.Position) - startInPos != (outStream.Position - startOutPos))
                //    Debug.WriteLine(string.Format("in: {0:X8} out: {1:X8} diff: {2:X8} {3}", (inStream.Position) - startInPos, (outStream.Position - startOutPos), (inStream.Position) - startInPos - (outStream.Position - startOutPos), blockType));

                blockNo++;
            }





            //Log.LogDebugMsg(string.Format("Block Count {0} {0:X8}", blockNo));
            return ret;
        }

        private static bool processType1Block(Stream inStream, Stream outStream, FileEntry entry, UInt32 blockNo, TranslationProvider tran)
        {
            //if (entry.Hash == 0x049164d5 && blockNo == 0x00000079)
            //    Noop.DoIt();

            long startInPos = inStream.Position;
            long startOutPos = outStream.Position;
            uint blockType = inStream.ReadUInt32();
            uint blockContentLength = inStream.ReadUInt32();
            uint blockHdr0008 = inStream.ReadUInt32();
            uint blockHdr000C = inStream.ReadUInt32();
            long blockStartPos = inStream.Position;
            uint cineCnt0010 = inStream.ReadUInt32();
            bool isEnic = cineCnt0010 == 0x43494E45; // "ENIC"

            bool hasSubtitle = false;
            long textStartPos = 0;
            long textEndPos = 0;
            string subtitleText = string.Empty;

            if (isEnic)
            {
                UInt32 cineCnt0014 = inStream.ReadUInt32();
                UInt32 cineCnt0018 = inStream.ReadUInt32();
                hasSubtitle = cineCnt0018 == 0x64 && blockContentLength > 0x80;
                if (hasSubtitle)
                {
                    // cineheader:
                    byte[] cineCnt001C = new byte[0x60];         // 0x001C-0x007B
                    inStream.Read(cineCnt001C, 0, cineCnt001C.Length);
                    UInt32 textBlockLen = inStream.ReadUInt32(); // 0x007C
                    UInt32 cineCnt0080 = inStream.ReadUInt32();  // 0x0080
                    UInt32 transTextLen = inStream.ReadUInt32(); // 0x0084
                    byte[] transText = new byte[transTextLen];   // 0x0088
                    inStream.Read(transText, 0, transText.Length);
                    //inStream.Move(textBlockLen - transTextLen - 8);
                    inStream.Position += inStream.Position.DiffToNextBoundary(0x10);


                    subtitleText = TRGameInfo.Conv.Enc.GetString(transText);
                    byte[] translated = TranslateTextBlock(subtitleText, entry, blockNo, tran);

                    if (outStream != Stream.Null)
                    {
                        UInt32 newContentLength = (0x88 + (UInt32)translated.Length).ExtendToBoundary(0x10);
                        // block header
                        outStream.WriteUInt32(blockType);
                        outStream.WriteUInt32((0x78 + (UInt32)translated.Length).ExtendToBoundary(0x10));
                        outStream.WriteUInt32(blockHdr0008);
                        outStream.WriteUInt32(blockHdr000C);
                        // block content
                        outStream.WriteUInt32(0x43494E45); // "ENIC"
                        outStream.WriteUInt32(cineCnt0014);
                        outStream.WriteUInt32(cineCnt0018);
                        outStream.Write(cineCnt001C, 0, cineCnt001C.Length);
                        outStream.WriteUInt32((0x08 + (UInt32)translated.Length).ExtendToBoundary(0x10));
                        outStream.WriteUInt32(cineCnt0080);
                        outStream.WriteUInt32((UInt32)translated.Length);
                        outStream.Write(translated, 0, translated.Length);
                        // extend out stream to 16 byte boundary
                        UInt32 padLen = (UInt32)(outStream.Position - startOutPos).DiffToNextBoundary(0x10);
                        byte[] padding = new byte[padLen];
                        outStream.Write(padding, 0, padding.Length);
                    }
                    //if ((inStream.Position) - startInPos != (outStream.Position - startOutPos))
                    //    Debug.WriteLine(string.Format("in: {0:X8} out: {1:X8} diff: {2:X8} {3}", (inStream.Position) - startInPos, (outStream.Position - startOutPos), (inStream.Position) - startInPos - (outStream.Position - startOutPos), blockType));

                }
            }
            else
            {
                #region try find non-enic text
                #region test #1
                // test #1 - it can have subtitles, if last non zero is 0x0D

                int lastNotNull = (int)(blockStartPos + blockContentLength);
                byte charRead = 0;

                inStream.Position = lastNotNull;
                while (charRead == 0 && inStream.Position > blockStartPos)
                {
                    inStream.Position -= 1;
                    charRead = inStream.PeekByte();
                }
                // test #1 - passed
                #endregion

                #region test #2
                if (charRead == 0x0D && inStream.Position > blockStartPos + 4)
                {
                    textEndPos = inStream.Position;

                    // test #2 - read integers backward & try to found text size (offset to textEndPos)
                    inStream.Position = (inStream.Position - startInPos - 4).ExtendToBoundary(4) + startInPos;

                    while ((textEndPos - inStream.Position) < CineConsts.MaxBlockSize && inStream.Position > blockStartPos && subtitleText == string.Empty)
                    {
                        // old method: search first 0 character (length value of textblock)
                        // new method: search text block content-length data on 4 bytes boundary
                        UInt32 readNumber = inStream.ReadUInt32();
                        // Debug.WriteLine(string.Format("testing {0:X4}", readNumber));
                        if (readNumber == 0)
                            break;
                        if (readNumber == textEndPos + 1 - inStream.Position)
                        {
                            textStartPos = inStream.Position;
                            byte[] buf = new byte[readNumber];
                            inStream.Read(buf, 0, buf.Length);
                            inStream.Position += inStream.Position.DiffToNextBoundary(0x10);

                            subtitleText = TRGameInfo.Conv.Enc.GetString(buf);
                            hasSubtitle = true;
                        }
                        else
                            inStream.Position -= 8;
                    }
                }
                #endregion
                #endregion
                #region it has subtitle
                if (subtitleText != string.Empty)
                {
                    if (entry.Hash == 0x3e2465ec && blockNo == 0x00000107)
                        Noop.DoIt();
                    byte[] translated = TranslateTextBlock(subtitleText, entry, blockNo, tran);
                    if (outStream != Stream.Null)
                    {
                        // write block header
                        outStream.WriteUInt32(blockType);
                        outStream.WriteUInt32((UInt32)(textStartPos - startInPos + translated.Length - 0x10).ExtendToBoundary(0x10));
                        outStream.WriteUInt32(blockHdr0008);
                        outStream.WriteUInt32(blockHdr000C);

                        // set instream position after block header
                        inStream.Position = startInPos + 0x10;

                        // calculate initial binary block content data length
                        // text offset in block (textStartPos - startInPos)  -  block header size (0x10) - stored translation length (4)
                        Int64 binDataLen = textStartPos - startInPos - 0x10 - 4;
                        // write binary data (copy from instream)
                        outStream.WriteFromStream(inStream, binDataLen);

                        // write translated text len, text
                        outStream.WriteUInt32((UInt32)translated.Length);
                        outStream.Write(translated, 0, translated.Length);

                        // extend out stream to 16 byte boundary
                        UInt32 padLen = (UInt32)(outStream.Position - startOutPos).DiffToNextBoundary(0x10);
                        byte[] padding = new byte[padLen];
                        outStream.Write(padding, 0, padding.Length);
                    }
                }
                #endregion
            }

            #region handle as binary data - copy block to output stream
            if (subtitleText == string.Empty)
            {
                inStream.Position = startInPos;
                outStream.WriteFromStream(inStream, (blockContentLength + CineConsts.CineBlockHeaderSize));

                // extend out stream to 16 byte boundary
                UInt32 padLen = (UInt32)(outStream.Position - startOutPos).DiffToNextBoundary(0x10);
                byte[] padding = new byte[padLen];
                outStream.Write(padding, 0, padding.Length);

                inStream.Position += inStream.Position.DiffToNextBoundary(0x10);
            }
            #endregion

            inStream.Position = startInPos + blockContentLength + 0x10;
            return subtitleText != string.Empty;
        }

        private static void processBinaryBlock(Stream inStream, Stream outStream)
        {
            long startInPos = inStream.Position;
            long startOutPos = outStream.Position;

            uint blockType = inStream.ReadUInt32();
            uint blockContentLength = inStream.ReadUInt32();
            uint headerUnk0008 = inStream.ReadUInt32();
            uint headerUnk000C = inStream.ReadUInt32();

            outStream.WriteUInt32(blockType);
            outStream.WriteUInt32(blockContentLength);
            outStream.WriteUInt32(headerUnk0008);
            outStream.WriteUInt32(headerUnk000C);
            outStream.WriteFromStream(inStream, blockContentLength);
            inStream.Position = inStream.Position.ExtendToBoundary(0x10);

            // extend out stream to 16 byte boundary
            UInt32 padLen = (UInt32)(outStream.Position - startOutPos).DiffToNextBoundary(0x10);
            byte[] padding = new byte[padLen];
            outStream.Write(padding, 0, padding.Length);

            if ((inStream.Position) - startInPos != (outStream.Position - startOutPos))
                Noop.DoIt();
        }

        private static byte[] TranslateTextBlock(string textBlock, FileEntry entry, UInt32 blockNo, TranslationProvider tp)
        {
            Regex rx = new Regex(@"^(\[[0-9a-z\.]+\]|)(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            StringBuilder ret = new StringBuilder();

            // split texts 
            //string[] elements = textBlock.Substring(4).Split((char)0x0D); // remove content-length value and split texts at delimiters
            string[] elements = textBlock.Split((char)0x0D); // remove content-length value and split texts at delimiters
            Dictionary<int, string> dupeFilter = new Dictionary<int, string>(); // for avoiding dupes in bigfile_english/thechosenone.mul
            for (Int32 i = 0; i < elements.Length - 1; i += 2)
            {
                // parse language code
                string langCode = elements[i];
                Int32 langCodeValue;
                if (!Int32.TryParse(langCode, out langCodeValue))
                    throw new Exception(string.Format("Invalid language code: \"{0}\"", langCode));

                string textElement = elements[i + 1];
                FileLanguage language = (FileLanguage)langCodeValue;

                if (language == FileLanguage.English)
                {
                    // debug
                    if (textBlock.Length > 2)
                        if (textElement.Length > 0)
                            if (textElement[0] == '[' && textElement[textElement.Length - 1] == ']')
                                Noop.DoIt();

                    string translated = string.Empty;
                    string prefix = string.Empty;
                    string text = string.Empty;

                    // split text element into prefix and text
                    Match m = rx.Match(textElement);
                    if (m.Success)
                    {
                        prefix = m.Groups[1].Value;
                        text = m.Groups[2].Value;

                        if (text == string.Empty)
                            Log.LogDebugMsg(string.Format("Empty CineFile text: \"{0}\" Lang: {1}", textBlock, language.ToString())); //trans
                    }
                    else
                    {
                        prefix = string.Empty;
                        text = textElement;
                    }
                    string[] context = null;

                    if (tp.UseContext)
                        context = new string[] { 
                            "blockNo", blockNo.ToString("X8"),
                            "prefix", prefix,
                            "filename", entry.Extra.FileNameForced, 
                            "hash", entry.HashText, 
                            "bigfile", entry.BigFile.Name,
                        };

                    if (!dupeFilter.TryGetValue(textElement.GetHashCode(), out translated))
                    {
                        translated = TRGameInfo.Conv.ToGameFormat(tp.GetTranslation(text.Replace("\n", "\r\n"), entry, context)).Replace("\r\n", "\n");
                        dupeFilter.Add(textElement.GetHashCode(), translated);  // for avoiding dupes in bigfile_english/thechosenone.mul
                    }

                    ret.Append(langCode + (char)0x0D + prefix + translated + (char)0x0D);
                }
                else
                {
                    // non-english texts: simply add them to return value
                    ret.Append(langCode + (char)0x0D + textElement + (char)0x0D);
                }
            }
            return Encoding.UTF8.GetBytes(ret.ToString());
        }
    }
}
