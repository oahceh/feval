using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Crypto
{
    public class AesDecryptor
    {
        AesManaged m_AesManager;
        ICryptoTransform m_AesDecryptor;

        public AesDecryptor(byte[] key, byte[] iv)
        {
            m_AesManager = new AesManaged {Mode = CipherMode.ECB, Padding = PaddingMode.None};
            m_AesDecryptor = new CounterModeCryptoTransform(m_AesManager, key, iv);
        }

        public long Decrypt(byte[] data, MemoryStream output, int offset = 0, int len = -1)
        {
            long beginPos = output.Position;
            len = len < 0 ? data.Length : len;
            output.SetLength(len + beginPos);
            var byteCount = m_AesDecryptor.TransformBlock(data, offset, len, output.GetBuffer(), (int) beginPos);
            output.Position = beginPos + byteCount;

            return byteCount;
        }
    }
}