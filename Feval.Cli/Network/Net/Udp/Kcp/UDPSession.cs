using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Udp.Kcp
{
    class UDPSession
    {
        private Socket mSocket = null;
        private Kcp mKcp = null;

        private ByteBuffer mRecvBuffer = ByteBuffer.Allocate(1024 * 32);
        private UInt32 mNextUpdateTime = 0;

        public bool IsConnected { get { return mSocket != null && mSocket.Connected; } }
        public bool WriteDelay { get; set; }

        public void Connect(string host, int port)
        {
            var endpoint = IPAddress.Parse(host);
            mSocket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            mSocket.Connect(endpoint, port);
            mKcp = new Kcp((uint)(new Random().Next(1, Int32.MaxValue)), rawSend);
            // normal:  0, 40, 2, 1
            // fast:    0, 30, 2, 1
            // fast2:   1, 20, 2, 1
            // fast3:   1, 10, 2, 1
            mKcp.NoDelay(0, 30, 2, 1);
            mRecvBuffer.Clear();
        }

        public void Close()
        {
            if (mSocket != null) {
                mSocket.Close();
                mSocket = null;
                mRecvBuffer.Clear();
            }
        }

        private void rawSend(byte[] data, int length)
        {
            if (mSocket != null) {
                mSocket.Send(data, length, SocketFlags.None);
            }
        }

        public int Send(byte[] data, int index, int length)
        {
            if (mSocket == null)
                return -1;

            if (mKcp.WaitSnd >= mKcp.SndWnd) {
                return 0;
            }

            mNextUpdateTime = 0;

            var n = mKcp.Send(data, index, length);

            if (mKcp.WaitSnd >= mKcp.SndWnd || !WriteDelay) {
                mKcp.Flush(false);
            }
            return n;
        }

        public int Recv(byte[] data, int index, int length)
        {
            // 上次剩下的部分
            if (mRecvBuffer.ReadableBytes > 0) {
                var recvBytes = Math.Min(mRecvBuffer.ReadableBytes, length);
                Buffer.BlockCopy(mRecvBuffer.RawBuffer, mRecvBuffer.ReaderIndex, data, index, recvBytes);
                mRecvBuffer.ReaderIndex += recvBytes;
                // 读完重置读写指针
                if (mRecvBuffer.ReaderIndex == mRecvBuffer.WriterIndex) {
                    mRecvBuffer.Clear();
                }
                return recvBytes;
            }

            if (mSocket == null)
                return -1;

            if (!mSocket.Poll(0, SelectMode.SelectRead)) {
                return 0;
            }

            var rn = 0;
            try {
                rn = mSocket.Receive(mRecvBuffer.RawBuffer, mRecvBuffer.WriterIndex, mRecvBuffer.WritableBytes, SocketFlags.None);
            } catch(Exception ex) {
                Console.WriteLine(ex.Message);
                rn = -1;
            }
            
            if (rn <= 0) {
                return rn;
            }
            mRecvBuffer.WriterIndex += rn;

            var inputN = mKcp.Input(mRecvBuffer.RawBuffer, mRecvBuffer.ReaderIndex, mRecvBuffer.ReadableBytes, true, true);
            if (inputN < 0) {
                mRecvBuffer.Clear();
                return inputN;
            }
            mRecvBuffer.Clear();

            // 读完所有完整的消息
            for (;;) {
                var size = mKcp.PeekSize();
                if (size <= 0) break;

                mRecvBuffer.EnsureWritableBytes(size);

                var n = mKcp.Recv(mRecvBuffer.RawBuffer, mRecvBuffer.WriterIndex, size);
                if (n > 0) mRecvBuffer.WriterIndex += n;
            }

            // 有数据待接收
            if (mRecvBuffer.ReadableBytes > 0) {
                return Recv(data, index, length);
            }

            return 0;
        }

        public void Update()
        {
            if (mSocket == null)
                return;

            if (0 == mNextUpdateTime || mKcp.CurrentMS >= mNextUpdateTime)
            {
                mKcp.Update();
                mNextUpdateTime = mKcp.Check();
            }
        }
    }
}
