using Crypto;
using Helper;
using Net.Common;
using System;
using System.Net;
using System.Net.Sockets;


namespace Net.Tcp.Common
{
    public sealed class TCPConnection : IConnection, IIOCP
    {
        #region Property

        public IHandlerMessage Handler { get; private set; }

        public IPack Pack { get; private set; }

        public bool Running { get; private set; }

        public IPEndPoint EndPoint { get; private set; }

        public Socket Socket { get; private set; }

        #endregion

        #region Interface

        public TCPConnection(Socket sock, IHandlerMessage handler, IPack pack, Action<IConnection> close, byte[] kiv,
            byte cccFlag)
        {
            Running = false;
            m_Initialized = false;
            Handler = handler;
            Pack = pack;
            m_Close = close;
            m_SendIdx = 0;
            m_RecvIdx = 0;
            m_CCCFlag = cccFlag;
            m_AesDecryptor = new AesDecryptor(kiv, kiv);
            m_AesEncryptor = new AesEncryptor(kiv, kiv);
            EndPoint = (IPEndPoint) (sock.RemoteEndPoint);
            Socket = sock;
        }

        public void Initialize()
        {
            if (!m_Initialized)
            {
                m_Initialized = true;
                Running = true;
                Handler.HandleInitialize(this);
                Handler.HandleConnected(true);
                IOCPConn.Start(this);
            }
        }

        public void Close()
        {
            if (Running)
            {
                Running = false;
                IOCPConn.Close(Socket);
                Socket = null;
                var close = m_Close;
                m_Close = null;
                close?.Invoke(this);
                m_Close = null;
                Handler?.HandleDisconnected();
                Handler?.HandleClose();
                Handler = null;
            }
        }

        /// <summary>
        /// 同步发送数据
        /// </summary>
        public void Send(byte[] buffer, int offset, int len)
        {
            SendInternal(buffer, offset, len);
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        public void BeginSend(byte[] buffer, int offset, int len)
        {
            BeginSendInternal(buffer, offset, len);
        }

        public void IOCPInitialize(SocketAsyncEventArgs e)
        {
        }

        public bool IOCPReceived(int len, SocketAsyncEventArgs e)
        {
            if (e.UserToken != this)
            {
                return false;
            }

            SplitPack(len, e, PushPack);
            return true;
        }

        public void IOCPClose()
        {
            Close();
        }

        #endregion

        #region Method

        private void BeginSendInternal(byte[] buffer, int offset, int len)
        {
            try
            {
                // 这个锁是用来保证CurSendIdx顺序的，不加锁多线程情况下会出现CurSendIdx 乱序到达目的地
                lock (m_SendLock)
                {
                    var ms = MemoryStreamPool.Get();
                    {
                        Pack.Encode(buffer, ms, offset, len, m_AesEncryptor, m_SendIdx, m_CCCFlag);
                        m_SendIdx = (m_SendIdx + 1) % 0x1F;
                        // 这里不得不通过ToArray()创建一份数据的副本，因为BeginSend()是一个异步过程，期间始终需要持有数据的引用。
                        var encodedData = ms.ToArray();
                        Socket.BeginSend(encodedData, 0, encodedData.Length, SocketFlags.None, SendCallback, this);
                    }
                    MemoryStreamPool.Release(ms);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Close();
            }
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        private void SendInternal(byte[] buffer, int offset, int length)
        {
            try
            {
                // 这个锁是用来保证CurSendIdx顺序的，不加锁多线程情况下会出现CurSendIdx 乱序到达目的地
                lock (m_SendLock)
                {
                    var ms = MemoryStreamPool.Get();
                    {
                        Pack.Encode(buffer, ms, offset, length, m_AesEncryptor, m_SendIdx, m_CCCFlag);
                        m_SendIdx = (m_SendIdx + 1) % 0x1F;

                        // 这里为避免多一次Copy并没有使用MemoryStream.ToArray
                        // 但MemoryStream的起始偏移并没有暴露, 这就要求ms的偏移
                        // 一定要为0, 这种做法虽不靠谱, 但考虑到收益和Network本
                        // 身内部库代码修改的需求较少, 就这样做了吧...
                        var data = ms.GetBuffer();

                        // 临时简单处理一下大包分小包, 以解决部分低端设备弱网一直重传
                        const int bytesPerSend = 200;
                        var time = (int) ms.Length / 200;
                        for (var i = 0; i < time; i++)
                        {
                            Socket.Send(data, i * bytesPerSend, bytesPerSend, SocketFlags.None);
                        }

                        var bytesSent = time * bytesPerSend;
                        var bytesLeft = (int) ms.Length - bytesSent;
                        if (bytesLeft > 0)
                        {
                            Socket.Send(data, bytesSent, bytesLeft, SocketFlags.None);
                        }
                    }
                    MemoryStreamPool.Release(ms);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Close();
            }
        }

        /// <summary>
        /// 发送回调，这里仅做一下异常处理
        /// </summary>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Close();
            }
        }

        private void PushPack(byte[] buffer, int offset, int size)
        {
            if (!Running)
            {
                return;
            }

            var ms = MemoryStreamPool.Get();
            {
                // 解压缩，解加密
                Pack.Decode(buffer, ms, offset, size, m_AesDecryptor, m_RecvIdx);
                m_RecvIdx = (m_RecvIdx + 1) % 0x1F;
                Handler?.Handle(ms);
            }
            // 为了优化一次内存拷贝，由上层来释放这个MemoryStream回池中
            // MemoryStreamPool.Release(ms);
        }

        private static void SplitPack(int len, SocketAsyncEventArgs e, Action<byte[], int, int> push)
        {
            //当前buf位置指针
            var offset = 0;
            var buffer = e.Buffer;
            var bufferSize = buffer.Length;
            var receivedSize = bufferSize - e.Count + len;
            // 在mBuffer中可能有多个逻辑数据包，逐个解出
            while (receivedSize - offset > NetHelper.PackHeadSize)
            {
                // 解包大小
                var packSize = NetHelper.ToInt32(buffer, offset);
                // 已经接收了一个完整的包
                if (receivedSize - offset - NetHelper.PackHeadSize >= packSize)
                {
                    // 当前buf指针加下包头偏移
                    offset += NetHelper.PackHeadSize;
                    // 存起来
                    push(buffer, offset, packSize);
                    // 当前buf指针加下Body偏移
                    offset += packSize;
                }
                // 收到的包比buff大,需要做Buff的扩容
                else if (bufferSize < packSize + NetHelper.PackHeadSize)
                {
                    // 要扩容到的Buff大小
                    var newBuffSize = packSize + NetHelper.PackHeadSize;

                    // 下面这段Baidu的 快速求 > newBuffSize 的 最小的2的幂次方数(原理近似快速的把最高为的1复制到右边所有的位置上然后+1)
                    newBuffSize |= newBuffSize >> 1;
                    newBuffSize |= newBuffSize >> 2;
                    newBuffSize |= newBuffSize >> 4;
                    newBuffSize |= newBuffSize >> 8;
                    newBuffSize |= newBuffSize >> 16;
                    newBuffSize++;
                    if (newBuffSize < 0)
                    {
                        newBuffSize >>= 1;
                    }

                    var newBuff = new byte[newBuffSize];

                    // 拷贝剩余的有效内容到新的buff
                    // Buffer中真正剩余的有效内容
                    receivedSize -= offset;
                    Buffer.BlockCopy(buffer, offset, newBuff, 0, receivedSize);
                    bufferSize = newBuffSize;
                    buffer = newBuff;
                    offset = 0;
                    break;
                }
                // 收到的包不完整 直接Break
                else
                {
                    break;
                }
            }

            receivedSize -= offset;
            if (receivedSize > 0)
            {
                //buf内容前移
                Buffer.BlockCopy(buffer, offset, buffer, 0, receivedSize);
            }

            e.SetBuffer(buffer, receivedSize, bufferSize - receivedSize);
        }

        #endregion

        #region Field

        private Action<IConnection> m_Close;

        private bool m_Initialized;

        private AesDecryptor m_AesDecryptor;

        private AesEncryptor m_AesEncryptor;

        private int m_SendIdx;

        private int m_RecvIdx;

        private byte m_CCCFlag;

        private readonly object m_SendLock = new object();

        #endregion
    }
}