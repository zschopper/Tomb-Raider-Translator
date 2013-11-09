using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ExtensionMethods;

namespace TRTR
{


    class FontV3
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
            byte[] buf = entry.ReadContent();
            Directory.CreateDirectory(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", entry.BigFile.Name));
            BigFileList.DumpToFile(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", entry.BigFile.Name, entry.Extra.FileNameOnlyForced), entry);
            inStream.Position = entry.Raw.Address;
            DRMFile drm = new DRMFile(entry);
            drm.Parse(inStream, contentLength, outStream);

            return true;
        }
    }
}
