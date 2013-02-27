using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace TRTR
{
    class TextConv
    {
        Encoding enc;

        internal Encoding Enc { get { return enc; } }
        private char[] textChars = null;
        private char[] gameChars = null;
        private bool isConversionNeeded;

        internal static string CanonizeFileName(string fileName)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(fileName, "");

        }

        public TextConv(char[] textChars, char[] gameChars, Encoding encoding)
        {
            enc = new UTF8Encoding();
            if (textChars.Length != gameChars.Length)
                throw new Exception(Errors.InvalidTextReplaceSettings);
            this.textChars = textChars;
            this.gameChars = gameChars;

            if (textChars == null || gameChars == null)
                isConversionNeeded = false;
            else
                isConversionNeeded = textChars.Length > 0;


            //GameFormat[0] = '\u00F4'; //o^
            //GameFormat[1] = '\u00FB'; //u^
            //GameFormat[2] = '\u00D4'; //O^
            //GameFormat[3] = '\u00DB'; //U^

            //Originals[0] = '\u0151'; //o"
            //Originals[1] = '\u0171'; //u"
            //Originals[2] = '\u0150'; //O"
            //Originals[3] = '\u0170'; //U"
        }

        internal string ToGameFormat(string text)
        {
            if (!isConversionNeeded)
                return text;
            StringBuilder sb = new StringBuilder(text);
            for (Int32 i = 0; i < gameChars.Length; i++)
                sb.Replace(textChars[i], gameChars[i]);

            return sb.ToString();
        }

        internal string ToOriginalFormat(string text)
        {
            if (!isConversionNeeded)
                return text;
            StringBuilder sb = new StringBuilder(text);
            for (Int32 i = 0; i < textChars.Length; i++)
                sb.Replace(gameChars[i], textChars[i]);

            //if (text != sb.ToString())
            //    return sb.ToString();
            //else
            return text;
        }
    }
}
