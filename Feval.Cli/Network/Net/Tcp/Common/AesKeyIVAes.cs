using Crypto;
using Helper;
using Net.Common;
using Net.Tcp.Client;
using System;
using System.Net.Sockets;


namespace Net.Tcp.Common
{
    internal class AesKeyIVAes : IIOCP
    {
        #region Property

        public Socket Socket { get; private set; }

        #endregion

        #region Interface

        public AesKeyIVAes(TCPClient.SocketArgs args, Action<TCPClient.SocketArgs, byte[]> conn,
            Action<TCPClient.SocketArgs> close, byte[] kiv)
        {
            m_KeyIV = kiv;
            m_HandleConnected = conn;
            m_HandleClose = close;
            m_SocketArgs = args;
            Socket = m_SocketArgs.socket;
            m_ReadState = ReadState.Head;
            IOCPConn.Start(this);
        }

        public void IOCPInitialize(SocketAsyncEventArgs e)
        {
            e.SetBuffer(0, 4);
        }

        public bool IOCPReceived(int dataLen, SocketAsyncEventArgs e)
        {
            if (e.UserToken != this)
            {
                return false;
            }

            var count = e.Count - dataLen;
            if (m_ReadState == ReadState.Head)
            {
                if (count == 0)
                {
                    //解包大小
                    var len = NetHelper.ToInt32(e.Buffer, 0);
                    if (len != e.Buffer.Length)
                    {
                        BufferManager.Free(e);
                        var buffer = new byte[len];
                        e.SetBuffer(buffer, 0, len);
                    }
                    else
                    {
                        e.SetBuffer(0, len);
                    }

                    m_ReadState = ReadState.Body;
                }
                else
                {
                    var offset = dataLen + e.Offset;
                    e.SetBuffer(offset, count);
                }
            }
            else
            {
                if (count == 0)
                {
                    //解Aes Key IV
                    var decrypt = new AesDecryptor(m_KeyIV, m_KeyIV);
                    var ms = MemoryStreamPool.Get();
                    var resultLen = decrypt.Decrypt(e.Buffer, ms);
                    if (resultLen != AesKeyIV.KeyIVLen)
                    {
                        Console.WriteLine($"Aes Key IV len error {ms.Length}");
                        IOCPConn.Close(e);
                    }
                    else
                    {
                        m_HandleConnected?.Invoke(m_SocketArgs, ms.ToArray());
                    }

                    MemoryStreamPool.Release(ms);
                    return false;
                }

                var offset = dataLen + e.Offset;
                e.SetBuffer(offset, count);
            }

            return true;
        }

        public void IOCPClose()
        {
            Socket = null;
            m_HandleClose?.Invoke(m_SocketArgs);
        }

        #endregion

        #region Field

        private enum ReadState
        {
            Head = 0,
            Body = 1,
        }

        private readonly Action<TCPClient.SocketArgs, byte[]> m_HandleConnected;

        private readonly Action<TCPClient.SocketArgs> m_HandleClose;

        private readonly byte[] m_KeyIV;

        private readonly TCPClient.SocketArgs m_SocketArgs;

        private ReadState m_ReadState;

        #endregion
    }
}