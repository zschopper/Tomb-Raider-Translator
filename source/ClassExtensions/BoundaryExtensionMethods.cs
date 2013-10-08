using System;
using System.Collections.Generic;
using System.Text;

namespace ExtensionMethods
{
    public static class BoundaryExtensions
    {
        public static Int32 ExtendToBoundary(this Int32 me, Int32 boundary)
        {
            return me + (boundary - 1) - (me - 1) % boundary;
        }

        public static UInt32 ExtendToBoundary(this UInt32 me, UInt32 boundary)
        {
            return me + (boundary - 1) - (me - 1) % boundary;
        }

        public static Int64 ExtendToBoundary(this Int64 me, Int64 boundary)
        {
            return me + (boundary - 1) - (me - 1) % boundary;
        }

        public static UInt64 ExtendToBoundary(this UInt64 me, UInt64 boundary)
        {
            return me + (boundary - 1) - (me - 1) % boundary;
        }

        public static Int32 ShrinkToBoundary(this Int32 me, Int32 boundary)
        {
            return me - (me % boundary);
        }

        public static UInt32 ShrinkToBoundary(this UInt32 me, UInt32 boundary)
        {
            return me - (me % boundary);
        }

        public static Int64 ShrinkToBoundary(this Int64 me, Int64 boundary)
        {
            return me - (me % boundary);
        }

        public static UInt64 ShrinkToBoundary(this UInt64 me, UInt64 boundary)
        {
            return me - (me % boundary);
        }

        public static Int32 DiffToNextBoundary(this Int32 me, Int32 boundary)
        {
            return (Int32)((boundary - 1) - (me - 1) % boundary);
        }

        public static UInt32 DiffToNextBoundary(this UInt32 me, UInt32 boundary)
        {
            return (UInt32)((boundary - 1) - (me - 1) % boundary);
        }

        public static Int64 DiffToNextBoundary(this Int64 me, Int64 boundary)
        {
            return (Int64)((boundary - 1) - (me - 1) % boundary);
        }

        public static UInt64 DiffToNextBoundary(this UInt64 me, UInt64 boundary)
        {
            return (UInt64)((boundary - 1) - (me - 1) % boundary);
        }

    }
}

/*
 
        internal static Int32 Up(Int32 value, Int32 boundary)
        {
            return (boundary - 1) - (value - 1) % boundary;
        }

        internal static Int32 Down(Int32 value, Int32 boundary)
        {
            return value % boundary;
        }

        internal static Int32 Extend(Int32 value, Int32 boundary)
        {
            return value + (boundary - 1) - (value - 1) % boundary;
        }

        internal static Int32 Shrink(Int32 value, Int32 boundary)
        {
            return value - (value % boundary);
        }
 * */