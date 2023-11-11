using Crypto;
using Helper;
using Net.Udp.Server;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Net.Udp.Common
{
    class AesKeyIV
    {
        internal const int KeyIVLen = 16;

        internal static byte[] GenKeyIV()
        {
            var bytes = new byte[KeyIVLen];
            var rand = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte) rand.Next(0, 10);
            }

            return bytes;
        }

        internal static bool Check(byte[] bytes)
        {
            return bytes != null && bytes.Length == KeyIVLen;
        }

        internal static byte[] PraseKey(byte[] bytes)
        {
            var key = new byte[KeyIVLen];
            Buffer.BlockCopy(bytes, 0, key, 0, key.Length);
            return key;
        }

        internal static byte[] PraseIV(byte[] bytes)
        {
            var iv = new byte[KeyIVLen];
            Buffer.BlockCopy(bytes, KeyIVLen, iv, 0, iv.Length);
            return iv;
        }
    }
}