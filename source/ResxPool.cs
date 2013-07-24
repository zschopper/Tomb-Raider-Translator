using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Resources;
using System.Diagnostics;


namespace TRTR
{
    internal enum ResXLockMode { Read, Write }

    class ResXHelper
    {

        #region private declarations
        private string fileName;
        private int lockLevel = 0;
        private ResXLockMode lockMode;
        private ResXResourceReader reader = null;
        private ResXResourceWriter writer = null;
        #endregion

        #region internal declarations
        internal string FileName { get { return fileName; } }
        internal ResXLockMode LockMode { get { return lockMode; } }
        internal int LockLevel { get { return lockLevel; } }
        internal ResXResourceReader Reader { get { return getReader(); } }
        internal ResXResourceWriter Writer { get { return getWriter(); } }
        #endregion

        internal ResXHelper(string fileName)
        {
            this.fileName = fileName;
        }

        private ResXResourceReader getReader()
        {
            if (!TryLockFor(ResXLockMode.Read))
                return null;

            if (writer != null)
            {
                writer.Generate();
                writer.Close();
                writer = null;
            }

            if (reader == null)
                reader = new ResXResourceReader(fileName);

            return reader;
        }

        private ResXResourceWriter getWriter()
        {
            if (!TryLockFor(ResXLockMode.Write))
                return null;

            if (reader != null)
            {
                reader.Close();
                reader = null;
            }

            if (writer == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName)); 
                writer = new ResXResourceWriter(fileName);
            }

            return writer;
        }

        internal bool TryLockFor(ResXLockMode lockMode)
        {
            if (lockLevel == 0)
            {
                lockLevel = 1;
                this.lockMode = lockMode;
                return true;
            }

            if (lockMode == this.lockMode)
            {
                lockLevel++;
                return true;
            }
            return false;
        }

        internal void Unlock()
        {
            if (lockLevel < 0)
                lockLevel--;
        }
    }

    class ResXPool
    {
        #region private declarations
        private static Dictionary<string, ResXHelper> list = new Dictionary<string, ResXHelper>();
        #endregion

        internal static ResXHelper GetResX(string fileName)
        {
            ResXHelper ret;
            if (!list.TryGetValue(fileName, out ret))
            {
                ret = new ResXHelper(fileName);
                list.Add(fileName, ret);
            }
            return ret;
        }

        internal static void CloseAll()
        {
            foreach (KeyValuePair<string, ResXHelper> helper in list)
            {
                if(helper.Value.LockMode == ResXLockMode.Write)
                {
                    helper.Value.Writer.Generate();
                    helper.Value.Writer.Close();
                }
                else
                    helper.Value.Reader.Close();
            }
            list.Clear();
        }
    }
}
