using Crypto;
using Helper;
using Net.Common;
using Net.Udp.Kcp;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Net.Udp.Common
{
    public interface IUDPConnection : IConnection
    {
        string Key { get; }
        void Receive(byte[] buffer, int offset, int len);
        void Update();
    }

    public sealed class UDPConnection : IUDPConnection
    {
        enum ConnState
        {
            Transra = 0,
            Message = 1,
        }

        public IPEndPoint EndPoint { get; private set; }
        public IPack Pack { get; private set; }
        public string Key { get; private set; }
        public bool Running => true;

        Kcp.Kcp m_Kcp;
        uint m_NextUpdateTime = 0;
        object m_LockKcp = new object();
        ByteBuffer m_KcpBuffer = ByteBuffer.Allocate(1024 * 4);
        ByteBuffer m_RcvBuffer = ByteBuffer.Allocate(1024 * 4);
        bool m_Server;
        Action<byte[], int> m_OutPut;
        ConnState m_ConnState;
        string m_Rsa;
        byte[] m_Kiv;
        AesDecryptor m_AesDecryptor;
        AesEncryptor m_AesEncryptor;

        int m_SendIdx;
        int m_RecvIdx;
        byte m_CCCFlag;

        Func<IUDPConnection, IHandlerMessage> m_HFunc;
        Action<IConnection> m_Close;
        public IHandlerMessage Handler { get; private set; }

        public UDPConnection(Socket sock, IPEndPoint endPoint, IPack pack, Action<IConnection> close, string rsa,
            Func<IUDPConnection, IHandlerMessage> func, bool server, byte cflag)
        {
            EndPoint = endPoint;
            Pack = Pack;
            Key = EndPoint.Address + ":" + EndPoint.Port;
            m_Close = close;
            m_Rsa = rsa;
            m_HFunc = func;
            m_Server = server;
            m_CCCFlag = cflag;
            m_OutPut = (data, len) =>
            {
                if (server)
                {
                    sock.SendTo(data, len, SocketFlags.None, EndPoint);
                }
                else
                {
                    sock.Send(data, len, SocketFlags.None);
                }
            };
        }

        private int InternalSend(byte[] data, int offset, int len)
        {
            lock (m_LockKcp)
            {
                if (m_Kcp.WaitSnd >= m_Kcp.SndWnd)
                {
                    m_Kcp.Flush(false);
                }

                m_NextUpdateTime = 0;

                var n = m_Kcp.Send(data, offset, len);

                if (m_Kcp.WaitSnd >= m_Kcp.SndWnd)
                {
                    m_Kcp.Flush(false);
                }

                return n;
            }
        }

        private readonly object m_SendLock = new object();

        public void Send(byte[] data, int offset, int len)
        {
            lock (m_SendLock)
            {
                var ms = MemoryStreamPool.Get();
                {
                    Pack.Encode(data, ms, offset, len, m_AesEncryptor, m_SendIdx, m_CCCFlag);
                    m_SendIdx = (m_SendIdx + 1) % 0x1F;

                    InternalSend(ms.GetBuffer(), 0, (int) ms.Length);
                }
                MemoryStreamPool.Release(ms);
            }
        }

        public void Initialize()
        {
            lock (m_LockKcp)
            {
                m_Kcp = new Kcp.Kcp(10086, m_OutPut);
                // normal:  0, 40, 2, 1
                // fast:    0, 30, 2, 1
                // fast2:   1, 20, 2, 1
                // fast3:   1, 10, 2, 1
                m_Kcp.NoDelay(0, 30, 2, 1);
                m_KcpBuffer.Clear();
                m_ConnState = ConnState.Transra;
            }

            if (!m_Server)
            {
                var kiv = AesKeyIV.GenKeyIV();
                byte[] ekiv;
                if (!Rsa.Encrypt(m_Rsa, kiv, out ekiv))
                {
                    throw new Exception("Rsa Encrypt error");
                }

                m_Kcp.Send(ekiv, 0, ekiv.Length);
                m_Kcp.Flush(false);
                m_Kiv = kiv;
            }
        }

        public void Receive(byte[] buffer, int offset, int len)
        {
            lock (m_LockKcp)
            {
                m_KcpBuffer.EnsureWritableBytes(len);
                Buffer.BlockCopy(buffer, offset, m_KcpBuffer.RawBuffer, m_KcpBuffer.WriterIndex, len);
                m_KcpBuffer.WriterIndex += len;
            }
        }

        void OnReceived(ByteBuffer m_RcvBuffer)
        {
            if (m_ConnState == ConnState.Transra)
            {
                byte[] bytes = new byte[m_RcvBuffer.ReadableBytes];
                Buffer.BlockCopy(m_RcvBuffer.RawBuffer, 0, bytes, 0, m_RcvBuffer.ReadableBytes);
                m_RcvBuffer.Clear();
                if (m_Server)
                {
                    if (!Rsa.Decrypt(m_Rsa, bytes, out bytes))
                    {
                        throw new Exception(string.Format("Rsa Decrypt Error {0}", m_Rsa));
                    }

                    if (!AesKeyIV.Check(bytes))
                    {
                        throw new Exception(string.Format("Aes Key IV len error {0}", bytes.Length));
                    }
                    else
                    {
                        var kiv = AesKeyIV.GenKeyIV();
                        var encrypt = new AesEncryptor(bytes, bytes);
                        var ms = MemoryStreamPool.Get();
                        var dataLen = encrypt.Encrypt(kiv, 0, kiv.Length, ms);
                        m_Kcp.Send(ms.GetBuffer(), 0, (int) dataLen);
                        MemoryStreamPool.Release(ms);
                        m_Kiv = kiv;

                        m_AesDecryptor = new AesDecryptor(m_Kiv, m_Kiv);
                        m_AesEncryptor = new AesEncryptor(m_Kiv, m_Kiv);
                        m_SendIdx = 0;
                        m_RecvIdx = 0;
                        m_ConnState = ConnState.Message;
                        Handler = m_HFunc.Invoke(this);
                        Handler.HandleInitialize(this);
                        Handler.HandleConnected(true);
                    }
                }
                else
                {
                    //解Aes Key IV
                    var decrypt = new AesDecryptor(m_Kiv, m_Kiv);
                    var ms = MemoryStreamPool.Get();
                    var dataLen = decrypt.Decrypt(bytes, ms);
                    if (dataLen != AesKeyIV.KeyIVLen)
                    {
                        MemoryStreamPool.Release(ms);
                        throw new Exception(string.Format("Aes Key IV len error {0}", dataLen));
                    }
                    else
                    {
                        m_Kiv = ms.ToArray();
                        MemoryStreamPool.Release(ms);

                        m_AesDecryptor = new AesDecryptor(m_Kiv, m_Kiv);
                        m_AesEncryptor = new AesEncryptor(m_Kiv, m_Kiv);
                        m_SendIdx = 0;
                        m_RecvIdx = 0;
                        m_ConnState = ConnState.Message;
                        Handler = m_HFunc.Invoke(this);
                        Handler.HandleInitialize(this);
                        Handler.HandleConnected(true);
                    }
                }
            }
            else
            {
                SplitPack(m_RcvBuffer, PushPack);
            }
        }

        static void SplitPack(ByteBuffer buf, Action<byte[], int, int> push)
        {
            //当前buf位置指针
            var offset = buf.ReaderIndex;
            var buffer = buf.RawBuffer;
            var bufferSize = buffer.Length;
            var receivedSize = buf.ReadableBytes;
            //在mBuffer中可能有多个逻辑数据包，逐个解出
            while (receivedSize - offset > NetHelper.PackHeadSize)
            {
                //解包大小
                var packSize = NetHelper.ToInt32(buffer, offset);
                if (receivedSize - offset - NetHelper.PackHeadSize >= packSize) //已经接收了一个完整的包
                {
                    //当前buf指针加下包头偏移
                    offset += NetHelper.PackHeadSize;

                    //包体大小
                    //var pack = new byte[packSize];

                    //解MsgBody
                    //Buffer.BlockCopy(recvBuffer, offset, pack, 0, packSize);

                    //存起来
                    //push(pack);
                    push(buffer, offset, packSize);

                    //当前buf指针加下Body偏移
                    offset += packSize;
                }
                else if (bufferSize < packSize + NetHelper.PackHeadSize) //收到的包比buff大,需要做Buff的扩容
                {
                    //要扩容到的Buff大小
                    var newBuffSize = packSize + NetHelper.PackHeadSize;

                    //下面这段Baidu的 快速求 > newBuffSize 的 最小的2的幂次方数(原理近似快速的把最高为的1复制到右边所有的位置上然后+1)
                    newBuffSize |= (newBuffSize >> 1);
                    newBuffSize |= (newBuffSize >> 2);
                    newBuffSize |= (newBuffSize >> 4);
                    newBuffSize |= (newBuffSize >> 8);
                    newBuffSize |= (newBuffSize >> 16);
                    newBuffSize++;
                    if (newBuffSize < 0)
                    {
                        newBuffSize >>= 1;
                    }

                    buf.SkipBytes(offset);
                    buf.TrimReadedBytes();
                    buf.EnsureWritableBytes(newBuffSize);

                    bufferSize = newBuffSize;
                    buffer = buf.RawBuffer;
                    offset = 0;
                    break;
                }
                else //收到的包不完整 直接Break
                {
                    break;
                }
            }

            if (offset > 0)
            {
                buf.SkipBytes(offset);
                buf.TrimReadedBytes();
            }
        }

        void PushPack(byte[] buffer, int offset, int size)
        {
            if (!Running)
            {
                return;
            }

            var ms = MemoryStreamPool.Get();
            {
                Pack.Decode(buffer, ms, offset, size, m_AesDecryptor, m_RecvIdx); //解压缩，解加密
                m_RecvIdx = (m_RecvIdx + 1) % 0x1F;
                Handler?.Handle(ms);
            }
            // 为了优化一次内存拷贝，由上层来释放这个MemoryStream回池中
            // MemoryStreamPool.Release(ms);
        }

        public void Update()
        {
            var o = m_LockKcp;
            if (Monitor.TryEnter(o))
            {
                try
                {
                    if (true)
                    {
                        m_Kcp.Input(m_KcpBuffer.RawBuffer, m_KcpBuffer.ReaderIndex, m_KcpBuffer.ReadableBytes, true,
                            true);
                        m_KcpBuffer.Clear();
                    }

                    if (0 == m_NextUpdateTime || m_Kcp.CurrentMS >= m_NextUpdateTime)
                    {
                        m_Kcp.Update();
                        m_NextUpdateTime = m_Kcp.Check();
                    }

                    for (;;)
                    {
                        var size = m_Kcp.PeekSize();
                        if (size <= 0) break;
                        m_RcvBuffer.EnsureWritableBytes(size);
                        var n = m_Kcp.Recv(m_RcvBuffer.RawBuffer, m_RcvBuffer.WriterIndex, size);
                        if (n > 0) m_RcvBuffer.WriterIndex += n;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    Monitor.Exit(o);
                }
            }

            if (m_RcvBuffer.ReadableBytes > 0)
            {
                try
                {
                    OnReceived(m_RcvBuffer);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Close();
                }
            }
        }

        public void Close()
        {
            var close = m_Close;
            m_Close = null;
            close?.Invoke(this);
        }
    }
}