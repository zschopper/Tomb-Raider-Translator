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

        private static Dictionary<string, ILogListener> Listeners = new Dictionary<string, ILogListener>();

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

        internal static ILogListener AddListener(string name, ILogListener listener)
        {
            Listeners.Add(name, listener);
            return listener;
        }

        internal static bool RemoveListener(string name)
        {
            ILogListener item;
            if (Listeners.TryGetValue(name, out item))
                item.FinalizeListener();
            return Listeners.Remove(name);
        }

        internal static void LogMsg(LogMessage logmsg)
        {
            foreach (ILogListener lstnr in Listeners.Values)
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

        internal static string GetLogContents()
        {
            return GetLogContents(Encoding.Unicode);
        }

        internal static string GetLogContents(Encoding encoding)
        {
            return internalListener.GetContents(encoding);
        }
    }

    public class LogMessage
    {
        internal LogEntryType LogType { get; set; }
        internal string Message { get; set; }
        internal int Progress { get; set; }
        internal DateTime Time { get; set; }
    }

    interface ILogListener
    {
        void InitializeListener();
        void LogMsg(LogMessage msg);
        void FinalizeListener();
    }

    public class DebugLogListener : ILogListener
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        public void InitializeListener() { }

        public void LogMsg(LogMessage msg)
        {
            string msgText;
            if (msg.LogType != LogEntryType.Progress)
                msgText = string.Format("{0} {1} {2}", msg.Time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffff"), msg.LogType.ToString(), msg.Message);
            else
                msgText = string.Format("{0} {1} {2} {3}%", msg.Time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffff"), msg.LogType.ToString(), msg.Message, msg.Progress);
            Debug.WriteLine(msgText);
            OutputDebugString(msgText);
        }

        public void FinalizeListener()
        {
        }

    }

    public class StreamLogListener : ILogListener
    {
        protected Stream internalStream;
        private Encoding encoding = Encoding.UTF8;

        public void InitializeListener() { }

        public void LogMsg(LogMessage msg)
        {
            if (msg.LogType != LogEntryType.Progress) // suppress progress reports
            {
                byte[] data = encoding.GetBytes(string.Format("{0} {1} {2}\r\n", msg.Time.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffff"), msg.LogType.ToString(), msg.Message));
                internalStream.Write(data, 0, data.Length);
            }
        }

        public void FinalizeListener()
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

        internal string GetContents(Encoding encoding)
        {
            byte[] data = new byte[internalStream.Length];
            internalStream.Position = 0;
            internalStream.Read(data, 0, data.Length);

            if (this.encoding != encoding)
                data = Encoding.Convert(this.encoding, encoding, data);
            return encoding.GetString(data);

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
