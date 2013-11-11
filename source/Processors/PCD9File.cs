using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ExtensionMethods;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TRTR
{
    public enum PCD9Format : uint
    {
        A8R8G8B8 = 21,

        DXT1 = 0x31545844,
        DXT3 = 0x33545844,
        DXT5 = 0x35545844,
    }

    public class PCD9Mipmap
    {
        public ushort Width;
        public ushort Height;
        public byte[] Data;
    }

    class PCD9File
    {
        public class Header
        {
            public Stream innerStream;
            public PCD9Format Format { get; set; }
            public ushort Width;
            public ushort Height;
            public UInt16 BPP;
            public List<PCD9Mipmap> Mipmaps = new List<PCD9Mipmap>();
            public UInt32 DataSize;

            public uint Unknown0C;
            public ushort Unknown16;
            public byte MipMapCount;
            public UInt16 Flags;
            public UInt16 Unknown1A;
        };

        public Header header = new Header();

        internal PCD9File(Stream inStream)
        {
            header.innerStream = inStream;
            uint magic = (inStream.ReadUInt32());
            header.Format = (PCD9Format)(inStream.ReadUInt32());
            header.DataSize = inStream.ReadUInt32();
            header.Unknown0C = inStream.ReadUInt32();
            header.Width = inStream.ReadUInt16();
            header.Height = inStream.ReadUInt16();
            header.BPP = inStream.ReadUInt16();
            header.Unknown16 = inStream.ReadUInt8();
            header.MipMapCount = (byte)(inStream.ReadUInt8() + 1);
            header.Flags = inStream.ReadUInt16();
            header.Unknown1A = inStream.ReadUInt16();
        }

        public Bitmap GetBitmap(Stream inStream = null)
        {
            if (inStream == null)
                inStream = header.innerStream;

            if ((header.Flags & (0x8000 | 0x4000)) != 0)
            {
                throw new NotImplementedException();
            }
            this.header.Mipmaps.Clear();
            byte[] data = new byte[header.DataSize];

            ushort mipWidth = this.header.Width;
            ushort mipHeight = this.header.Height;

            inStream.Read(data, 0, (int)(header.DataSize));

            for (int i = 0; i < header.MipMapCount; i++)
            {
                if (mipWidth == 0)
                {
                    mipWidth = 1;
                }

                if (mipHeight == 0)
                {
                    mipHeight = 1;
                }

                int size;
                switch (this.header.Format)
                {
                    case PCD9Format.A8R8G8B8:
                        {
                            size = mipWidth * mipHeight * 4;
                            break;
                        }

                    case PCD9Format.DXT1:
                    case PCD9Format.DXT3:
                    case PCD9Format.DXT5:
                        {
                            int blockCount = ((mipWidth + 3) / 4) * ((mipHeight + 3) / 4);
                            int blockSize = this.header.Format == PCD9Format.DXT1 ? 8 : 16;
                            size = blockCount * blockSize;
                            break;
                        }

                    default:
                        {
                            throw new NotSupportedException();
                        }
                }

                byte[] buffer = new byte[size];
                //if (data.Read(buffer, 0, buffer.Length) != buffer.Length)
                //{
                //    throw new EndOfStreamException();
                //}

                this.header.Mipmaps.Add(new PCD9Mipmap()
                {
                    Width = mipWidth,
                    Height = mipHeight,
                    Data = buffer,
                });

                mipWidth >>= 1;
                mipHeight >>= 1;
            }

            //if (data.Position != data.Length)
            //{
            //    throw new InvalidOperationException();
            //}
            return MakeBitmapFromTrueColor(this.header.Width, this.header.Height, data, true);
        }

        private static Bitmap MakeBitmapFromTrueColor(uint width, uint height, byte[] input, bool keepAlpha)
        {
            byte[] output = new byte[width * height * 4];
            Bitmap bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format32bppArgb);

            for (uint i = 0; i < width * height * 4; i += 4)
            {
                output[i + 0] = input[i + 2];
                output[i + 1] = input[i + 1];
                output[i + 2] = input[i + 0];
                output[i + 3] = keepAlpha == false ? (byte)0xFF : input[i + 3];
            }

            Rectangle area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(output, 0, data.Scan0, output.Length);
            bitmap.UnlockBits(data);
            return bitmap;
        }

        private static byte[] MakeTrueColorFromBitmap(Bitmap bitmap)
        {
            byte[] output = new byte[bitmap.Width * bitmap.Height * 4];

            byte[] input = new byte[bitmap.Width * bitmap.Height * 4];
            Rectangle area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(data.Scan0, input, 0, input.Length);
            bitmap.UnlockBits(data);

            for (uint i = 0; i < bitmap.Width * bitmap.Height * 4; i += 4)
            {
                output[i + 0] = input[i + 2];
                output[i + 1] = input[i + 1];
                output[i + 2] = input[i + 0];
                output[i + 3] = input[i + 3];
            }

            return output;
        }

        private static Bitmap MakeBitmapFromGrayscale(uint width, uint height, byte[] input)
        {
            byte[] output = new byte[width * height * 4];
            Bitmap bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format32bppArgb);

            uint o = 0;
            for (uint i = 0; i < width * height; i++)
            {
                byte v = input[i];
                output[o + 0] = v;
                output[o + 1] = v;
                output[o + 2] = v;
                output[o + 3] = 0xFF;
                o += 4;
            }

            Rectangle area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(output, 0, data.Scan0, output.Length);
            bitmap.UnlockBits(data);
            bitmap.Save("c:\\tmp\\x.bmp", ImageFormat.Bmp);
            return bitmap;
        }

        private static Bitmap MakeBitmapFromAlphaGrayscale(uint width, uint height, byte[] input, bool keepAlpha)
        {
            byte[] output = new byte[width * height * 4];
            Bitmap bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format32bppArgb);

            uint o = 0;
            for (uint i = 0; i < width * height * 2; i += 2)
            {
                byte c = input[i + 0];
                byte a = input[i + 1];

                output[o + 0] = c;
                output[o + 1] = c;
                output[o + 2] = c;
                output[o + 3] = keepAlpha == false ? (byte)0xFF : a;

                o += 4;
            }

            Rectangle area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(output, 0, data.Scan0, output.Length);
            bitmap.UnlockBits(data);
            bitmap.Save("c:\\tmp\\x.bmp", ImageFormat.Bmp);
            return bitmap;
        }

    }
}
