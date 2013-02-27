using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace TRTR
{
    static class Noop
    {
        internal static void DoIt()
        {
            return;
        }
    }
    /*

        static class _ArrayUtils
        {
            internal static T[] ByteArrayToStruct<T>(byte[] buf, Int32 count)
            {
                Int32 structSize = Marshal.SizeOf(typeof(T));
                byte[] workBuf = new byte[structSize];
                GCHandle handle;
                T[] ret = new T[count];
                for (Int32 i = 0; i < count; i++)
                {
                    Array.Copy(buf, i * structSize, workBuf, 0, structSize);
                    handle = GCHandle.Alloc(workBuf, GCHandleType.Pinned);
                    ret[i] = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                    handle.Free();
                }
                return ret;
            }

            internal static byte[] StructToByteArray<T>(T str)
            {
                Int32 structSize = Marshal.SizeOf(typeof(T));
                byte[] ret = new byte[structSize];
                GCHandle handle = GCHandle.Alloc(ret, GCHandleType.Pinned);
                try
                {
                    Marshal.StructureToPtr(str, handle.AddrOfPinnedObject(), true);
                }
                finally
                {
                    handle.Free();
                }
                return ret;
            }

            internal static Int32 ReadIntFromArray(byte[] buf, Int32 offset)
            {
                Int32 ret = 0;
                byte[] workBuf = new byte[sizeof(Int32)];
                Array.Copy(buf, offset, workBuf, 0, sizeof(Int32));
                GCHandle handle = GCHandle.Alloc(workBuf, GCHandleType.Pinned);
                ret = Marshal.ReadInt32(handle.AddrOfPinnedObject());
                handle.Free();
                return ret;
            }

            internal static UInt32 ReadUIntFromArray(byte[] buf, Int32 offset)
            {
                return (uint)ReadIntFromArray(buf, offset);
            }
        }

    */
    internal enum FilePathOption
    {
        IncludeTrailingBackSlash = 0,
        NotIncludeTrailingBackSlash = 1
    }

    internal enum FileNameOptions
    {
        LastWithDot = 0,
        LastWithoutDot = 1,
        AllWithDot = 2,
        AllWithoutDot = 3
    }

    class FileNameUtils
    { 
        internal static string IncludeTrailingBackSlash(string path)
        {
            if (path.Length == 0 || path[path.Length - 1] != '\\')
                return path += '\\';
            else
                return path;
        }
        
        internal static string FilePath(string fileName)
        {
            return FilePath(fileName, FilePathOption.IncludeTrailingBackSlash);
        }

        internal static string FilePath(string fileName, FilePathOption options)
        {
            Int32 index = fileName.LastIndexOf('\\');
            if (index < 0)
                return string.Empty;
            else
                if (options == FilePathOption.IncludeTrailingBackSlash)
                    return fileName.Substring(0, index + 1);
                else
                    return fileName.Substring(0, index);
        }

        internal static string FileName(string fileName)
        {
            Int32 index = fileName.LastIndexOf('\\');
            return (index < 0)? fileName: fileName.Substring(index + 1, fileName.Length - index - 1);
        }
        
        internal static string FileExt(string fileName, FileNameOptions options)
        {
//            onlyFileName = FileName(fileName);
            Int32 index;
            if (options == FileNameOptions.AllWithoutDot || options == FileNameOptions.AllWithDot)
                index = fileName.IndexOf('.');
            else
                index = fileName.LastIndexOf('.');
            if (options != FileNameOptions.AllWithDot && options != FileNameOptions.LastWithDot)
                index++;
            if (index >= 0)
                return fileName.Substring(index, fileName.Length - index);
            else
                return string.Empty;
        }
    }
}
