using Proquint;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ProquintUtil {

    public class PQ {
        public static string bytes2quints(byte[] data)
        {
            if (data.Length % 4 != 0)
            {
                throw new Exception("data length must be divisible by 4");
            }
            var ret = new StringBuilder("");
            for (var i = 0; i < data.Length; i += 4)
            {
                if (i > 0) ret.Append("-");
                ret.Append(new Quint32(                     
                        (( ((uint)data[i    ]) << 24) & 0xff000000) 
                      | (( ((uint)data[i + 1]) << 16) & 0x00ff0000) 
                      | (( ((uint)data[i + 2]) <<  8) & 0x0000ff00) 
                      | (   (uint)data[i + 3]       ) & 0x000000ff).ToString());
            }
            return ret.ToString();
        }


        public static string bytes2hex(byte[] bytes){
            var ret = new StringBuilder("");
            foreach (var b in bytes){
                ret.Append(String.Format("{0:x02}",b));
            }
            return ret.ToString();
        }


        public static byte[] hex2bytes(string hex){
            return parseHex(hex);
        }


        public static byte[] parseHex(string hex){
            if (hex.Length%2!=0)
            {
                throw new Exception("data length must be divisible by 4");
            }
            var ret = new byte[hex.Length/2];
            var byteIdx=0;
            for (var i=0; i<hex.Length; i+=2){
                ret[byteIdx]=Byte.Parse(hex.Substring(i,2),NumberStyles.HexNumber);
                byteIdx++;
            }
            return ret;
        }

        public static byte[] quints2bytes(string quints)    
        {
            var parts = quints.Split('-');
            var pattern = "[a-z]{5}-[a-z]{5}(-[a-z]{5}-[a-z]{5})*";
            if (!Regex.IsMatch(quints, pattern))
            {
                throw new Exception("bad input fformat - expected CVCVC-CVCVC(-CVCVC-CVCVC)*");
            }
            if (parts.Length % 2 != 0)
            {
                throw new Exception("number of dashed parts must be divisible by 2");
            }

            var ret = new byte[parts.Length/2*4];
            var byteIdx=0;
            for (var i=0; i< parts.Length; i+=2 ){
                var value = new Quint32(String.Format("{0}-{1}", parts[i], parts[i+1]));
                ret[byteIdx]   = (byte)(value >>24 & 0x000000FF);
                ret[byteIdx+1] = (byte)(value >>16 & 0x000000FF);
                ret[byteIdx+2] = (byte)(value >> 8 & 0x000000FF);
                ret[byteIdx+3] = (byte)(value      & 0x000000FF);
                byteIdx+=4;
            }
            return ret;
        }
    }


}