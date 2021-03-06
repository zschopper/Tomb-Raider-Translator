﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ExtensionMethods;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace TRTR
{
    partial class DRMFile
    {
        public enum SectionType : byte
        {
            Generic = 0,
            Empty = 1,
            Animation = 2,
            RenderResource = 5,
            Wave = 6,
            DTPData = 7,
            Script = 8,
            ShaderLib = 9,
            Material = 10,
            Object = 11,
            RenderMesh = 12,
            CollisionMesh = 13,
            StreamGroupList = 14,
        }

        public struct DRMHeader
        {
            public int OrigIdx;
            public uint DataSize;
            public SectionType Type;
            public byte Unknown05;
            public ushort Unknown06;
            public uint Flags;
            public uint Id;
            public uint Unknown10;
            public uint U1;
            public uint Address;
            public uint Length;
            public uint U4;
            public IBigFile BigFile;
            public uint BigFileIdx;
            public uint BigFilePriority;
            public uint Offset;
        }
    }

    partial class DRMFile
    {
        #region private declarations
        IFileEntry entry = null;
        #endregion

        IFileEntry Entry { get { return entry; } }

        internal DRMFile(IFileEntry entry)
        {
            this.entry = entry;
        }

        internal void Parse(Stream inStream, long contentLength, Stream outStream)
        {
            Int64 startInPos = inStream.Position;
            ;

            uint Magic = inStream.ReadUInt32(); // version
            uint unknown04_Size = inStream.ReadUInt32();
            uint unknown08_Size = inStream.ReadUInt32();
            uint unknown0C = inStream.ReadUInt32(); // extra data after first block?
            uint unknown10 = inStream.ReadUInt32();
            uint Flags = inStream.ReadUInt32();
            uint sectionCount = inStream.ReadUInt32();
            uint unknown1C_Count = inStream.ReadUInt32();
            uint ssum = 0;
            DRMFile.DRMHeader[] header = new DRMHeader[sectionCount];
            for (int i = 0; i < sectionCount; i++)
            {
                header[i].OrigIdx = i;
                header[i].DataSize = inStream.ReadUInt32();
                header[i].Type = (SectionType)(inStream.ReadUInt8());
                header[i].Unknown05 = inStream.ReadUInt8();
                header[i].Unknown06 = inStream.ReadUInt16();
                header[i].Flags = inStream.ReadUInt32();
                header[i].Id = inStream.ReadUInt32();
                header[i].Unknown10 = inStream.ReadUInt32();
            }

            if (unknown04_Size > 0)
                inStream.Position += unknown04_Size;

            for (int i = 0; i < sectionCount; i++)
            {
                header[i].U1 = inStream.ReadUInt32();
                header[i].Address = inStream.ReadUInt32();
                header[i].Length = inStream.ReadUInt32();
                header[i].U4 = inStream.ReadUInt32();
                header[i].BigFileIdx = header[i].Address & 0xF;
                header[i].BigFilePriority = (header[i].Address % 0x800) >> 4;
                header[i].Offset = header[i].Address & 0xFFFFF800;
                header[i].BigFile = entry.BigFile.Parent.GetBigFileByPriority(header[i].BigFilePriority, false);
                ssum += header[i].DataSize;
            }
            Array.Sort<DRMHeader>(header, ((e1, e2) =>
            {
                int i = e1.BigFilePriority.CompareTo(e2.BigFilePriority);
                if (i != 0)
                    return i;
                i = e1.BigFileIdx.CompareTo(e2.BigFileIdx);
                if (i != 0)
                    return i;
                return e1.Offset.CompareTo(e2.Offset);

                //int i = e1.Id.CompareTo(e2.Id);
                //if (i != 0)
                //    return i;
                //i = e1.Unknown10.CompareTo(e2.Unknown10);
                //if (i != 0)
                //    return i;
                //return e1.Type.CompareTo(e2.Type);
            }));

            Directory.CreateDirectory(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", entry.BigFile.Name));
            TextWriter tw = new StreamWriter(Path.Combine(TRGameInfo.Game.WorkFolder, "extract", entry.BigFile.Name, Path.ChangeExtension(entry.Extra.FileNameOnlyForced, ".log")));
            tw.WriteLine(string.Format("{0,-8} {1,-2} {2,-2} {3,-4} {4,-8} {5,-8} {6,-8} | {7,-8} {8,-8} {9,-8} {10,-8} | {11,-15} | {12,-2} {13,-2} {14,-8} {15}",
                "DataSize", "ty", "05", "06", "Flags", "id", "unk10", "u1", "Ad", "Le", "u4", "type", "pr", "bf", "offset", "bigfile"));
            for (int i = 0; i < sectionCount; i++)
            {
                DRMHeader hdr = header[i];
                tw.WriteLine(string.Format("{0:X8} {1:X2} {2:X2} {3:X4} {4:X8} {5:X8} {6:X8} | {7:X8} {8:X8} {9:X8} {10:X8} | {11,-15} | {12,2:X} {13,2:X} {14:X8} {15}",
                    hdr.DataSize,
                    (byte)(hdr.Type),
                    hdr.Unknown05,
                    hdr.Unknown06,
                    hdr.Flags,
                    hdr.Id,
                    hdr.Unknown10,
                    hdr.U1,
                    hdr.Address,
                    hdr.Length,
                    hdr.U4,
                    hdr.Type,
                    hdr.BigFilePriority,
                    hdr.BigFileIdx,
                    hdr.Offset,
                    hdr.BigFile.Name
                    ));

                FileStream fs = TRGameInfo.FilePool.Open(hdr.BigFile.Name, hdr.BigFileIdx);
                fs.Position = hdr.Offset;
                try
                {
                    //Debug.WriteLine(string.Format("ExtractCDRM {0} {1} {2} {3:X8}", entry.BigFile.Name, entry.Extra.FileNameForced, i, hdr.Offset));
                    ExtractCDRM(hdr, fs);
                    //Debug.WriteLine(string.Format("ExtractCDRM end"));
                }
                finally
                {
                    TRGameInfo.FilePool.Close(hdr.BigFile.Name, hdr.BigFileIdx);
                }
            }
            tw.Close();

        }

        struct CDRMHeader
        {
            public uint ucmpSize;
            public byte type;   // 1 uncompressed, 2 zlib, 3??
            public UInt32 cmpSize;
        }

        internal void ExtractCDRM(DRMHeader drmHdr, Stream inStream)
        {
            long contentLength = drmHdr.Length;
            long inStreamPos = inStream.Position;
            uint magic = inStream.ReadUInt32();     // CDRM
            uint version = inStream.ReadUInt32();   // 0 ( 2??)
            uint count;                             // entry count

            count = inStream.ReadUInt32();
            uint unknown0C = inStream.ReadUInt32();
            if (version != 0)
                throw new Exception("cdrm version not 0");

            List<CDRMHeader> items = new List<CDRMHeader>((int)count);

            for (int i = 0; i < count; i++)
            {
                CDRMHeader hdr;
                uint v01 = inStream.ReadUInt32();
                hdr.ucmpSize = v01 >> 8;
                hdr.type = (byte)(v01 & 0xFF);
                hdr.cmpSize = inStream.ReadUInt32();
                items.Add(hdr);
            }
            for (int i = 0; i < count; i++)
            {
                CDRMHeader hdr = items[i];
                //Debug.WriteLine(string.Format("{0}, {1} {2}", entry.Extra.FileName, i, hdr.type));
                if (count > 2)
                    Noop.DoIt();

                inStream.Position = inStream.Position.ExtendToBoundary(0x10);
                if (hdr.type == 2)
                {
                    InflaterInputStream unzipStream = new InflaterInputStream(inStream);
                    try
                    {
                        //string folder = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "drm", entry.BigFile.Name, entry.Extra.FileNameOnlyForced);
                        //Directory.CreateDirectory(folder);
                        //Stream outputStream = new FileStream(Path.Combine(folder, string.Format("{0:X4},{1:X4}c", idx, i)), FileMode.Create);
                        Stream outputStream = new MemoryStream();
                        try
                        {
                            long start = inStream.Position;
                            byte[] buf = new byte[hdr.ucmpSize];
                            unzipStream.Read(buf, 0, (int)hdr.ucmpSize);

                            string dataMagic = Encoding.ASCII.GetString(buf, 0, 4);
                            if (dataMagic == "PCD9")
                            {
                                MemoryStream ms = new MemoryStream(buf);
                                try
                                {
                                    PCD9File pcd9 = new PCD9File(ms);

                                    Debug.WriteLine(string.Format("PCD9File: {0}x{1} ({2}bpp) {3} size: {4}", pcd9.header.Width, pcd9.header.Height, pcd9.header.BPP, pcd9.header.MipMapCount, pcd9.header.DataSize));
                                    if (pcd9.header.Width == 512 && pcd9.header.Height == 128)
                                    {
                                        Bitmap bmp = pcd9.GetBitmap(ms);
                                        string folder = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "font", entry.BigFile.Name);
                                        Directory.CreateDirectory(folder);
                                        bmp.Save(Path.Combine(folder, string.Format("{0}.{1:X4}-{2:X4}-{3:X4}.bmp", entry.Extra.FileNameOnlyForced, drmHdr.Id, drmHdr.OrigIdx, i)), ImageFormat.Bmp);
                                        bmp.Save(Path.Combine(folder, string.Format("{0}.{1:X4}-{2:X4}-{3:X4}.png", entry.Extra.FileNameOnlyForced, drmHdr.Id, drmHdr.OrigIdx, i)), ImageFormat.Png);
                                    }

                                }
                                finally
                                {
                                    ms.Close();
                                }
                            }
                            outputStream.Write(buf, 0, buf.Length);
                            if (inStream.Position > start + hdr.cmpSize.ExtendToBoundary(0x10))
                            {
                                Debug.WriteLine("Roll back stream: bytes: " + (inStream.Position - (start + hdr.cmpSize.ExtendToBoundary(0x10))));
                            }
                            inStream.Position = start + hdr.cmpSize.ExtendToBoundary(0x10);
                        }
                        finally
                        {
                            outputStream.Close();
                        }
                    }
                    finally
                    {
                        //unzipStream.Close();
                    }
                }
                else
                {
                    if (hdr.cmpSize != hdr.ucmpSize)
                        throw new Exception("CDRM header error.");
                    Noop.DoIt();
                    try
                    {
                        //string folder = Path.Combine(TRGameInfo.Game.WorkFolder, "extract", "drm", entry.BigFile.Name, entry.Extra.FileNameOnlyForced);
                        //Directory.CreateDirectory(folder);
                        //Stream outputStream = new FileStream(Path.Combine(folder, string.Format("{0:X4},{1:X4}u", idx, i)), FileMode.Create);
                        Stream outputStream = new MemoryStream();
                        try
                        {
                            byte[] buf = new byte[hdr.ucmpSize];
                            inStream.Read(buf, 0, (int)hdr.ucmpSize);
                            outputStream.Write(buf, 0, buf.Length);
                        }
                        finally
                        {
                            outputStream.Close();
                        }
                    }
                    finally
                    {
                    }
                }
            }
        }
    }
}
