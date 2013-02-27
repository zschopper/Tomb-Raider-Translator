using System;
using System.Collections.Generic;
using System.Text;

namespace TRTR
{
    class HexEncode
    {
        static string hexChars = "0123456789ABCDEF";

        internal static string Encode(byte[] buf)
        {
            char[] ret = new char[buf.Length * 2];
            for(Int32 i = 0; i < buf.Length; i++)
            {
                ret[i << 1] = hexChars[buf[i] >> 4];
                ret[(i << 1) + 1] = hexChars[buf[i] & 0xF];
            }
            return new string(ret);
        }

        internal static byte[] Decode__2(string encoded, Int32 length)
        {
            //return UInt32.Parse(HexEncode.Encode(new byte[] { 10, 20, 30 }), System.Globalization.NumberStyles.HexNumber);
            return null;
        }

        internal static byte[] Decode(string encoded, Int32 length)
        {
            byte[] decoded = Decode(encoded);
            byte[] ret = new byte[length];
            Array.Copy(decoded, 0, ret, length - decoded.Length, decoded.Length);
            return ret;
        }

        internal static byte[] Decode(string encoded)
        {
            //"F0AF0"
            //"15, 10, 240"
            // ret[0] = encoded[0 - odd] (enc.len - (enc.len & 1)
            // ret[1] = encoded[1] + encoded[2]
            // ret[2] = encoded[3] + encoded[4]

            byte odd = (byte)(encoded.Length & 1); 
            byte[] ret = new byte[(encoded.Length + 1) / 2];

            for (Int32 i = 0; i < encoded.Length + odd; i++)
            {
                byte val = 0;
                char c;
                if(i == 0 && odd == 1)
                    c = '0';
                else
                    c = encoded[i - odd];

                if (c >= '0' && c <= '9')
                    val = (byte)(c - '0');
                else
                    if (c >= 'A' && c <= 'F')
                        val = (byte)(c - 'A' + 10);
                    else
                        if (c >= 'a' && c <= 'f')
                            val = (byte)(c - 'a' + 10);

                if ((i & 1) == 0) // not odd
                    ret[i >> 1] += (byte)(val << 4);
                else
                    ret[i >> 1] += val;
            }
            return ret;
        }

        internal class JSMHexConverter
        {
            /// <summary>
            /// Helper array to speedup conversion
            /// </summary>
            static string[] BATHS = { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F", "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F", "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF", "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF", "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF", "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF", "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF" };

            /// <summary>
            /// Function converts byte array to it's hexadecimal implementation
            /// </summary>
            /// <param name="ArrayToConvert">Array to be converted</param>
            /// <param name="Delimiter">Delimiter to be inserted between bytes</param>
            /// <returns>String to represent given array</returns>
            static string ByteArrayToHexString(byte[] ArrayToConvert, string Delimiter)
            {
                Int32 LengthRequired = (ArrayToConvert.Length + Delimiter.Length) * 2;
                StringBuilder tempstr = new StringBuilder(LengthRequired, LengthRequired);
                foreach (byte CurrentElem in ArrayToConvert)
                {
                    tempstr.Append(BATHS[CurrentElem]);
                    tempstr.Append(Delimiter);
                }

                return tempstr.ToString();
            }

        }
    }
    static class HexStringConverter
    {
        internal static byte[] ToByteArray(String HexString)
        {
            Int32 NumberChars = HexString.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (Int32 i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }
            return bytes;
        }
    }
}