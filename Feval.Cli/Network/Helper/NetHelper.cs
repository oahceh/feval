using Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Helper
{
    public sealed class NetHelper
    {
        public const int PackHeadSize = 4;

        public const byte CCC_Compress = 0x80;
        public const byte CCC_Crypto = 0x40;
        public const byte CCC_Crc = 0x20;
        static bool NetBigEndian = false;

        public static int NextPow2(int a)
        {
            var val = 1;
            while (val < a)
            {
                val <<= 1;
            }

            return val;
        }

        public static bool ToBoolean(byte[] bytes, int offset)
        {
            var val = BitConverter.ToBoolean(bytes, offset);
            return val;
        }

        public static unsafe ushort ToUShort(byte[] buffer, int offset)
        {
            fixed (byte* ptr = buffer)
            {
                return BitConverter.IsLittleEndian == !NetBigEndian
                    ? *(ushort*) (ptr + offset)
                    : (*(ushort*) (ptr + offset)).ReverseBytes();
            }
        }

        public static short ToShort(byte[] bytes, int offset)
        {
            return (short) ToUShort(bytes, offset);
        }

        public static int ToInt32(byte[] bytes, int offset)
        {
            return (int) ToUInt32(bytes, offset);
        }

        public static unsafe uint ToUInt32(byte[] buffer, int offset)
        {
            fixed (byte* ptr = buffer)
            {
                return BitConverter.IsLittleEndian == !NetBigEndian
                    ? *(uint*) (ptr + offset)
                    : (*(uint*) (ptr + offset)).ReverseBytes();
            }
        }

        public static unsafe float ToFloat(byte[] buffer, int offset)
        {
            fixed (byte* ptr = buffer)
            {
                if (BitConverter.IsLittleEndian == !NetBigEndian)
                {
                    return *(float*) (ptr + offset);
                }
                else
                {
                    uint uvalue = (*(uint*) (ptr + offset)).ReverseBytes();
                    return *(float*) (&uvalue);
                }
            }
        }

        public static byte[] ToBytes(int i)
        {
            uint value = (uint) i;
            if (BitConverter.IsLittleEndian == NetBigEndian)
                value = value.ReverseBytes();

            var bytes = BitConverter.GetBytes(value);
            return bytes;
        }

        /// <summary>
        /// 将一个uint转成传输数据需求的大小端
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static unsafe uint GetTransferUInt(uint value)
        {
            return BitConverter.IsLittleEndian == !NetBigEndian ? value : value.ReverseBytes();
        }

        public static IPAddress ParseIpAddressV6(string address)
        {
            IPAddress addrOut = null;

            // 如果是IP，直接返回
            if (IPAddress.TryParse(address, out addrOut))
                return addrOut;

            // 如果是域名，查一下
            IPAddress[] addrList = Dns.GetHostAddresses(address);
            if (addrList.Length <= 0)
                return null; // 没找到就只能给空了

            // 默认使用第一ip
            addrOut = addrList[0];

#if false // 优先使用 v6
            for (int i = 0; i < addrList.Length; i++)
            {
                if (addrList[i].AddressFamily == AddressFamily.InterNetworkV6)
                    return addrList[i];
            }
#endif

            return addrOut;
        }

        static bool Reachable(IPAddress addr, int port, int timeout)
        {
            var ep = new IPEndPoint(addr, port);
            var suc = false;
            var wh = new System.Diagnostics.Stopwatch();
            wh.Start();
            using (var s = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    var ar = s.BeginConnect(ep, null, null);
                    suc = ar.AsyncWaitHandle.WaitOne(timeout, true);
                    if (suc)
                    {
                        s.EndConnect(ar);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    s.Close();
                }
            }

            wh.Stop();
            Console.WriteLine("Reachable : {0}:{1} {2} in {3}ms timeout {4}ms", addr, port, suc,
                wh.ElapsedMilliseconds, timeout);
            return suc;
        }

        public static IPAddress ParseIpAddressV6(string address, int port)
        {
            IPAddress addrOut;
            if (IPAddress.TryParse(address, out addrOut))
            {
                return addrOut;
            }

            IPAddress[] addrList = Dns.GetHostAddresses(address);

            if (addrList.Length == 1)
            {
                addrOut = addrList[0];
            }

            if (addrOut == null)
            {
                for (int i = 0; i < addrList.Length; i++)
                {
                    if (Reachable(addrList[i], port, 500))
                    {
                        addrOut = addrList[i];
                        break;
                    }
                }
            }

            if (addrOut == null)
            {
                for (int i = 0; i < addrList.Length; i++)
                {
                    if (Reachable(addrList[i], port, 2000))
                    {
                        addrOut = addrList[i];
                        break;
                    }
                }
            }

            if (addrOut == null)
            {
                for (int i = 0; i < addrList.Length; i++)
                {
                    if (Reachable(addrList[i], port, 30000))
                    {
                        addrOut = addrList[i];
                        break;
                    }
                }
            }

            return addrOut;
        }

        public static bool PortInUse(int port)
        {
            try
            {
                var tl = new TcpListener(IPAddress.Loopback, port);
                tl.Start();
                tl.Stop();
            }
            catch
            {
                return true;
            }

            return false;
        }
    }

    public static class BytesReverser
    {
        public static ushort ReverseBytes(this ushort input)
        {
            return (ushort) (((input & 0x00FFU) << 8) |
                             ((input & 0xFF00U) >> 8));
        }

        public static uint ReverseBytes(this uint input)
        {
            return ((input & 0x000000FFU) << 24) |
                   ((input & 0x0000FF00U) << 8) |
                   ((input & 0x00FF0000U) >> 8) |
                   ((input & 0xFF000000U) >> 24);
        }

        public static ulong ReverseBytes(this ulong input)
        {
            return (((input & 0x00000000000000FFUL) << 56) |
                    ((input & 0x000000000000FF00UL) << 40) |
                    ((input & 0x0000000000FF0000UL) << 24) |
                    ((input & 0x00000000FF000000UL) << 8) |
                    ((input & 0x000000FF00000000UL) >> 8) |
                    ((input & 0x0000FF0000000000UL) >> 24) |
                    ((input & 0x00FF000000000000UL) >> 40) |
                    ((input & 0xFF00000000000000UL) >> 56));
        }
    }
}