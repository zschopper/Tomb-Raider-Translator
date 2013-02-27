using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace TRTR
{
    static class Log
    {
        private static FileStream fs = null;
        private static string fileName = ".\\TRTR.log";
        private static bool clear = true;
        private static Encoding enc = new UTF8Encoding();

        internal static string FileName
        {
            get { return fileName; }
        }

        [Conditional("DEBUGLOG")]
        private static void EnsureOpened()
        {
            if (fs == null)
            {
                bool notExists = !File.Exists(fileName);

                if (!notExists && clear)
                {
                    File.Delete(fileName);
                    notExists = true;
                }

                fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
                if (notExists)
                    fs.Write(new byte[] {0xEF, 0xBB, 0xBF}, 0, 3);
                byte[] buf = enc.GetBytes(String.Format("\r\n\r\nLog start: {0}\r\n", DateTime.Now.ToString()));
                fs.Write(buf, 0, buf.Length);
            }
            if (enc == null)
                enc = new UTF8Encoding();
        }

        [Conditional("DEBUGLOG")]
        private static void Write(Exception ex, Int32 indent)
        {
            
            char[] indentChars = new char[indent];
            for (Int32 i = 0; i < indentChars.Length; i++)
                indentChars[i] = ' ';
            string indentStr = new string(indentChars);
            StringBuilder ret = new StringBuilder();
            ret.AppendFormat("Message: {0}\r\n", ex.Message);
            ret.AppendFormat("Source: {0}\r\n", ex.Source);
            foreach (object ob in ex.Data.Keys)
            {
                object Data = ex.Data[ob];
                ret.AppendFormat("Data[{0}]: {1}\r\n", ob, ex.Data[ob]);
                if (Data is string)
                {
                    ret.Append("Hex Values: ");
                    foreach (char c in (string)(ex.Data[ob]))
                        ret.Append(((Int32)c).ToString("x2") + " ");
                    ret.Append("\r\n");
                }
            }
            ret.AppendFormat("StackTrace:\r\n{0}\r\n", ex.StackTrace);
            if (ex.InnerException == null)
                ret.Append("InnerException: [none]\r\n");
            else
                ret.Append("InnerException: [details below]\r\n");

            Write(indentStr + ret.ToString().Replace("\n", "\n" + indentStr).TrimEnd());
            if (ex.InnerException != null)
                Write(ex.InnerException, indent + 4);
        }

        [Conditional("DEBUGLOG")]
        internal static void Write(Exception ex)
        {
            Write(ex, 0);
        }

        [Conditional("DEBUGLOG")]
        internal static void Write(string msg)
        {
            Write(enc.GetBytes(msg + "\r\n"));
        }

        [Conditional("DEBUGLOG")]
        internal static void Write(byte[] data)
        {
            EnsureOpened();
            fs.Write(data, 0, data.Length);
        }

        [Conditional("DEBUGLOG")]
        internal static void Close()
        {
            if (fs != null)
            {
                fs.Close();
                fs = null;
                fileName = string.Empty;
            }
        }
    }
}
