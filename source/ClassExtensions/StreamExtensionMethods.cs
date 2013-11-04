using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ExtensionMethods
{
    public static class StreamExtensionMethods
    {
        public static void Move(this Stream me, Int64 delta)
        {
            if (delta != 0)
                me.Seek(delta, SeekOrigin.Current);
        }

        //public Int32 PeekByte(this Stream me)
        //{
        //    return this.;
        //}

        public static byte PeekByte(this Stream me)
        {
            byte ret = (byte)me.ReadByte();
            me.Position -= 1;
            return ret;
        }

        public static Int16 PeekInt16(this Stream me)
        {
            Int16 ret = me.ReadInt16();
            me.Position -= 2;
            return ret;
        }

        public static UInt16 PeekUInt16(this Stream me)
        {
            UInt16 ret = me.ReadUInt16();
            me.Position -= 2;
            return ret;
        }

        public static Int32 PeekInt32(this Stream me)
        {
            Int32 ret = me.ReadInt32();
            me.Position -= 4;
            return ret;
        }

        public static UInt32 PeekUInt32(this Stream me)
        {
            UInt32 ret = me.ReadUInt32();
            me.Position -= 4;
            return ret;
        }

        public static Int64 PeekInt64(this Stream me)
        {
            Int64 ret = me.ReadInt64();
            me.Position -= 8;
            return ret;
        }

        public static UInt64 PeekUInt64(this Stream me)
        {
            UInt64 ret = me.ReadUInt64();
            me.Position -= 8;
            return ret;
        }

        public static sbyte ReadInt8(this Stream me)
        {
            byte[] buf = new byte[1];
            me.Read(buf, 0, buf.Length);
            return (sbyte)(buf[0]);
        }

        public static byte ReadUInt8(this Stream me)
        {
            byte[] buf = new byte[1];
            me.Read(buf, 0, buf.Length);
            return (byte)(buf[0]);
        }

        public static Int16 ReadInt16(this Stream me)
        {
            byte[] buf = new byte[2];
            me.Read(buf, 0, buf.Length);
            return (Int16)(buf[0] | (buf[1] << 8));
        }

        public static UInt16 ReadUInt16(this Stream me)
        {
            byte[] buf = new byte[2];
            me.Read(buf, 0, buf.Length);
            return (UInt16)(buf[0] | (buf[1] << 8));
        }

        public static Int32 ReadInt32(this Stream me)
        {
            byte[] buf = new byte[4];
            me.Read(buf, 0, buf.Length);
            return (Int32)(((buf[0] | (buf[1] << 8)) | (buf[2] << 0x10)) | (buf[3] << 0x18));
        }

        public static UInt32 ReadUInt32(this Stream me)
        {
            byte[] buf = new byte[4];
            me.Read(buf, 0, buf.Length);
            return (UInt32)(((buf[0] | (buf[1] << 8)) | (buf[2] << 0x10)) | (buf[3] << 0x18));
        }

        public static Int64 ReadInt64(this Stream me)
        {
            byte[] buf = new byte[8];
            me.Read(buf, 0, buf.Length);
            Int32 num1 = (Int32)(((buf[0] | (buf[1] << 8)) | (buf[2] << 0x10)) | (buf[3] << 0x18));
            Int32 num2 = (Int32)(((buf[4] | (buf[5] << 8)) | (buf[6] << 0x10)) | (buf[7] << 0x18));
            return ((num2 << 0x20) | num1);
        }

        public static UInt64 ReadUInt64(this Stream me)
        {
            byte[] buf = new byte[8];
            me.Read(buf, 0, buf.Length);
            UInt32 num1 = (UInt32)(((buf[0] | (buf[1] << 8)) | (buf[2] << 0x10)) | (buf[3] << 0x18));
            UInt32 num2 = (UInt32)(((buf[4] | (buf[5] << 8)) | (buf[6] << 0x10)) | (buf[7] << 0x18));
            return ((num2 << 0x20) | num1);
        }

        public static void WriteInt64(this Stream me, Int64 value)
        {
            byte[] buf = new byte[8];
            buf[0] = (byte)value;
            buf[1] = (byte)(value >> 8);
            buf[2] = (byte)(value >> 0x10);
            buf[3] = (byte)(value >> 0x18);
            buf[4] = (byte)(value >> 0x20);
            buf[5] = (byte)(value >> 40);
            buf[6] = (byte)(value >> 0x30);
            buf[7] = (byte)(value >> 0x38);
            me.Write(buf, 0, buf.Length);
        }

        public static void WriteUInt64(this Stream me, UInt64 value)
        {
            byte[] buf = new byte[8];
            buf[0] = (byte)value;
            buf[1] = (byte)(value >> 8);
            buf[2] = (byte)(value >> 0x10);
            buf[3] = (byte)(value >> 0x18);
            buf[4] = (byte)(value >> 0x20);
            buf[5] = (byte)(value >> 40);
            buf[6] = (byte)(value >> 0x30);
            buf[7] = (byte)(value >> 0x38);
            me.Write(buf, 0, buf.Length);
        }

        public static void WriteInt32(this Stream me, Int32 value)
        {
            byte[] buf = new byte[4];
            buf[0] = (byte)value;
            buf[1] = (byte)(value >> 8);
            buf[2] = (byte)(value >> 0x10);
            buf[3] = (byte)(value >> 0x18);
            me.Write(buf, 0, buf.Length);
        }

        public static void WriteUInt32(this Stream me, UInt32 value)
        {
            byte[] buf = new byte[4];
            buf[0] = (byte)value;
            buf[1] = (byte)(value >> 8);
            buf[2] = (byte)(value >> 0x10);
            buf[3] = (byte)(value >> 0x18);
            me.Write(buf, 0, buf.Length);
        }

        public static void WriteInt16(this Stream me, Int32 value)
        {
            byte[] buf = new byte[2];
            buf[0] = (byte)value;
            buf[1] = (byte)(value >> 8);
            me.Write(buf, 0, buf.Length);
        }

        public static void WriteUInt16(this Stream me, UInt32 value)
        {
            byte[] buf = new byte[2];
            buf[0] = (byte)value;
            buf[1] = (byte)(value >> 8);
            me.Write(buf, 0, buf.Length);
        }

        public static void WriteFromStream(this Stream me, Stream sourceStream, long count, int bufferSize = 1048576)
        {
            //if (count > sourceStream.Length - sourceStream.Position)
            //    throw new IOException("Source stream is too small");

            long copiedBytes = 0;
            byte[] buf = new byte[Math.Min(bufferSize, count)];

            while (count > copiedBytes)
            {
                int bytesRead = sourceStream.Read(buf, 0, (int)Math.Min(count - copiedBytes, bufferSize));
                me.Write(buf, 0, bytesRead);
                copiedBytes += bytesRead;
            };
        }
    }

}
