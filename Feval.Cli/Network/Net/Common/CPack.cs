using Crypto;
using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Common
{
    class CPack : IPack
    {
        void IPack.Encode(byte[] arr, MemoryStream output, int offset, int len, AesEncryptor encryptor, int sidx,
            byte cccflag)
        {
            byte flag = (byte) (sidx & 0x1F);
            int crc32 = 0;
            cccflag = NetHelper.CCC_Crypto;
            if (((cccflag & NetHelper.CCC_Compress) == NetHelper.CCC_Compress))
            {
                if (arr.Length >= 1024)
                {
                    arr = ZLib.Zip(arr);
                    flag |= NetHelper.CCC_Compress;
                }
            }

            if (((cccflag & NetHelper.CCC_Crypto) == NetHelper.CCC_Crypto))
            {
                bool aesed = (new Random().Next() % 10) < 3;
                if (aesed)
                {
                    MemoryStream stream = new MemoryStream();
                    encryptor.Encrypt(arr, offset, len, stream);
                    arr = stream.ToArray();
                    flag |= NetHelper.CCC_Crypto;
                }
            }

            if (((cccflag & NetHelper.CCC_Crc) == NetHelper.CCC_Crc))
            {
                bool crced = (new Random().Next() % 10) < 3;
                if (crced)
                {
                    crc32 = Crc.Crc32(arr);
                    flag |= NetHelper.CCC_Crc;
                }
            }

            int alllen = arr.Length + 1 + 4;
            using (var os = new MemoryStream())
            {
                using (var writer = new BinaryWriter(os))
                {
                    writer.Write(NetHelper.GetTransferUInt((uint)alllen)); //allLen
                    writer.Write(flag); //flag
                    writer.Write(NetHelper.GetTransferUInt((uint)crc32)); //crc
                    writer.Write(arr, 0, arr.Length);
                    writer.Flush();
                    var result = os.ToArray();
                    output.Write(result, 0, result.Length);
                }
            }
        }

        void IPack.Decode(byte[] buffer, MemoryStream output, int offset, int size, AesDecryptor decryptor, int ridx)
        {
            byte flag = buffer[offset];
            bool ziped = ((flag & 0x80) == 0x80);
            bool aesed = ((flag & 0x40) == 0x40);
            bool crced = ((flag & 0x20) == 0x20);

            int idx = flag & 0x1F;
            if (ridx == idx)
            {
                int crc32 = NetHelper.ToInt32(buffer, offset + 1);
                int ncrc32 = 0;
                if (crced)
                {
                    ncrc32 = Crc.Crc32(buffer, offset + 1 + 4, size - 1 - 4);
                }

                if (ncrc32 == crc32)
                {
                    byte[] data;
                    bool cached = false;
                    if (aesed && ziped)
                    {
                        var ms = MemoryStreamPool.Get();
                        decryptor.Decrypt(buffer, ms, offset + 1 + 4, size - 1 - 4);
                        data = ZLib.UnZip(ms.GetBuffer(), 0, (int) ms.Length);
                        MemoryStreamPool.Release(ms);
                    }
                    else if (aesed)
                    {
                        decryptor.Decrypt(buffer, output, offset + 1 + 4, size - 1 - 4);
                        data = null;
                        cached = true;
                    }
                    else if (ziped)
                    {
                        data = ZLib.UnZip(buffer, offset + 1 + 4, size - 1 - 4);
                    }
                    else
                    {
                        data = new byte[size - 1 - 4];
                        Buffer.BlockCopy(buffer, offset + 1 + 4, data, 0, data.Length);
                    }

                    if (data != null)
                    {
                        output.Write(data, 0, data.Length);
                    }
                    else if (!cached)
                    {
                        throw new Exception("Recv Decode data null");
                    }
                }
                else
                {
                    throw new Exception("Recv error crc32 " + crc32 + "   ncrc32" + ncrc32);
                }
            }
            else
            {
                throw new Exception("Recv error idx " + idx + "   lidx" + ridx);
            }
        }
    }
}