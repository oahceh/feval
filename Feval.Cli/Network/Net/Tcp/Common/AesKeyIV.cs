using Crypto;
using Helper;
using System;
using System.Net.Sockets;
using Net.Common;

using Random = System.Random;

namespace Net.Tcp.Common
{
    internal class AesKeyIV
    {
        internal const int KeyIVLen = 16;

        internal static byte[] GenKeyIV()
        {
            var bytes = new byte[KeyIVLen];
            var rand = new Random(DateTime.Now.Millisecond);
            for (var i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte) rand.Next(0, 10);
            }

            return bytes;
        }

        internal static bool Check(byte[] bytes)
        {
            return bytes != null && bytes.Length == KeyIVLen;
        }

        internal static byte[] ParseKey(byte[] bytes)
        {
            var key = new byte[KeyIVLen];
            Buffer.BlockCopy(bytes, 0, key, 0, key.Length);
            return key;
        }

        internal static byte[] ParseIV(byte[] bytes)
        {
            var iv = new byte[KeyIVLen];
            Buffer.BlockCopy(bytes, KeyIVLen, iv, 0, iv.Length);
            return iv;
        }

        internal static void SendAesKeyIVRsa(Socket sock, string rsaPub, byte[] kiv)
        {
            if (sock == null)
            {
                Console.WriteLine("Null socket");
                return;
            }

            if (!sock.Connected)
            {
                Console.WriteLine("Socket is not connected");
                return;
            }

            if (!Rsa.Encrypt(rsaPub, kiv, out kiv))
            {
                sock.Close();
                throw new Exception("Rsa Encrypt error");
            }

            var len = NetHelper.ToBytes(kiv.Length);
            sock.Send(len);
            sock.Send(kiv);
        }

        internal static void SendAesKeyIVAes(Socket sock, byte[] aesKIV, byte[] kiv)
        {
            if (sock == null || !sock.Connected)
            {
                return;
            }

            var encrypt = new AesEncryptor(aesKIV, aesKIV);
            var ms = MemoryStreamPool.Get();
            {
                var dataLen = encrypt.Encrypt(kiv, 0, kiv.Length, ms);
                var len = NetHelper.ToBytes((int) dataLen);
                sock.Send(len);
                sock.Send(ms.GetBuffer(), 0, (int) ms.Length, SocketFlags.None);
            }
            MemoryStreamPool.Release(ms);
        }
    }
}