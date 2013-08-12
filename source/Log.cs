using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TRTR
{
    internal enum LogEntryType { Debug, Info, Progress, Warning, Error, Critical };

    static class Log
    {
        private static StreamLogListener internalListener = new StreamLogListener(new MemoryStream());

        private static Dictionary<string, LogListener> Listeners = new Dictionary<string, LogListener>();

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

            Write(LogEntryType.Error, indentStr + ret.ToString().Replace("\n", "\n" + indentStr).TrimEnd());
            if (ex.InnerException != null)
                Write(ex.InnerException, indent + 4);
        }

        internal static void Write(Exception ex)
        {
            Write(ex, 0);
        }

        internal static void Write(LogEntryType type, string msgText)
        {
            LogMsg(new LogMessage
            {
                LogType = type,
                Message = msgText,
                Progress = 0,
                Time = DateTime.Now
            });
        }

        static Log()
        {
            AddListener("internal", internalListener);
        }

        internal static LogListener AddListener(string name, LogListener listener)
        {
            Listeners.Add(name, listener);
            return listener;
        }

        internal static bool RemoveListener(string name)
        {
            LogListener item;
            if (Listeners.TryGetValue(name, out item))
                item.FinalizeListener();
            return Listeners.Remove(name);
        }

        internal static void LogMsg(LogMessage logmsg)
        {
            foreach (LogListener lstnr in Listeners.Values)
                lstnr.LogMsg(logmsg);
        }

        internal static void LogMsg(LogEntryType type, string msg, int progress = 0)
        {
            LogMsg(new LogMessage
            {
                LogType = type,
                Message = msg,
                Progress = progress,
                Time = DateTime.Now,
            });
        }

        internal static void LogDebugMsg(string msg)
        {
            LogMsg(LogEntryType.Debug, msg);
        }

        internal static void LogProgress(string msg, int progress)
        {
            LogMsg(LogEntryType.Progress, msg, progress); 
        }
    }

    internal class LogMessage
    {
        internal LogEntryType LogType { get; set; }
        internal string Message { get; set; }
        internal int Progress { get; set; }
        internal DateTime Time { get; set; }
    }

    internal abstract class LogListener
    {
        internal abstract void InitializeListener();
        internal abstract void LogMsg(LogMessage msg);
        internal abstract void FinalizeListener();
    }

    internal class DebugLogListener : LogListener
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        internal override void InitializeListener()
        {
        }

        internal override void LogMsg(LogMessage msg)
        {

            string msgText;
            if(msg.LogType != LogEntryType.Progress)
                msgText = string.Format("{0} {1} {2}", msg.Time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffff"), msg.LogType.ToString(), msg.Message);
            else
                msgText = string.Format("{0} {1} {2} {3}%", msg.Time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffff"), msg.LogType.ToString(), msg.Message, msg.Progress);
            Debug.WriteLine(msgText);
            OutputDebugString(msgText);
        }

        internal override void FinalizeListener()
        {
        }

    }

    internal class StreamLogListener : LogListener
    {
        protected Stream internalStream;
        private Encoding encoding = Encoding.UTF8;

        internal override void InitializeListener() { }

        internal override void LogMsg(LogMessage msg)
        {
            if (msg.LogType != LogEntryType.Progress) // suppress progress reports
            {
                byte[] data = encoding.GetBytes(string.Format("{0} {1} {2}\r\n", msg.Time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffff"), msg.LogType.ToString(), msg.Message));
                internalStream.Write(data, 0, data.Length);
            }
        }

        internal override void FinalizeListener()
        {
            internalStream.Close();
        }

        internal StreamLogListener(Stream stream)
        {
            if (stream != null)
            {
                this.internalStream = stream;
                InitializeListener();
            }
        }
    }

    internal class FileLogListener : StreamLogListener
    {
        internal static uint LOG_HISTORY_SIZE = 5;

        private void rotateLogs()
        {
            if (File.Exists(string.Format("{0}.{1}", fileName, LOG_HISTORY_SIZE)))
                File.Delete(string.Format("{0}.{1}", fileName, LOG_HISTORY_SIZE));

            for (uint i = LOG_HISTORY_SIZE - 1; i > 0; i--)
                if (File.Exists(string.Format("{0}.{1}", fileName, i)))
                    File.Move(string.Format("{0}.{1}", fileName, i), string.Format("{0}.{1}", fileName, i + 1));

            if (File.Exists(fileName))
                File.Move(fileName, string.Format("{0}.{1}", fileName, 1));
        }

        internal FileLogListener(string fileName)
            : this(fileName, Encoding.UTF8)
        {
        }

        internal FileLogListener(string fileName, Encoding encoding)
            : base(null)
        {
            this.fileName = fileName;
            this.encoding = encoding;
            // rotate logs
            rotateLogs();
            this.internalStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }


        FileStream fs;
        private string fileName = string.Empty;
        private Encoding encoding = Encoding.UTF8;

        private void EnsureOpened()
        {
            if (fs == null)
            {
                bool notExists = !File.Exists(fileName);

                if (!notExists)
                {
                    File.Delete(fileName);
                    notExists = true;
                }

                fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
                if (notExists)
                    fs.Write(new byte[] { 0xEF, 0xBB, 0xBF }, 0, 3);
                byte[] buf = encoding.GetBytes(String.Format("\r\n\r\nLog start: {0}\r\n", DateTime.Now.ToString()));
                fs.Write(buf, 0, buf.Length);
            }
            if (encoding == null)
                encoding = new UTF8Encoding();
        }


    }

}
