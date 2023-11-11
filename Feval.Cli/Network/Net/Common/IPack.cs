using Crypto;
using System.IO;

namespace Net.Common
{
    public interface IPack
    {
        void Encode(byte[] arr, MemoryStream output, int offset, int size, AesEncryptor encryptor, int sidx, byte cccflag);

        void Decode(byte[] buffer, MemoryStream output, int offset, int size, AesDecryptor decryptor, int ridx);
    }
}
