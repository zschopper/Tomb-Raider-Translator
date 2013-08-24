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