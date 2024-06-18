using System;
using Crypto;
using Helper;
using Net.Common;
using System.Net.Sockets;


namespace Net.Tcp.Common
{
    internal class AesKeyIVRsa : IIOCP
    {
        #region Property

        public Socket Socket { get; private set; }

        #endregion

        #region Interface

        public AesKeyIVRsa(Socket sock, IHandlerServer handler, IServer server, string rsa, byte cccFlag)
        {
            m_Handler = handler;
            m_Server = server;
            m_RsaKey = rsa;
            m_CCCFlag = cccFlag;
            Socket = sock;
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
                    // 解包大小
                    var len = NetHelper.ToInt32(e.Buffer, 0);
                    if (len < 0)
                    {
                        Console.WriteLine($"Invalid data length: {len}");
                        IOCPConn.Close(e);
                        return false;
                    }

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
                    // 解Aes Key IV
                    if (!Rsa.Decrypt(m_RsaKey, e.Buffer, out var bytes))
                    {
                        Console.WriteLine($"Rsa Decrypt Error {m_RsaKey}");
                        IOCPConn.Close(e);
                        return false;
                    }

                    if (!AesKeyIV.Check(bytes))
                    {
                        Console.WriteLine($"Aes Key IV len error {bytes.Length}");
                        IOCPConn.Close(e);
                        return false;
                    }

                    var kiv = AesKeyIV.GenKeyIV();
                    AesKeyIV.SendAesKeyIVAes(Socket, bytes, kiv);
                    m_Handler.HandleAcceptConnected((h, c) =>
                        new TCPConnection(Socket, h, m_Server.Pack, c, kiv, m_CCCFlag));
                    return false;
                }

                var offset = dataLen + e.Offset;
                e.SetBuffer(offset, count);
            }

            return true;
        }

        public void IOCPClose()
        {
            IOCPConn.Close(Socket);
            Socket = null;
        }

        #endregion

        #region Field

        private enum ReadState
        {
            Head = 0,
            Body = 1,
        }

        private IHandlerServer m_Handler;

        private IServer m_Server;

        private string m_RsaKey;

        private byte m_CCCFlag;

        private ReadState m_ReadState;

        #endregion
    }
}