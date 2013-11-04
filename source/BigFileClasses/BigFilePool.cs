using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TRTR
{
    // Physical file
    class FilePoolEntry : IDisposable
    {
        internal FileStream Stream = null;
        private Int32 count = 0;
        Int32 Count { get { return count; } }
        string fileName = string.Empty;

        internal Int32 Open()
        {
            if (count++ == 0)
                Stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            return count;
        }

        internal Int32 Close()
        {
            if (--count == 0)
            {
                if (Stream != null)
                {
                    FileStream fs = Stream;
                    Stream = null;
                    fs.Close();
                }
            }
            return count;
        }

        internal void CloseAll()
        {
            if (Stream != null)
            {
                FileStream fs = Stream;
                Stream = null;
                fs.Close();
            }
            count = 0;
        }

        internal FilePoolEntry(string fileName)
        {
            this.fileName = fileName;
        }

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Close();
                Stream.Dispose();
                Stream = null;
            }
        }
    }

    internal class BigFilePool
    {
        #region private declarations
        private Dictionary<string, FilePoolEntry> pool = new Dictionary<string, FilePoolEntry>();
        private BigFileList bigFiles;
        #endregion

        internal BigFilePool(BigFileList bigFiles)
        {
            this.bigFiles = bigFiles;
        }

        internal static string CreateKey(string name, UInt32 index)
        {
            return string.Format("{0}.{1:D3}", name, index);
        }

        internal static string CreateKey(FileEntry entry)
        {
            return CreateKey(entry.BigFile.Name, entry.Raw.BigFileIndex);
        }

        internal FileStream Open(FileEntry entry)
        {
            return Open(entry.BigFile.Name, entry.Raw.BigFileIndex);
        }

        internal FileStream Open(string name, UInt32 index)
        {

            FilePoolEntry entry;
            string key = CreateKey(name, index);

            if (!pool.ContainsKey(key))
            {
                BigFileV3 bigFile = null;
                if (!bigFiles.ItemsByName.TryGetValue(name, out bigFile))
                {
                    bigFile = null;
                }
                entry = new FilePoolEntry(string.Format(bigFile.FilePatternFull, index));
                pool.Add(key, entry);
            }
            else
                entry = pool[key];

            entry.Open();
            return entry.Stream;
        }

        internal void CloseAll()
        {
            foreach (string key in pool.Keys)
                pool[key].CloseAll();
            pool.Clear();
        }

        internal void Close(string name, UInt32 index)
        {
            Close(CreateKey(name, index));
        }

        internal void Close(FileEntry entry)
        {
            Close(CreateKey(entry));
        }

        internal void Close(string key)
        {
            pool[key].Close();
        }
    }

    // not in use
    internal static class BigFilePoolSingleton
    {
        #region private declarations
        private static Dictionary<string, FilePoolEntry> pool = new Dictionary<string, FilePoolEntry>();
        private static BigFileList bigFiles;
        #endregion

        internal static void SetBigFiles(BigFileList bigFiles)
        {
            BigFilePoolSingleton.bigFiles = bigFiles;
        }

        internal static string CreateKey(string name, UInt32 index)
        {
            return string.Format("{0}.{1:D3}", name, index);
        }

        internal static string CreateKey(FileEntry entry)
        {
            return CreateKey(entry.BigFile.Name, entry.Raw.BigFileIndex);
        }

        internal static FileStream Open(FileEntry entry)
        {
            return Open(entry.BigFile.Name, entry.Raw.BigFileIndex);
        }

        internal static FileStream Open(string name, UInt32 index)
        {

            FilePoolEntry entry;
            string key = CreateKey(name, index);

            if (!pool.ContainsKey(key))
            {
                BigFileV3 bigFile = null;
                if (!bigFiles.ItemsByName.TryGetValue(name, out bigFile))
                {
                    bigFile = null;
                }
                entry = new FilePoolEntry(string.Format(bigFile.FilePatternFull, index));
                pool.Add(key, entry);
            }
            else
                entry = pool[key];

            entry.Open();
            return entry.Stream;
        }

        internal static void CloseAll()
        {
            foreach (string key in pool.Keys)
                pool[key].CloseAll();
            pool.Clear();
        }

        internal static void Close(string name, UInt32 index)
        {
            Close(CreateKey(name, index));
        }

        internal static void Close(FileEntry entry)
        {
            Close(CreateKey(entry));
        }

        internal static void Close(string key)
        {
            pool[key].Close();
        }
    }
}
