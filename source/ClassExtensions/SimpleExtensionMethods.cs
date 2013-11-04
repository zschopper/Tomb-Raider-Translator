using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtensionMethods
{
    public static class SimpleExtensionMethods
    {
        static char PrivateLow = System.Text.Encoding.UTF8.GetString(new byte[] { 0xEE, 0x00, 0x00 })[0];
        static char PrivateHigh = System.Text.Encoding.UTF8.GetString(new byte[] { 0xEF, 0xFF, 0xFF })[0];

        public static bool IsInPrivateRange(this char me)
        {
            return me >= PrivateLow && me <= PrivateHigh;
        }

    }
}
