using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Crypto
{
    public class AesEncryptor
    {
        AesManaged m_AesManager;
        ICryptoTransform m_AesEncryptor;

        public AesEncryptor(byte[] key, byte[] iv)
        {
            m_AesManager = new AesManaged {Mode = CipherMode.ECB, Padding = PaddingMode.None};
            m_AesEncryptor = new CounterModeCryptoTransform(m_AesManager, key, iv);
        }

        public long Encrypt(byte[] data, int offset, int len, MemoryStream output)
        {
            long beginPos = output.Position;
            output.SetLength(len + beginPos);
            var byteCount = m_AesEncryptor.TransformBlock(data, offset, len, output.GetBuffer(), (int) beginPos);
            output.Position = beginPos + byteCount;

            return byteCount;
        }
    }
}