using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TRTR
{
    static class BigFileBackupCreator
    {
        internal static void AddContainedFile(FileEntry entry, FATEntry newRaw)
        {
            FileStream fs = TRGameInfo.FilePool.Open(entry);
            try
            {
                fs.Position = entry.Raw.Address;
                Log.LogDebugMsg(string.Format("File hash: {0}", entry.HashText));
                Log.LogDebugMsg(string.Format("File name: {0}", entry.Extra.FileNameForced));
                Log.LogDebugMsg(string.Format("File size: {0} {1}", entry.Raw.Length, newRaw.Length));
                Log.LogDebugMsg(string.Format("Offset: {0} {1}", entry.Raw.Address , newRaw.Address));
                Log.LogDebugMsg(string.Format("Bigfile name: {0} {1}", entry.BigFile.Name, newRaw.BigFile.Name));
                Log.LogDebugMsg(string.Format("Bigfile index: {0} {1}", entry.Raw.BigFileIndex, newRaw.BigFileIndex));
                Log.LogDebugMsg(string.Format("Bigfile size: {0}", fs.Length));

            }
            finally
            {
                TRGameInfo.FilePool.Close(entry);
            }
        }
    }
}
