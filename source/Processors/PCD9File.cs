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

    class PCD9
    {
        PCD9Format Format { get; set; }
        public ushort Width;
        public ushort Height;
        public UInt16 BPP;
        public List<PCD9Mipmap> Mipmaps = new List<PCD9Mipmap>();

        public uint Unknown0C;
        public ushort Unknown16;
        //        public bool unknownFlag;

        public void ReadFromStream(Stream inStream)
        {
            uint magic = (inStream.ReadUInt32());
            Format = (PCD9Format)(inStream.ReadUInt32());
            UInt32 dataSize = inStream.ReadUInt32();
            Unknown0C = inStream.ReadUInt32();
            Width = inStream.ReadUInt16();
            Height = inStream.ReadUInt16();
            BPP = inStream.ReadUInt16();
            Unknown16 = inStream.ReadUInt8();
            byte mipMapCount = (byte)(inStream.ReadUInt8() + 1);
            UInt16 flags = inStream.ReadUInt16();
            UInt16 Unknown1A = inStream.ReadUInt16();

            Debug.WriteLine(string.Format("PCD9: {0}x{1} ({2}bpp) {3} size: {4}", Width, Height, BPP, mipMapCount, dataSize));
            if (Width != 512 || Height != 128)
                return;

            if ((flags & (0x8000 | 0x4000)) != 0)
            {
                throw new NotImplementedException();
            }
            this.Mipmaps.Clear();
            byte[] data = new byte[dataSize];

            ushort mipWidth = this.Width;
            ushort mipHeight = this.Height;

            inStream.Read(data, 0, (int)dataSize);

            for (int i = 0; i < mipMapCount; i++)
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
                switch (this.Format)
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
                            int blockSize = this.Format == PCD9Format.DXT1 ? 8 : 16;
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

                this.Mipmaps.Add(new PCD9Mipmap()
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
            MakeBitmapFromTrueColor(this.Width, this.Height, data, true);
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
            bitmap.Save("c:\\tmp\\x.bmp", ImageFormat.Bmp);
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
